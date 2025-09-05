using Newtonsoft.Json;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class BlockEntityBehaviorType
{
	[JsonProperty]
	public string Name;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject properties;
}
