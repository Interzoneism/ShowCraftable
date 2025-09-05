using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class NoiseClimatePatchy : NoiseClimate
{
	public NoiseClimatePatchy(long seed)
		: base(seed)
	{
	}

	public override int GetClimateAt(int posX, int posZ)
	{
		InitPositionSeed(posX, posZ);
		return GetRandomClimate();
	}

	public override int GetLerpedClimateAt(double posX, double posZ)
	{
		int num = (int)posX;
		int num2 = (int)posZ;
		InitPositionSeed(num, num2);
		int randomClimate = GetRandomClimate();
		InitPositionSeed(num + 1, num2);
		int randomClimate2 = GetRandomClimate();
		InitPositionSeed(num, num2 + 1);
		int randomClimate3 = GetRandomClimate();
		InitPositionSeed(num + 1, num2 + 1);
		int randomClimate4 = GetRandomClimate();
		return GameMath.BiSerpRgbColor((float)(posX - (double)num), (float)(posZ - (double)num2), randomClimate, randomClimate2, randomClimate3, randomClimate4);
	}

	public override int GetLerpedClimateAt(double posX, double posZ, int[] climateCache, int sizeX)
	{
		int num = (int)posX;
		int num2 = (int)posZ;
		return GameMath.BiSerpRgbColor((float)(posX - (double)num), (float)(posZ - (double)num2), climateCache[num2 * sizeX + num], climateCache[num2 * sizeX + num + 1], climateCache[(num2 + 1) * sizeX + num], climateCache[(num2 + 1) * sizeX + num + 1]);
	}

	protected int gaussRnd3(int maxint)
	{
		return Math.Min(255, (NextInt(maxint) + NextInt(maxint) + NextInt(maxint)) / 3);
	}

	protected int gaussRnd2(int maxint)
	{
		return Math.Min(255, (NextInt(maxint) + NextInt(maxint)) / 2);
	}

	protected virtual int GetRandomClimate()
	{
		int num = NextIntFast(127);
		int num2 = Math.Max(0, NextInt(256) - 128) * 2;
		int num3;
		int num4;
		if (num < 20)
		{
			num3 = Math.Min(255, (int)((float)gaussRnd3(60) * tempMul));
			num4 = Math.Min(255, (int)((float)gaussRnd3(130) * rainMul));
			return (num3 << 16) + (num4 << 8) + num2;
		}
		if (num < 40)
		{
			num3 = Math.Min(255, (int)((float)(220 + gaussRnd3(75)) * tempMul));
			num4 = Math.Min(255, (int)((float)gaussRnd3(20) * rainMul));
			return (num3 << 16) + (num4 << 8) + num2;
		}
		if (num < 50)
		{
			num3 = Math.Min(255, (int)((float)(220 + gaussRnd3(75)) * tempMul));
			num4 = Math.Min(255, (int)((float)(220 + NextInt(35)) * rainMul));
			return (num3 << 16) + (num4 << 8) + num2;
		}
		if (num < 55)
		{
			num3 = Math.Min(255, (int)((float)(120 + NextInt(60)) * tempMul));
			num4 = Math.Min(255, (int)((float)(200 + NextInt(50)) * rainMul));
			return (num3 << 16) + (num4 << 8) + num2;
		}
		num3 = Math.Min(255, (int)((float)(100 + gaussRnd2(165)) * tempMul));
		num4 = Math.Min(255, (int)((float)gaussRnd3(210 - (150 - num3)) * rainMul));
		return (num3 << 16) + (num4 << 8) + num2;
	}
}
