using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class AiTaskFlyLeave : AiTaskFlyWander
{
	public bool AllowExecute;

	public AiTaskFlyLeave(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
	}

	public override bool ShouldExecute()
	{
		if (AllowExecute)
		{
			return base.ShouldExecute();
		}
		return false;
	}
}
