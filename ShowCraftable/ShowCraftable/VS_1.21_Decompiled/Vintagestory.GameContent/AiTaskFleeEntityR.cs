using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskFleeEntityR : AiTaskBaseTargetableR
{
	protected Vec3d targetPos = new Vec3d();

	protected Vec3d ownPos = new Vec3d();

	protected float targetYaw;

	protected long fleeStartMs;

	protected bool stuck;

	protected float currentFleeingDistance;

	protected bool instaFleeNow;

	protected const float minimumPathTraversalTolerance = 0.5f;

	private readonly Vec3d tmpVec3 = new Vec3d();

	private readonly Vec3d tmpVec2 = new Vec3d();

	private readonly Vec3d tmpVec1 = new Vec3d();

	public override bool AggressiveTargeting => false;

	private AiTaskFleeEntityConfig Config => GetConfig<AiTaskFleeEntityConfig>();

	public AiTaskFleeEntityR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskFleeEntityConfig>(entity, taskConfig, aiConfig);
	}

	public virtual void InstaFleeFrom(Entity fromEntity)
	{
		instaFleeNow = true;
		targetEntity = fromEntity;
	}

	public override bool ShouldExecute()
	{
		Config.SoundChance = Math.Min(Config.MaxSoundChance, Config.SoundChance + Config.SoundChanceRestoreRate);
		if (instaFleeNow)
		{
			return TryInstaFlee();
		}
		if (!PreconditionsSatisficed())
		{
			return false;
		}
		ownPos.SetWithDimension(entity.ServerPos);
		float adjustedRange = ((Config.WhenInEmotionState != null) ? 1f : GetFearReductionFactor()) * Config.SeekingRange;
		entity.World.FrameProfiler.Mark("task-fleeentity-shouldexecute-init");
		if (Config.LowStabilityAttracted)
		{
			targetEntity = partitionUtil.GetNearestEntity(ownPos, adjustedRange, delegate(Entity entity)
			{
				if (!(entity is EntityAgent))
				{
					return false;
				}
				if (!IsTargetableEntity(entity, adjustedRange))
				{
					return false;
				}
				return !(entity is EntityPlayer) || entity.WatchedAttributes.GetDouble("temporalStability", 1.0) > (double)Config.RequiredTemporalStability;
			}, Config.SearchType) as EntityAgent;
		}
		else
		{
			targetEntity = partitionUtil.GetNearestEntity(ownPos, adjustedRange, (Entity entity) => IsTargetableEntity(entity, adjustedRange), Config.SearchType);
		}
		currentFleeingDistance = Config.FleeingDistance;
		entity.World.FrameProfiler.Mark("task-fleeentity-shouldexecute-entitysearch");
		if (targetEntity != null)
		{
			if (entity.ToleratesDamageFrom(targetEntity))
			{
				currentFleeingDistance *= Config.FleeDistanceReductionIfToleratesDamage;
			}
			currentFleeingDistance += (entity.SelectionBox.XSize + targetEntity.SelectionBox.XSize) / 2f;
			float yaw = (float)Math.Atan2(targetEntity.ServerPos.X - entity.ServerPos.X, targetEntity.ServerPos.Z - entity.ServerPos.Z);
			UpdateTargetPosFleeMode(targetPos, yaw);
			return true;
		}
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		Config.SoundChance = Math.Max(Config.SoundChanceMinimum, Config.SoundChance - Config.SoundChanceDecreaseRate);
		float targetDistance = Math.Max(0.5f, entity.SelectionBox.XSize / 2f);
		pathTraverser.WalkTowards(targetPos, Config.MoveSpeed, targetDistance, OnGoalReached, OnStuck);
		fleeStartMs = entity.World.ElapsedMilliseconds;
		stuck = false;
	}

	public override bool ContinueExecute(float dt)
	{
		if (!base.ContinueExecute(dt))
		{
			return false;
		}
		if (world.Rand.NextDouble() <= (double)Config.ChanceToAdjustDirection)
		{
			float yaw = ((targetEntity == null) ? (0f - targetYaw) : ((float)Math.Atan2(targetEntity.ServerPos.X - entity.ServerPos.X, targetEntity.ServerPos.Z - entity.ServerPos.Z)));
			tmpVec3.Set(targetPos);
			UpdateTargetPosFleeMode(tmpVec3, yaw);
			pathTraverser.CurrentTarget.X = tmpVec3.X;
			pathTraverser.CurrentTarget.Y = tmpVec3.Y;
			pathTraverser.CurrentTarget.Z = tmpVec3.Z;
			pathTraverser.Retarget();
		}
		if (targetEntity != null && entity.ServerPos.SquareDistanceTo(targetEntity.ServerPos) > currentFleeingDistance * currentFleeingDistance)
		{
			return false;
		}
		if (targetEntity == null && entity.World.ElapsedMilliseconds - fleeStartMs > Config.FleeDurationWhenTargetLost)
		{
			return false;
		}
		if (world.Rand.NextDouble() < (double)Config.ChanceToCheckLightLevel)
		{
			return CheckEntityLightLevel();
		}
		if (!stuck && (targetEntity == null || targetEntity.Alive) && entity.World.ElapsedMilliseconds - fleeStartMs < Config.FleeDurationMs)
		{
			return pathTraverser.Active;
		}
		return false;
	}

	public override void FinishExecute(bool cancelled)
	{
		pathTraverser.Stop();
		base.FinishExecute(cancelled);
	}

	public override void OnEntityHurt(DamageSource source, float damage)
	{
		base.OnEntityHurt(source, damage);
		if (source.Type != EnumDamageType.Heal && entity.World.Rand.NextDouble() < (double)Config.InstaFleeOnDamageChance)
		{
			instaFleeNow = true;
			targetEntity = source.GetCauseEntity();
		}
	}

	protected override bool CheckEntityLightLevel()
	{
		if (entity.Attributes.GetBool("ignoreDaylightFlee"))
		{
			return false;
		}
		if (Config.IgnoreDeepDayLight && entity.ServerPos.Y < (double)((float)world.SeaLevel + Config.DeepDayLightLevelOffset))
		{
			return false;
		}
		return base.CheckEntityLightLevel();
	}

	protected virtual bool TryInstaFlee()
	{
		if (targetEntity == null || entity.ServerPos.DistanceTo(targetEntity.ServerPos) > (double)Config.SeekingRange)
		{
			float num = GameMath.Cos(entity.ServerPos.Yaw);
			float num2 = GameMath.Sin(entity.ServerPos.Yaw);
			double num3 = 200.0;
			targetPos.Set(entity.ServerPos.X + (double)num2 * num3, entity.ServerPos.Y, entity.ServerPos.Z + (double)num * num3);
			targetYaw = entity.ServerPos.Yaw;
			targetEntity = null;
		}
		else
		{
			currentFleeingDistance = (float)entity.ServerPos.DistanceTo(targetEntity.ServerPos) + Config.FleeSeekRangeDifference;
			if (entity.ToleratesDamageFrom(targetEntity))
			{
				currentFleeingDistance /= Config.FleeDistanceReductionIfToleratesDamage;
			}
			UpdateTargetPosFleeMode(targetPos, entity.ServerPos.Yaw);
		}
		instaFleeNow = false;
		return true;
	}

	protected virtual void OnStuck()
	{
		stuck = true;
	}

	protected virtual void OnGoalReached()
	{
		pathTraverser.Retarget();
	}

	protected void UpdateTargetPosFleeMode(Vec3d targetPos, float yaw)
	{
		tmpVec1.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		tmpVec1.Ahead(0.9, 0f, yaw);
		if (Traversable(tmpVec1))
		{
			targetPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10.0, 0f, yaw);
			return;
		}
		tmpVec1.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		tmpVec1.Ahead(0.9, 0f, yaw - (float)Math.PI / 2f);
		if (Traversable(tmpVec1))
		{
			targetPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10.0, 0f, yaw - (float)Math.PI / 2f);
			return;
		}
		tmpVec1.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		tmpVec1.Ahead(0.9, 0f, yaw + (float)Math.PI / 2f);
		if (Traversable(tmpVec1))
		{
			targetPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10.0, 0f, yaw + (float)Math.PI / 2f);
			return;
		}
		tmpVec1.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		tmpVec1.Ahead(0.9, 0f, yaw + (float)Math.PI);
		targetPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10.0, 0f, yaw + (float)Math.PI);
	}

	protected bool Traversable(Vec3d pos)
	{
		if (world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, pos, alsoCheckTouch: false))
		{
			return !world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, tmpVec2.Set(pos).Add(0.0, Math.Min(1f, stepHeight), 0.0), alsoCheckTouch: false);
		}
		return true;
	}
}
