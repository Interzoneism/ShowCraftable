using System;

namespace Vintagestory.ServerMods;

internal class MapLayerDebugWind : MapLayerBase
{
	private NoiseWind windmap;

	public MapLayerDebugWind(long seed)
		: base(seed)
	{
		windmap = new NoiseWind(seed);
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] array = new int[sizeX * sizeZ];
		int num = 16;
		float num2 = 128f;
		for (int i = 0; i < sizeX + num; i += num)
		{
			for (int j = 0; j < sizeZ + num; j += num)
			{
				PolarVector windAt = windmap.getWindAt(((float)xCoord + (float)i) / num2, ((float)zCoord + (float)j) / num2);
				int num3 = (int)((double)windAt.length * Math.Cos(windAt.angle));
				int num4 = (int)((double)windAt.length * Math.Sin(windAt.angle));
				plotLine(array, sizeX, i, j, i + num3, j + num4);
				if (i < sizeX && j < sizeZ)
				{
					array[j * sizeX + i] = 16711680;
				}
			}
		}
		return array;
	}

	private void plotLine(int[] map, int sizeX, int x0, int y0, int x1, int y1)
	{
		int num = Math.Abs(x1 - x0);
		int num2 = ((x0 < x1) ? 1 : (-1));
		int num3 = -Math.Abs(y1 - y0);
		int num4 = ((y0 < y1) ? 1 : (-1));
		int num5 = num + num3;
		while (true)
		{
			if (x0 >= 0 && x0 < sizeX && y0 >= 0 && y0 < sizeX)
			{
				map[y0 * sizeX + x0] = 7895160;
			}
			if (x0 != x1 || y0 != y1)
			{
				int num6 = 2 * num5;
				if (num6 >= num3)
				{
					num5 += num3;
					x0 += num2;
				}
				if (num6 <= num)
				{
					num5 += num;
					y0 += num4;
				}
				continue;
			}
			break;
		}
	}
}
