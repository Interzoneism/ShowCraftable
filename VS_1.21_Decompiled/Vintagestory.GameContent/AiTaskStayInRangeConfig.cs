using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskStayInRangeConfig : AiTaskBaseTargetableConfig
{
	[JsonProperty]
	public bool RetaliateUnconditionally;

	[JsonProperty]
	public float TargetRangeMin = 15f;

	[JsonProperty]
	public float TargetRangeMax = 25f;

	[JsonProperty]
	public float MoveSpeed = 0.02f;

	[JsonProperty]
	public EnumAICreatureType AiCreatureType;

	[JsonProperty]
	public bool CanStepInLiquid;
}
