using System;

namespace Vintagestory.API.MathTools;

internal class QVec4f
{
	public static float[] Create()
	{
		return new float[4] { 0f, 0f, 0f, 0f };
	}

	public static float[] CloneIt(float[] a)
	{
		return new float[4]
		{
			a[0],
			a[1],
			a[2],
			a[3]
		};
	}

	public static float[] FromValues(float x, float y, float z, float w)
	{
		return new float[4] { x, y, z, w };
	}

	public static float[] Copy(float[] output, float[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		output[3] = a[3];
		return output;
	}

	public static float[] Set(float[] output, float x, float y, float z, float w)
	{
		output[0] = x;
		output[1] = y;
		output[2] = z;
		output[3] = w;
		return output;
	}

	public static float[] Add(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] + b[0];
		output[1] = a[1] + b[1];
		output[2] = a[2] + b[2];
		output[3] = a[3] + b[3];
		return output;
	}

	public static float[] Subtract(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] - b[0];
		output[1] = a[1] - b[1];
		output[2] = a[2] - b[2];
		output[3] = a[3] - b[3];
		return output;
	}

	public static float[] Sub(float[] output, float[] a, float[] b)
	{
		return Subtract(output, a, b);
	}

	public static float[] Multiply(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] * b[0];
		output[1] = a[1] * b[1];
		output[2] = a[2] * b[2];
		output[3] = a[3] * b[3];
		return output;
	}

	public static float[] Mul(float[] output, float[] a, float[] b)
	{
		return Multiply(output, a, b);
	}

	public static float[] Divide(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] / b[0];
		output[1] = a[1] / b[1];
		output[2] = a[2] / b[2];
		output[3] = a[3] / b[3];
		return output;
	}

	public static float[] Div(float[] output, float[] a, float[] b)
	{
		return Divide(output, a, b);
	}

	public static float[] Min(float[] output, float[] a, float[] b)
	{
		output[0] = Math.Min(a[0], b[0]);
		output[1] = Math.Min(a[1], b[1]);
		output[2] = Math.Min(a[2], b[2]);
		output[3] = Math.Min(a[3], b[3]);
		return output;
	}

	public static float[] Max(float[] output, float[] a, float[] b)
	{
		output[0] = Math.Max(a[0], b[0]);
		output[1] = Math.Max(a[1], b[1]);
		output[2] = Math.Max(a[2], b[2]);
		output[3] = Math.Max(a[3], b[3]);
		return output;
	}

	public static float[] Scale(float[] output, float[] a, float b)
	{
		output[0] = a[0] * b;
		output[1] = a[1] * b;
		output[2] = a[2] * b;
		output[3] = a[3] * b;
		return output;
	}

	public static float[] ScaleAndAdd(float[] output, float[] a, float[] b, float scale)
	{
		output[0] = a[0] + b[0] * scale;
		output[1] = a[1] + b[1] * scale;
		output[2] = a[2] + b[2] * scale;
		output[3] = a[3] + b[3] * scale;
		return output;
	}

	public static float Distance(float[] a, float[] b)
	{
		float num = b[0] - a[0];
		float num2 = b[1] - a[1];
		float num3 = b[2] - a[2];
		float num4 = b[3] - a[3];
		return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3 + num4 * num4);
	}

	public static float Dist(float[] a, float[] b)
	{
		return Distance(a, b);
	}

	public static float SquaredDistance(float[] a, float[] b)
	{
		float num = b[0] - a[0];
		float num2 = b[1] - a[1];
		float num3 = b[2] - a[2];
		float num4 = b[3] - a[3];
		return num * num + num2 * num2 + num3 * num3 + num4 * num4;
	}

	public static float SqrDist(float[] a, float[] b)
	{
		return SquaredDistance(a, b);
	}

	public static float Length_(float[] a)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3 + num4 * num4);
	}

	public static float Len(float[] a)
	{
		return Length_(a);
	}

	public static float SquaredLength(float[] a)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		return num * num + num2 * num2 + num3 * num3 + num4 * num4;
	}

	public static float SqrLen(float[] a)
	{
		return SquaredLength(a);
	}

	public static float[] Negate(float[] output, float[] a)
	{
		output[0] = 0f - a[0];
		output[1] = 0f - a[1];
		output[2] = 0f - a[2];
		output[3] = 0f - a[3];
		return output;
	}

	public static float[] Normalize(float[] output, float[] a)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		float num5 = num * num + num2 * num2 + num3 * num3 + num4 * num4;
		if (num5 > 0f)
		{
			num5 = 1f / GameMath.Sqrt(num5);
			output[0] = a[0] * num5;
			output[1] = a[1] * num5;
			output[2] = a[2] * num5;
			output[3] = a[3] * num5;
		}
		return output;
	}

	public static float Dot(float[] a, float[] b)
	{
		return a[0] * b[0] + a[1] * b[1] + a[2] * b[2] + a[3] * b[3];
	}

	public static float[] Lerp(float[] output, float[] a, float[] b, float t)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		output[0] = num + t * (b[0] - num);
		output[1] = num2 + t * (b[1] - num2);
		output[2] = num3 + t * (b[2] - num3);
		output[3] = num4 + t * (b[3] - num4);
		return output;
	}

	public static float[] TransformMat4(float[] output, float[] a, float[] m)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		output[0] = m[0] * num + m[4] * num2 + m[8] * num3 + m[12] * num4;
		output[1] = m[1] * num + m[5] * num2 + m[9] * num3 + m[13] * num4;
		output[2] = m[2] * num + m[6] * num2 + m[10] * num3 + m[14] * num4;
		output[3] = m[3] * num + m[7] * num2 + m[11] * num3 + m[15] * num4;
		return output;
	}

	public static float[] transformQuat(float[] output, float[] a, float[] q)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = q[0];
		float num5 = q[1];
		float num6 = q[2];
		float num7 = q[3];
		float num8 = num7 * num + num5 * num3 - num6 * num2;
		float num9 = num7 * num2 + num6 * num - num4 * num3;
		float num10 = num7 * num3 + num4 * num2 - num5 * num;
		float num11 = (0f - num4) * num - num5 * num2 - num6 * num3;
		output[0] = num8 * num7 + num11 * (0f - num4) + num9 * (0f - num6) - num10 * (0f - num5);
		output[1] = num9 * num7 + num11 * (0f - num5) + num10 * (0f - num4) - num8 * (0f - num6);
		output[2] = num10 * num7 + num11 * (0f - num6) + num8 * (0f - num5) - num9 * (0f - num4);
		return output;
	}
}
