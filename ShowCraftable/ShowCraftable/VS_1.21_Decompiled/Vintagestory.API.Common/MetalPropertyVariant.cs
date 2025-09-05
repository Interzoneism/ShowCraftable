using Newtonsoft.Json;

namespace Vintagestory.API.Common;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class MetalPropertyVariant : WorldPropertyVariant
{
	[JsonProperty]
	public int Tier;
}
