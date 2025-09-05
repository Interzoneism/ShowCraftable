using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskFlyDiveAttack : AiTaskBaseTargetable
{
	protected float damage = 2f;

	protected EnumDamageType damageType = EnumDamageType.BluntAttack;

	protected int damageTier;

	protected float knockbackStrength = 1f;

	protected long lastCheckOrAttackMs;

	protected float seekingRangeVer = 25f;

	protected float seekingRangeHor = 25f;

	protected float damageRange = 5f;

	protected float moveSpeed = 0.04f;

	protected TimeSpan attemptToExecuteCooldownMs = TimeSpan.FromMilliseconds(1500.0);

	protected TimeSpan targetRetentionTime = TimeSpan.FromSeconds(30.0);

	protected const float minVerticalDistance = 9f;

	protected const float minHorizontalDistance = 20f;

	protected const float sensePlayerRange = 15f;

	protected float diveRange = 20f;

	protected float requireMinRange = 30f;

	protected float diveHeight = 30f;

	protected float timeSwitchProbability = 0.5f;

	protected long globalAttackCooldownMs = 3000L;

	protected HashSet<long> didDamageEntity = new HashSet<long>();

	protected EntityPos targetPos = new EntityPos();

	protected EntityBehaviorHealth? healthBehavior;

	protected float damageAccum;

	protected bool diving;

	protected bool impacted;

	protected double diveDistance = 1.0;

	protected bool shouldUseTimeSwitchThisTime;

	public bool Enabled { get; set; } = true;

	protected int CurrentDimension => entity.Pos.Dimension;

	protected int TargetDimension => targetEntity?.Pos.Dimension ?? CurrentDimension;

	public AiTaskFlyDiveAttack(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		healthBehavior = entity.GetBehavior<EntityBehaviorHealth>();
		moveSpeed = taskConfig["moveSpeed"].AsFloat(0.04f);
		damage = taskConfig["damage"].AsFloat(2f);
		knockbackStrength = taskConfig["knockbackStrength"].AsFloat(GameMath.Sqrt(damage / 2f));
		seekingRangeHor = taskConfig["seekingRangeHor"].AsFloat(25f);
		seekingRangeVer = taskConfig["seekingRangeVer"].AsFloat(25f);
		damageRange = taskConfig["damageRange"].AsFloat(2f);
		damageType = Enum.Parse<EnumDamageType>(taskConfig["damageType"].AsString("BluntAttack"));
		damageTier = taskConfig["damageTier"].AsInt();
		attemptToExecuteCooldownMs = TimeSpan.FromMilliseconds(taskConfig["attemptToExecuteCooldownMs"].AsInt(1500));
		targetRetentionTime = TimeSpan.FromSeconds(taskConfig["targetRetentionTimeSec"].AsInt(30));
		diveHeight = taskConfig["diveHeight"].AsFloat(30f);
		timeSwitchProbability = taskConfig["timeSwitchProbability"].AsFloat(0.5f);
		globalAttackCooldownMs = taskConfig["globalAttackCooldownMs"].AsInt(3000);
	}

	public override bool ShouldExecute()
	{
		if (!Enabled)
		{
			return false;
		}
		long elapsedMilliseconds = entity.World.ElapsedMilliseconds;
		if (cooldownUntilMs > elapsedMilliseconds)
		{
			return false;
		}
		cooldownUntilMs = entity.World.ElapsedMilliseconds + (long)attemptToExecuteCooldownMs.TotalMilliseconds;
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		if (!checkGlobalAttackCooldown())
		{
			return false;
		}
		Vec3d position = entity.ServerPos.XYZ.Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		if (entity.World.ElapsedMilliseconds - attackedByEntityMs > (long)targetRetentionTime.TotalMilliseconds)
		{
			attackedByEntity = null;
		}
		if (retaliateAttacks && attackedByEntity != null && attackedByEntity.Alive && attackedByEntity.IsInteractable && IsTargetableEntity(attackedByEntity, 15f, ignoreEntityCode: true))
		{
			targetEntity = attackedByEntity;
		}
		else
		{
			targetEntity = entity.World.GetNearestEntity(position, seekingRangeHor, seekingRangeVer, (Entity e) => IsTargetableEntity(e, seekingRangeHor) && hasDirectContact(e, seekingRangeHor, seekingRangeVer));
		}
		lastCheckOrAttackMs = entity.World.ElapsedMilliseconds;
		if (targetEntity != null && entity.ServerPos.Y - targetEntity.ServerPos.Y > 9.0)
		{
			return entity.ServerPos.HorDistanceTo(targetEntity.ServerPos) > 20.0;
		}
		return false;
	}

	public override void StartExecute()
	{
		didDamageEntity.Clear();
		targetPos.SetFrom(targetEntity.ServerPos);
		diving = false;
		impacted = false;
		base.StartExecute();
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		if (timeoutExceeded())
		{
			return false;
		}
		updateTargetPosition();
		if (impacted)
		{
			return onImpact();
		}
		if (!diving)
		{
			if (entity.ServerPos.Y - targetPos.Y < (double)diveHeight)
			{
				entity.ServerPos.Motion.Y = 0.15000000596046448;
				entity.ServerPos.Motion.X *= 0.8999999761581421;
				entity.ServerPos.Motion.Z *= 0.8999999761581421;
				followTargetOnFlyUp();
				return true;
			}
			entity.AnimManager.StopAnimation("fly-idle");
			entity.AnimManager.StopAnimation("fly-flapactive");
			entity.AnimManager.StopAnimation("fly-flapcruise");
			entity.AnimManager.StartAnimation("dive");
			diveDistance = distanceToTarget();
			diving = true;
		}
		followTarget();
		if (entity.Collided)
		{
			entity.AnimManager.StopAnimation("dive");
			entity.AnimManager.StartAnimation("slam");
			impacted = true;
			attackEntities();
			return onImpact();
		}
		damageAccum += dt;
		if (damageAccum > 0.2f)
		{
			attackEntities();
			damageAccum = 0f;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		pathTraverser.Stop();
		entity.AnimManager.StartAnimation("fly-idle");
		entity.AnimManager.StopAnimation("slam");
		(entity as EntityErel).LastAttackTime = entity.World.ElapsedMilliseconds;
		base.FinishExecute(cancelled);
	}

	protected bool checkGlobalAttackCooldown()
	{
		long lastAttackTime = (entity as EntityErel).LastAttackTime;
		return entity.World.ElapsedMilliseconds - lastAttackTime > globalAttackCooldownMs;
	}

	protected void updateTargetPosition()
	{
		if (targetEntity.Pos.Dimension == entity.Pos.Dimension)
		{
			targetPos.SetFrom(targetEntity.ServerPos);
		}
	}

	protected bool onImpact()
	{
		entity.ServerPos.Roll = 0f;
		entity.ServerPos.Motion.Set(0.0, 0.0, 0.0);
		RunningAnimation animationState = entity.AnimManager.GetAnimationState("slam");
		if (animationState != null && animationState.AnimProgress > 0.5f)
		{
			entity.AnimManager.StartAnimation("takeoff");
		}
		if (animationState != null)
		{
			return animationState.AnimProgress < 0.6f;
		}
		return true;
	}

	protected void followTargetOnFlyUp()
	{
		Vec3d vec3d = targetPos.XYZ - entity.ServerPos.XYZ;
		vec3d.Normalize();
		entity.ServerPos.Motion.Length();
		entity.ServerPos.Roll = -(float)Math.PI / 12f;
		entity.ServerPos.Yaw = (float)Math.Atan2(vec3d.X, vec3d.Z);
	}

	protected void followTarget()
	{
		Vec3d vec3d = targetPos.XYZ - entity.ServerPos.XYZ;
		Vec3d vec3d2 = vec3d.Normalize();
		entity.ServerPos.Motion.X = vec3d2.X * (double)moveSpeed * 10.0;
		entity.ServerPos.Motion.Y = vec3d2.Y * (double)moveSpeed * 10.0;
		entity.ServerPos.Motion.Z = vec3d2.Z * (double)moveSpeed * 10.0;
		double num = entity.ServerPos.Motion.Length();
		if (num > 0.01)
		{
			entity.ServerPos.Roll = (float)Math.Asin(GameMath.Clamp((0.0 - vec3d2.Y) / num, -1.0, 1.0));
		}
		entity.ServerPos.Yaw = (float)Math.Atan2(vec3d.X, vec3d.Z);
	}

	protected void attackEntities()
	{
		List<Entity> attackableEntities = new List<Entity>();
		entity.Api.ModLoader.GetModSystem<EntityPartitioning>().GetNearestEntity(entity.ServerPos.XYZ, damageRange + 1f, delegate(Entity e)
		{
			if (IsTargetableEntity(e, damageRange) && hasDirectContact(e, damageRange, damageRange) && !didDamageEntity.Contains(entity.EntityId))
			{
				attackableEntities.Add(e);
			}
			return false;
		}, EnumEntitySearchType.Creatures);
		foreach (Entity item in attackableEntities)
		{
			item.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Entity,
				SourceEntity = entity,
				Type = damageType,
				DamageTier = damageTier,
				KnockbackStrength = knockbackStrength
			}, damage * GlobalConstants.CreatureDamageModifier);
			if (entity is IMeleeAttackListener meleeAttackListener)
			{
				meleeAttackListener.DidAttack(item);
			}
			didDamageEntity.Add(entity.EntityId);
		}
	}

	protected double distanceToTarget()
	{
		double num = entity.ServerPos.X - targetEntity.Pos.X;
		double num2 = entity.ServerPos.Y - targetEntity.Pos.Y;
		double num3 = entity.ServerPos.Z - targetEntity.Pos.Z;
		return Math.Sqrt(num * num + num2 * num2 + num3 * num3);
	}
}
