namespace Vintagestory.API.MathTools;

public class Mat3d
{
	public static double[] Create()
	{
		return new double[9] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 };
	}

	public static double[] FromMat4(double[] output, double[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		output[3] = a[4];
		output[4] = a[5];
		output[5] = a[6];
		output[6] = a[8];
		output[7] = a[9];
		output[8] = a[10];
		return output;
	}

	public static double[] CloneIt(double[] a)
	{
		return new double[9]
		{
			a[0],
			a[1],
			a[2],
			a[3],
			a[4],
			a[5],
			a[6],
			a[7],
			a[8]
		};
	}

	public static double[] Copy(double[] output, double[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		output[3] = a[3];
		output[4] = a[4];
		output[5] = a[5];
		output[6] = a[6];
		output[7] = a[7];
		output[8] = a[8];
		return output;
	}

	public static double[] Identity_(double[] output)
	{
		output[0] = 1.0;
		output[1] = 0.0;
		output[2] = 0.0;
		output[3] = 0.0;
		output[4] = 1.0;
		output[5] = 0.0;
		output[6] = 0.0;
		output[7] = 0.0;
		output[8] = 1.0;
		return output;
	}

	public static double[] Transpose(double[] output, double[] a)
	{
		if (output == a)
		{
			double num = a[1];
			double num2 = a[2];
			double num3 = a[5];
			output[1] = a[3];
			output[2] = a[6];
			output[3] = num;
			output[5] = a[7];
			output[6] = num2;
			output[7] = num3;
		}
		else
		{
			output[0] = a[0];
			output[1] = a[3];
			output[2] = a[6];
			output[3] = a[1];
			output[4] = a[4];
			output[5] = a[7];
			output[6] = a[2];
			output[7] = a[5];
			output[8] = a[8];
		}
		return output;
	}

	public static double[] Invert(double[] output, double[] a)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = a[4];
		double num6 = a[5];
		double num7 = a[6];
		double num8 = a[7];
		double num9 = a[8];
		double num10 = num9 * num5 - num6 * num8;
		double num11 = (0.0 - num9) * num4 + num6 * num7;
		double num12 = num8 * num4 - num5 * num7;
		double num13 = num * num10 + num2 * num11 + num3 * num12;
		if (num13 == 0.0)
		{
			return null;
		}
		num13 = 1.0 / num13;
		output[0] = num10 * num13;
		output[1] = ((0.0 - num9) * num2 + num3 * num8) * num13;
		output[2] = (num6 * num2 - num3 * num5) * num13;
		output[3] = num11 * num13;
		output[4] = (num9 * num - num3 * num7) * num13;
		output[5] = ((0.0 - num6) * num + num3 * num4) * num13;
		output[6] = num12 * num13;
		output[7] = ((0.0 - num8) * num + num2 * num7) * num13;
		output[8] = (num5 * num - num2 * num4) * num13;
		return output;
	}

	public static double[] Adjoint(double[] output, double[] a)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = a[4];
		double num6 = a[5];
		double num7 = a[6];
		double num8 = a[7];
		double num9 = a[8];
		output[0] = num5 * num9 - num6 * num8;
		output[1] = num3 * num8 - num2 * num9;
		output[2] = num2 * num6 - num3 * num5;
		output[3] = num6 * num7 - num4 * num9;
		output[4] = num * num9 - num3 * num7;
		output[5] = num3 * num4 - num * num6;
		output[6] = num4 * num8 - num5 * num7;
		output[7] = num2 * num7 - num * num8;
		output[8] = num * num5 - num2 * num4;
		return output;
	}

	public static double Determinant(double[] a)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = a[4];
		double num6 = a[5];
		double num7 = a[6];
		double num8 = a[7];
		double num9 = a[8];
		return num * (num9 * num5 - num6 * num8) + num2 * ((0.0 - num9) * num4 + num6 * num7) + num3 * (num8 * num4 - num5 * num7);
	}

	public static double[] Multiply(double[] output, double[] a, double[] b)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = a[4];
		double num6 = a[5];
		double num7 = a[6];
		double num8 = a[7];
		double num9 = a[8];
		double num10 = b[0];
		double num11 = b[1];
		double num12 = b[2];
		double num13 = b[3];
		double num14 = b[4];
		double num15 = b[5];
		double num16 = b[6];
		double num17 = b[7];
		double num18 = b[8];
		output[0] = num10 * num + num11 * num4 + num12 * num7;
		output[1] = num10 * num2 + num11 * num5 + num12 * num8;
		output[2] = num10 * num3 + num11 * num6 + num12 * num9;
		output[3] = num13 * num + num14 * num4 + num15 * num7;
		output[4] = num13 * num2 + num14 * num5 + num15 * num8;
		output[5] = num13 * num3 + num14 * num6 + num15 * num9;
		output[6] = num16 * num + num17 * num4 + num18 * num7;
		output[7] = num16 * num2 + num17 * num5 + num18 * num8;
		output[8] = num16 * num3 + num17 * num6 + num18 * num9;
		return output;
	}

	public static double[] Mul(double[] output, double[] a, double[] b)
	{
		return Multiply(output, a, b);
	}

	public static double[] Translate(double[] output, double[] a, double[] v)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = a[4];
		double num6 = a[5];
		double num7 = a[6];
		double num8 = a[7];
		double num9 = a[8];
		double num10 = v[0];
		double num11 = v[1];
		output[0] = num;
		output[1] = num2;
		output[2] = num3;
		output[3] = num4;
		output[4] = num5;
		output[5] = num6;
		output[6] = num10 * num + num11 * num4 + num7;
		output[7] = num10 * num2 + num11 * num5 + num8;
		output[8] = num10 * num3 + num11 * num6 + num9;
		return output;
	}

	public static double[] Rotate(double[] output, double[] a, double rad)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = a[4];
		double num6 = a[5];
		double num7 = a[6];
		double num8 = a[7];
		double num9 = a[8];
		double num10 = GameMath.Sin(rad);
		double num11 = GameMath.Cos(rad);
		output[0] = num11 * num + num10 * num4;
		output[1] = num11 * num2 + num10 * num5;
		output[2] = num11 * num3 + num10 * num6;
		output[3] = num11 * num4 - num10 * num;
		output[4] = num11 * num5 - num10 * num2;
		output[5] = num11 * num6 - num10 * num3;
		output[6] = num7;
		output[7] = num8;
		output[8] = num9;
		return output;
	}

	public static double[] Scale(double[] output, double[] a, double[] v)
	{
		double num = v[0];
		double num2 = v[1];
		output[0] = num * a[0];
		output[1] = num * a[1];
		output[2] = num * a[2];
		output[3] = num2 * a[3];
		output[4] = num2 * a[4];
		output[5] = num2 * a[5];
		output[6] = a[6];
		output[7] = a[7];
		output[8] = a[8];
		return output;
	}

	public static double[] FromMat2d(double[] output, double[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = 0.0;
		output[3] = a[2];
		output[4] = a[3];
		output[5] = 0.0;
		output[6] = a[4];
		output[7] = a[5];
		output[8] = 1.0;
		return output;
	}

	public static double[] FromQuat(double[] output, double[] q)
	{
		double num = q[0];
		double num2 = q[1];
		double num3 = q[2];
		double num4 = q[3];
		double num5 = num + num;
		double num6 = num2 + num2;
		double num7 = num3 + num3;
		double num8 = num * num5;
		double num9 = num * num6;
		double num10 = num * num7;
		double num11 = num2 * num6;
		double num12 = num2 * num7;
		double num13 = num3 * num7;
		double num14 = num4 * num5;
		double num15 = num4 * num6;
		double num16 = num4 * num7;
		output[0] = 1.0 - (num11 + num13);
		output[3] = num9 + num16;
		output[6] = num10 - num15;
		output[1] = num9 - num16;
		output[4] = 1.0 - (num8 + num13);
		output[7] = num12 + num14;
		output[2] = num10 + num15;
		output[5] = num12 - num14;
		output[8] = 1.0 - (num8 + num11);
		return output;
	}

	public static double[] NormalFromMat4(double[] output, double[] a)
	{
		double num = a[0];
		double num2 = a[1];
		double num3 = a[2];
		double num4 = a[3];
		double num5 = a[4];
		double num6 = a[5];
		double num7 = a[6];
		double num8 = a[7];
		double num9 = a[8];
		double num10 = a[9];
		double num11 = a[10];
		double num12 = a[11];
		double num13 = a[12];
		double num14 = a[13];
		double num15 = a[14];
		double num16 = a[15];
		double num17 = num * num6 - num2 * num5;
		double num18 = num * num7 - num3 * num5;
		double num19 = num * num8 - num4 * num5;
		double num20 = num2 * num7 - num3 * num6;
		double num21 = num2 * num8 - num4 * num6;
		double num22 = num3 * num8 - num4 * num7;
		double num23 = num9 * num14 - num10 * num13;
		double num24 = num9 * num15 - num11 * num13;
		double num25 = num9 * num16 - num12 * num13;
		double num26 = num10 * num15 - num11 * num14;
		double num27 = num10 * num16 - num12 * num14;
		double num28 = num11 * num16 - num12 * num15;
		double num29 = num17 * num28 - num18 * num27 + num19 * num26 + num20 * num25 - num21 * num24 + num22 * num23;
		if (num29 == 0.0)
		{
			return null;
		}
		num29 = 1.0 / num29;
		output[0] = (num6 * num28 - num7 * num27 + num8 * num26) * num29;
		output[1] = (num7 * num25 - num5 * num28 - num8 * num24) * num29;
		output[2] = (num5 * num27 - num6 * num25 + num8 * num23) * num29;
		output[3] = (num3 * num27 - num2 * num28 - num4 * num26) * num29;
		output[4] = (num * num28 - num3 * num25 + num4 * num24) * num29;
		output[5] = (num2 * num25 - num * num27 - num4 * num23) * num29;
		output[6] = (num14 * num22 - num15 * num21 + num16 * num20) * num29;
		output[7] = (num15 * num19 - num13 * num22 - num16 * num18) * num29;
		output[8] = (num13 * num21 - num14 * num19 + num16 * num17) * num29;
		return output;
	}

	private void f()
	{
	}
}
