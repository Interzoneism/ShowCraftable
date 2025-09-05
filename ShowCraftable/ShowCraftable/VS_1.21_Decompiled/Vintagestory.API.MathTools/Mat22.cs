namespace Vintagestory.API.MathTools;

public class Mat22
{
	public static float[] Create()
	{
		return new float[4] { 1f, 0f, 0f, 1f };
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

	public static float[] Copy(float[] output, float[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		output[3] = a[3];
		return output;
	}

	public static float[] Identity_(float[] output)
	{
		output[0] = 1f;
		output[1] = 0f;
		output[2] = 0f;
		output[3] = 1f;
		return output;
	}

	public static float[] Transpose(float[] output, float[] a)
	{
		output[0] = a[0];
		output[1] = a[2];
		output[2] = a[1];
		output[3] = a[3];
		return output;
	}

	public static float[] Invert(float[] output, float[] a)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		float num5 = num * num4 - num3 * num2;
		if (num5 == 0f)
		{
			return null;
		}
		num5 = 1f / num5;
		output[0] = num4 * num5;
		output[1] = (0f - num2) * num5;
		output[2] = (0f - num3) * num5;
		output[3] = num * num5;
		return output;
	}

	public static float[] Adjoint(float[] output, float[] a)
	{
		float num = a[0];
		output[0] = a[3];
		output[1] = 0f - a[1];
		output[2] = 0f - a[2];
		output[3] = num;
		return output;
	}

	public static float Determinant(float[] a)
	{
		return a[0] * a[3] - a[2] * a[1];
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
		output[0] = num * num5 + num2 * num7;
		output[1] = num * num6 + num2 * num8;
		output[2] = num3 * num5 + num4 * num7;
		output[3] = num3 * num6 + num4 * num8;
		return output;
	}

	public static float[] Mul(float[] output, float[] a, float[] b)
	{
		return Multiply(output, a, b);
	}

	public static float[] Rotate(float[] output, float[] a, float rad)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		float num5 = GameMath.Sin(rad);
		float num6 = GameMath.Cos(rad);
		output[0] = num * num6 + num2 * num5;
		output[1] = num * (0f - num5) + num2 * num6;
		output[2] = num3 * num6 + num4 * num5;
		output[3] = num3 * (0f - num5) + num4 * num6;
		return output;
	}

	public static float[] Scale(float[] output, float[] a, float[] v)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		float num5 = v[0];
		float num6 = v[1];
		output[0] = num * num5;
		output[1] = num2 * num6;
		output[2] = num3 * num5;
		output[3] = num4 * num6;
		return output;
	}
}
