using System;

namespace Vintagestory.ServerMods;

internal class NoiseWind : NoiseBase
{
	private static int minStrength = 1;

	private static int maxStrength = 20;

	public NoiseWind(long seed)
		: base(seed)
	{
	}

	public PolarVector getWindAt(double xpos, double zpos)
	{
		int num = (int)xpos;
		int num2 = (int)zpos;
		InitPositionSeed(num, num2);
		PolarVector leftTop = new PolarVector((float)((double)NextInt(360) / 180.0 * Math.PI), NextInt(maxStrength - minStrength) + minStrength);
		InitPositionSeed(num - 1, num2);
		PolarVector rightTop = new PolarVector((float)((double)NextInt(360) / 180.0 * Math.PI), NextInt(maxStrength - minStrength) + minStrength);
		InitPositionSeed(num, num2 - 1);
		PolarVector leftBottom = new PolarVector((float)((double)NextInt(360) / 180.0 * Math.PI), NextInt(maxStrength - minStrength) + minStrength);
		InitPositionSeed(num - 1, num2 - 1);
		PolarVector rightBottom = new PolarVector((float)((double)NextInt(360) / 180.0 * Math.PI), NextInt(maxStrength - minStrength) + minStrength);
		return BiLerp((float)(xpos - (double)num), (float)(zpos - (double)num2), leftTop, rightTop, leftBottom, rightBottom);
	}

	private static PolarVector BiLerp(float lx, float ly, PolarVector leftTop, PolarVector rightTop, PolarVector leftBottom, PolarVector rightBottom)
	{
		PolarVector polarVector = new PolarVector(lx * leftTop.angle + (1f - lx) * rightTop.angle, lx * leftTop.length + (1f - lx) * rightTop.length);
		PolarVector polarVector2 = new PolarVector(lx * leftBottom.angle + (1f - lx) * rightBottom.angle, lx * leftBottom.length + (1f - lx) * rightBottom.length);
		return new PolarVector(ly * polarVector.angle + (1f - ly) * polarVector2.angle, ly * polarVector.length + (1f - ly) * polarVector2.length);
	}

	private static PolarVector SmoothLerp(float lx, float ly, PolarVector w1, PolarVector w2, PolarVector w3, PolarVector w4)
	{
		float num = SmoothStep(lx);
		float num2 = SmoothStep(ly);
		float num3 = SmoothStep(1f - lx);
		float num4 = SmoothStep(1f - ly);
		PolarVector polarVector = new PolarVector(num * w1.angle + num3 * w2.angle, num * w1.length + num3 * w2.length);
		PolarVector polarVector2 = new PolarVector(num * w3.angle + num3 * w4.angle, num * w3.length + num3 * w4.length);
		return new PolarVector(num2 * polarVector.angle + num4 * polarVector2.angle, num2 * polarVector.length + num4 * polarVector2.length);
	}

	private static float SmoothStep(float x)
	{
		return x * x * (3f - 2f * x);
	}
}
