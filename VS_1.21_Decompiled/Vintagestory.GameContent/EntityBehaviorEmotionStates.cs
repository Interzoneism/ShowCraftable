using System;
using System.Collections.Generic;
using System.Linq;
using VSEssentialsMod.Entity.AI.Task;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Essentials;

namespace Vintagestory.GameContent;

public class EntityBehaviorEmotionStates : EntityBehavior
{
	private EmotionState[] availableStates;

	public Dictionary<string, ActiveEmoState> ActiveStatesByCode = new Dictionary<string, ActiveEmoState>();

	private TreeAttribute entityAttr;

	private float healthRel;

	private float tickAccum;

	private EntityPartitioning epartSys;

	private EnumCreatureHostility _enumCreatureHostility;

	private PathfinderTask pathtask;

	private int nopathEmoStateid;

	private long sourceEntityId;

	public EntityBehaviorEmotionStates(Entity entity)
		: base(entity)
	{
		if (entity.Attributes.HasAttribute("emotionstates"))
		{
			entityAttr = entity.Attributes["emotionstates"] as TreeAttribute;
		}
		else
		{
			entity.Attributes["emotionstates"] = (entityAttr = new TreeAttribute());
		}
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		base.Initialize(properties, typeAttributes);
		JsonObject[] array = typeAttributes["states"].AsArray();
		availableStates = new EmotionState[array.Length];
		int num = 0;
		JsonObject[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			EmotionState emotionState = array2[i].AsObject<EmotionState>();
			availableStates[num++] = emotionState;
			if (emotionState.EntityCodes != null)
			{
				emotionState.EntityCodeLocs = emotionState.EntityCodes.Select((string str) => new AssetLocation(str)).ToArray();
			}
		}
		tickAccum = (float)(entity.World.Rand.NextDouble() * 0.33);
		epartSys = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
		_enumCreatureHostility = entity.World.Config.GetString("creatureHostility") switch
		{
			"aggressive" => EnumCreatureHostility.Aggressive, 
			"passive" => EnumCreatureHostility.Passive, 
			"off" => EnumCreatureHostility.NeverHostile, 
			_ => EnumCreatureHostility.Aggressive, 
		};
	}

	public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
	{
		EntityBehaviorHealth behavior = entity.GetBehavior<EntityBehaviorHealth>();
		healthRel = ((behavior == null) ? 1f : (behavior.Health / behavior.MaxHealth));
		EnumDamageSource source = damageSource.Source;
		bool flag = (uint)(source - 2) <= 1u;
		if (flag || _enumCreatureHostility == EnumCreatureHostility.NeverHostile)
		{
			return;
		}
		Entity causeEntity = damageSource.GetCauseEntity();
		long sourceEntityId = causeEntity?.EntityId ?? 0;
		long herdId = (entity as EntityAgent).HerdId;
		if (TryTriggerState("alarmherdondamage", sourceEntityId) && causeEntity != null && herdId > 0)
		{
			EmotionState emotionState = availableStates.First((EmotionState s) => s.Code == "alarmherdondamage");
			entity.World.GetNearestEntity(entity.ServerPos.XYZ, emotionState.NotifyRange, emotionState.NotifyRange, delegate(Entity e)
			{
				EntityAgent entityAgent = e as EntityAgent;
				if (e.EntityId != entity.EntityId && entityAgent != null && entityAgent.Alive && entityAgent.HerdId == herdId)
				{
					entityAgent.GetBehavior<EntityBehaviorEmotionStates>()?.TryTriggerState("aggressiveondamage", sourceEntityId);
				}
				return false;
			});
		}
		if (TryTriggerState("aggressiveondamage", sourceEntityId))
		{
			TryTriggerState("aggressivealarmondamage", sourceEntityId);
		}
		if (TryTriggerState("fleeondamage", sourceEntityId))
		{
			TryTriggerState("fleealarmondamage", sourceEntityId);
		}
	}

	public bool IsInEmotionState(string statecode)
	{
		return ActiveStatesByCode.ContainsKey(statecode);
	}

	public void ClearStates()
	{
		ActiveStatesByCode.Clear();
	}

	public ActiveEmoState GetActiveEmotionState(string statecode)
	{
		ActiveStatesByCode.TryGetValue(statecode, out var value);
		return value;
	}

	public bool TryTriggerState(string statecode, long sourceEntityId)
	{
		return TryTriggerState(statecode, entity.World.Rand.NextDouble(), sourceEntityId);
	}

	public bool TryTriggerState(string statecode, double rndValue, long sourceEntityId)
	{
		bool result = false;
		for (int i = 0; i < availableStates.Length; i++)
		{
			EmotionState emotionState = availableStates[i];
			if (!(emotionState.Code != statecode) && !(rndValue > (double)emotionState.Chance))
			{
				if (emotionState.whenSourceUntargetable)
				{
					TryTarget(i, sourceEntityId);
				}
				else if (tryActivateState(i, sourceEntityId))
				{
					result = true;
				}
			}
		}
		return result;
	}

	private void TryTarget(int emostateid, long sourceEntityId)
	{
		if (pathtask == null)
		{
			ICoreAPI api = entity.World.Api;
			PathfindingAsync modSystem = api.ModLoader.GetModSystem<PathfindingAsync>();
			WaypointsTraverser waypointsTraverser = entity.GetBehavior<EntityBehaviorTaskAI>()?.PathTraverser;
			if (waypointsTraverser != null)
			{
				pathtask = waypointsTraverser.PreparePathfinderTask(entity.ServerPos.AsBlockPos, api.World.GetEntityById(sourceEntityId).ServerPos.AsBlockPos);
				modSystem.EnqueuePathfinderTask(pathtask);
				nopathEmoStateid = emostateid;
				this.sourceEntityId = sourceEntityId;
			}
		}
	}

	private bool tryActivateState(int stateid, long sourceEntityId)
	{
		EmotionState emotionState = availableStates[stateid];
		string code = emotionState.Code;
		ActiveEmoState activeEmoState = null;
		if (emotionState.whenHealthRelBelow < healthRel)
		{
			return false;
		}
		foreach (KeyValuePair<string, ActiveEmoState> item in ActiveStatesByCode)
		{
			if (item.Key == emotionState.Code)
			{
				activeEmoState = item.Value;
				continue;
			}
			int stateId = item.Value.StateId;
			EmotionState emotionState2 = availableStates[stateId];
			if (emotionState2.Slot != emotionState.Slot)
			{
				continue;
			}
			if (emotionState2.Priority > emotionState.Priority)
			{
				return false;
			}
			ActiveStatesByCode.Remove(item.Key);
			entityAttr.RemoveAttribute(emotionState.Code);
			break;
		}
		if (emotionState.MaxGeneration < entity.WatchedAttributes.GetInt("generation"))
		{
			return false;
		}
		if (code == "aggressivearoundentities" && (activeEmoState != null || !entitiesNearby(emotionState)))
		{
			return false;
		}
		float num = emotionState.Duration;
		if (emotionState.BelowTempThreshold > -99f && entity.World.BlockAccessor.GetClimateAt(entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, entity.World.Calendar.TotalDays).Temperature < emotionState.BelowTempThreshold)
		{
			num = emotionState.BelowTempDuration;
		}
		float num2 = 0f;
		if (emotionState.AccumType == EnumAccumType.Sum)
		{
			num2 = activeEmoState?.Duration ?? (0f + num);
		}
		if (emotionState.AccumType == EnumAccumType.Max)
		{
			num2 = Math.Max(activeEmoState?.Duration ?? 0f, num);
		}
		if (emotionState.AccumType == EnumAccumType.NoAccum)
		{
			num2 = ((activeEmoState == null || !(activeEmoState.Duration > 0f)) ? num : (activeEmoState?.Duration ?? 0f));
		}
		if (activeEmoState == null)
		{
			ActiveStatesByCode[emotionState.Code] = new ActiveEmoState
			{
				Duration = num2,
				SourceEntityId = sourceEntityId,
				StateId = stateid
			};
		}
		else
		{
			activeEmoState.SourceEntityId = sourceEntityId;
		}
		entityAttr.SetFloat(emotionState.Code, num2);
		return true;
	}

	public override void OnGameTick(float deltaTime)
	{
		if (pathtask != null && pathtask.Finished)
		{
			if (pathtask.waypoints == null)
			{
				tryActivateState(nopathEmoStateid, sourceEntityId);
			}
			pathtask = null;
			nopathEmoStateid = 0;
			sourceEntityId = 0L;
		}
		if ((tickAccum += deltaTime) < 0.33f)
		{
			return;
		}
		tickAccum = 0f;
		if (_enumCreatureHostility == EnumCreatureHostility.Aggressive)
		{
			TryTriggerState("aggressivearoundentities", 0L);
		}
		float num = 0f;
		List<string> list = null;
		foreach (KeyValuePair<string, ActiveEmoState> item in ActiveStatesByCode)
		{
			string key = item.Key;
			ActiveEmoState value = item.Value;
			if ((value.Duration -= 10f * deltaTime) <= 0f)
			{
				if (list == null)
				{
					list = new List<string>();
				}
				list.Add(key);
				entityAttr.RemoveAttribute(key);
			}
			else
			{
				num += availableStates[value.StateId].StressLevel;
			}
		}
		if (list != null)
		{
			foreach (string item2 in list)
			{
				ActiveStatesByCode.Remove(item2);
			}
		}
		float num2 = entity.WatchedAttributes.GetFloat("stressLevel");
		if (num > 0f)
		{
			entity.WatchedAttributes.SetFloat("stressLevel", Math.Max(num2, num));
		}
		else if (num2 > 0f)
		{
			num2 = Math.Max(0f, num2 - deltaTime * 1.25f);
			entity.WatchedAttributes.SetFloat("stressLevel", num2);
		}
		if (entity.World.EntityDebugMode)
		{
			entity.DebugAttributes.SetString("emotionstates", string.Join(", ", ActiveStatesByCode.Keys.ToList()));
		}
	}

	private bool entitiesNearby(EmotionState newstate)
	{
		return epartSys.GetNearestEntity(entity.ServerPos.XYZ, newstate.NotifyRange, delegate(Entity e)
		{
			if (newstate.EntityCodeLocs == null)
			{
				return false;
			}
			for (int i = 0; i < newstate.EntityCodeLocs.Length; i++)
			{
				if (newstate.EntityCodeLocs[i].Equals(e.Code))
				{
					return e.IsInteractable;
				}
			}
			return false;
		}, EnumEntitySearchType.Creatures) != null;
	}

	public override string PropertyName()
	{
		return "emotionstates";
	}
}
