using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class EntityActivity : IEntityActivity
{
	public int currentActionIndex = -1;

	private EntityActivitySystem vas;

	public double origPriority;

	[JsonProperty]
	public int Slot { get; set; }

	[JsonProperty]
	public double Priority { get; set; } = 1.0;

	[JsonProperty]
	public string Name { get; set; }

	[JsonProperty]
	public string Code { get; set; }

	[JsonProperty]
	public IActionCondition[] Conditions { get; set; } = Array.Empty<IActionCondition>();

	[JsonProperty]
	public IEntityAction[] Actions { get; set; } = Array.Empty<IEntityAction>();

	[JsonProperty]
	public EnumConditionLogicOp ConditionsOp { get; set; } = EnumConditionLogicOp.AND;

	public IEntityAction CurrentAction
	{
		get
		{
			if (currentActionIndex >= 0)
			{
				return Actions[currentActionIndex];
			}
			return null;
		}
	}

	public bool Finished { get; set; }

	public EntityActivity()
	{
	}

	public EntityActivity(EntityActivitySystem vas)
	{
		this.vas = vas;
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
		if (Code == null || Code.Length == 0)
		{
			Code = Name;
		}
		if (Actions != null)
		{
			IEntityAction[] actions = Actions;
			for (int i = 0; i < actions.Length; i++)
			{
				actions[i].OnLoaded(vas);
			}
		}
		if (Conditions != null)
		{
			IActionCondition[] conditions = Conditions;
			for (int i = 0; i < conditions.Length; i++)
			{
				conditions[i].OnLoaded(vas);
			}
		}
		origPriority = Priority;
	}

	public void Cancel()
	{
		CurrentAction?.Cancel();
		currentActionIndex = -1;
		Finished = true;
		Priority = origPriority;
	}

	public void Start()
	{
		Finished = false;
		currentActionIndex = 0;
		CurrentAction.Start(this);
		if (vas.Debug)
		{
			vas.Entity.World.Logger.Debug("ActivitySystem entity {0}, starting new Activity - {1}", vas.Entity.EntityId, Name);
			vas.Entity.World.Logger.Debug("starting next action {0}", CurrentAction?.Type);
		}
	}

	public void Finish()
	{
		CurrentAction?.Finish();
		Priority = origPriority;
	}

	public void Pause(EnumInteruptionType interuptionType)
	{
		CurrentAction?.Pause(interuptionType);
	}

	public void Resume()
	{
		CurrentAction?.Resume();
	}

	public void OnTick(float dt)
	{
		if (CurrentAction == null)
		{
			return;
		}
		CurrentAction.OnTick(dt);
		if (!CurrentAction.IsFinished())
		{
			return;
		}
		CurrentAction.Finish();
		if (currentActionIndex < Actions.Length - 1)
		{
			currentActionIndex++;
			CurrentAction.Start(this);
			if (vas.Debug)
			{
				vas.Entity.World.Logger.Debug("ActivitySystem entity {0}, starting next Action - {1}", vas.Entity.EntityId, CurrentAction?.Type);
			}
		}
		else
		{
			currentActionIndex = -1;
			Finished = true;
		}
	}

	public void LoadState(ITreeAttribute tree)
	{
		IStorableTypedComponent[] actions = Actions;
		loadState(actions, tree, "action");
		actions = Conditions;
		loadState(actions, tree, "condition");
	}

	public void StoreState(ITreeAttribute tree)
	{
		if (Actions != null)
		{
			IStorableTypedComponent[] actions = Actions;
			storeState(actions, tree, "action");
		}
		if (Conditions != null)
		{
			IStorableTypedComponent[] actions = Conditions;
			storeState(actions, tree, "condition");
		}
	}

	public void storeState(IStorableTypedComponent[] elems, ITreeAttribute tree, string key)
	{
		for (int i = 0; i < elems.Length; i++)
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			elems[i].StoreState(treeAttribute);
			treeAttribute.SetString("type", elems[i].Type);
			tree[key + i] = treeAttribute;
		}
	}

	public void loadState(IStorableTypedComponent[] elems, ITreeAttribute tree, string key)
	{
		for (int i = 0; i < elems.Length; i++)
		{
			ITreeAttribute treeAttribute = tree.GetTreeAttribute(key + i);
			if (treeAttribute != null)
			{
				elems[i].LoadState(treeAttribute);
				continue;
			}
			break;
		}
	}

	public override string ToString()
	{
		return base.ToString();
	}

	public EntityActivity Clone()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		JsonSerializerSettings val = new JsonSerializerSettings
		{
			TypeNameHandling = (TypeNameHandling)3
		};
		EntityActivity entityActivity = JsonUtil.ToObject<EntityActivity>(JsonConvert.SerializeObject((object)this, (Formatting)1, val), "", val);
		entityActivity.OnLoaded(vas);
		return entityActivity;
	}
}
