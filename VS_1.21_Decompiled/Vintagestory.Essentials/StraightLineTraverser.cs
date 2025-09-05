using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Essentials;

public class StraightLineTraverser : PathTraverserBase
{
	private float minTurnAnglePerSec;

	private float maxTurnAnglePerSec;

	private Vec3f targetVec = new Vec3f();

	private Vec3d prevPos = new Vec3d();

	public StraightLineTraverser(EntityAgent entity)
		: base(entity)
	{
		if (entity?.Properties.Server?.Attributes?.GetTreeAttribute("pathfinder") != null)
		{
			minTurnAnglePerSec = (float)entity.Properties.Server.Attributes.GetTreeAttribute("pathfinder").GetDecimal("minTurnAnglePerSec", 250.0);
			maxTurnAnglePerSec = (float)entity.Properties.Server.Attributes.GetTreeAttribute("pathfinder").GetDecimal("maxTurnAnglePerSec", 450.0);
		}
		else
		{
			minTurnAnglePerSec = 250f;
			maxTurnAnglePerSec = 450f;
		}
	}

	protected override bool BeginGo()
	{
		entity.Controls.Forward = true;
		entity.ServerControls.Forward = true;
		curTurnRadPerSec = minTurnAnglePerSec + (float)entity.World.Rand.NextDouble() * (maxTurnAnglePerSec - minTurnAnglePerSec);
		curTurnRadPerSec *= 0.87266463f;
		stuckCounter = 0;
		return true;
	}

	public override void OnGameTick(float dt)
	{
		if (!Active)
		{
			return;
		}
		double num = ((entity.Properties.Habitat == EnumHabitat.Land) ? target.SquareDistanceTo(entity.ServerPos.X, target.Y, entity.ServerPos.Z) : target.SquareDistanceTo(entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z));
		if (num < (double)(TargetDistance * TargetDistance))
		{
			Stop();
			OnGoalReached?.Invoke();
			return;
		}
		bool flag = (entity.CollidedVertically && entity.Controls.IsClimbing) || entity.ServerPos.SquareDistanceTo(prevPos) < 2.5E-05 || (entity.CollidedHorizontally && entity.ServerPos.Motion.Y <= 0.0);
		prevPos.Set(entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z);
		stuckCounter = (flag ? (stuckCounter + 1) : 0);
		if (GlobalConstants.OverallSpeedMultiplier > 0f && (float)stuckCounter > 20f / GlobalConstants.OverallSpeedMultiplier)
		{
			Stop();
			OnStuck?.Invoke();
			return;
		}
		EntityControls entityControls = ((entity.MountedOn == null) ? entity.Controls : entity.MountedOn.Controls);
		if (entityControls != null)
		{
			targetVec.Set((float)(target.X - entity.ServerPos.X), (float)(target.Y - entity.ServerPos.InternalY), (float)(target.Z - entity.ServerPos.Z));
			float desiredYaw = 0f;
			if (num >= 0.01)
			{
				desiredYaw = (float)Math.Atan2(targetVec.X, targetVec.Z);
			}
			float nowMoveSpeed = movingSpeed;
			if (num < 1.0)
			{
				nowMoveSpeed = Math.Max(0.005f, movingSpeed * Math.Max((float)num, 0.2f));
			}
			yawToMotion(dt, entityControls, desiredYaw, nowMoveSpeed);
		}
	}

	private void yawToMotion(float dt, EntityControls controls, float desiredYaw, float nowMoveSpeed)
	{
		float num = GameMath.AngleRadDistance(entity.ServerPos.Yaw, desiredYaw);
		float num2 = curTurnRadPerSec * dt * GlobalConstants.OverallSpeedMultiplier * movingSpeed;
		entity.ServerPos.Yaw += GameMath.Clamp(num, 0f - num2, num2);
		entity.ServerPos.Yaw = entity.ServerPos.Yaw % ((float)Math.PI * 2f);
		double z = Math.Cos(entity.ServerPos.Yaw);
		double x = Math.Sin(entity.ServerPos.Yaw);
		controls.WalkVector.Set(x, GameMath.Clamp(targetVec.Y, -1f, 1f), z);
		controls.WalkVector.Mul(nowMoveSpeed * GlobalConstants.OverallSpeedMultiplier / Math.Max(1f, num * 3f));
		if (entity.Properties.RotateModelOnClimb && entity.Controls.IsClimbing && entity.ClimbingOnFace != null && entity.Alive)
		{
			BlockFacing climbingOnFace = entity.ClimbingOnFace;
			if (Math.Sign(climbingOnFace.Normali.X) == Math.Sign(controls.WalkVector.X))
			{
				controls.WalkVector.X = 0.0;
			}
			if (Math.Sign(climbingOnFace.Normali.Z) == Math.Sign(controls.WalkVector.Z))
			{
				controls.WalkVector.Z = 0.0;
			}
		}
		if (entity.Swimming)
		{
			controls.FlyVector.Set(controls.WalkVector);
			Vec3d xYZ = entity.Pos.XYZ;
			Block blockRaw = entity.World.BlockAccessor.GetBlockRaw((int)xYZ.X, (int)xYZ.Y, (int)xYZ.Z, 2);
			Block blockRaw2 = entity.World.BlockAccessor.GetBlockRaw((int)xYZ.X, (int)(xYZ.Y + 1.0), (int)xYZ.Z, 2);
			float num3 = GameMath.Clamp((float)(int)xYZ.Y + (float)blockRaw.LiquidLevel / 8f + (blockRaw2.IsLiquid() ? 1.125f : 0f) - (float)xYZ.Y - (float)entity.SwimmingOffsetY, 0f, 1f);
			num3 = Math.Min(1f, num3 + 0.075f);
			controls.FlyVector.Y = GameMath.Clamp(controls.FlyVector.Y, 0.0020000000949949026, 0.004000000189989805) * (double)num3;
			if (entity.CollidedHorizontally)
			{
				controls.FlyVector.Y = 0.05000000074505806;
			}
		}
	}

	public override void Stop()
	{
		Active = false;
		entity.Controls.Forward = false;
		entity.ServerControls.Forward = false;
		entity.Controls.WalkVector.Set(0.0, 0.0, 0.0);
		stuckCounter = 0;
	}
}
