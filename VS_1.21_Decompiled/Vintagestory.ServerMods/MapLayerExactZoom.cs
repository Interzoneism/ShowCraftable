namespace Vintagestory.ServerMods;

internal class MapLayerExactZoom : MapLayerBase
{
	private MapLayerBase parent;

	private int zoomLevel;

	public MapLayerExactZoom(MapLayerBase parent, int zoomLevel)
		: base(0L)
	{
		this.parent = parent;
		this.zoomLevel = zoomLevel;
	}

	public override int[] GenLayer(int xCoord, int yCoord, int sizeX, int sizeY)
	{
		sizeX += zoomLevel;
		sizeY += zoomLevel;
		int[] array = new int[sizeX * sizeY];
		int xCoord2 = xCoord / zoomLevel - 1;
		int zCoord = yCoord / zoomLevel - 1;
		int num = sizeX / zoomLevel;
		int sizeZ = sizeY / zoomLevel;
		int[] array2 = parent.GenLayer(xCoord2, zCoord, num, sizeZ);
		for (int i = 0; i < array2.Length; i++)
		{
			int num2 = i % num;
			int num3 = i / num;
			int num4 = array2[i];
			int num5 = zoomLevel * num2 + zoomLevel * num3 * sizeX;
			for (int j = 0; j < zoomLevel * zoomLevel; j++)
			{
				array[num5 + sizeX * (j / zoomLevel) + j % zoomLevel] = num4;
			}
		}
		return CutRightAndBottom(array, sizeX, sizeY, zoomLevel);
	}
}
