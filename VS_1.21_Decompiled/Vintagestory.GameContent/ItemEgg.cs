using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class ItemEgg : Item
{
	public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority)
	{
		string[] array = new string[GlobalConstants.IgnoredStackAttributes.Length + 1];
		array[0] = "chick";
		Array.Copy(GlobalConstants.IgnoredStackAttributes, 0, array, 1, GlobalConstants.IgnoredStackAttributes.Length);
		if (Equals(sinkStack, sourceStack, array) && sinkStack.StackSize < MaxStackSize)
		{
			return Math.Min(MaxStackSize - sinkStack.StackSize, sourceStack.StackSize);
		}
		return 0;
	}

	public override void TryMergeStacks(ItemStackMergeOperation op)
	{
		IAttribute attribute = op.SourceSlot.Itemstack?.Attributes?["chick"];
		IAttribute attribute2 = op.SinkSlot.Itemstack?.Attributes?["chick"];
		bool flag = (attribute == null && attribute2 == null) || (attribute?.Equals(attribute2) ?? false);
		base.TryMergeStacks(op);
		if (op.MovedQuantity > 0 && !flag)
		{
			op.SinkSlot.Itemstack?.Attributes?.RemoveAttribute("chick");
		}
	}
}
