using System;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.MathTools;

public class NormalizedSimplexNoise
{
	public struct ColumnNoise
	{
		private struct OctaveEntry
		{
			public SimplexNoiseOctave Octave;

			public double X;

			public double FrequencyY;

			public double Z;

			public double Amplitude;

			public double Threshold;

			public double StopBound;
		}

		private OctaveEntry[] orderedOctaveEntries;

		public double UncurvedBound { get; private set; }

		public double BoundMin { get; private set; }

		public double BoundMax { get; private set; }

		public ColumnNoise(NormalizedSimplexNoise terrainNoise, double relativeYFrequency, double[] amplitudes, double[] thresholds, double noiseX, double noiseZ)
		{
			int num = terrainNoise.frequencies.Length;
			int num2 = 0;
			double[] array = new double[num];
			int[] array2 = new int[num];
			double num3 = 0.0;
			for (int num4 = num - 1; num4 >= 0; num4--)
			{
				array[num4] = Math.Max(0.0, Math.Abs(amplitudes[num4]) - thresholds[num4]) * 1.1845758506756423;
				num3 += array[num4];
				if (array[num4] != 0.0)
				{
					array2[num2] = num4;
					for (int num5 = num2 - 1; num5 >= 0; num5--)
					{
						if (array[array2[num5 + 1]] > array[array2[num5]])
						{
							int num6 = array2[num5];
							array2[num5] = array2[num5 + 1];
							array2[num5 + 1] = num6;
						}
					}
					num2++;
				}
			}
			UncurvedBound = num3;
			BoundMin = NoiseValueCurve(0.0 - num3);
			BoundMax = NoiseValueCurve(num3);
			orderedOctaveEntries = new OctaveEntry[num2];
			double num7 = 0.0;
			for (int num8 = num2 - 1; num8 >= 0; num8--)
			{
				int num9 = array2[num8];
				num7 += array[num9];
				double num10 = terrainNoise.frequencies[num9];
				orderedOctaveEntries[num8] = new OctaveEntry
				{
					Octave = terrainNoise.octaves[num9],
					X = noiseX * num10,
					Z = noiseZ * num10,
					FrequencyY = num10 * relativeYFrequency,
					Amplitude = amplitudes[num9] * 1.2,
					Threshold = thresholds[num9] * 1.2,
					StopBound = num7
				};
			}
		}

		public double NoiseSign(double y, double inverseCurvedThresholder)
		{
			double num = inverseCurvedThresholder;
			for (int i = 0; i < orderedOctaveEntries.Length; i++)
			{
				ref OctaveEntry reference = ref orderedOctaveEntries[i];
				if (num >= reference.StopBound || num <= 0.0 - reference.StopBound)
				{
					break;
				}
				double value = reference.Octave.Evaluate(reference.X, y * reference.FrequencyY, reference.Z) * reference.Amplitude;
				num += ApplyThresholding(value, reference.Threshold);
			}
			return num;
		}

		public double Noise(double y)
		{
			double num = 0.0;
			for (int i = 0; i < orderedOctaveEntries.Length; i++)
			{
				ref OctaveEntry reference = ref orderedOctaveEntries[i];
				double value = reference.Octave.Evaluate(reference.X, y * reference.FrequencyY, reference.Z) * reference.Amplitude;
				num += ApplyThresholding(value, reference.Threshold);
			}
			return NoiseValueCurve(num);
		}
	}

	private const double VALUE_MULTIPLIER = 1.2;

	public double[] scaledAmplitudes2D;

	public double[] scaledAmplitudes3D;

	public double[] inputAmplitudes;

	public double[] frequencies;

	public SimplexNoiseOctave[] octaves;

	public NormalizedSimplexNoise(double[] inputAmplitudes, double[] frequencies, long seed)
	{
		this.frequencies = frequencies;
		this.inputAmplitudes = inputAmplitudes;
		octaves = new SimplexNoiseOctave[inputAmplitudes.Length];
		for (int i = 0; i < octaves.Length; i++)
		{
			octaves[i] = new SimplexNoiseOctave(seed * 65599 + i);
		}
		CalculateAmplitudes(inputAmplitudes);
	}

	public static NormalizedSimplexNoise FromDefaultOctaves(int quantityOctaves, double baseFrequency, double persistence, long seed)
	{
		double[] array = new double[quantityOctaves];
		double[] array2 = new double[quantityOctaves];
		for (int i = 0; i < quantityOctaves; i++)
		{
			array[i] = Math.Pow(2.0, i) * baseFrequency;
			array2[i] = Math.Pow(persistence, i);
		}
		return new NormalizedSimplexNoise(array2, array, seed);
	}

	internal virtual void CalculateAmplitudes(double[] inputAmplitudes)
	{
		double num = 0.0;
		double num2 = 0.0;
		for (int i = 0; i < inputAmplitudes.Length; i++)
		{
			num += inputAmplitudes[i] * Math.Pow(0.64, i + 1);
			num2 += inputAmplitudes[i] * Math.Pow(0.73, i + 1);
		}
		scaledAmplitudes2D = new double[inputAmplitudes.Length];
		for (int j = 0; j < inputAmplitudes.Length; j++)
		{
			scaledAmplitudes2D[j] = inputAmplitudes[j] / num2;
		}
		scaledAmplitudes3D = new double[inputAmplitudes.Length];
		for (int k = 0; k < inputAmplitudes.Length; k++)
		{
			scaledAmplitudes3D[k] = inputAmplitudes[k] / num;
		}
	}

	public virtual double Noise(double x, double y)
	{
		double num = 0.0;
		for (int i = 0; i < scaledAmplitudes2D.Length; i++)
		{
			num += 1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * scaledAmplitudes2D[i];
		}
		return NoiseValueCurve(num);
	}

	public double Noise(double x, double y, double[] thresholds)
	{
		double num = 0.0;
		for (int i = 0; i < scaledAmplitudes2D.Length; i++)
		{
			double num2 = octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * scaledAmplitudes2D[i];
			num += 1.2 * ((num2 > 0.0) ? Math.Max(0.0, num2 - thresholds[i]) : Math.Min(0.0, num2 + thresholds[i]));
		}
		return NoiseValueCurve(num);
	}

	public virtual double Noise(double x, double y, double z)
	{
		double num = 0.0;
		for (int i = 0; i < scaledAmplitudes3D.Length; i++)
		{
			num += 1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i]) * scaledAmplitudes3D[i];
		}
		return NoiseValueCurve(num);
	}

	public virtual double Noise(double x, double y, double z, double[] amplitudes)
	{
		double num = 0.0;
		for (int i = 0; i < scaledAmplitudes3D.Length; i++)
		{
			num += 1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i]) * amplitudes[i];
		}
		return NoiseValueCurve(num);
	}

	public double Noise(double x, double y, double z, double[] amplitudes, double[] thresholds)
	{
		double num = 0.0;
		for (int i = 0; i < scaledAmplitudes3D.Length; i++)
		{
			double num2 = frequencies[i];
			double value = octaves[i].Evaluate(x * num2, y * num2, z * num2) * amplitudes[i];
			num += 1.2 * ApplyThresholding(value, thresholds[i]);
		}
		return NoiseValueCurve(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double NoiseValueCurve(double value)
	{
		return Math.Tanh(value) * 0.5 + 0.5;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double NoiseValueCurveInverse(double value)
	{
		if (value <= 0.0)
		{
			return double.NegativeInfinity;
		}
		if (value >= 1.0)
		{
			return double.PositiveInfinity;
		}
		return 0.5 * Math.Log(value / (1.0 - value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double ApplyThresholding(double value, double threshold)
	{
		if (!(value > 0.0))
		{
			return Math.Min(0.0, value + threshold);
		}
		return Math.Max(0.0, value - threshold);
	}

	public ColumnNoise ForColumn(double relativeYFrequency, double[] amplitudes, double[] thresholds, double noiseX, double noiseZ)
	{
		return new ColumnNoise(this, relativeYFrequency, amplitudes, thresholds, noiseX, noiseZ);
	}
}
