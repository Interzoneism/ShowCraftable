using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class ItemSlot
{
	protected ItemStack itemstack;

	protected InventoryBase inventory;

	public string BackgroundIcon;

	public string HexBackgroundColor;

	public virtual int MaxSlotStackSize { get; set; } = 999999;

	public InventoryBase Inventory => inventory;

	public virtual bool DrawUnavailable { get; set; }

	public ItemStack Itemstack
	{
		get
		{
			return itemstack;
		}
		set
		{
			itemstack = value;
		}
	}

	public int StackSize
	{
		get
		{
			if (itemstack != null)
			{
				return itemstack.StackSize;
			}
			return 0;
		}
	}

	public virtual bool Empty => itemstack == null;

	public virtual EnumItemStorageFlags StorageType { get; set; } = EnumItemStorageFlags.General | EnumItemStorageFlags.Metallurgy | EnumItemStorageFlags.Jewellery | EnumItemStorageFlags.Alchemy | EnumItemStorageFlags.Agriculture | EnumItemStorageFlags.Outfit;

	public event ActionConsumable MarkedDirty;

	public ItemSlot(InventoryBase inventory)
	{
		this.inventory = inventory;
	}

	public virtual int GetRemainingSlotSpace(ItemStack forItemstack)
	{
		return Math.Max(0, MaxSlotStackSize - StackSize);
	}

	public virtual bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
	{
		InventoryBase inventoryBase = inventory;
		if (inventoryBase != null && inventoryBase.PutLocked)
		{
			return false;
		}
		ItemStack itemStack = sourceSlot.Itemstack;
		if (itemStack == null)
		{
			return false;
		}
		if ((itemStack.Collectible.GetStorageFlags(itemStack) & StorageType) > (EnumItemStorageFlags)0 && (itemstack == null || itemstack.Collectible.GetMergableQuantity(itemstack, itemStack, priority) > 0))
		{
			return GetRemainingSlotSpace(itemStack) > 0;
		}
		return false;
	}

	public virtual bool CanHold(ItemSlot sourceSlot)
	{
		InventoryBase inventoryBase = inventory;
		if (inventoryBase != null && inventoryBase.PutLocked)
		{
			return false;
		}
		if (sourceSlot?.Itemstack?.Collectible != null && (sourceSlot.Itemstack.Collectible.GetStorageFlags(sourceSlot.Itemstack) & StorageType) > (EnumItemStorageFlags)0)
		{
			return inventory.CanContain(this, sourceSlot);
		}
		return false;
	}

	public virtual bool CanTake()
	{
		InventoryBase inventoryBase = inventory;
		if (inventoryBase != null && inventoryBase.TakeLocked)
		{
			return false;
		}
		return itemstack != null;
	}

	public virtual ItemStack TakeOutWhole()
	{
		ItemStack itemStack = itemstack.Clone();
		itemstack.StackSize = 0;
		itemstack = null;
		OnItemSlotModified(itemStack);
		return itemStack;
	}

	public virtual ItemStack TakeOut(int quantity)
	{
		if (itemstack == null)
		{
			return null;
		}
		if (quantity >= itemstack.StackSize)
		{
			return TakeOutWhole();
		}
		ItemStack emptyClone = itemstack.GetEmptyClone();
		emptyClone.StackSize = quantity;
		itemstack.StackSize -= quantity;
		if (itemstack.StackSize <= 0)
		{
			itemstack = null;
		}
		return emptyClone;
	}

	public virtual int TryPutInto(IWorldAccessor world, ItemSlot sinkSlot, int quantity = 1)
	{
		ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, quantity);
		return TryPutInto(sinkSlot, ref op);
	}

	public virtual int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		if (!sinkSlot.CanTakeFrom(this) || !CanTake() || itemstack == null)
		{
			return 0;
		}
		InventoryBase inventoryBase = sinkSlot.inventory;
		if (inventoryBase != null && !inventoryBase.CanContain(sinkSlot, this))
		{
			return 0;
		}
		if (sinkSlot.Itemstack == null)
		{
			int num = Math.Min(sinkSlot.GetRemainingSlotSpace(itemstack), op.RequestedQuantity);
			if (num > 0)
			{
				sinkSlot.Itemstack = TakeOut(num);
				op.MovedQuantity = (op.MovableQuantity = Math.Min(sinkSlot.StackSize, num));
				sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
				OnItemSlotModified(sinkSlot.Itemstack);
			}
			return op.MovedQuantity;
		}
		ItemStackMergeOperation itemStackMergeOperation = (ItemStackMergeOperation)(op = op.ToMergeOperation(sinkSlot, this));
		int requestedQuantity = op.RequestedQuantity;
		op.RequestedQuantity = Math.Min(sinkSlot.GetRemainingSlotSpace(itemstack), op.RequestedQuantity);
		sinkSlot.Itemstack.Collectible.TryMergeStacks(itemStackMergeOperation);
		if (itemStackMergeOperation.MovedQuantity > 0)
		{
			sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
			OnItemSlotModified(sinkSlot.Itemstack);
		}
		op.RequestedQuantity = requestedQuantity;
		return itemStackMergeOperation.MovedQuantity;
	}

	public virtual bool TryFlipWith(ItemSlot itemSlot)
	{
		if (itemSlot.StackSize > MaxSlotStackSize)
		{
			return false;
		}
		bool num = (itemSlot.Empty || CanHold(itemSlot)) && (Empty || CanTake());
		bool flag = (Empty || itemSlot.CanHold(this)) && (itemSlot.Empty || itemSlot.CanTake());
		if (num && flag)
		{
			itemSlot.FlipWith(this);
			itemSlot.OnItemSlotModified(itemstack);
			OnItemSlotModified(itemSlot.itemstack);
			return true;
		}
		return false;
	}

	protected virtual void FlipWith(ItemSlot withSlot)
	{
		if (withSlot.StackSize > MaxSlotStackSize)
		{
			if (Empty)
			{
				itemstack = withSlot.TakeOut(MaxSlotStackSize);
			}
		}
		else
		{
			ItemStack itemStack = withSlot.itemstack;
			withSlot.itemstack = itemstack;
			itemstack = itemStack;
		}
	}

	public virtual void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		if (Empty && sourceSlot.Empty)
		{
			return;
		}
		switch (op.MouseButton)
		{
		case EnumMouseButton.Left:
			ActivateSlotLeftClick(sourceSlot, ref op);
			break;
		case EnumMouseButton.Middle:
			ActivateSlotMiddleClick(sourceSlot, ref op);
			break;
		case EnumMouseButton.Right:
			ActivateSlotRightClick(sourceSlot, ref op);
			break;
		case EnumMouseButton.Wheel:
			if (op.WheelDir > 0)
			{
				sourceSlot.TryPutInto(this, ref op);
			}
			else
			{
				TryPutInto(sourceSlot, ref op);
			}
			break;
		}
	}

	protected virtual void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		if (Empty)
		{
			if (CanHold(sourceSlot))
			{
				int val = Math.Min(sourceSlot.StackSize, MaxSlotStackSize);
				val = Math.Min(val, GetRemainingSlotSpace(sourceSlot.itemstack));
				itemstack = sourceSlot.TakeOut(val);
				op.MovedQuantity = itemstack.StackSize;
				OnItemSlotModified(itemstack);
			}
			return;
		}
		if (sourceSlot.Empty)
		{
			op.RequestedQuantity = StackSize;
			TryPutInto(sourceSlot, ref op);
			return;
		}
		int mergableQuantity = itemstack.Collectible.GetMergableQuantity(itemstack, sourceSlot.itemstack, op.CurrentPriority);
		if (mergableQuantity > 0)
		{
			int requestedQuantity = op.RequestedQuantity;
			op.RequestedQuantity = GameMath.Min(mergableQuantity, sourceSlot.itemstack.StackSize, GetRemainingSlotSpace(sourceSlot.itemstack));
			ItemStackMergeOperation op2 = (ItemStackMergeOperation)(op = op.ToMergeOperation(this, sourceSlot));
			itemstack.Collectible.TryMergeStacks(op2);
			sourceSlot.OnItemSlotModified(itemstack);
			OnItemSlotModified(itemstack);
			op.RequestedQuantity = requestedQuantity;
		}
		else
		{
			TryFlipWith(sourceSlot);
		}
	}

	protected virtual void ActivateSlotMiddleClick(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		if (!Empty)
		{
			IPlayer actingPlayer = op.ActingPlayer;
			if (actingPlayer != null && actingPlayer.WorldData?.CurrentGameMode == EnumGameMode.Creative)
			{
				sinkSlot.Itemstack = Itemstack.Clone();
				op.MovedQuantity = Itemstack.StackSize;
				sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
			}
		}
	}

	protected virtual void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		if (Empty)
		{
			if (CanHold(sourceSlot))
			{
				itemstack = sourceSlot.TakeOut(1);
				sourceSlot.OnItemSlotModified(itemstack);
				OnItemSlotModified(itemstack);
			}
		}
		else if (sourceSlot.Empty)
		{
			op.RequestedQuantity = (int)Math.Ceiling((float)itemstack.StackSize / 2f);
			TryPutInto(sourceSlot, ref op);
		}
		else
		{
			op.RequestedQuantity = 1;
			sourceSlot.TryPutInto(this, ref op);
			if (op.MovedQuantity <= 0)
			{
				TryFlipWith(sourceSlot);
			}
		}
	}

	public virtual void OnItemSlotModified(ItemStack sinkStack)
	{
		if (inventory != null)
		{
			inventory.DidModifyItemSlot(this, sinkStack);
			if (itemstack?.Collectible != null)
			{
				itemstack.Collectible.UpdateAndGetTransitionStates(inventory.Api.World, this);
			}
		}
	}

	public virtual void MarkDirty()
	{
		if ((this.MarkedDirty == null || !this.MarkedDirty()) && inventory != null)
		{
			inventory.DidModifyItemSlot(this);
			if (itemstack?.Collectible != null)
			{
				itemstack.Collectible.UpdateAndGetTransitionStates(inventory.Api.World, this);
			}
		}
	}

	public virtual string GetStackName()
	{
		return itemstack?.GetName();
	}

	public virtual string GetStackDescription(IClientWorldAccessor world, bool extendedDebugInfo)
	{
		return itemstack?.GetDescription(world, this, extendedDebugInfo);
	}

	public override string ToString()
	{
		if (Empty)
		{
			return base.ToString();
		}
		return base.ToString() + " (" + itemstack.ToString() + ")";
	}

	public virtual WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op = null, List<ItemSlot> skipSlots = null)
	{
		return inventory.GetBestSuitedSlot(sourceSlot, op, skipSlots);
	}

	public virtual void OnBeforeRender(ItemRenderInfo renderInfo)
	{
	}
}
