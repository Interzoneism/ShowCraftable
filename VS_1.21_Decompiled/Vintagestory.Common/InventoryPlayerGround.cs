using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

internal class InventoryPlayerGround : InventoryBasePlayer
{
	private ItemSlot slot;

	public override int Count => 1;

	public override ItemSlot this[int slotId]
	{
		get
		{
			if (slotId != 0)
			{
				return null;
			}
			return slot;
		}
		set
		{
			if (slotId != 0)
			{
				throw new ArgumentOutOfRangeException("slotId");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			slot = value;
		}
	}

	public InventoryPlayerGround(string className, string playerUID, ICoreAPI api)
		: base(className, playerUID, api)
	{
		slot = new ItemSlotGround(this);
	}

	public InventoryPlayerGround(string inventoryID, ICoreAPI api)
		: base(inventoryID, api)
	{
		slot = new ItemSlotGround(this);
	}

	public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
	{
		return new WeightedSlot
		{
			slot = null,
			weight = 0f
		};
	}

	public override void FromTreeAttributes(ITreeAttribute tree)
	{
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
	}

	public override void OnItemSlotModified(ItemSlot slot)
	{
		Entity entity = Api.World.PlayerByUid(playerUID)?.Entity;
		if (slot.Itemstack != null && entity != null)
		{
			Vec3d vec3d = entity.SidedPos.XYZ.Add(0.0, entity.CollisionBox.Y1 + entity.CollisionBox.Y2 * 0.75f, 0.0);
			Vec3d vec3d2 = (entity.SidedPos.AheadCopy(1.0).XYZ.Add(entity.LocalEyePos) - vec3d) * 0.1 + entity.SidedPos.Motion * 1.5;
			ItemStack itemstack = slot.Itemstack;
			slot.Itemstack = null;
			while (itemstack.StackSize > 0)
			{
				Vec3d velocity = vec3d2.Clone().Add((float)(Api.World.Rand.NextDouble() - 0.5) / 60f, (float)(Api.World.Rand.NextDouble() - 0.5) / 60f, (float)(Api.World.Rand.NextDouble() - 0.5) / 60f);
				ItemStack itemStack = itemstack.Clone();
				itemStack.StackSize = Math.Min(4, itemstack.StackSize);
				itemstack.StackSize -= itemStack.StackSize;
				Api.World.SpawnItemEntity(itemStack, vec3d, velocity);
			}
		}
	}

	public override void MarkSlotDirty(int slotId)
	{
	}
}
