using System;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class PModuleInAir : PModule
{
	public float AirMovingStrength = 0.05f;

	public double WallDragFactor = 0.30000001192092896;

	public override void Initialize(JsonObject config, Entity entity)
	{
		if (config != null)
		{
			WallDragFactor = 0.3 * (double)(float)config["wallDragFactor"].AsDouble(1.0);
			AirMovingStrength = (float)config["airMovingStrength"].AsDouble(0.05);
		}
	}

	public override bool Applicable(Entity entity, EntityPos pos, EntityControls controls)
	{
		if ((!entity.Collided && !entity.FeetInLiquid) || controls.IsFlying)
		{
			return entity.Alive;
		}
		return false;
	}

	public override void DoApply(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if (controls.IsFlying)
		{
			ApplyFlying(dt, entity, pos, controls);
		}
		else
		{
			ApplyFreeFall(dt, entity, pos, controls);
		}
	}

	public virtual void ApplyFreeFall(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if (controls.IsClimbing)
		{
			pos.Motion.Add(controls.WalkVector);
			pos.Motion.Scale(Math.Pow(1.0 - WallDragFactor, dt * 60f));
		}
		else
		{
			float num = AirMovingStrength * dt * 60f;
			Vec3d walkVector = controls.WalkVector;
			pos.Motion.Add(walkVector.X * (double)num, walkVector.Y * (double)num, walkVector.Z * (double)num);
		}
	}

	public virtual void ApplyFlying(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		Vec3d flyVector = controls.FlyVector;
		double num = flyVector.Y;
		if (controls.Up || controls.Down)
		{
			float num2 = Math.Min(0.2f, dt) * GlobalConstants.BaseMoveSpeed * controls.MovespeedMultiplier / 2f;
			num = (controls.Up ? num2 : 0f) + (controls.Down ? (0f - num2) : 0f);
		}
		if (num > 0.0 && pos.Y % 32768.0 > 24576.0)
		{
			num = 0.0;
		}
		pos.Motion.Add(flyVector.X, num, flyVector.Z);
	}
}
