using System;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public static class BlockMaterialUtil
{
	private static double[][] blastResistances;

	private static double[][] blastDropChances;

	static BlockMaterialUtil()
	{
		Array values = Enum.GetValues(typeof(EnumBlastType));
		Array values2 = Enum.GetValues(typeof(EnumBlockMaterial));
		blastResistances = new double[values.Length][];
		blastDropChances = new double[values.Length][];
		int num = 1;
		blastResistances[num] = new double[values2.Length];
		blastDropChances[num] = new double[values2.Length];
		blastDropChances[num].Fill(0.2);
		blastResistances[num][0] = 0.0;
		blastResistances[num][1] = 3.2;
		blastResistances[num][2] = 3.2;
		blastResistances[num][3] = 2.4;
		blastResistances[num][4] = 2.0;
		blastResistances[num][5] = 0.4;
		blastResistances[num][6] = 2.0;
		blastResistances[num][7] = 16.0;
		blastResistances[num][8] = 4.0;
		blastResistances[num][9] = 0.4;
		blastResistances[num][10] = 2.0;
		blastResistances[num][11] = 8.0;
		blastResistances[num][12] = 999999.0;
		blastResistances[num][13] = 0.1;
		blastResistances[num][14] = 0.1;
		blastResistances[num][15] = 0.3;
		blastResistances[num][16] = 0.2;
		blastResistances[num][17] = 6.0;
		blastResistances[num][18] = 4.0;
		blastResistances[num][21] = 1.0;
		num = 0;
		blastDropChances[num] = new double[values2.Length];
		blastDropChances[num].Fill(0.25);
		blastDropChances[num][7] = 0.9;
		blastResistances[num] = new double[values2.Length];
		blastResistances[num][0] = 0.0;
		blastResistances[num][1] = 1.6;
		blastResistances[num][2] = 3.2;
		blastResistances[num][3] = 2.4;
		blastResistances[num][4] = 2.0;
		blastResistances[num][5] = 0.4;
		blastResistances[num][6] = 3.0;
		blastResistances[num][8] = 4.0;
		blastResistances[num][9] = 0.4;
		blastResistances[num][10] = 2.0;
		blastResistances[num][11] = 8.0;
		blastResistances[num][12] = 999999.0;
		blastResistances[num][13] = 0.1;
		blastResistances[num][14] = 0.1;
		blastResistances[num][15] = 0.3;
		blastResistances[num][16] = 0.2;
		blastResistances[num][17] = 6.0;
		blastResistances[num][18] = 4.0;
		blastResistances[num][21] = 1.0;
		num = 2;
		blastDropChances[num] = new double[values2.Length];
		blastDropChances[num].Fill(0.5);
		blastResistances[num] = new double[values2.Length];
		blastResistances[num][0] = 0.0;
		blastResistances[num][1] = 38.400000000000006;
		blastResistances[num][2] = 38.400000000000006;
		blastResistances[num][3] = 28.799999999999997;
		blastResistances[num][4] = 24.0;
		blastResistances[num][5] = 4.800000000000001;
		blastResistances[num][6] = 67.19999999999999;
		blastResistances[num][7] = 48.0;
		blastResistances[num][8] = 48.0;
		blastResistances[num][9] = 4.800000000000001;
		blastResistances[num][10] = 24.0;
		blastResistances[num][11] = 96.0;
		blastResistances[num][12] = 11999988.0;
		blastResistances[num][13] = 1.2000000000000002;
		blastResistances[num][14] = 1.2000000000000002;
		blastResistances[num][15] = 3.5999999999999996;
		blastResistances[num][16] = 2.4000000000000004;
		blastResistances[num][17] = 72.0;
		blastResistances[num][18] = 48.0;
		blastResistances[num][21] = 12.0;
	}

	public static double MaterialBlastResistance(EnumBlastType blastType, EnumBlockMaterial material)
	{
		return blastResistances[(int)blastType][(int)material];
	}

	public static double MaterialBlastDropChances(EnumBlastType blastType, EnumBlockMaterial material)
	{
		return blastDropChances[(int)blastType][(int)material];
	}
}
