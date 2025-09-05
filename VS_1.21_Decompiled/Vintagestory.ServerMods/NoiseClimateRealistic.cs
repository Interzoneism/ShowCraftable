using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class NoiseClimateRealistic : NoiseClimatePatchy
{
	private double halfRange;

	private float geologicActivityInv = 10f;

	public float GeologicActivityStrength
	{
		set
		{
			geologicActivityInv = 1f / value;
		}
	}

	public double ZOffset { get; private set; }

	public NoiseClimateRealistic(long seed, double mapsizeZ, int polarEquatorDistance, int spawnMinTemp, int spawnMaxTemp)
		: base(seed + 1)
	{
		halfRange = polarEquatorDistance / TerraGenConfig.climateMapScale / TerraGenConfig.climateMapSubScale;
		int num = Climate.DescaleTemperature(spawnMinTemp);
		int num2 = Climate.DescaleTemperature(spawnMaxTemp);
		double num3 = num + NextInt(num2 - num + 1);
		double num4 = halfRange / 255.0;
		ZOffset = num3 * num4 - mapsizeZ / 2.0;
	}

	public override int GetClimateAt(int posX, int posZ)
	{
		InitPositionSeed(posX, posZ);
		return GetRandomClimate(posX, posZ);
	}

	public override int GetLerpedClimateAt(double posX, double posZ)
	{
		int num = (int)posX;
		int num2 = (int)posZ;
		InitPositionSeed(num, num2);
		int randomClimate = GetRandomClimate(posX, posZ);
		InitPositionSeed(num + 1, num2);
		int randomClimate2 = GetRandomClimate(posX, posZ);
		InitPositionSeed(num, num2 + 1);
		int randomClimate3 = GetRandomClimate(posX, posZ);
		InitPositionSeed(num + 1, num2 + 1);
		int randomClimate4 = GetRandomClimate(posX, posZ);
		return GameMath.BiSerpRgbColor((float)(posX - (double)num), (float)(posZ - (double)num2), randomClimate, randomClimate2, randomClimate3, randomClimate4);
	}

	public override int GetLerpedClimateAt(double posX, double posZ, int[] climateCache, int sizeX)
	{
		int num = (int)posX;
		int num2 = (int)posZ;
		return GameMath.BiSerpRgbColor((float)(posX - (double)num), (float)(posZ - (double)num2), climateCache[num2 * sizeX + num], climateCache[num2 * sizeX + num + 1], climateCache[(num2 + 1) * sizeX + num], climateCache[(num2 + 1) * sizeX + num + 1]);
	}

	private int GetRandomClimate(double posX, double posZ)
	{
		int num = NextInt(51) - 35;
		double num2 = halfRange;
		double value = posZ + ZOffset;
		int num3 = GameMath.Clamp((int)((float)((int)(255.0 / num2 * (num2 - Math.Abs(Math.Abs(value) % (2.0 * num2) - num2))) + num) * tempMul), 0, 255);
		int num4 = Math.Min(255, (int)((float)NextInt(256) * rainMul));
		int num5 = (int)Math.Max(0.0, Math.Pow((float)NextInt(256) / 255f, geologicActivityInv) * 255.0);
		return (num3 << 16) + (num4 << 8) + num5;
	}
}
