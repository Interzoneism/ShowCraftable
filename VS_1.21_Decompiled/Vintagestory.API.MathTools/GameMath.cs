using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.MathTools;

public static class GameMath
{
	public const float TWOPI = (float)Math.PI * 2f;

	public const float PI = (float)Math.PI;

	public const float PIHALF = (float)Math.PI / 2f;

	public const double DEG2RAD_DOUBLE = Math.PI / 180.0;

	public const float DEG2RAD = (float)Math.PI / 180f;

	public const float RAD2DEG = 180f / (float)Math.PI;

	private const uint murmurseed = 144u;

	private static int SIN_BITS;

	private static int SIN_MASK;

	private static int SIN_COUNT;

	private static float radFull;

	private static float radToIndex;

	private static float degFull;

	private static float degToIndex;

	private static float[] sinValues;

	private static float[] cosValues;

	private static readonly int OaatIterations;

	public static float Sin(float value)
	{
		return (float)Math.Sin(value);
	}

	public static float Cos(float value)
	{
		return (float)Math.Cos(value);
	}

	public static float Acos(float value)
	{
		return (float)Math.Acos(value);
	}

	public static float Asin(float value)
	{
		return (float)Math.Asin(value);
	}

	public static float Tan(float value)
	{
		return (float)Math.Tan(value);
	}

	public static double Sin(double value)
	{
		return Math.Sin(value);
	}

	public static double Cos(double value)
	{
		return Math.Cos(value);
	}

	public static double Acos(double value)
	{
		return Math.Acos(value);
	}

	public static double Asin(double value)
	{
		return Math.Asin(value);
	}

	public static double Tan(double value)
	{
		return Math.Tan(value);
	}

	public static float FastSin(float rad)
	{
		return sinValues[(int)(rad * radToIndex) & SIN_MASK];
	}

	public static float FastCos(float rad)
	{
		return cosValues[(int)(rad * radToIndex) & SIN_MASK];
	}

	public static float FastSinDeg(float deg)
	{
		return sinValues[(int)(deg * degToIndex) & SIN_MASK];
	}

	public static float FastCosDeg(float deg)
	{
		return cosValues[(int)(deg * degToIndex) & SIN_MASK];
	}

	static GameMath()
	{
		OaatIterations = 3;
		SIN_BITS = 12;
		SIN_MASK = ~(-1 << SIN_BITS);
		SIN_COUNT = SIN_MASK + 1;
		radFull = (float)Math.PI * 2f;
		degFull = 360f;
		radToIndex = (float)SIN_COUNT / radFull;
		degToIndex = (float)SIN_COUNT / degFull;
		sinValues = new float[SIN_COUNT];
		cosValues = new float[SIN_COUNT];
		for (int i = 0; i < SIN_COUNT; i++)
		{
			sinValues[i] = (float)Math.Sin(((float)i + 0.5f) / (float)SIN_COUNT * radFull);
			cosValues[i] = (float)Math.Cos(((float)i + 0.5f) / (float)SIN_COUNT * radFull);
		}
		for (int j = 0; j < 360; j += 90)
		{
			sinValues[(int)((float)j * degToIndex) & SIN_MASK] = (float)Math.Sin((double)j * Math.PI / 180.0);
			cosValues[(int)((float)j * degToIndex) & SIN_MASK] = (float)Math.Cos((double)j * Math.PI / 180.0);
		}
	}

	public static float Sqrt(float value)
	{
		return (float)Math.Sqrt(value);
	}

	public static float Sqrt(double value)
	{
		return (float)Math.Sqrt(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float RootSumOfSquares(float a, float b, float c)
	{
		return (float)Math.Sqrt(a * a + b * b + c * c);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double SumOfSquares(double a, double b, double c)
	{
		return a * a + b * b + c * c;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Square(double a)
	{
		return a * a;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Clamp(float val, float min, float max)
	{
		if (!(val < min))
		{
			if (!(val > max))
			{
				return val;
			}
			return max;
		}
		return min;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Clamp(int val, int min, int max)
	{
		if (val >= min)
		{
			if (val <= max)
			{
				return val;
			}
			return max;
		}
		return min;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte Clamp(byte val, byte min, byte max)
	{
		if (val >= min)
		{
			if (val <= max)
			{
				return val;
			}
			return max;
		}
		return min;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Clamp(double val, double min, double max)
	{
		if (!(val < min))
		{
			if (!(val > max))
			{
				return val;
			}
			return max;
		}
		return min;
	}

	public static int InverseClamp(int val, int atLeastNeg, int atLeastPos)
	{
		if (val >= atLeastPos)
		{
			if (val <= atLeastNeg)
			{
				return val;
			}
			return atLeastNeg;
		}
		return atLeastPos;
	}

	public static int Mod(int k, int n)
	{
		if ((k %= n) >= 0)
		{
			return k;
		}
		return k + n;
	}

	public static uint Mod(uint k, uint n)
	{
		if ((k %= n) >= 0)
		{
			return k;
		}
		return k + n;
	}

	public static float Mod(float k, float n)
	{
		if (!((k %= n) < 0f))
		{
			return k;
		}
		return k + n;
	}

	public static double Mod(double k, double n)
	{
		if (!((k %= n) < 0.0))
		{
			return k;
		}
		return k + n;
	}

	public static int RoundRandom(Random rand, float value)
	{
		return (int)value + ((rand.NextDouble() < (double)(value - (float)(int)value)) ? 1 : 0);
	}

	public static int RoundRandom(IRandom rand, float value)
	{
		return (int)value + ((rand.NextDouble() < (double)(value - (float)(int)value)) ? 1 : 0);
	}

	public static float AngleDegDistance(float start, float end)
	{
		return ((end - start) % 360f + 540f) % 360f - 180f;
	}

	public static float AngleRadDistance(float start, float end)
	{
		return ((end - start) % ((float)Math.PI * 2f) + (float)Math.PI * 2f + (float)Math.PI) % ((float)Math.PI * 2f) - (float)Math.PI;
	}

	public static float NormaliseAngleRad(float angleRad)
	{
		float num = 3.2E-06f;
		float num2 = Mod(angleRad, (float)Math.PI * 2f);
		if ((num2 + num) % ((float)Math.PI / 2f) <= num * 2f)
		{
			num2 = (float)((int)((num2 + num) / ((float)Math.PI / 2f)) % 4) * ((float)Math.PI / 2f);
		}
		return num2;
	}

	public static double Smallest(double a, double b)
	{
		double num = Math.Abs(a);
		double num2 = Math.Abs(b);
		if (num < num2)
		{
			return a;
		}
		return b;
	}

	public static double Largest(double a, double b)
	{
		double num = Math.Abs(a);
		double num2 = Math.Abs(b);
		if (num > num2)
		{
			return a;
		}
		return b;
	}

	public static float CyclicValueDistance(float start, float end, float period)
	{
		return ((end - start) % period + period * 1.5f) % period - period / 2f;
	}

	public static double CyclicValueDistance(double start, double end, double period)
	{
		return ((end - start) % period + period * 1.5) % period - period / 2.0;
	}

	public static double[,] GenGaussKernel(double sigma = 1.0, int size = 5)
	{
		double[,] array = new double[size, size];
		double num = (double)size / 2.0;
		double num2 = 0.0;
		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				array[i, j] = Math.Exp(-0.5 * (Math.Pow(((double)i - num) / sigma, 2.0) + Math.Pow(((double)j - num) / sigma, 2.0))) / (Math.PI * 2.0 * sigma * sigma);
				num2 += array[i, j];
			}
		}
		for (int k = 0; k < size; k++)
		{
			for (int l = 0; l < size; l++)
			{
				array[k, l] /= num2;
			}
		}
		return array;
	}

	public static int[] BiLerpColorMap(IntDataMap2D map, int zoom)
	{
		int innerSize = map.InnerSize;
		int num = innerSize * zoom;
		int[] array = new int[num * num];
		int topLeftPadding = map.TopLeftPadding;
		for (int i = 0; i < innerSize; i++)
		{
			for (int j = 0; j < innerSize; j++)
			{
				int leftTop = map.Data[(j + topLeftPadding) * map.Size + i + topLeftPadding];
				int rightTop = map.Data[(j + topLeftPadding) * map.Size + i + 1 + topLeftPadding];
				int leftBottom = map.Data[(j + 1 + topLeftPadding) * map.Size + i + topLeftPadding];
				int rightBottom = map.Data[(j + 1 + topLeftPadding) * map.Size + i + 1 + topLeftPadding];
				for (int k = 0; k < zoom; k++)
				{
					int num2 = j * zoom + k;
					for (int l = 0; l < zoom; l++)
					{
						int num3 = i * zoom + l;
						array[num2 * num + num3] = BiLerpRgbColor((float)l / (float)zoom, (float)k / (float)zoom, leftTop, rightTop, leftBottom, rightBottom);
					}
				}
			}
		}
		return array;
	}

	public static byte BiLerpByte(float lx, float ly, int byteIndex, int leftTop, int rightTop, int leftBottom, int rightBottom)
	{
		byte left = LerpByte(lx, (byte)(leftTop >> byteIndex * 8), (byte)(rightTop >> byteIndex * 8));
		byte right = LerpByte(lx, (byte)(leftBottom >> byteIndex * 8), (byte)(rightBottom >> byteIndex * 8));
		return LerpByte(ly, left, right);
	}

	public static byte BiSerpByte(float lx, float ly, int byteIndex, int leftTop, int rightTop, int leftBottom, int rightBottom)
	{
		return BiLerpByte(SmoothStep(lx), SmoothStep(ly), byteIndex, leftTop, rightTop, leftBottom, rightBottom);
	}

	public static int BiLerpRgbaColor(float lx, float ly, int leftTop, int rightTop, int leftBottom, int rightBottom)
	{
		return BiLerpRgbColor(lx, ly, leftTop, rightTop, leftBottom, rightBottom) + (BiLerpAndMask(leftTop >> 4, rightTop >> 4, leftBottom >> 4, rightBottom >> 4, lx, ly, 267386880) << 4);
	}

	public static int BiLerpRgbColor(float lx, float ly, int leftTop, int rightTop, int leftBottom, int rightBottom)
	{
		return BiLerpAndMask(leftTop, rightTop, leftBottom, rightBottom, lx, ly, 255) + BiLerpAndMask(leftTop, rightTop, leftBottom, rightBottom, lx, ly, 65280) + BiLerpAndMask(leftTop, rightTop, leftBottom, rightBottom, lx, ly, 16711680);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int BiLerpAndMask(int leftTop, int rightTop, int leftBottom, int rightBottom, float lx, float ly, int mask)
	{
		return (int)Lerp(Lerp(leftTop & mask, rightTop & mask, lx), Lerp(leftBottom & mask, rightBottom & mask, lx), ly) & mask;
	}

	public static int BiSerpRgbColor(float lx, float ly, int leftTop, int rightTop, int leftBottom, int rightBottom)
	{
		return BiLerpRgbColor(SmoothStep(lx), SmoothStep(ly), leftTop, rightTop, leftBottom, rightBottom);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LerpRgbColor(float lx, int left, int right)
	{
		return (int)((1f - lx) * (float)(left & 0xFF) + lx * (float)(right & 0xFF)) + ((int)((1f - lx) * (float)(left & 0xFF00) + lx * (float)(right & 0xFF00)) & 0xFF00) + ((int)((1f - lx) * (float)(left & 0xFF0000) + lx * (float)(right & 0xFF0000)) & 0xFF0000);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LerpRgbaColor(float lx, int left, int right)
	{
		return (int)((1f - lx) * (float)(left & 0xFF) + lx * (float)(right & 0xFF)) + ((int)((1f - lx) * (float)(left & 0xFF00) + lx * (float)(right & 0xFF00)) & 0xFF00) + ((int)((1f - lx) * (float)(left & 0xFF0000) + lx * (float)(right & 0xFF0000)) & 0xFF0000) + ((int)((1f - lx) * (float)((left >> 24) & 0xFF) + lx * (float)((right >> 24) & 0xFF)) << 24);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte LerpByte(float lx, byte left, byte right)
	{
		return (byte)((1f - lx) * (float)(int)left + lx * (float)(int)right);
	}

	public static float BiLerp(float topleft, float topright, float botleft, float botright, float x, float z)
	{
		float num = topleft + (topright - topleft) * x;
		float num2 = botleft + (botright - botleft) * x;
		return num + (num2 - num) * z;
	}

	public static double BiLerp(double topleft, double topright, double botleft, double botright, double x, double z)
	{
		double num = topleft + (topright - topleft) * x;
		double num2 = botleft + (botright - botleft) * x;
		return num + (num2 - num) * z;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Mix(float v0, float v1, float t)
	{
		return v0 + (v1 - v0) * t;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Mix(int v0, int v1, float t)
	{
		return (int)((float)v0 + (float)(v1 - v0) * t);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Lerp(float v0, float v1, float t)
	{
		return v0 + (v1 - v0) * t;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Lerp(double v0, double v1, double t)
	{
		return v0 + (v1 - v0) * t;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Serp(float v0, float v1, float t)
	{
		return v0 + (v1 - v0) * t * t * (3f - 2f * t);
	}

	public static double CPCatmullRomSplineLerp(double t, double[] p, double[] time)
	{
		double num = p[0] * (time[1] - t) / (time[1] - time[0]) + p[1] * (t - time[0]) / (time[1] - time[0]);
		double num2 = p[1] * (time[2] - t) / (time[2] - time[1]) + p[2] * (t - time[1]) / (time[2] - time[1]);
		double num3 = p[2] * (time[3] - t) / (time[3] - time[2]) + p[3] * (t - time[2]) / (time[3] - time[2]);
		double num4 = num * (time[2] - t) / (time[2] - time[0]) + num2 * (t - time[0]) / (time[2] - time[0]);
		double num5 = num2 * (time[3] - t) / (time[3] - time[1]) + num3 * (t - time[1]) / (time[3] - time[1]);
		return num4 * (time[2] - t) / (time[2] - time[1]) + num5 * (t - time[1]) / (time[2] - time[1]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float SmoothStep(float x)
	{
		return x * x * (3f - 2f * x);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double SmoothStep(double x)
	{
		return x * x * (3.0 - 2.0 * x);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Smootherstep(float edge0, float edge1, float x)
	{
		x = Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
		return x * x * x * (x * (x * 6f - 15f) + 10f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Smootherstep(double edge0, double edge1, double x)
	{
		x = Clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
		return x * x * x * (x * (x * 6.0 - 15.0) + 10.0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Smootherstep(double x)
	{
		return x * x * x * (x * (x * 6.0 - 15.0) + 10.0);
	}

	public static float TriangleStep(int val, int left, int right)
	{
		float num = (left + right) / 2;
		float num2 = (right - left) / 2;
		return Math.Max(0f, 1f - Math.Abs((float)val - num) / num2);
	}

	public static float TriangleStep(float val, float left, float right)
	{
		float num = (left + right) / 2f;
		float num2 = (right - left) / 2f;
		return Math.Max(0f, 1f - Math.Abs(val - num) / num2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float TriangleStepFast(int val, int mid, int range)
	{
		return Math.Max(0, 1 - Math.Abs(val - mid) / range);
	}

	public static double Max(double a, double b)
	{
		return Math.Max(a, b);
	}

	public static double Max(params double[] values)
	{
		double num = values[0];
		for (int i = 0; i < values.Length; i++)
		{
			num = Math.Max(num, values[i]);
		}
		return num;
	}

	public static float Max(float a, float b)
	{
		return Math.Max(a, b);
	}

	public static float Max(params float[] values)
	{
		float num = values[0];
		for (int i = 0; i < values.Length; i++)
		{
			num = Math.Max(num, values[i]);
		}
		return num;
	}

	public static int Max(int a, int b)
	{
		return Math.Max(a, b);
	}

	public static int Max(params int[] values)
	{
		int num = values[0];
		for (int i = 0; i < values.Length; i++)
		{
			num = Math.Max(num, values[i]);
		}
		return num;
	}

	public static int Min(int a, int b)
	{
		return Math.Min(a, b);
	}

	public static int Min(params int[] values)
	{
		int num = values[0];
		for (int i = 0; i < values.Length; i++)
		{
			num = Math.Min(num, values[i]);
		}
		return num;
	}

	public static float Min(float a, float b)
	{
		return Math.Min(a, b);
	}

	public static float Min(params float[] values)
	{
		float num = values[0];
		for (int i = 0; i < values.Length; i++)
		{
			num = Math.Min(num, values[i]);
		}
		return num;
	}

	public static float SmoothMin(float a, float b, float smoothingFactor)
	{
		float num = Math.Max(smoothingFactor - Math.Abs(a - b), 0f) / smoothingFactor;
		return Math.Min(a, b) - num * num * smoothingFactor * 0.25f;
	}

	public static float SmoothMax(float a, float b, float smoothingFactor)
	{
		float num = Math.Max(smoothingFactor - Math.Abs(a - b), 0f) / smoothingFactor;
		return Math.Max(a, b) + num * num * smoothingFactor * 0.25f;
	}

	public static double SmoothMin(double a, double b, double smoothingFactor)
	{
		double num = Math.Max(smoothingFactor - Math.Abs(a - b), 0.0) / smoothingFactor;
		return Math.Min(a, b) - num * num * smoothingFactor * 0.25;
	}

	public static double SmoothMax(double a, double b, double smoothingFactor)
	{
		double num = Math.Max(smoothingFactor - Math.Abs(a - b), 0.0) / smoothingFactor;
		return Math.Max(a, b) + num * num * smoothingFactor * 0.25;
	}

	public static uint Crc32(string input)
	{
		return Crc32(Encoding.UTF8.GetBytes(input));
	}

	public static uint Crc32(byte[] input)
	{
		return Crc32Algorithm.Compute(input);
	}

	public unsafe static int DotNetStringHash(string text)
	{
		fixed (char* ptr = text)
		{
			int num = 352654597;
			int num2 = num;
			int* ptr2 = (int*)ptr;
			int num3;
			for (num3 = text.Length; num3 > 2; num3 -= 4)
			{
				num = ((num << 5) + num + (num >> 27)) ^ *ptr2;
				num2 = ((num2 << 5) + num2 + (num2 >> 27)) ^ ptr2[1];
				ptr2 += 2;
			}
			if (num3 > 0)
			{
				num = ((num << 5) + num + (num >> 27)) ^ *ptr2;
			}
			return num + num2 * 1566083941;
		}
	}

	public static string Md5Hash(string input)
	{
		using MD5 mD = MD5.Create();
		byte[] array = mD.ComputeHash(Encoding.UTF8.GetBytes(input));
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.Append(array[i].ToString("x2"));
		}
		return stringBuilder.ToString();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int oaatHashMany(int x)
	{
		for (int num = OaatIterations; num > 0; num--)
		{
			x += x << 10;
			x ^= x >> 6;
			x += x << 3;
			x ^= x >> 11;
			x += x << 15;
		}
		return x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int oaatHashMany(int x, int count)
	{
		for (int num = count; num > 0; num--)
		{
			x += x << 10;
			x ^= x >> 6;
			x += x << 3;
			x ^= x >> 11;
			x += x << 15;
		}
		return x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint oaatHashUMany(uint x)
	{
		for (int num = OaatIterations; num > 0; num--)
		{
			x += x << 10;
			x ^= x >> 6;
			x += x << 3;
			x ^= x >> 11;
			x += x << 15;
		}
		return x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int oaatHash(Vec2i v)
	{
		return oaatHashMany(v.X ^ oaatHashMany(v.Y));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int oaatHash(int x, int y)
	{
		return oaatHashMany(x ^ oaatHashMany(y));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int oaatHash(Vec3i v)
	{
		return oaatHashMany(v.X) ^ oaatHashMany(v.Y) ^ oaatHashMany(v.Z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int oaatHash(int x, int y, int z)
	{
		return oaatHashMany(x) ^ oaatHashMany(y) ^ oaatHashMany(z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint oaatHashU(int x, int y, int z)
	{
		return oaatHashUMany((uint)x) ^ oaatHashUMany((uint)y) ^ oaatHashUMany((uint)z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int oaatHash(Vec4i v)
	{
		return oaatHashMany(v.X) ^ oaatHashMany(v.Y) ^ oaatHashMany(v.Z) ^ oaatHashMany(v.W);
	}

	public static float PrettyBadHash(int x, int y)
	{
		return (float)Mod(((double)x * 12.9898 + (double)y * 78.233) * 43758.5453, 1.0);
	}

	public static int MurmurHash3Mod(int x, int y, int z, int mod)
	{
		return Mod(MurmurHash3(x, y, z), mod);
	}

	public static int MurmurHash3(int x, int y, int z)
	{
		return (int)fmix((uint)(((int)(rotl32((uint)((int)(rotl32((uint)((int)(rotl32(0x90 ^ (rotl32((uint)(x * -862048943), 15) * 461845907), 13) * 5) + -430675100) ^ (rotl32((uint)(y * -862048943), 15) * 461845907), 13) * 5) + -430675100) ^ (rotl32((uint)(z * -862048943), 15) * 461845907), 13) * 5) + -430675100) ^ 3));
	}

	private static uint rotl32(uint x, int r)
	{
		return (x << r) | (x >> 32 - r);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint fmix(uint h)
	{
		h = (h ^ (h >> 16)) * 2246822507u;
		h = (h ^ (h >> 13)) * 3266489909u;
		return h ^ (h >> 16);
	}

	public static double R2Sequence1D(int n)
	{
		double num = 1.618033988749895;
		double num2 = 1.0 / num;
		return (0.5 + num2 * (double)n) % 1.0;
	}

	public static Vec2d R2Sequence2D(int n)
	{
		double num = 1.324717957244746;
		double num2 = 1.0 / num;
		double num3 = 1.0 / (num * num);
		return new Vec2d((0.5 + num2 * (double)n) % 1.0, (0.5 + num3 * (double)n) % 1.0);
	}

	public static Vec3d R2Sequence3D(int n)
	{
		double num = 1.2207440846057596;
		double num2 = 1.0 / num;
		double num3 = 1.0 / (num * num);
		double num4 = 1.0 / (num * num * num);
		return new Vec3d((0.5 + num2 * (double)n) % 1.0, (0.5 + num3 * (double)n) % 1.0, (0.5 + num4 * (double)n) % 1.0);
	}

	public static void FlipVal(ref int x1, ref int x2)
	{
		int num = x1;
		x2 = x1;
		x1 = num;
	}

	public static void FlipVal(ref double x1, ref double x2)
	{
		double num = x1;
		x2 = x1;
		x1 = num;
	}

	public static void Shuffle<T>(Random rand, T[] array)
	{
		int num = array.Length;
		while (num > 1)
		{
			int num2 = rand.Next(num);
			num--;
			T val = array[num];
			array[num] = array[num2];
			array[num2] = val;
		}
	}

	public static void Shuffle<T>(Random rand, List<T> array)
	{
		int num = array.Count;
		while (num > 1)
		{
			int index = rand.Next(num);
			num--;
			T value = array[num];
			array[num] = array[index];
			array[index] = value;
		}
	}

	public static void Shuffle<T>(LCGRandom rand, List<T> array)
	{
		int num = array.Count;
		while (num > 1)
		{
			int index = rand.NextInt(num);
			num--;
			T value = array[num];
			array[num] = array[index];
			array[index] = value;
		}
	}

	public static void BresenHamPlotLine3d(int x0, int y0, int z0, int x1, int y1, int z1, PlotDelegate3D onPlot)
	{
		int num = Math.Abs(x1 - x0);
		int num2 = ((x0 < x1) ? 1 : (-1));
		int num3 = Math.Abs(y1 - y0);
		int num4 = ((y0 < y1) ? 1 : (-1));
		int num5 = Math.Abs(z1 - z0);
		int num6 = ((z0 < z1) ? 1 : (-1));
		int num7 = Max(num, num3, num5);
		int num8 = num7;
		x1 = (y1 = (z1 = num7 / 2));
		while (true)
		{
			onPlot(x0, y0, z0);
			if (num8-- != 0)
			{
				x1 -= num;
				if (x1 < 0)
				{
					x1 += num7;
					x0 += num2;
				}
				y1 -= num3;
				if (y1 < 0)
				{
					y1 += num7;
					y0 += num4;
				}
				z1 -= num5;
				if (z1 < 0)
				{
					z1 += num7;
					z0 += num6;
				}
				continue;
			}
			break;
		}
	}

	public static void BresenHamPlotLine3d(int x0, int y0, int z0, int x1, int y1, int z1, PlotDelegate3DBlockPos onPlot)
	{
		int num = Math.Abs(x1 - x0);
		int num2 = ((x0 < x1) ? 1 : (-1));
		int num3 = Math.Abs(y1 - y0);
		int num4 = ((y0 < y1) ? 1 : (-1));
		int num5 = Math.Abs(z1 - z0);
		int num6 = ((z0 < z1) ? 1 : (-1));
		int num7 = Max(num, num3, num5);
		int num8 = num7;
		x1 = (y1 = (z1 = num7 / 2));
		BlockPos blockPos = new BlockPos();
		while (true)
		{
			blockPos.Set(x0, y0, z0);
			onPlot(blockPos);
			if (num8-- != 0)
			{
				x1 -= num;
				if (x1 < 0)
				{
					x1 += num7;
					x0 += num2;
				}
				y1 -= num3;
				if (y1 < 0)
				{
					y1 += num7;
					y0 += num4;
				}
				z1 -= num5;
				if (z1 < 0)
				{
					z1 += num7;
					z0 += num6;
				}
				continue;
			}
			break;
		}
	}

	public static void BresenHamPlotLine2d(int x0, int y0, int x1, int y1, PlotDelegate2D onPlot)
	{
		int num = Math.Abs(x1 - x0);
		int num2 = ((x0 < x1) ? 1 : (-1));
		int num3 = -Math.Abs(y1 - y0);
		int num4 = ((y0 < y1) ? 1 : (-1));
		int num5 = num + num3;
		while (true)
		{
			onPlot(x0, y0);
			if (x0 != x1 || y0 != y1)
			{
				int num6 = 2 * num5;
				if (num6 >= num3)
				{
					num5 += num3;
					x0 += num2;
				}
				if (num6 <= num)
				{
					num5 += num;
					y0 += num4;
				}
				continue;
			}
			break;
		}
	}

	public static Vec3f ToEulerAngles(Vec4f q)
	{
		Vec3d vec3d = ToEulerAngles(new Vec4d(q.X, q.Y, q.Z, q.W));
		return new Vec3f((float)vec3d.X, (float)vec3d.Y, (float)vec3d.Z);
	}

	public static Vec3d ToEulerAngles(Vec4d q)
	{
		Vec3d vec3d = new Vec3d();
		double y = 2.0 * (q.W * q.X + q.Y * q.Z);
		double x = 1.0 - 2.0 * (q.X * q.X + q.Y * q.Y);
		vec3d.X = Math.Atan2(y, x);
		double d = 2.0 * (q.W * q.Y - q.Z * q.X);
		vec3d.Y = Math.Asin(d);
		double y2 = 2.0 * (q.W * q.Z + q.X * q.Y);
		double x2 = 1.0 - 2.0 * (q.Y * q.Y + q.Z * q.Z);
		vec3d.Z = Math.Atan2(y2, x2);
		return vec3d;
	}

	public static int IntFromBools(int[] intBools)
	{
		int num = 0;
		int num2 = intBools.Length;
		while (num2-- != 0)
		{
			if (intBools[num2] != 0)
			{
				num += 1 << num2;
			}
		}
		return num;
	}

	public static int IntFromBools(bool[] bools)
	{
		int num = 0;
		int num2 = bools.Length;
		while (num2-- != 0)
		{
			if (bools[num2])
			{
				num += 1 << num2;
			}
		}
		return num;
	}

	public static void BoolsFromInt(bool[] bools, int v)
	{
		int num = bools.Length;
		while (num-- != 0)
		{
			bools[num] = (v & (1 << num)) != 0;
		}
	}

	public static T Map<T>(T value, T fromMin, T fromMax, T toMin, T toMax) where T : INumber<T>
	{
		return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
	}
}
