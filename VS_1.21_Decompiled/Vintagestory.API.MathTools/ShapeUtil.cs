using System;
using System.Collections.Generic;
using System.Linq;

namespace Vintagestory.API.MathTools;

public static class ShapeUtil
{
	private static Vec3f[][] cubicShellNormalizedVectors;

	public static int MaxShells;

	static ShapeUtil()
	{
		MaxShells = 38;
		cubicShellNormalizedVectors = new Vec3f[MaxShells][];
		int[] array = new int[2];
		for (int i = 1; i < MaxShells; i++)
		{
			cubicShellNormalizedVectors[i] = new Vec3f[(2 * i + 1) * (2 * i + 1) * 6];
			int num = 0;
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing blockFacing in aLLFACES)
			{
				array[0] = -i;
				while (array[0] <= i)
				{
					array[1] = -i;
					while (array[1] <= i)
					{
						Vec3f vec3f = new Vec3f(blockFacing.Normali.X * i, blockFacing.Normali.Y * i, blockFacing.Normali.Z * i);
						int num2 = 0;
						if (vec3f.X == 0f)
						{
							vec3f.X = array[num2++];
						}
						if (vec3f.Y == 0f)
						{
							vec3f.Y = array[num2++];
						}
						if (num2 < 2 && vec3f.Z == 0f)
						{
							vec3f.Z = array[num2++];
						}
						cubicShellNormalizedVectors[i][num++] = vec3f.Normalize();
						array[1]++;
					}
					array[0]++;
				}
			}
		}
	}

	public static Vec3f[] GetCachedCubicShellNormalizedVectors(int radius)
	{
		return cubicShellNormalizedVectors[radius];
	}

	public static Vec3i[] GenCubicShellVectors(int r)
	{
		int[] array = new int[2];
		Vec3i[] array2 = new Vec3i[(2 * r + 1) * (2 * r + 1) * 6];
		int num = 0;
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing in aLLFACES)
		{
			array[0] = -r;
			while (array[0] <= r)
			{
				array[1] = -r;
				while (array[1] <= r)
				{
					Vec3i vec3i = new Vec3i(blockFacing.Normali.X * r, blockFacing.Normali.Y * r, blockFacing.Normali.Z * r);
					int num2 = 0;
					if (vec3i.X == 0)
					{
						vec3i.X = array[num2++];
					}
					if (vec3i.Y == 0)
					{
						vec3i.Y = array[num2++];
					}
					if (num2 < 2 && vec3i.Z == 0)
					{
						vec3i.Z = array[num2++];
					}
					array2[num++] = vec3i;
					array[1]++;
				}
				array[0]++;
			}
		}
		return array2;
	}

	public static Vec2i[] GetSquarePointsSortedByMDist(int halflength)
	{
		if (halflength == 0)
		{
			return Array.Empty<Vec2i>();
		}
		Vec2i[] array = new Vec2i[(2 * halflength + 1) * (2 * halflength + 1) - 1];
		int num = 0;
		for (int i = -halflength; i <= halflength; i++)
		{
			for (int j = -halflength; j <= halflength; j++)
			{
				if (i != 0 || j != 0)
				{
					array[num++] = new Vec2i(i, j);
				}
			}
		}
		return array.OrderBy((Vec2i vec) => vec.ManhattenDistance(0, 0)).ToArray();
	}

	public static Vec2i[] GetHollowSquarePoints(int halflength)
	{
		if (halflength == 0)
		{
			return Array.Empty<Vec2i>();
		}
		int num = halflength * 2 + 1;
		Vec2i[] array = new Vec2i[num * 4 - 4];
		int num2 = 0;
		for (int i = 0; i < num * 4 - 1; i++)
		{
			int num3 = i % num - halflength;
			int num4 = i % num - halflength;
			int num5 = i / num;
			switch (num5)
			{
			case 0:
				num4 = -halflength;
				break;
			case 1:
				num3 = halflength;
				break;
			case 2:
				num4 = halflength;
				num3 = -num3;
				break;
			case 3:
				num3 = -halflength;
				num4 = -num4;
				break;
			}
			array[num2++] = new Vec2i(num3, num4);
			if ((i + 1) / num > num5)
			{
				i++;
			}
		}
		return array;
	}

	public static Vec2i[] GetOctagonPoints(int x, int y, int r)
	{
		if (r == 0)
		{
			return new Vec2i[1]
			{
				new Vec2i(x, y)
			};
		}
		List<Vec2i> list = new List<Vec2i>();
		int num = 9;
		int num2 = 2 * r;
		int num3 = Math.Min(num2, num);
		int num4 = (int)Math.Ceiling((double)Math.Max(0, num2 - num) / 2.0);
		int num5 = num3 / 2;
		for (int i = 0; i < num3; i++)
		{
			list.Add(new Vec2i(x + i - num5, y - r));
			list.Add(new Vec2i(x - i + num5, y + r));
			list.Add(new Vec2i(x - r, y - i + num5));
			list.Add(new Vec2i(x + r, y + i - num5));
		}
		for (int j = 0; j < num4; j++)
		{
			list.Add(new Vec2i(x + num5 + j, y - r + j));
			list.Add(new Vec2i(x - r + j, y + num5 + j));
			list.Add(new Vec2i(x - r + j, y - num5 - j));
			list.Add(new Vec2i(x + num5 + j, y + r - j));
		}
		return Enumerable.ToArray(list);
	}

	public static void LoadOctagonIndices(ICollection<long> list, int x, int y, int r, int mapSizeX)
	{
		if (r == 0)
		{
			list.Add(MapUtil.Index2dL(x, y, mapSizeX));
			return;
		}
		int num = 2 * r;
		int num2 = Math.Min(num, 9);
		int num3 = (int)((double)Math.Max(0, num - 9) / Math.Sqrt(2.0));
		int num4 = num2 / 2;
		for (int i = 0; i < num2; i++)
		{
			list.Add(MapUtil.Index2dL(x + i - num4, y - r, mapSizeX));
			list.Add(MapUtil.Index2dL(x - i + num4, y + r, mapSizeX));
			list.Add(MapUtil.Index2dL(x - r, y - i + num4, mapSizeX));
			list.Add(MapUtil.Index2dL(x + r, y + i - num4, mapSizeX));
		}
		for (int j = 0; j < num3; j++)
		{
			list.Add(MapUtil.Index2dL(x + num4 + j, y - r + j, mapSizeX));
			list.Add(MapUtil.Index2dL(x - r + j, y + num4 + j, mapSizeX));
			list.Add(MapUtil.Index2dL(x - r + j, y - num4 - j, mapSizeX));
			list.Add(MapUtil.Index2dL(x + num4 + j, y + r - j, mapSizeX));
		}
	}

	public static Vec2i[] GetPointsOfCircle(int xm, int ym, int r)
	{
		List<Vec2i> list = new List<Vec2i>();
		int num = -r;
		int num2 = 0;
		int num3 = 2 - 2 * r;
		do
		{
			list.Add(new Vec2i(xm - num, ym + num2));
			list.Add(new Vec2i(xm - num2, ym - num));
			list.Add(new Vec2i(xm + num, ym - num2));
			list.Add(new Vec2i(xm + num2, ym + num));
			r = num3;
			if (r <= num2)
			{
				num3 += ++num2 * 2 + 1;
			}
			if (r > num || num3 > num2)
			{
				num3 += ++num * 2 + 1;
			}
		}
		while (num < 0);
		return Enumerable.ToArray(list);
	}
}
