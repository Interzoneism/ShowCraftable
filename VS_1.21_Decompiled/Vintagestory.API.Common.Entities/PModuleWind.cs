using System;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class PModuleWind : PModule
{
	private readonly Vec3d windForce = new Vec3d();

	private bool applyWindForce;

	private float accum;

	public override void Initialize(JsonObject config, Entity entity)
	{
		applyWindForce = entity.World.Config.GetBool("windAffectedEntityMovement");
	}

	public override bool Applicable(Entity entity, EntityPos pos, EntityControls controls)
	{
		if (controls.TriesToMove)
		{
			if (entity.OnGround)
			{
				return !entity.Swimming;
			}
			return false;
		}
		return false;
	}

	public override void DoApply(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if ((accum += dt) > 5f)
		{
			accum = 0f;
			UpdateWindForce(entity);
		}
		pos.Motion.Add(windForce);
	}

	public virtual void UpdateWindForce(Entity entity)
	{
		if (!entity.Alive || !applyWindForce)
		{
			windForce.Set(0.0, 0.0, 0.0);
			return;
		}
		if ((double)entity.World.BlockAccessor.GetRainMapHeightAt((int)entity.Pos.X, (int)entity.Pos.Z) > entity.Pos.Y)
		{
			windForce.Set(0.0, 0.0, 0.0);
			return;
		}
		Vec3d windSpeedAt = entity.World.BlockAccessor.GetWindSpeedAt(entity.Pos.XYZ);
		windForce.X = Math.Max(0.0, Math.Abs(windSpeedAt.X) - 0.8) / 40.0 * (double)Math.Sign(windSpeedAt.X);
		windForce.Y = Math.Max(0.0, Math.Abs(windSpeedAt.Y) - 0.8) / 40.0 * (double)Math.Sign(windSpeedAt.Y);
		windForce.Z = Math.Max(0.0, Math.Abs(windSpeedAt.Z) - 0.8) / 40.0 * (double)Math.Sign(windSpeedAt.Z);
	}
}
