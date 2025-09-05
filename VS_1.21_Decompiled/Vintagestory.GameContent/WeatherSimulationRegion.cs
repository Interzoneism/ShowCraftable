using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class WeatherSimulationRegion
{
	public bool Transitioning;

	public float TransitionDelay;

	public WeatherPattern NewWePattern;

	public WeatherPattern OldWePattern;

	public RingArray<SnowAccumSnapshot> SnowAccumSnapshots;

	public static object snowAccumSnapshotLock = new object();

	public WindPattern CurWindPattern;

	public WeatherEvent CurWeatherEvent;

	public float Weight;

	public double LastUpdateTotalHours;

	public LCGRandom Rand;

	public int regionX;

	public int regionZ;

	public int cloudTilebasePosX;

	public int cloudTilebasePosZ;

	public WeatherDataSnapshot weatherData = new WeatherDataSnapshot();

	public bool IsInitialized;

	public bool IsDummy;

	public WeatherPattern[] WeatherPatterns;

	public WindPattern[] WindPatterns;

	public WeatherEvent[] WeatherEvents;

	protected WeatherSystemBase ws;

	protected WeatherSystemServer wsServer;

	protected ICoreClientAPI capi;

	protected float quarterSecAccum;

	protected BlockPos regionCenterPos;

	protected Vec3d tmpVecPos = new Vec3d();

	public IMapRegion MapRegion;

	public static int snowAccumResolution = 2;

	public WeatherSimulationRegion(WeatherSystemBase ws, int regionX, int regionZ)
	{
		this.ws = ws;
		this.regionX = regionX;
		this.regionZ = regionZ;
		SnowAccumSnapshots = new RingArray<SnowAccumSnapshot>((int)((float)ws.api.World.Calendar.DaysPerYear * ws.api.World.Calendar.HoursPerDay) + 1);
		int regionSize = ws.api.World.BlockAccessor.RegionSize;
		LastUpdateTotalHours = ws.api.World.Calendar.TotalHours;
		cloudTilebasePosX = regionX * regionSize / ws.CloudTileSize;
		cloudTilebasePosZ = regionZ * regionSize / ws.CloudTileSize;
		regionCenterPos = new BlockPos(regionX * regionSize + regionSize / 2, 0, regionZ * regionSize + regionSize / 2);
		Rand = new LCGRandom(ws.api.World.Seed);
		Rand.InitPositionSeed(regionX / 3, regionZ / 3);
		weatherData.Ambient = new AmbientModifier().EnsurePopulated();
		if (ws.api.Side == EnumAppSide.Client)
		{
			capi = ws.api as ICoreClientAPI;
			weatherData.Ambient.FogColor = capi.Ambient.Base.FogColor.Clone();
		}
		else
		{
			wsServer = ws as WeatherSystemServer;
		}
		ReloadPatterns(ws.api.World.Seed);
	}

	internal void ReloadPatterns(int seed)
	{
		WeatherPatterns = new WeatherPattern[ws.WeatherConfigs.Length];
		for (int i = 0; i < ws.WeatherConfigs.Length; i++)
		{
			WeatherPatterns[i] = new WeatherPattern(ws, ws.WeatherConfigs[i], Rand, cloudTilebasePosX, cloudTilebasePosZ);
			WeatherPatterns[i].State.Index = i;
		}
		WindPatterns = new WindPattern[ws.WindConfigs.Length];
		for (int j = 0; j < ws.WindConfigs.Length; j++)
		{
			WindPatterns[j] = new WindPattern(ws.api, ws.WindConfigs[j], j, Rand, seed);
		}
		WeatherEvents = new WeatherEvent[ws.WeatherEventConfigs.Length];
		for (int k = 0; k < ws.WeatherEventConfigs.Length; k++)
		{
			WeatherEvents[k] = new WeatherEvent(ws.api, ws.WeatherEventConfigs[k], k, Rand, seed - 876);
		}
	}

	internal void LoadRandomPattern()
	{
		NewWePattern = RandomWeatherPattern();
		OldWePattern = RandomWeatherPattern();
		NewWePattern.OnBeginUse();
		OldWePattern.OnBeginUse();
		CurWindPattern = WindPatterns[Rand.NextInt(WindPatterns.Length)];
		CurWindPattern.OnBeginUse();
		CurWeatherEvent = RandomWeatherEvent();
		CurWeatherEvent.OnBeginUse();
		Weight = 1f;
		wsServer?.SendWeatherStateUpdate(new WeatherState
		{
			RegionX = regionX,
			RegionZ = regionZ,
			NewPattern = NewWePattern.State,
			OldPattern = OldWePattern.State,
			WindPattern = CurWindPattern.State,
			WeatherEvent = CurWeatherEvent?.State,
			TransitionDelay = 0f,
			Transitioning = false,
			Weight = Weight,
			updateInstant = false,
			LcgCurrentSeed = Rand.currentSeed,
			LcgMapGenSeed = Rand.mapGenSeed,
			LcgWorldSeed = Rand.worldSeed
		});
	}

	internal void Initialize()
	{
		for (int i = 0; i < WeatherPatterns.Length; i++)
		{
			WeatherPatterns[i].Initialize(i, ws.api.World.Seed);
		}
		NewWePattern = WeatherPatterns[0];
		OldWePattern = WeatherPatterns[0];
		CurWindPattern = WindPatterns[0];
		CurWeatherEvent = WeatherEvents[0];
		IsInitialized = true;
	}

	public void UpdateWeatherData()
	{
		weatherData.SetAmbientLerped(OldWePattern, NewWePattern, Weight, (capi == null) ? 0f : capi.Ambient.Base.FogDensity.Value);
	}

	public void UpdateSnowAccumulation(int count)
	{
		SnowAccumSnapshot[] array = new SnowAccumSnapshot[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = new SnowAccumSnapshot
			{
				TotalHours = LastUpdateTotalHours + (double)i,
				SnowAccumulationByRegionCorner = new FloatDataMap3D(snowAccumResolution, snowAccumResolution, snowAccumResolution)
			};
		}
		BlockPos blockPos = new BlockPos();
		int regionSize = ws.api.World.BlockAccessor.RegionSize;
		for (int j = 0; j < snowAccumResolution; j++)
		{
			for (int k = 0; k < snowAccumResolution; k++)
			{
				for (int l = 0; l < snowAccumResolution; l++)
				{
					int y = ((k == 0) ? ws.api.World.SeaLevel : (ws.api.World.BlockAccessor.MapSizeY - 1));
					blockPos.Set(regionX * regionSize + j * (regionSize - 1), y, regionZ * regionSize + l * (regionSize - 1));
					ClimateCondition climateCondition = null;
					for (int m = 0; m < array.Length; m++)
					{
						double totalDays = (LastUpdateTotalHours + (double)m + 0.5) / (double)ws.api.World.Calendar.HoursPerDay;
						if (climateCondition == null)
						{
							climateCondition = ws.api.World.BlockAccessor.GetClimateAt(blockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureRainfallOnly, totalDays);
							if (climateCondition == null)
							{
								return;
							}
						}
						else
						{
							ws.api.World.BlockAccessor.GetClimateAt(blockPos, climateCondition, EnumGetClimateMode.ForSuppliedDate_TemperatureRainfallOnly, totalDays);
						}
						SnowAccumSnapshot snowAccumSnapshot = array[m];
						if (climateCondition.Temperature > 1.5f || ((double)climateCondition.Rainfall < 0.05 && climateCondition.Temperature > 0f))
						{
							snowAccumSnapshot.SnowAccumulationByRegionCorner.AddValue(j, k, l, (0f - climateCondition.Temperature) / 15f);
						}
						else
						{
							snowAccumSnapshot.SnowAccumulationByRegionCorner.AddValue(j, k, l, climateCondition.Rainfall / 3f);
						}
					}
				}
			}
		}
		lock (snowAccumSnapshotLock)
		{
			foreach (SnowAccumSnapshot snowAccumSnapshot2 in array)
			{
				SnowAccumSnapshots.Add(snowAccumSnapshot2);
				snowAccumSnapshot2.Checks++;
			}
		}
		LastUpdateTotalHours += count;
	}

	public void TickEvery25ms(float dt)
	{
		if (ws.api.Side == EnumAppSide.Client)
		{
			clientUpdate(dt);
		}
		else
		{
			int num = (int)(ws.api.World.Calendar.TotalHours - LastUpdateTotalHours);
			if (num > 0)
			{
				UpdateSnowAccumulation(Math.Min(num, 480));
			}
			ws.api.World.FrameProfiler.Mark("snowaccum");
			Random rand = ws.api.World.Rand;
			float lightningMinTemp = CurWeatherEvent.State.LightningMinTemp;
			if (rand.NextDouble() < (double)CurWeatherEvent.State.LightningRate)
			{
				ClimateCondition climateAt = ws.api.World.BlockAccessor.GetClimateAt(regionCenterPos);
				if (climateAt.Temperature >= lightningMinTemp && (double)climateAt.RainCloudOverlay > 0.15)
				{
					Vec3d pos = regionCenterPos.ToVec3d().Add(-200.0 + rand.NextDouble() * 400.0, ws.api.World.SeaLevel, -200.0 + rand.NextDouble() * 400.0);
					ws.SpawnLightningFlash(pos);
				}
				ws.api.World.FrameProfiler.Mark("lightningcheck");
			}
		}
		if (Transitioning)
		{
			float num2 = ws.api.World.Calendar.SpeedOfTime / 60f;
			Weight += dt / TransitionDelay * num2;
			if (Weight > 1f)
			{
				Transitioning = false;
				Weight = 1f;
			}
		}
		else if (ws.autoChangePatterns && ws.api.Side == EnumAppSide.Server && ws.api.World.Calendar.TotalHours > NewWePattern.State.ActiveUntilTotalHours)
		{
			TriggerTransition();
			ws.api.World.FrameProfiler.Mark("weathertransition");
		}
		if (ws.autoChangePatterns && ws.api.Side == EnumAppSide.Server)
		{
			bool flag = false;
			if (ws.api.World.Calendar.TotalHours > CurWindPattern.State.ActiveUntilTotalHours)
			{
				CurWindPattern = WindPatterns[Rand.NextInt(WindPatterns.Length)];
				CurWindPattern.OnBeginUse();
				flag = true;
			}
			if (ws.api.World.Calendar.TotalHours > CurWeatherEvent.State.ActiveUntilTotalHours || CurWeatherEvent.ShouldStop(weatherData.climateCond.Rainfall, weatherData.climateCond.Temperature))
			{
				selectRandomWeatherEvent();
				flag = true;
			}
			if (flag)
			{
				sendWeatherUpdatePacket();
			}
			ws.api.World.FrameProfiler.Mark("weatherchange");
		}
		NewWePattern.Update(dt);
		OldWePattern.Update(dt);
		CurWindPattern.Update(dt);
		CurWeatherEvent.Update(dt);
		float x = weatherData.curWindSpeed.X;
		float num3 = (float)GetWindSpeed(ws.api.World.SeaLevel);
		x += GameMath.Clamp((num3 - x) * dt, -0.001f, 0.001f);
		weatherData.curWindSpeed.X = x;
		quarterSecAccum += dt;
		if (quarterSecAccum > 0.25f)
		{
			regionCenterPos.Y = ws.api.World.BlockAccessor.GetRainMapHeightAt(regionCenterPos);
			if (regionCenterPos.Y == 0)
			{
				regionCenterPos.Y = ws.api.World.SeaLevel;
			}
			ClimateCondition climateAt2 = ws.api.World.BlockAccessor.GetClimateAt(regionCenterPos);
			if (climateAt2 != null)
			{
				weatherData.climateCond = climateAt2;
			}
			quarterSecAccum = 0f;
		}
		weatherData.BlendedPrecType = CurWeatherEvent.State.PrecType;
	}

	public void selectRandomWeatherEvent()
	{
		CurWeatherEvent = RandomWeatherEvent();
		CurWeatherEvent.OnBeginUse();
	}

	public void sendWeatherUpdatePacket()
	{
		wsServer.SendWeatherStateUpdate(new WeatherState
		{
			RegionX = regionX,
			RegionZ = regionZ,
			NewPattern = NewWePattern.State,
			OldPattern = OldWePattern.State,
			WindPattern = CurWindPattern.State,
			WeatherEvent = CurWeatherEvent?.State,
			TransitionDelay = TransitionDelay,
			Transitioning = Transitioning,
			Weight = Weight,
			LcgCurrentSeed = Rand.currentSeed,
			LcgMapGenSeed = Rand.mapGenSeed,
			LcgWorldSeed = Rand.worldSeed
		});
	}

	private void clientUpdate(float dt)
	{
		EntityPlayer entity = (ws.api as ICoreClientAPI).World.Player.Entity;
		regionCenterPos.Y = (int)entity.Pos.Y;
		float nearThunderRate = CurWeatherEvent.State.NearThunderRate;
		float distantThunderRate = CurWeatherEvent.State.DistantThunderRate;
		float lightningMinTemp = CurWeatherEvent.State.LightningMinTemp;
		weatherData.nearLightningRate += GameMath.Clamp((nearThunderRate - weatherData.nearLightningRate) * dt, -0.001f, 0.001f);
		weatherData.distantLightningRate += GameMath.Clamp((distantThunderRate - weatherData.distantLightningRate) * dt, -0.001f, 0.001f);
		weatherData.lightningMinTemp += GameMath.Clamp((lightningMinTemp - weatherData.lightningMinTemp) * dt, -0.001f, 0.001f);
		weatherData.BlendedPrecType = CurWeatherEvent.State.PrecType;
	}

	public double GetWindSpeed(double posY)
	{
		if (CurWindPattern == null)
		{
			return 0.0;
		}
		double num = CurWindPattern.Strength;
		if (posY > (double)ws.api.World.SeaLevel)
		{
			num *= Math.Max(1.0, 0.9 + (posY - (double)ws.api.World.SeaLevel) / 100.0);
			return Math.Min(num, 1.5);
		}
		return num / (1.0 + ((double)ws.api.World.SeaLevel - posY) / 4.0);
	}

	public EnumPrecipitationType GetPrecipitationType()
	{
		return weatherData.BlendedPrecType;
	}

	public bool SetWindPattern(string code, bool updateInstant)
	{
		WindPattern windPattern = WindPatterns.FirstOrDefault((WindPattern p) => p.config.Code == code);
		if (windPattern == null)
		{
			return false;
		}
		CurWindPattern = windPattern;
		CurWindPattern.OnBeginUse();
		sendState(updateInstant);
		return true;
	}

	public bool SetWeatherEvent(string code, bool updateInstant)
	{
		WeatherEvent weatherEvent = WeatherEvents.FirstOrDefault((WeatherEvent p) => p.config.Code == code);
		if (weatherEvent == null)
		{
			return false;
		}
		CurWeatherEvent = weatherEvent;
		CurWeatherEvent.OnBeginUse();
		sendState(updateInstant);
		return true;
	}

	public bool SetWeatherPattern(string code, bool updateInstant)
	{
		WeatherPattern weatherPattern = WeatherPatterns.FirstOrDefault((WeatherPattern p) => p.config.Code == code);
		if (weatherPattern == null)
		{
			return false;
		}
		OldWePattern = NewWePattern;
		NewWePattern = weatherPattern;
		Weight = 1f;
		Transitioning = false;
		TransitionDelay = 0f;
		if (NewWePattern != OldWePattern || updateInstant)
		{
			NewWePattern.OnBeginUse();
		}
		UpdateWeatherData();
		sendState(updateInstant);
		return true;
	}

	private void sendState(bool updateInstant)
	{
		wsServer.SendWeatherStateUpdate(new WeatherState
		{
			RegionX = regionX,
			RegionZ = regionZ,
			NewPattern = NewWePattern.State,
			OldPattern = OldWePattern.State,
			WindPattern = CurWindPattern.State,
			WeatherEvent = CurWeatherEvent?.State,
			TransitionDelay = 0f,
			Transitioning = false,
			Weight = Weight,
			updateInstant = updateInstant,
			LcgCurrentSeed = Rand.currentSeed,
			LcgMapGenSeed = Rand.mapGenSeed,
			LcgWorldSeed = Rand.worldSeed
		});
	}

	public void TriggerTransition()
	{
		TriggerTransition(30f + Rand.NextFloat() * 60f * 60f / ws.api.World.Calendar.SpeedOfTime);
	}

	public void TriggerTransition(float delay)
	{
		Transitioning = true;
		TransitionDelay = delay;
		Weight = 0f;
		OldWePattern = NewWePattern;
		NewWePattern = RandomWeatherPattern();
		if (NewWePattern != OldWePattern)
		{
			NewWePattern.OnBeginUse();
		}
		wsServer.SendWeatherStateUpdate(new WeatherState
		{
			RegionX = regionX,
			RegionZ = regionZ,
			NewPattern = NewWePattern.State,
			OldPattern = OldWePattern.State,
			WindPattern = CurWindPattern.State,
			WeatherEvent = CurWeatherEvent?.State,
			TransitionDelay = TransitionDelay,
			Transitioning = true,
			Weight = Weight,
			LcgCurrentSeed = Rand.currentSeed,
			LcgMapGenSeed = Rand.mapGenSeed,
			LcgWorldSeed = Rand.worldSeed
		});
	}

	public WeatherEvent RandomWeatherEvent()
	{
		float num = 0f;
		for (int i = 0; i < WeatherEvents.Length; i++)
		{
			WeatherEvents[i].updateHereChance(weatherData.climateCond.WorldgenRainfall, weatherData.climateCond.Temperature);
			num += WeatherEvents[i].hereChance;
		}
		float num2 = Rand.NextFloat() * num;
		for (int j = 0; j < WeatherEvents.Length; j++)
		{
			num2 -= WeatherEvents[j].config.Weight;
			if (num2 <= 0f)
			{
				return WeatherEvents[j];
			}
		}
		return WeatherEvents[WeatherEvents.Length - 1];
	}

	public WeatherPattern RandomWeatherPattern()
	{
		float num = 0f;
		for (int i = 0; i < WeatherPatterns.Length; i++)
		{
			WeatherPatterns[i].updateHereChance(weatherData.climateCond.Rainfall, weatherData.climateCond.Temperature);
			num += WeatherPatterns[i].hereChance;
		}
		float num2 = Rand.NextFloat() * num;
		for (int j = 0; j < WeatherPatterns.Length; j++)
		{
			num2 -= WeatherPatterns[j].hereChance;
			if (num2 <= 0f)
			{
				return WeatherPatterns[j];
			}
		}
		return WeatherPatterns[WeatherPatterns.Length - 1];
	}

	public double GetBlendedCloudThicknessAt(int cloudTilePosX, int cloudTilePosZ)
	{
		if (IsDummy)
		{
			return 0.0;
		}
		int dx = cloudTilePosX - cloudTilebasePosX;
		int dz = cloudTilePosZ - cloudTilebasePosZ;
		return NewWePattern.GetCloudDensityAt(dx, dz) * (double)Weight + OldWePattern.GetCloudDensityAt(dx, dz) * (double)(1f - Weight);
	}

	public double GetBlendedCloudOpaqueness()
	{
		return NewWePattern.State.nowbaseOpaqueness * Weight + OldWePattern.State.nowbaseOpaqueness * (1f - Weight);
	}

	public double GetBlendedCloudBrightness(float b)
	{
		float num = weatherData.Ambient.CloudBrightness.Weight;
		if (IsDummy)
		{
			num = 0f;
		}
		float num2 = weatherData.Ambient.CloudBrightness.Value * weatherData.Ambient.SceneBrightness.Value;
		return b * (1f - num) + num2 * num;
	}

	public double GetBlendedThinCloudModeness()
	{
		return NewWePattern.State.nowThinCloudModeness * Weight + OldWePattern.State.nowThinCloudModeness * (1f - Weight);
	}

	public double GetBlendedUndulatingCloudModeness()
	{
		return NewWePattern.State.nowUndulatingCloudModeness * Weight + OldWePattern.State.nowUndulatingCloudModeness * (1f - Weight);
	}

	internal void EnsureCloudTileCacheIsFresh(Vec3i tilePos)
	{
		if (!IsDummy)
		{
			NewWePattern.EnsureCloudTileCacheIsFresh(tilePos);
			OldWePattern.EnsureCloudTileCacheIsFresh(tilePos);
		}
	}

	public byte[] ToBytes()
	{
		using FastMemoryStream ms = new FastMemoryStream();
		return ToBytes(ms);
	}

	public byte[] ToBytes(FastMemoryStream ms)
	{
		return SerializerUtil.Serialize(new WeatherState
		{
			NewPattern = (NewWePattern?.State ?? null),
			OldPattern = (OldWePattern?.State ?? null),
			WindPattern = (CurWindPattern?.State ?? null),
			WeatherEvent = (CurWeatherEvent?.State ?? null),
			Weight = Weight,
			TransitionDelay = TransitionDelay,
			Transitioning = Transitioning,
			LastUpdateTotalHours = LastUpdateTotalHours,
			LcgCurrentSeed = Rand.currentSeed,
			LcgMapGenSeed = Rand.mapGenSeed,
			LcgWorldSeed = Rand.worldSeed,
			SnowAccumSnapshots = SnowAccumSnapshots.Values,
			Ringarraycursor = SnowAccumSnapshots.EndPosition
		}, ms);
	}

	internal void FromBytes(byte[] data)
	{
		if (data == null)
		{
			LoadRandomPattern();
			NewWePattern.OnBeginUse();
			return;
		}
		WeatherState weatherState = SerializerUtil.Deserialize<WeatherState>(data);
		if (weatherState.NewPattern != null)
		{
			NewWePattern = WeatherPatterns[GameMath.Clamp(weatherState.NewPattern.Index, 0, WeatherPatterns.Length - 1)];
			NewWePattern.State = weatherState.NewPattern;
		}
		else
		{
			NewWePattern = WeatherPatterns[0];
		}
		if (weatherState.OldPattern != null && weatherState.OldPattern.Index < WeatherPatterns.Length)
		{
			OldWePattern = WeatherPatterns[GameMath.Clamp(weatherState.OldPattern.Index, 0, WeatherPatterns.Length - 1)];
			OldWePattern.State = weatherState.OldPattern;
		}
		else
		{
			OldWePattern = WeatherPatterns[0];
		}
		if (weatherState.WindPattern != null)
		{
			CurWindPattern = WindPatterns[GameMath.Clamp(weatherState.WindPattern.Index, 0, WindPatterns.Length - 1)];
			CurWindPattern.State = weatherState.WindPattern;
		}
		Weight = weatherState.Weight;
		TransitionDelay = weatherState.TransitionDelay;
		Transitioning = weatherState.Transitioning;
		LastUpdateTotalHours = weatherState.LastUpdateTotalHours;
		Rand.worldSeed = weatherState.LcgWorldSeed;
		Rand.currentSeed = weatherState.LcgCurrentSeed;
		Rand.mapGenSeed = weatherState.LcgMapGenSeed;
		double totalHours = ws.api.World.Calendar.TotalHours;
		LastUpdateTotalHours = Math.Max(LastUpdateTotalHours, totalHours - (double)(ws.api.World.Calendar.DaysPerYear * 24) + ws.api.World.Rand.NextDouble());
		int num = (int)((float)ws.api.World.Calendar.DaysPerYear * ws.api.World.Calendar.HoursPerDay) + 1;
		SnowAccumSnapshots = new RingArray<SnowAccumSnapshot>(num, weatherState.SnowAccumSnapshots);
		SnowAccumSnapshots.EndPosition = GameMath.Clamp(weatherState.Ringarraycursor, 0, num - 1);
		if (weatherState.WeatherEvent != null)
		{
			CurWeatherEvent = WeatherEvents[weatherState.WeatherEvent.Index];
			CurWeatherEvent.State = weatherState.WeatherEvent;
		}
		if (CurWeatherEvent == null)
		{
			CurWeatherEvent = RandomWeatherEvent();
			CurWeatherEvent.OnBeginUse();
		}
	}
}
