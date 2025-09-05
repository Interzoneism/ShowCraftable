using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemRandomLore : ItemBook
{
	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		string text = itemslot.Itemstack.Attributes.GetString("category");
		if (byEntity.World.Side == EnumAppSide.Server && text != null)
		{
			IPlayer player = null;
			if (byEntity is EntityPlayer)
			{
				player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			if (!(player is IServerPlayer))
			{
				return;
			}
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute.SetString("playeruid", player?.PlayerUID);
			treeAttribute.SetString("category", text);
			treeAttribute.SetItemstack("itemstack", itemslot.Itemstack.Clone());
			api.Event.PushEvent("loreDiscovery", treeAttribute);
		}
		base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		handling = EnumHandHandling.PreventDefault;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string text = inSlot.Itemstack.Attributes.GetString("category");
		if (text != null)
		{
			dsc.Append(Lang.Get("loretype-" + text));
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-addtojournal",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
