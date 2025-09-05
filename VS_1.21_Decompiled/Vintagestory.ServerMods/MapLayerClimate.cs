using System;

namespace Vintagestory.ServerMods;

internal class MapLayerClimate : MapLayerBase
{
	public NoiseClimate noiseMap;

	public MapLayerClimate(long seed, NoiseClimate map)
		: base(seed)
	{
		noiseMap = map;
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] array = new int[sizeX * sizeZ];
		int num = (int)Math.Ceiling((float)sizeX / (float)TerraGenConfig.climateMapSubScale) + 1;
		int climateCacheSizeZ = (int)Math.Ceiling((float)sizeZ / (float)TerraGenConfig.climateMapSubScale) + 1;
		int[] climateCache = getClimateCache((int)Math.Floor((float)xCoord / (float)TerraGenConfig.climateMapSubScale), (int)Math.Floor((float)zCoord / (float)TerraGenConfig.climateMapSubScale), num, climateCacheSizeZ);
		for (int i = 0; i < sizeX; i++)
		{
			for (int j = 0; j < sizeZ; j++)
			{
				array[j * sizeX + i] = noiseMap.GetLerpedClimateAt((double)i / (double)TerraGenConfig.climateMapSubScale, (double)j / (double)TerraGenConfig.climateMapSubScale, climateCache, num);
			}
		}
		return array;
	}

	private int[] getClimateCache(int coordX, int coordZ, int climateCacheSizeX, int climateCacheSizeZ)
	{
		int[] array = new int[climateCacheSizeX * climateCacheSizeZ];
		for (int i = 0; i < climateCacheSizeX; i++)
		{
			for (int j = 0; j < climateCacheSizeZ; j++)
			{
				array[j * climateCacheSizeX + i] = noiseMap.GetClimateAt(coordX + i, coordZ + j);
			}
		}
		return array;
	}
}
