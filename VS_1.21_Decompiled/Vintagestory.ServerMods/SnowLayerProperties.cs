using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class SnowLayerProperties
{
	[JsonProperty]
	public int MaxTemp;

	[JsonProperty]
	public int TransitionSize;

	[JsonProperty]
	public AssetLocation BlockCode;

	public int BlockId;
}
