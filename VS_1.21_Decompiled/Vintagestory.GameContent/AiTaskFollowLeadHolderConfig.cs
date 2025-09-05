using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskFollowLeadHolderConfig : AiTaskBaseTargetableConfig
{
	[JsonProperty]
	public float MoveSpeed = 0.3f;

	[JsonProperty]
	public int MinGeneration;

	[JsonProperty]
	public int GoalReachedCooldownMs = 1000;

	[JsonProperty]
	public float MaxDistanceToTarget = 2f;

	[JsonProperty]
	public float ExtraMinDistanceToTarget = 1f;

	[JsonProperty]
	public EnumAICreatureType AiCreatureType;
}
