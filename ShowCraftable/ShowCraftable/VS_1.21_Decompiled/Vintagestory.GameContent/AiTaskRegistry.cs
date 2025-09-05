using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public static class AiTaskRegistry
{
	public static readonly Dictionary<string, Type> TaskTypes;

	public static readonly Dictionary<Type, string> TaskCodes;

	public static void Register<TTask>(string code) where TTask : IAiTask
	{
		TaskTypes[code] = typeof(TTask);
		TaskCodes[typeof(TTask)] = code;
	}

	static AiTaskRegistry()
	{
		TaskTypes = new Dictionary<string, Type>();
		TaskCodes = new Dictionary<Type, string>();
		Register<AiTaskWander>("wander");
		Register<AiTaskLookAround>("lookaround");
		Register<AiTaskMeleeAttack>("meleeattack");
		Register<AiTaskSeekEntity>("seekentity");
		Register<AiTaskFleeEntity>("fleeentity");
		Register<AiTaskStayCloseToEntity>("stayclosetoentity");
		Register<AiTaskGetOutOfWater>("getoutofwater");
		Register<AiTaskIdle>("idle");
		Register<AiTaskSeekFoodAndEat>("seekfoodandeat");
		Register<AiTaskSeekBlockAndLay>("seekblockandlay");
		Register<AiTaskUseInventory>("useinventory");
		Register<AiTaskMeleeAttackTargetingEntity>("meleeattacktargetingentity");
		Register<AiTaskSeekTargetingEntity>("seektargetingentity");
		Register<AiTaskStayCloseToGuardedEntity>("stayclosetoguardedentity");
		Register<AiTaskJealousMeleeAttack>("jealousmeleeattack");
		Register<AiTaskJealousSeekEntity>("jealousseekentity");
		Register<AiTaskLookAtEntity>("lookatentity");
		Register<AiTaskGotoEntity>("gotoentity");
		Register<AiTaskWanderR>("wander-r");
		Register<AiTaskBellAlarmR>("bellalarm-r");
		Register<AiTaskComeToOwnerR>("cometoowner-r");
		Register<AiTaskEatHeldItemR>("eathelditem-r");
		Register<AiTaskFleeEntityR>("fleeentity-r");
		Register<AiTaskGetOutOfWaterR>("getoutofwater-r");
		Register<AiTaskIdleR>("idle-r");
		Register<AiTaskLookAroundR>("lookaround-r");
		Register<AiTaskLookAtEntityR>("lookatentity-r");
		Register<AiTaskMeleeAttackR>("meleeattack-r");
		Register<AiTaskSeekFoodAndEatR>("seekfoodandeat-r");
		Register<AiTaskSeekEntityR>("seekentity-r");
		Register<AiTaskShootAtEntityR>("shootatentity-r");
		Register<AiTaskStayCloseToEntityR>("stayclosetoentity-r");
		Register<AiTaskStayInRangeR>("stayinrange-r");
		Register<AiTaskTurretModeR>("turretmode-r");
	}
}
