using System;
using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Datastructures;

[ProtoContract]
public class FloatDataMap3D
{
	[ProtoMember(1)]
	public float[] Data;

	[ProtoMember(2)]
	public int Width;

	[ProtoMember(3)]
	public int Length;

	[ProtoMember(4)]
	public int Height;

	public FloatDataMap3D()
	{
	}

	public FloatDataMap3D(int width, int height, int length)
	{
		Width = width;
		Length = length;
		Height = height;
		Data = new float[width * height * length];
	}

	public float GetValue(int x, int y, int z)
	{
		return Data[(y * Length + z) * Width + x];
	}

	public void SetValue(int x, int y, int z, float value)
	{
		Data[(y * Length + z) * Width + x] = value;
	}

	public void AddValue(int x, int y, int z, float value)
	{
		Data[(y * Length + z) * Width + x] += value;
	}

	public float GetLerped(float x, float y, float z)
	{
		int num = (int)x;
		int num2 = num + 1;
		int num3 = (int)y;
		int num4 = num3 + 1;
		int num5 = (int)z;
		int num6 = num5 + 1;
		float x2 = x - (float)(int)x;
		float t = y - (float)(int)y;
		float z2 = z - (float)(int)z;
		float v = GameMath.BiLerp(Data[(num3 * Length + num5) * Width + num], Data[(num3 * Length + num5) * Width + num2], Data[(num3 * Length + num6) * Width + num], Data[(num3 * Length + num6) * Width + num2], x2, z2);
		float v2 = GameMath.BiLerp(Data[(num4 * Length + num5) * Width + num], Data[(num4 * Length + num5) * Width + num2], Data[(num4 * Length + num6) * Width + num], Data[(num4 * Length + num6) * Width + num2], x2, z2);
		return GameMath.Lerp(v, v2, t);
	}

	public float GetLerpedCenterPixel(float x, float y, float z)
	{
		int num = (int)Math.Floor(x - 0.5f);
		int num2 = num + 1;
		int num3 = (int)Math.Floor(y - 0.5f);
		int num4 = num3 + 1;
		int num5 = (int)Math.Floor(z - 0.5f);
		int num6 = num5 + 1;
		float x2 = x - ((float)num + 0.5f);
		float t = y - ((float)num3 + 0.5f);
		float z2 = z - ((float)num5 + 0.5f);
		float v = GameMath.BiLerp(Data[(num3 * Length + num5) * Width + num], Data[(num3 * Length + num5) * Width + num2], Data[(num3 * Length + num6) * Width + num], Data[(num3 * Length + num6) * Width + num2], x2, z2);
		return GameMath.Lerp(GameMath.BiLerp(Data[(num4 * Length + num5) * Width + num], Data[(num4 * Length + num5) * Width + num2], Data[(num4 * Length + num6) * Width + num], Data[(num4 * Length + num6) * Width + num2], x2, z2), v, t);
	}
}
