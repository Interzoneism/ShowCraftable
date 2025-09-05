using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class MapLayerPerlinWobble : MapLayerTransformBase
{
	protected NormalizedSimplexNoise noisegenX;

	protected NormalizedSimplexNoise noisegenY;

	protected float scale;

	protected float intensity;

	public MapLayerPerlinWobble(long seed, MapLayerBase parent, int octaves, float persistence, float scale, float intensity = 1f)
		: base(seed, parent)
	{
		noisegenX = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scale, persistence, seed);
		noisegenY = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scale, persistence, seed + 1231296);
		this.scale = scale;
		this.intensity = intensity;
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int num = (int)Math.Ceiling(intensity);
		int num2 = sizeX + 2 * num;
		int num3 = sizeZ + 2 * num;
		int[] array = parent.GenLayer(xCoord - num, zCoord - num, num2, num3);
		int[] array2 = new int[sizeX * sizeZ];
		for (int i = 0; i < sizeZ; i++)
		{
			for (int j = 0; j < sizeX; j++)
			{
				int num4 = (int)((double)intensity * noisegenX.Noise(xCoord + j + num, zCoord + i + num));
				int num5 = (int)((double)intensity * noisegenY.Noise(xCoord + j + num, zCoord + i + num));
				int num6 = GameMath.Mod(j + num4 + num / 2, num2);
				int num7 = GameMath.Mod(i + num5 + num / 2, num3);
				array2[i * sizeX + j] = array[num7 * num2 + num6];
			}
		}
		return array2;
	}
}
