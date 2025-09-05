using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class ModSystemSyncHarvestableDropsToClient : ModSystem
{
	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void AssetsFinalize(ICoreAPI api)
	{
		base.AssetsFinalize(api);
		foreach (EntityProperties entityType in api.World.EntityTypes)
		{
			JsonObject[] behaviorsAsJsonObj = entityType.Server.BehaviorsAsJsonObj;
			foreach (JsonObject jsonObject in behaviorsAsJsonObj)
			{
				if (jsonObject["code"].AsString() == "harvestable")
				{
					if (entityType.Attributes == null)
					{
						entityType.Attributes = new JsonObject(JToken.Parse("{}"));
					}
					entityType.Attributes.Token[(object)"harvestableDrops"] = jsonObject["drops"].Token;
				}
			}
		}
	}
}
