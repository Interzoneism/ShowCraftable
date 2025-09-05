using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskFlySwoopAttack : AiTaskBaseTargetable
{
	protected EnumDamageType damageType = EnumDamageType.BluntAttack;

	protected int damageTier;

	protected float damage = 2f;

	protected float knockbackStrength = 1f;

	protected float seekingRangeVer = 25f;

	protected float seekingRangeHor = 25f;

	protected float damageRange = 5f;

	protected float moveSpeed = 0.04f;

	protected TimeSpan attemptToExecuteCooldownMs = TimeSpan.FromMilliseconds(1500.0);

	protected TimeSpan targetRetentionTime = TimeSpan.FromSeconds(30.0);

	protected float firstTimeSwitchDistance = 0.9f;

	protected float secondTimeSwitchDistance = 0.25f;

	protected float secondTimeSwitchMinimumDistance = 0.1f;

	protected float timeSwitchHealthThreshold = 0.75f;

	protected float minVerticalDistance = 9f;

	protected float minHorizontalDistance = 25f;

	protected const float sensePlayerRange = 15f;

	protected const float pathRefreshCooldown = 1f;

	protected const float pathStopRefreshThreshold = 5f;

	protected bool timeSwitchAtTheStart;

	protected int pathLength = 25;

	protected float speedThresholdForDamage = 0.3f;

	protected float timeSwitchProbability = 0.5f;

	protected long globalAttackCooldownMs = 3000L;

	protected long lastCheckOrAttackMs;

	protected HashSet<long> didDamageEntity = new HashSet<long>();

	protected Vec3d beginAttackPos;

	protected List<Vec3d> swoopPath;

	protected EntityBehaviorHealth? healthBehavior;

	protected float pathRefreshAccum;

	protected float pathStopRefreshAccum;

	protected float damageAccum;

	protected double initialDistanceToTarget;

	protected int intendedDimension;

	protected NatFloat timeSwitchRandom;

	protected bool shouldUseTimeSwitchThisTime;

	protected int CurrentDimension => entity.Pos.Dimension;

	protected int TargetDimension => targetEntity?.Pos.Dimension ?? CurrentDimension;

	public AiTaskFlySwoopAttack(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		healthBehavior = entity.GetBehavior<EntityBehaviorHealth>();
		timeSwitchRandom = new NatFloat(0.5f, 0.5f, EnumDistribution.UNIFORM);
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
		timeSwitchHealthThreshold = taskConfig["timeSwitchHealthThreshold"].AsFloat(0.75f);
		firstTimeSwitchDistance = taskConfig["firstTimeSwitchDistance"].AsFloat(0.9f);
		secondTimeSwitchDistance = taskConfig["secondTimeSwitchDistance"].AsFloat(0.25f);
		secondTimeSwitchMinimumDistance = taskConfig["secondTimeSwitchMinimumDistance"].AsFloat(0.1f);
		timeSwitchAtTheStart = taskConfig["timeSwitchAtTheStart"].AsBool();
		pathLength = taskConfig["pathLength"].AsInt(35);
		speedThresholdForDamage = taskConfig["speedThresholdForDamage"].AsFloat(0.3f);
		timeSwitchProbability = taskConfig["timeSwitchProbability"].AsFloat(0.5f);
		globalAttackCooldownMs = taskConfig["globalAttackCooldownMs"].AsInt(3000);
		minVerticalDistance = taskConfig["minVerticalDistance"].AsFloat(9f);
		minHorizontalDistance = taskConfig["minHorizontalDistance"].AsFloat(25f);
	}

	public override bool ShouldExecute()
	{
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
		if (targetEntity == null || !(entity.ServerPos.Y - targetEntity.ServerPos.Y > (double)minVerticalDistance) || !(entity.ServerPos.HorDistanceTo(targetEntity.ServerPos) > (double)minHorizontalDistance))
		{
			return false;
		}
		beginAttackPos = entity.ServerPos.XYZ;
		swoopPath = new List<Vec3d>(getSwoopPath(targetEntity as EntityAgent, pathLength, simplifiedOut: false));
		return pathClear(swoopPath);
	}

	public override void StartExecute()
	{
		didDamageEntity.Clear();
		swoopPath.Clear();
		swoopPath.AddRange(getSwoopPath(targetEntity as EntityAgent, pathLength, simplifiedOut: true));
		pathTraverser.FollowRoute(swoopPath, moveSpeed, 8f, null, null);
		pathStopRefreshAccum = 0f;
		pathRefreshAccum = 0f;
		initialDistanceToTarget = distanceToTarget();
		intendedDimension = CurrentDimension;
		shouldUseTimeSwitchThisTime = timeSwitchRandom.nextFloat() < timeSwitchProbability;
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
		pathStopRefreshAccum += dt;
		pathRefreshAccum += dt;
		if (pathRefreshAccum > 1f && pathStopRefreshAccum < 5f && targetEntity.Pos.Dimension == entity.Pos.Dimension)
		{
			refreshPath();
			pathRefreshAccum = 0f;
		}
		if (distanceToTarget() > (double)(Math.Max(seekingRangeHor, seekingRangeVer) * 2f))
		{
			return false;
		}
		damageAccum += dt;
		if (damageAccum > 0.2f && entity.Pos.Motion.Length() > (double)speedThresholdForDamage)
		{
			damageAccum = 0f;
			attackEntities();
		}
		double num = entity.ServerPos.Motion.Length();
		if (num > 0.01)
		{
			entity.ServerPos.Roll = (float)Math.Asin(GameMath.Clamp((0.0 - entity.ServerPos.Motion.Y) / num, -1.0, 1.0));
		}
		if (!pathTraverser.Active)
		{
			return false;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		pathTraverser.Stop();
		(entity as EntityErel).LastAttackTime = entity.World.ElapsedMilliseconds;
		base.FinishExecute(cancelled);
	}

	protected bool checkGlobalAttackCooldown()
	{
		long lastAttackTime = (entity as EntityErel).LastAttackTime;
		return entity.World.ElapsedMilliseconds - lastAttackTime > globalAttackCooldownMs;
	}

	protected bool pathClear(List<Vec3d> swoopPath)
	{
		int num = 2;
		Vec3d vec3d = new Vec3d();
		for (int i = 0; i < swoopPath.Count; i += num)
		{
			vec3d.Set(swoopPath[i]);
			vec3d.Y -= 1.0;
			if (world.CollisionTester.IsColliding(entity.World.BlockAccessor, entity.CollisionBox, vec3d))
			{
				return false;
			}
		}
		return true;
	}

	protected virtual Vec3d[] getSwoopPath(Entity target, int its, bool simplifiedOut)
	{
		EntityPos entityPos = target.ServerPos.Copy();
		entityPos.Dimension = 0;
		EntityPos serverPos = entity.ServerPos;
		serverPos.Dimension = 0;
		Vec3d vec3d = entityPos.XYZ.AddCopy(target.LocalEyePos);
		Vec3d xYZ = entity.ServerPos.XYZ;
		Vec3d vec3d3;
		Vec3d vec3d2 = (vec3d3 = new Vec3d(xYZ.X, vec3d.Y + 10.0, xYZ.Z));
		Vec3d vec3d4 = vec3d;
		Vec3d vec3d5 = vec3d2 - xYZ;
		Vec3d vec3d6 = vec3d4 - vec3d3;
		int num = (simplifiedOut ? (its / 3) : its);
		Vec3d[] array = new Vec3d[its + num];
		for (int i = 0; i < its; i++)
		{
			double num2 = (double)i / (double)its;
			Vec3d vec3d7 = xYZ + num2 * vec3d5;
			Vec3d vec3d8 = vec3d3 + num2 * vec3d6;
			array[i] = (1.0 - num2) * vec3d7 + num2 * vec3d8;
		}
		xYZ = array[its - 1];
		Vec3d vec3d9 = (entityPos.XYZ - serverPos.XYZ) * 1f;
		Vec3d vec3d10 = (vec3d3 = new Vec3d(vec3d.X + vec3d9.X, vec3d.Y, vec3d.Z + vec3d9.Z));
		vec3d4 = new Vec3d(vec3d.X + vec3d9.X * 1.2999999523162842, vec3d.Y + (beginAttackPos.Y - vec3d.Y) * 0.5, vec3d.Z + vec3d9.Z * 1.2999999523162842);
		vec3d5 = vec3d10 - xYZ;
		vec3d6 = vec3d4 - vec3d3;
		for (int j = 0; j < num; j++)
		{
			double num3 = (double)j / (double)num;
			Vec3d vec3d11 = xYZ + num3 * vec3d5;
			Vec3d vec3d12 = vec3d3 + num3 * vec3d6;
			array[its + j] = (1.0 - num3) * vec3d11 + num3 * vec3d12;
		}
		return array;
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

	protected void updateSwoopPathDimension(int fromDimension, int toDimension)
	{
		double num = (toDimension - fromDimension) * 32768;
		foreach (Vec3d item in swoopPath)
		{
			item.Y += num;
		}
	}

	protected void refreshPath()
	{
		Vec3d[] collection = getSwoopPath(targetEntity as EntityAgent, pathLength, simplifiedOut: true);
		if (pathClear(new List<Vec3d>(collection)))
		{
			swoopPath.Clear();
			swoopPath.AddRange(collection);
		}
	}
}
