using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class MapLayerLandforms : MapLayerBase
{
	private NoiseLandforms noiseLandforms;

	private NoiseClimate climateNoise;

	private NormalizedSimplexNoise noisegenX;

	private NormalizedSimplexNoise noisegenY;

	private float wobbleIntensity;

	public float landFormHorizontalScale = 1f;

	public MapLayerLandforms(long seed, NoiseClimate climateNoise, ICoreServerAPI api, float landformScale)
		: base(seed)
	{
		this.climateNoise = climateNoise;
		float num = (float)TerraGenConfig.landformMapScale * landformScale;
		num *= Math.Max(1f, (float)api.WorldManager.MapSizeY / 256f);
		noiseLandforms = new NoiseLandforms(seed, api, num);
		int quantityOctaves = 2;
		float num2 = 2f * (float)TerraGenConfig.landformMapScale;
		float num3 = 0.9f;
		wobbleIntensity = (float)TerraGenConfig.landformMapScale * 1.5f;
		noisegenX = NormalizedSimplexNoise.FromDefaultOctaves(quantityOctaves, 1f / num2, num3, seed + 2);
		noisegenY = NormalizedSimplexNoise.FromDefaultOctaves(quantityOctaves, 1f / num2, num3, seed + 1231296);
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] array = new int[sizeX * sizeZ];
		for (int i = 0; i < sizeX; i++)
		{
			for (int j = 0; j < sizeZ; j++)
			{
				int num = (int)((double)wobbleIntensity * noisegenX.Noise(xCoord + i, zCoord + j) * 1.2000000476837158);
				int num2 = (int)((double)wobbleIntensity * noisegenY.Noise(xCoord + i, zCoord + j) * 1.2000000476837158);
				int num3 = xCoord + i + num;
				int num4 = zCoord + j + num2;
				int lerpedClimateAt = climateNoise.GetLerpedClimateAt(num3 / TerraGenConfig.climateMapScale, num4 / TerraGenConfig.climateMapScale);
				int rain = (lerpedClimateAt >> 8) & 0xFF;
				int scaledAdjustedTemperature = Climate.GetScaledAdjustedTemperature((lerpedClimateAt >> 16) & 0xFF, 0);
				array[j * sizeX + i] = noiseLandforms.GetLandformIndexAt(num3, num4, scaledAdjustedTemperature, rain);
			}
		}
		return array;
	}
}
