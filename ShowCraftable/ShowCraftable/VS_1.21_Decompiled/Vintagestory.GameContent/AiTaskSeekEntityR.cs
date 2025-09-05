using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class AiTaskSeekEntityR : AiTaskBaseTargetableR
{
	protected int pathSearchDepth = 3500;

	protected int pathDeepSearchDepth = 10000;

	protected float chanceOfDeepSearch = 0.05f;

	protected int updatePathDepth = 2000;

	protected int circlePathSearchDepth = 3500;

	protected float currentSeekingRange;

	protected float currentFollowTimeSec;

	protected float lastPathUpdateSecondsSec;

	protected EnumAttackPattern attackPattern;

	protected long finishedMs;

	protected bool jumpAnimationOn;

	protected long jumpedMs;

	protected readonly Dictionary<long, int> futilityCounters = new Dictionary<long, int>();

	protected readonly Vec3d targetPosition = new Vec3d();

	protected readonly Vec3d previousPosition = new Vec3d();

	protected bool updatedPathAfterLanding = true;

	protected Vec3d jumpHorizontalVelocity = new Vec3d();

	private readonly Vec3d posBuffer = new Vec3d();

	public override string Id => "seekentity";

	private AiTaskSeekEntityConfig Config => GetConfig<AiTaskSeekEntityConfig>();

	public AiTaskSeekEntityR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskSeekEntityConfig>(entity, taskConfig, aiConfig);
	}

	public override bool ShouldExecute()
	{
		if (jumpAnimationOn && entity.World.ElapsedMilliseconds - finishedMs > Config.JumpAnimationTimeoutMs)
		{
			entity.AnimManager.StopAnimation("jump");
			jumpAnimationOn = false;
		}
		if (!PreconditionsSatisficed() && (!Config.RetaliateUnconditionally || !base.RecentlyAttacked))
		{
			return false;
		}
		SetSeekingRange();
		if (!base.RecentlyAttacked)
		{
			ClearAttacker();
		}
		if (ShouldRetaliate() && attackedByEntity != null)
		{
			targetEntity = attackedByEntity;
			targetPosition.SetWithDimension(attackedByEntity.ServerPos);
			AlarmHerd();
			return true;
		}
		if (!CheckAndResetSearchCooldown())
		{
			return false;
		}
		SearchForTarget();
		if (targetEntity == null)
		{
			return false;
		}
		AlarmHerd();
		targetPosition.SetWithDimension(targetEntity.ServerPos);
		if (entity.ServerPos.SquareDistanceTo(targetPosition) <= (double)MinDistanceToTarget(Config.ExtraTargetDistance))
		{
			return false;
		}
		return true;
	}

	public override void StartExecute()
	{
		if (targetEntity != null)
		{
			base.StartExecute();
			currentFollowTimeSec = 0f;
			attackPattern = EnumAttackPattern.DirectAttack;
			int searchDepth = pathSearchDepth;
			if (world.Rand.NextDouble() < (double)chanceOfDeepSearch)
			{
				searchDepth = pathDeepSearchDepth;
			}
			pathTraverser.NavigateTo_Async(targetEntity.Pos.XYZ, Config.MoveSpeed, MinDistanceToTarget(Config.ExtraTargetDistance), OnGoalReached, OnStuck, OnSeekUnable, searchDepth, 1, Config.AiCreatureType);
			previousPosition.SetWithDimension(entity.Pos);
		}
	}

	public override bool ContinueExecute(float dt)
	{
		if (targetEntity == null)
		{
			return false;
		}
		if (!base.ContinueExecute(dt))
		{
			return false;
		}
		currentFollowTimeSec += dt;
		lastPathUpdateSecondsSec += dt;
		if (Config.JumpAtTarget && !updatedPathAfterLanding && entity.OnGround && entity.Collided)
		{
			pathTraverser.NavigateTo(targetPosition, Config.MoveSpeed, MinDistanceToTarget(Config.ExtraTargetDistance), OnGoalReached, OnStuck, null, giveUpWhenNoPath: false, updatePathDepth, 1, Config.AiCreatureType);
			lastPathUpdateSecondsSec = 0f;
			updatedPathAfterLanding = true;
		}
		if (!Config.JumpAtTarget || entity.OnGround || updatedPathAfterLanding)
		{
			UpdatePath();
		}
		if (Config.JumpAtTarget && !updatedPathAfterLanding && !entity.OnGround)
		{
			entity.ServerPos.Motion.X = jumpHorizontalVelocity.X;
			entity.ServerPos.Motion.Z = jumpHorizontalVelocity.Z;
		}
		RestoreMainAnimation();
		if (attackPattern == EnumAttackPattern.DirectAttack)
		{
			pathTraverser.CurrentTarget.X = targetEntity.ServerPos.X;
			pathTraverser.CurrentTarget.Y = targetEntity.ServerPos.InternalY;
			pathTraverser.CurrentTarget.Z = targetEntity.ServerPos.Z;
		}
		if (PerformJump())
		{
			updatedPathAfterLanding = false;
			pathTraverser.Stop();
		}
		double distanceToTarget = GetDistanceToTarget();
		EntityPlayer obj = targetEntity as EntityPlayer;
		bool flag = obj != null && obj.Player?.WorldData.CurrentGameMode == EnumGameMode.Creative && !Config.TargetPlayerInAllGameModes;
		float num = MinDistanceToTarget(Config.ExtraTargetDistance);
		bool flag2 = pathTraverser.Active || !updatedPathAfterLanding;
		if (targetEntity.Alive && !flag && flag2 && currentFollowTimeSec < Config.MaxFollowTimeSec && distanceToTarget < (double)currentSeekingRange)
		{
			if (!(distanceToTarget > (double)num))
			{
				if (targetEntity is EntityAgent entityAgent)
				{
					return entityAgent.ServerControls.TriesToMove;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		finishedMs = entity.World.ElapsedMilliseconds;
		pathTraverser.Stop();
		active = false;
	}

	public override bool CanContinueExecute()
	{
		return pathTraverser.Ready;
	}

	public override bool Notify(string key, object data)
	{
		if (key == "seekEntity")
		{
			targetEntity = (Entity)data;
			targetPosition.SetWithDimension(targetEntity.ServerPos);
			return true;
		}
		return false;
	}

	public override void OnEntityHurt(DamageSource source, float damage)
	{
		base.OnEntityHurt(source, damage);
		if (!active || targetEntity != source.GetCauseEntity() || targetEntity == null || targetEntity.ServerPos.DistanceTo(entity.ServerPos) <= (double)currentSeekingRange)
		{
			return;
		}
		if (Config.StopWhenAttackedByTargetOutsideOfSeekingRange)
		{
			stopTask = true;
		}
		if (Config.FleeWhenAttackedByTargetOutsideOfSeekingRange)
		{
			entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager.AllTasks.ForEach(delegate(IAiTask t)
			{
				(t as AiTaskFleeEntity)?.InstaFleeFrom(targetEntity);
			});
		}
	}

	protected override bool CanSense(Entity target, double range)
	{
		if (!base.CanSense(target, range))
		{
			return false;
		}
		if (futilityCounters != null && futilityCounters.TryGetValue(target.EntityId, out var value) && value > 0)
		{
			value -= 2;
			futilityCounters[target.EntityId] = value;
			return false;
		}
		return true;
	}

	protected override bool SearchForTarget()
	{
		bool fullyTamed = (float)GetOwnGeneration() >= Config.TamingGenerations;
		posBuffer.SetWithDimension(entity.ServerPos);
		targetEntity = partitionUtil.GetNearestEntity(posBuffer, currentSeekingRange, (Entity potentialTarget) => (!(Config.IgnorePlayerIfFullyTamed && fullyTamed) || (!IsNonAttackingPlayer(potentialTarget) && !entity.ToleratesDamageFrom(attackedByEntity))) && IsTargetableEntity(potentialTarget, currentSeekingRange), Config.SearchType);
		return targetEntity != null;
	}

	protected virtual void OnSeekUnable()
	{
		if (targetPosition.DistanceTo(entity.ServerPos.XYZ) < currentSeekingRange && !TryCircleTarget())
		{
			OnCircleTargetUnable();
		}
	}

	protected virtual void OnCircleTargetUnable()
	{
		if (targetEntity != null && Config.FleeIfCantReach)
		{
			taskAiBehavior.TaskManager.GetTasks<AiTaskFleeEntity>().Foreach(delegate(AiTaskFleeEntity task)
			{
				task.InstaFleeFrom(targetEntity);
			});
		}
	}

	protected virtual bool TryCircleTarget()
	{
		attackPattern = EnumAttackPattern.CircleTarget;
		float num = (float)Math.Atan2(entity.ServerPos.X - targetPosition.X, entity.ServerPos.Z - targetPosition.Z);
		for (int i = 0; i < 3; i++)
		{
			double value = (double)num + 0.5 + world.Rand.NextDouble() / 2.0;
			double num2 = 4.0 + world.Rand.NextDouble() * 6.0;
			double x = GameMath.Sin(value) * num2;
			double z = GameMath.Cos(value) * num2;
			targetPosition.Add(x, 0.0, z);
			int num3 = 0;
			bool flag = false;
			BlockPos blockPos = new BlockPos((int)targetPosition.X, (int)targetPosition.Y, (int)targetPosition.Z);
			int num4 = 0;
			while (num3 < 5)
			{
				if (world.BlockAccessor.GetBlockBelow(blockPos, num4).SideSolid[BlockFacing.UP.Index] && !world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, new Vec3d((double)blockPos.X + 0.5, blockPos.Y - num4 + 1, (double)blockPos.Z + 0.5), alsoCheckTouch: false))
				{
					flag = true;
					targetPosition.Y -= num4;
					targetPosition.Y += 1.0;
					break;
				}
				if (world.BlockAccessor.GetBlockAbove(blockPos, num4).SideSolid[BlockFacing.UP.Index] && !world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, new Vec3d((double)blockPos.X + 0.5, blockPos.Y + num4 + 1, (double)blockPos.Z + 0.5), alsoCheckTouch: false))
				{
					flag = true;
					targetPosition.Y += num4;
					targetPosition.Y += 1.0;
					break;
				}
				num3++;
				num4++;
			}
			if (flag)
			{
				pathTraverser.NavigateTo_Async(targetPosition.Clone(), Config.MoveSpeed, MinDistanceToTarget(Config.ExtraTargetDistance), OnGoalReached, OnStuck, OnCircleTargetUnable, circlePathSearchDepth, 1, Config.AiCreatureType);
				return true;
			}
		}
		return false;
	}

	protected virtual double GetDistanceToTarget()
	{
		if (targetEntity == null)
		{
			return double.MaxValue;
		}
		Cuboidd cuboidd = targetEntity.SelectionBox.ToDouble().Translate(targetEntity.ServerPos.X, targetEntity.ServerPos.InternalY, targetEntity.ServerPos.Z);
		posBuffer.SetWithDimension(entity.ServerPos);
		posBuffer.Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		return cuboidd.ShortestDistanceFrom(posBuffer);
	}

	protected virtual void UpdatePath()
	{
		if (targetEntity != null && attackPattern == EnumAttackPattern.DirectAttack && lastPathUpdateSecondsSec >= Config.PathUpdateCooldownSec && targetPosition.SquareDistanceTo(targetEntity.ServerPos.X, targetEntity.ServerPos.InternalY, targetEntity.ServerPos.Z) >= Config.MinDistanceToUpdatePath * Config.MinDistanceToUpdatePath)
		{
			targetPosition.Set(targetEntity.ServerPos.X + targetEntity.ServerPos.Motion.X * (double)Config.MotionAnticipationFactor, targetEntity.ServerPos.InternalY, targetEntity.ServerPos.Z + targetEntity.ServerPos.Motion.Z * (double)Config.MotionAnticipationFactor);
			pathTraverser.NavigateTo(targetPosition, Config.MoveSpeed, MinDistanceToTarget(Config.ExtraTargetDistance), OnGoalReached, OnStuck, null, giveUpWhenNoPath: false, updatePathDepth, 1, Config.AiCreatureType);
			lastPathUpdateSecondsSec = 0f;
		}
	}

	protected virtual void RestoreMainAnimation()
	{
		if (Config.AnimationMeta == null)
		{
			return;
		}
		if (Config.JumpAtTarget && !entity.AnimManager.IsAnimationActive(Config.AnimationMeta.Code))
		{
			RunningAnimation animationState = entity.AnimManager.Animator.GetAnimationState(Config.JumpAnimationCode);
			if (animationState == null || !animationState.Active)
			{
				Config.AnimationMeta.EaseInSpeed = 1f;
				Config.AnimationMeta.EaseOutSpeed = 1f;
				entity.AnimManager.StartAnimation(Config.AnimationMeta);
			}
		}
		if (jumpAnimationOn && entity.World.ElapsedMilliseconds - finishedMs > Config.JumpAnimationTimeoutMs)
		{
			entity.AnimManager.StopAnimation(Config.JumpAnimationCode);
			Config.AnimationMeta.EaseInSpeed = 1f;
			Config.AnimationMeta.EaseOutSpeed = 1f;
			entity.AnimManager.StartAnimation(Config.AnimationMeta);
		}
	}

	protected virtual void PlayJumpAnimation()
	{
		if (Config.JumpAnimationCode != null)
		{
			string[] animationToStopForJump = Config.AnimationToStopForJump;
			foreach (string code in animationToStopForJump)
			{
				entity.AnimManager.StopAnimation(code);
				entity.AnimManager.StopAnimation(code);
			}
			entity.AnimManager.StartAnimation(new AnimationMetaData
			{
				Animation = Config.JumpAnimationCode,
				Code = Config.JumpAnimationCode
			}.Init());
			jumpAnimationOn = true;
		}
	}

	protected virtual bool PerformJump()
	{
		if (targetEntity == null)
		{
			return false;
		}
		double distanceToTarget = GetDistanceToTarget();
		EntityPlayer obj = targetEntity as EntityPlayer;
		if ((obj != null && obj.Player?.WorldData.CurrentGameMode == EnumGameMode.Creative && !Config.TargetPlayerInAllGameModes) || !Config.JumpAtTarget || base.Rand.NextDouble() > (double)Config.JumpChance)
		{
			return false;
		}
		bool flag = entity.World.ElapsedMilliseconds - jumpedMs < Config.JumpCooldownMs;
		bool result = false;
		if (distanceToTarget >= (double)Config.DistanceToTargetToJump[0] && distanceToTarget <= (double)Config.DistanceToTargetToJump[1] && !flag && (double)Config.MaxHeightDifferenceToJump >= entity.ServerPos.Y - targetEntity.ServerPos.Y)
		{
			Vec3d vec3d = new Vec3d(entity.ServerPos.Motion.X, 0.0, entity.ServerPos.Motion.Z);
			double val = vec3d.Length();
			double num = (targetEntity.ServerPos.X - entity.ServerPos.X + targetEntity.ServerPos.Motion.X * (double)Config.JumpMotionAnticipationFactor) * (double)Config.JumpSpeedFactor * 0.032999999821186066 * 0.5;
			double num2 = (targetEntity.ServerPos.Z - entity.ServerPos.Z + targetEntity.ServerPos.Motion.Z * (double)Config.JumpMotionAnticipationFactor) * (double)Config.JumpSpeedFactor * 0.032999999821186066 * 0.5;
			double y = (double)Config.JumpHeightFactor * GameMath.Max(Config.MaxVerticalJumpSpeed, (targetEntity.ServerPos.Y - entity.ServerPos.Y) * 0.032999999821186066);
			vec3d.Set(num, 0.0, num2).Normalize().Mul(val);
			jumpHorizontalVelocity.Set(num + entity.ServerPos.Motion.X, 0.0, num2 + entity.ServerPos.Motion.Z);
			entity.ServerPos.Motion.X = vec3d.X;
			entity.ServerPos.Motion.Z = vec3d.Z;
			entity.ServerPos.Motion.Add(num, y, num2);
			float yaw = (float)Math.Atan2(num, num2);
			entity.ServerPos.Yaw = yaw;
			PlayJumpAnimation();
			jumpedMs = entity.World.ElapsedMilliseconds;
			finishedMs = entity.World.ElapsedMilliseconds;
			result = true;
		}
		if (flag && !entity.Collided && distanceToTarget < (double)Config.DistanceToTargetToJump[0])
		{
			entity.ServerPos.Motion *= Config.AfterJumpSpeedReduction;
		}
		return result;
	}

	protected virtual void AlarmHerd()
	{
		if (!Config.AlarmHerd || entity.HerdId == 0L)
		{
			return;
		}
		posBuffer.SetWithDimension(entity.ServerPos);
		entity.World.GetNearestEntity(posBuffer, Config.HerdAlarmRange, Config.HerdAlarmRange, delegate(Entity target)
		{
			if (target.EntityId != entity.EntityId && target is EntityAgent { Alive: not false } entityAgent && entityAgent.HerdId == entity.HerdId)
			{
				entityAgent.Notify("seekEntity", targetEntity);
			}
			return false;
		});
	}

	protected virtual void OnStuck()
	{
		stopTask = true;
	}

	protected virtual void OnGoalReached()
	{
		if (targetEntity == null || attackPattern != EnumAttackPattern.DirectAttack)
		{
			return;
		}
		if ((double)previousPosition.SquareDistanceTo(entity.Pos) < 0.001)
		{
			futilityCounters.TryGetValue(targetEntity.EntityId, out var value);
			value++;
			futilityCounters[targetEntity.EntityId] = value;
			if (value > 19)
			{
				return;
			}
		}
		previousPosition.SetWithDimension(entity.Pos);
		pathTraverser.Retarget();
	}

	protected virtual float SetSeekingRange()
	{
		currentSeekingRange = Config.SeekingRange;
		if (Config.BelowTemperatureThreshold > -99f && entity.World.BlockAccessor.GetClimateAt(entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, entity.World.Calendar.TotalDays).Temperature <= Config.BelowTemperatureThreshold)
		{
			currentSeekingRange = Config.BelowTemperatureSeekingRange;
		}
		if (Config.RetaliationSeekingRangeFactor != 1f && Config.RetaliateAttacks && attackedByEntity != null && attackedByEntity.Alive && attackedByEntity.IsInteractable && CanSense(attackedByEntity, currentSeekingRange * Config.RetaliationSeekingRangeFactor) && !entity.ToleratesDamageFrom(attackedByEntity))
		{
			currentSeekingRange *= Config.RetaliationSeekingRangeFactor;
		}
		currentSeekingRange *= GetFearReductionFactor();
		return currentSeekingRange;
	}
}
