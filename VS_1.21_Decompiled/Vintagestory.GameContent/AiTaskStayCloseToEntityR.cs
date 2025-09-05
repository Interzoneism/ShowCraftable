using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskStayCloseToEntityR : AiTaskBaseTargetableR
{
	protected bool stuck;

	protected FastVec3d targetOffset;

	protected FastVec3d initialTargetPos;

	protected float executingTimeSec;

	protected int stuckCounter;

	private readonly BlockPos blockPosBuffer = new BlockPos(0);

	public virtual float TeleportMaxRange => Config.TeleportMaxRange;

	public virtual int AllowTeleportCount { get; set; }

	private AiTaskStayCloseToEntityConfig Config => GetConfig<AiTaskStayCloseToEntityConfig>();

	public AiTaskStayCloseToEntityR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskStayCloseToEntityConfig>(entity, taskConfig, aiConfig);
	}

	public override bool ShouldExecute()
	{
		if (stuckCounter > 3)
		{
			stuckCounter = 0;
			cooldownUntilMs = entity.World.ElapsedMilliseconds + 60000;
		}
		if (!PreconditionsSatisficed())
		{
			return false;
		}
		if (!SearchForTarget())
		{
			return false;
		}
		if (targetEntity != null)
		{
			float num = entity.ServerPos.SquareDistanceTo(targetEntity.ServerPos);
			float num2 = MinDistanceToTarget(Config.MinRangeToTrigger);
			if (num <= num2 * num2)
			{
				return false;
			}
		}
		return true;
	}

	public override void StartExecute()
	{
		if (targetEntity != null)
		{
			base.StartExecute();
			executingTimeSec = 0f;
			initialTargetPos.Set(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
			pathTraverser.NavigateTo_Async(targetEntity.ServerPos.XYZ, Config.MoveSpeed, MinDistanceToTarget(Config.ExtraMinDistanceToTarget), OnGoalReached, OnStuck, OnNoPath, 1000, 1, Config.AiCreatureType);
			targetOffset.Set(entity.World.Rand.NextDouble() * (double)Config.RandomTargetOffset - (double)(Config.RandomTargetOffset / 2f), 0.0, entity.World.Rand.NextDouble() * (double)Config.RandomTargetOffset - (double)(Config.RandomTargetOffset / 2f));
			stuck = false;
		}
	}

	public override bool ContinueExecute(float dt)
	{
		if (!base.ContinueExecute(dt))
		{
			return false;
		}
		if (targetEntity == null)
		{
			return false;
		}
		if (initialTargetPos.Distance(targetEntity.ServerPos.XYZ) > (double)Config.MinDistanceToRetarget)
		{
			initialTargetPos.Set(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
			pathTraverser.Retarget();
		}
		double x = targetEntity.ServerPos.X + targetOffset.X;
		double y = targetEntity.ServerPos.Y;
		double z = targetEntity.ServerPos.Z + targetOffset.Z;
		pathTraverser.CurrentTarget.X = x;
		pathTraverser.CurrentTarget.Y = y;
		pathTraverser.CurrentTarget.Z = z;
		float num = entity.ServerPos.SquareDistanceTo(x, y, z);
		float num2 = MinDistanceToTarget();
		if (num < num2 * num2)
		{
			pathTraverser.Stop();
			return false;
		}
		if ((Config.AllowTeleport || AllowTeleportCount > 0) && executingTimeSec > Config.TeleportDelaySec && (num > Config.TeleportAfterRange * Config.TeleportAfterRange || stuck) && num < Config.TeleportMaxRange * Config.TeleportMaxRange && base.Rand.NextDouble() < (double)Config.TeleportChance)
		{
			TryTeleport();
		}
		executingTimeSec += dt;
		if (stuck || !pathTraverser.Active)
		{
			return executingTimeSec < Config.MinTimeBeforeGiveUpSec;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		if (stuck)
		{
			stuckCounter++;
		}
		else
		{
			stuckCounter = 0;
		}
		base.FinishExecute(cancelled);
	}

	public override bool CanContinueExecute()
	{
		return pathTraverser.Ready;
	}

	protected virtual bool FindDecentTeleportPos(out FastVec3d teleportPosition)
	{
		teleportPosition = default(FastVec3d);
		if (targetEntity == null)
		{
			return false;
		}
		IBlockAccessor blockAccessor = entity.World.BlockAccessor;
		for (int i = (int)Config.MinTeleportDistanceToTarget * 5; i < (int)(Config.MaxTeleportDistanceToTarget + 1f) * 5; i++)
		{
			float num = GameMath.Clamp((float)i / 5f, Config.MinTeleportDistanceToTarget, Config.MaxTeleportDistanceToTarget);
			double num2 = base.Rand.NextDouble() * 2.0 * (double)num - (double)num;
			double num3 = base.Rand.NextDouble() * 2.0 * (double)num - (double)num;
			for (int j = 0; j < 8; j++)
			{
				int num4 = (1 - j % 2 * 2) * (int)Math.Ceiling((float)j / 2f);
				teleportPosition.Set(targetEntity.ServerPos.X + num2, targetEntity.ServerPos.Y + (double)num4, targetEntity.ServerPos.Z + num3);
				blockPosBuffer.Set((int)teleportPosition.X, (int)teleportPosition.Y, (int)teleportPosition.Z);
				Block block = blockAccessor.GetBlock(blockPosBuffer);
				Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccessor, blockPosBuffer);
				if (collisionBoxes != null && collisionBoxes.Length != 0)
				{
					continue;
				}
				JsonObject attributes = block.Attributes;
				if (attributes != null && attributes["insideDamage"].AsInt() > 0)
				{
					continue;
				}
				blockPosBuffer.Set((int)teleportPosition.X, (int)teleportPosition.Y - 1, (int)teleportPosition.Z);
				Block block2 = blockAccessor.GetBlock(blockPosBuffer);
				collisionBoxes = block2.GetCollisionBoxes(blockAccessor, blockPosBuffer);
				if (collisionBoxes == null || collisionBoxes.Length == 0)
				{
					continue;
				}
				JsonObject attributes2 = block2.Attributes;
				if (attributes2 == null || attributes2["insideDamage"].AsInt() <= 0)
				{
					teleportPosition.Y = (float)((int)teleportPosition.Y - 1) + collisionBoxes.Max((Cuboidf cuboid) => cuboid.Y2);
					return true;
				}
			}
		}
		return false;
	}

	protected virtual void TryTeleport()
	{
		if ((Config.AllowTeleport || AllowTeleportCount > 0) && targetEntity != null && FindDecentTeleportPos(out var teleportPosition))
		{
			entity.TeleportToDouble(teleportPosition.X, teleportPosition.Y, teleportPosition.Z, delegate
			{
				initialTargetPos.Set(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
				pathTraverser.Retarget();
				AllowTeleportCount = Math.Max(0, AllowTeleportCount - 1);
			});
		}
	}

	protected virtual void OnStuck()
	{
		stuck = true;
	}

	protected virtual void OnNoPath()
	{
	}

	protected virtual void OnGoalReached()
	{
		stopTask = true;
	}
}
