using Newtonsoft.Json;

namespace Vintagestory.API.Common;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class WorldProperty<T>
{
	[JsonProperty]
	public AssetLocation Code;

	[JsonProperty]
	public T[] Variants;
}
