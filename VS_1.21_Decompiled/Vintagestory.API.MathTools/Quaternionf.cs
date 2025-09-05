using System;

namespace Vintagestory.API.MathTools;

public class Quaternionf
{
	public static float[] Create()
	{
		return new float[4] { 0f, 0f, 0f, 1f };
	}

	public static float[] RotationTo(float[] output, float[] a, float[] b)
	{
		float[] array = Vec3Utilsf.Create();
		float[] a2 = Vec3Utilsf.FromValues(1f, 0f, 0f);
		float[] a3 = Vec3Utilsf.FromValues(0f, 1f, 0f);
		float num = Vec3Utilsf.Dot(a, b);
		float num2 = 999999f;
		num2 /= 1000000f;
		float num3 = 1f;
		num3 /= 1000000f;
		if (num < 0f - num2)
		{
			Vec3Utilsf.Cross(array, a2, a);
			if (Vec3Utilsf.Length_(array) < num3)
			{
				Vec3Utilsf.Cross(array, a3, a);
			}
			Vec3Utilsf.Normalize(array, array);
			SetAxisAngle(output, array, (float)Math.PI);
			return output;
		}
		if (num > num2)
		{
			output[0] = 0f;
			output[1] = 0f;
			output[2] = 0f;
			output[3] = 1f;
			return output;
		}
		Vec3Utilsf.Cross(array, a, b);
		output[0] = array[0];
		output[1] = array[1];
		output[2] = array[2];
		output[3] = 1f + num;
		return Normalize(output, output);
	}

	public static float[] SetAxes(float[] output, float[] view, float[] right, float[] up)
	{
		float[] array = Mat3f.Create();
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

	public static float[] CloneIt(float[] a)
	{
		return QVec4f.CloneIt(a);
	}

	public static float[] FromValues(float x, float y, float z, float w)
	{
		return QVec4f.FromValues(x, y, z, w);
	}

	public static float[] Copy(float[] output, float[] a)
	{
		return QVec4f.Copy(output, a);
	}

	public static float[] Set(float[] output, float x, float y, float z, float w)
	{
		return QVec4f.Set(output, x, y, z, w);
	}

	public static float[] Identity_(float[] output)
	{
		output[0] = 0f;
		output[1] = 0f;
		output[2] = 0f;
		output[3] = 1f;
		return output;
	}

	public static float[] SetAxisAngle(float[] output, float[] axis, float rad)
	{
		rad /= 2f;
		float num = GameMath.Sin(rad);
		output[0] = num * axis[0];
		output[1] = num * axis[1];
		output[2] = num * axis[2];
		output[3] = GameMath.Cos(rad);
		return output;
	}

	public static float[] Add(float[] output, float[] a, float[] b)
	{
		return QVec4f.Add(output, a, b);
	}

	public static float[] Multiply(float[] output, float[] a, float[] b)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		float num5 = b[0];
		float num6 = b[1];
		float num7 = b[2];
		float num8 = b[3];
		output[0] = num * num8 + num4 * num5 + num2 * num7 - num3 * num6;
		output[1] = num2 * num8 + num4 * num6 + num3 * num5 - num * num7;
		output[2] = num3 * num8 + num4 * num7 + num * num6 - num2 * num5;
		output[3] = num4 * num8 - num * num5 - num2 * num6 - num3 * num7;
		return output;
	}

	public static float[] Mul(float[] output, float[] a, float[] b)
	{
		return Multiply(output, a, b);
	}

	public static float[] Scale(float[] output, float[] a, float b)
	{
		return QVec4f.Scale(output, a, b);
	}

	public static float[] RotateX(float[] output, float[] a, float rad)
	{
		rad /= 2f;
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		float num5 = GameMath.Sin(rad);
		float num6 = GameMath.Cos(rad);
		output[0] = num * num6 + num4 * num5;
		output[1] = num2 * num6 + num3 * num5;
		output[2] = num3 * num6 - num2 * num5;
		output[3] = num4 * num6 - num * num5;
		return output;
	}

	public static float[] RotateY(float[] output, float[] a, float rad)
	{
		rad /= 2f;
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		float num5 = GameMath.Sin(rad);
		float num6 = GameMath.Cos(rad);
		output[0] = num * num6 - num3 * num5;
		output[1] = num2 * num6 + num4 * num5;
		output[2] = num3 * num6 + num * num5;
		output[3] = num4 * num6 - num2 * num5;
		return output;
	}

	public static float[] RotateZ(float[] output, float[] a, float rad)
	{
		rad /= 2f;
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		float num5 = GameMath.Sin(rad);
		float num6 = GameMath.Cos(rad);
		output[0] = num * num6 + num2 * num5;
		output[1] = num2 * num6 - num * num5;
		output[2] = num3 * num6 + num4 * num5;
		output[3] = num4 * num6 - num3 * num5;
		return output;
	}

	public static float[] CalculateW(float[] output, float[] a)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		output[0] = num;
		output[1] = num2;
		output[2] = num3;
		float num4 = 1f;
		output[3] = 0f - GameMath.Sqrt(Math.Abs(num4 - num * num - num2 * num2 - num3 * num3));
		return output;
	}

	public static float Dot(float[] a, float[] b)
	{
		return QVec4f.Dot(a, b);
	}

	public static float[] ToEulerAngles(float[] quat)
	{
		float[] array = new float[3];
		float num = 2f * (quat[3] * quat[0] + quat[1] * quat[2]);
		float num2 = 1f - 2f * (quat[0] * quat[0] + quat[1] * quat[1]);
		array[2] = (float)Math.Atan2(num, num2);
		float num3 = 2f * (quat[3] * quat[1] - quat[2] * quat[0]);
		if (Math.Abs(num3) >= 1f)
		{
			array[1] = (float)Math.PI / 2f * (float)Math.Sign(num3);
		}
		else
		{
			array[1] = (float)Math.Asin(num3);
		}
		float num4 = 2f * (quat[3] * quat[2] + quat[0] * quat[1]);
		float num5 = 1f - 2f * (quat[1] * quat[1] + quat[2] * quat[2]);
		array[0] = (float)Math.Atan2(num4, num5);
		return array;
	}

	public static float[] Lerp(float[] output, float[] a, float[] b, float t)
	{
		return QVec4f.Lerp(output, a, b, t);
	}

	public static float[] Slerp(float[] output, float[] a, float[] b, float t)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		float num5 = b[0];
		float num6 = b[1];
		float num7 = b[2];
		float num8 = b[3];
		float num9 = num * num5 + num2 * num6 + num3 * num7 + num4 * num8;
		if (num9 < 0f)
		{
			num9 = 0f - num9;
			num5 = 0f - num5;
			num6 = 0f - num6;
			num7 = 0f - num7;
			num8 = 0f - num8;
		}
		float num10 = 1f;
		float num11 = num10 / 1000000f;
		float num14;
		float num15;
		if (num10 - num9 > num11)
		{
			float num12 = GameMath.Acos(num9);
			float num13 = GameMath.Sin(num12);
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

	public float[] Invert(float[] output, float[] a)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		float num5 = num * num + num2 * num2 + num3 * num3 + num4 * num4;
		float num6 = 1f;
		float num7 = ((num5 != 0f) ? (num6 / num5) : 0f);
		output[0] = (0f - num) * num7;
		output[1] = (0f - num2) * num7;
		output[2] = (0f - num3) * num7;
		output[3] = num4 * num7;
		return output;
	}

	public float[] Conjugate(float[] output, float[] a)
	{
		output[0] = 0f - a[0];
		output[1] = 0f - a[1];
		output[2] = 0f - a[2];
		output[3] = a[3];
		return output;
	}

	public static float Length_(float[] a)
	{
		return QVec4f.Length_(a);
	}

	public static float Len(float[] a)
	{
		return Length_(a);
	}

	public static float SquaredLength(float[] a)
	{
		return QVec4f.SquaredLength(a);
	}

	public static float SqrLen(float[] a)
	{
		return SquaredLength(a);
	}

	public static float[] Normalize(float[] output, float[] a)
	{
		return QVec4f.Normalize(output, a);
	}

	public static float[] FromMat3(float[] output, float[] m)
	{
		float num = m[0] + m[4] + m[8];
		float num2 = 0f;
		float num3 = 1f;
		float num4 = num3 / 2f;
		if (num > num2)
		{
			float num5 = GameMath.Sqrt(num + num3);
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
			float num5 = GameMath.Sqrt(m[num6 * 3 + num6] - m[num7 * 3 + num7] - m[num8 * 3 + num8] + num3);
			output[num6] = num4 * num5;
			num5 = num4 / num5;
			output[3] = (m[num8 * 3 + num7] - m[num7 * 3 + num8]) * num5;
			output[num7] = (m[num7 * 3 + num6] + m[num6 * 3 + num7]) * num5;
			output[num8] = (m[num8 * 3 + num6] + m[num6 * 3 + num8]) * num5;
		}
		return output;
	}
}
