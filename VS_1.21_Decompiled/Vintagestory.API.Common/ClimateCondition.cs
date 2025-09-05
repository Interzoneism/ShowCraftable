namespace Vintagestory.API.Common;

public class ClimateCondition
{
	public float Temperature;

	public float WorldgenRainfall;

	public float WorldGenTemperature;

	public float GeologicActivity;

	public float Rainfall;

	public float RainCloudOverlay;

	public float Fertility;

	public float ForestDensity;

	public float ShrubDensity;

	public void SetLerped(ClimateCondition left, ClimateCondition right, float w)
	{
		Temperature = left.Temperature * (1f - w) + right.Temperature * w;
		Rainfall = left.Rainfall * (1f - w) + right.Rainfall * w;
		Fertility = left.Fertility * (1f - w) + right.Fertility * w;
		ForestDensity = left.ForestDensity * (1f - w) + right.ForestDensity * w;
		ShrubDensity = left.ShrubDensity * (1f - w) + right.ShrubDensity * w;
	}
}
