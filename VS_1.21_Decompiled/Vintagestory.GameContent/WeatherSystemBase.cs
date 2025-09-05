using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class WeatherSystemBase : ModSystem
{
	public ICoreAPI api;

	public WeatherSystemConfig GeneralConfig;

	public WeatherPatternConfig[] WeatherConfigs;

	public WindPatternConfig[] WindConfigs;

	public WeatherEventConfig[] WeatherEventConfigs;

	public bool autoChangePatterns = true;

	public Dictionary<long, WeatherSimulationRegion> weatherSimByMapRegion = new Dictionary<long, WeatherSimulationRegion>();

	protected SimplexNoise precipitationNoise;

	protected SimplexNoise precipitationNoiseSub;

	public WeatherSimulationRegion dummySim;

	public WeatherDataReader WeatherDataSlowAccess;

	public WeatherPattern rainOverlayPattern;

	public WeatherDataSnapshot rainOverlaySnap;

	public WeatherSimulationLightning simLightning;

	private object weatherSimByMapRegionLock = new object();

	public virtual float? OverridePrecipitation { get; set; }

	public virtual double RainCloudDaysOffset { get; set; }

	public virtual int CloudTileSize { get; set; } = 50;

	public virtual float CloudLevelRel { get; set; } = 1f;

	public event LightningImpactDelegate OnLightningImpactBegin;

	public event LightningImpactDelegate OnLightningImpactEnd;

	public override void Start(ICoreAPI api)
	{
		this.api = api;
		api.Network.RegisterChannel("weather").RegisterMessageType(typeof(WeatherState)).RegisterMessageType(typeof(WeatherConfigPacket))
			.RegisterMessageType(typeof(WeatherPatternAssetsPacket))
			.RegisterMessageType(typeof(LightningFlashPacket))
			.RegisterMessageType(typeof(WeatherCloudYposPacket));
		api.Event.OnGetWindSpeed += Event_OnGetWindSpeed;
	}

	private void Event_OnGetWindSpeed(Vec3d pos, ref Vec3d windSpeed)
	{
		windSpeed.X = WeatherDataSlowAccess.GetWindSpeed(pos);
	}

	public void Initialize()
	{
		precipitationNoise = SimplexNoise.FromDefaultOctaves(4, 1.0 / 150.0, 0.95, api.World.Seed - 18971121);
		precipitationNoiseSub = SimplexNoise.FromDefaultOctaves(3, 1.0 / 750.0, 0.95, api.World.Seed - 1717121);
		simLightning = new WeatherSimulationLightning(api, this);
	}

	public void InitDummySim()
	{
		dummySim = new WeatherSimulationRegion(this, 0, 0);
		dummySim.IsDummy = true;
		dummySim.Initialize();
		LCGRandom lCGRandom = new LCGRandom(api.World.Seed);
		lCGRandom.InitPositionSeed(3, 3);
		rainOverlayPattern = new WeatherPattern(this, GeneralConfig.RainOverlayPattern, lCGRandom, 0, 0);
		rainOverlayPattern.Initialize(0, api.World.Seed);
		rainOverlayPattern.OnBeginUse();
		rainOverlaySnap = new WeatherDataSnapshot();
	}

	public double GetEnvironmentWetness(BlockPos pos, double days, double hourResolution = 2.0)
	{
		double num = api.World.Calendar.TotalDays - days;
		double totalDays = api.World.Calendar.TotalDays;
		double num2 = 0.0;
		double num3 = num;
		double num4 = api.World.Calendar.HoursPerDay;
		double num5 = 1.0 / 84.0;
		for (; num3 < totalDays; num3 += hourResolution / num4)
		{
			num2 += num5 * (double)api.World.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.ForSuppliedDateValues, num3).Rainfall;
		}
		return GameMath.Clamp(num2, 0.0, 1.0);
	}

	public PrecipitationState GetPrecipitationState(Vec3d pos)
	{
		return GetPrecipitationState(pos, api.World.Calendar.TotalDays);
	}

	public PrecipitationState GetPrecipitationState(Vec3d pos, double totalDays)
	{
		float precipitation = GetPrecipitation(pos.X, pos.Y, pos.Z, totalDays);
		return new PrecipitationState
		{
			Level = Math.Max(0f, precipitation - 0.5f),
			ParticleSize = Math.Max(0f, precipitation - 0.5f),
			Type = ((precipitation > 0f) ? WeatherDataSlowAccess.GetPrecType(pos) : EnumPrecipitationType.Auto)
		};
	}

	public float GetPrecipitation(Vec3d pos)
	{
		return GetPrecipitation(pos.X, pos.Y, pos.Z, api.World.Calendar.TotalDays);
	}

	public float GetPrecipitation(double posX, double posY, double posZ)
	{
		return GetPrecipitation(posX, posY, posZ, api.World.Calendar.TotalDays);
	}

	public float GetPrecipitation(double posX, double posY, double posZ, double totalDays)
	{
		ClimateCondition climateAt = api.World.BlockAccessor.GetClimateAt(new BlockPos((int)posX, (int)posY, (int)posZ), EnumGetClimateMode.WorldGenValues, totalDays);
		return Math.Max(0f, GetRainCloudness(climateAt, posX, posZ, totalDays) - 0.5f);
	}

	public float GetPrecipitation(BlockPos pos, double totalDays, ClimateCondition conds)
	{
		return Math.Max(0f, GetRainCloudness(conds, (double)pos.X + 0.5, (double)pos.Z + 0.5, totalDays) - 0.5f);
	}

	protected void Event_OnGetClimate(ref ClimateCondition climate, BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0)
	{
		if (mode != EnumGetClimateMode.WorldGenValues && mode != EnumGetClimateMode.ForSuppliedDate_TemperatureOnly)
		{
			float rainCloudness = GetRainCloudness(climate, (double)pos.X + 0.5, (double)pos.Z + 0.5, totalDays);
			climate.Rainfall = GameMath.Clamp(rainCloudness - 0.5f, 0f, 1f);
			climate.RainCloudOverlay = GameMath.Clamp(rainCloudness, 0f, 1f);
		}
	}

	public float GetRainCloudness(ClimateCondition conds, double posX, double posZ, double totalDays)
	{
		if (OverridePrecipitation.HasValue)
		{
			return OverridePrecipitation.Value + 0.5f;
		}
		float wgenRain = 0f;
		if (conds != null)
		{
			wgenRain = GameMath.Clamp((conds.Rainfall - 0.6f) * 2f, -1f, 1f);
		}
		return getPrecipNoise(posX, posZ, totalDays + RainCloudDaysOffset, wgenRain);
	}

	public ClimateCondition GetClimateFast(BlockPos pos, int climate)
	{
		return api.World.BlockAccessor.GetClimateAt(pos, climate);
	}

	private float getPrecipNoise(double posX, double posZ, double totalDays, float wgenRain)
	{
		return (float)GameMath.Max(precipitationNoise.Noise(posX / 9.0 / 2.0 + totalDays * 18.0, posZ / 9.0 / 2.0, totalDays * 4.0) * 1.600000023841858 - GameMath.Clamp(precipitationNoiseSub.Noise(posX / 4.0 / 2.0 + totalDays * 24.0, posZ / 4.0 / 2.0, totalDays * 6.0) * 5.0 - 1.0 - (double)wgenRain, 0.0, 1.0) + (double)wgenRain, 0.0);
	}

	public WeatherDataReader getWeatherDataReader()
	{
		return new WeatherDataReader(api, this);
	}

	public WeatherDataReaderPreLoad getWeatherDataReaderPreLoad()
	{
		return new WeatherDataReaderPreLoad(api, this);
	}

	public WeatherSimulationRegion getOrCreateWeatherSimForRegion(int regionX, int regionZ)
	{
		long index2d = MapRegionIndex2D(regionX, regionZ);
		IMapRegion mapRegion = api.World.BlockAccessor.GetMapRegion(regionX, regionZ);
		if (mapRegion == null)
		{
			return null;
		}
		return getOrCreateWeatherSimForRegion(index2d, mapRegion);
	}

	public WeatherSimulationRegion getOrCreateWeatherSimForRegion(long index2d, IMapRegion mapregion)
	{
		Vec3i vec3i = MapRegionPosFromIndex2D(index2d);
		WeatherSimulationRegion value;
		lock (weatherSimByMapRegionLock)
		{
			if (weatherSimByMapRegion.TryGetValue(index2d, out value))
			{
				return value;
			}
		}
		value = new WeatherSimulationRegion(this, vec3i.X, vec3i.Z);
		value.Initialize();
		mapregion.RemoveModdata("weather");
		byte[] moddata = mapregion.GetModdata("weatherState");
		if (moddata != null)
		{
			try
			{
				value.FromBytes(moddata);
			}
			catch (Exception)
			{
				value.LoadRandomPattern();
				value.NewWePattern.OnBeginUse();
			}
		}
		else
		{
			value.LoadRandomPattern();
			value.NewWePattern.OnBeginUse();
			mapregion.SetModdata("weatherState", value.ToBytes());
		}
		value.MapRegion = mapregion;
		lock (weatherSimByMapRegionLock)
		{
			weatherSimByMapRegion[index2d] = value;
			return value;
		}
	}

	public long MapRegionIndex2D(int regionX, int regionZ)
	{
		return ((long)regionZ << 32) + regionX;
	}

	public Vec3i MapRegionPosFromIndex2D(long index)
	{
		return new Vec3i((int)index, 0, (int)(index >> 32));
	}

	public virtual void SpawnLightningFlash(Vec3d pos)
	{
	}

	internal void TriggerOnLightningImpactStart(ref Vec3d impactPos, out EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		if (this.OnLightningImpactBegin != null)
		{
			TriggerOnLightningImpactAny(ref impactPos, out handling, this.OnLightningImpactBegin.GetInvocationList());
		}
	}

	internal void TriggerOnLightningImpactEnd(Vec3d impactPos, out EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		if (this.OnLightningImpactEnd != null)
		{
			TriggerOnLightningImpactAny(ref impactPos, out handling, this.OnLightningImpactEnd.GetInvocationList());
		}
	}

	internal void TriggerOnLightningImpactAny(ref Vec3d pos, out EnumHandling handling, Delegate[] delegates)
	{
		handling = EnumHandling.PassThrough;
		for (int i = 0; i < delegates.Length; i++)
		{
			LightningImpactDelegate obj = (LightningImpactDelegate)delegates[i];
			EnumHandling handling2 = EnumHandling.PassThrough;
			obj(ref pos, ref handling2);
			switch (handling2)
			{
			case EnumHandling.PreventSubsequent:
				handling = EnumHandling.PreventSubsequent;
				return;
			case EnumHandling.PreventDefault:
				handling = EnumHandling.PreventDefault;
				break;
			}
		}
	}
}
