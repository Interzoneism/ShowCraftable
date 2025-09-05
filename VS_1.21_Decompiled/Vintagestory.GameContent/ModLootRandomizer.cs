using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModLootRandomizer : ModSystem
{
	private ICoreClientAPI capi;

	private Dictionary<ItemSlot, GuiDialogGeneric> dialogs = new Dictionary<ItemSlot, GuiDialogGeneric>();

	private IClientNetworkChannel clientChannel;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		api.Event.RegisterEventBusListener(OnEventLootRandomizer, 0.5, "OpenLootRandomizerDialog");
		api.Event.RegisterEventBusListener(OnEventStackRandomizer, 0.5, "OpenStackRandomizerDialog");
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		clientChannel = api.Network.RegisterChannel("lootrandomizer").RegisterMessageType(typeof(SaveLootRandomizerAttributes)).RegisterMessageType(typeof(SaveStackRandomizerAttributes));
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		api.Network.RegisterChannel("lootrandomizer").RegisterMessageType(typeof(SaveLootRandomizerAttributes)).RegisterMessageType(typeof(SaveStackRandomizerAttributes))
			.SetMessageHandler<SaveLootRandomizerAttributes>(OnLootRndMsg)
			.SetMessageHandler<SaveStackRandomizerAttributes>(OnStackRndMsg);
	}

	private void OnLootRndMsg(IServerPlayer fromPlayer, SaveLootRandomizerAttributes networkMessage)
	{
		if (!fromPlayer.HasPrivilege("controlserver"))
		{
			fromPlayer.SendIngameError("noprivilege", "No privilege to set up a loot randomizer");
			return;
		}
		ItemSlot itemSlot = fromPlayer.InventoryManager.GetInventory(networkMessage.InventoryId)?[networkMessage.SlotId];
		if (itemSlot == null || itemSlot.Empty)
		{
			return;
		}
		if (!(itemSlot.Itemstack.Collectible is ItemLootRandomizer))
		{
			fromPlayer.SendIngameError("noprivilege", "Not a loot randomizer");
			return;
		}
		using MemoryStream input = new MemoryStream(networkMessage.attributes);
		itemSlot.Itemstack.Attributes.FromBytes(new BinaryReader(input));
	}

	private void OnStackRndMsg(IServerPlayer fromPlayer, SaveStackRandomizerAttributes networkMessage)
	{
		if (!fromPlayer.HasPrivilege("controlserver"))
		{
			fromPlayer.SendIngameError("noprivilege", "No privilege to set up a loot randomizer");
			return;
		}
		ItemSlot itemSlot = fromPlayer.InventoryManager.GetInventory(networkMessage.InventoryId)?[networkMessage.SlotId];
		if (itemSlot != null && !itemSlot.Empty)
		{
			CollectibleObject collectible = itemSlot.Itemstack.Collectible;
			if ((!(collectible is ItemLootRandomizer) && !(collectible is ItemStackRandomizer)) || 1 == 0)
			{
				fromPlayer.SendIngameError("noprivilege", "Not a loot or stack randomizer");
			}
			else
			{
				itemSlot.Itemstack.Attributes.SetFloat("totalChance", networkMessage.TotalChance);
			}
		}
	}

	private void OnEventLootRandomizer(string eventName, ref EnumHandling handling, IAttribute data)
	{
		if (capi == null)
		{
			return;
		}
		string inventoryId = (data as TreeAttribute).GetString("inventoryId");
		int slotId = (data as TreeAttribute).GetInt("slotId");
		ItemSlot slot = capi.World.Player.InventoryManager.GetInventory(inventoryId)[slotId];
		if (dialogs.ContainsKey(slot))
		{
			return;
		}
		float[] array = new float[10];
		ItemStack[] array2 = new ItemStack[10];
		int num = 0;
		foreach (KeyValuePair<string, IAttribute> attribute in slot.Itemstack.Attributes)
		{
			if (attribute.Key.StartsWithOrdinal("stack") && attribute.Value is TreeAttribute)
			{
				TreeAttribute treeAttribute = attribute.Value as TreeAttribute;
				array[num] = treeAttribute.GetFloat("chance");
				array2[num] = treeAttribute.GetItemstack("stack");
				array2[num].ResolveBlockOrItem(capi.World);
				num++;
			}
		}
		dialogs[slot] = new GuiDialogItemLootRandomizer(array2, array, capi);
		dialogs[slot].TryOpen();
		dialogs[slot].OnClosed += delegate
		{
			DidCloseLootRandomizer(slot, dialogs[slot]);
		};
	}

	private void OnEventStackRandomizer(string eventName, ref EnumHandling handling, IAttribute data)
	{
		if (capi == null)
		{
			return;
		}
		string inventoryId = (data as TreeAttribute).GetString("inventoryId");
		int slotId = (data as TreeAttribute).GetInt("slotId");
		ItemSlot slot = capi.World.Player.InventoryManager.GetInventory(inventoryId)[slotId];
		if (!dialogs.ContainsKey(slot))
		{
			dialogs[slot] = new GuiDialogItemStackRandomizer((data as TreeAttribute).GetFloat("totalChance"), capi);
			dialogs[slot].TryOpen();
			dialogs[slot].OnClosed += delegate
			{
				DidCloseStackRandomizer(slot, dialogs[slot]);
			};
		}
	}

	private void DidCloseStackRandomizer(ItemSlot slot, GuiDialogGeneric dialog)
	{
		dialogs.Remove(slot);
		if (slot.Itemstack == null || dialog.Attributes.GetInt("save") == 0)
		{
			return;
		}
		slot.Itemstack.Attributes.SetFloat("totalChance", dialog.Attributes.GetFloat("totalChance"));
		using MemoryStream output = new MemoryStream();
		BinaryWriter stream = new BinaryWriter(output);
		slot.Itemstack.Attributes.ToBytes(stream);
		clientChannel.SendPacket(new SaveStackRandomizerAttributes
		{
			TotalChance = dialog.Attributes.GetFloat("totalChance"),
			InventoryId = slot.Inventory.InventoryID,
			SlotId = slot.Inventory.GetSlotId(slot)
		});
	}

	private void DidCloseLootRandomizer(ItemSlot slot, GuiDialogGeneric dialog)
	{
		dialogs.Remove(slot);
		if (slot.Itemstack == null)
		{
			return;
		}
		ITreeAttribute attributes = dialog.Attributes;
		if (attributes.GetInt("save") == 0)
		{
			return;
		}
		for (int i = 0; i < 10; i++)
		{
			if (!(attributes["stack" + i] is ITreeAttribute value))
			{
				slot.Itemstack.Attributes.RemoveAttribute("stack" + i);
			}
			else
			{
				slot.Itemstack.Attributes["stack" + i] = value;
			}
		}
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter stream = new BinaryWriter(memoryStream);
		slot.Itemstack.Attributes.ToBytes(stream);
		clientChannel.SendPacket(new SaveLootRandomizerAttributes
		{
			attributes = memoryStream.ToArray(),
			InventoryId = slot.Inventory.InventoryID,
			SlotId = slot.Inventory.GetSlotId(slot)
		});
	}
}
