using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskStayCloseToEntityConfig : AiTaskBaseTargetableConfig
{
	[JsonProperty]
	public float MoveSpeed = 0.03f;

	[JsonProperty]
	public float TeleportMaxRange = float.MaxValue;

	[JsonProperty]
	public float MinTimeBeforeGiveUpSec = 3f;

	[JsonProperty]
	public float ExtraMinDistanceToTarget = 1f;

	[JsonProperty]
	public EnumAICreatureType? AiCreatureType = EnumAICreatureType.LandCreature;

	[JsonProperty]
	public float RandomTargetOffset = 2f;

	[JsonProperty]
	public float MinDistanceToRetarget = 3f;

	[JsonProperty]
	public bool AllowTeleport;

	[JsonProperty]
	public float TeleportAfterRange = 30f;

	[JsonProperty]
	public float TeleportDelaySec = 4f;

	[JsonProperty]
	public float TeleportChance = 0.05f;

	[JsonProperty]
	public float MinTeleportDistanceToTarget = 2f;

	[JsonProperty]
	public float MaxTeleportDistanceToTarget = 4.5f;

	[JsonProperty]
	public float MinRangeToTrigger = float.MinValue;

	public override void Init(EntityAgent entity)
	{
		base.Init(entity);
		if (MinRangeToTrigger <= float.MinValue)
		{
			MinRangeToTrigger = ExtraMinDistanceToTarget;
		}
	}
}
