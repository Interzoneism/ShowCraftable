using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common.Entities;

public abstract class EntitySidedProperties
{
	public ITreeAttribute Attributes;

	public JsonObject[] BehaviorsAsJsonObj;

	public List<EntityBehavior> Behaviors = new List<EntityBehavior>();

	public EntitySidedProperties(JsonObject[] behaviors, Dictionary<string, JsonObject> commonConfigs)
	{
		BehaviorsAsJsonObj = new JsonObject[behaviors.Length];
		int num = 0;
		foreach (JsonObject jsonObject in behaviors)
		{
			if (!jsonObject["enabled"].AsBool(defaultValue: true))
			{
				continue;
			}
			string text = jsonObject["code"].AsString();
			if (text != null)
			{
				JsonObject original = jsonObject;
				if (commonConfigs != null && commonConfigs.ContainsKey(text))
				{
					JToken obj = commonConfigs[text].Token.DeepClone();
					JToken obj2 = ((obj is JObject) ? obj : null);
					_003F val = obj2;
					JToken token = jsonObject.Token;
					((JContainer)val).Merge((object)((token is JObject) ? token : null));
					original = new JsonObject(obj2);
				}
				BehaviorsAsJsonObj[num++] = new JsonObject_ReadOnly(original);
			}
		}
		if (num < behaviors.Length)
		{
			Array.Resize(ref BehaviorsAsJsonObj, num);
		}
	}

	public void loadBehaviors(Entity entity, EntityProperties properties, IWorldAccessor world)
	{
		if (BehaviorsAsJsonObj == null)
		{
			return;
		}
		Behaviors.Clear();
		for (int i = 0; i < BehaviorsAsJsonObj.Length; i++)
		{
			JsonObject jsonObject = BehaviorsAsJsonObj[i];
			string text = jsonObject["code"].AsString();
			if (world.ClassRegistry.GetEntityBehaviorClass(text) != null)
			{
				EntityBehavior entityBehavior = world.ClassRegistry.CreateEntityBehavior(entity, text);
				Behaviors.Add(entityBehavior);
				entityBehavior.FromBytes(isSync: false);
				entityBehavior.Initialize(properties, jsonObject);
			}
			else
			{
				world.Logger.Notification("Entity behavior {0} for entity {1} not found, will not load it.", text, properties.Code);
			}
		}
	}

	public abstract EntitySidedProperties Clone();
}
