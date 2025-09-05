using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.Essentials;

namespace Vintagestory.GameContent;

public class EntityBehaviorGoalAI : EntityBehavior
{
	public AiGoalManager goalManager;

	public PathTraverserBase PathTraverser;

	public EntityBehaviorGoalAI(Entity entity)
		: base(entity)
	{
		goalManager = new AiGoalManager(entity);
	}

	public override void Initialize(EntityProperties properties, JsonObject aiconfig)
	{
		if (!(entity is EntityAgent))
		{
			entity.World.Logger.Error("The goal ai currently only works on entities inheriting from EntityAgent. Will ignore loading goals for entity {0} ", entity.Code);
			return;
		}
		PathTraverser = new StraightLineTraverser(entity as EntityAgent);
		JsonObject[] array = aiconfig["aigoals"]?.AsArray();
		if (array == null)
		{
			return;
		}
		JsonObject[] array2 = array;
		foreach (JsonObject jsonObject in array2)
		{
			string text = jsonObject["code"]?.AsString();
			if (!AiGoalRegistry.GoalTypes.TryGetValue(text, out var value))
			{
				entity.World.Logger.Error("Goal with code {0} for entity {1} does not exist. Ignoring.", text, entity.Code);
			}
			else
			{
				AiGoalBase aiGoalBase = (AiGoalBase)Activator.CreateInstance(value, (EntityAgent)entity);
				aiGoalBase.LoadConfig(jsonObject, aiconfig);
				goalManager.AddGoal(aiGoalBase);
			}
		}
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.State == EnumEntityState.Active)
		{
			PathTraverser.OnGameTick(deltaTime);
			goalManager.OnGameTick(deltaTime);
			entity.World.FrameProfiler.Mark("entity-ai");
		}
	}

	public override void OnStateChanged(EnumEntityState beforeState, ref EnumHandling handled)
	{
		goalManager.OnStateChanged(beforeState);
	}

	public override void Notify(string key, object data)
	{
		goalManager.Notify(key, data);
	}

	public override string PropertyName()
	{
		return "goalai";
	}
}
