using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public static class NormalUtil
{
	public static int NegBit = 512;

	public static int tenBitMask = 1023;

	public static int nineBitMask = 511;

	public static int tenthBitMask = 512;

	public static void FromPackedNormal(int normal, ref Vec4f toFill)
	{
		int num = normal >> 10;
		int num2 = normal >> 20;
		bool flag = (tenthBitMask & normal) > 0;
		bool flag2 = (tenthBitMask & num) > 0;
		bool flag3 = (tenthBitMask & num2) > 0;
		toFill.X = (float)(flag ? (~normal & nineBitMask) : (normal & nineBitMask)) / 512f;
		toFill.Y = (float)(flag2 ? (~num & nineBitMask) : (num & nineBitMask)) / 512f;
		toFill.Z = (float)(flag3 ? (~num2 & nineBitMask) : (num2 & nineBitMask)) / 512f;
		toFill.W = normal >> 30;
	}

	public static void FromPackedNormal(int normal, ref float[] toFill)
	{
		int num = normal >> 10;
		int num2 = normal >> 20;
		bool flag = (tenthBitMask & normal) > 0;
		bool flag2 = (tenthBitMask & num) > 0;
		bool flag3 = (tenthBitMask & num2) > 0;
		toFill[0] = (float)(flag ? (~normal & nineBitMask) : (normal & nineBitMask)) / 512f;
		toFill[1] = (float)(flag2 ? (~num & nineBitMask) : (num & nineBitMask)) / 512f;
		toFill[2] = (float)(flag3 ? (~num2 & nineBitMask) : (num2 & nineBitMask)) / 512f;
		toFill[3] = normal >> 30;
	}

	public static int PackNormal(Vec4f normal)
	{
		bool num = normal.X < 0f;
		bool flag = normal.Y < 0f;
		bool flag2 = normal.Z < 0f;
		int num2 = (int)Math.Abs(normal.X * 511f);
		int num3 = (int)Math.Abs(normal.Y * 511f);
		int num4 = (int)Math.Abs(normal.Z * 511f);
		return (num ? (NegBit | (~num2 & nineBitMask)) : num2) | ((flag ? (NegBit | (~num3 & nineBitMask)) : num3) << 10) | ((flag2 ? (NegBit | (~num4 & nineBitMask)) : num4) << 20) | ((int)normal.W << 30);
	}

	public static int PackNormal(float x, float y, float z)
	{
		bool num = x < 0f;
		bool flag = y < 0f;
		bool flag2 = z < 0f;
		int num2 = (num ? (NegBit | (~(int)Math.Abs(x * 511f) & nineBitMask)) : ((int)(x * 511f) & nineBitMask));
		int num3 = (flag ? (NegBit | (~(int)Math.Abs(y * 511f) & nineBitMask)) : ((int)(y * 511f) & nineBitMask));
		int num4 = (flag2 ? (NegBit | (~(int)Math.Abs(z * 511f) & nineBitMask)) : ((int)(z * 511f) & nineBitMask));
		return num2 | (num3 << 10) | (num4 << 20);
	}

	internal static int PackNormal(float[] normal)
	{
		bool num = normal[0] < 0f;
		bool flag = normal[1] < 0f;
		bool flag2 = normal[2] < 0f;
		int num2 = (int)Math.Abs(normal[0] * 511f);
		int num3 = (int)Math.Abs(normal[1] * 511f);
		int num4 = (int)Math.Abs(normal[2] * 511f);
		return (num ? (NegBit | (~num2 & nineBitMask)) : num2) | ((flag ? (NegBit | (~num3 & nineBitMask)) : num3) << 10) | ((flag2 ? (NegBit | (~num4 & nineBitMask)) : num4) << 20) | ((int)normal[3] << 30);
	}

	internal static void FromPackedNormal(int normal, ref double[] toFill)
	{
		int num = normal >> 10;
		int num2 = normal >> 20;
		bool flag = (tenthBitMask & normal) > 0;
		bool flag2 = (tenthBitMask & num) > 0;
		bool flag3 = (tenthBitMask & num2) > 0;
		toFill[0] = (float)(flag ? (~normal & nineBitMask) : (normal & nineBitMask)) / 512f;
		toFill[1] = (float)(flag2 ? (~num & nineBitMask) : (num & nineBitMask)) / 512f;
		toFill[2] = (float)(flag3 ? (~num2 & nineBitMask) : (num2 & nineBitMask)) / 512f;
		toFill[3] = normal >> 30;
	}

	internal static int PackNormal(double[] normal)
	{
		bool num = normal[0] < 0.0;
		bool flag = normal[1] < 0.0;
		bool flag2 = normal[2] < 0.0;
		int num2 = (int)Math.Abs(normal[0] * 512.0);
		int num3 = (int)Math.Abs(normal[1] * 512.0);
		int num4 = (int)Math.Abs(normal[2] * 512.0);
		return (num ? (NegBit | (~num2 & tenBitMask)) : num2) | ((flag ? (NegBit | (~num3 & tenBitMask)) : num3) << 10) | ((flag2 ? (NegBit | (~num4 & tenBitMask)) : num4) << 20) | ((int)normal[3] << 30);
	}
}
