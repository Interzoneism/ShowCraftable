using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskStayCloseToEntity : AiTaskBase
{
	protected Entity targetEntity;

	protected float moveSpeed = 0.03f;

	protected float range = 8f;

	protected float maxDistance = 3f;

	protected string entityCode;

	protected bool stuck;

	protected bool onlyIfLowerId;

	protected bool allowTeleport;

	protected float teleportAfterRange;

	protected int teleportToRange;

	public int TeleportMaxRange;

	public float minSeekSeconds = 3f;

	protected Vec3d targetOffset = new Vec3d();

	protected Vec3d initialTargetPos;

	public int allowTeleportCount;

	private float executingSeconds;

	private int stuckCounter;

	private long cooldownUntilTotalMs;

	public AiTaskStayCloseToEntity(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		moveSpeed = taskConfig["movespeed"].AsFloat(0.03f);
		range = taskConfig["searchRange"].AsFloat(8f);
		maxDistance = taskConfig["maxDistance"].AsFloat(3f);
		minSeekSeconds = taskConfig["minSeekSeconds"].AsFloat(3f);
		onlyIfLowerId = taskConfig["onlyIfLowerId"].AsBool();
		entityCode = taskConfig["entityCode"].AsString();
		allowTeleport = taskConfig["allowTeleport"].AsBool();
		teleportAfterRange = taskConfig["teleportAfterRange"].AsFloat(30f);
		teleportToRange = taskConfig["teleportToRange"].AsInt(1);
		TeleportMaxRange = taskConfig["teleportMaxRange"].AsInt(int.MaxValue);
	}

	public override bool ShouldExecute()
	{
		if (base.rand.NextDouble() > 0.009999999776482582)
		{
			return false;
		}
		if (stuckCounter > 3)
		{
			stuckCounter = 0;
			cooldownUntilTotalMs = entity.World.ElapsedMilliseconds + 60000;
		}
		if (entity.World.ElapsedMilliseconds < cooldownUntilMs)
		{
			return false;
		}
		if (targetEntity == null || !targetEntity.Alive)
		{
			if (onlyIfLowerId)
			{
				targetEntity = entity.World.GetNearestEntity(entity.ServerPos.XYZ, range, 2f, (Entity e) => e.EntityId < entity.EntityId && e.Code.Path.Equals(entityCode));
			}
			else
			{
				targetEntity = entity.World.GetNearestEntity(entity.ServerPos.XYZ, range, 2f, (Entity e) => e.Code.Path.Equals(entityCode));
			}
		}
		if (targetEntity != null && (!targetEntity.Alive || targetEntity.ShouldDespawn))
		{
			targetEntity = null;
		}
		if (targetEntity == null)
		{
			return false;
		}
		double x = targetEntity.ServerPos.X;
		double y = targetEntity.ServerPos.Y;
		double z = targetEntity.ServerPos.Z;
		return (double)entity.ServerPos.SquareDistanceTo(x, y, z) > (double)(maxDistance * maxDistance);
	}

	public override void StartExecute()
	{
		base.StartExecute();
		executingSeconds = 0f;
		initialTargetPos = targetEntity.ServerPos.XYZ;
		if (targetEntity.ServerPos.DistanceTo(entity.ServerPos) > (double)TeleportMaxRange)
		{
			stuck = true;
			return;
		}
		float xSize = targetEntity.SelectionBox.XSize;
		pathTraverser.NavigateTo_Async(targetEntity.ServerPos.XYZ, moveSpeed, xSize + 0.2f, OnGoalReached, OnStuck, OnNoPath, 1000, 1);
		targetOffset.Set(entity.World.Rand.NextDouble() * 2.0 - 1.0, 0.0, entity.World.Rand.NextDouble() * 2.0 - 1.0);
		stuck = false;
	}

	public override bool CanContinueExecute()
	{
		return pathTraverser.Ready;
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		if (initialTargetPos.DistanceTo(targetEntity.ServerPos.XYZ) > 3f)
		{
			initialTargetPos = targetEntity.ServerPos.XYZ;
			pathTraverser.Retarget();
		}
		double x = targetEntity.ServerPos.X + targetOffset.X;
		double y = targetEntity.ServerPos.Y;
		double z = targetEntity.ServerPos.Z + targetOffset.Z;
		pathTraverser.CurrentTarget.X = x;
		pathTraverser.CurrentTarget.Y = y;
		pathTraverser.CurrentTarget.Z = z;
		float num = entity.ServerPos.SquareDistanceTo(x, y, z);
		if (num < 9f)
		{
			pathTraverser.Stop();
			return false;
		}
		if ((allowTeleport || allowTeleportCount > 0) && executingSeconds > 4f && (num > teleportAfterRange * teleportAfterRange || stuck) && entity.World.Rand.NextDouble() < 0.05)
		{
			tryTeleport();
		}
		executingSeconds += dt;
		if (stuck || !pathTraverser.Active)
		{
			return executingSeconds < minSeekSeconds;
		}
		return true;
	}

	private Vec3d findDecentTeleportPos()
	{
		IBlockAccessor blockAccessor = entity.World.BlockAccessor;
		Random random = entity.World.Rand;
		Vec3d vec3d = new Vec3d();
		BlockPos blockPos = new BlockPos();
		for (int i = teleportToRange; i < teleportToRange + 30; i++)
		{
			float num = GameMath.Clamp((float)i / 5f, 2f, 4.5f);
			double num2 = random.NextDouble() * 2.0 * (double)num - (double)num;
			double num3 = random.NextDouble() * 2.0 * (double)num - (double)num;
			for (int j = 0; j < 8; j++)
			{
				int num4 = (1 - j % 2 * 2) * (int)Math.Ceiling((float)j / 2f);
				vec3d.Set(targetEntity.ServerPos.X + num2, targetEntity.ServerPos.Y + (double)num4, targetEntity.ServerPos.Z + num3);
				blockPos.Set((int)vec3d.X, (int)vec3d.Y, (int)vec3d.Z);
				Block block = blockAccessor.GetBlock(blockPos);
				Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccessor, blockPos);
				if (collisionBoxes != null && collisionBoxes.Length != 0)
				{
					continue;
				}
				JsonObject attributes = block.Attributes;
				if (attributes != null && attributes["insideDamage"].AsInt() > 0)
				{
					continue;
				}
				blockPos.Set((int)vec3d.X, (int)vec3d.Y - 1, (int)vec3d.Z);
				Block block2 = blockAccessor.GetBlock(blockPos);
				collisionBoxes = block2.GetCollisionBoxes(blockAccessor, blockPos);
				if (collisionBoxes == null || collisionBoxes.Length == 0)
				{
					continue;
				}
				JsonObject attributes2 = block2.Attributes;
				if (attributes2 == null || attributes2["insideDamage"].AsInt() <= 0)
				{
					vec3d.Y = (float)((int)vec3d.Y - 1) + collisionBoxes.Max((Cuboidf c) => c.Y2);
					return vec3d;
				}
			}
		}
		return null;
	}

	protected void tryTeleport()
	{
		if ((!allowTeleport && allowTeleportCount <= 0) || targetEntity == null)
		{
			return;
		}
		Vec3d vec3d = findDecentTeleportPos();
		if (vec3d != null)
		{
			entity.TeleportToDouble(vec3d.X, vec3d.Y, vec3d.Z, delegate
			{
				initialTargetPos = targetEntity.ServerPos.XYZ;
				pathTraverser.Retarget();
				allowTeleportCount = Math.Max(0, allowTeleportCount - 1);
			});
		}
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

	protected void OnStuck()
	{
		stuck = true;
	}

	public void OnNoPath()
	{
	}

	protected virtual void OnGoalReached()
	{
	}
}
