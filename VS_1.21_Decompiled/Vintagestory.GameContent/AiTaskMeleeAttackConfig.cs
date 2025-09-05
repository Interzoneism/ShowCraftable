using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskMeleeAttackConfig : AiTaskBaseTargetableConfig
{
	[JsonProperty]
	public float Damage;

	[JsonProperty]
	public float KnockbackStrength = float.MinValue;

	[JsonProperty]
	public EnumDamageType DamageType = EnumDamageType.BluntAttack;

	[JsonProperty]
	public int DamageTier;

	[JsonProperty]
	public float MaxAttackDistance = 2f;

	[JsonProperty]
	public float MaxAttackVerticalDistance = 1f;

	[JsonProperty]
	public float AttackAngleRangeDeg = 20f;

	[JsonProperty]
	public int AttackDurationMs = 1000;

	[JsonProperty]
	public int[] DamageWindowMs = new int[2] { 0, 2147483647 };

	[JsonProperty]
	public bool TurnToTarget = true;

	[JsonProperty]
	public bool EatAfterKill = true;

	[JsonProperty]
	public bool PlayerIsMeal;

	[JsonProperty]
	public bool IgnoreInvFrames = true;

	[JsonProperty]
	public bool AffectedByGlobalDamageMultiplier = true;

	[JsonProperty]
	public bool RetaliateUnconditionally;

	public override void Init(EntityAgent entity)
	{
		base.Init(entity);
		if (KnockbackStrength <= float.MinValue)
		{
			KnockbackStrength = ((Damage >= 0f) ? GameMath.Sqrt(Damage / 4f) : GameMath.Sqrt((0f - Damage) / 4f));
		}
		int num = MaxCooldownMs - MinCooldownMs;
		MinCooldownMs = Math.Max(MinCooldownMs, AttackDurationMs);
		MaxCooldownMs = Math.Max(Math.Max(MaxCooldownMs, MinCooldownMs + num), AttackDurationMs);
	}
}
