namespace Vintagestory.API.MathTools;

public class Mat23
{
	public static float[] Create()
	{
		return new float[6] { 1f, 0f, 0f, 1f, 0f, 0f };
	}

	public static float[] CloneIt(float[] a)
	{
		return new float[6]
		{
			a[0],
			a[1],
			a[2],
			a[3],
			a[4],
			a[5]
		};
	}

	public static float[] Copy(float[] output, float[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		output[3] = a[3];
		output[4] = a[4];
		output[5] = a[5];
		return output;
	}

	public static float[] Identity_(float[] output)
	{
		output[0] = 1f;
		output[1] = 0f;
		output[2] = 0f;
		output[3] = 1f;
		output[4] = 0f;
		output[5] = 0f;
		return output;
	}

	public static float[] Invert(float[] output, float[] a)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		float num5 = a[4];
		float num6 = a[5];
		float num7 = num * num4 - num2 * num3;
		if (num7 == 0f)
		{
			return null;
		}
		num7 = 1f / num7;
		output[0] = num4 * num7;
		output[1] = (0f - num2) * num7;
		output[2] = (0f - num3) * num7;
		output[3] = num * num7;
		output[4] = (num3 * num6 - num4 * num5) * num7;
		output[5] = (num2 * num5 - num * num6) * num7;
		return output;
	}

	public static float Determinant(float[] a)
	{
		return a[0] * a[3] - a[1] * a[2];
	}

	public static float[] Multiply(float[] output, float[] a, float[] b)
	{
		float num = a[0];
		float num2 = a[1];
		float num3 = a[2];
		float num4 = a[3];
		float num5 = a[4];
		float num6 = a[5];
		float num7 = b[0];
		float num8 = b[1];
		float num9 = b[2];
		float num10 = b[3];
		float num11 = b[4];
		float num12 = b[5];
		output[0] = num * num7 + num2 * num9;
		output[1] = num * num8 + num2 * num10;
		output[2] = num3 * num7 + num4 * num9;
		output[3] = num3 * num8 + num4 * num10;
		output[4] = num7 * num5 + num9 * num6 + num11;
		output[5] = num8 * num5 + num10 * num6 + num12;
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
		float num5 = a[4];
		float num6 = a[5];
		float num7 = GameMath.Sin(rad);
		float num8 = GameMath.Cos(rad);
		output[0] = num * num8 + num2 * num7;
		output[1] = (0f - num) * num7 + num2 * num8;
		output[2] = num3 * num8 + num4 * num7;
		output[3] = (0f - num3) * num7 + num8 * num4;
		output[4] = num8 * num5 + num7 * num6;
		output[5] = num8 * num6 - num7 * num5;
		return output;
	}

	public static float[] Scale(float[] output, float[] a, float[] v)
	{
		float num = v[0];
		float num2 = v[1];
		output[0] = a[0] * num;
		output[1] = a[1] * num2;
		output[2] = a[2] * num;
		output[3] = a[3] * num2;
		output[4] = a[4] * num;
		output[5] = a[5] * num2;
		return output;
	}

	public static float[] Translate(float[] output, float[] a, float[] v)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		output[3] = a[3];
		output[4] = a[4] + v[0];
		output[5] = a[5] + v[1];
		return output;
	}
}
