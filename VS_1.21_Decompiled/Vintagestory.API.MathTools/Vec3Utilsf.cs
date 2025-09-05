using System;

namespace Vintagestory.API.MathTools;

public class Vec3Utilsf
{
	public static float[] Create()
	{
		return new float[3] { 0f, 0f, 0f };
	}

	public static float[] CloneIt(float[] a)
	{
		return new float[3]
		{
			a[0],
			a[1],
			a[2]
		};
	}

	public static float[] FromValues(float x, float y, float z)
	{
		return new float[3] { x, y, z };
	}

	public static float[] Copy(float[] output, float[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		return output;
	}

	public static float[] Set(float[] output, float x, float y, float z)
	{
		output[0] = x;
		output[1] = y;
		output[2] = z;
		return output;
	}

	public static float[] Add(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] + b[0];
		output[1] = a[1] + b[1];
		output[2] = a[2] + b[2];
		return output;
	}

	public static float[] Substract(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] - b[0];
		output[1] = a[1] - b[1];
		output[2] = a[2] - b[2];
		return output;
	}

	public static float[] Multiply(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] * b[0];
		output[1] = a[1] * b[1];
		output[2] = a[2] * b[2];
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
		return output;
	}

	public static float[] Min(float[] output, float[] a, float[] b)
	{
		output[0] = Math.Min(a[0], b[0]);
		output[1] = Math.Min(a[1], b[1]);
		output[2] = Math.Min(a[2], b[2]);
		return output;
	}

	public static float[] Max(float[] output, float[] a, float[] b)
	{
		output[0] = Math.Max(a[0], b[0]);
		output[1] = Math.Max(a[1], b[1]);
		output[2] = Math.Max(a[2], b[2]);
		return output;
	}

	public static float[] Scale(float[] output, float[] a, float b)
	{
		output[0] = a[0] * b;
		output[1] = a[1] * b;
		output[2] = a[2] * b;
		return output;
	}

	public static float[] ScaleAndAdd(float[] output, float[] a, float[] b, float scale)
	{
		output[0] = a[0] + b[0] * scale;
		output[1] = a[1] + b[1] * scale;
		output[2] = a[2] + b[2] * scale;
		return output;
	}

	public static float Distance(float[] a, float[] b)
	{
		float num = b[0] - a[0];
		float num2 = b[1] - a[1];
		float num3 = b[2] - a[2];
		return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3);
	}

	public static float SquaredDistance(float[] a, float[] b)
	{
		float num = b[0] - a[0];
		float num2 = b[1] - a[1];
		float num3 = b[2] - a[2];
		return num * num + num2 * num2 + num3 * num3;
	}

	public static float Length_(float[] a)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3);
	}

	public static float SquaredLength(float[] a)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		return num * num + num2 * num2 + num3 * num3;
	}

	public static float[] Negate(float[] output, float[] a)
	{
		output[0] = 0f - a[0];
		output[1] = 0f - a[1];
		output[2] = 0f - a[2];
		return output;
	}

	public static float[] Normalize(float[] output, float[] a)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = num * num + num2 * num2 + num3 * num3;
		if (num4 > 0f)
		{
			num4 = 1f / GameMath.Sqrt(num4);
			output[0] = a[0] * num4;
			output[1] = a[1] * num4;
			output[2] = a[2] * num4;
		}
		return output;
	}

	public static float Dot(float[] a, float[] b)
	{
		return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
	}

	public static float[] Cross(float[] output, float[] a, float[] b)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = b[0];
		float num5 = b[1];
		float num6 = b[2];
		output[0] = num2 * num6 - num3 * num5;
		output[1] = num3 * num4 - num * num6;
		output[2] = num * num5 - num2 * num4;
		return output;
	}

	public static float[] Lerp(float[] output, float[] a, float[] b, float t)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		output[0] = num + t * (b[0] - num);
		output[1] = num2 + t * (b[1] - num2);
		output[2] = num3 + t * (b[2] - num3);
		return output;
	}

	public static float[] TransformMat4(float[] output, float[] a, float[] m)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		output[0] = m[0] * num + m[4] * num2 + m[8] * num3 + m[12];
		output[1] = m[1] * num + m[5] * num2 + m[9] * num3 + m[13];
		output[2] = m[2] * num + m[6] * num2 + m[10] * num3 + m[14];
		return output;
	}

	public static float[] TransformMat3(float[] output, float[] a, float[] m)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		output[0] = num * m[0] + num2 * m[3] + num3 * m[6];
		output[1] = num * m[1] + num2 * m[4] + num3 * m[7];
		output[2] = num * m[2] + num2 * m[5] + num3 * m[8];
		return output;
	}

	public static float[] TransformQuat(float[] output, float[] a, float[] q)
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
