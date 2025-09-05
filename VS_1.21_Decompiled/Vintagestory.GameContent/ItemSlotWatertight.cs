using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class ItemSlotWatertight : ItemSlotSurvival
{
	public float capacityLitres;

	public ItemSlotWatertight(InventoryBase inventory, float capacityLitres = 6f)
		: base(inventory)
	{
		this.capacityLitres = capacityLitres;
	}

	public override bool CanTake()
	{
		if (!Empty && itemstack.Collectible.IsLiquid())
		{
			return false;
		}
		return base.CanTake();
	}

	protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		IWorldAccessor world = inventory.Api.World;
		if (sourceSlot.Itemstack?.Block is BlockLiquidContainerBase blockLiquidContainerBase)
		{
			ItemStack content = blockLiquidContainerBase.GetContent(sourceSlot.Itemstack);
			float num = BlockLiquidContainerBase.GetContainableProps(content)?.ItemsPerLitre ?? 1f;
			bool flag = !Empty && itemstack.Equals(world, content, GlobalConstants.IgnoredStackAttributes);
			if ((Empty || flag) && content != null)
			{
				ItemStack itemStack = sourceSlot.Itemstack;
				float val = ((op?.ActingPlayer?.Entity.Controls.ShiftKey == true) ? blockLiquidContainerBase.CapacityLitres : blockLiquidContainerBase.TransferSizeLitres);
				float num2 = (float)base.StackSize / num;
				float val2 = (float)content.StackSize / num;
				val = Math.Min(val, val2);
				val *= (float)itemStack.StackSize;
				val = Math.Min(val, capacityLitres - num2);
				if (val > 0f)
				{
					int num3 = (int)(num * val);
					ItemStack itemStack2 = blockLiquidContainerBase.TryTakeContent(itemStack, num3 / itemStack.StackSize);
					itemStack2.StackSize *= itemStack.StackSize;
					itemStack2.StackSize += base.StackSize;
					itemstack = itemStack2;
					MarkDirty();
					op.MovedQuantity = num3;
				}
			}
			return;
		}
		string text = sourceSlot.Itemstack?.ItemAttributes?["contentItemCode"].AsString();
		if (text != null)
		{
			ItemStack itemStack3 = new ItemStack(world.GetItem(AssetLocation.Create(text, sourceSlot.Itemstack.Collectible.Code.Domain)));
			bool flag2 = !Empty && itemstack.Equals(world, itemStack3, GlobalConstants.IgnoredStackAttributes);
			if (!(Empty || flag2) || itemStack3 == null)
			{
				return;
			}
			if (flag2)
			{
				itemstack.StackSize++;
			}
			else
			{
				itemstack = itemStack3;
			}
			MarkDirty();
			ItemStack itemStack4 = new ItemStack(world.GetBlock(AssetLocation.Create(sourceSlot.Itemstack.ItemAttributes["emptiedBlockCode"].AsString(), sourceSlot.Itemstack.Collectible.Code.Domain)));
			if (sourceSlot.StackSize == 1)
			{
				sourceSlot.Itemstack = itemStack4;
			}
			else
			{
				sourceSlot.Itemstack.StackSize--;
				if (!op.ActingPlayer.InventoryManager.TryGiveItemstack(itemStack4))
				{
					world.SpawnItemEntity(itemStack4, op.ActingPlayer.Entity.Pos.XYZ);
				}
			}
			sourceSlot.MarkDirty();
		}
		else
		{
			ItemStack itemStack5 = sourceSlot.Itemstack;
			if (itemStack5 == null || itemStack5.ItemAttributes?["contentItem2BlockCodes"].Exists != true)
			{
				base.ActivateSlotLeftClick(sourceSlot, ref op);
			}
		}
	}

	protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		IWorldAccessor world = inventory.Api.World;
		if (sourceSlot.Itemstack?.Block is BlockLiquidContainerBase blockLiquidContainerBase)
		{
			if (!Empty)
			{
				ItemStack content = blockLiquidContainerBase.GetContent(sourceSlot.Itemstack);
				float num = (op.ShiftDown ? blockLiquidContainerBase.CapacityLitres : blockLiquidContainerBase.TransferSizeLitres);
				WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(base.Itemstack);
				float val = (float)base.StackSize / (containableProps?.ItemsPerLitre ?? 1f);
				num *= (float)sourceSlot.Itemstack.StackSize;
				num = Math.Min(num, val);
				if (content == null)
				{
					int num2 = blockLiquidContainerBase.TryPutLiquid(sourceSlot.Itemstack, base.Itemstack, num / (float)sourceSlot.Itemstack.StackSize);
					TakeOut(num2 * sourceSlot.Itemstack.StackSize);
					MarkDirty();
				}
				else if (itemstack.Equals(world, content, GlobalConstants.IgnoredStackAttributes))
				{
					int num3 = blockLiquidContainerBase.TryPutLiquid(sourceSlot.Itemstack, blockLiquidContainerBase.GetContent(sourceSlot.Itemstack), num / (float)sourceSlot.Itemstack.StackSize);
					TakeOut(num3 * sourceSlot.Itemstack.StackSize);
					MarkDirty();
				}
			}
			return;
		}
		if (itemstack != null)
		{
			ItemStack itemStack = sourceSlot.Itemstack;
			if (itemStack != null && itemStack.ItemAttributes?["contentItem2BlockCodes"].Exists == true)
			{
				string text = sourceSlot.Itemstack.ItemAttributes["contentItem2BlockCodes"][itemstack.Collectible.Code.ToShortString()].AsString();
				if (text == null)
				{
					return;
				}
				ItemStack itemStack2 = new ItemStack(world.GetBlock(AssetLocation.Create(text, sourceSlot.Itemstack.Collectible.Code.Domain)));
				if (sourceSlot.StackSize == 1)
				{
					sourceSlot.Itemstack = itemStack2;
				}
				else
				{
					sourceSlot.Itemstack.StackSize--;
					if (!op.ActingPlayer.InventoryManager.TryGiveItemstack(itemStack2))
					{
						world.SpawnItemEntity(itemStack2, op.ActingPlayer.Entity.Pos.XYZ);
					}
				}
				sourceSlot.MarkDirty();
				TakeOut(1);
				return;
			}
		}
		ItemStack itemStack3 = sourceSlot.Itemstack;
		if ((itemStack3 == null || itemStack3.ItemAttributes?["contentItem2BlockCodes"].Exists != true) && sourceSlot.Itemstack?.ItemAttributes?["contentItemCode"].AsString() == null)
		{
			base.ActivateSlotRightClick(sourceSlot, ref op);
		}
	}
}
