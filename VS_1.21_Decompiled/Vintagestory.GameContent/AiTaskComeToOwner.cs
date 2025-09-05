using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class AiTaskComeToOwner : AiTaskStayCloseToEntity
{
	private long lastExecutedMs;

	public AiTaskComeToOwner(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		minSeekSeconds = 10f;
	}

	public override bool ShouldExecute()
	{
		if (entity.WatchedAttributes.GetTreeAttribute("ownedby") == null)
		{
			lastExecutedMs = -99999L;
			return false;
		}
		if ((float)(entity.World.ElapsedMilliseconds - lastExecutedMs) / 1000f < 20f)
		{
			return base.ShouldExecute();
		}
		return false;
	}

	public override void StartExecute()
	{
		lastExecutedMs = entity.World.ElapsedMilliseconds;
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("ownedby");
		if (treeAttribute != null)
		{
			string playerUid = treeAttribute.GetString("uid");
			targetEntity = entity.World.PlayerByUid(playerUid)?.Entity;
			if (targetEntity != null)
			{
				float xSize = targetEntity.SelectionBox.XSize;
				pathTraverser.NavigateTo_Async(targetEntity.ServerPos.XYZ, moveSpeed, xSize + 0.2f, OnGoalReached, base.OnStuck, null, 1000, 1);
				targetOffset.Set(entity.World.Rand.NextDouble() * 2.0 - 1.0, 0.0, entity.World.Rand.NextDouble() * 2.0 - 1.0);
				stuck = false;
			}
			base.StartExecute();
		}
	}

	public override bool CanContinueExecute()
	{
		return pathTraverser.Ready;
	}

	public override bool ContinueExecute(float dt)
	{
		if (targetEntity != null)
		{
			return base.ContinueExecute(dt);
		}
		return false;
	}
}
