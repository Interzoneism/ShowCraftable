using System;

namespace Vintagestory.API.Common;

public class ItemStackMoveOperation
{
	public IWorldAccessor World;

	public IPlayer ActingPlayer;

	public EnumMouseButton MouseButton;

	public EnumModifierKey Modifiers;

	public EnumMergePriority CurrentPriority;

	public EnumMergePriority? RequiredPriority;

	public string ConfirmationMessageCode;

	public int RequestedQuantity;

	public int MovableQuantity;

	public int MovedQuantity;

	public int WheelDir;

	public int NotMovedQuantity => Math.Max(0, RequestedQuantity - MovedQuantity);

	public bool ShiftDown => (Modifiers & EnumModifierKey.SHIFT) > (EnumModifierKey)0;

	public bool CtrlDown => (Modifiers & EnumModifierKey.CTRL) > (EnumModifierKey)0;

	public bool AltDown => (Modifiers & EnumModifierKey.ALT) > (EnumModifierKey)0;

	public ItemStackMoveOperation(IWorldAccessor world, EnumMouseButton mouseButton, EnumModifierKey modifiers, EnumMergePriority currentPriority, int requestedQuantity = 0)
	{
		World = world;
		MouseButton = mouseButton;
		Modifiers = modifiers;
		CurrentPriority = currentPriority;
		RequestedQuantity = requestedQuantity;
	}

	public ItemStackMergeOperation ToMergeOperation(ItemSlot SinkSlot, ItemSlot SourceSlot)
	{
		return new ItemStackMergeOperation(World, MouseButton, Modifiers, CurrentPriority, RequestedQuantity)
		{
			SinkSlot = SinkSlot,
			SourceSlot = SourceSlot,
			ActingPlayer = ActingPlayer
		};
	}
}
