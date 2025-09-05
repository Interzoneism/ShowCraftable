using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class TrapChances
{
	public float TrapChance;

	public float TrapDestroyChance;

	public static Dictionary<string, TrapChances> FromEntityAttr(Entity entity)
	{
		return entity.Properties.Attributes?["trappable"].AsObject<Dictionary<string, TrapChances>>();
	}

	public static bool IsTrappable(Entity entity, string traptype)
	{
		JsonObject attributes = entity.Properties.Attributes;
		if (attributes == null)
		{
			return false;
		}
		return attributes["trappable"]?[traptype].Exists == true;
	}
}
