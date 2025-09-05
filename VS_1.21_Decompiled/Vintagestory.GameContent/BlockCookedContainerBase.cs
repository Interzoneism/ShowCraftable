using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockCookedContainerBase : BlockContainer, IBlockMealContainer, IContainedInteractable, IContainedCustomName, IHandBookPageCodeProvider
{
	public void SetContents(string? recipeCode, float servings, ItemStack containerStack, ItemStack[] stacks)
	{
		base.SetContents(containerStack, stacks);
		containerStack.Attributes.SetFloat("quantityServings", servings);
		if (recipeCode != null)
		{
			containerStack.Attributes.SetString("recipeCode", recipeCode);
		}
	}

	public void SetContents(string? recipeCode, ItemStack containerStack, ItemStack?[] stacks, float quantityServings = 1f)
	{
		base.SetContents(containerStack, stacks);
		if (recipeCode == null)
		{
			containerStack.Attributes.RemoveAttribute("recipeCode");
		}
		else
		{
			containerStack.Attributes.SetString("recipeCode", recipeCode);
		}
		containerStack.Attributes.SetFloat("quantityServings", quantityServings);
	}

	public float GetQuantityServings(IWorldAccessor world, ItemStack byItemStack)
	{
		return (float)byItemStack.Attributes.GetDecimal("quantityServings");
	}

	public void SetQuantityServings(IWorldAccessor world, ItemStack byItemStack, float value)
	{
		if (value <= 0f)
		{
			SetRecipeCode(world, byItemStack, null);
		}
		else
		{
			byItemStack.Attributes.SetFloat("quantityServings", value);
		}
	}

	public CookingRecipe? GetCookingRecipe(IWorldAccessor world, ItemStack? containerStack)
	{
		return api.GetCookingRecipe(GetRecipeCode(world, containerStack));
	}

	public string? GetRecipeCode(IWorldAccessor world, ItemStack? containerStack)
	{
		return containerStack?.Attributes.GetString("recipeCode");
	}

	public void SetRecipeCode(IWorldAccessor world, ItemStack containerStack, string? code)
	{
		if (code == null)
		{
			containerStack.Attributes.RemoveAttribute("recipeCode");
			containerStack.Attributes.RemoveAttribute("quantityServings");
			containerStack.Attributes.RemoveAttribute("contents");
		}
		else
		{
			containerStack.Attributes.SetString("recipeCode", code);
		}
	}

	internal float GetServings(IWorldAccessor world, ItemStack? byItemStack)
	{
		return (float)(byItemStack?.Attributes.GetDecimal("quantityServings") ?? 0.0);
	}

	internal void SetServings(IWorldAccessor world, ItemStack byItemStack, float value)
	{
		byItemStack.Attributes.SetFloat("quantityServings", value);
	}

	internal void SetServingsMaybeEmpty(IWorldAccessor world, ItemSlot potslot, float value)
	{
		SetQuantityServings(world, potslot.Itemstack, value);
		if (!(value <= 0f))
		{
			return;
		}
		string text = Attributes["emptiedBlockCode"].AsString();
		if (text != null)
		{
			Block block = world.GetBlock(new AssetLocation(text));
			if (block != null)
			{
				potslot.Itemstack = new ItemStack(block);
			}
		}
	}

	public CookingRecipe? GetMealRecipe(IWorldAccessor world, ItemStack? containerStack)
	{
		string recipeCode = GetRecipeCode(world, containerStack);
		return api.GetCookingRecipe(recipeCode);
	}

	public void ServeIntoBowl(Block selectedBlock, BlockPos pos, ItemSlot potslot, IWorldAccessor world)
	{
		if (world.Side == EnumAppSide.Client)
		{
			return;
		}
		string domainAndPath = selectedBlock.Attributes["mealBlockCode"].AsString();
		Block block = api.World.GetBlock(new AssetLocation(domainAndPath));
		world.BlockAccessor.SetBlock(block.BlockId, pos);
		if (api.World.BlockAccessor.GetBlockEntity(pos) is IBlockEntityMealContainer blockEntityMealContainer && !tryMergeServingsIntoBE(blockEntityMealContainer, potslot))
		{
			blockEntityMealContainer.RecipeCode = GetRecipeCode(world, potslot.Itemstack);
			ItemStack[] nonEmptyContents = GetNonEmptyContents(api.World, potslot.Itemstack);
			for (int i = 0; i < nonEmptyContents.Length; i++)
			{
				blockEntityMealContainer.inventory[i].Itemstack = nonEmptyContents[i].Clone();
			}
			float servings = GetServings(world, potslot.Itemstack);
			float num = (blockEntityMealContainer.QuantityServings = Math.Min(servings, selectedBlock.Attributes["servingCapacity"].AsFloat(1f)));
			SetServingsMaybeEmpty(world, potslot, servings - num);
			potslot.MarkDirty();
			blockEntityMealContainer.MarkDirty(redrawonclient: true);
		}
	}

	public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority)
	{
		if (priority != EnumMergePriority.AutoMerge && (sourceStack?.Block is IBlockMealContainer || sourceStack?.Collectible?.Attributes?.IsTrue("mealContainer") == true) && GetServings(api.World, sinkStack) > 0f)
		{
			return Math.Max(1, Math.Min(MaxStackSize - sinkStack.StackSize, sourceStack.StackSize));
		}
		return base.GetMergableQuantity(sinkStack, sourceStack, priority);
	}

	public override void TryMergeStacks(ItemStackMergeOperation op)
	{
		if (!(op.SourceSlot.Itemstack.Block is IBlockMealContainer))
		{
			JsonObject attributes = op.SourceSlot.Itemstack.Collectible.Attributes;
			if (attributes == null || !attributes.IsTrue("mealContainer"))
			{
				base.TryMergeStacks(op);
				return;
			}
		}
		if (op.CurrentPriority != EnumMergePriority.DirectMerge)
		{
			if (Math.Min(MaxStackSize - op.SinkSlot.Itemstack.StackSize, op.SourceSlot.Itemstack.StackSize) > 0)
			{
				base.TryMergeStacks(op);
			}
			return;
		}
		ItemStack itemStack = null;
		if (op.SourceSlot.Itemstack.StackSize > 1)
		{
			itemStack = op.SourceSlot.TakeOut(op.SourceSlot.Itemstack.StackSize - 1);
		}
		if (ServeIntoStack(op.SourceSlot, op.SinkSlot, op.World))
		{
			if (api is ICoreServerAPI)
			{
				IPlayer actingPlayer = op.ActingPlayer;
				if (actingPlayer != null && !actingPlayer.Entity.TryGiveItemStack(itemStack))
				{
					op.World.SpawnItemEntity(itemStack, op.ActingPlayer.Entity.Pos.AsBlockPos);
				}
			}
		}
		else
		{
			new DummySlot(itemStack).TryPutInto(op.World, op.SourceSlot);
			if (Math.Min(MaxStackSize - op.SinkSlot.Itemstack.StackSize, op.SourceSlot.Itemstack.StackSize) > 0)
			{
				base.TryMergeStacks(op);
			}
		}
	}

	private bool tryMergeServingsIntoBE(IBlockEntityMealContainer bemeal, ItemSlot potslot)
	{
		ItemStack[] nonEmptyContents = GetNonEmptyContents(api.World, potslot.Itemstack);
		string recipeCode = bemeal.RecipeCode;
		ItemStack[] nonEmptyContentStacks = bemeal.GetNonEmptyContentStacks();
		float quantityServings = bemeal.QuantityServings;
		string recipeCode2 = GetRecipeCode(api.World, potslot.Itemstack);
		float num = (bemeal as BlockEntity)?.Block.Attributes["servingCapacity"].AsFloat(1f) ?? 1f;
		if (nonEmptyContentStacks == null || quantityServings == 0f)
		{
			return false;
		}
		if (nonEmptyContents.Length != nonEmptyContentStacks.Length)
		{
			return true;
		}
		if (recipeCode2 != recipeCode)
		{
			return true;
		}
		float num2 = num - quantityServings;
		if (num2 <= 0f)
		{
			return true;
		}
		for (int i = 0; i < nonEmptyContents.Length; i++)
		{
			if (!nonEmptyContents[i].Equals(api.World, nonEmptyContentStacks[i], GlobalConstants.IgnoredStackAttributes))
			{
				return true;
			}
		}
		for (int j = 0; j < nonEmptyContentStacks.Length; j++)
		{
			ItemStackMergeOperation itemStackMergeOperation = new ItemStackMergeOperation(api.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.ConfirmedMerge, nonEmptyContents[j].StackSize);
			itemStackMergeOperation.SourceSlot = new DummySlot(nonEmptyContents[j]);
			itemStackMergeOperation.SinkSlot = new DummySlot(nonEmptyContentStacks[j]);
			nonEmptyContentStacks[j].Collectible.TryMergeStacks(itemStackMergeOperation);
		}
		float servings = GetServings(api.World, potslot.Itemstack);
		float num3 = Math.Min(num2, servings);
		bemeal.QuantityServings = quantityServings + num3;
		SetServingsMaybeEmpty(api.World, potslot, servings - num3);
		potslot.MarkDirty();
		bemeal.MarkDirty(redrawonclient: true);
		return true;
	}

	public bool ServeIntoStack(ItemSlot bowlSlot, ItemSlot potslot, IWorldAccessor world)
	{
		float servings = GetServings(world, potslot.Itemstack);
		string recipeCode = GetRecipeCode(world, potslot.Itemstack);
		float num = bowlSlot.Itemstack?.Block.Attributes["servingCapacity"].AsFloat(1f) ?? 1f;
		if (bowlSlot.Itemstack?.Block is IBlockMealContainer blockMealContainer)
		{
			ItemStack[] nonEmptyContents = GetNonEmptyContents(api.World, potslot.Itemstack);
			string recipeCode2 = blockMealContainer.GetRecipeCode(world, bowlSlot.Itemstack);
			ItemStack[] nonEmptyContents2 = blockMealContainer.GetNonEmptyContents(world, bowlSlot.Itemstack);
			float quantityServings = blockMealContainer.GetQuantityServings(world, bowlSlot.Itemstack);
			if (nonEmptyContents2 != null && quantityServings > 0f)
			{
				if (nonEmptyContents.Length != nonEmptyContents2.Length)
				{
					return false;
				}
				if (recipeCode != recipeCode2)
				{
					return false;
				}
				float num2 = num - quantityServings;
				if (num2 <= 0f)
				{
					return false;
				}
				for (int i = 0; i < nonEmptyContents.Length; i++)
				{
					if (!nonEmptyContents[i].Equals(world, nonEmptyContents2[i], GlobalConstants.IgnoredStackAttributes))
					{
						return false;
					}
				}
				if (world.Side == EnumAppSide.Client)
				{
					return true;
				}
				for (int j = 0; j < nonEmptyContents2.Length; j++)
				{
					ItemStackMergeOperation itemStackMergeOperation = new ItemStackMergeOperation(world, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.ConfirmedMerge, nonEmptyContents[j].StackSize);
					itemStackMergeOperation.SourceSlot = new DummySlot(nonEmptyContents[j]);
					itemStackMergeOperation.SinkSlot = new DummySlot(nonEmptyContents2[j]);
					nonEmptyContents2[j].Collectible.TryMergeStacks(itemStackMergeOperation);
				}
				float num3 = Math.Min(num2, servings);
				blockMealContainer.SetQuantityServings(world, bowlSlot.Itemstack, quantityServings + num3);
				SetServingsMaybeEmpty(world, potslot, servings - num3);
				potslot.Itemstack?.Attributes.RemoveAttribute("sealed");
				potslot.MarkDirty();
				bowlSlot.MarkDirty();
				return true;
			}
		}
		if (bowlSlot.Itemstack?.Block is BlockLiquidContainerTopOpened)
		{
			ItemStack[] contents = GetContents(world, bowlSlot.Itemstack);
			if (contents != null && contents.Length != 0)
			{
				return false;
			}
		}
		if (world.Side == EnumAppSide.Client)
		{
			return true;
		}
		ItemStack[] contents2 = GetContents(api.World, potslot.Itemstack);
		string text = bowlSlot.Itemstack?.Block.Attributes["mealBlockCode"].AsString();
		if (text == null)
		{
			return false;
		}
		Block block = api.World.GetBlock(new AssetLocation(text));
		float num4 = Math.Min(servings, num);
		ItemStack itemStack = new ItemStack(block);
		(block as IBlockMealContainer)?.SetContents(recipeCode, itemStack, contents2, num4);
		SetServingsMaybeEmpty(world, potslot, servings - num4);
		potslot.Itemstack?.Attributes.RemoveAttribute("sealed");
		potslot.MarkDirty();
		bowlSlot.Itemstack = itemStack;
		bowlSlot.MarkDirty();
		return true;
	}

	public string GetContainedName(ItemSlot inSlot, int quantity)
	{
		return GetHeldItemName(inSlot.Itemstack);
	}

	public string GetContainedInfo(ItemSlot inSlot)
	{
		IWorldAccessor world = api.World;
		CookingRecipe mealRecipe = GetMealRecipe(world, inSlot.Itemstack);
		float num = inSlot.Itemstack?.Attributes.GetFloat("quantityServings") ?? 0f;
		ItemStack[] nonEmptyContents = GetNonEmptyContents(world, inSlot.Itemstack);
		if (nonEmptyContents.Length == 0)
		{
			return Lang.GetWithFallback("contained-empty-container", "{0} (Empty)", inSlot.GetStackName());
		}
		Block block = inSlot.Itemstack?.Block;
		if (block == null)
		{
			return Lang.Get("unknown");
		}
		string text = block.Attributes?["emptiedBlockCode"].AsString();
		string name = new ItemStack((text == null) ? block : api.World.GetBlock(text)).GetName();
		bool flag = MealMeshCache.ContentsRotten(nonEmptyContents);
		if (num > 0f || flag)
		{
			string key = "contained-food-servings";
			string text2 = mealRecipe?.GetOutputName(world, nonEmptyContents) ?? nonEmptyContents[0].GetName();
			if (mealRecipe?.CooksInto != null)
			{
				key = "contained-nonfood-portions";
				int num2 = text2.IndexOf('\n');
				if (num2 > 0)
				{
					text2 = text2.Substring(0, num2);
				}
			}
			else if (flag)
			{
				key = "contained-food-singleservingmax";
				text2 = Lang.Get("Rotten Food");
				num = 1f;
			}
			return Lang.Get(key, Math.Round(num, 1), text2, name, PerishableInfoCompactContainer(api, inSlot));
		}
		return Lang.Get("contained-foodstacks-insideof", Lang.Get("meal-ingredientlist-" + nonEmptyContents.Length, ((IEnumerable<object>)nonEmptyContents.Select((ItemStack stack) => Lang.Get("{0}x {1}", stack.StackSize, stack.GetName()))).ToArray()), name) + PerishableInfoCompactContainer(api, inSlot);
	}

	public virtual bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Empty)
		{
			return false;
		}
		JsonObject attributes = activeHotbarSlot.Itemstack.Collectible.Attributes;
		if (((attributes != null && attributes.IsTrue("mealContainer")) || activeHotbarSlot.Itemstack.Block is IBlockMealContainer) && GetServings(api.World, slot.Itemstack) > 0f)
		{
			bool flag = false;
			if (activeHotbarSlot.StackSize > 1)
			{
				activeHotbarSlot = new DummySlot(activeHotbarSlot.TakeOut(1));
				byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
				flag = ServeIntoStack(activeHotbarSlot, slot, api.World);
				if (!byPlayer.InventoryManager.TryGiveItemstack(activeHotbarSlot.Itemstack, slotNotifyEffect: true))
				{
					api.World.SpawnItemEntity(activeHotbarSlot.Itemstack, byPlayer.Entity.ServerPos.XYZ);
				}
			}
			else
			{
				flag = ServeIntoStack(activeHotbarSlot, slot, api.World);
			}
			slot.MarkDirty();
			be.MarkDirty(redrawOnClient: true);
			if (!(be is BlockEntityGroundStorage))
			{
				return flag;
			}
			return true;
		}
		return false;
	}

	public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		return false;
	}

	public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
	}

	public virtual string HandbookPageCodeForStack(IWorldAccessor world, ItemStack stack)
	{
		string recipeCode = GetRecipeCode(world, stack);
		if (recipeCode != null)
		{
			return "handbook-mealrecipe-" + recipeCode;
		}
		return GuiHandbookItemStackPage.PageCodeForStack(stack);
	}
}
