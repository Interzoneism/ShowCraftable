using System;

namespace Vintagestory.API.MathTools;

internal class QVec4d
{
	public static double[] Create()
	{
		return new double[4] { 0.0, 0.0, 0.0, 0.0 };
	}

	public static double[] CloneIt(double[] a)
	{
		return new double[4]
		{
			a[0],
			a[1],
			a[2],
			a[3]
		};
	}

	public static double[] FromValues(double x, double y, double z, double w)
	{
		return new double[4] { x, y, z, w };
	}

	public static double[] Copy(double[] output, double[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		output[3] = a[3];
		return output;
	}

	public static double[] Set(double[] output, double x, double y, double z, double w)
	{
		output[0] = x;
		output[1] = y;
		output[2] = z;
		output[3] = w;
		return output;
	}

	public static double[] Add(double[] output, double[] a, double[] b)
	{
		output[0] = a[0] + b[0];
		output[1] = a[1] + b[1];
		output[2] = a[2] + b[2];
		output[3] = a[3] + b[3];
		return output;
	}

	public static double[] Subtract(double[] output, double[] a, double[] b)
	{
		output[0] = a[0] - b[0];
		output[1] = a[1] - b[1];
		output[2] = a[2] - b[2];
		output[3] = a[3] - b[3];
		return output;
	}

	public static double[] Multiply(double[] output, double[] a, double[] b)
	{
		output[0] = a[0] * b[0];
		output[1] = a[1] * b[1];
		output[2] = a[2] * b[2];
		output[3] = a[3] * b[3];
		return output;
	}

	public static double[] Divide(double[] output, double[] a, double[] b)
	{
		output[0] = a[0] / b[0];
		output[1] = a[1] / b[1];
		output[2] = a[2] / b[2];
		output[3] = a[3] / b[3];
		return output;
	}

	public static double[] Min(double[] output, double[] a, double[] b)
	{
		output[0] = Math.Min(a[0], b[0]);
		output[1] = Math.Min(a[1], b[1]);
		output[2] = Math.Min(a[2], b[2]);
		output[3] = Math.Min(a[3], b[3]);
		return output;
	}

	public static double[] Max(double[] output, double[] a, double[] b)
	{
		output[0] = Math.Max(a[0], b[0]);
		output[1] = Math.Max(a[1], b[1]);
		output[2] = Math.Max(a[2], b[2]);
		output[3] = Math.Max(a[3], b[3]);
		return output;
	}

	public static double[] Scale(double[] output, double[] a, double b)
	{
		output[0] = a[0] * b;
		output[1] = a[1] * b;
		output[2] = a[2] * b;
		output[3] = a[3] * b;
		return output;
	}

	public static double[] ScaleAndAdd(double[] output, double[] a, double[] b, double scale)
	{
		output[0] = a[0] + b[0] * scale;
		output[1] = a[1] + b[1] * scale;
		output[2] = a[2] + b[2] * scale;
		output[3] = a[3] + b[3] * scale;
		return output;
	}

	public static double Distance(double[] a, double[] b)
	{
		double num = b[0] - a[0];
		double num2 = b[1] - a[1];
		double num3 = b[2] - a[2];
		double num4 = b[3] - a[3];
		return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3 + num4 * num4);
	}

	public static double SquaredDistance(double[] a, double[] b)
	{
		double num = b[0] - a[0];
		double num2 = b[1] - a[1];
		double num3 = b[2] - a[2];
		double num4 = b[3] - a[3];
		return num * num + num2 * num2 + num3 * num3 + num4 * num4;
	}

	public static double Length_(double[] a)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3 + num4 * num4);
	}

	public static double SquaredLength(double[] a)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		return num * num + num2 * num2 + num3 * num3 + num4 * num4;
	}

	public static double[] Negate(double[] output, double[] a)
	{
		output[0] = 0.0 - a[0];
		output[1] = 0.0 - a[1];
		output[2] = 0.0 - a[2];
		output[3] = 0.0 - a[3];
		return output;
	}

	public static double[] Normalize(double[] output, double[] a)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = num * num + num2 * num2 + num3 * num3 + num4 * num4;
		if (num5 > 0.0)
		{
			num5 = 1.0 / (double)GameMath.Sqrt(num5);
			output[0] = a[0] * num5;
			output[1] = a[1] * num5;
			output[2] = a[2] * num5;
			output[3] = a[3] * num5;
		}
		return output;
	}

	public static double Dot(double[] a, double[] b)
	{
		return a[0] * b[0] + a[1] * b[1] + a[2] * b[2] + a[3] * b[3];
	}

	public static double[] Lerp(double[] output, double[] a, double[] b, double t)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		output[0] = num + t * (b[0] - num);
		output[1] = num2 + t * (b[1] - num2);
		output[2] = num3 + t * (b[2] - num3);
		output[3] = num4 + t * (b[3] - num4);
		return output;
	}

	public static double[] TransformMat4(double[] output, double[] a, double[] m)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		output[0] = m[0] * num + m[4] * num2 + m[8] * num3 + m[12] * num4;
		output[1] = m[1] * num + m[5] * num2 + m[9] * num3 + m[13] * num4;
		output[2] = m[2] * num + m[6] * num2 + m[10] * num3 + m[14] * num4;
		output[3] = m[3] * num + m[7] * num2 + m[11] * num3 + m[15] * num4;
		return output;
	}

	public static double[] transformQuat(double[] output, double[] a, double[] q)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = q[0];
		double num5 = q[1];
		double num6 = q[2];
		double num7 = q[3];
		double num8 = num7 * num + num5 * num3 - num6 * num2;
		double num9 = num7 * num2 + num6 * num - num4 * num3;
		double num10 = num7 * num3 + num4 * num2 - num5 * num;
		double num11 = (0.0 - num4) * num - num5 * num2 - num6 * num3;
		output[0] = num8 * num7 + num11 * (0.0 - num4) + num9 * (0.0 - num6) - num10 * (0.0 - num5);
		output[1] = num9 * num7 + num11 * (0.0 - num5) + num10 * (0.0 - num4) - num8 * (0.0 - num6);
		output[2] = num10 * num7 + num11 * (0.0 - num6) + num8 * (0.0 - num5) - num9 * (0.0 - num4);
		return output;
	}
}
