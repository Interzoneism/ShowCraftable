using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class WeatherDataReaderBase
{
	public WeatherDataSnapshot BlendedWeatherData = new WeatherDataSnapshot();

	protected WeatherDataSnapshot blendedWeatherDataNoPrec = new WeatherDataSnapshot();

	protected WeatherDataSnapshot topBlendedWeatherData = new WeatherDataSnapshot();

	protected WeatherDataSnapshot botBlendedWeatherData = new WeatherDataSnapshot();

	public WeatherSimulationRegion[] AdjacentSims = new WeatherSimulationRegion[4];

	public double LerpLeftRight;

	public double LerpTopBot;

	private ICoreAPI api;

	private WeatherSystemBase ws;

	private WeatherPattern rainOverlayData;

	private WeatherDataSnapshot rainSnapData;

	public float lerpRainCloudOverlay;

	public float lerpRainOverlay;

	private BlockPos tmpPos = new BlockPos();

	private IMapRegion hereMapRegion;

	public WeatherDataReaderBase(ICoreAPI api, WeatherSystemBase ws)
	{
		this.api = api;
		this.ws = ws;
		BlendedWeatherData.Ambient = new AmbientModifier().EnsurePopulated();
		blendedWeatherDataNoPrec.Ambient = new AmbientModifier().EnsurePopulated();
		AdjacentSims[0] = ws.dummySim;
		AdjacentSims[1] = ws.dummySim;
		AdjacentSims[2] = ws.dummySim;
		AdjacentSims[3] = ws.dummySim;
	}

	public void LoadAdjacentSims(Vec3d pos)
	{
		int regionSize = api.World.BlockAccessor.RegionSize;
		int num = (int)pos.X / regionSize;
		int num2 = (int)pos.Z / regionSize;
		int num3 = (int)Math.Round(pos.X / (double)regionSize) - 1;
		int num4 = (int)Math.Round(pos.Z / (double)regionSize) - 1;
		int num5 = 0;
		for (int i = 0; i <= 1; i++)
		{
			for (int j = 0; j <= 1; j++)
			{
				int num6 = num3 + i;
				int num7 = num4 + j;
				WeatherSimulationRegion weatherSimulationRegion = ws.getOrCreateWeatherSimForRegion(num6, num7);
				if (weatherSimulationRegion == null)
				{
					weatherSimulationRegion = ws.dummySim;
				}
				AdjacentSims[num5++] = weatherSimulationRegion;
				if (num6 == num && num7 == num2)
				{
					hereMapRegion = weatherSimulationRegion.MapRegion;
				}
			}
		}
	}

	public void LoadAdjacentSimsAndLerpValues(Vec3d pos, bool useArgValues, float lerpRainCloudOverlay = 0f, float lerpRainOverlay = 0f, float dt = 1f)
	{
		LoadAdjacentSims(pos);
		LoadLerp(pos, useArgValues, lerpRainCloudOverlay, lerpRainOverlay, dt);
	}

	public void LoadLerp(Vec3d pos, bool useArgValues, float lerpRainCloudOverlay = 0f, float lerpRainOverlay = 0f, float dt = 1f)
	{
		int regionSize = api.World.BlockAccessor.RegionSize;
		double num = pos.X / (double)regionSize - (double)(int)Math.Round(pos.X / (double)regionSize);
		double num2 = pos.Z / (double)regionSize - (double)(int)Math.Round(pos.Z / (double)regionSize);
		LerpTopBot = GameMath.Smootherstep(num + 0.5);
		LerpLeftRight = GameMath.Smootherstep(num2 + 0.5);
		rainOverlayData = ws.rainOverlayPattern;
		rainSnapData = ws.rainOverlaySnap;
		if (hereMapRegion == null)
		{
			this.lerpRainCloudOverlay = 0f;
			this.lerpRainOverlay = 0f;
			return;
		}
		if (useArgValues)
		{
			this.lerpRainCloudOverlay = lerpRainCloudOverlay;
			this.lerpRainOverlay = lerpRainOverlay;
			return;
		}
		tmpPos.Set((int)pos.X, (int)pos.Y, (int)pos.Z);
		int innerSize = hereMapRegion.ClimateMap.InnerSize;
		int climate = 8421504;
		if (innerSize > 0)
		{
			double num3 = Math.Max(0.0, (pos.X / (double)regionSize - (double)((int)pos.X / regionSize)) * (double)innerSize);
			double num4 = Math.Max(0.0, (pos.Z / (double)regionSize - (double)((int)pos.Z / regionSize)) * (double)innerSize);
			climate = hereMapRegion.ClimateMap.GetUnpaddedColorLerped((float)num3, (float)num4);
		}
		ClimateCondition climateFast = ws.GetClimateFast(tmpPos, climate);
		float num5 = Math.Min(1f, dt * 10f);
		this.lerpRainCloudOverlay += (climateFast.RainCloudOverlay - this.lerpRainCloudOverlay) * num5;
		this.lerpRainOverlay += (climateFast.Rainfall - this.lerpRainOverlay) * num5;
	}

	protected void updateAdjacentAndBlendWeatherData()
	{
		AdjacentSims[0].UpdateWeatherData();
		AdjacentSims[1].UpdateWeatherData();
		AdjacentSims[2].UpdateWeatherData();
		AdjacentSims[3].UpdateWeatherData();
		topBlendedWeatherData.SetLerped(AdjacentSims[0].weatherData, AdjacentSims[1].weatherData, (float)LerpLeftRight);
		botBlendedWeatherData.SetLerped(AdjacentSims[2].weatherData, AdjacentSims[3].weatherData, (float)LerpLeftRight);
		blendedWeatherDataNoPrec.SetLerped(topBlendedWeatherData, botBlendedWeatherData, (float)LerpTopBot);
		blendedWeatherDataNoPrec.Ambient.CloudBrightness.Weight = 0f;
		BlendedWeatherData.SetLerpedPrec(blendedWeatherDataNoPrec, rainSnapData, lerpRainOverlay);
	}

	protected void ensureCloudTileCacheIsFresh(Vec3i tilePos)
	{
		AdjacentSims[0].EnsureCloudTileCacheIsFresh(tilePos);
		AdjacentSims[1].EnsureCloudTileCacheIsFresh(tilePos);
		AdjacentSims[2].EnsureCloudTileCacheIsFresh(tilePos);
		AdjacentSims[3].EnsureCloudTileCacheIsFresh(tilePos);
	}

	protected EnumPrecipitationType pgGetPrecType()
	{
		if (LerpTopBot <= 0.5)
		{
			if (!(LerpLeftRight <= 0.5))
			{
				return AdjacentSims[1].GetPrecipitationType();
			}
			return AdjacentSims[0].GetPrecipitationType();
		}
		if (!(LerpLeftRight <= 0.5))
		{
			return AdjacentSims[3].GetPrecipitationType();
		}
		return AdjacentSims[2].GetPrecipitationType();
	}

	protected double pgetWindSpeed(double posY)
	{
		return GameMath.BiLerp(AdjacentSims[0].GetWindSpeed(posY), AdjacentSims[1].GetWindSpeed(posY), AdjacentSims[2].GetWindSpeed(posY), AdjacentSims[3].GetWindSpeed(posY), LerpLeftRight, LerpTopBot);
	}

	protected double pgetBlendedCloudThicknessAt(int cloudTileX, int cloudTileZ)
	{
		double v = GameMath.BiLerp(AdjacentSims[0].GetBlendedCloudThicknessAt(cloudTileX, cloudTileZ), AdjacentSims[1].GetBlendedCloudThicknessAt(cloudTileX, cloudTileZ), AdjacentSims[2].GetBlendedCloudThicknessAt(cloudTileX, cloudTileZ), AdjacentSims[3].GetBlendedCloudThicknessAt(cloudTileX, cloudTileZ), LerpLeftRight, LerpTopBot);
		double v2 = rainOverlayData.State.nowbaseThickness;
		return GameMath.Lerp(v, v2, lerpRainCloudOverlay);
	}

	protected double pgetBlendedCloudOpaqueness()
	{
		double v = GameMath.BiLerp(AdjacentSims[0].GetBlendedCloudOpaqueness(), AdjacentSims[1].GetBlendedCloudOpaqueness(), AdjacentSims[2].GetBlendedCloudOpaqueness(), AdjacentSims[3].GetBlendedCloudOpaqueness(), LerpLeftRight, LerpTopBot);
		double v2 = rainOverlayData.State.nowbaseOpaqueness;
		return GameMath.Lerp(v, v2, lerpRainCloudOverlay);
	}

	protected double pgetBlendedCloudBrightness(float b)
	{
		double v = GameMath.BiLerp(AdjacentSims[0].GetBlendedCloudBrightness(b), AdjacentSims[1].GetBlendedCloudBrightness(b), AdjacentSims[2].GetBlendedCloudBrightness(b), AdjacentSims[3].GetBlendedCloudBrightness(b), LerpLeftRight, LerpTopBot);
		double v2 = rainOverlayData.State.nowCloudBrightness;
		return GameMath.Lerp(v, v2, lerpRainCloudOverlay);
	}

	protected double pgetBlendedThinCloudModeness()
	{
		return GameMath.BiLerp(AdjacentSims[0].GetBlendedThinCloudModeness(), AdjacentSims[1].GetBlendedThinCloudModeness(), AdjacentSims[2].GetBlendedThinCloudModeness(), AdjacentSims[3].GetBlendedThinCloudModeness(), LerpLeftRight, LerpTopBot);
	}

	protected double pgetBlendedUndulatingCloudModeness()
	{
		return GameMath.BiLerp(AdjacentSims[0].GetBlendedUndulatingCloudModeness(), AdjacentSims[1].GetBlendedUndulatingCloudModeness(), AdjacentSims[2].GetBlendedUndulatingCloudModeness(), AdjacentSims[3].GetBlendedUndulatingCloudModeness(), LerpLeftRight, LerpTopBot);
	}
}
