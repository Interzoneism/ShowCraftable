using System;

namespace Vintagestory.API.Common;

public class ItemSlotCharacter : ItemSlot
{
	public EnumCharacterDressType Type;

	public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.Outfit;

	public ItemSlotCharacter(EnumCharacterDressType type, InventoryBase inventory)
		: base(inventory)
	{
		Type = type;
	}

	public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
	{
		if (!IsDressType(sourceSlot.Itemstack, Type))
		{
			return false;
		}
		return base.CanTakeFrom(sourceSlot, priority);
	}

	public override bool CanHold(ItemSlot itemstackFromSourceSlot)
	{
		if (!IsDressType(itemstackFromSourceSlot.Itemstack, Type))
		{
			return false;
		}
		return base.CanHold(itemstackFromSourceSlot);
	}

	public static bool IsDressType(IItemStack itemstack, EnumCharacterDressType dressType)
	{
		if (itemstack == null || itemstack.Collectible.Attributes == null)
		{
			return false;
		}
		string text = itemstack.Collectible.Attributes["clothescategory"].AsString() ?? itemstack.Collectible.Attributes["attachableToEntity"]["categoryCode"].AsString();
		if (text != null)
		{
			return dressType.ToString().Equals(text, StringComparison.InvariantCultureIgnoreCase);
		}
		return false;
	}
}
