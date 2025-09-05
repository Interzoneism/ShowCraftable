using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

internal class WeightedOctavedNoise : NormalizedSimplexNoise
{
	private double[] offsets;

	public WeightedOctavedNoise(double[] offsets, double[] inputAmplitudes, double[] frequencies, long seed)
		: base(inputAmplitudes, frequencies, seed)
	{
		this.offsets = offsets;
	}

	public override double Noise(double x, double y)
	{
		double num = 1.0;
		for (int i = 0; i < inputAmplitudes.Length; i++)
		{
			double num2 = inputAmplitudes[i];
			num += Math.Min(num2, Math.Max(0.0 - num2, octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * num2 - offsets[i]));
		}
		return num / 2.0;
	}

	public override double Noise(double x, double y, double z)
	{
		double num = 1.0;
		for (int i = 0; i < inputAmplitudes.Length; i++)
		{
			double num2 = inputAmplitudes[i];
			num += Math.Min(num2, Math.Max(0.0 - num2, octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i]) * num2));
		}
		return num / 2.0;
	}
}
