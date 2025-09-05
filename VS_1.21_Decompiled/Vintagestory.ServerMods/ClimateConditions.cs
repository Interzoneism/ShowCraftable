using Newtonsoft.Json;

namespace Vintagestory.ServerMods;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class ClimateConditions
{
	[JsonProperty]
	public float MinTemp = -50f;

	[JsonProperty]
	public float MaxTemp = 50f;

	[JsonProperty]
	public float MinRain;

	[JsonProperty]
	public float MaxRain = 1f;

	[JsonProperty]
	public float MinY;

	[JsonProperty]
	public float MaxY = 2f;

	public ClimateConditions Clone()
	{
		return new ClimateConditions
		{
			MinRain = MinRain,
			MinTemp = MinTemp,
			MaxRain = MaxRain,
			MaxTemp = MaxTemp,
			MinY = MinY,
			MaxY = MaxY
		};
	}
}
