using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common;

public class ItemSlotCraftingOutput : ItemSlotOutput
{
	public bool hasLeftOvers;

	private ItemStack prevStack;

	private InventoryCraftingGrid inv => (InventoryCraftingGrid)inventory;

	public ItemSlotCraftingOutput(InventoryBase inventory)
		: base(inventory)
	{
	}

	protected override void FlipWith(ItemSlot withSlot)
	{
		ItemStackMoveOperation op = new ItemStackMoveOperation(inv.Api.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, base.StackSize);
		CraftSingle(withSlot, ref op);
	}

	public override int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		if (Empty)
		{
			return 0;
		}
		op.RequestedQuantity = base.StackSize;
		ItemStack craftedStack = itemstack.Clone();
		if (hasLeftOvers)
		{
			int num = base.TryPutInto(sinkSlot, ref op);
			if (!Empty)
			{
				triggerEvent(craftedStack, num, op.ActingPlayer);
				return num;
			}
			hasLeftOvers = false;
			inv.ConsumeIngredients(sinkSlot);
			if (inv.CanStillCraftCurrent())
			{
				itemstack = prevStack.Clone();
			}
		}
		if (op.ShiftDown)
		{
			CraftMany(sinkSlot, ref op);
		}
		else
		{
			CraftSingle(sinkSlot, ref op);
		}
		if (op.ActingPlayer != null)
		{
			triggerEvent(craftedStack, op.MovedQuantity, op.ActingPlayer);
		}
		else if (base.Inventory is InventoryBasePlayer inventoryBasePlayer)
		{
			triggerEvent(craftedStack, op.MovedQuantity, inventoryBasePlayer.Player);
		}
		return op.MovedQuantity;
	}

	private void triggerEvent(ItemStack craftedStack, int moved, IPlayer actingPlayer)
	{
		TreeAttribute treeAttribute = new TreeAttribute();
		craftedStack.StackSize = moved;
		treeAttribute["itemstack"] = new ItemstackAttribute(craftedStack);
		treeAttribute["byentityid"] = new LongAttribute(actingPlayer.Entity.EntityId);
		actingPlayer.Entity.World.Api.Event.PushEvent("onitemcrafted", treeAttribute);
	}

	private void CraftMany(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		if (itemstack == null)
		{
			return;
		}
		int num = 0;
		while (true)
		{
			prevStack = itemstack.Clone();
			int stackSize = base.StackSize;
			op.RequestedQuantity = base.StackSize;
			op.MovedQuantity = 0;
			int num2 = TryPutIntoNoEvent(sinkSlot, ref op);
			num += num2;
			if (stackSize > num2)
			{
				hasLeftOvers = num2 > 0;
				break;
			}
			inv.ConsumeIngredients(sinkSlot);
			if (!inv.CanStillCraftCurrent())
			{
				break;
			}
			itemstack = prevStack;
		}
		if (num > 0)
		{
			sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
			OnItemSlotModified(sinkSlot.Itemstack);
		}
	}

	public virtual int TryPutIntoNoEvent(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		if (!sinkSlot.CanTakeFrom(this) || !CanTake() || itemstack == null)
		{
			return 0;
		}
		if (sinkSlot.Itemstack == null)
		{
			int num = Math.Min(sinkSlot.GetRemainingSlotSpace(base.Itemstack), op.RequestedQuantity);
			if (num > 0)
			{
				sinkSlot.Itemstack = TakeOut(num);
				op.MovedQuantity = (op.MovableQuantity = Math.Min(sinkSlot.StackSize, num));
			}
			return op.MovedQuantity;
		}
		ItemStackMergeOperation itemStackMergeOperation = (ItemStackMergeOperation)(op = op.ToMergeOperation(sinkSlot, this));
		int requestedQuantity = op.RequestedQuantity;
		op.RequestedQuantity = Math.Min(sinkSlot.GetRemainingSlotSpace(itemstack), op.RequestedQuantity);
		sinkSlot.Itemstack.Collectible.TryMergeStacks(itemStackMergeOperation);
		op.RequestedQuantity = requestedQuantity;
		return itemStackMergeOperation.MovedQuantity;
	}

	private void CraftSingle(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		int stackSize = base.StackSize;
		int num = TryPutIntoNoEvent(sinkSlot, ref op);
		if (num == stackSize)
		{
			inv.ConsumeIngredients(sinkSlot);
		}
		if (num > 0)
		{
			sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
			OnItemSlotModified(sinkSlot.Itemstack);
		}
	}
}
