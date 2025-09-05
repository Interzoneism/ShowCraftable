using System.Collections.Generic;

namespace Vintagestory.API.Common;

public interface IHeldBag
{
	bool IsEmpty(ItemStack bagstack);

	int GetQuantitySlots(ItemStack bagstack);

	ItemStack[] GetContents(ItemStack bagstack, IWorldAccessor world);

	List<ItemSlotBagContent> GetOrCreateSlots(ItemStack bagstack, InventoryBase parentinv, int bagIndex, IWorldAccessor world);

	void Store(ItemStack bagstack, ItemSlotBagContent slot);

	void Clear(ItemStack bagstack);

	string GetSlotBgColor(ItemStack bagstack);

	EnumItemStorageFlags GetStorageFlags(ItemStack bagstack);
}
