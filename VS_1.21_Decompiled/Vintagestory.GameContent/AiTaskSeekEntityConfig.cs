using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskSeekEntityConfig : AiTaskBaseTargetableConfig
{
	[JsonProperty]
	public float MoveSpeed = 0.02f;

	[JsonProperty]
	public string? JumpAnimationCode = "jump";

	[JsonProperty]
	public float JumpChance = 1f;

	[JsonProperty]
	public float JumpHeightFactor = 1f;

	[JsonProperty]
	public bool JumpAtTarget;

	[JsonProperty]
	public float ExtraTargetDistance;

	[JsonProperty]
	public float BelowTemperatureSeekingRange = 25f;

	[JsonProperty]
	public float BelowTemperatureThreshold = -99f;

	[JsonProperty]
	public float MaxFollowTimeSec = 60f;

	[JsonProperty]
	public bool AlarmHerd;

	[JsonProperty]
	public float HerdAlarmRange;

	[JsonProperty]
	public EnumAICreatureType? AiCreatureType;

	[JsonProperty]
	public bool StopWhenAttackedByTargetOutsideOfSeekingRange = true;

	[JsonProperty]
	public bool FleeWhenAttackedByTargetOutsideOfSeekingRange = true;

	[JsonProperty]
	public bool RetaliateUnconditionally = true;

	[JsonProperty]
	public float RetaliationSeekingRangeFactor = 1.5f;

	[JsonProperty]
	public float PathUpdateCooldownSec = 0.75f;

	[JsonProperty]
	public float MinDistanceToUpdatePath = 3f;

	[JsonProperty]
	public float MotionAnticipationFactor = 10f;

	[JsonProperty]
	public int JumpAnimationTimeoutMs = 2000;

	[JsonProperty]
	public float[] DistanceToTargetToJump = new float[2] { 0.5f, 4f };

	[JsonProperty]
	public float MaxHeightDifferenceToJump = 0.1f;

	[JsonProperty]
	public int JumpCooldownMs = 3000;

	[JsonProperty]
	public string[] AnimationToStopForJump = new string[2] { "walk", "run" };

	[JsonProperty]
	public float JumpMotionAnticipationFactor = 80f;

	[JsonProperty]
	public float JumpSpeedFactor = 1f;

	[JsonProperty]
	public float AfterJumpSpeedReduction = 0.5f;

	[JsonProperty]
	public bool FleeIfCantReach = true;

	[JsonProperty]
	public bool IgnorePlayerIfFullyTamed;

	[JsonProperty]
	public float MaxVerticalJumpSpeed = 0.13f;

	public override void Init(EntityAgent entity)
	{
		base.Init(entity);
		if (HerdAlarmRange <= 0f)
		{
			HerdAlarmRange = SeekingRange;
		}
	}
}
