using System;

namespace Vintagestory.ServerMods;

internal class MapLayerOre : MapLayerBase
{
	private NoiseOre map;

	private float zoomMul;

	private float contrastMul;

	private float sub;

	public MapLayerOre(long seed, NoiseOre map, float zoomMul, float contrastMul, float sub)
		: base(seed)
	{
		this.map = map;
		this.zoomMul = zoomMul;
		this.contrastMul = contrastMul;
		this.sub = sub;
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] array = new int[sizeX * sizeZ];
		float num = (float)TerraGenConfig.oreMapSubScale * zoomMul;
		int num2 = (int)Math.Ceiling((float)sizeX / num) + 1;
		int oreCacheSizeZ = (int)Math.Ceiling((float)sizeZ / num) + 1;
		int[] oreCache = getOreCache((int)((float)xCoord / num), (int)((float)zCoord / num), num2, oreCacheSizeZ);
		for (int i = 0; i < sizeX; i++)
		{
			for (int j = 0; j < sizeZ; j++)
			{
				array[j * sizeX + i] = map.GetLerpedOreValueAt((double)i / (double)num, (double)j / (double)num, oreCache, num2, contrastMul, sub);
			}
		}
		return array;
	}

	private int[] getOreCache(int coordX, int coordZ, int oreCacheSizeX, int oreCacheSizeZ)
	{
		int[] array = new int[oreCacheSizeX * oreCacheSizeZ];
		for (int i = 0; i < oreCacheSizeX; i++)
		{
			for (int j = 0; j < oreCacheSizeZ; j++)
			{
				array[j * oreCacheSizeX + i] = map.GetOreAt(coordX + i, coordZ + j);
			}
		}
		return array;
	}
}
