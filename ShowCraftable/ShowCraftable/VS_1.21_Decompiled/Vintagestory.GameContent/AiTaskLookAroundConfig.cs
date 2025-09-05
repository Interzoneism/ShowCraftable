using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskLookAroundConfig : AiTaskBaseConfig
{
	[JsonProperty]
	public float TurnAngleFactor = 0.75f;
}
