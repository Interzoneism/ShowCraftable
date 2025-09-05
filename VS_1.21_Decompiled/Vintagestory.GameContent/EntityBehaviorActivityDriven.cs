using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorActivityDriven : EntityBehavior
{
	public delegate EnumInteruptionType ActivitySystemInterruptionHandler();

	private ICoreAPI Api;

	public EntityActivitySystem ActivitySystem;

	private bool active = true;

	private bool wasRunAiActivities;

	public event ActivitySystemInterruptionHandler OnShouldRunActivitySystem;

	public EntityBehaviorActivityDriven(Entity entity)
		: base(entity)
	{
		Api = entity.Api;
		if (!(entity is EntityAgent))
		{
			throw new InvalidOperationException("ActivityDriven behavior only avaialble for EntityAgent classes.");
		}
		ActivitySystem = new EntityActivitySystem(entity as EntityAgent);
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		string p = attributes?["activityCollectionPath"]?.AsString();
		load(p);
	}

	public bool load(string p)
	{
		return ActivitySystem.Load((p == null) ? null : AssetLocation.Create(p, entity.Code.Domain));
	}

	public override void OnEntitySpawn()
	{
		base.OnEntitySpawn();
		if (Api.Side == EnumAppSide.Server)
		{
			setupTaskBlocker();
		}
	}

	public override void OnEntityLoaded()
	{
		base.OnEntityLoaded();
		if (Api.Side == EnumAppSide.Server)
		{
			setupTaskBlocker();
		}
	}

	private void setupTaskBlocker()
	{
		EntityAgent eagent = entity as EntityAgent;
		EntityBehaviorTaskAI behavior = entity.GetBehavior<EntityBehaviorTaskAI>();
		if (behavior != null)
		{
			behavior.TaskManager.OnShouldExecuteTask += delegate(IAiTask task)
			{
				if (task is AiTaskGotoEntity)
				{
					return true;
				}
				return eagent.MountedOn == null && ActivitySystem.ActiveActivitiesBySlot.Values.Any((IEntityActivity a) => a.CurrentAction?.Type == "standardai");
			};
		}
		EntityBehaviorConversable behavior2 = entity.GetBehavior<EntityBehaviorConversable>();
		if (behavior2 != null)
		{
			behavior2.CanConverse += Ebc_CanConverse;
		}
	}

	private bool Ebc_CanConverse(out string errorMessage)
	{
		bool flag = !Api.ModLoader.GetModSystem<VariablesModSystem>().GetVariable(EnumActivityVariableScope.Entity, "tooBusyToTalk", entity).ToBool();
		errorMessage = (flag ? null : "cantconverse-toobusy");
		return flag;
	}

	public override void OnGameTick(float deltaTime)
	{
		if (!entity.Alive)
		{
			return;
		}
		if (!AiRuntimeConfig.RunAiActivities)
		{
			if (wasRunAiActivities)
			{
				ActivitySystem.CancelAll();
			}
			wasRunAiActivities = false;
			return;
		}
		wasRunAiActivities = AiRuntimeConfig.RunAiActivities;
		base.OnGameTick(deltaTime);
		if (this.OnShouldRunActivitySystem != null)
		{
			bool flag = active;
			active = true;
			EnumInteruptionType enumInteruptionType = EnumInteruptionType.None;
			Delegate[] invocationList = this.OnShouldRunActivitySystem.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				EnumInteruptionType enumInteruptionType2 = ((ActivitySystemInterruptionHandler)invocationList[i])();
				if (enumInteruptionType2 > enumInteruptionType)
				{
					enumInteruptionType = enumInteruptionType2;
				}
			}
			active = enumInteruptionType == EnumInteruptionType.None;
			if (flag && !active)
			{
				ActivitySystem.Pause(enumInteruptionType);
			}
			if (!flag && active)
			{
				ActivitySystem.Resume();
			}
		}
		Api.World.FrameProfiler.Mark("behavior-activitydriven-checks");
		if (active)
		{
			ActivitySystem.OnTick(deltaTime);
		}
	}

	public override string PropertyName()
	{
		return "activitydriven";
	}
}
