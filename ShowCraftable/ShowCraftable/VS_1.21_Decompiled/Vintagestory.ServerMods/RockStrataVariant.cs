using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class RockStrataVariant : WorldPropertyVariant
{
	[JsonProperty]
	public AssetLocation BlockCode;

	[JsonProperty]
	public string RockType;

	[JsonProperty]
	public float Weight;

	[JsonProperty]
	public float SoilpH;

	[JsonProperty]
	public float WeatheringFactor;

	[JsonProperty]
	public float HeightErosion;

	[JsonProperty]
	public EnumRockGroup RockGroup;
}
