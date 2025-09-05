using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class NoiseOre : NoiseBase
{
	public NoiseOre(long worldSeed)
		: base(worldSeed)
	{
	}

	public int GetOreAt(int posX, int posZ)
	{
		InitPositionSeed(posX, posZ);
		return GetRandomOre();
	}

	public int GetLerpedOreValueAt(double posX, double posZ, int[] oreCache, int sizeX, float contrastMul, float sub)
	{
		int num = (int)posX;
		int num2 = (int)posZ;
		byte b = GameMath.BiSerpByte((float)(posX - (double)num), (float)(posZ - (double)num2), 0, oreCache[num2 * sizeX + num], oreCache[num2 * sizeX + num + 1], oreCache[(num2 + 1) * sizeX + num], oreCache[(num2 + 1) * sizeX + num + 1]);
		b = (byte)GameMath.Clamp(((float)(int)b - 128f) * contrastMul + 128f - sub, 0f, 255f);
		int num3 = Math.Max(oreCache[(num2 + 1) * sizeX + num + 1] & 0xFF0000, Math.Max(oreCache[(num2 + 1) * sizeX + num] & 0xFF0000, Math.Max(oreCache[num2 * sizeX + num] & 0xFF0000, oreCache[num2 * sizeX + num + 1] & 0xFF0000)));
		int num4 = Math.Max(oreCache[(num2 + 1) * sizeX + num + 1] & 0xFF00, Math.Max(oreCache[(num2 + 1) * sizeX + num] & 0xFF00, Math.Max(oreCache[num2 * sizeX + num] & 0xFF00, oreCache[num2 * sizeX + num + 1] & 0xFF00)));
		if (b != 0)
		{
			return b | num3 | num4;
		}
		return 0;
	}

	private int GetRandomOre()
	{
		int num = NextInt(1024);
		int num2 = ((NextInt(2) > 0) ? 1 : ((NextInt(50) <= 15) ? 2 : 0)) << 10;
		if (num < 1)
		{
			return num2 | 0x100 | 0xFF;
		}
		if (num < 30)
		{
			return num2 | 0xFF;
		}
		if (num < 105)
		{
			return num2 | (75 + NextInt(100));
		}
		if (num < 190)
		{
			return num2 | (20 + NextInt(20));
		}
		return 0;
	}
}
