namespace Vintagestory.API.Common;

public class ItemStackMergeOperation : ItemStackMoveOperation
{
	public ItemSlot SinkSlot;

	public ItemSlot SourceSlot;

	public ItemStackMergeOperation(IWorldAccessor world, EnumMouseButton mouseButton, EnumModifierKey modifiers, EnumMergePriority currentPriority, int requestedQuantity)
		: base(world, mouseButton, modifiers, currentPriority, requestedQuantity)
	{
	}
}
