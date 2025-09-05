using System;

namespace Vintagestory.ServerMods;

internal class MapLayerBlur : MapLayerBase
{
	private int range;

	private MapLayerBase parent;

	public MapLayerBlur(long seed, MapLayerBase parent, int range)
		: base(seed)
	{
		if ((range & 1) == 0)
		{
			throw new InvalidOperationException("Range must be odd!");
		}
		this.parent = parent;
		this.range = range;
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] array = parent.GenLayer(xCoord, zCoord, sizeX, sizeZ);
		BoxBlurHorizontal(array, range, 0, 0, sizeX, sizeZ);
		BoxBlurVertical(array, range, 0, 0, sizeX, sizeZ);
		return array;
	}

	internal unsafe void BoxBlurHorizontal(int[] map, int range, int xStart, int yStart, int xEnd, int yEnd)
	{
		fixed (int* ptr = map)
		{
			uint* ptr2 = (uint*)ptr;
			int num = xEnd - xStart;
			int num2 = range / 2;
			int num3 = yStart * num;
			uint[] array = new uint[num];
			for (int i = yStart; i < yEnd; i++)
			{
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				int num7 = 0;
				int num8 = 0;
				for (int j = xStart - num2; j < xEnd; j++)
				{
					int num9 = j - num2 - 1;
					if (num9 >= xStart)
					{
						int num10 = (int)ptr2[num3 + num9];
						if (num10 != 0)
						{
							num5 -= (num10 >> 24) & 0xFF;
							num6 -= (num10 >> 16) & 0xFF;
							num7 -= (num10 >> 8) & 0xFF;
							num8 -= num10 & 0xFF;
						}
						num4--;
					}
					int num11 = j + num2;
					if (num11 < xEnd)
					{
						int num12 = (int)ptr2[num3 + num11];
						if (num12 != 0)
						{
							num5 += (num12 >> 24) & 0xFF;
							num6 += (num12 >> 16) & 0xFF;
							num7 += (num12 >> 8) & 0xFF;
							num8 += num12 & 0xFF;
						}
						num4++;
					}
					if (j >= xStart)
					{
						uint num13 = (uint)(((byte)(num5 / num4) << 24) | ((byte)(num6 / num4) << 16) | ((byte)(num7 / num4) << 8) | (byte)(num8 / num4));
						array[j] = num13;
					}
				}
				for (int k = xStart; k < xEnd; k++)
				{
					ptr2[num3 + k] = array[k];
				}
				num3 += num;
			}
		}
	}

	internal unsafe void BoxBlurVertical(int[] map, int range, int xStart, int yStart, int xEnd, int yEnd)
	{
		fixed (int* ptr = map)
		{
			uint* ptr2 = (uint*)ptr;
			int num = xEnd - xStart;
			int num2 = yEnd - yStart;
			int num3 = range / 2;
			uint[] array = new uint[num2];
			int num4 = -(num3 + 1) * num;
			int num5 = num3 * num;
			for (int i = xStart; i < xEnd; i++)
			{
				int num6 = 0;
				int num7 = 0;
				int num8 = 0;
				int num9 = 0;
				int num10 = 0;
				int num11 = yStart * num - num3 * num + i;
				for (int j = yStart - num3; j < yEnd; j++)
				{
					if (j - num3 - 1 >= yStart)
					{
						int num12 = (int)ptr2[num11 + num4];
						if (num12 != 0)
						{
							num7 -= (num12 >> 24) & 0xFF;
							num8 -= (num12 >> 16) & 0xFF;
							num9 -= (num12 >> 8) & 0xFF;
							num10 -= num12 & 0xFF;
						}
						num6--;
					}
					if (j + num3 < yEnd)
					{
						int num13 = (int)ptr2[num11 + num5];
						if (num13 != 0)
						{
							num7 += (num13 >> 24) & 0xFF;
							num8 += (num13 >> 16) & 0xFF;
							num9 += (num13 >> 8) & 0xFF;
							num10 += num13 & 0xFF;
						}
						num6++;
					}
					if (j >= yStart)
					{
						uint num14 = (uint)(((byte)(num7 / num6) << 24) | ((byte)(num8 / num6) << 16) | ((byte)(num9 / num6) << 8) | (byte)(num10 / num6));
						array[j] = num14;
					}
					num11 += num;
				}
				for (int k = yStart; k < yEnd; k++)
				{
					ptr2[k * num + i] = array[k];
				}
			}
		}
	}
}
