using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common.Entities;

public abstract class PModule
{
	public abstract void Initialize(JsonObject config, Entity entity);

	public abstract bool Applicable(Entity entity, EntityPos pos, EntityControls controls);

	public abstract void DoApply(float dt, Entity entity, EntityPos pos, EntityControls controls);
}
