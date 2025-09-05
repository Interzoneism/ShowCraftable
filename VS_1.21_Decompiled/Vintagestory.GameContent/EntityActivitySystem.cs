using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Essentials;

namespace Vintagestory.GameContent;

public class EntityActivitySystem
{
	public List<IEntityActivity> AvailableActivities = new List<IEntityActivity>();

	public Dictionary<int, IEntityActivity> ActiveActivitiesBySlot = new Dictionary<int, IEntityActivity>();

	public PathTraverserBase linepathTraverser;

	public WaypointsTraverser wppathTraverser;

	public EntityAgent Entity;

	private float accum;

	private BlockPos activityOffset;

	private bool pauseAutoSelection;

	private bool clearDelay;

	public string Code { get; set; }

	public bool Debug { get; set; } = ActivityModSystem.Debug;

	public BlockPos ActivityOffset
	{
		get
		{
			if (activityOffset == null)
			{
				activityOffset = Entity.WatchedAttributes.GetBlockPos("importOffset", new BlockPos(Entity.Pos.Dimension));
			}
			return activityOffset;
		}
		set
		{
			activityOffset = value;
			Entity.WatchedAttributes.SetBlockPos("importOffset", activityOffset);
		}
	}

	public EntityActivitySystem(EntityAgent entity)
	{
		Entity = entity;
	}

	public bool StartActivity(string code, float priority = 9999f, int slot = -1)
	{
		int num = AvailableActivities.IndexOf((IEntityActivity item) => item.Code == code);
		if (num < 0)
		{
			return false;
		}
		IEntityActivity entityActivity = AvailableActivities[num];
		if (slot < 0)
		{
			slot = entityActivity.Slot;
		}
		if (priority < 0f)
		{
			priority = (float)entityActivity.Priority;
		}
		if (ActiveActivitiesBySlot.TryGetValue(entityActivity.Slot, out var value))
		{
			if (value.Priority > (double)priority)
			{
				return false;
			}
			value.Cancel();
		}
		ActiveActivitiesBySlot[entityActivity.Slot] = entityActivity;
		entityActivity.Priority = priority;
		entityActivity.Start();
		return true;
	}

	public bool CancelAll()
	{
		bool result = false;
		foreach (IEntityActivity value in ActiveActivitiesBySlot.Values)
		{
			if (value != null)
			{
				value.Cancel();
				result = true;
			}
		}
		return result;
	}

	public void PauseAutoSelection(bool paused)
	{
		pauseAutoSelection = paused;
	}

	public void Pause(EnumInteruptionType interuptionType)
	{
		foreach (IEntityActivity value in ActiveActivitiesBySlot.Values)
		{
			value?.Pause(interuptionType);
		}
	}

	public void Resume()
	{
		foreach (IEntityActivity value in ActiveActivitiesBySlot.Values)
		{
			value?.Resume();
		}
	}

	public void ClearNextActionDelay()
	{
		clearDelay = true;
	}

	public void OnTick(float dt)
	{
		linepathTraverser.OnGameTick(dt);
		wppathTraverser.OnGameTick(dt);
		accum += dt;
		if ((double)accum < 0.25 && !clearDelay)
		{
			return;
		}
		clearDelay = false;
		foreach (int key in ActiveActivitiesBySlot.Keys)
		{
			IEntityActivity entityActivity = ActiveActivitiesBySlot[key];
			if (entityActivity == null)
			{
				continue;
			}
			if (entityActivity.Finished)
			{
				entityActivity.Finish();
				Entity.Attributes.SetString("lastActivity", entityActivity.Code);
				if (Debug)
				{
					Entity.World.Logger.Debug("ActivitySystem entity {0} activity {1} has finished", Entity.EntityId, entityActivity.Name);
				}
				ActiveActivitiesBySlot.Remove(key);
			}
			else
			{
				entityActivity.OnTick(accum);
				Entity.World.FrameProfiler.Mark("behavior-activitydriven-tick-", entityActivity.Code);
			}
		}
		accum = 0f;
		if (!pauseAutoSelection)
		{
			foreach (IEntityActivity availableActivity in AvailableActivities)
			{
				int slot = availableActivity.Slot;
				if (ActiveActivitiesBySlot.TryGetValue(slot, out var value) && value != null && value.Priority >= availableActivity.Priority)
				{
					continue;
				}
				bool flag = availableActivity.ConditionsOp == EnumConditionLogicOp.AND;
				for (int i = 0; i < availableActivity.Conditions.Length; i++)
				{
					if (!flag && availableActivity.ConditionsOp != EnumConditionLogicOp.OR)
					{
						break;
					}
					IActionCondition obj = availableActivity.Conditions[i];
					bool flag2 = obj.ConditionSatisfied(Entity);
					if (obj.Invert)
					{
						flag2 = !flag2;
					}
					if (availableActivity.ConditionsOp == EnumConditionLogicOp.OR)
					{
						if (Debug && flag2)
						{
							Entity.World.Logger.Debug("ActivitySystem entity {0} activity condition {1} is satisfied, will execute {2}", Entity.EntityId, availableActivity.Conditions[i].Type, availableActivity.Name);
						}
						flag = flag || flag2;
					}
					else
					{
						flag = flag && flag2;
					}
				}
				if (flag)
				{
					ActiveActivitiesBySlot.TryGetValue(slot, out var value2);
					value2?.Cancel();
					ActiveActivitiesBySlot[slot] = availableActivity;
					availableActivity?.Start();
				}
			}
		}
		if (!Entity.World.EntityDebugMode)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<int, IEntityActivity> item in ActiveActivitiesBySlot)
		{
			stringBuilder.Append(item.Key + ": " + item.Value.Name + "/" + item.Value.CurrentAction?.Type);
		}
		Entity.DebugAttributes.SetString("activities", stringBuilder.ToString());
	}

	public bool Load(AssetLocation activityCollectionPath)
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected O, but got Unknown
		linepathTraverser = new StraightLineTraverser(Entity);
		wppathTraverser = new WaypointsTraverser(Entity);
		AvailableActivities.Clear();
		ActiveActivitiesBySlot.Clear();
		if (activityCollectionPath == null)
		{
			return false;
		}
		IAsset asset = Entity.Api.Assets.TryGet(activityCollectionPath.WithPathPrefixOnce("config/activitycollections/").WithPathAppendixOnce(".json"));
		if (asset == null)
		{
			Entity.World.Logger.Error(string.Concat("Unable to load activity file ", activityCollectionPath, " not such file found"));
			return false;
		}
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			TypeNameHandling = (TypeNameHandling)3
		};
		EntityActivityCollection entityActivityCollection = asset.ToObject<EntityActivityCollection>(settings);
		AvailableActivities.AddRange(entityActivityCollection.Activities);
		entityActivityCollection.OnLoaded(this);
		return true;
	}

	public void StoreState(TreeAttribute attributes, bool forClient)
	{
		if (!forClient)
		{
			storeStateActivities(attributes, "executingActions", ActiveActivitiesBySlot.Values);
		}
	}

	public void LoadState(TreeAttribute attributes, bool forClient)
	{
		if (forClient)
		{
			return;
		}
		ActiveActivitiesBySlot.Clear();
		foreach (IEntityActivity item in loadStateActivities(attributes, "executingActions"))
		{
			ActiveActivitiesBySlot[item.Slot] = item;
		}
	}

	private void storeStateActivities(TreeAttribute attributes, string key, IEnumerable<IEntityActivity> activities)
	{
		ITreeAttribute treeAttribute = (ITreeAttribute)(attributes[key] = new TreeAttribute());
		int num = 0;
		foreach (IEntityActivity activity in activities)
		{
			ITreeAttribute treeAttribute2 = new TreeAttribute();
			activity.StoreState(treeAttribute2);
			treeAttribute["activitiy" + num++] = treeAttribute2;
		}
	}

	private IEnumerable<IEntityActivity> loadStateActivities(TreeAttribute attributes, string key)
	{
		List<IEntityActivity> list = new List<IEntityActivity>();
		ITreeAttribute treeAttribute = attributes.GetTreeAttribute(key);
		if (treeAttribute == null)
		{
			return list;
		}
		int num = 0;
		while (num < 200)
		{
			ITreeAttribute treeAttribute2 = treeAttribute.GetTreeAttribute("activity" + num++);
			if (treeAttribute2 == null)
			{
				break;
			}
			EntityActivity entityActivity = new EntityActivity();
			entityActivity.LoadState(treeAttribute2);
			list.Add(entityActivity);
		}
		return list;
	}
}
