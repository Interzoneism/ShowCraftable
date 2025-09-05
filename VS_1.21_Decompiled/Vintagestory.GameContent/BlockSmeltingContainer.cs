using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockSmeltingContainer : Block
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			failureCode = "__ignore__";
			return false;
		}
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		if (world.BlockAccessor.GetBlock(blockSel.Position.DownCopy()).CanAttachBlockAt(world.BlockAccessor, this, blockSel.Position.DownCopy(), BlockFacing.UP))
		{
			DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			return true;
		}
		failureCode = "requiresolidground";
		return false;
	}

	public override float GetMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
	{
		float num = 0f;
		ItemStack[] ingredients = GetIngredients(world, cookingSlotsProvider);
		for (int i = 0; i < ingredients.Length; i++)
		{
			if (ingredients[i]?.Collectible?.CombustibleProps != null)
			{
				float meltingDuration = ingredients[i].Collectible.GetMeltingDuration(world, cookingSlotsProvider, inputSlot);
				num += meltingDuration * (float)ingredients[i].StackSize / (float)ingredients[i].Collectible.CombustibleProps.SmeltedRatio;
			}
		}
		return num;
	}

	public override float GetMeltingPoint(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
	{
		float num = 0f;
		ItemStack[] ingredients = GetIngredients(world, cookingSlotsProvider);
		for (int i = 0; i < ingredients.Length; i++)
		{
			if (ingredients[i] != null)
			{
				num = Math.Max(num, ingredients[i].Collectible.GetMeltingPoint(world, cookingSlotsProvider, inputSlot));
			}
		}
		return num;
	}

	public override bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
	{
		ItemStack[] ingredients = GetIngredients(world, cookingSlotsProvider);
		if (GetMatchingAlloy(world, ingredients) != null)
		{
			return true;
		}
		for (int i = 0; i < ingredients.Length; i++)
		{
			CombustibleProperties combustibleProperties = ingredients[i]?.Collectible.CombustibleProps;
			if (combustibleProperties != null && !combustibleProperties.RequiresContainer)
			{
				return false;
			}
		}
		return GetSingleSmeltableStack(ingredients) != null;
	}

	public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
	{
		ItemStack[] ingredients = GetIngredients(world, cookingSlotsProvider);
		AlloyRecipe matchingAlloy = GetMatchingAlloy(world, ingredients);
		Block block = world.GetBlock(CodeWithVariant("type", "smelted"));
		ItemStack itemStack = new ItemStack(block);
		if (matchingAlloy != null)
		{
			ItemStack output = matchingAlloy.Output.ResolvedItemstack.Clone();
			int units = (int)Math.Round(matchingAlloy.GetTotalOutputQuantity(ingredients) * 100.0, 0);
			((BlockSmeltedContainer)block).SetContents(itemStack, output, units);
			itemStack.Collectible.SetTemperature(world, itemStack, GetIngredientsTemperature(world, ingredients));
			outputSlot.Itemstack = itemStack;
			inputSlot.Itemstack = null;
			for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
			{
				cookingSlotsProvider.Slots[i].Itemstack = null;
			}
			return;
		}
		MatchedSmeltableStack singleSmeltableStack = GetSingleSmeltableStack(ingredients);
		if (singleSmeltableStack != null)
		{
			((BlockSmeltedContainer)block).SetContents(itemStack, singleSmeltableStack.output, (int)Math.Round(singleSmeltableStack.stackSize * 100.0, 0));
			itemStack.Collectible.SetTemperature(world, itemStack, GetIngredientsTemperature(world, ingredients));
			outputSlot.Itemstack = itemStack;
			inputSlot.Itemstack = null;
			for (int j = 0; j < cookingSlotsProvider.Slots.Length; j++)
			{
				cookingSlotsProvider.Slots[j].Itemstack = null;
			}
		}
	}

	public string GetOutputText(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
	{
		if (inputSlot.Itemstack == null)
		{
			return null;
		}
		if (inputSlot.Itemstack.Collectible is BlockSmeltingContainer)
		{
			BlockSmeltingContainer blockSmeltingContainer = (BlockSmeltingContainer)inputSlot.Itemstack.Collectible;
			ItemStack[] ingredients = blockSmeltingContainer.GetIngredients(world, cookingSlotsProvider);
			for (int i = 0; i < ingredients.Length; i++)
			{
				CombustibleProperties combustibleProperties = ingredients[i]?.Collectible.CombustibleProps;
				if (combustibleProperties != null && !combustibleProperties.RequiresContainer)
				{
					return null;
				}
			}
			AlloyRecipe matchingAlloy = blockSmeltingContainer.GetMatchingAlloy(world, ingredients);
			if (matchingAlloy != null)
			{
				double totalOutputQuantity = matchingAlloy.GetTotalOutputQuantity(ingredients);
				return Lang.Get("Will create {0} units of {1}", (int)Math.Round(totalOutputQuantity * 100.0, 0), GetMetal(matchingAlloy.Output.ResolvedItemstack));
			}
			MatchedSmeltableStack singleSmeltableStack = GetSingleSmeltableStack(ingredients);
			if (singleSmeltableStack != null)
			{
				return Lang.Get("Will create {0} units of {1}", (int)Math.Round(singleSmeltableStack.stackSize * 100.0, 0), GetMetal(singleSmeltableStack.output));
			}
			return null;
		}
		return null;
	}

	public static string GetMetal(ItemStack ingot)
	{
		if (ingot.Collectible.Variant.TryGetValue("metal", out var value))
		{
			return Lang.Get("material-" + value);
		}
		if (ingot.Collectible.Code.Path.Equals("ironbloom"))
		{
			return Lang.Get("material-iron");
		}
		return ingot.GetName();
	}

	public AlloyRecipe GetMatchingAlloy(IWorldAccessor world, ItemStack[] stacks)
	{
		List<AlloyRecipe> metalAlloys = api.GetMetalAlloys();
		if (metalAlloys == null)
		{
			return null;
		}
		for (int i = 0; i < metalAlloys.Count; i++)
		{
			if (metalAlloys[i].Matches(stacks))
			{
				return metalAlloys[i];
			}
		}
		return null;
	}

	public float GetIngredientsTemperature(IWorldAccessor world, ItemStack[] ingredients)
	{
		bool flag = false;
		float num = 0f;
		for (int i = 0; i < ingredients.Length; i++)
		{
			if (ingredients[i] != null)
			{
				float temperature = ingredients[i].Collectible.GetTemperature(world, ingredients[i]);
				num = (flag ? Math.Min(num, temperature) : temperature);
				flag = true;
			}
		}
		return num;
	}

	public static MatchedSmeltableStack GetSingleSmeltableStack(ItemStack[] stacks)
	{
		ItemStack itemStack = null;
		double num = 0.0;
		for (int i = 0; i < stacks.Length; i++)
		{
			if (stacks[i] == null)
			{
				continue;
			}
			ItemStack itemStack2 = stacks[i];
			double num2 = itemStack2.StackSize;
			if (itemStack2.Collectible.CombustibleProps?.SmeltedStack != null && itemStack2.Collectible.CombustibleProps.MeltingPoint > 0)
			{
				num2 *= (double)itemStack2.Collectible.CombustibleProps.SmeltedStack.StackSize;
				num2 /= (double)itemStack2.Collectible.CombustibleProps.SmeltedRatio;
				itemStack2 = itemStack2.Collectible.CombustibleProps.SmeltedStack.ResolvedItemstack;
				if (itemStack == null)
				{
					itemStack = itemStack2.Clone();
					num += num2;
					continue;
				}
				if (itemStack.Class != itemStack2.Class || itemStack.Id != itemStack2.Id)
				{
					return null;
				}
				num += num2;
				continue;
			}
			return null;
		}
		if (itemStack == null)
		{
			return null;
		}
		if (itemStack.Collectible is BlockSmeltingContainer)
		{
			return null;
		}
		return new MatchedSmeltableStack
		{
			output = itemStack,
			stackSize = num
		};
	}

	public ItemStack[] GetIngredients(IWorldAccessor world, ISlotProvider cookingSlotsProvider)
	{
		ItemStack[] array = new ItemStack[cookingSlotsProvider.Slots.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = cookingSlotsProvider.Slots[i].Itemstack;
		}
		return array;
	}
}
