using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common.Entities;

public class PModuleKnockback : PModule
{
	public override void Initialize(JsonObject config, Entity entity)
	{
	}

	public override bool Applicable(Entity entity, EntityPos pos, EntityControls controls)
	{
		return true;
	}

	public override void DoApply(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if (entity.Attributes.GetInt("dmgkb") == 1)
		{
			double num = entity.WatchedAttributes.GetDouble("kbdirX");
			double num2 = entity.WatchedAttributes.GetDouble("kbdirY");
			double num3 = entity.WatchedAttributes.GetDouble("kbdirZ");
			pos.Motion.X += num;
			pos.Motion.Y += num2;
			pos.Motion.Z += num3;
			entity.Attributes.SetInt("dmgkb", 0);
		}
	}
}
