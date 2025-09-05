using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.ServerMods;

namespace Vintagestory.API.Common;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class RockStrataConfig : WorldProperty<RockStratum>
{
	public Dictionary<EnumRockGroup, float> MaxThicknessPerGroup = new Dictionary<EnumRockGroup, float>();
}
