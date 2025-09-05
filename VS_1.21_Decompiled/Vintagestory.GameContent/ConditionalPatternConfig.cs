using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class ConditionalPatternConfig
{
	public EnumChanceFunction WeightFunction;

	public float? MinRain;

	public float? MaxRain;

	public float RainRange = 1f;

	public float? MinTemp;

	public float? MaxTemp;

	public float TempRange = 1f;

	public float Weight = 1f;

	public float getWeight(float rainfall, float temperature)
	{
		float num = Weight;
		switch (WeightFunction)
		{
		case EnumChanceFunction.TestRainTemp:
			if (MinRain.HasValue)
			{
				num *= GameMath.Clamp(rainfall - MinRain.Value, 0f, RainRange) / RainRange;
			}
			if (MinTemp.HasValue)
			{
				num *= GameMath.Clamp(temperature - MinTemp.Value, 0f, TempRange) / TempRange;
			}
			if (MaxRain.HasValue)
			{
				num *= GameMath.Clamp(MaxRain.Value - rainfall, 0f, RainRange) / RainRange;
			}
			if (MaxTemp.HasValue)
			{
				num *= GameMath.Clamp(MaxTemp.Value - temperature, 0f, TempRange) / TempRange;
			}
			break;
		case EnumChanceFunction.AvoidHotAndDry:
		{
			float num2 = (TempRange + 20f) / 60f;
			float num3 = rainfall * (1f - num2);
			num *= num3;
			break;
		}
		}
		return num;
	}
}
