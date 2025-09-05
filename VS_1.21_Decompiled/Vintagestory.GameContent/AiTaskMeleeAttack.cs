using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskMeleeAttack : AiTaskBaseTargetable
{
	protected long lastCheckOrAttackMs;

	protected float damage = 2f;

	protected float knockbackStrength = 1f;

	protected float minDist = 1.5f;

	protected float minVerDist = 1f;

	protected float attackAngleRangeDeg = 20f;

	protected bool damageInflicted;

	protected int attackDurationMs = 1500;

	protected int damagePlayerAtMs = 500;

	public EnumDamageType damageType = EnumDamageType.BluntAttack;

	public int damageTier;

	protected float attackRange = 3f;

	protected bool turnToTarget = true;

	private float curTurnRadPerSec;

	private bool didStartAnim;

	public AiTaskMeleeAttack(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		damage = taskConfig["damage"].AsFloat(2f);
		knockbackStrength = taskConfig["knockbackStrength"].AsFloat(GameMath.Sqrt(damage / 4f));
		attackAngleRangeDeg = taskConfig["attackAngleRangeDeg"].AsFloat(20f);
		attackDurationMs = taskConfig["attackDurationMs"].AsInt(1500);
		damagePlayerAtMs = taskConfig["damagePlayerAtMs"].AsInt(1000);
		minDist = taskConfig["minDist"].AsFloat(2f);
		minVerDist = taskConfig["minVerDist"].AsFloat(1f);
		string text = taskConfig["damageType"].AsString();
		if (text != null)
		{
			damageType = (EnumDamageType)Enum.Parse(typeof(EnumDamageType), text, ignoreCase: true);
		}
		damageTier = taskConfig["damageTier"].AsInt();
		entity.WatchedAttributes.GetTreeAttribute("extraInfoText").SetString("dmgTier", Lang.Get("Damage tier: {0}", damageTier));
	}

	public override bool ShouldExecute()
	{
		long elapsedMilliseconds = entity.World.ElapsedMilliseconds;
		if (elapsedMilliseconds - lastCheckOrAttackMs < attackDurationMs || cooldownUntilMs > elapsedMilliseconds)
		{
			return false;
		}
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		Vec3d position = entity.ServerPos.XYZ.Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		int ownGeneration = GetOwnGeneration();
		bool fullyTamed = (float)ownGeneration >= tamingGenerations;
		float num = Math.Max(0f, (tamingGenerations - (float)ownGeneration) / tamingGenerations);
		if (WhenInEmotionState != null)
		{
			num = 1f;
		}
		if (num <= 0f)
		{
			return false;
		}
		if (entity.World.ElapsedMilliseconds - attackedByEntityMs > 30000)
		{
			attackedByEntity = null;
		}
		if (retaliateAttacks && attackedByEntity != null && attackedByEntity.Alive && attackedByEntity.IsInteractable && IsTargetableEntity(attackedByEntity, 15f, ignoreEntityCode: true) && hasDirectContact(attackedByEntity, minDist, minVerDist) && !entity.ToleratesDamageFrom(attackedByEntity))
		{
			targetEntity = attackedByEntity;
		}
		else
		{
			targetEntity = entity.World.GetNearestEntity(position, attackRange * num, attackRange * num, delegate(Entity e)
			{
				if (fullyTamed && (isNonAttackingPlayer(e) || entity.ToleratesDamageFrom(attackedByEntity)))
				{
					return false;
				}
				return IsTargetableEntity(e, 15f) && hasDirectContact(e, minDist, minVerDist);
			});
		}
		lastCheckOrAttackMs = entity.World.ElapsedMilliseconds;
		damageInflicted = false;
		return targetEntity != null;
	}

	public override void StartExecute()
	{
		didStartAnim = false;
		curTurnRadPerSec = entity.GetBehavior<EntityBehaviorTaskAI>().PathTraverser.curTurnRadPerSec;
		if (!turnToTarget)
		{
			base.StartExecute();
		}
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
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
		if (turnToTarget)
		{
			float end = (float)Math.Atan2(serverPos2.X - serverPos.X, serverPos2.Z - serverPos.Z);
			float num = GameMath.AngleRadDistance(entity.ServerPos.Yaw, end);
			entity.ServerPos.Yaw += GameMath.Clamp(num, (0f - curTurnRadPerSec) * dt * GlobalConstants.OverallSpeedMultiplier, curTurnRadPerSec * dt * GlobalConstants.OverallSpeedMultiplier);
			entity.ServerPos.Yaw = entity.ServerPos.Yaw % ((float)Math.PI * 2f);
			flag = Math.Abs(num) < attackAngleRangeDeg * ((float)Math.PI / 180f);
			if (flag && !didStartAnim)
			{
				didStartAnim = true;
				base.StartExecute();
			}
		}
		if (lastCheckOrAttackMs + damagePlayerAtMs > entity.World.ElapsedMilliseconds)
		{
			return true;
		}
		if (!damageInflicted && flag)
		{
			attackTarget();
			damageInflicted = true;
		}
		if (lastCheckOrAttackMs + attackDurationMs > entity.World.ElapsedMilliseconds)
		{
			return true;
		}
		return false;
	}

	protected virtual void attackTarget()
	{
		if (!hasDirectContact(targetEntity, minDist, minVerDist))
		{
			return;
		}
		bool alive = targetEntity.Alive;
		targetEntity.ReceiveDamage(new DamageSource
		{
			Source = EnumDamageSource.Entity,
			SourceEntity = entity,
			Type = damageType,
			DamageTier = damageTier,
			KnockbackStrength = knockbackStrength
		}, damage * GlobalConstants.CreatureDamageModifier);
		if (entity is IMeleeAttackListener meleeAttackListener)
		{
			meleeAttackListener.DidAttack(targetEntity);
		}
		if (alive && !targetEntity.Alive)
		{
			if (!(targetEntity is EntityPlayer))
			{
				entity.WatchedAttributes.SetDouble("lastMealEatenTotalHours", entity.World.Calendar.TotalHours);
			}
			bhEmo?.TryTriggerState("saturated", targetEntity.EntityId);
		}
	}
}
