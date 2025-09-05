using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common.Entities;

public class EntityServerProperties : EntitySidedProperties
{
	public SpawnConditions SpawnConditions;

	public EntityServerProperties(JsonObject[] behaviors, Dictionary<string, JsonObject> commonConfigs)
		: base(behaviors, commonConfigs)
	{
	}

	public override EntitySidedProperties Clone()
	{
		return new EntityServerProperties(BehaviorsAsJsonObj, null)
		{
			Attributes = Attributes,
			SpawnConditions = SpawnConditions
		};
	}
}
