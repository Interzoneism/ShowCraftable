namespace Vintagestory.ServerMods;

internal class MapLayerLines : MapLayerBase
{
	public MapLayerLines(long seed)
		: base(seed)
	{
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] array = new int[sizeX * sizeX];
		for (int i = 0; i < sizeX; i++)
		{
			for (int j = 0; j < sizeZ; j++)
			{
				array[j * sizeX + i] = ((j % 20 <= 2) ? 255 : 0);
			}
		}
		return array;
	}
}
