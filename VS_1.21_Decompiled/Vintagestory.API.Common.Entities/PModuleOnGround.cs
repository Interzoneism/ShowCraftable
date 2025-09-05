using System;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class PModuleOnGround : PModule
{
	private const long MinimumJumpInterval = 500L;

	private long lastJump;

	private double groundDragFactor = 0.30000001192092896;

	private float accum;

	private float coyoteTimer;

	private float antiCoyoteTimer;

	private double motionDeltaX;

	private double motionDeltaZ;

	public override void Initialize(JsonObject config, Entity entity)
	{
		if (config != null)
		{
			groundDragFactor = 0.3 * (double)(float)config["groundDragFactor"].AsDouble(1.0);
		}
	}

	public override bool Applicable(Entity entity, EntityPos pos, EntityControls controls)
	{
		bool flag = entity.OnGround && !entity.Swimming;
		if (flag && antiCoyoteTimer <= 0f)
		{
			coyoteTimer = 0.15f;
		}
		if (coyoteTimer > 0f)
		{
			if (entity.Attributes.GetInt("dmgkb") > 0)
			{
				coyoteTimer = 0f;
				antiCoyoteTimer = 0.16f;
				return flag;
			}
			return true;
		}
		return flag;
	}

	public override void DoApply(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		coyoteTimer -= dt;
		if (antiCoyoteTimer > 0f)
		{
			antiCoyoteTimer = Math.Max(0f, antiCoyoteTimer - dt);
		}
		float dragMultiplier = entity.World.BlockAccessor.GetBlockRaw((int)pos.X, (int)(pos.InternalY - 0.05000000074505806), (int)pos.Z).DragMultiplier;
		float num = Math.Min(1f, accum + dt);
		float num2 = 1f / 60f;
		double walkSpeedMultiplier = (entity as EntityAgent).GetWalkSpeedMultiplier(groundDragFactor);
		double num3 = controls.WalkVector.X * walkSpeedMultiplier;
		double num4 = controls.WalkVector.Z * walkSpeedMultiplier;
		Vec3d motion = pos.Motion;
		double num5 = 1.0 - groundDragFactor;
		while (num > num2)
		{
			num -= num2;
			if (entity.Alive)
			{
				motionDeltaX += (num3 - motionDeltaX) * (double)dragMultiplier;
				motionDeltaZ += (num4 - motionDeltaZ) * (double)dragMultiplier;
				motion.Add(motionDeltaX, 0.0, motionDeltaZ);
			}
			motion.X *= num5;
			motion.Z *= num5;
		}
		accum = num;
		if (controls.Jump && entity.World.ElapsedMilliseconds - lastJump > 500 && entity.Alive)
		{
			EntityPlayer entityPlayer = entity as EntityPlayer;
			lastJump = entity.World.ElapsedMilliseconds;
			float num6 = MathF.Sqrt(MathF.Max(1f, entityPlayer?.Stats.GetBlended("jumpHeightMul") ?? 1f));
			pos.Motion.Y = GlobalConstants.BaseJumpForce * 1f / 60f * num6;
			IPlayer dualCallByPlayer = entityPlayer?.World.PlayerByUid(entityPlayer.PlayerUID);
			entity.PlayEntitySound("jump", dualCallByPlayer, randomizePitch: false);
		}
	}
}
