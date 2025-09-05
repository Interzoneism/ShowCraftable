using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class AiTaskComeToOwnerR : AiTaskStayCloseToEntityR
{
	public Entity? TargetOwner
	{
		get
		{
			return targetEntity;
		}
		set
		{
			targetEntity = value;
		}
	}

	public AiTaskComeToOwnerR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
	}

	public override bool ShouldExecute()
	{
		if (!PreconditionsSatisficed())
		{
			return false;
		}
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("ownedby");
		if (treeAttribute == null)
		{
			return false;
		}
		string text = treeAttribute.GetString("uid");
		if (text == null)
		{
			return false;
		}
		targetEntity = entity.World.PlayerByUid(text)?.Entity;
		if (targetEntity == null)
		{
			return false;
		}
		return CanSense(targetEntity, GetSeekingRange());
	}
}
