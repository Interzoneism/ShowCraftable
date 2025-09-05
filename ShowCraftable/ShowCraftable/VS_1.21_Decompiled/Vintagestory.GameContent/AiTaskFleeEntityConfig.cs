using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskFleeEntityConfig : AiTaskBaseTargetableConfig
{
	[JsonProperty]
	public float MoveSpeed = 0.02f;

	[JsonProperty]
	public bool IgnoreDeepDayLight;

	[JsonProperty]
	public float FleeingDistance;

	[JsonProperty]
	public int FleeDurationMs = 9000;

	[JsonProperty]
	public int FleeDurationWhenTargetLost = 5000;

	[JsonProperty]
	public float InstaFleeOnDamageChance;

	[JsonProperty]
	public bool SpawnCloserDuringLowStability;

	[JsonProperty]
	public float FleeDistanceReductionIfToleratesDamage = 0.5f;

	[JsonProperty]
	public float ChanceToAdjustDirection = 0.2f;

	[JsonProperty]
	public float ChanceToCheckLightLevel = 0.25f;

	[JsonProperty]
	public float SoundChanceRestoreRate = 0.002f;

	[JsonProperty]
	public float SoundChanceDecreaseRate = 0.2f;

	[JsonProperty]
	public float SoundChanceMinimum = 0.25f;

	[JsonProperty]
	public float RequiredTemporalStability = 0.25f;

	[JsonProperty]
	public float DeepDayLightLevelOffset = -2f;

	public bool LowStabilityAttracted;

	public float FleeSeekRangeDifference = 15f;

	public float MaxSoundChance;

	public override void Init(EntityAgent entity)
	{
		base.Init(entity);
		if (FleeingDistance <= 0f)
		{
			FleeingDistance = SeekingRange + FleeSeekRangeDifference;
		}
		MaxSoundChance = SoundChance;
		FleeSeekRangeDifference = FleeingDistance - SeekingRange;
		int lowStabilityAttracted;
		if (entity.World.Config.GetString("temporalStability").ToBool(defaultValue: true))
		{
			JsonObject attributes = entity.Properties.Attributes;
			lowStabilityAttracted = (((attributes != null && attributes["spawnCloserDuringLowStability"]?.AsBool() == true) || SpawnCloserDuringLowStability) ? 1 : 0);
		}
		else
		{
			lowStabilityAttracted = 0;
		}
		LowStabilityAttracted = (byte)lowStabilityAttracted != 0;
	}
}
