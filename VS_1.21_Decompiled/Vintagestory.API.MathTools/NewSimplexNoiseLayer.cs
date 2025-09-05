using System;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.MathTools;

public static class NewSimplexNoiseLayer
{
	public const double OldToNewFrequency = 0.6123724356957945;

	public const double MaxYSlope_ImprovedXZ = 5.0;

	private const long PrimeX = 5910200641878280303L;

	private const long PrimeY = 6452764530575939509L;

	private const long PrimeZ = 6614699811220273867L;

	private const long HashMultiplier = 6026932503003350773L;

	private const long SeedFlip3D = -5968755714895566377L;

	private const double Root3Over3 = 0.577350269189626;

	private const double FallbackRotate3 = 2.0 / 3.0;

	private const double Rotate3Orthogonalizer = -0.211324865405187;

	private const int NGrads3DExponent = 8;

	private const int NGrads3D = 256;

	private const double Normalizer3D = 0.2781926117527186;

	private static readonly float[] Gradients3D;

	public static float Evaluate_ImprovedXZ(long seed, double x, double y, double z)
	{
		double num = x + z;
		double num2 = num * -0.211324865405187;
		double num3 = y * 0.577350269189626;
		double xr = x + num2 + num3;
		double zr = z + num2 + num3;
		double yr = num * -0.577350269189626 + num3;
		return Noise3_UnrotatedBase(seed, xr, yr, zr);
	}

	public static float Evaluate_FallbackOrientation(long seed, double x, double y, double z)
	{
		double num = 2.0 / 3.0 * (x + y + z);
		double xr = x - num;
		double yr = y - num;
		double zr = z - num;
		return Noise3_UnrotatedBase(seed, xr, yr, zr);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float Noise3_UnrotatedBase(long seed, double xr, double yr, double zr)
	{
		int num = (int)Math.Floor(xr);
		int num2 = (int)Math.Floor(yr);
		int num3 = (int)Math.Floor(zr);
		float num4 = (float)(xr - (double)num);
		float num5 = (float)(yr - (double)num2);
		float num6 = (float)(zr - (double)num3);
		long num7 = num * 5910200641878280303L;
		long num8 = num2 * 6452764530575939509L;
		long num9 = num3 * 6614699811220273867L;
		long seed2 = seed ^ -5968755714895566377L;
		int num10 = (int)(-0.5f - num4);
		int num11 = (int)(-0.5f - num5);
		int num12 = (int)(-0.5f - num6);
		float num13 = num4 + (float)num10;
		float num14 = num5 + (float)num11;
		float num15 = num6 + (float)num12;
		float num16 = 0.75f - num13 * num13 - num14 * num14 - num15 * num15;
		long hash = HashPrimes(seed, num7 + (num10 & 0x5205402B9270C86FL), num8 + (num11 & 0x598CD327003817B5L), num9 + (num12 & 0x5BCC226E9FA0BACBL));
		float num17 = num16 * num16 * (num16 * num16) * Grad(hash, num13, num14, num15);
		float num18 = num4 - 0.5f;
		float num19 = num5 - 0.5f;
		float num20 = num6 - 0.5f;
		float num21 = 0.75f - num18 * num18 - num19 * num19 - num20 * num20;
		num17 += num21 * num21 * (num21 * num21) * Grad(HashPrimes(seed2, num7 + 5910200641878280303L, num8 + 6452764530575939509L, num9 + 6614699811220273867L), num18, num19, num20);
		float num22 = (float)((num10 | 1) << 1) * num18;
		float num23 = (float)((num11 | 1) << 1) * num19;
		float num24 = (float)((num12 | 1) << 1) * num20;
		float num25 = (float)(-2 - (num10 << 2)) * num18 - 1f;
		float num26 = (float)(-2 - (num11 << 2)) * num19 - 1f;
		float num27 = (float)(-2 - (num12 << 2)) * num20 - 1f;
		bool flag = false;
		float num28 = num22 + num16;
		if (num28 > 0f)
		{
			num17 += num28 * num28 * (num28 * num28) * Grad(HashPrimes(seed, num7 + (~num10 & 0x5205402B9270C86FL), num8 + (num11 & 0x598CD327003817B5L), num9 + (num12 & 0x5BCC226E9FA0BACBL)), num13 - (float)(num10 | 1), num14, num15);
		}
		else
		{
			float num29 = num23 + num24 + num16;
			if (num29 > 0f)
			{
				num17 += num29 * num29 * (num29 * num29) * Grad(HashPrimes(seed, num7 + (num10 & 0x5205402B9270C86FL), num8 + (~num11 & 0x598CD327003817B5L), num9 + (~num12 & 0x5BCC226E9FA0BACBL)), num13, num14 - (float)(num11 | 1), num15 - (float)(num12 | 1));
			}
			float num30 = num25 + num21;
			if (num30 > 0f)
			{
				num17 += num30 * num30 * (num30 * num30) * Grad(HashPrimes(seed2, num7 + (num10 & -6626342789952991010L), num8 + 6452764530575939509L, num9 + 6614699811220273867L), (float)(num10 | 1) + num18, num19, num20);
				flag = true;
			}
		}
		bool flag2 = false;
		float num31 = num23 + num16;
		if (num31 > 0f)
		{
			num17 += num31 * num31 * (num31 * num31) * Grad(HashPrimes(seed, num7 + (num10 & 0x5205402B9270C86FL), num8 + (~num11 & 0x598CD327003817B5L), num9 + (num12 & 0x5BCC226E9FA0BACBL)), num13, num14 - (float)(num11 | 1), num15);
		}
		else
		{
			float num32 = num22 + num24 + num16;
			if (num32 > 0f)
			{
				num17 += num32 * num32 * (num32 * num32) * Grad(HashPrimes(seed, num7 + (~num10 & 0x5205402B9270C86FL), num8 + (num11 & 0x598CD327003817B5L), num9 + (~num12 & 0x5BCC226E9FA0BACBL)), num13 - (float)(num10 | 1), num14, num15 - (float)(num12 | 1));
			}
			float num33 = num26 + num21;
			if (num33 > 0f)
			{
				num17 += num33 * num33 * (num33 * num33) * Grad(HashPrimes(seed2, num7 + 5910200641878280303L, num8 + (num11 & -5541215012557672598L), num9 + 6614699811220273867L), num18, (float)(num11 | 1) + num19, num20);
				flag2 = true;
			}
		}
		bool flag3 = false;
		float num34 = num24 + num16;
		if (num34 > 0f)
		{
			num17 += num34 * num34 * (num34 * num34) * Grad(HashPrimes(seed, num7 + (num10 & 0x5205402B9270C86FL), num8 + (num11 & 0x598CD327003817B5L), num9 + (~num12 & 0x5BCC226E9FA0BACBL)), num13, num14, num15 - (float)(num12 | 1));
		}
		else
		{
			float num35 = num22 + num23 + num16;
			if (num35 > 0f)
			{
				num17 += num35 * num35 * (num35 * num35) * Grad(HashPrimes(seed, num7 + (~num10 & 0x5205402B9270C86FL), num8 + (~num11 & 0x598CD327003817B5L), num9 + (num12 & 0x5BCC226E9FA0BACBL)), num13 - (float)(num10 | 1), num14 - (float)(num11 | 1), num15);
			}
			float num36 = num27 + num21;
			if (num36 > 0f)
			{
				num17 += num36 * num36 * (num36 * num36) * Grad(HashPrimes(seed2, num7 + 5910200641878280303L, num8 + 6452764530575939509L, num9 + (num12 & -5217344451269003882L)), num18, num19, (float)(num12 | 1) + num20);
				flag3 = true;
			}
		}
		if (!flag)
		{
			float num37 = num26 + num27 + num21;
			if (num37 > 0f)
			{
				num17 += num37 * num37 * (num37 * num37) * Grad(HashPrimes(seed2, num7 + 5910200641878280303L, num8 + (num11 & -5541215012557672598L), num9 + (num12 & -5217344451269003882L)), num18, (float)(num11 | 1) + num19, (float)(num12 | 1) + num20);
			}
		}
		if (!flag2)
		{
			float num38 = num25 + num27 + num21;
			if (num38 > 0f)
			{
				num17 += num38 * num38 * (num38 * num38) * Grad(HashPrimes(seed2, num7 + (num10 & -6626342789952991010L), num8 + 6452764530575939509L, num9 + (num12 & -5217344451269003882L)), (float)(num10 | 1) + num18, num19, (float)(num12 | 1) + num20);
			}
		}
		if (!flag3)
		{
			float num39 = num25 + num26 + num21;
			if (num39 > 0f)
			{
				num17 += num39 * num39 * (num39 * num39) * Grad(HashPrimes(seed2, num7 + (num10 & -6626342789952991010L), num8 + (num11 & -5541215012557672598L), num9 + 6614699811220273867L), (float)(num10 | 1) + num18, (float)(num11 | 1) + num19, num20);
			}
		}
		return num17;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static long HashPrimes(long seed, long xsvp, long ysvp, long zsvp)
	{
		return seed ^ xsvp ^ ysvp ^ zsvp;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float Grad(long hash, float dx, float dy, float dz)
	{
		hash *= 6026932503003350773L;
		hash ^= hash >> 58;
		int num = (int)hash & 0x3FC;
		return Gradients3D[num | 0] * dx + Gradients3D[num | 1] * dy + Gradients3D[num | 2] * dz;
	}

	static NewSimplexNoiseLayer()
	{
		Gradients3D = new float[1024];
		float[] array = new float[192]
		{
			2.2247448f, 2.2247448f, -1f, 0f, 2.2247448f, 2.2247448f, 1f, 0f, 3.0862665f, 1.1721513f,
			0f, 0f, 1.1721513f, 3.0862665f, 0f, 0f, -2.2247448f, 2.2247448f, -1f, 0f,
			-2.2247448f, 2.2247448f, 1f, 0f, -1.1721513f, 3.0862665f, 0f, 0f, -3.0862665f, 1.1721513f,
			0f, 0f, -1f, -2.2247448f, -2.2247448f, 0f, 1f, -2.2247448f, -2.2247448f, 0f,
			0f, -3.0862665f, -1.1721513f, 0f, 0f, -1.1721513f, -3.0862665f, 0f, -1f, -2.2247448f,
			2.2247448f, 0f, 1f, -2.2247448f, 2.2247448f, 0f, 0f, -1.1721513f, 3.0862665f, 0f,
			0f, -3.0862665f, 1.1721513f, 0f, -2.2247448f, -2.2247448f, -1f, 0f, -2.2247448f, -2.2247448f,
			1f, 0f, -3.0862665f, -1.1721513f, 0f, 0f, -1.1721513f, -3.0862665f, 0f, 0f,
			-2.2247448f, -1f, -2.2247448f, 0f, -2.2247448f, 1f, -2.2247448f, 0f, -1.1721513f, 0f,
			-3.0862665f, 0f, -3.0862665f, 0f, -1.1721513f, 0f, -2.2247448f, -1f, 2.2247448f, 0f,
			-2.2247448f, 1f, 2.2247448f, 0f, -3.0862665f, 0f, 1.1721513f, 0f, -1.1721513f, 0f,
			3.0862665f, 0f, -1f, 2.2247448f, -2.2247448f, 0f, 1f, 2.2247448f, -2.2247448f, 0f,
			0f, 1.1721513f, -3.0862665f, 0f, 0f, 3.0862665f, -1.1721513f, 0f, -1f, 2.2247448f,
			2.2247448f, 0f, 1f, 2.2247448f, 2.2247448f, 0f, 0f, 3.0862665f, 1.1721513f, 0f,
			0f, 1.1721513f, 3.0862665f, 0f, 2.2247448f, -2.2247448f, -1f, 0f, 2.2247448f, -2.2247448f,
			1f, 0f, 1.1721513f, -3.0862665f, 0f, 0f, 3.0862665f, -1.1721513f, 0f, 0f,
			2.2247448f, -1f, -2.2247448f, 0f, 2.2247448f, 1f, -2.2247448f, 0f, 3.0862665f, 0f,
			-1.1721513f, 0f, 1.1721513f, 0f, -3.0862665f, 0f, 2.2247448f, -1f, 2.2247448f, 0f,
			2.2247448f, 1f, 2.2247448f, 0f, 1.1721513f, 0f, 3.0862665f, 0f, 3.0862665f, 0f,
			1.1721513f, 0f
		};
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (float)((double)array[i] / 0.2781926117527186);
		}
		int num = 0;
		int num2 = 0;
		while (num < Gradients3D.Length)
		{
			if (num2 == array.Length)
			{
				num2 = 0;
			}
			Gradients3D[num] = array[num2];
			num++;
			num2++;
		}
	}
}
