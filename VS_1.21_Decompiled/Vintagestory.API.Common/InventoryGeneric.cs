using System;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class InventoryGeneric : InventoryBase
{
	protected ItemSlot[] slots;

	private NewSlotDelegate onNewSlot;

	public GetSuitabilityDelegate OnGetSuitability;

	public GetAutoPushIntoSlotDelegate OnGetAutoPushIntoSlot;

	public GetAutoPullFromSlotDelegate OnGetAutoPullFromSlot;

	public Dictionary<EnumTransitionType, float> TransitionableSpeedMulByType { get; set; }

	public Dictionary<EnumFoodCategory, float> PerishableFactorByFoodCategory { get; set; }

	public float BaseWeight
	{
		get
		{
			return baseWeight;
		}
		set
		{
			baseWeight = value;
		}
	}

	public override int Count => slots.Length;

	public override ItemSlot this[int slotId]
	{
		get
		{
			if (slotId < 0 || slotId >= Count)
			{
				throw new ArgumentOutOfRangeException("slotId");
			}
			return slots[slotId];
		}
		set
		{
			if (slotId < 0 || slotId >= Count)
			{
				throw new ArgumentOutOfRangeException("slotId");
			}
			slots[slotId] = value ?? throw new ArgumentNullException("value");
		}
	}

	public InventoryGeneric(ICoreAPI api)
		: base("-", api)
	{
	}

	public void Init(int quantitySlots, string className, string instanceId, NewSlotDelegate onNewSlot = null)
	{
		instanceID = instanceId;
		base.className = className;
		OnGetSuitability = (ItemSlot s, ItemSlot t, bool isMerge) => (!isMerge) ? (baseWeight + 1f) : (baseWeight + 3f);
		this.onNewSlot = onNewSlot;
		slots = GenEmptySlots(quantitySlots);
	}

	public InventoryGeneric(int quantitySlots, string className, string instanceId, ICoreAPI api, NewSlotDelegate onNewSlot = null)
		: base(className, instanceId, api)
	{
		Init(quantitySlots, className, instanceId, onNewSlot);
	}

	public InventoryGeneric(int quantitySlots, string invId, ICoreAPI api, NewSlotDelegate onNewSlot = null)
		: base(invId, api)
	{
		OnGetSuitability = (ItemSlot s, ItemSlot t, bool isMerge) => (!isMerge) ? (baseWeight + 1f) : (baseWeight + 3f);
		this.onNewSlot = onNewSlot;
		slots = GenEmptySlots(quantitySlots);
	}

	public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
	{
		return OnGetSuitability(sourceSlot, targetSlot, isMerge);
	}

	public override void FromTreeAttributes(ITreeAttribute treeAttribute)
	{
		int num = slots.Length;
		slots = SlotsFromTreeAttributes(treeAttribute, slots);
		int amount = num - slots.Length;
		AddSlots(amount);
	}

	public void AddSlots(int amount)
	{
		while (amount-- > 0)
		{
			slots = slots.Append(NewSlot(slots.Length));
		}
	}

	public override void ToTreeAttributes(ITreeAttribute invtree)
	{
		SlotsToTreeAttributes(slots, invtree);
	}

	protected override ItemSlot NewSlot(int slotId)
	{
		if (onNewSlot != null)
		{
			return onNewSlot(slotId, this);
		}
		return new ItemSlotSurvival(this);
	}

	public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
	{
		float value = GetDefaultTransitionSpeedMul(transType);
		if (transType == EnumTransitionType.Perish && PerishableFactorByFoodCategory != null && stack.Collectible.NutritionProps != null)
		{
			if (!PerishableFactorByFoodCategory.TryGetValue(stack.Collectible.NutritionProps.FoodCategory, out value))
			{
				value = GetDefaultTransitionSpeedMul(transType);
			}
		}
		else if (TransitionableSpeedMulByType != null && !TransitionableSpeedMulByType.TryGetValue(transType, out value))
		{
			value = GetDefaultTransitionSpeedMul(transType);
		}
		return InvokeTransitionSpeedDelegates(transType, stack, value);
	}

	public override ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
	{
		if (OnGetAutoPullFromSlot != null)
		{
			return OnGetAutoPullFromSlot(atBlockFace);
		}
		return base.GetAutoPullFromSlot(atBlockFace);
	}

	public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
	{
		if (OnGetAutoPushIntoSlot != null)
		{
			return OnGetAutoPushIntoSlot(atBlockFace, fromSlot);
		}
		return base.GetAutoPushIntoSlot(atBlockFace, fromSlot);
	}
}
