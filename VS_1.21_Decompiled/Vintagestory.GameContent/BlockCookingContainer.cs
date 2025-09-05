using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCookingContainer : Block, IInFirepitRendererSupplier, IAttachableToEntity, IContainedCustomName
{
	public int MaxServingSize = 6;

	private Cuboidi? attachmentArea;

	private IAttachableToEntity? attrAtta;

	public int RequiresBehindSlots { get; set; }

	public bool IsAttachable(Entity toEntity, ItemStack itemStack)
	{
		return attrAtta != null;
	}

	string? IAttachableToEntity.GetCategoryCode(ItemStack stack)
	{
		return attrAtta?.GetCategoryCode(stack);
	}

	CompositeShape? IAttachableToEntity.GetAttachedShape(ItemStack stack, string slotCode)
	{
		return attrAtta?.GetAttachedShape(stack, slotCode);
	}

	string[]? IAttachableToEntity.GetDisableElements(ItemStack stack)
	{
		return attrAtta?.GetDisableElements(stack);
	}

	string[]? IAttachableToEntity.GetKeepElements(ItemStack stack)
	{
		return attrAtta?.GetKeepElements(stack);
	}

	string? IAttachableToEntity.GetTexturePrefixCode(ItemStack stack)
	{
		return attrAtta?.GetTexturePrefixCode(stack);
	}

	void IAttachableToEntity.CollectTextures(ItemStack itemstack, Shape intoShape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
	{
		string text = itemstack.Block.Variant["color"];
		string text2 = itemstack.Block.Variant["type"];
		Block block = api.World.GetBlock(CodeWithVariants(new string[2] { "color", "type" }, new string[2] { text, text2 }));
		char c = intoShape.Elements[0].StepParentName.Last();
		intoShape.Textures["ceramic" + c] = block.Textures["ceramic"].Base;
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		attachmentArea = Attributes?["attachmentArea"].AsObject<Cuboidi>();
		MaxServingSize = Attributes?["maxServingSize"].AsInt(6) ?? 6;
		attrAtta = IAttachableToEntity.FromAttributes(this);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!activeHotbarSlot.Empty)
		{
			JsonObject attributes = activeHotbarSlot.Itemstack.Collectible.Attributes;
			if (attributes != null && attributes.IsTrue("handleCookingContainerInteract"))
			{
				EnumHandHandling handling = EnumHandHandling.NotHandled;
				activeHotbarSlot.Itemstack.Collectible.OnHeldInteractStart(activeHotbarSlot, byPlayer.Entity, blockSel, null, firstEvent: true, ref handling);
				if (handling == EnumHandHandling.PreventDefault || handling == EnumHandHandling.PreventDefaultAction)
				{
					return true;
				}
			}
		}
		ItemStack itemstack = OnPickBlock(world, blockSel.Position);
		if (byPlayer.InventoryManager.TryGiveItemstack(itemstack, slotNotifyEffect: true))
		{
			world.BlockAccessor.SetBlock(0, blockSel.Position);
			world.PlaySoundAt(Sounds.Place, byPlayer, byPlayer);
			return true;
		}
		return false;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			failureCode = "onlywhensneaking";
			return false;
		}
		if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode) && world.BlockAccessor.GetBlock(blockSel.Position.DownCopy()).CanAttachBlockAt(world.BlockAccessor, this, blockSel.Position.DownCopy(), BlockFacing.UP, attachmentArea))
		{
			DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			return true;
		}
		return false;
	}

	public override float GetMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
	{
		float num = 0f;
		ItemStack[] cookingStacks = GetCookingStacks(cookingSlotsProvider, clone: false);
		foreach (ItemStack itemStack in cookingStacks)
		{
			int num2 = itemStack.StackSize;
			if (itemStack.Collectible?.CombustibleProps == null)
			{
				CollectibleObject collectible = itemStack.Collectible;
				if (collectible != null && collectible.Attributes?["waterTightContainerProps"].Exists == true)
				{
					WaterTightContainableProps? containableProps = BlockLiquidContainerBase.GetContainableProps(itemStack);
					num2 = (int)(((float)itemStack.StackSize / containableProps?.ItemsPerLitre) ?? 1f);
				}
				num += (float)(20 * num2);
			}
			else
			{
				float meltingDuration = itemStack.Collectible.GetMeltingDuration(world, cookingSlotsProvider, inputSlot);
				num += meltingDuration * (float)num2 / (float)itemStack.Collectible.CombustibleProps.SmeltedRatio;
			}
		}
		return Math.Max(40f, num / 3f);
	}

	public override float GetMeltingPoint(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
	{
		float num = 0f;
		ItemStack[] cookingStacks = GetCookingStacks(cookingSlotsProvider, clone: false);
		for (int i = 0; i < cookingStacks.Length; i++)
		{
			num = Math.Max(num, cookingStacks[i].Collectible.GetMeltingPoint(world, cookingSlotsProvider, inputSlot));
		}
		return Math.Max(100f, num);
	}

	public override bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
	{
		GetMatchingCookingRecipe(world, GetCookingStacks(cookingSlotsProvider, clone: false), out var quantityServings);
		if (quantityServings > 0)
		{
			return quantityServings <= MaxServingSize;
		}
		return false;
	}

	public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
	{
		ItemStack[] array = GetCookingStacks(cookingSlotsProvider);
		int quantityServings;
		CookingRecipe matchingCookingRecipe = GetMatchingCookingRecipe(world, array, out quantityServings);
		Block block = world.GetBlock(CodeWithVariant("type", "cooked"));
		if (matchingCookingRecipe == null || quantityServings < 1 || quantityServings > MaxServingSize)
		{
			return;
		}
		if (matchingCookingRecipe.CooksInto != null)
		{
			ItemStack itemStack = matchingCookingRecipe.CooksInto.ResolvedItemstack?.Clone();
			if (itemStack != null)
			{
				itemStack.StackSize *= quantityServings;
				array = new ItemStack[1] { itemStack };
				if (!matchingCookingRecipe.IsFood)
				{
					block = world.GetBlock(new AssetLocation(Attributes["dirtiedBlockCode"].AsString()));
				}
			}
		}
		else
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i].StackSize = array[i].StackSize / quantityServings;
				ItemStack itemStack2 = matchingCookingRecipe.GetIngrendientFor(array[i])?.GetMatchingStack(array[i])?.CookedStack?.ResolvedItemstack.Clone();
				if (itemStack2 != null)
				{
					array[i] = itemStack2;
				}
			}
		}
		ItemStack itemStack3 = new ItemStack(block);
		itemStack3.Collectible.SetTemperature(world, itemStack3, GetIngredientsTemperature(world, array));
		TransitionableProperties transitionableProperties = matchingCookingRecipe.PerishableProps?.Clone();
		transitionableProperties?.TransitionedStack.Resolve(world, "cooking container perished stack");
		if (transitionableProperties != null)
		{
			CollectibleObject.CarryOverFreshness(api, cookingSlotsProvider.Slots, array, transitionableProperties);
		}
		if (matchingCookingRecipe.CooksInto != null)
		{
			for (int j = 0; j < cookingSlotsProvider.Slots.Length; j++)
			{
				cookingSlotsProvider.Slots[j].Itemstack = ((j == 0) ? array[0] : null);
			}
			inputSlot.Itemstack = itemStack3;
			return;
		}
		for (int k = 0; k < cookingSlotsProvider.Slots.Length; k++)
		{
			cookingSlotsProvider.Slots[k].Itemstack = null;
		}
		((BlockCookedContainer)block).SetContents(matchingCookingRecipe.Code, quantityServings, itemStack3, array);
		outputSlot.Itemstack = itemStack3;
		inputSlot.Itemstack = null;
	}

	internal float PutMeal(BlockPos pos, ItemStack[] itemStack, string recipeCode, float quantityServings)
	{
		Block block = api.World.GetBlock(CodeWithVariant("type", "cooked"));
		api.World.BlockAccessor.SetBlock(block.Id, pos);
		float result = Math.Min(quantityServings, Attributes["servingCapacity"].AsInt(1));
		if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityCookedContainer blockEntityCookedContainer)
		{
			blockEntityCookedContainer.RecipeCode = recipeCode;
			blockEntityCookedContainer.QuantityServings = quantityServings;
			for (int i = 0; i < itemStack.Length; i++)
			{
				blockEntityCookedContainer.inventory[i].Itemstack = itemStack[i];
			}
			blockEntityCookedContainer.MarkDirty(redrawOnClient: true);
		}
		return result;
	}

	public string? GetOutputText(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
	{
		if (inputSlot.Itemstack == null)
		{
			return null;
		}
		if (!(inputSlot.Itemstack.Collectible is BlockCookingContainer))
		{
			return null;
		}
		ItemStack[] cookingStacks = GetCookingStacks(cookingSlotsProvider);
		int quantityServings;
		CookingRecipe matchingCookingRecipe = GetMatchingCookingRecipe(world, cookingStacks, out quantityServings);
		if (matchingCookingRecipe != null)
		{
			string text = matchingCookingRecipe.GetOutputName(world, cookingStacks);
			string key;
			if (matchingCookingRecipe.CooksInto != null)
			{
				ItemStack resolvedItemstack = matchingCookingRecipe.CooksInto.ResolvedItemstack;
				key = "mealcreation-nonfood";
				text = resolvedItemstack?.GetName();
				if (quantityServings == -1)
				{
					return Lang.Get("mealcreation-recipeerror", text?.ToLower() ?? Lang.Get("unknown"));
				}
				quantityServings *= matchingCookingRecipe.CooksInto.Quantity;
				if (resolvedItemstack != null && resolvedItemstack.Collectible.Attributes?["waterTightContainerProps"].Exists == true)
				{
					float num = ((float)quantityServings / BlockLiquidContainerBase.GetContainableProps(resolvedItemstack)?.ItemsPerLitre) ?? 1f;
					string text2 = ((!((double)num < 0.1)) ? Lang.Get("{0:0.##} L", num) : Lang.Get("{0} mL", (int)(num * 1000f)));
					return Lang.Get("mealcreation-nonfood-liquid", text2, text?.ToLower() ?? Lang.Get("unknown"));
				}
			}
			else
			{
				key = ((quantityServings == 1) ? "mealcreation-makesingular" : "mealcreation-makeplural");
			}
			if (quantityServings == -1)
			{
				return Lang.Get("mealcreation-recipeerror", text?.ToLower() ?? Lang.Get("unknown"));
			}
			if (quantityServings > MaxServingSize)
			{
				return Lang.Get("mealcreation-toomuch", inputSlot.GetStackName(), quantityServings, text?.ToLower() ?? Lang.Get("unknown"));
			}
			return Lang.Get(key, quantityServings, text?.ToLower() ?? Lang.Get("unknown"));
		}
		if (!cookingStacks.All((ItemStack stack) => stack == null))
		{
			return Lang.Get("mealcreation-norecipe");
		}
		return null;
	}

	public CookingRecipe? GetMatchingCookingRecipe(IWorldAccessor world, ItemStack[] stacks, out int quantityServings)
	{
		quantityServings = 0;
		List<CookingRecipe> cookingRecipes = world.Api.GetCookingRecipes();
		if (cookingRecipes == null)
		{
			return null;
		}
		bool flag = Attributes["isDirtyPot"].AsBool();
		foreach (CookingRecipe item in cookingRecipes)
		{
			if (!flag || (item.CooksInto != null && !item.IsFood))
			{
				quantityServings = 0;
				if (item.Matches(stacks, ref quantityServings) || quantityServings == -1)
				{
					return item;
				}
			}
		}
		return null;
	}

	public static float GetIngredientsTemperature(IWorldAccessor world, ItemStack[] ingredients)
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

	public ItemStack[] GetCookingStacks(ISlotProvider cookingSlotsProvider, bool clone = true)
	{
		List<ItemStack> list = new List<ItemStack>(4);
		for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
		{
			ItemStack itemstack = cookingSlotsProvider.Slots[i].Itemstack;
			if (itemstack != null)
			{
				list.Add(clone ? itemstack.Clone() : itemstack);
			}
		}
		return list.ToArray();
	}

	public IInFirepitRenderer GetRendererWhenInFirepit(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
	{
		return new PotInFirepitRenderer(api as ICoreClientAPI, stack, firepit.Pos, forOutputSlot);
	}

	public EnumFirepitModel GetDesiredFirepitModel(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
	{
		return EnumFirepitModel.Wide;
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		return capi.BlockTextureAtlas.GetRandomColor(Textures["ceramic"].Baked.TextureSubId, rndIndex);
	}

	public override int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
	{
		return capi.BlockTextureAtlas.GetRandomColor(Textures["ceramic"].Baked.TextureSubId);
	}

	public string? GetContainedName(ItemSlot inSlot, int quantity)
	{
		return null;
	}

	public string GetContainedInfo(ItemSlot inSlot)
	{
		return Lang.GetWithFallback("contained-empty-container", "{0} (Empty)", inSlot.GetStackName());
	}
}
