using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherSimulationSound
{
	private WeatherSystemClient weatherSys;

	private ILoadedSound[] rainSoundsLeafless;

	private ILoadedSound[] rainSoundsLeafy;

	private ILoadedSound lowTrembleSound;

	private ILoadedSound hailSound;

	private ILoadedSound windSoundLeafy;

	private ILoadedSound windSoundLeafless;

	private ICoreClientAPI capi;

	private bool windSoundsOn;

	private bool rainSoundsOn;

	private bool hailSoundsOn;

	private float curWindVolumeLeafy;

	private float curWindVolumeLeafless;

	private float curRainVolumeLeafy;

	private float curRainVolumeLeafless;

	private float curRainPitch = 1f;

	private float curHailVolume;

	private float curHailPitch = 1f;

	private float curTrembleVolume;

	private float curTremblePitch;

	private float quarterSecAccum;

	private bool searchComplete = true;

	public static float roomVolumePitchLoss;

	private bool soundsReady;

	private BlockPos plrPos = new BlockPos();

	public WeatherSimulationSound(ICoreClientAPI capi, WeatherSystemClient weatherSys)
	{
		this.weatherSys = weatherSys;
		this.capi = capi;
	}

	internal void Initialize()
	{
		lowTrembleSound = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/weather/tracks/verylowtremble.ogg"),
			ShouldLoop = true,
			DisposeOnFinish = false,
			Position = new Vec3f(0f, 0f, 0f),
			RelativePosition = true,
			Range = 16f,
			SoundType = EnumSoundType.Weather,
			Volume = 1f
		});
		hailSound = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/weather/tracks/hail.ogg"),
			ShouldLoop = true,
			DisposeOnFinish = false,
			Position = new Vec3f(0f, 0f, 0f),
			RelativePosition = true,
			Range = 16f,
			SoundType = EnumSoundType.Weather,
			Volume = 1f
		});
		rainSoundsLeafless = new ILoadedSound[1];
		rainSoundsLeafless[0] = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/weather/tracks/rain-leafless.ogg"),
			ShouldLoop = true,
			DisposeOnFinish = false,
			Position = new Vec3f(0f, 0f, 0f),
			RelativePosition = true,
			Range = 16f,
			SoundType = EnumSoundType.Weather,
			Volume = 1f
		});
		rainSoundsLeafy = new ILoadedSound[1];
		rainSoundsLeafy[0] = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/weather/tracks/rain-leafy.ogg"),
			ShouldLoop = true,
			DisposeOnFinish = false,
			Position = new Vec3f(0f, 0f, 0f),
			RelativePosition = true,
			Range = 16f,
			SoundType = EnumSoundType.Weather,
			Volume = 1f
		});
		windSoundLeafy = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/weather/wind-leafy.ogg"),
			ShouldLoop = true,
			DisposeOnFinish = false,
			Position = new Vec3f(0f, 0f, 0f),
			RelativePosition = true,
			Range = 16f,
			SoundType = EnumSoundType.Weather,
			Volume = 1f
		});
		windSoundLeafless = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/weather/wind-leafless.ogg"),
			ShouldLoop = true,
			DisposeOnFinish = false,
			Position = new Vec3f(0f, 0f, 0f),
			RelativePosition = true,
			Range = 16f,
			SoundType = EnumSoundType.Weather,
			Volume = 1f
		});
	}

	public void Update(float dt)
	{
		if (lowTrembleSound != null)
		{
			dt = Math.Min(0.5f, dt);
			quarterSecAccum += dt;
			if (quarterSecAccum > 0.25f)
			{
				updateSounds(dt);
			}
		}
	}

	private void updateSounds(float dt)
	{
		if (!soundsReady)
		{
			if (!lowTrembleSound.IsReady || !hailSound.IsReady || !rainSoundsLeafless.All((ILoadedSound s) => s.IsReady) || !rainSoundsLeafy.All((ILoadedSound s) => s.IsReady) || !windSoundLeafy.IsReady || !windSoundLeafless.IsReady)
			{
				return;
			}
			soundsReady = true;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 1f;
		float num7 = 1f;
		WeatherDataSnapshot blendedWeatherData = weatherSys.BlendedWeatherData;
		if (searchComplete)
		{
			EntityPlayer entity = capi.World.Player.Entity;
			plrPos.Set((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z);
			searchComplete = false;
			TyronThreadPool.QueueTask(delegate
			{
				int distanceToRainFall = capi.World.BlockAccessor.GetDistanceToRainFall(plrPos, 12, 4);
				roomVolumePitchLoss = GameMath.Clamp((float)Math.Pow(Math.Max(0f, (float)(distanceToRainFall - 2) / 10f), 2.0), 0f, 1f);
				searchComplete = true;
			}, "weathersimulationsound");
		}
		EnumPrecipitationType enumPrecipitationType = blendedWeatherData.BlendedPrecType;
		if (enumPrecipitationType == EnumPrecipitationType.Auto)
		{
			enumPrecipitationType = ((weatherSys.clientClimateCond?.Temperature < blendedWeatherData.snowThresholdTemp) ? EnumPrecipitationType.Snow : EnumPrecipitationType.Rain);
		}
		float num8 = GameMath.Clamp(GlobalConstants.CurrentNearbyRelLeavesCountClient * 60f, 0f, 1f);
		ClimateCondition clientClimateCond = weatherSys.clientClimateCond;
		if (clientClimateCond.Rainfall > 0f)
		{
			if (enumPrecipitationType == EnumPrecipitationType.Rain || weatherSys.clientClimateCond.Temperature < blendedWeatherData.snowThresholdTemp)
			{
				num = num8 * GameMath.Clamp(clientClimateCond.Rainfall * 2f - Math.Max(0f, 2f * (blendedWeatherData.snowThresholdTemp - weatherSys.clientClimateCond.Temperature)), 0f, 1f);
				num = GameMath.Max(0f, num - roomVolumePitchLoss);
				num2 = Math.Max(0.3f, 1f - num8) * GameMath.Clamp(clientClimateCond.Rainfall * 2f - Math.Max(0f, 2f * (blendedWeatherData.snowThresholdTemp - weatherSys.clientClimateCond.Temperature)), 0f, 1f);
				num2 = GameMath.Max(0f, num2 - roomVolumePitchLoss);
				num6 = Math.Max(0.7f, 1.25f - clientClimateCond.Rainfall * 0.7f);
				num6 = Math.Max(0f, num6 - roomVolumePitchLoss / 4f);
				num4 = GameMath.Clamp(clientClimateCond.Rainfall * 1.6f - 0.8f - roomVolumePitchLoss * 0.25f, 0f, 1f);
				num5 = GameMath.Clamp(1f - roomVolumePitchLoss * 0.65f, 0f, 1f);
				if (!rainSoundsOn && ((double)num > 0.01 || (double)num2 > 0.01))
				{
					for (int num9 = 0; num9 < rainSoundsLeafless.Length; num9++)
					{
						rainSoundsLeafless[num9]?.Start();
					}
					for (int num10 = 0; num10 < rainSoundsLeafy.Length; num10++)
					{
						rainSoundsLeafy[num10]?.Start();
					}
					lowTrembleSound?.Start();
					rainSoundsOn = true;
					curRainPitch = num6;
				}
				if (capi.World.Player.Entity.IsEyesSubmerged())
				{
					curRainPitch = num6 / 2f;
					num *= 0.75f;
					num2 *= 0.75f;
				}
			}
			if (enumPrecipitationType == EnumPrecipitationType.Hail)
			{
				num3 = GameMath.Clamp(clientClimateCond.Rainfall * 2f - roomVolumePitchLoss, 0f, 1f);
				num3 = GameMath.Max(0f, num3 - roomVolumePitchLoss);
				num7 = Math.Max(0.7f, 1.25f - clientClimateCond.Rainfall * 0.7f);
				num7 = Math.Max(0f, num7 - roomVolumePitchLoss / 4f);
				if (!hailSoundsOn && (double)num3 > 0.01)
				{
					hailSound?.Start();
					hailSoundsOn = true;
					curHailPitch = num7;
				}
			}
		}
		curRainVolumeLeafy += (num - curRainVolumeLeafy) * dt / 2f;
		curRainVolumeLeafless += (num2 - curRainVolumeLeafless) * dt / 2f;
		curTrembleVolume += (num4 - curTrembleVolume) * dt;
		curHailVolume += (num3 - curHailVolume) * dt;
		curHailPitch += (num7 - curHailPitch) * dt;
		curRainPitch += (num6 - curRainPitch) * dt;
		curTremblePitch += (num5 - curTremblePitch) * dt;
		if (rainSoundsOn)
		{
			for (int num11 = 0; num11 < rainSoundsLeafless.Length; num11++)
			{
				rainSoundsLeafless[num11]?.SetVolume(curRainVolumeLeafless);
				rainSoundsLeafless[num11]?.SetPitch(curRainPitch);
			}
			for (int num12 = 0; num12 < rainSoundsLeafy.Length; num12++)
			{
				rainSoundsLeafy[num12]?.SetVolume(curRainVolumeLeafy);
				rainSoundsLeafy[num12]?.SetPitch(curRainPitch);
			}
		}
		lowTrembleSound?.SetVolume(curTrembleVolume);
		lowTrembleSound?.SetPitch(curTremblePitch);
		if (hailSoundsOn)
		{
			hailSound?.SetVolume(curHailVolume);
			hailSound?.SetPitch(curHailPitch);
		}
		if ((double)curRainVolumeLeafless < 0.01 && (double)curRainVolumeLeafy < 0.01)
		{
			for (int num13 = 0; num13 < rainSoundsLeafless.Length; num13++)
			{
				rainSoundsLeafless[num13]?.Stop();
			}
			for (int num14 = 0; num14 < rainSoundsLeafy.Length; num14++)
			{
				rainSoundsLeafy[num14]?.Stop();
			}
			rainSoundsOn = false;
		}
		if ((double)curHailVolume < 0.01)
		{
			hailSound?.Stop();
			hailSoundsOn = false;
		}
		float num15 = (1f - roomVolumePitchLoss) * blendedWeatherData.curWindSpeed.X - 0.3f;
		if (num15 > 0.03f || curWindVolumeLeafy > 0.01f || curWindVolumeLeafless > 0.01f)
		{
			if (!windSoundsOn)
			{
				windSoundLeafy?.Start();
				windSoundLeafless?.Start();
				windSoundsOn = true;
			}
			float num16 = num8 * 1.2f * num15;
			float num17 = (1f - num8) * 1.2f * num15;
			curWindVolumeLeafy += (num16 - curWindVolumeLeafy) * dt;
			curWindVolumeLeafless += (num17 - curWindVolumeLeafless) * dt;
			windSoundLeafy?.SetVolume(Math.Max(0f, curWindVolumeLeafy));
			windSoundLeafless?.SetVolume(Math.Max(0f, curWindVolumeLeafless));
		}
		else if (windSoundsOn)
		{
			windSoundLeafy?.Stop();
			windSoundLeafless?.Stop();
			windSoundsOn = false;
		}
	}

	public void Dispose()
	{
		if (rainSoundsLeafless != null)
		{
			ILoadedSound[] array = rainSoundsLeafless;
			for (int i = 0; i < array.Length; i++)
			{
				array[i]?.Dispose();
			}
		}
		if (rainSoundsLeafy != null)
		{
			ILoadedSound[] array = rainSoundsLeafy;
			for (int i = 0; i < array.Length; i++)
			{
				array[i]?.Dispose();
			}
		}
		hailSound?.Dispose();
		lowTrembleSound?.Dispose();
		windSoundLeafy?.Dispose();
		windSoundLeafless?.Dispose();
	}
}
