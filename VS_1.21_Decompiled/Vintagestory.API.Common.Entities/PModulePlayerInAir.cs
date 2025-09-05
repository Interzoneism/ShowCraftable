using System;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class PModulePlayerInAir : PModuleInAir
{
	private float airMovingStrengthFalling;

	public override void Initialize(JsonObject config, Entity entity)
	{
		base.Initialize(config, entity);
		airMovingStrengthFalling = AirMovingStrength / 4f;
	}

	public override void ApplyFreeFall(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if (controls.IsClimbing)
		{
			base.ApplyFreeFall(dt, entity, pos, controls);
			return;
		}
		float num = AirMovingStrength * Math.Min(1f, ((EntityPlayer)entity).walkSpeed) * dt * 60f;
		if (!controls.Jump)
		{
			num = airMovingStrengthFalling;
			pos.Motion.X *= (float)Math.Pow(0.9800000190734863, dt * 33f);
			pos.Motion.Z *= (float)Math.Pow(0.9800000190734863, dt * 33f);
		}
		pos.Motion.Add(controls.WalkVector.X * (double)num, controls.WalkVector.Y * (double)num, controls.WalkVector.Z * (double)num);
	}

	public override void ApplyFlying(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if (controls.Gliding)
		{
			double num = Math.Cos(pos.Pitch);
			double num2 = Math.Sin(pos.Pitch);
			double num3 = Math.Cos(pos.Yaw);
			double num4 = Math.Sin(pos.Yaw);
			double num5 = num2 + 0.15;
			controls.GlideSpeed = GameMath.Clamp(controls.GlideSpeed - num5 * (double)dt * 0.25, 0.004999999888241291, 0.75);
			double num6 = GameMath.Clamp(max: (double)entity.Stats.GetBlended("gliderSpeedMax") - 0.8, val: controls.GlideSpeed, min: 0.004999999888241291);
			float blended = entity.Stats.GetBlended("gliderLiftMax");
			double y = Math.Min(num2 * num6, blended);
			pos.Motion.Add((0.0 - num) * num4 * num6, y, (0.0 - num) * num3 * num6);
			pos.Motion.Mul(GameMath.Clamp(1.0 - pos.Motion.Length() * 0.12999999523162842, 0.0, 1.0));
		}
		else
		{
			base.ApplyFlying(dt, entity, pos, controls);
		}
	}
}
