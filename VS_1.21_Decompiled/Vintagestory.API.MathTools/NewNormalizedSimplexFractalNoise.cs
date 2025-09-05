using System;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.MathTools;

public class NewNormalizedSimplexFractalNoise
{
	public struct ColumnNoise
	{
		private struct OctaveEntry
		{
			public long Seed;

			public double X;

			public double FrequencyY;

			public double Z;

			public double Amplitude;

			public double Threshold;

			public double SmoothingFactor;

			public double StopBound;
		}

		private struct PastEvaluation
		{
			public double Value;

			public double Y;
		}

		private OctaveEntry[] orderedOctaveEntries;

		private PastEvaluation[] pastEvaluations;

		public double UncurvedBound { get; private set; }

		public double BoundMin { get; private set; }

		public double BoundMax { get; private set; }

		public ColumnNoise(NewNormalizedSimplexFractalNoise terrainNoise, double relativeYFrequency, double[] amplitudes, double[] thresholds, double noiseX, double noiseZ)
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
			pastEvaluations = new PastEvaluation[num2];
			double num7 = 0.0;
			for (int num8 = num2 - 1; num8 >= 0; num8--)
			{
				int num9 = array2[num8];
				num7 += array[num9];
				double num10 = terrainNoise.frequencies[num9];
				orderedOctaveEntries[num8] = new OctaveEntry
				{
					Seed = terrainNoise.octaveSeeds[num9],
					X = noiseX * num10,
					Z = noiseZ * num10,
					FrequencyY = num10 * relativeYFrequency,
					Amplitude = amplitudes[num9] * 1.1845758506756423,
					Threshold = thresholds[num9] * 1.2000000000000002,
					SmoothingFactor = amplitudes[num9] * num10 * 3.5,
					StopBound = num7
				};
				pastEvaluations[num8] = new PastEvaluation
				{
					Y = double.NaN
				};
			}
		}

		public double NoiseSign(double y, double inverseCurvedThresholder)
		{
			double num = inverseCurvedThresholder;
			double num2 = inverseCurvedThresholder;
			double num3 = inverseCurvedThresholder;
			for (int i = 0; i < orderedOctaveEntries.Length; i++)
			{
				if (!(num3 <= 0.0) && !(num2 >= 0.0))
				{
					break;
				}
				ref OctaveEntry reference = ref orderedOctaveEntries[i];
				if (num2 >= reference.StopBound)
				{
					return num2;
				}
				if (num3 <= 0.0 - reference.StopBound)
				{
					return num3;
				}
				double num4 = y * reference.FrequencyY;
				double num5 = Math.Abs(pastEvaluations[i].Y - num4);
				num2 += ApplyThresholding(Math.Max(-1.0, pastEvaluations[i].Value - num5 * 5.0) * reference.Amplitude, reference.Threshold, reference.SmoothingFactor);
				num3 += ApplyThresholding(Math.Min(1.0, pastEvaluations[i].Value + num5 * 5.0) * reference.Amplitude, reference.Threshold, reference.SmoothingFactor);
			}
			for (int j = 0; j < orderedOctaveEntries.Length; j++)
			{
				ref OctaveEntry reference2 = ref orderedOctaveEntries[j];
				if (num >= reference2.StopBound || num <= 0.0 - reference2.StopBound)
				{
					break;
				}
				double y2 = y * reference2.FrequencyY;
				double num6 = NewSimplexNoiseLayer.Evaluate_ImprovedXZ(reference2.Seed, reference2.X, y2, reference2.Z);
				pastEvaluations[j].Value = num6;
				pastEvaluations[j].Y = y2;
				num += ApplyThresholding(num6 * reference2.Amplitude, reference2.Threshold, reference2.SmoothingFactor);
			}
			return num;
		}

		public double Noise(double y)
		{
			double num = 0.0;
			for (int i = 0; i < orderedOctaveEntries.Length; i++)
			{
				ref OctaveEntry reference = ref orderedOctaveEntries[i];
				double value = (double)NewSimplexNoiseLayer.Evaluate_ImprovedXZ(reference.Seed, reference.X, y * reference.FrequencyY, reference.Z) * reference.Amplitude;
				num += ApplyThresholding(value, reference.Threshold, reference.SmoothingFactor);
			}
			return NoiseValueCurve(num);
		}
	}

	private const double ValueMultiplier = 1.1845758506756423;

	private const double ThresholdRescaleOldToNew = 1.0130208203346036;

	private const double AmpAndFreqToThresholdSmoothing = 3.5;

	public double[] scaledAmplitudes2D;

	public double[] scaledAmplitudes3D;

	public double[] inputAmplitudes;

	public double[] frequencies;

	public long[] octaveSeeds;

	public NewNormalizedSimplexFractalNoise(double[] inputAmplitudes, double[] frequencies, long seed)
	{
		this.frequencies = frequencies;
		this.inputAmplitudes = inputAmplitudes;
		octaveSeeds = new long[inputAmplitudes.Length];
		for (int i = 0; i < octaveSeeds.Length; i++)
		{
			octaveSeeds[i] = seed * 65599 + i;
		}
		CalculateAmplitudes(inputAmplitudes);
	}

	public static NewNormalizedSimplexFractalNoise FromDefaultOctaves(int quantityOctaves, double baseFrequency, double persistence, long seed)
	{
		double[] array = new double[quantityOctaves];
		double[] array2 = new double[quantityOctaves];
		for (int i = 0; i < quantityOctaves; i++)
		{
			array[i] = Math.Pow(2.0, i) * baseFrequency;
			array2[i] = Math.Pow(persistence, i);
		}
		return new NewNormalizedSimplexFractalNoise(array2, array, seed);
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

	public double Noise(double x, double y, double z, double[] amplitudes, double[] thresholds)
	{
		double num = 0.0;
		for (int i = 0; i < scaledAmplitudes3D.Length; i++)
		{
			double num2 = frequencies[i];
			double value = (double)NewSimplexNoiseLayer.Evaluate_ImprovedXZ(octaveSeeds[i], x * num2, y * num2, z * num2) * amplitudes[i];
			double smoothingFactor = amplitudes[i] * num2 * 3.5;
			num += 1.1845758506756423 * ApplyThresholding(value, thresholds[i] * 1.0130208203346036, smoothingFactor);
		}
		return NoiseValueCurve(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double NoiseValueCurve(double value)
	{
		return value / Math.Sqrt(1.0 + value * value) * 0.5 + 0.5;
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
		value = value * 2.0 - 1.0;
		return value / Math.Sqrt(1.0 - value * value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double ApplyThresholding(double value, double threshold, double smoothingFactor)
	{
		return GameMath.SmoothMax(0.0, value - threshold, smoothingFactor) + GameMath.SmoothMin(0.0, value + threshold, smoothingFactor);
	}

	public ColumnNoise ForColumn(double relativeYFrequency, double[] amplitudes, double[] thresholds, double noiseX, double noiseZ)
	{
		return new ColumnNoise(this, relativeYFrequency, amplitudes, thresholds, noiseX, noiseZ);
	}
}
