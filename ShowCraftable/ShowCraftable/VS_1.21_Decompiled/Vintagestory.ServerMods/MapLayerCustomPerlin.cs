using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class MapLayerCustomPerlin : MapLayerBase
{
	private SimplexNoise noisegen;

	private double[] thresholds;

	public int clampMin;

	public int clampMax = 255;

	public MapLayerCustomPerlin(long seed, double[] amplitudes, double[] frequencies, double[] thresholds)
		: base(seed)
	{
		noisegen = new SimplexNoise(amplitudes, frequencies, seed + 12321);
		this.thresholds = thresholds;
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] array = new int[sizeX * sizeZ];
		for (int i = 0; i < sizeZ; i++)
		{
			for (int j = 0; j < sizeX; j++)
			{
				array[i * sizeX + j] = (int)GameMath.Clamp(noisegen.Noise(xCoord + j, zCoord + i, thresholds), clampMin, clampMax);
			}
		}
		return array;
	}
}
