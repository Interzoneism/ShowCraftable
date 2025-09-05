using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class SeatableInteractionHelp
{
	public static WorldInteraction[] GetOrCreateInteractionHelp(ICoreAPI api, EntityBehaviorSeatable eba, IMountableSeat[] seats, int slotIndex)
	{
		IMountableSeat seat = getSeat(eba, seats, slotIndex);
		if (seat == null)
		{
			return null;
		}
		JsonObject attributes = seat.Config.Attributes;
		if (attributes != null && attributes["ropeTieablesOnly"].AsBool())
		{
			List<ItemStack> orCreate = ObjectCacheUtil.GetOrCreate(api, "interactionhelp-ropetiablestacks", delegate
			{
				List<ItemStack> list = new List<ItemStack>();
				foreach (EntityProperties entityType in api.World.EntityTypes)
				{
					JsonObject[] behaviorsAsJsonObj = entityType.Client.BehaviorsAsJsonObj;
					for (int i = 0; i < behaviorsAsJsonObj.Length; i++)
					{
						if (behaviorsAsJsonObj[i]["code"].AsString() == "ropetieable")
						{
							Item item = api.World.GetItem(AssetLocation.Create("creature-" + entityType.Code.Path, entityType.Code.Domain));
							if (item != null)
							{
								list.Add(new ItemStack(item));
							}
						}
					}
				}
				return list;
			});
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = ((seat.Passenger != null) ? "seatableentity-dismountcreature" : "seatableentity-mountcreature"),
					Itemstacks = orCreate.ToArray(),
					MouseButton = EnumMouseButton.Right
				}
			};
		}
		if (eba.CanSitOn(seat, slotIndex))
		{
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "seatableentity-mount",
					MouseButton = EnumMouseButton.Right
				}
			};
		}
		return null;
	}

	private static IMountableSeat getSeat(EntityBehaviorSeatable eba, IMountableSeat[] seats, int slotIndex)
	{
		EntityBehaviorSelectionBoxes behavior = eba.entity.GetBehavior<EntityBehaviorSelectionBoxes>();
		if (behavior == null)
		{
			return null;
		}
		AttachmentPointAndPose attachmentPointAndPose = behavior.selectionBoxes[slotIndex];
		string apname = attachmentPointAndPose.AttachPoint.Code;
		return seats.FirstOrDefault((IMountableSeat seat) => seat.Config.APName == apname || seat.Config.SelectionBox == apname);
	}
}
