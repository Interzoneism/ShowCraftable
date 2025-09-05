namespace Vintagestory.API.Common;

public class ItemSlotSurvival : ItemSlot
{
	public ItemSlotSurvival(InventoryBase inventory)
		: base(inventory)
	{
	}

	public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
	{
		IHeldBag heldBag = sourceSlot.Itemstack?.Collectible.GetCollectibleInterface<IHeldBag>() ?? null;
		if (heldBag != null && !heldBag.IsEmpty(sourceSlot.Itemstack))
		{
			return false;
		}
		return base.CanTakeFrom(sourceSlot, priority);
	}

	public override bool CanHold(ItemSlot sourceSlot)
	{
		IHeldBag heldBag = sourceSlot.Itemstack?.Collectible.GetCollectibleInterface<IHeldBag>() ?? null;
		if (base.CanHold(sourceSlot) && (heldBag == null || heldBag.IsEmpty(sourceSlot.Itemstack)))
		{
			return inventory.CanContain(this, sourceSlot);
		}
		return false;
	}
}
