using System;
using System.Collections.Generic;
using System.Text;

namespace Vintagestory.API.Common;

public interface IPlayerInventoryManager
{
	EnumTool? ActiveTool { get; }

	EnumTool? OffhandTool { get; }

	int ActiveHotbarSlotNumber { get; set; }

	ItemSlot ActiveHotbarSlot { get; }

	ItemSlot OffhandHotbarSlot { get; }

	Dictionary<string, IInventory> Inventories { get; }

	IEnumerable<InventoryBase> InventoriesOrdered { get; }

	List<IInventory> OpenedInventories { get; }

	ItemSlot MouseItemSlot { get; }

	ItemSlot CurrentHoveredSlot { get; set; }

	bool DropMouseSlotItems(bool dropAll);

	bool DropItem(ItemSlot slot, bool fullStack);

	void NotifySlot(IPlayer player, ItemSlot slot);

	string GetInventoryName(string inventoryClassName);

	IInventory GetOwnInventory(string inventoryClassName);

	IInventory GetInventory(string inventoryId);

	ItemStack GetHotbarItemstack(int slotId);

	IInventory GetHotbarInventory();

	bool GetInventory(string invID, out InventoryBase invFound);

	ItemSlot GetBestSuitedSlot(ItemSlot sourceSlot, bool onlyPlayerInventory, ItemStackMoveOperation op = null, List<ItemSlot> skipSlots = null);

	[Obsolete("Use GetBestSuitedSlot(ItemSlot sourceSlot, bool onlyPlayerInventory, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null) instead")]
	ItemSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots);

	object[] TryTransferAway(ItemSlot sourceSlot, ref ItemStackMoveOperation op, bool onlyPlayerInventory, bool slotNotifyEffect = false);

	object[] TryTransferAway(ItemSlot sourceSlot, ref ItemStackMoveOperation op, bool onlyPlayerInventory, StringBuilder shiftClickDebugText, bool slotNotifyEffect = false);

	object TryTransferTo(ItemSlot sourceSlot, ItemSlot targetSlot, ref ItemStackMoveOperation op);

	bool TryGiveItemstack(ItemStack itemstack, bool slotNotifyEffect = false);

	object OpenInventory(IInventory inventory);

	object CloseInventory(IInventory inventory);

	void CloseInventoryAndSync(IInventory inventory);

	bool Find(System.Func<ItemSlot, bool> matcher);

	bool HasInventory(IInventory inventory);

	void DiscardAll();

	void OnDeath();

	void DropAllInventoryItems(IInventory inv);

	void BroadcastHotbarSlot();
}
