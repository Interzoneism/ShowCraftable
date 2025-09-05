using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.Essentials;

namespace Vintagestory.GameContent;

public class EntityBehaviorTaskAI : EntityBehavior
{
	public AiTaskManager TaskManager = new AiTaskManager(entity);

	public WaypointsTraverser? PathTraverser;

	public EntityBehaviorTaskAI(Entity entity)
		: base(entity)
	{
	}

	public override string PropertyName()
	{
		return "taskai";
	}

	public override void OnEntitySpawn()
	{
		base.OnEntitySpawn();
		TaskManager.OnEntitySpawn();
	}

	public override void OnEntityLoaded()
	{
		base.OnEntityLoaded();
		TaskManager.OnEntityLoaded();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		TaskManager.OnEntityDespawn(despawn);
	}

	public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
	{
		base.OnEntityReceiveDamage(damageSource, ref damage);
		TaskManager.OnEntityHurt(damageSource, damage);
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		ILogger logger = entity.World.Logger;
		if (!(entity is EntityAgent entityAgent))
		{
			logger.Error($"The task ai currently only works on entities inheriting from EntityAgent. Will ignore loading tasks for entity {entity.Code}.");
			return;
		}
		TaskManager.Shuffle = attributes["shuffle"].AsBool();
		string value = attributes["aiCreatureType"].AsString("Default");
		if (!Enum.TryParse<EnumAICreatureType>(value, out var result))
		{
			result = EnumAICreatureType.Default;
			logger.Warning($"Entity {entity.Code} Task AI, invalid aiCreatureType '{value}'. Will default to 'Default'.");
		}
		PathTraverser = new WaypointsTraverser(entityAgent, result);
		JsonObject[] array = attributes["aitasks"]?.AsArray();
		if (array == null)
		{
			return;
		}
		JsonObject[] array2 = array;
		foreach (JsonObject jsonObject in array2)
		{
			JsonObject jsonObject2 = jsonObject["enabled"];
			if (jsonObject2 != null && !jsonObject2.AsBool(defaultValue: true))
			{
				continue;
			}
			string text = jsonObject["code"]?.AsString();
			if (text == null)
			{
				logger.Error($"Task does not have 'code' specified, for entity '{entity.Code}', will skip it.");
				continue;
			}
			if (!AiTaskRegistry.TaskTypes.TryGetValue(text, out Type value2))
			{
				logger.Error($"Task with code {text} for entity {entity.Code} does not exist, will skip it.");
				continue;
			}
			IAiTask aiTask;
			try
			{
				aiTask = (IAiTask)Activator.CreateInstance(value2, entityAgent, jsonObject, attributes);
			}
			catch
			{
				logger.Error($"Task with code '{text}' for entity '{entity.Code}': failed to instantiate task, possible error in task config json.");
				throw;
			}
			if (aiTask != null)
			{
				TaskManager.AddTask(aiTask);
				continue;
			}
			logger.Error($"Task with code {text} for entity {entity.Code}: failed to instantiate task.");
		}
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		TaskManager.AfterInitialize();
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.State == EnumEntityState.Active && entity.Alive)
		{
			entity.World.FrameProfiler.Mark("ai-init");
			PathTraverser?.OnGameTick(deltaTime);
			entity.World.FrameProfiler.Mark("ai-pathfinding");
			entity.World.FrameProfiler.Enter("ai-tasks");
			TaskManager.OnGameTick(deltaTime);
			entity.World.FrameProfiler.Leave();
		}
	}

	public override void OnStateChanged(EnumEntityState beforeState, ref EnumHandling handling)
	{
		TaskManager.OnStateChanged(beforeState);
	}

	public override void Notify(string key, object data)
	{
		TaskManager.Notify(key, data);
	}
}
