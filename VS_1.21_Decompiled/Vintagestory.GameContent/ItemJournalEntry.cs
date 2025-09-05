using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemJournalEntry : Item
{
	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (byEntity.World.Side != EnumAppSide.Server)
		{
			handling = EnumHandHandling.PreventDefault;
			return;
		}
		IPlayer player = (byEntity.World as EntityPlayer)?.Player;
		if (player == null)
		{
			return;
		}
		try
		{
			JournalEntry entry = Attributes["journalentry"].AsObject<JournalEntry>();
			api.ModLoader.GetModSystem<ModJournal>().AddOrUpdateJournalEntry(player as IServerPlayer, entry);
			itemslot.TakeOut(1);
			itemslot.MarkDirty();
			handling = EnumHandHandling.PreventDefault;
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/writing"), byEntity, player);
		}
		catch (Exception e)
		{
			byEntity.World.Logger.Error("Failed adding journal entry.");
			byEntity.World.Logger.Error(e);
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
