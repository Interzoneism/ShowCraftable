namespace Vintagestory.API.Common;

public class ItemSlotOffhand : ItemSlot
{
	public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.Offhand;

	public ItemSlotOffhand(InventoryBase inventory)
		: base(inventory)
	{
		BackgroundIcon = "left_hand";
	}
}
