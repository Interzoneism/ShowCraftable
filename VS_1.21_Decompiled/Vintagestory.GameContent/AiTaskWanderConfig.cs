using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskWanderConfig : AiTaskBaseConfig
{
	[JsonProperty]
	public double MaxDistanceToSpawn;

	[JsonProperty]
	public int TeleportToSpawnTimeout = 120000;

	[JsonProperty]
	public float NoPlayersRange = 15f;

	[JsonProperty]
	public float MoveSpeed = 0.03f;

	[JsonProperty]
	public float MinDistanceToTarget = 0.12f;

	[JsonProperty]
	public float MaxHeight = 7f;

	[JsonProperty]
	public int PreferredLightLevel = -1;

	[JsonProperty]
	public EnumLightLevelType PreferredLightLevelType = EnumLightLevelType.MaxTimeOfDayLight;

	[JsonProperty]
	private float wanderRangeMin = 3f;

	[JsonProperty]
	private float wanderRangeMax = 30f;

	[JsonProperty]
	private float wanderVerticalRangeMin = 3f;

	[JsonProperty]
	private float wanderVerticalRangeMax = 10f;

	[JsonProperty]
	public bool DoRandomWanderRangeChanges;

	[JsonProperty]
	public int MaxBlocksChecked = 9;

	public NatFloat WanderRangeHorizontal = new NatFloat(0f, 0f, EnumDistribution.UNIFORM);

	public NatFloat WanderRangeVertical = new NatFloat(0f, 0f, EnumDistribution.UNIFORM);

	public bool IgnoreLightLevel;

	public bool StayCloseToSpawn;

	public override void Init(EntityAgent entity)
	{
		base.Init(entity);
		WanderRangeHorizontal = new NatFloat(wanderRangeMin, wanderRangeMax, EnumDistribution.INVEXP);
		WanderRangeVertical = new NatFloat(wanderVerticalRangeMin, wanderVerticalRangeMax, EnumDistribution.INVEXP);
		StayCloseToSpawn = MaxDistanceToSpawn > 0.0;
		IgnoreLightLevel = PreferredLightLevel < 0;
	}
}
