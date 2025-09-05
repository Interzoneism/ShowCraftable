namespace Vintagestory.API.Common;

public class ItemSlotBackpack : ItemSlot
{
	public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.Backpack;

	public override int MaxSlotStackSize => 1;

	public ItemSlotBackpack(InventoryBase inventory)
		: base(inventory)
	{
		BackgroundIcon = "basket";
	}
}
