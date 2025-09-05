using System;
using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Datastructures;

[ProtoContract]
public class IntDataMap2D
{
	[ProtoMember(1, IsPacked = true)]
	public int[] Data;

	[ProtoMember(2)]
	public int Size;

	[ProtoMember(3)]
	public int TopLeftPadding;

	[ProtoMember(4)]
	public int BottomRightPadding;

	public int InnerSize => Size - TopLeftPadding - BottomRightPadding;

	public static IntDataMap2D CreateEmpty()
	{
		return new IntDataMap2D
		{
			Data = Array.Empty<int>(),
			Size = 0
		};
	}

	public int GetInt(int x, int z)
	{
		return Data[z * Size + x];
	}

	public void SetInt(int x, int z, int value)
	{
		Data[z * Size + x] = value;
	}

	public int GetUnpaddedInt(int x, int z)
	{
		return Data[(z + TopLeftPadding) * Size + x + TopLeftPadding];
	}

	public int GetUnpaddedColorLerped(float x, float z)
	{
		int num = (int)x;
		int num2 = (int)z;
		int num3 = (num2 + TopLeftPadding) * Size + num + TopLeftPadding;
		if (num3 < 0 || num3 + Size + 1 >= Data.Length)
		{
			throw new IndexOutOfRangeException("MapRegion data, index was " + (num3 + Size + 1) + " but length was " + Data.Length);
		}
		return GameMath.BiLerpRgbColor(x - (float)num, z - (float)num2, Data[num3], Data[num3 + 1], Data[num3 + Size], Data[num3 + Size + 1]);
	}

	public int GetUnpaddedColorLerpedForNormalizedPos(float x, float z)
	{
		int innerSize = InnerSize;
		return GetUnpaddedColorLerped(x * (float)innerSize, z * (float)innerSize);
	}

	public int GetUnpaddedIntLerpedForBlockPos(int x, int z, int regionSize)
	{
		float x2 = (float)((double)x / (double)regionSize - (double)(x / regionSize));
		float z2 = (float)((double)z / (double)regionSize - (double)(z / regionSize));
		return GetUnpaddedColorLerpedForNormalizedPos(x2, z2);
	}

	public float GetUnpaddedIntLerped(float x, float z)
	{
		int num = (int)x;
		int num2 = (int)z;
		return GameMath.BiLerp(Data[(num2 + TopLeftPadding) * Size + num + TopLeftPadding], Data[(num2 + TopLeftPadding) * Size + num + 1 + TopLeftPadding], Data[(num2 + 1 + TopLeftPadding) * Size + num + TopLeftPadding], Data[(num2 + 1 + TopLeftPadding) * Size + num + 1 + TopLeftPadding], x - (float)num, z - (float)num2);
	}

	public float GetIntLerpedCorrectly(float x, float z)
	{
		int num = (int)Math.Floor(x - 0.5f);
		int num2 = (int)Math.Floor(z - 0.5f);
		float x2 = x - ((float)num + 0.5f);
		float z2 = z - ((float)num2 + 0.5f);
		int num3 = (num2 + TopLeftPadding) * Size + num + TopLeftPadding;
		return GameMath.BiLerp(Data[num3], Data[num3 + 1], Data[num3 + Size], Data[num3 + Size + 1], x2, z2);
	}

	public int GetColorLerpedCorrectly(float x, float z)
	{
		int num = (int)Math.Floor(x - 0.5f);
		int num2 = (int)Math.Floor(z - 0.5f);
		float lx = x - ((float)num + 0.5f);
		float ly = z - ((float)num2 + 0.5f);
		int num3 = (num2 + TopLeftPadding) * Size + num + TopLeftPadding;
		return GameMath.BiLerpRgbColor(lx, ly, Data[num3], Data[num3 + 1], Data[num3 + Size], Data[num3 + Size + 1]);
	}
}
