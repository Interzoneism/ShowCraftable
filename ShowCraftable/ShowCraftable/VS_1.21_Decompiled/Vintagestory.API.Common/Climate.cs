using System;

namespace Vintagestory.API.Common;

public class Climate
{
	public static int Sealevel = 110;

	public static float TemperatureScaleConversion = 4.25f;

	public static int DescaleTemperature(float temperature)
	{
		return Math.Clamp((int)((temperature + 20f) * TemperatureScaleConversion), 0, 255);
	}

	public static int GetRainFall(int rainfall, int y)
	{
		return Math.Clamp(rainfall + (y - Sealevel) / 2 + 5 * Math.Clamp(8 + Sealevel - y, 0, 8), 0, 255);
	}

	public static int GetScaledAdjustedTemperature(int unscaledTemp, int distToSealevel)
	{
		return Math.Clamp((int)(((float)unscaledTemp - (float)distToSealevel / 1.5f) / TemperatureScaleConversion) - 20, -20, 40);
	}

	public static float GetScaledAdjustedTemperatureFloat(int unscaledTemp, int distToSealevel)
	{
		return Math.Clamp(((float)unscaledTemp - (float)distToSealevel / 1.5f) / TemperatureScaleConversion - 20f, -20f, 40f);
	}

	public static float GetScaledAdjustedTemperatureFloatClient(int unscaledTemp, int distToSealevel)
	{
		return Math.Clamp(((float)unscaledTemp - (float)distToSealevel / 1.5f) / TemperatureScaleConversion - 20f, -50f, 40f);
	}

	public static int GetAdjustedTemperature(int unscaledTemp, int distToSealevel)
	{
		return (int)Math.Clamp((float)unscaledTemp - (float)distToSealevel / 1.5f, 0f, 255f);
	}

	public static int GetFertility(int rain, float scaledTemp, float posYRel)
	{
		float num = Math.Min(255f, (float)rain / 2f + Math.Max(0f, (float)(rain * DescaleTemperature(scaledTemp)) / 512f));
		float num2 = 1f - Math.Max(0f, (80f - num) / 80f);
		return (int)Math.Max(0f, num - Math.Max(0f, 50f * (posYRel - 0.5f)) * num2);
	}

	public static int GetFertilityFromUnscaledTemp(int rain, int unscaledTemp, float posYRel)
	{
		float num = Math.Min(255f, (float)rain / 2f + Math.Max(0f, (float)(rain * unscaledTemp) / 512f));
		float num2 = 1f - Math.Max(0f, (80f - num) / 80f);
		return (int)Math.Max(0f, num - Math.Max(0f, 50f * (posYRel - 0.5f)) * num2);
	}
}
