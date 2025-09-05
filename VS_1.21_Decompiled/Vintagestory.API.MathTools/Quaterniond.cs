using System;

namespace Vintagestory.API.MathTools;

public class Quaterniond
{
	public static double[] Create()
	{
		return new double[4] { 0.0, 0.0, 0.0, 1.0 };
	}

	public static double[] RotationTo(double[] output, double[] a, double[] b)
	{
		double[] array = Vec3Utilsd.Create();
		double[] a2 = Vec3Utilsd.FromValues(1.0, 0.0, 0.0);
		double[] a3 = Vec3Utilsd.FromValues(0.0, 1.0, 0.0);
		double num = Vec3Utilsd.Dot(a, b);
		double num2 = 999999.0;
		num2 /= 1000000.0;
		double num3 = 1.0;
		num3 /= 1000000.0;
		if (num < 0.0 - num2)
		{
			Vec3Utilsd.Cross(array, a2, a);
			if (Vec3Utilsd.Length_(array) < num3)
			{
				Vec3Utilsd.Cross(array, a3, a);
			}
			Vec3Utilsd.Normalize(array, array);
			SetAxisAngle(output, array, 3.1415927410125732);
			return output;
		}
		if (num > num2)
		{
			output[0] = 0.0;
			output[1] = 0.0;
			output[2] = 0.0;
			output[3] = 1.0;
			return output;
		}
		Vec3Utilsd.Cross(array, a, b);
		output[0] = array[0];
		output[1] = array[1];
		output[2] = array[2];
		output[3] = 1.0 + num;
		return Normalize(output, output);
	}

	public static double[] SetAxes(double[] output, double[] view, double[] right, double[] up)
	{
		double[] array = Mat3d.Create();
		array[0] = right[0];
		array[3] = right[1];
		array[6] = right[2];
		array[1] = up[0];
		array[4] = up[1];
		array[7] = up[2];
		array[2] = view[0];
		array[5] = view[1];
		array[8] = view[2];
		return Normalize(output, FromMat3(output, array));
	}

	public static double[] CloneIt(double[] a)
	{
		return QVec4d.CloneIt(a);
	}

	public static double[] FromValues(double x, double y, double z, double w)
	{
		return QVec4d.FromValues(x, y, z, w);
	}

	public static double[] Copy(double[] output, double[] a)
	{
		return QVec4d.Copy(output, a);
	}

	public static double[] Set(double[] output, double x, double y, double z, double w)
	{
		return QVec4d.Set(output, x, y, z, w);
	}

	public static double[] Identity_(double[] output)
	{
		output[0] = 0.0;
		output[1] = 0.0;
		output[2] = 0.0;
		output[3] = 1.0;
		return output;
	}

	public static double[] SetAxisAngle(double[] output, double[] axis, double rad)
	{
		rad /= 2.0;
		double num = GameMath.Sin(rad);
		output[0] = num * axis[0];
		output[1] = num * axis[1];
		output[2] = num * axis[2];
		output[3] = GameMath.Cos(rad);
		return output;
	}

	public static double[] Add(double[] output, double[] a, double[] b)
	{
		return QVec4d.Add(output, a, b);
	}

	public static double[] Multiply(double[] output, double[] a, double[] b)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = b[0];
		double num6 = b[1];
		double num7 = b[2];
		double num8 = b[3];
		output[0] = num * num8 + num4 * num5 + num2 * num7 - num3 * num6;
		output[1] = num2 * num8 + num4 * num6 + num3 * num5 - num * num7;
		output[2] = num3 * num8 + num4 * num7 + num * num6 - num2 * num5;
		output[3] = num4 * num8 - num * num5 - num2 * num6 - num3 * num7;
		return output;
	}

	public static double[] Scale(double[] output, double[] a, double b)
	{
		return QVec4d.Scale(output, a, b);
	}

	public static double[] RotateX(double[] output, double[] a, double rad)
	{
		rad /= 2.0;
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = GameMath.Sin(rad);
		double num6 = GameMath.Cos(rad);
		output[0] = num * num6 + num4 * num5;
		output[1] = num2 * num6 + num3 * num5;
		output[2] = num3 * num6 - num2 * num5;
		output[3] = num4 * num6 - num * num5;
		return output;
	}

	public static double[] RotateY(double[] output, double[] a, double rad)
	{
		rad /= 2.0;
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = GameMath.Sin(rad);
		double num6 = GameMath.Cos(rad);
		output[0] = num * num6 - num3 * num5;
		output[1] = num2 * num6 + num4 * num5;
		output[2] = num3 * num6 + num * num5;
		output[3] = num4 * num6 - num2 * num5;
		return output;
	}

	public static double[] RotateZ(double[] output, double[] a, double rad)
	{
		rad /= 2.0;
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = GameMath.Sin(rad);
		double num6 = GameMath.Cos(rad);
		output[0] = num * num6 + num2 * num5;
		output[1] = num2 * num6 - num * num5;
		output[2] = num3 * num6 + num4 * num5;
		output[3] = num4 * num6 - num3 * num5;
		return output;
	}

	public static double[] CalculateW(double[] output, double[] a)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		output[0] = num;
		output[1] = num2;
		output[2] = num3;
		double num4 = 1.0;
		output[3] = 0f - GameMath.Sqrt(Math.Abs(num4 - num * num - num2 * num2 - num3 * num3));
		return output;
	}

	public static double Dot(double[] a, double[] b)
	{
		return QVec4d.Dot(a, b);
	}

	public static float[] ToEulerAngles(double[] quat)
	{
		float[] array = new float[3];
		double y = 2.0 * (quat[3] * quat[0] + quat[1] * quat[2]);
		double x = 1.0 - 2.0 * (quat[0] * quat[0] + quat[1] * quat[1]);
		array[2] = (float)Math.Atan2(y, x);
		double num = 2.0 * (quat[3] * quat[1] - quat[2] * quat[0]);
		if (Math.Abs(num) >= 1.0)
		{
			array[1] = (float)Math.PI / 2f * (float)Math.Sign(num);
		}
		else
		{
			array[1] = (float)Math.Asin(num);
		}
		double y2 = 2.0 * (quat[3] * quat[2] + quat[0] * quat[1]);
		double x2 = 1.0 - 2.0 * (quat[1] * quat[1] + quat[2] * quat[2]);
		array[0] = (float)Math.Atan2(y2, x2);
		return array;
	}

	public static double[] Lerp(double[] output, double[] a, double[] b, double t)
	{
		return QVec4d.Lerp(output, a, b, t);
	}

	public static double[] Slerp(double[] output, double[] a, double[] b, double t)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = b[0];
		double num6 = b[1];
		double num7 = b[2];
		double num8 = b[3];
		double num9 = num * num5 + num2 * num6 + num3 * num7 + num4 * num8;
		if (num9 < 0.0)
		{
			num9 = 0.0 - num9;
			num5 = 0.0 - num5;
			num6 = 0.0 - num6;
			num7 = 0.0 - num7;
			num8 = 0.0 - num8;
		}
		double num10 = 1.0;
		double num11 = num10 / 1000000.0;
		double num14;
		double num15;
		if (num10 - num9 > num11)
		{
			double num12 = GameMath.Acos(num9);
			double num13 = GameMath.Sin(num12);
			num14 = GameMath.Sin((num10 - t) * num12) / num13;
			num15 = GameMath.Sin(t * num12) / num13;
		}
		else
		{
			num14 = num10 - t;
			num15 = t;
		}
		output[0] = num14 * num + num15 * num5;
		output[1] = num14 * num2 + num15 * num6;
		output[2] = num14 * num3 + num15 * num7;
		output[3] = num14 * num4 + num15 * num8;
		return output;
	}

	public double[] Invert(double[] output, double[] a)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = num * num + num2 * num2 + num3 * num3 + num4 * num4;
		double num6 = 1.0;
		double num7 = ((num5 != 0.0) ? (num6 / num5) : 0.0);
		output[0] = (0.0 - num) * num7;
		output[1] = (0.0 - num2) * num7;
		output[2] = (0.0 - num3) * num7;
		output[3] = num4 * num7;
		return output;
	}

	public double[] Conjugate(double[] output, double[] a)
	{
		output[0] = 0.0 - a[0];
		output[1] = 0.0 - a[1];
		output[2] = 0.0 - a[2];
		output[3] = a[3];
		return output;
	}

	public static double Length_(double[] a)
	{
		return QVec4d.Length_(a);
	}

	public static double SquaredLength(double[] a)
	{
		return QVec4d.SquaredLength(a);
	}

	public static double[] Normalize(double[] output, double[] a)
	{
		return QVec4d.Normalize(output, a);
	}

	public static double[] FromMat3(double[] output, double[] m)
	{
		double num = m[0] + m[4] + m[8];
		double num2 = 0.0;
		double num3 = 1.0;
		double num4 = num3 / 2.0;
		if (num > num2)
		{
			double num5 = GameMath.Sqrt(num + num3);
			output[3] = num4 * num5;
			num5 = num4 / num5;
			output[0] = (m[7] - m[5]) * num5;
			output[1] = (m[2] - m[6]) * num5;
			output[2] = (m[3] - m[1]) * num5;
		}
		else
		{
			int num6 = 0;
			if (m[4] > m[0])
			{
				num6 = 1;
			}
			if (m[8] > m[num6 * 3 + num6])
			{
				num6 = 2;
			}
			int num7 = (num6 + 1) % 3;
			int num8 = (num6 + 2) % 3;
			double num5 = GameMath.Sqrt(m[num6 * 3 + num6] - m[num7 * 3 + num7] - m[num8 * 3 + num8] + num3);
			output[num6] = num4 * num5;
			num5 = num4 / num5;
			output[3] = (m[num8 * 3 + num7] - m[num7 * 3 + num8]) * num5;
			output[num7] = (m[num7 * 3 + num6] + m[num6 * 3 + num7]) * num5;
			output[num8] = (m[num8 * 3 + num6] + m[num6 * 3 + num8]) * num5;
		}
		return output;
	}
}
