using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemEditableBook : ModSystem
{
	private Dictionary<string, ItemSlot> nowEditing = new Dictionary<string, ItemSlot>();

	private ICoreAPI api;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public void Transcribe(IPlayer player, string pageText, string bookTitle, int pageNumber, ItemSlot bookSlot)
	{
		ItemSlot matSlot = null;
		player.Entity.WalkInventory(delegate(ItemSlot slot)
		{
			if (slot.Empty)
			{
				return true;
			}
			JsonObject attributes = slot.Itemstack.Collectible.Attributes;
			if (attributes != null && attributes["canTranscribeOn"].AsBool() && !slot.Itemstack.Attributes.HasAttribute("text"))
			{
				matSlot = slot;
				return false;
			}
			return true;
		});
		if (matSlot == null)
		{
			(player as IServerPlayer)?.SendIngameError("nomats", Lang.Get("Need something to transcribe it on first, such as parchment"));
			(api as ICoreClientAPI)?.TriggerIngameError(this, "nomats", Lang.Get("Need something to transcribe it on first, such as parchment"));
			return;
		}
		if (!ItemBook.isWritingTool(player.Entity.LeftHandItemSlot))
		{
			(player as IServerPlayer)?.SendIngameError("noink", Lang.Get("Need ink and quill in my off hand"));
			(api as ICoreClientAPI)?.TriggerIngameError(this, "noink", Lang.Get("Need ink and quill in my off hand"));
			return;
		}
		if (api is ICoreClientAPI coreClientAPI)
		{
			coreClientAPI.Network.GetChannel("editablebook").SendPacket(new TranscribePacket
			{
				Title = bookTitle,
				Text = pageText,
				PageNumber = pageNumber
			});
			return;
		}
		ItemStack itemStack = matSlot.TakeOut(1);
		itemStack.Attributes.SetString("text", pageText);
		itemStack.Attributes.SetString("title", bookTitle);
		itemStack.Attributes.SetInt("pageNumber", pageNumber);
		itemStack.Attributes.SetString("signedby", bookSlot.Itemstack.Attributes.GetString("signedby"));
		itemStack.Attributes.SetString("signedbyuid", bookSlot.Itemstack.Attributes.GetString("signedbyuid"));
		itemStack.Attributes.SetString("transcribedby", player.PlayerName);
		itemStack.Attributes.SetString("transcribedbyuid", player.PlayerUID);
		if (!player.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true))
		{
			api.World.SpawnItemEntity(itemStack, player.Entity.Pos.XYZ);
		}
		api.World.PlaySoundAt(new AssetLocation("sounds/effect/writing"), player.Entity);
	}

	public void BeginEdit(IPlayer player, ItemSlot slot)
	{
		nowEditing[player.PlayerUID] = slot;
		api.World.PlaySoundAt(new AssetLocation("sounds/held/bookturn*"), player.Entity);
	}

	public void EndEdit(IPlayer player, string text, string title, bool didSign)
	{
		if (nowEditing.TryGetValue(player.PlayerUID, out var value))
		{
			value.Itemstack.Attributes.SetString("text", text);
			value.Itemstack.Attributes.SetString("title", title);
			if (didSign)
			{
				value.Itemstack.Attributes.SetString("signedby", player.PlayerName);
				value.Itemstack.Attributes.SetString("signedbyuid", player.PlayerUID);
			}
			value.MarkDirty();
			if (api is ICoreClientAPI coreClientAPI)
			{
				coreClientAPI.Network.GetChannel("editablebook").SendPacket(new EditbookPacket
				{
					DidSave = true,
					DidSign = didSign,
					Text = text,
					Title = title
				});
				api.World.PlaySoundAt(new AssetLocation("sounds/held/bookclose*"), player.Entity);
			}
		}
		nowEditing.Remove(player.PlayerUID);
	}

	public void CancelEdit(IPlayer player)
	{
		nowEditing.Remove(player.PlayerUID);
		if (api is ICoreClientAPI coreClientAPI)
		{
			coreClientAPI.Network.GetChannel("editablebook").SendPacket(new EditbookPacket
			{
				DidSave = false
			});
			api.World.PlaySoundAt(new AssetLocation("sounds/held/bookclose*"), player.Entity);
		}
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		this.api = api;
		api.Network.RegisterChannel("editablebook").RegisterMessageType<EditbookPacket>().RegisterMessageType<TranscribePacket>();
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		api.Network.GetChannel("editablebook").SetMessageHandler<EditbookPacket>(onEditBookPacket).SetMessageHandler<TranscribePacket>(onTranscribePacket);
	}

	private void onTranscribePacket(IServerPlayer fromPlayer, TranscribePacket packet)
	{
		if (nowEditing.TryGetValue(fromPlayer.PlayerUID, out var value))
		{
			Transcribe(fromPlayer, packet.Text, packet.Title, packet.PageNumber, value);
		}
	}

	private void onEditBookPacket(IServerPlayer fromPlayer, EditbookPacket packet)
	{
		if (nowEditing.TryGetValue(fromPlayer.PlayerUID, out var _))
		{
			if (packet.DidSave)
			{
				EndEdit(fromPlayer, packet.Text, packet.Title, packet.DidSign);
			}
			else
			{
				CancelEdit(fromPlayer);
			}
		}
	}
}
