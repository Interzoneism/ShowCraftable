using System;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class PModuleGravity : PModule
{
	private double gravityPerSecond = GlobalConstants.GravityPerSecond;

	public override void Initialize(JsonObject config, Entity entity)
	{
		if (config != null)
		{
			gravityPerSecond = GlobalConstants.GravityPerSecond * (float)config["gravityFactor"].AsDouble(1.0);
		}
	}

	public override bool Applicable(Entity entity, EntityPos pos, EntityControls controls)
	{
		switch (entity.Properties.Habitat)
		{
		case EnumHabitat.Air:
			return false;
		case EnumHabitat.Sea:
		case EnumHabitat.Underwater:
			if (entity.Swimming)
			{
				return false;
			}
			break;
		}
		if (controls.IsFlying && !controls.Gliding)
		{
			return false;
		}
		if (!controls.IsClimbing)
		{
			return entity.ApplyGravity;
		}
		return false;
	}

	public override void DoApply(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if ((!entity.Swimming || !controls.TriesToMove || !entity.Alive) && pos.Y > -100.0)
		{
			double num = (gravityPerSecond + Math.Max(0.0, -0.014999999664723873 * pos.Motion.Y)) * (double)(entity.FeetInLiquid ? 0.33f : 1f) * (double)dt;
			pos.Motion.Y -= num * GameMath.Clamp(1.0 - 50.0 * controls.GlideSpeed * controls.GlideSpeed, 0.0, 1.0);
		}
	}
}
