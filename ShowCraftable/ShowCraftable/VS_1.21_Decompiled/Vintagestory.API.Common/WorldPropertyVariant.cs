using Newtonsoft.Json;

namespace Vintagestory.API.Common;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class WorldPropertyVariant
{
	[JsonProperty]
	public AssetLocation Code;
}
