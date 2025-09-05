using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class InventoryCraftingGrid : InventoryBasePlayer
{
	private int GridSize = 3;

	private int GridSizeSq = 9;

	private ItemSlot[] slots;

	private ItemSlotCraftingOutput outputSlot;

	public GridRecipe MatchingRecipe;

	private bool isCrafting;

	public override int Count => GridSizeSq + 1;

	public override ItemSlot this[int slotId]
	{
		get
		{
			if (slotId < 0 || slotId >= Count)
			{
				return null;
			}
			if (slotId == GridSizeSq)
			{
				return outputSlot;
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
			if (slotId == GridSizeSq)
			{
				outputSlot = (ItemSlotCraftingOutput)value;
			}
			else
			{
				slots[slotId] = value;
			}
		}
	}

	public InventoryCraftingGrid(string inventoryID, ICoreAPI api)
		: base(inventoryID, api)
	{
		slots = GenEmptySlots(GridSizeSq);
		outputSlot = new ItemSlotCraftingOutput(this);
		InvNetworkUtil = new CraftingInventoryNetworkUtil(this, api);
	}

	public InventoryCraftingGrid(string className, string instanceID, ICoreAPI api)
		: base(className, instanceID, api)
	{
		slots = GenEmptySlots(GridSizeSq);
		outputSlot = new ItemSlotCraftingOutput(this);
		InvNetworkUtil = new CraftingInventoryNetworkUtil(this, api);
	}

	public override void LateInitialize(string inventoryID, ICoreAPI api)
	{
		base.LateInitialize(inventoryID, api);
		(InvNetworkUtil as CraftingInventoryNetworkUtil).Api = api;
	}

	internal void BeginCraft()
	{
		isCrafting = true;
	}

	internal void EndCraft()
	{
		isCrafting = false;
		FindMatchingRecipe();
	}

	public bool CanStillCraftCurrent()
	{
		if (MatchingRecipe != null)
		{
			return MatchingRecipe.Matches(Api.World.PlayerByUid(playerUID), slots, GridSize);
		}
		return false;
	}

	public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
	{
		if (stack == outputSlot.Itemstack)
		{
			return 0f;
		}
		return base.GetTransitionSpeedMul(transType, stack);
	}

	public override object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		object result;
		if (slotId == GridSizeSq)
		{
			BeginCraft();
			result = base.ActivateSlot(slotId, sourceSlot, ref op);
			if (!outputSlot.Empty && op.ShiftDown)
			{
				if (Api.Side == EnumAppSide.Client)
				{
					outputSlot.Itemstack = null;
				}
				else
				{
					base.Player.InventoryManager.DropItem(outputSlot, fullStack: true);
				}
			}
			EndCraft();
		}
		else
		{
			result = base.ActivateSlot(slotId, sourceSlot, ref op);
		}
		return result;
	}

	public override void OnItemSlotModified(ItemSlot slot)
	{
		if (!isCrafting && !(slot is ItemSlotCraftingOutput))
		{
			DropSlotIfHot(slot, base.Player);
			FindMatchingRecipe();
		}
	}

	public override void DidModifyItemSlot(ItemSlot slot, ItemStack extractedStack = null)
	{
		base.DidModifyItemSlot(slot, extractedStack);
	}

	public override bool TryMoveItemStack(IPlayer player, string[] invIds, int[] slotIds, ref ItemStackMoveOperation op)
	{
		bool num = base.TryMoveItemStack(player, invIds, slotIds, ref op);
		if (num)
		{
			FindMatchingRecipe();
		}
		return num;
	}

	internal void FindMatchingRecipe()
	{
		MatchingRecipe = null;
		outputSlot.Itemstack = null;
		List<GridRecipe> gridRecipes = Api.World.GridRecipes;
		IPlayer forPlayer = Api.World.PlayerByUid(playerUID);
		foreach (GridRecipe item in gridRecipes)
		{
			if (!item.Shapeless && item.Enabled && item.Matches(forPlayer, slots, GridSize))
			{
				FoundMatch(item);
				return;
			}
		}
		foreach (GridRecipe item2 in gridRecipes)
		{
			if (item2.Shapeless && item2.Enabled && item2.Matches(forPlayer, slots, GridSize))
			{
				FoundMatch(item2);
				return;
			}
		}
		dirtySlots.Add(GridSizeSq);
	}

	private void FoundMatch(GridRecipe recipe)
	{
		MatchingRecipe = recipe;
		MatchingRecipe.GenerateOutputStack(slots, outputSlot);
		dirtySlots.Add(GridSizeSq);
	}

	internal void ConsumeIngredients(ItemSlot outputSlot)
	{
		if (MatchingRecipe != null && outputSlot.Itemstack != null)
		{
			if (!outputSlot.Itemstack.Collectible.ConsumeCraftingIngredients(slots, outputSlot, MatchingRecipe))
			{
				MatchingRecipe.ConsumeInput(Api.World.PlayerByUid(playerUID), slots, GridSize);
			}
			for (int i = 0; i < GridSizeSq + 1; i++)
			{
				dirtySlots.Add(i);
			}
		}
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
		ItemSlot[] array = SlotsFromTreeAttributes(tree);
		if (array?.Length == slots.Length)
		{
			slots = array;
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		SlotsToTreeAttributes(slots, tree);
		ResolveBlocksOrItems();
	}

	public override void OnOwningEntityDeath(Vec3d pos)
	{
		using IEnumerator<ItemSlot> enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			ItemSlot current = enumerator.Current;
			if (!(current is ItemSlotCraftingOutput) && !current.Empty)
			{
				Api.World.SpawnItemEntity(current.Itemstack, pos);
				current.Itemstack = null;
				current.MarkDirty();
			}
		}
	}
}
