namespace Vintagestory.API.Common;

public class InventoryDisplayed : InventoryGeneric
{
	private readonly BlockEntity container;

	public InventoryDisplayed(BlockEntity be, int quantitySlots, string invId, ICoreAPI api, NewSlotDelegate onNewSlot = null)
		: base(quantitySlots, invId, api, onNewSlot)
	{
		container = be;
	}

	public override void OnItemSlotModified(ItemSlot slot)
	{
		base.OnItemSlotModified(slot);
		container.MarkDirty(redrawOnClient: true);
	}
}
