using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class MapLayerDoublePerlin : MapLayerBase
{
	private NormalizedSimplexNoise noisegenX;

	private NormalizedSimplexNoise noisegenY;

	private float multiplier;

	public MapLayerDoublePerlin(long seed, int octaves, float persistence, int scale, int multiplier)
		: base(seed)
	{
		noisegenX = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / (float)scale, persistence, seed);
		noisegenY = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / (float)scale, persistence, seed + 1232);
		this.multiplier = multiplier;
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] array = new int[sizeX * sizeZ];
		for (int i = 0; i < sizeZ; i++)
		{
			for (int j = 0; j < sizeX; j++)
			{
				array[i * sizeX + j] = (int)GameMath.Clamp((double)multiplier * noisegenX.Noise(xCoord + j, zCoord + i), 0.0, 255.0) | ((int)GameMath.Clamp((double)multiplier * noisegenY.Noise(xCoord + j, zCoord + i), 0.0, 255.0) << 8);
			}
		}
		return array;
	}
}
