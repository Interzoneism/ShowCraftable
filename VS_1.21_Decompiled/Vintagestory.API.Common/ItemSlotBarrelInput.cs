using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Vintagestory.API.Common;

public class ItemSlotBarrelInput : ItemSlot
{
	public ItemSlotBarrelInput(InventoryBase inventory)
		: base(inventory)
	{
	}

	public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		base.ActivateSlot(sourceSlot, ref op);
	}

	public override void OnItemSlotModified(ItemStack stack)
	{
		base.OnItemSlotModified(stack);
		if (itemstack == null)
		{
			return;
		}
		ItemSlotLiquidOnly itemSlotLiquidOnly = inventory[1] as ItemSlotLiquidOnly;
		bool flag = !itemSlotLiquidOnly.Empty && itemSlotLiquidOnly.Itemstack.Equals(inventory.Api.World, itemstack, GlobalConstants.IgnoredStackAttributes);
		if (flag)
		{
			int max = itemSlotLiquidOnly.Itemstack.Collectible.MaxStackSize - itemSlotLiquidOnly.Itemstack.StackSize;
			WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(itemSlotLiquidOnly.Itemstack);
			if (containableProps != null)
			{
				int val = (int)(itemSlotLiquidOnly.CapacityLitres * containableProps.ItemsPerLitre);
				int maxStackSize = containableProps.MaxStackSize;
				max = Math.Max(val, maxStackSize) - itemSlotLiquidOnly.Itemstack.StackSize;
			}
			int num = GameMath.Clamp(itemstack.StackSize, 0, max);
			itemSlotLiquidOnly.Itemstack.StackSize += num;
			itemstack.StackSize -= num;
			if (itemstack.StackSize <= 0)
			{
				itemstack = null;
			}
			itemSlotLiquidOnly.MarkDirty();
			MarkDirty();
			return;
		}
		JsonObject attributes = itemstack.Collectible.Attributes;
		if (attributes == null || !attributes.IsTrue("barrelMoveToLiquidSlot"))
		{
			if (!flag)
			{
				return;
			}
			JsonObject attributes2 = itemstack.Collectible.Attributes;
			if (attributes2 == null || !attributes2["waterTightContainerProps"].Exists)
			{
				return;
			}
		}
		if (flag)
		{
			int val2 = itemstack.Collectible.MaxStackSize - itemSlotLiquidOnly.StackSize;
			int num2 = Math.Min(itemstack.StackSize, val2);
			itemSlotLiquidOnly.Itemstack.StackSize += num2;
			itemstack.StackSize -= num2;
			if (base.StackSize <= 0)
			{
				itemstack = null;
			}
			MarkDirty();
			itemSlotLiquidOnly.MarkDirty();
		}
		else if (itemSlotLiquidOnly.Empty)
		{
			itemSlotLiquidOnly.Itemstack = itemstack.Clone();
			itemstack = null;
			MarkDirty();
			itemSlotLiquidOnly.MarkDirty();
		}
	}

	protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		ItemSlotLiquidOnly itemSlotLiquidOnly = inventory[1] as ItemSlotLiquidOnly;
		IWorldAccessor world = inventory.Api.World;
		if (sourceSlot?.Itemstack?.Collectible is ILiquidSink liquidSink && !itemSlotLiquidOnly.Empty && liquidSink.AllowHeldLiquidTransfer)
		{
			ItemStack itemStack = itemSlotLiquidOnly.Itemstack;
			ItemStack content = liquidSink.GetContent(sourceSlot.Itemstack);
			if (content != null && !itemStack.Equals(world, content, GlobalConstants.IgnoredStackAttributes))
			{
				return;
			}
			WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(itemStack);
			float val = (float)itemStack.StackSize / (containableProps?.ItemsPerLitre ?? 1f);
			float currentLitres = liquidSink.GetCurrentLitres(sourceSlot.Itemstack);
			float num = (op.CtrlDown ? liquidSink.TransferSizeLitres : (liquidSink.CapacityLitres - currentLitres));
			num *= (float)sourceSlot.StackSize;
			num = Math.Min(val, num);
			if (num > 0f)
			{
				op.MovedQuantity = liquidSink.TryPutLiquid(sourceSlot.Itemstack, itemStack, num / (float)sourceSlot.StackSize);
				itemSlotLiquidOnly.Itemstack.StackSize -= op.MovedQuantity * sourceSlot.StackSize;
				if (itemSlotLiquidOnly.Itemstack.StackSize <= 0)
				{
					itemSlotLiquidOnly.Itemstack = null;
				}
				itemSlotLiquidOnly.MarkDirty();
				sourceSlot.MarkDirty();
				EntityPos entityPos = op.ActingPlayer?.Entity?.Pos;
				if (entityPos != null)
				{
					op.World.PlaySoundAt(containableProps?.PourSound ?? ((AssetLocation)"sounds/effect/water-pour.ogg"), entityPos.X, entityPos.InternalY, entityPos.Z);
				}
			}
		}
		else
		{
			base.ActivateSlotRightClick(sourceSlot, ref op);
		}
	}

	protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		if (sourceSlot.Empty)
		{
			base.ActivateSlotLeftClick(sourceSlot, ref op);
			return;
		}
		IWorldAccessor world = inventory.Api.World;
		if (sourceSlot.Itemstack.Collectible is ILiquidSource { AllowHeldLiquidTransfer: not false } liquidSource)
		{
			ItemSlotLiquidOnly itemSlotLiquidOnly = inventory[1] as ItemSlotLiquidOnly;
			ItemStack content = liquidSource.GetContent(sourceSlot.Itemstack);
			bool flag = !itemSlotLiquidOnly.Empty && itemSlotLiquidOnly.Itemstack.Equals(world, content, GlobalConstants.IgnoredStackAttributes);
			if (!(itemSlotLiquidOnly.Empty || flag) || content == null)
			{
				return;
			}
			ItemStack itemStack = sourceSlot.Itemstack;
			WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(content);
			float num = containableProps?.ItemsPerLitre ?? 1f;
			float val = (op.CtrlDown ? liquidSource.TransferSizeLitres : liquidSource.CapacityLitres);
			float val2 = (float)content.StackSize / num * (float)itemStack.StackSize;
			float num2 = (float)itemSlotLiquidOnly.StackSize / num;
			val = Math.Min(val, val2);
			val = Math.Min(val, itemSlotLiquidOnly.CapacityLitres - num2);
			if (val > 0f)
			{
				int num3 = (int)(val * num);
				ItemStack itemStack2 = liquidSource.TryTakeContent(itemStack, num3 / itemStack.StackSize);
				itemStack2.StackSize *= itemStack.StackSize;
				itemStack2.StackSize += itemSlotLiquidOnly.StackSize;
				itemSlotLiquidOnly.Itemstack = itemStack2;
				itemSlotLiquidOnly.MarkDirty();
				op.MovedQuantity = num3;
				EntityPos entityPos = op.ActingPlayer?.Entity?.Pos;
				if (entityPos != null)
				{
					op.World.PlaySoundAt(containableProps?.FillSound ?? ((AssetLocation)"sounds/effect/water-fill.ogg"), entityPos.X, entityPos.InternalY, entityPos.Z);
				}
			}
			return;
		}
		string text = sourceSlot.Itemstack?.ItemAttributes?["contentItemCode"].AsString();
		if (text != null)
		{
			ItemSlot itemSlot = inventory[1];
			ItemStack itemStack3 = new ItemStack(world.GetItem(new AssetLocation(text)));
			bool flag2 = !itemSlot.Empty && itemSlot.Itemstack.Equals(world, itemStack3, GlobalConstants.IgnoredStackAttributes);
			if (!(itemSlot.Empty || flag2) || itemStack3 == null)
			{
				return;
			}
			if (flag2)
			{
				itemSlot.Itemstack.StackSize++;
			}
			else
			{
				itemSlot.Itemstack = itemStack3;
			}
			itemSlot.MarkDirty();
			ItemStack itemStack4 = new ItemStack(world.GetBlock(new AssetLocation(sourceSlot.Itemstack.ItemAttributes["emptiedBlockCode"].AsString())));
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
			base.ActivateSlotLeftClick(sourceSlot, ref op);
		}
	}
}
