namespace Vintagestory.ServerMods;

internal class NoiseOcean : NoiseBase
{
	private float landcover;

	private float scale;

	public NoiseOcean(long seed, float scale, float landcover)
		: base(seed)
	{
		this.landcover = landcover;
		this.scale = scale;
	}

	public int GetOceanIndexAt(int unscaledXpos, int unscaledZpos)
	{
		int xPos = (int)((float)unscaledXpos / scale);
		int zPos = (int)((float)unscaledZpos / scale);
		InitPositionSeed(xPos, zPos);
		if ((double)NextInt(10000) / 10000.0 < (double)landcover)
		{
			return 0;
		}
		return 255;
	}
}
