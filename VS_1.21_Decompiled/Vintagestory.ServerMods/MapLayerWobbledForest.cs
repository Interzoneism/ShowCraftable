using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class MapLayerWobbledForest : MapLayerBase
{
	private NormalizedSimplexNoise noisegen;

	private float multiplier;

	private int offset;

	private NormalizedSimplexNoise noisegenX;

	private NormalizedSimplexNoise noisegenY;

	public float wobbleIntensity;

	public MapLayerWobbledForest(long seed, int octaves, float persistence, float scale, float multiplier = 255f, int offset = 0)
		: base(seed)
	{
		double[] array = new double[3];
		double[] array2 = new double[3];
		for (int i = 0; i < octaves; i++)
		{
			array[i] = Math.Pow(3.0, i) * 1.0 / (double)scale;
			array2[i] = Math.Pow(persistence, i);
		}
		noisegen = new NormalizedSimplexNoise(array2, array, seed);
		this.offset = offset;
		this.multiplier = multiplier;
		int quantityOctaves = 3;
		float num = 128f;
		float num2 = 0.9f;
		wobbleIntensity = scale / 3f;
		noisegenX = NormalizedSimplexNoise.FromDefaultOctaves(quantityOctaves, 1f / num, num2, seed + 2);
		noisegenY = NormalizedSimplexNoise.FromDefaultOctaves(quantityOctaves, 1f / num, num2, seed + 1231296);
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] array = new int[sizeX * sizeZ];
		for (int i = 0; i < sizeZ; i++)
		{
			for (int j = 0; j < sizeX; j++)
			{
				int num = (int)((double)wobbleIntensity * noisegenX.Noise(xCoord + j, zCoord + i));
				int num2 = (int)((double)wobbleIntensity * noisegenY.Noise(xCoord + j, zCoord + i));
				int num3 = xCoord + j + num;
				int num4 = zCoord + i + num2;
				double num5 = (double)offset + (double)multiplier * noisegen.Noise(num3, num4);
				int unpaddedInt = inputMap.GetUnpaddedInt(j * inputMap.InnerSize / outputMap.InnerSize, i * inputMap.InnerSize / outputMap.InnerSize);
				float num6 = (unpaddedInt >> 8) & 0xFF;
				float num7 = (unpaddedInt >> 16) & 0xFF;
				array[i * sizeX + j] = (int)GameMath.Clamp(num5 - (double)GameMath.Clamp(128f - num6 * num7 / 65025f, 0f, 128f), 0.0, 255.0);
			}
		}
		return array;
	}
}
