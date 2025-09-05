using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockCrock : BlockCookedContainerBase, IBlockMealContainer, IContainedMeshSource
{
	private string shapeLocation = "game:shapes/block/clay/crock/";

	private string[] labelNames = new string[13]
	{
		"carrot", "cabbage", "onion", "parsnip", "turnip", "pumpkin", "soybean", "bellpepper", "cassava", "mushroom",
		"redmeat", "poultry", "porridge"
	};

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		string[] array = Attributes?["labelNames"].AsArray<string>();
		if (array != null)
		{
			labelNames = array;
		}
		if (Vintagestory.API.Common.Shape.TryGet(api, Shape.Base?.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json")) != null)
		{
			shapeLocation = Shape.Base.Clone().WithFilename("").WithPathPrefixOnce("shapes/")
				.ToString();
		}
		else if (Vintagestory.API.Common.Shape.TryGet(api, AssetLocation.Create("shapes/block/clay/crock/base.json", Code.Domain)) != null)
		{
			shapeLocation = Code.Domain + ":shapes/block/clay/crock/";
		}
	}

	public override float GetContainingTransitionModifierContained(IWorldAccessor world, ItemSlot inSlot, EnumTransitionType transType)
	{
		float num = 1f;
		if (transType == EnumTransitionType.Perish)
		{
			ItemStack itemstack = inSlot.Itemstack;
			num = ((itemstack == null || itemstack.Attributes?.GetBool("sealed") != true) ? (num * 0.85f) : ((inSlot.Itemstack.Attributes.GetString("recipeCode") == null) ? (num * 0.25f) : (num * 0.1f)));
		}
		return num;
	}

	public override float GetContainingTransitionModifierPlaced(IWorldAccessor world, BlockPos pos, EnumTransitionType transType)
	{
		float num = 1f;
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCrock blockEntityCrock))
		{
			return num;
		}
		if (transType == EnumTransitionType.Perish)
		{
			num = ((!blockEntityCrock.Sealed) ? (num * 0.85f) : ((blockEntityCrock.RecipeCode == null) ? (num * 0.25f) : (num * 0.1f)));
		}
		return num;
	}

	public AssetLocation LabelForContents(string? recipeCode, ItemStack[]? contents)
	{
		if (contents == null || contents.Length == 0 || contents[0] == null)
		{
			return AssetLocation.Create(shapeLocation + "label-empty.json");
		}
		if (MealMeshCache.ContentsRotten(contents))
		{
			return AssetLocation.Create(shapeLocation + "label-rot.json");
		}
		if (recipeCode != null && recipeCode.Length > 0)
		{
			return AssetLocation.Create(shapeLocation + "label-" + ((labelNames.Contains<string>(recipeCode) ? recipeCode : null) ?? CodeToLabel(getMostCommonMealIngredient(contents)) ?? "meal") + ".json");
		}
		return AssetLocation.Create(shapeLocation + "label-" + (CodeToLabel(contents[0].Collectible.Code) ?? "empty") + ".json");
	}

	public string? CodeToLabel(AssetLocation? loc)
	{
		if (loc == null)
		{
			return null;
		}
		string result = null;
		string[] array = labelNames;
		foreach (string text in array)
		{
			if (loc.Path.Contains(text))
			{
				result = text;
				break;
			}
		}
		return result;
	}

	private AssetLocation? getMostCommonMealIngredient(ItemStack[] contents)
	{
		Dictionary<AssetLocation, int> dictionary = new Dictionary<AssetLocation, int>();
		foreach (ItemStack itemStack in contents)
		{
			dictionary.TryGetValue(itemStack.Collectible.Code, out var value);
			dictionary[itemStack.Collectible.Code] = 1 + value;
		}
		AssetLocation key = dictionary.Aggregate((KeyValuePair<AssetLocation, int> l, KeyValuePair<AssetLocation, int> r) => (l.Value <= r.Value) ? r : l).Key;
		if (dictionary[key] < 3)
		{
			return null;
		}
		return key;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = new ItemStack(world.GetBlock(CodeWithVariant("side", "east")));
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCrock blockEntityCrock)
		{
			ItemStack[] contentStacks = blockEntityCrock.GetContentStacks();
			for (int i = 0; i < contentStacks.Length; i++)
			{
				if (contentStacks[i] != null)
				{
					SetContents(itemStack, contentStacks);
					if (blockEntityCrock.RecipeCode != null)
					{
						itemStack.Attributes.SetString("recipeCode", blockEntityCrock.RecipeCode);
						itemStack.Attributes.SetFloat("quantityServings", blockEntityCrock.QuantityServings);
						itemStack.Attributes.SetBool("sealed", blockEntityCrock.Sealed);
					}
				}
			}
		}
		return itemStack;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		bool flag = itemstack.Attributes.GetBool("sealed");
		ItemStack[] nonEmptyContents = GetNonEmptyContents(capi.World, itemstack);
		string recipeCode = itemstack.Attributes.GetString("recipeCode");
		AssetLocation assetLocation = LabelForContents(recipeCode, nonEmptyContents);
		if (assetLocation == null)
		{
			return;
		}
		Dictionary<string, MultiTextureMeshRef> orCreate = ObjectCacheUtil.GetOrCreate(capi, "blockcrockGuiMeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());
		string key = Code.ToShortString() + assetLocation.ToShortString() + (flag ? "sealed" : "");
		if (!orCreate.TryGetValue(key, out var value))
		{
			MeshData meshData = GenMesh(capi, assetLocation, new Vec3f(0f, 270f, 0f));
			if (flag)
			{
				MeshData meshData2 = GenSealMesh(capi);
				if (meshData2 != null)
				{
					meshData.AddMeshData(meshData2);
				}
			}
			value = (orCreate[key] = capi.Render.UploadMultiTextureMesh(meshData));
		}
		renderinfo.ModelRef = value;
	}

	public virtual string GetMeshCacheKey(ItemStack itemstack)
	{
		bool flag = itemstack.Attributes.GetBool("sealed");
		ItemStack[] nonEmptyContents = GetNonEmptyContents(api.World, itemstack);
		string recipeCode = itemstack.Attributes.GetString("recipeCode");
		AssetLocation assetLocation = LabelForContents(recipeCode, nonEmptyContents);
		return Code.ToShortString() + assetLocation.ToShortString() + (flag ? "sealed" : "");
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos? forBlockPos = null)
	{
		ICoreClientAPI capi = (ICoreClientAPI)api;
		ItemStack[] nonEmptyContents = GetNonEmptyContents(api.World, itemstack);
		string recipeCode = itemstack.Attributes.GetString("recipeCode");
		MeshData meshData = GenMesh(capi, LabelForContents(recipeCode, nonEmptyContents));
		if (itemstack.Attributes.GetBool("sealed"))
		{
			MeshData meshData2 = GenSealMesh(capi);
			if (meshData2 != null)
			{
				meshData.AddMeshData(meshData2);
			}
		}
		return meshData;
	}

	public MeshData GenMesh(ICoreClientAPI capi, AssetLocation labelLoc, Vec3f? rot = null)
	{
		ITesselatorAPI tesselator = capi.Tesselator;
		Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, AssetLocation.Create(shapeLocation + "base.json"));
		Shape shape2 = Vintagestory.API.Common.Shape.TryGet(capi, labelLoc);
		tesselator.TesselateShape(this, shape, out var modeldata, rot);
		if (shape2 != null)
		{
			tesselator.TesselateShape(this, shape2, out var modeldata2, rot);
			modeldata.AddMeshData(modeldata2);
		}
		return modeldata;
	}

	public MeshData? GenSealMesh(ICoreClientAPI capi)
	{
		ITesselatorAPI tesselator = capi.Tesselator;
		Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, AssetLocation.Create(shapeLocation + "seal.json"));
		if (shape == null)
		{
			return null;
		}
		tesselator.TesselateShape(this, shape, out var modeldata);
		return modeldata;
	}

	public override bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
	{
		if (!(gridRecipe.Output.ResolvedItemstack?.Collectible is BlockCrock))
		{
			return base.MatchesForCrafting(inputStack, gridRecipe, ingredient);
		}
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < gridRecipe.resolvedIngredients.Length; i++)
		{
			ItemStack resolvedItemstack = gridRecipe.resolvedIngredients[i].ResolvedItemstack;
			if (resolvedItemstack?.Collectible is BlockCrock)
			{
				flag2 = resolvedItemstack.Attributes.GetBool("sealed");
			}
			else if (resolvedItemstack != null && resolvedItemstack.ItemAttributes["canSealCrock"].AsBool())
			{
				flag = true;
			}
		}
		if (flag)
		{
			return !flag2;
		}
		return false;
	}

	public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
	{
		if (outputSlot.Itemstack == null)
		{
			return;
		}
		foreach (ItemSlot itemSlot in allInputslots)
		{
			if (itemSlot.Itemstack?.Collectible is BlockCrock)
			{
				outputSlot.Itemstack.Attributes = itemSlot.Itemstack.Attributes.Clone();
				outputSlot.Itemstack.Attributes.SetBool("sealed", value: true);
			}
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel?.Position == null)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		ItemStack itemstack = slot.Itemstack;
		if (itemstack == null)
		{
			return;
		}
		Block block = api.World.BlockAccessor.GetBlock(blockSel.Position);
		float num = (float)itemstack.Attributes.GetDecimal("quantityServings");
		if (block != null && block.Attributes?.IsTrue("mealContainer") == true)
		{
			if (byEntity.Controls.ShiftKey)
			{
				if (num > 0f)
				{
					ServeIntoBowl(block, blockSel.Position, slot, byEntity.World);
				}
				handHandling = EnumHandHandling.PreventDefault;
			}
			return;
		}
		if (block is BlockGroundStorage)
		{
			if (!byEntity.Controls.ShiftKey || !(api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage blockEntityGroundStorage))
			{
				return;
			}
			ItemSlot itemSlot = blockEntityGroundStorage?.GetSlotAt(blockSel);
			if (itemSlot == null || itemSlot.Empty)
			{
				return;
			}
			JsonObject itemAttributes = itemSlot.Itemstack.ItemAttributes;
			if (itemAttributes != null && itemAttributes.IsTrue("mealContainer"))
			{
				if (num > 0f)
				{
					ServeIntoStack(itemSlot, slot, byEntity.World);
					itemSlot.MarkDirty();
					blockEntityGroundStorage.updateMeshes();
					blockEntityGroundStorage.MarkDirty(redrawOnClient: true);
				}
				handHandling = EnumHandHandling.PreventDefault;
				return;
			}
		}
		if (block is BlockCookingContainer blockCookingContainer && itemstack.Attributes.HasAttribute("recipeCode"))
		{
			handHandling = EnumHandHandling.PreventDefault;
			((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			string recipeCode = itemstack.Attributes.GetString("recipeCode");
			float num2 = blockCookingContainer.PutMeal(blockSel.Position, GetNonEmptyContents(api.World, itemstack), recipeCode, num);
			num -= num2;
			if (num > 0f)
			{
				itemstack.Attributes.SetFloat("quantityServings", num);
				return;
			}
			itemstack.Attributes.RemoveAttribute("recipeCode");
			itemstack.Attributes.RemoveAttribute("quantityServings");
			itemstack.Attributes.RemoveAttribute("contents");
		}
		else if (block is BlockBarrel)
		{
			if (api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBarrel blockEntityBarrel)
			{
				ItemStack[] nonEmptyContents = GetNonEmptyContents(api.World, itemstack);
				if (nonEmptyContents == null || nonEmptyContents.Length == 0)
				{
					ItemSlot itemSlot2 = blockEntityBarrel.Inventory.FirstOrDefault(delegate(ItemSlot itemSlot3)
					{
						ItemStack itemstack2 = itemSlot3.Itemstack;
						return itemstack2 != null && itemstack2.Collectible.Attributes?.IsTrue("crockable") == true;
					});
					if (itemSlot2 != null)
					{
						float num3 = itemstack.Block.Attributes["servingCapacity"].AsFloat(1f);
						float num4 = (BlockLiquidContainerBase.GetContainableProps(itemSlot2.Itemstack)?.ItemsPerLitre ?? 1f) * 4f;
						ItemStack itemStack = itemSlot2.TakeOut((int)(num3 * num4));
						if (itemStack != null)
						{
							float quantityServings = (float)itemStack.StackSize / num4;
							itemStack.StackSize = (int)num4;
							SetContents(null, itemstack, new ItemStack[1] { itemStack }, quantityServings);
							blockEntityBarrel.MarkDirty(redrawOnClient: true);
							slot.MarkDirty();
						}
					}
				}
				else if (nonEmptyContents.Length == 1 && itemstack.Attributes.GetString("recipeCode") == null)
				{
					WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(nonEmptyContents[0]);
					ItemSlot sinkSlot = blockEntityBarrel.Inventory[(containableProps != null) ? 1 : 0];
					float num5 = (containableProps?.ItemsPerLitre ?? 1f) * 4f;
					ItemStack itemStack2 = nonEmptyContents[0].Clone();
					itemStack2.StackSize = (int)(num * num5);
					int num6 = new DummySlot(itemStack2).TryPutInto(api.World, sinkSlot, itemStack2.StackSize);
					num = (num * num5 - (float)num6) / num5;
					if (num <= 0f)
					{
						itemstack.Attributes.RemoveAttribute("recipeCode");
						itemstack.Attributes.RemoveAttribute("quantityServings");
						itemstack.Attributes.RemoveAttribute("contents");
					}
					else
					{
						itemstack.Attributes.SetFloat("quantityServings", num);
					}
					itemstack.Attributes.RemoveAttribute("sealed");
					blockEntityBarrel.MarkDirty(redrawOnClient: true);
					slot.MarkDirty();
				}
			}
			handHandling = EnumHandHandling.PreventDefault;
			((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		}
		else
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!activeHotbarSlot.Empty)
		{
			JsonObject attributes = activeHotbarSlot.Itemstack.Collectible.Attributes;
			if (attributes != null && attributes.IsTrue("mealContainer") && (!(activeHotbarSlot.Itemstack.Collectible is BlockCrock) || activeHotbarSlot.StackSize == 1))
			{
				if (!(world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityCrock blockEntityCrock))
				{
					return false;
				}
				if (activeHotbarSlot.Itemstack.Attributes.GetDecimal("quantityServings") == 0.0)
				{
					blockEntityCrock.ServeInto(byPlayer, activeHotbarSlot);
					return true;
				}
				if (blockEntityCrock.QuantityServings == 0f)
				{
					ServeIntoBowl(this, blockSel.Position, activeHotbarSlot, world);
					blockEntityCrock.Sealed = false;
					blockEntityCrock.MarkDirty(redrawOnClient: true);
					return true;
				}
			}
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		ItemStack crockStack = inSlot.Itemstack;
		if (crockStack == null)
		{
			return;
		}
		CookingRecipe cookingRecipe = GetCookingRecipe(world, crockStack);
		ItemStack[] nonEmptyContents = GetNonEmptyContents(world, crockStack);
		if (nonEmptyContents == null || nonEmptyContents.Length == 0)
		{
			dsc.AppendLine(Lang.Get("Empty"));
			if (crockStack.Attributes.GetBool("sealed"))
			{
				dsc.AppendLine("<font color=\"lightgreen\">" + Lang.Get("Sealed.") + "</font>");
			}
			return;
		}
		DummyInventory dummyInventory = new DummyInventory(api);
		ItemSlot dummySlotForFirstPerishableStack = GetDummySlotForFirstPerishableStack(api.World, nonEmptyContents, null, dummyInventory);
		dummyInventory.OnAcquireTransitionSpeed += delegate(EnumTransitionType transType, ItemStack stack, float mul)
		{
			float num3 = mul * GetContainingTransitionModifierContained(world, inSlot, transType);
			if (inSlot.Inventory != null)
			{
				num3 *= inSlot.Inventory.GetTransitionSpeedMul(transType, crockStack);
			}
			return num3;
		};
		if (cookingRecipe != null)
		{
			double num = crockStack.Attributes.GetDecimal("quantityServings");
			if (cookingRecipe != null)
			{
				if (num == 1.0)
				{
					dsc.AppendLine(Lang.Get("{0} serving of {1}", Math.Round(num, 1), cookingRecipe.GetOutputName(world, nonEmptyContents)));
				}
				else
				{
					dsc.AppendLine(Lang.Get("{0} servings of {1}", Math.Round(num, 1), cookingRecipe.GetOutputName(world, nonEmptyContents)));
				}
			}
			BlockMeal[]? array = BlockMeal.AllMealBowls(api);
			string text = ((array == null) ? null : array[0]?.GetContentNutritionFacts(world, inSlot, null));
			if (text != null)
			{
				dsc.Append(text);
			}
		}
		else if (crockStack.Attributes.HasAttribute("quantityServings"))
		{
			double num2 = crockStack.Attributes.GetDecimal("quantityServings");
			if (Math.Round(num2, 1) < 0.05)
			{
				dsc.AppendLine(Lang.Get("meal-servingsleft-percent", Math.Round(num2 * 100.0, 0)));
			}
			else
			{
				dsc.AppendLine(Lang.Get("{0} servings left", Math.Round(num2, 1)));
			}
		}
		else if (!MealMeshCache.ContentsRotten(nonEmptyContents))
		{
			dsc.AppendLine(Lang.Get("Contents: {0}", Lang.Get("meal-ingredientlist-" + nonEmptyContents.Length, nonEmptyContents.Select((ItemStack stack) => Lang.Get("{0}x {1}", stack.StackSize, stack.GetName())))));
		}
		dummySlotForFirstPerishableStack.Itemstack?.Collectible.AppendPerishableInfoText(dummySlotForFirstPerishableStack, dsc, world);
		if (crockStack.Attributes.GetBool("sealed"))
		{
			dsc.AppendLine("<font color=\"lightgreen\">" + Lang.Get("Sealed.") + "</font>");
		}
	}

	public override string GetHeldItemName(ItemStack? itemStack)
	{
		if (IsEmpty(itemStack))
		{
			return base.GetHeldItemName(itemStack);
		}
		ItemStack[] contents = GetContents(api.World, itemStack);
		string text = itemStack?.Collectible.GetCollectibleInterface<IBlockMealContainer>()?.GetRecipeCode(api.World, itemStack);
		string text2 = Lang.Get("mealrecipe-name-" + text + "-in-container");
		if (text == null)
		{
			text2 = ((!MealMeshCache.ContentsRotten(contents)) ? contents[0].GetName() : Lang.Get("Rotten Food"));
		}
		AssetLocation assetLocation = CodeWithVariant("type", "meal");
		return Lang.GetMatching(assetLocation.Domain + ":block-" + assetLocation.Path, text2);
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCrock blockEntityCrock))
		{
			return "";
		}
		CookingRecipe cookingRecipe = api.GetCookingRecipe(blockEntityCrock.RecipeCode);
		ItemStack[] array = (from slot in blockEntityCrock.inventory
			where !slot.Empty
			select slot.Itemstack).ToArray();
		if (array.Length == 0)
		{
			return Lang.Get("Empty");
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (cookingRecipe != null)
		{
			ItemSlot dummySlotForFirstPerishableStack = GetDummySlotForFirstPerishableStack(api.World, array, forPlayer.Entity, blockEntityCrock.inventory);
			if (cookingRecipe != null)
			{
				if (blockEntityCrock.QuantityServings == 1f)
				{
					stringBuilder.AppendLine(Lang.Get("{0} serving of {1}", Math.Round(blockEntityCrock.QuantityServings, 1), cookingRecipe.GetOutputName(world, array)));
				}
				else
				{
					stringBuilder.AppendLine(Lang.Get("{0} servings of {1}", Math.Round(blockEntityCrock.QuantityServings, 1), cookingRecipe.GetOutputName(world, array)));
				}
			}
			BlockMeal[]? array2 = BlockMeal.AllMealBowls(api);
			string text = ((array2 == null) ? null : array2[0]?.GetContentNutritionFacts(world, new DummySlot(OnPickBlock(world, pos)), null));
			if (text != null)
			{
				stringBuilder.Append(text);
			}
			dummySlotForFirstPerishableStack.Itemstack?.Collectible.AppendPerishableInfoText(dummySlotForFirstPerishableStack, stringBuilder, world);
		}
		else
		{
			stringBuilder.AppendLine("Contents:");
			ItemStack[] array3 = array;
			foreach (ItemStack itemStack in array3)
			{
				if (itemStack != null)
				{
					stringBuilder.AppendLine(itemStack.StackSize + "x  " + itemStack.GetName());
				}
			}
			blockEntityCrock.inventory[0].Itemstack?.Collectible.AppendPerishableInfoText(blockEntityCrock.inventory[0], stringBuilder, api.World);
		}
		if (blockEntityCrock.Sealed)
		{
			stringBuilder.AppendLine("<font color=\"lightgreen\">" + Lang.Get("Sealed.") + "</font>");
		}
		return stringBuilder.ToString();
	}

	public static ItemSlot GetDummySlotForFirstPerishableStack(IWorldAccessor world, ItemStack[]? stacks, Entity? forEntity, InventoryBase slotInventory)
	{
		ItemStack stack = null;
		if (stacks != null)
		{
			for (int i = 0; i < stacks.Length; i++)
			{
				if (stacks[i] != null)
				{
					TransitionableProperties[] transitionableProperties = stacks[i].Collectible.GetTransitionableProperties(world, stacks[i], forEntity);
					if (transitionableProperties != null && transitionableProperties.Length != 0)
					{
						stack = stacks[i];
						break;
					}
				}
			}
		}
		DummySlot dummySlot = new DummySlot(stack, slotInventory);
		dummySlot.MarkedDirty += () => true;
		return dummySlot;
	}

	public override TransitionState[]? UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
	{
		ItemStack itemstack = inslot.Itemstack;
		if (itemstack == null)
		{
			return null;
		}
		ItemStack[] nonEmptyContents = GetNonEmptyContents(world, itemstack);
		ItemStack[] array = nonEmptyContents;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].StackSize *= (int)Math.Max(1f, itemstack.Attributes.TryGetFloat("quantityServings") ?? 1f);
		}
		SetContents(itemstack, nonEmptyContents);
		TransitionState[] result = base.UpdateAndGetTransitionStates(world, inslot);
		nonEmptyContents = GetNonEmptyContents(world, itemstack);
		if (nonEmptyContents.Length == 0 || MealMeshCache.ContentsRotten(nonEmptyContents))
		{
			for (int j = 0; j < nonEmptyContents.Length; j++)
			{
				TransitionableProperties transitionableProperties = nonEmptyContents[j].Collectible.GetTransitionableProperties(world, nonEmptyContents[j], null)?.FirstOrDefault((TransitionableProperties props) => props.Type == EnumTransitionType.Perish);
				if (transitionableProperties != null)
				{
					nonEmptyContents[j] = nonEmptyContents[j].Collectible.OnTransitionNow(GetContentInDummySlot(inslot, nonEmptyContents[j]), transitionableProperties);
				}
			}
			SetContents(itemstack, nonEmptyContents);
			itemstack.Attributes.RemoveAttribute("recipeCode");
			itemstack.Attributes.RemoveAttribute("quantityServings");
		}
		array = nonEmptyContents;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].StackSize /= (int)Math.Max(1f, itemstack.Attributes.TryGetFloat("quantityServings") ?? 1f);
		}
		SetContents(itemstack, nonEmptyContents);
		return result;
	}

	public override void OnGroundIdle(EntityItem entityItem)
	{
		base.OnGroundIdle(entityItem);
		IWorldAccessor world = entityItem.World;
		if (world.Side != EnumAppSide.Server || !entityItem.Swimming || !(world.Rand.NextDouble() < 0.01))
		{
			return;
		}
		ItemStack[] contents = GetContents(world, entityItem.Itemstack);
		if (!MealMeshCache.ContentsRotten(contents))
		{
			return;
		}
		for (int i = 0; i < contents.Length; i++)
		{
			if (contents[i] != null && contents[i].StackSize > 0 && contents[i].Collectible.Code.Path == "rot")
			{
				world.SpawnItemEntity(contents[i], entityItem.ServerPos.XYZ);
			}
		}
		entityItem.Itemstack.Attributes.RemoveAttribute("sealed");
		entityItem.Itemstack.Attributes.RemoveAttribute("recipeCode");
		entityItem.Itemstack.Attributes.RemoveAttribute("quantityServings");
		entityItem.Itemstack.Attributes.RemoveAttribute("contents");
		entityItem.MarkShapeModified();
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (!(api is ICoreClientAPI coreClientAPI) || !coreClientAPI.ObjectCache.TryGetValue("blockcrockGuiMeshRefs", out var value))
		{
			return;
		}
		if (value is Dictionary<string, MultiTextureMeshRef> dictionary)
		{
			foreach (KeyValuePair<string, MultiTextureMeshRef> item in dictionary)
			{
				item.Value.Dispose();
			}
		}
		coreClientAPI.ObjectCache.Remove("blockcrockGuiMeshRefs");
	}

	public bool IsFullAndUnsealed(ItemStack stack)
	{
		if (!IsEmpty(stack))
		{
			return !stack.Attributes.GetBool("sealed");
		}
		return false;
	}

	public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority)
	{
		if (priority == EnumMergePriority.DirectMerge)
		{
			JsonObject itemAttributes = sourceStack.ItemAttributes;
			if (itemAttributes != null && itemAttributes["canSealCrock"]?.AsBool() == true && IsFullAndUnsealed(sinkStack))
			{
				return 1;
			}
		}
		return base.GetMergableQuantity(sinkStack, sourceStack, priority);
	}

	public override void TryMergeStacks(ItemStackMergeOperation op)
	{
		ItemSlot sourceSlot = op.SourceSlot;
		if (op.CurrentPriority == EnumMergePriority.DirectMerge)
		{
			JsonObject itemAttributes = sourceSlot.Itemstack.ItemAttributes;
			if (itemAttributes != null && itemAttributes["canSealCrock"]?.AsBool() == true)
			{
				ItemSlot sinkSlot = op.SinkSlot;
				if (IsFullAndUnsealed(sinkSlot.Itemstack))
				{
					sinkSlot.Itemstack.Attributes.SetBool("sealed", value: true);
					op.MovedQuantity = 1;
					sourceSlot.TakeOut(1);
					sinkSlot.MarkDirty();
				}
				return;
			}
		}
		base.TryMergeStacks(op);
	}

	public override bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		ItemStack itemstack = activeHotbarSlot.Itemstack;
		if (itemstack != null && itemstack.Collectible.Attributes?["canSealCrock"]?.AsBool() == true)
		{
			if (IsFullAndUnsealed(slot.Itemstack))
			{
				slot.Itemstack.Attributes.SetBool("sealed", value: true);
				activeHotbarSlot.TakeOut(1);
				activeHotbarSlot.MarkDirty();
			}
			else
			{
				(api as ICoreClientAPI)?.TriggerIngameError(this, "crockemptyorsealed", Lang.Get("ingameerror-crock-empty-or-sealed"));
			}
			return true;
		}
		return base.OnContainedInteractStart(be, slot, byPlayer, blockSel);
	}
}
