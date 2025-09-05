using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskMeleeAttackR : AiTaskBaseTargetableR
{
	protected bool damageInflicted;

	protected float currentTurnRadPerSec;

	protected bool didStartAnimation;

	protected bool fullyTamed;

	public static bool ShowExtraDamageInfo { get; set; } = true;

	private AiTaskMeleeAttackConfig Config => GetConfig<AiTaskMeleeAttackConfig>();

	public AiTaskMeleeAttackR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskMeleeAttackConfig>(entity, taskConfig, aiConfig);
		if (Config.DamageWindowMs.Length != 2)
		{
			string message = $"Error loading AI task config for task '{Config.Code}' and entity '{entity.Code}': damageWindow should be an array of two integers.";
			entity.Api.Logger.Error(message);
			throw new ArgumentException(message);
		}
		int ownGeneration = GetOwnGeneration();
		fullyTamed = (float)ownGeneration >= Config.TamingGenerations;
		SetExtraInfoText();
	}

	public override bool ShouldExecute()
	{
		if (!PreconditionsSatisficed() && (!Config.RetaliateUnconditionally || !base.RecentlyAttacked))
		{
			return false;
		}
		float fearReductionFactor = GetFearReductionFactor();
		if (fearReductionFactor <= 0f)
		{
			return false;
		}
		if (!base.RecentlyAttacked)
		{
			ClearAttacker();
		}
		if (ShouldRetaliate())
		{
			targetEntity = attackedByEntity;
		}
		else
		{
			Vec3d position = entity.ServerPos.XYZ.Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
			targetEntity = partitionUtil.GetNearestEntity(position, Config.SeekingRange * fearReductionFactor, (Entity entity) => IsTargetableEntity(entity, Config.SeekingRange * fearReductionFactor), Config.SearchType);
		}
		return targetEntity != null;
	}

	public override void StartExecute()
	{
		didStartAnimation = false;
		damageInflicted = false;
		currentTurnRadPerSec = pathTraverser.curTurnRadPerSec;
		if (!Config.TurnToTarget)
		{
			base.StartExecute();
		}
	}

	public override bool ContinueExecute(float dt)
	{
		if (targetEntity == null)
		{
			return false;
		}
		EntityPos serverPos = entity.ServerPos;
		EntityPos serverPos2 = targetEntity.ServerPos;
		if (serverPos.Dimension != serverPos2.Dimension)
		{
			return false;
		}
		bool flag = true;
		if (Config.TurnToTarget)
		{
			float end = (float)Math.Atan2(serverPos2.X - serverPos.X, serverPos2.Z - serverPos.Z);
			float num = GameMath.AngleRadDistance(entity.ServerPos.Yaw, end);
			entity.ServerPos.Yaw += GameMath.Clamp(num, (0f - currentTurnRadPerSec) * dt * GlobalConstants.OverallSpeedMultiplier, currentTurnRadPerSec * dt * GlobalConstants.OverallSpeedMultiplier);
			entity.ServerPos.Yaw %= (float)Math.PI * 2f;
			flag = Math.Abs(num) < Config.AttackAngleRangeDeg * ((float)Math.PI / 180f);
			if (flag && !didStartAnimation)
			{
				didStartAnimation = true;
				base.StartExecute();
			}
		}
		if (executionStartTimeMs + Config.DamageWindowMs[0] > entity.World.ElapsedMilliseconds)
		{
			return true;
		}
		if (executionStartTimeMs + Config.DamageWindowMs[0] <= entity.World.ElapsedMilliseconds && executionStartTimeMs + Config.DamageWindowMs[1] >= entity.World.ElapsedMilliseconds && !damageInflicted && flag)
		{
			damageInflicted = AttackTarget();
		}
		if (executionStartTimeMs + Config.AttackDurationMs > entity.World.ElapsedMilliseconds)
		{
			return true;
		}
		return false;
	}

	protected virtual bool AttackTarget()
	{
		if (targetEntity == null)
		{
			return false;
		}
		if (!HasDirectContact(targetEntity, Config.MaxAttackDistance, Config.MaxAttackVerticalDistance))
		{
			return false;
		}
		bool alive = targetEntity.Alive;
		targetEntity.ReceiveDamage(new DamageSource
		{
			Source = EnumDamageSource.Entity,
			SourceEntity = entity,
			Type = Config.DamageType,
			DamageTier = Config.DamageTier,
			KnockbackStrength = Config.KnockbackStrength,
			IgnoreInvFrames = Config.IgnoreInvFrames
		}, Config.Damage * (Config.AffectedByGlobalDamageMultiplier ? GlobalConstants.CreatureDamageModifier : 1f));
		if (entity is IMeleeAttackListener meleeAttackListener)
		{
			meleeAttackListener.DidAttack(targetEntity);
		}
		if (alive && !targetEntity.Alive && Config.EatAfterKill)
		{
			if (Config.PlayerIsMeal || !(targetEntity is EntityPlayer))
			{
				entity.WatchedAttributes.SetDouble("lastMealEatenTotalHours", entity.World.Calendar.TotalHours);
			}
			emotionStatesBehavior?.TryTriggerState("saturated", targetEntity.EntityId);
		}
		return true;
	}

	protected override bool IsTargetableEntity(Entity target, float range)
	{
		if (fullyTamed && (IsNonAttackingPlayer(target) || entity.ToleratesDamageFrom(target)))
		{
			return false;
		}
		if (!base.IsTargetableEntity(target, range))
		{
			return false;
		}
		return HasDirectContact(target, Config.MaxAttackDistance, Config.MaxAttackVerticalDistance);
	}

	protected override bool ShouldRetaliate()
	{
		if (attackedByEntity != null && base.ShouldRetaliate())
		{
			return HasDirectContact(attackedByEntity, Config.MaxAttackDistance, Config.MaxAttackVerticalDistance);
		}
		return false;
	}

	protected void SetExtraInfoText()
	{
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("extraInfoText");
		treeAttribute.SetString("dmgTier", Lang.Get("Damage tier: {0}", Config.DamageTier));
		if (ShowExtraDamageInfo)
		{
			treeAttribute.SetString("dmgDamage", Lang.Get("Damage: {0}", Config.Damage));
			treeAttribute.SetString("dmgType", Lang.Get("Damage type: {0}", Lang.Get($"{Config.DamageType}")));
		}
	}
}
