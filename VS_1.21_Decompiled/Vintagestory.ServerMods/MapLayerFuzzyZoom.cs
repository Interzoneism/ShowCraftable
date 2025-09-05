using System;

namespace Vintagestory.ServerMods;

internal class MapLayerFuzzyZoom : MapLayerBase
{
	private MapLayerBase parent;

	public MapLayerFuzzyZoom(long currentSeed, MapLayerBase parent)
		: base(currentSeed)
	{
		this.parent = parent;
	}

	public override int[] GenLayer(int xPos, int zPos, int xSize, int zSize)
	{
		int num = xPos >> 1;
		int num2 = zPos >> 1;
		int num3 = (xSize >> 1) + 2;
		int num4 = (zSize >> 1) + 2;
		int[] array = parent.GenLayer(num, num2, num3, num4);
		int num5 = num3 - 1 << 1;
		int num6 = num4 - 1 << 1;
		int[] array2 = new int[num5 * num6];
		for (int i = 0; i < num4 - 1; i++)
		{
			int num7 = (i << 1) * num5;
			for (int j = 0; j < num3 - 1; j++)
			{
				InitPositionSeed(num + j, num2 + i);
				int num8 = array[j + i * num3];
				int num9 = array[j + 1 + i * num3];
				int num10 = array[j + (i + 1) * num3];
				int num11 = array[j + 1 + (i + 1) * num3];
				array2[num7] = num8;
				array2[num7 + num5] = selectRandom(num8, num10);
				array2[num7 + 1] = selectRandom(num8, num9);
				array2[num7 + 1 + num5] = selectRandom(num8, num9, num10, num11);
				num7 += 2;
			}
		}
		int[] array3 = new int[xSize * zSize];
		for (int k = 0; k < zSize; k++)
		{
			int sourceIndex = (k + (zPos & 1)) * num5 + (xPos & 1);
			Array.Copy(array2, sourceIndex, array3, k * xSize, xSize);
		}
		return array3;
	}

	protected int selectRandom(params int[] numbers)
	{
		return numbers[NextInt(numbers.Length)];
	}

	public static MapLayerBase magnify(long seed, MapLayerBase parent, int zoomLevels)
	{
		MapLayerBase result = parent;
		for (int i = 0; i < zoomLevels; i++)
		{
			result = new MapLayerFuzzyZoom(seed + i, result);
		}
		return result;
	}
}
