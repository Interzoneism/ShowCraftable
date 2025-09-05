using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class InventorySmelting : InventoryBase, ISlotProvider
{
	private ItemSlot[] slots;

	private ItemSlot[] cookingSlots;

	public BlockPos pos;

	private int defaultStorageType = 189;

	public ItemSlot[] CookingSlots
	{
		get
		{
			if (!HaveCookingContainer)
			{
				return Array.Empty<ItemSlot>();
			}
			return cookingSlots;
		}
	}

	public ItemSlot[] Slots => cookingSlots;

	public override Size3f MaxContentDimensions
	{
		get
		{
			return slots[1].Itemstack?.ItemAttributes?["maxContentDimensions"].AsObject<Size3f>();
		}
		set
		{
		}
	}

	public bool HaveCookingContainer
	{
		get
		{
			ItemStack itemstack = slots[1].Itemstack;
			if (itemstack == null)
			{
				return false;
			}
			return itemstack.ItemAttributes?.KeyExists("cookingContainerSlots") == true;
		}
	}

	public float CookingSlotCapacityLitres
	{
		get
		{
			ItemSlot[] array = slots;
			return ((array == null) ? ((float?)null) : array[1]?.Itemstack?.ItemAttributes?["cookingSlotCapacityLitres"].AsFloat(6f)) ?? 6f;
		}
	}

	public int CookingContainerMaxSlotStackSize
	{
		get
		{
			if (!HaveCookingContainer)
			{
				return 0;
			}
			return slots[1].Itemstack.ItemAttributes["maxContainerSlotStackSize"].AsInt(999);
		}
	}

	public override int Count => slots.Length;

	public override ItemSlot this[int slotId]
	{
		get
		{
			if (slotId < 0 || slotId >= Count)
			{
				return null;
			}
			return slots[slotId];
		}
		set
		{
			if (slotId < 0 || slotId >= Count)
			{
				throw new ArgumentOutOfRangeException("slotId");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			slots[slotId] = value;
		}
	}

	public override bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot)
	{
		if (GetSlotId(sinkSlot) >= 3)
		{
			return base.CanContain(sinkSlot, sourceSlot);
		}
		return true;
	}

	public InventorySmelting(string inventoryID, ICoreAPI api)
		: base(inventoryID, api)
	{
		slots = GenEmptySlots(7);
		cookingSlots = new ItemSlot[4]
		{
			slots[3],
			slots[4],
			slots[5],
			slots[6]
		};
		baseWeight = 4f;
	}

	public InventorySmelting(string className, string instanceID, ICoreAPI api)
		: base(className, instanceID, api)
	{
		slots = GenEmptySlots(7);
		cookingSlots = new ItemSlot[4]
		{
			slots[3],
			slots[4],
			slots[5],
			slots[6]
		};
		baseWeight = 4f;
	}

	public override void LateInitialize(string inventoryID, ICoreAPI api)
	{
		base.LateInitialize(inventoryID, api);
		for (int i = 0; i < cookingSlots.Length; i++)
		{
			cookingSlots[i].MaxSlotStackSize = CookingContainerMaxSlotStackSize;
		}
		updateStorageTypeFromContainer(slots[1].Itemstack);
	}

	public override void DidModifyItemSlot(ItemSlot slot, ItemStack extractedStack = null)
	{
		base.DidModifyItemSlot(slot, extractedStack);
		if (slots[1] == slot)
		{
			if (slot == null || slot.Itemstack?.ItemAttributes?["storageType"].Exists != true)
			{
				discardCookingSlots();
			}
			else
			{
				updateStorageTypeFromContainer(slot.Itemstack);
			}
		}
	}

	private void updateStorageTypeFromContainer(ItemStack stack)
	{
		int storageType = defaultStorageType;
		if (stack?.ItemAttributes?["storageType"] != null)
		{
			storageType = stack.ItemAttributes["storageType"].AsInt(defaultStorageType);
		}
		for (int i = 0; i < cookingSlots.Length; i++)
		{
			cookingSlots[i].StorageType = (EnumItemStorageFlags)storageType;
			cookingSlots[i].MaxSlotStackSize = CookingContainerMaxSlotStackSize;
			(cookingSlots[i] as ItemSlotWatertight).capacityLitres = CookingSlotCapacityLitres;
		}
	}

	public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
	{
		return base.GetTransitionSpeedMul(transType, stack);
	}

	public void discardCookingSlots()
	{
		Vec3d position = pos.ToVec3d().Add(0.5, 0.5, 0.5);
		for (int i = 0; i < cookingSlots.Length; i++)
		{
			if (cookingSlots[i] != null)
			{
				Api.World.SpawnItemEntity(cookingSlots[i].Itemstack, position);
				cookingSlots[i].Itemstack = null;
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree)
	{
		List<ItemSlot> list = new List<ItemSlot>();
		slots = SlotsFromTreeAttributes(tree, slots, list);
		for (int i = 0; i < list.Count; i++)
		{
			DidModifyItemSlot(list[i]);
		}
		if (Api != null)
		{
			for (int j = 0; j < cookingSlots.Length; j++)
			{
				cookingSlots[j].MaxSlotStackSize = CookingContainerMaxSlotStackSize;
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		SlotsToTreeAttributes(slots, tree);
	}

	public override void OnItemSlotModified(ItemSlot slot)
	{
		base.OnItemSlotModified(slot);
	}

	protected override ItemSlot NewSlot(int i)
	{
		return i switch
		{
			0 => new ItemSlotSurvival(this), 
			1 => new ItemSlotInput(this, 2), 
			2 => new ItemSlotOutput(this), 
			_ => new ItemSlotWatertight(this, CookingSlotCapacityLitres), 
		};
	}

	public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
	{
		if (!HaveCookingContainer)
		{
			if (skipSlots == null)
			{
				skipSlots = new List<ItemSlot>();
			}
			skipSlots.Add(slots[2]);
			skipSlots.Add(slots[3]);
			skipSlots.Add(slots[4]);
			skipSlots.Add(slots[5]);
			skipSlots.Add(slots[6]);
		}
		return base.GetBestSuitedSlot(sourceSlot, op, skipSlots);
	}

	public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
	{
		ItemStack itemstack = sourceSlot.Itemstack;
		if (targetSlot == slots[1] && (itemstack.Collectible is BlockSmeltingContainer || itemstack.Collectible is BlockCookingContainer))
		{
			return 2.2f;
		}
		if (targetSlot == slots[0] && (itemstack.Collectible.CombustibleProps == null || itemstack.Collectible.CombustibleProps.BurnTemperature <= 0))
		{
			return 0f;
		}
		if (targetSlot == slots[1] && (itemstack.Collectible.CombustibleProps == null || itemstack.Collectible.CombustibleProps.SmeltedStack == null))
		{
			return 0.5f;
		}
		return base.GetSuitability(sourceSlot, targetSlot, isMerge);
	}

	public string GetOutputText()
	{
		ItemStack itemstack = slots[1].Itemstack;
		if (itemstack == null)
		{
			return null;
		}
		if (itemstack.Collectible is BlockSmeltingContainer)
		{
			return ((BlockSmeltingContainer)itemstack.Collectible).GetOutputText(Api.World, this, slots[1]);
		}
		if (itemstack.Collectible is BlockCookingContainer)
		{
			return ((BlockCookingContainer)itemstack.Collectible).GetOutputText(Api.World, this, slots[1]);
		}
		ItemStack itemStack = itemstack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;
		if (itemStack == null)
		{
			return null;
		}
		if (itemstack.Collectible.CombustibleProps.SmeltingType == EnumSmeltType.Fire)
		{
			return Lang.Get("Can't smelt, requires a kiln");
		}
		if (itemstack.Collectible.CombustibleProps.RequiresContainer)
		{
			return Lang.Get("Can't smelt, requires smelting container (i.e. Crucible)");
		}
		return Lang.Get("firepit-gui-willcreate", itemstack.StackSize / itemstack.Collectible.CombustibleProps.SmeltedRatio, itemStack.GetName());
	}
}
