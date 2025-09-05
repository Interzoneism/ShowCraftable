namespace Vintagestory.API.MathTools;

public class ClampedSimplexNoise
{
	public SimplexNoiseOctave[] octaves;

	public double[] amplitudes;

	public double[] frequencies;

	private long seed;

	public ClampedSimplexNoise(double[] amplitudes, double[] frequencies, long seed)
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

	public virtual double Noise(double x, double y, double offset = 0.0)
	{
		double num = 1.0;
		for (int i = 0; i < amplitudes.Length; i++)
		{
			num += octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * amplitudes[i];
		}
		return GameMath.Clamp(num / 2.0 + offset, 0.0, 1.0);
	}

	public ClampedSimplexNoise Clone()
	{
		return new ClampedSimplexNoise((double[])amplitudes.Clone(), (double[])frequencies.Clone(), seed);
	}
}
