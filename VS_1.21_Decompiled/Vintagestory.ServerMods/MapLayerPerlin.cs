using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class MapLayerPerlin : MapLayerBase
{
	private NormalizedSimplexNoise noisegen;

	private float multiplier;

	private double[] thresholds;

	public MapLayerPerlin(long seed, int octaves, float persistence, int scale, int multiplier)
		: base(seed)
	{
		noisegen = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / (float)scale, persistence, seed + 12321);
		this.multiplier = multiplier;
	}

	public MapLayerPerlin(long seed, int octaves, float persistence, int scale, int multiplier, double[] thresholds)
		: base(seed)
	{
		noisegen = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / (float)scale, persistence, seed + 12321);
		this.multiplier = multiplier;
		this.thresholds = thresholds;
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] array = new int[sizeX * sizeZ];
		if (thresholds != null)
		{
			for (int i = 0; i < sizeZ; i++)
			{
				for (int j = 0; j < sizeX; j++)
				{
					array[i * sizeX + j] = (int)GameMath.Clamp((double)multiplier * noisegen.Noise(xCoord + j, zCoord + i, thresholds), 0.0, 255.0);
				}
			}
		}
		else
		{
			for (int k = 0; k < sizeZ; k++)
			{
				for (int l = 0; l < sizeX; l++)
				{
					array[k * sizeX + l] = (int)GameMath.Clamp((double)multiplier * noisegen.Noise(xCoord + l, zCoord + k), 0.0, 255.0);
				}
			}
		}
		return array;
	}

	public int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ, double[] thresholds)
	{
		int[] array = new int[sizeX * sizeZ];
		for (int i = 0; i < sizeZ; i++)
		{
			for (int j = 0; j < sizeX; j++)
			{
				array[i * sizeX + j] = (int)GameMath.Clamp((double)multiplier * noisegen.Noise(xCoord + j, zCoord + i, thresholds), 0.0, 255.0);
			}
		}
		return array;
	}
}
