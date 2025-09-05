using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskShootAtEntityConfig : AiTaskBaseTargetableConfig
{
	[JsonProperty]
	public bool Immobile;

	[JsonProperty]
	public float MaxThrowingAngleDeg;

	[JsonProperty]
	public bool RetaliateUnconditionally = true;

	[JsonProperty]
	public float MaxTurnAngleDeg = 360f;

	[JsonProperty]
	public float SpawnAngleDeg;

	[JsonProperty]
	public float DefaultMinTurnAngleDegPerSec = 250f;

	[JsonProperty]
	public float DefaultMaxTurnAngleDegPerSec = 450f;

	[JsonProperty]
	public int ThrowAtMs = 1000;

	[JsonProperty]
	public float VerticalRangeFactor = 0.5f;

	[JsonProperty]
	public AssetLocation ProjectileCode = new AssetLocation("thrownstone-{rock}");

	[JsonProperty]
	public AssetLocation ProjectileItem = new AssetLocation("stone-{rock}");

	[JsonProperty]
	public bool NonCollectible = true;

	[JsonProperty]
	public float ProjectileDamage = 1f;

	[JsonProperty]
	public int ProjectileDamageTier;

	[JsonProperty]
	public EnumDamageType ProjectileDamageType = EnumDamageType.BluntAttack;

	[JsonProperty]
	public bool IgnoreInvFrames = true;

	[JsonProperty]
	public float YawDispersionDeg;

	[JsonProperty]
	public float PitchDispersionDeg;

	[JsonProperty]
	public EnumDistribution DispersionDistribution = EnumDistribution.GAUSSIAN;

	[JsonProperty]
	public float MaxYawDispersionDeg;

	[JsonProperty]
	public float MaxPitchDispersionDeg;

	[JsonProperty]
	public float DispersionReductionSpeedDeg;

	[JsonProperty]
	public bool ReplaceRockVariant = true;

	[JsonProperty]
	public float DropOnImpactChance = 1f;

	[JsonProperty]
	public bool DamageStackOnImpact;

	[JsonProperty]
	public AssetLocation? ShootSound;

	[JsonProperty]
	public double ProjectileSpeed = 10.0;

	[JsonProperty]
	public double ProjectileGravityFactor = 1.0;

	public float MaxTurnAngleRad => MaxTurnAngleDeg * ((float)Math.PI / 180f);

	public float SpawnAngleRad => SpawnAngleDeg * ((float)Math.PI / 180f);

	public float MaxThrowingAngleRad => MaxThrowingAngleDeg * ((float)Math.PI / 180f);

	public override void Init(EntityAgent entity)
	{
		base.Init(entity);
		if (ShootSound != null)
		{
			ShootSound = ShootSound.WithPathPrefixOnce("sounds/");
		}
	}
}
