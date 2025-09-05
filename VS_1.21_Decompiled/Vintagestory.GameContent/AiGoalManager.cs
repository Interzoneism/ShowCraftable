using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class AiGoalManager
{
	private Entity entity;

	private List<AiGoalBase> Goals = new List<AiGoalBase>();

	private AiGoalBase activeGoal;

	public AiGoalManager(Entity entity)
	{
		this.entity = entity;
	}

	public void AddGoal(AiGoalBase goal)
	{
		Goals.Add(goal);
	}

	public void RemoveGoal(AiGoalBase goal)
	{
		Goals.Remove(goal);
	}

	public void OnGameTick(float dt)
	{
		foreach (AiGoalBase goal in Goals)
		{
			if ((activeGoal == null || goal.Priority > activeGoal.PriorityForCancel) && goal.ShouldExecuteAll())
			{
				activeGoal?.FinishExecuteAll(cancelled: true);
				activeGoal = goal;
				goal.StartExecuteAll();
			}
		}
		if (activeGoal != null && !activeGoal.ContinueExecuteAll(dt))
		{
			activeGoal.FinishExecuteAll(cancelled: false);
			activeGoal = null;
		}
		if (entity.World.EntityDebugMode)
		{
			string text = "";
			if (activeGoal != null)
			{
				text = text + AiTaskRegistry.TaskCodes[activeGoal.GetType()] + "(" + activeGoal.Priority + ")";
			}
			entity.DebugAttributes.SetString("AI Goal", (text.Length > 0) ? text : "-");
		}
	}

	internal void Notify(string key, object data)
	{
		for (int i = 0; i < Goals.Count; i++)
		{
			AiGoalBase aiGoalBase = Goals[i];
			if (aiGoalBase.Notify(key, data) && (aiGoalBase == null || aiGoalBase.Priority > activeGoal.PriorityForCancel))
			{
				activeGoal?.FinishExecuteAll(cancelled: true);
				activeGoal = aiGoalBase;
				aiGoalBase.StartExecuteAll();
			}
		}
	}

	internal void OnStateChanged(EnumEntityState beforeState)
	{
		foreach (IAiTask goal in Goals)
		{
			goal.OnStateChanged(beforeState);
		}
	}
}
