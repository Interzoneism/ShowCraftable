using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class WorldWoodPropertyVariant
{
	[JsonProperty]
	public AssetLocation Code;

	[JsonProperty]
	public EnumTreeType TreeType;
}
