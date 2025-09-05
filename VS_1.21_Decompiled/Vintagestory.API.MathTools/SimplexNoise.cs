using System;

namespace Vintagestory.API.MathTools;

public class SimplexNoise
{
	public SimplexNoiseOctave[] octaves;

	public double[] amplitudes;

	public double[] frequencies;

	private long seed;

	public SimplexNoise(double[] amplitudes, double[] frequencies, long seed)
	{
		this.amplitudes = amplitudes;
		this.frequencies = frequencies;
		this.seed = seed;
		octaves = new SimplexNoiseOctave[amplitudes.Length];
		for (int i = 0; i < octaves.Length; i++)
		{
			octaves[i] = new SimplexNoiseOctave(seed * 65599 + i);
		}
	}

	public static SimplexNoise FromDefaultOctaves(int quantityOctaves, double baseFrequency, double persistence, long seed)
	{
		double[] array = new double[quantityOctaves];
		double[] array2 = new double[quantityOctaves];
		for (int i = 0; i < quantityOctaves; i++)
		{
			array[i] = Math.Pow(2.0, i) * baseFrequency;
			array2[i] = Math.Pow(persistence, i);
		}
		return new SimplexNoise(array2, array, seed);
	}

	public virtual double Noise(double x, double y)
	{
		double num = 0.0;
		for (int i = 0; i < amplitudes.Length; i++)
		{
			double num2 = frequencies[i];
			num += octaves[i].Evaluate(x * num2, y * num2) * amplitudes[i];
		}
		return num;
	}

	public static void NoiseFairWarpVector(SimplexNoise originalWarpX, SimplexNoise originalWarpY, double x, double y, out double distX, out double distY)
	{
		distX = (distY = 0.0);
		for (int i = 0; i < originalWarpX.amplitudes.Length; i++)
		{
			double num = originalWarpX.frequencies[i];
			SimplexNoiseOctave.EvaluateFairWarpVector(originalWarpX.octaves[i], originalWarpY.octaves[i], x * num, y * num, out var distX2, out var distY2);
			distX += distX2 * originalWarpX.amplitudes[i];
			distY += distY2 * originalWarpX.amplitudes[i];
		}
	}

	public double Noise(double x, double y, double[] thresholds)
	{
		double num = 0.0;
		for (int i = 0; i < amplitudes.Length; i++)
		{
			double num2 = frequencies[i];
			double num3 = octaves[i].Evaluate(x * num2, y * num2) * amplitudes[i];
			num += ((num3 > 0.0) ? Math.Max(0.0, num3 - thresholds[i]) : Math.Min(0.0, num3 + thresholds[i]));
		}
		return num;
	}

	public double NoiseWithThreshold(double x, double y, double threshold)
	{
		double num = 0.0;
		for (int i = 0; i < amplitudes.Length; i++)
		{
			double num2 = threshold * amplitudes[i];
			double num3 = octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * amplitudes[i];
			num += ((num3 > 0.0) ? Math.Max(0.0, num3 - num2) : Math.Min(0.0, num3 + num2));
		}
		return num;
	}

	public virtual double Noise(double x, double y, double z)
	{
		double num = 0.0;
		for (int i = 0; i < amplitudes.Length; i++)
		{
			double num2 = frequencies[i];
			num += octaves[i].Evaluate(x * num2, y * num2, z * num2) * amplitudes[i];
		}
		return num;
	}

	public virtual double AbsNoise(double x, double y, double z)
	{
		double num = 0.0;
		for (int i = 0; i < amplitudes.Length; i++)
		{
			num += Math.Abs(octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i]) * amplitudes[i]);
		}
		return num;
	}

	public SimplexNoise Clone()
	{
		return new SimplexNoise((double[])amplitudes.Clone(), (double[])frequencies.Clone(), seed);
	}
}
