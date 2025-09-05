using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskStayInRangeR : AiTaskBaseTargetableR
{
	private AiTaskStayInRangeConfig Config => GetConfig<AiTaskStayInRangeConfig>();

	public new Entity? TargetEntity
	{
		get
		{
			return targetEntity;
		}
		set
		{
			targetEntity = value;
		}
	}

	public AiTaskStayInRangeR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskStayInRangeConfig>(entity, taskConfig, aiConfig);
	}

	public override bool ShouldExecute()
	{
		if (targetEntity != null)
		{
			return CheckIfOutOfRange();
		}
		if (!PreconditionsSatisficed() && (!Config.RetaliateUnconditionally || !base.RecentlyAttacked))
		{
			return false;
		}
		if (!base.RecentlyAttacked)
		{
			attackedByEntity = null;
		}
		if (!CheckAndResetSearchCooldown())
		{
			return false;
		}
		if (Config.RetaliateAttacks && attackedByEntity != null && attackedByEntity.Alive && attackedByEntity.IsInteractable && CanSense(attackedByEntity, GetSeekingRange()) && !entity.ToleratesDamageFrom(attackedByEntity))
		{
			targetEntity = attackedByEntity;
			return true;
		}
		SearchForTarget();
		if (targetEntity != null)
		{
			return CheckIfOutOfRange();
		}
		return false;
	}

	public override bool ContinueExecute(float dt)
	{
		if (!ContinueExecuteChecks(dt))
		{
			return false;
		}
		if (pathTraverser.Active)
		{
			return true;
		}
		CheckIfOutOfRange(out var tooFar, out var tooNear);
		bool flag = false;
		if (tooFar)
		{
			flag = WalkTowards(-1);
		}
		else if (tooNear)
		{
			flag = WalkTowards(1);
		}
		if (flag)
		{
			return tooFar || tooNear;
		}
		return false;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		targetEntity = null;
	}

	protected virtual bool CheckIfOutOfRange()
	{
		bool tooFar;
		bool tooNear;
		return CheckIfOutOfRange(out tooFar, out tooNear);
	}

	protected virtual bool CheckIfOutOfRange(out bool tooFar, out bool tooNear)
	{
		tooFar = false;
		tooNear = false;
		if (targetEntity == null)
		{
			return false;
		}
		double num = entity.ServerPos.SquareDistanceTo(targetEntity.ServerPos.XYZ);
		tooFar = num >= (double)(Config.TargetRangeMin * Config.TargetRangeMin);
		tooNear = num <= (double)(Config.TargetRangeMax * Config.TargetRangeMax);
		return tooNear | tooFar;
	}

	protected virtual bool WalkTowards(int sign)
	{
		_ = entity.World.BlockAccessor;
		Vec3d xYZ = entity.ServerPos.XYZ;
		Vec3d vec3d = xYZ.SubCopy(targetEntity.ServerPos.X, xYZ.Y, targetEntity.ServerPos.Z).Normalize();
		Vec3d vec3d2 = xYZ + sign * vec3d;
		Vec3d vec3d3 = new Vec3d((double)(int)vec3d2.X + 0.5, (int)vec3d2.Y, (double)(int)vec3d2.Z + 0.5);
		if (CanStepTowards(vec3d2))
		{
			pathTraverser.WalkTowards(vec3d2, Config.MoveSpeed, 0.3f, OnGoalReached, OnStuck, Config.AiCreatureType);
			return true;
		}
		int num = 1 - entity.World.Rand.Next(2) * 2;
		Vec3d vec3d4 = vec3d.RotatedCopy((float)num * ((float)Math.PI / 2f));
		vec3d2 = xYZ + vec3d4;
		vec3d3 = new Vec3d((double)(int)vec3d2.X + 0.5, (int)vec3d2.Y, (double)(int)vec3d2.Z + 0.5);
		if (CanStepTowards(vec3d3))
		{
			pathTraverser.WalkTowards(vec3d2, Config.MoveSpeed, 0.3f, OnGoalReached, OnStuck, Config.AiCreatureType);
			return true;
		}
		Vec3d vec3d5 = vec3d.RotatedCopy((float)(-num) * ((float)Math.PI / 2f));
		vec3d2 = xYZ + vec3d5;
		vec3d3 = new Vec3d((double)(int)vec3d2.X + 0.5, (int)vec3d2.Y, (double)(int)vec3d2.Z + 0.5);
		if (CanStepTowards(vec3d3))
		{
			pathTraverser.WalkTowards(vec3d2, Config.MoveSpeed, 0.3f, OnGoalReached, OnStuck, Config.AiCreatureType);
			return true;
		}
		return false;
	}

	protected virtual bool CanStepTowards(Vec3d nextPos)
	{
		if (targetEntity == null)
		{
			return false;
		}
		Vec3d vec3d = new Vec3d();
		bool flag = world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, nextPos, alsoCheckTouch: false);
		if (flag && !world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, vec3d.Set(nextPos).Add(0.0, Math.Min(1f, stepHeight), 0.0), alsoCheckTouch: false))
		{
			return true;
		}
		if (flag)
		{
			return false;
		}
		if (IsLiquidAt(nextPos) && !Config.CanStepInLiquid)
		{
			return false;
		}
		if (world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, vec3d.Set(nextPos).Add(0.0, -1.1, 0.0), alsoCheckTouch: false))
		{
			nextPos.Y -= 1.0;
			return true;
		}
		if (IsLiquidAt(vec3d) && !Config.CanStepInLiquid)
		{
			return false;
		}
		bool flag2 = world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, vec3d.Set(nextPos).Add(0.0, -2.1, 0.0), alsoCheckTouch: false);
		if (flag2 && entity.ServerPos.Y - targetEntity.ServerPos.Y >= 1.0)
		{
			nextPos.Y -= 2.0;
			return true;
		}
		if (IsLiquidAt(vec3d) && !Config.CanStepInLiquid)
		{
			return false;
		}
		bool flag3 = world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, vec3d.Set(nextPos).Add(0.0, -3.1, 0.0), alsoCheckTouch: false);
		if (!flag2 && flag3 && entity.ServerPos.Y - targetEntity.ServerPos.Y >= 2.0)
		{
			nextPos.Y -= 3.0;
			return true;
		}
		return false;
	}

	protected virtual bool IsLiquidAt(Vec3d pos)
	{
		BlockPos blockPos = new BlockPos(0);
		blockPos.SetAndCorrectDimension(pos);
		return entity.World.BlockAccessor.GetBlock(blockPos).IsLiquid();
	}

	protected virtual void OnStuck()
	{
	}

	protected virtual void OnGoalReached()
	{
	}
}
