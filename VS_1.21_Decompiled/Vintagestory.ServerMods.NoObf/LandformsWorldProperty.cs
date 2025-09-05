using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods.NoObf;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class LandformsWorldProperty : WorldProperty<LandformVariant>
{
	[JsonIgnore]
	public LandformVariant[] LandFormsByIndex;
}
