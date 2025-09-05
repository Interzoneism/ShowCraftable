using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockPie : BlockMeal, IBakeableCallback, IShelvable
{
	private MealMeshCache? ms;

	private WorldInteraction[]? interactions;

	public static PieTopCrustType[] TopCrustTypes;

	private ModelTransform oneSliceTranformGui = new ModelTransform
	{
		Origin = new FastVec3f(0.375f, 0.1f, 0.375f),
		Scale = 2.82f,
		Rotation = new FastVec3f(-27f, 132f, -5f)
	}.EnsureDefaultValues();

	private ModelTransform oneSliceTranformTp = new ModelTransform
	{
		Translation = new FastVec3f(-0.82f, -0.34f, -0.57f),
		Origin = new FastVec3f(0.5f, 0.13f, 0.5f),
		Scale = 0.7f,
		Rotation = new FastVec3f(-49f, 29f, -112f)
	}.EnsureDefaultValues();

	public string State => Variant["state"];

	protected override bool PlacedBlockEating => false;

	public EnumShelvableLayout? GetShelvableType(ItemStack stack)
	{
		return stack.Attributes.GetAsInt("pieSize") switch
		{
			1 => EnumShelvableLayout.Quadrants, 
			2 => EnumShelvableLayout.Halves, 
			_ => EnumShelvableLayout.SingleCenter, 
		};
	}

	public ModelTransform? GetOnShelfTransform(ItemStack stack)
	{
		return GetShelvableType(stack) switch
		{
			EnumShelvableLayout.Quadrants => stack.Collectible.Attributes?["onShelfQuarterTransform"].AsObject<ModelTransform>(), 
			EnumShelvableLayout.Halves => stack.Collectible.Attributes?["onShelfHalfTransform"].AsObject<ModelTransform>(), 
			_ => stack.Collectible.Attributes?["onShelfFullTransform"].AsObject<ModelTransform>(), 
		};
	}

	[MemberNotNull(new string[] { "ms", "interactions" })]
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (TopCrustTypes == null)
		{
			TopCrustTypes = api.Assets.Get("config/pietopcrusttypes.json").ToObject<PieTopCrustType[]>();
		}
		InteractionHelpYOffset = 0.375f;
		interactions = ObjectCacheUtil.GetOrCreate(api, "pieInteractions-", delegate
		{
			ItemStack[] knifeStacks = BlockUtil.GetKnifeStacks(api);
			List<ItemStack> list = new List<ItemStack>();
			List<ItemStack> list2 = new List<ItemStack>();
			if (list.Count == 0 && list2.Count == 0)
			{
				foreach (CollectibleObject collectible in api.World.Collectibles)
				{
					if (collectible is ItemDough)
					{
						list2.Add(new ItemStack(collectible, 2));
					}
					if (collectible.Attributes?["inPieProperties"]?.AsObject<InPieProperties>(null, collectible.Code.Domain) != null && !(collectible is ItemDough))
					{
						list.Add(new ItemStack(collectible, 2));
					}
				}
			}
			return new WorldInteraction[4]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-pie-cut",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = knifeStacks,
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityPie { State: not null, SlicesLeft: >1 }) ? wi.Itemstacks : null
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-pie-addfilling",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityPie { State: "raw", HasAllFilling: false }) ? wi.Itemstacks : null
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-pie-addcrust",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list2.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityPie { State: "raw", HasAllFilling: not false, HasCrust: false }) ? wi.Itemstacks : null
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-pie-changecruststyle",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = knifeStacks,
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityPie { State: "raw", HasCrust: not false }) ? wi.Itemstacks : null
				}
			};
		});
		ms = api.ModLoader.GetModSystem<MealMeshCache>();
		displayContentsInfo = false;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (canEat(slot))
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (!canEat(slot))
		{
			return false;
		}
		return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (canEat(slot))
		{
			base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
		}
	}

	protected bool canEat(ItemSlot slot)
	{
		ItemStack itemstack = slot.Itemstack;
		if (itemstack != null && itemstack.Attributes?.GetAsInt("pieSize") == 1)
		{
			return State != "raw";
		}
		return false;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		if (itemstack.Attributes.GetAsInt("pieSize") == 1)
		{
			if (target == EnumItemRenderTarget.Gui)
			{
				renderinfo.Transform = oneSliceTranformGui;
			}
			if (target == EnumItemRenderTarget.HandTp)
			{
				renderinfo.Transform = oneSliceTranformTp;
			}
		}
		renderinfo.ModelRef = ms.GetOrCreatePieMeshRef(itemstack);
	}

	public override MeshData? GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos? atBlockPos = null)
	{
		return ms.GetPieMesh(itemstack);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = (world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPie)?.Inventory[0].Itemstack;
		if (itemStack != null)
		{
			return itemStack.Clone();
		}
		return base.OnPickBlock(world, pos);
	}

	public void OnBaked(ItemStack oldStack, ItemStack newStack)
	{
		newStack.Attributes["contents"] = oldStack.Attributes["contents"];
		newStack.Attributes.SetInt("pieSize", oldStack.Attributes.GetAsInt("pieSize"));
		newStack.Attributes.SetString("topCrustType", GetTopCrustType(oldStack));
		newStack.Attributes.SetInt("bakeLevel", oldStack.Attributes.GetAsInt("bakeLevel") + 1);
		ItemStack[] contents = GetContents(api.World, newStack);
		TransitionableProperties transitionableProperties = newStack.Collectible.GetTransitionableProperties(api.World, newStack, null).FirstOrDefault((TransitionableProperties p) => p.Type == EnumTransitionType.Perish);
		transitionableProperties?.TransitionedStack.Resolve(api.World, "pie perished stack");
		DummyInventory dummyInventory = new DummyInventory(api, 4);
		dummyInventory[0].Itemstack = contents[0];
		dummyInventory[1].Itemstack = contents[1];
		dummyInventory[2].Itemstack = contents[2];
		dummyInventory[3].Itemstack = contents[3];
		if (transitionableProperties != null)
		{
			CollectibleObject.CarryOverFreshness(api, dummyInventory.Slots, contents, transitionableProperties);
		}
		SetContents(newStack, contents);
	}

	public void TryPlacePie(EntityAgent byEntity, BlockSelection blockSel)
	{
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		InPieProperties obj = (player?.InventoryManager.ActiveHotbarSlot)?.Itemstack?.ItemAttributes["inPieProperties"]?.AsObject<InPieProperties>();
		if (obj != null && obj.PartType == EnumPiePartType.Crust)
		{
			BlockPos blockPos = blockSel.Position.UpCopy();
			if (api.World.BlockAccessor.GetBlock(blockPos).Replaceable >= 6000)
			{
				api.World.BlockAccessor.SetBlock(Id, blockPos);
				(api.World.BlockAccessor.GetBlockEntity(blockPos) as BlockEntityPie)?.OnPlaced(player);
			}
		}
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = (world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPie)?.Inventory[0]?.Itemstack;
		if (itemStack != null)
		{
			return GetHeldItemName(itemStack);
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public override string GetHeldItemName(ItemStack? itemStack)
	{
		ItemStack[] contents = GetContents(api.World, itemStack);
		if (contents.Length <= 1)
		{
			return Lang.Get("pie-empty");
		}
		ItemStack itemStack2 = contents[1];
		if (itemStack2 == null)
		{
			return Lang.Get("pie-empty");
		}
		bool flag = true;
		int num = 2;
		while (flag && num < contents.Length - 1)
		{
			if (contents[num] != null)
			{
				flag &= itemStack2.Equals(api.World, contents[num], GlobalConstants.IgnoredStackAttributes);
				itemStack2 = contents[num];
			}
			num++;
		}
		string text = Variant["state"];
		if (MealMeshCache.ContentsRotten(contents))
		{
			return Lang.Get("pie-single-rotten");
		}
		if (flag)
		{
			return Lang.Get("pie-single-" + itemStack2.Collectible.Code.ToShortString() + "-" + text);
		}
		return Lang.Get("pie-mixed-" + FillingFoodCategory(contents[1]).ToString().ToLowerInvariant() + "-" + text);
	}

	public static EnumFoodCategory FillingFoodCategory(ItemStack? stack)
	{
		return stack?.Collectible.NutritionProps?.FoodCategory ?? stack?.ItemAttributes?["nutritionPropsWhenInMeal"]?.AsObject<FoodNutritionProperties>()?.FoodCategory ?? EnumFoodCategory.Vegetable;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		ItemStack itemstack = inSlot.Itemstack;
		if (itemstack != null)
		{
			int asInt = itemstack.Attributes.GetAsInt("pieSize");
			float num = GetQuantityServings(world, itemstack);
			if (!itemstack.Attributes.HasAttribute("quantityServings"))
			{
				num = 1f;
			}
			if (asInt == 1)
			{
				dsc.AppendLine(Lang.Get("pie-slice-single", num));
			}
			else
			{
				dsc.AppendLine(Lang.Get("pie-slices", asInt));
			}
			TransitionableProperties[] transitionableProperties = itemstack.Collectible.GetTransitionableProperties(api.World, itemstack, null);
			if (transitionableProperties != null && transitionableProperties.Length != 0)
			{
				itemstack.Collectible.AppendPerishableInfoText(inSlot, dsc, api.World);
			}
			ItemStack[] contents = GetContents(api.World, itemstack);
			EntityPlayer forEntity = (world as IClientWorldAccessor)?.Player?.Entity;
			float[] nutritionHealthMul = GetNutritionHealthMul(null, inSlot, forEntity);
			dsc.AppendLine(GetContentNutritionFacts(api.World, inSlot, contents, null, mulWithStacksize: true, num * nutritionHealthMul[0], num * nutritionHealthMul[1]));
		}
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		InventoryBase inventoryBase = (world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPie)?.Inventory;
		if (inventoryBase != null && inventoryBase.Count >= 1)
		{
			ItemStack itemstack = inventoryBase[0].Itemstack;
			if (itemstack != null)
			{
				ItemStack[] contents = GetContents(api.World, itemstack);
				StringBuilder stringBuilder = new StringBuilder();
				TransitionableProperties[] transitionableProperties = itemstack.Collectible.GetTransitionableProperties(api.World, itemstack, null);
				if (transitionableProperties != null && transitionableProperties.Length != 0)
				{
					itemstack.Collectible.AppendPerishableInfoText(inventoryBase[0], stringBuilder, api.World);
				}
				float num = GetQuantityServings(world, itemstack);
				ITreeAttribute attributes = itemstack.Attributes;
				if (attributes != null && !attributes.HasAttribute("quantityServings"))
				{
					num = (float)itemstack.Attributes.GetAsInt("pieSize") / 4f;
				}
				float[] nutritionHealthMul = GetNutritionHealthMul(pos, null, forPlayer.Entity);
				return stringBuilder.ToString() + GetContentNutritionFacts(api.World, inventoryBase[0], contents, null, mulWithStacksize: true, nutritionHealthMul[0] * num, nutritionHealthMul[1] * num);
			}
		}
		return "";
	}

	protected override TransitionState[]? UpdateAndGetTransitionStatesNative(IWorldAccessor world, ItemSlot inslot)
	{
		return base.UpdateAndGetTransitionStatesNative(world, inslot);
	}

	public override TransitionState? UpdateAndGetTransitionState(IWorldAccessor world, ItemSlot inslot, EnumTransitionType type)
	{
		ItemStack[] contents = GetContents(world, inslot.Itemstack);
		UnspoilContents(world, contents);
		SetContents(inslot.Itemstack, contents);
		return base.UpdateAndGetTransitionState(world, inslot, type);
	}

	public override TransitionState[]? UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
	{
		ItemStack[] contents = GetContents(world, inslot.Itemstack);
		UnspoilContents(world, contents);
		SetContents(inslot.Itemstack, contents);
		return base.UpdateAndGetTransitionStatesNative(world, inslot);
	}

	public override string GetContentNutritionFacts(IWorldAccessor world, ItemSlot inSlotorFirstSlot, ItemStack[] contentStacks, EntityAgent? forEntity, bool mulWithStacksize = false, float nutritionMul = 1f, float healthMul = 1f)
	{
		UnspoilContents(world, contentStacks);
		return base.GetContentNutritionFacts(world, inSlotorFirstSlot, contentStacks, forEntity, mulWithStacksize, nutritionMul, healthMul);
	}

	protected void UnspoilContents(IWorldAccessor world, ItemStack[] cstacks)
	{
		foreach (ItemStack itemStack in cstacks)
		{
			if (itemStack == null)
			{
				continue;
			}
			if (!(itemStack.Attributes["transitionstate"] is ITreeAttribute))
			{
				itemStack.Attributes["transitionstate"] = new TreeAttribute();
			}
			ITreeAttribute treeAttribute = (ITreeAttribute)itemStack.Attributes["transitionstate"];
			if (treeAttribute.HasAttribute("createdTotalHours"))
			{
				treeAttribute.SetDouble("createdTotalHours", world.Calendar.TotalHours);
				treeAttribute.SetDouble("lastUpdatedTotalHours", world.Calendar.TotalHours);
				float[] array = (treeAttribute["transitionedHours"] as FloatArrayAttribute)?.value;
				int num = 0;
				while (array != null && num < array.Length)
				{
					array[num] = 0f;
					num++;
				}
			}
		}
	}

	public override float[] GetNutritionHealthMul(BlockPos? pos, ItemSlot? slot, EntityAgent? forEntity)
	{
		float num = 1f;
		if (slot == null && pos != null)
		{
			slot = (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPie)?.Inventory[0];
		}
		if (slot != null)
		{
			num = GlobalConstants.FoodSpoilageSatLossMul((slot.Itemstack?.Collectible.UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish))?.TransitionLevel ?? 0f, slot.Itemstack, forEntity);
		}
		return new float[2]
		{
			Attributes["nutritionMul"].AsFloat(1f) * num,
			num
		};
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntityPie obj = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityPie;
		if (obj == null || !obj.OnInteract(byPlayer))
		{
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		return true;
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
	}

	public override int GetRandomContentColor(ICoreClientAPI capi, ItemStack[] stacks)
	{
		if (stacks.Length == 0)
		{
			return 8421504;
		}
		ItemStack[] contents = GetContents(capi.World, stacks[0]);
		if (contents.Length == 0)
		{
			return 8421504;
		}
		ItemStack itemStack = contents[capi.World.Rand.Next(stacks.Length)];
		return itemStack.Collectible.GetRandomColor(capi, itemStack);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		WorldInteraction[] placedBlockInteractionHelp = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
		placedBlockInteractionHelp = placedBlockInteractionHelp.RemoveAt(1);
		return interactions.Append<WorldInteraction>(placedBlockInteractionHelp);
	}

	public static List<CookingRecipe> GetHandbookRecipes(ICoreAPI api, ItemStack[] allStacks)
	{
		List<ItemStack> doughs = new List<ItemStack>();
		Dictionary<EnumFoodCategory, List<ItemStack>> dictionary = new Dictionary<EnumFoodCategory, List<ItemStack>>();
		List<ItemStack> crusts = new List<ItemStack>();
		List<ItemStack> list = new List<ItemStack>();
		foreach (ItemStack itemStack in allStacks)
		{
			InPieProperties inPieProperties = itemStack.ItemAttributes?["inPieProperties"]?.AsObject<InPieProperties>();
			if (inPieProperties != null && inPieProperties.PartType == EnumPiePartType.Crust)
			{
				doughs.Add(itemStack);
			}
			if (inPieProperties != null && inPieProperties.PartType == EnumPiePartType.Filling && inPieProperties.AllowMixing)
			{
				EnumFoodCategory enumFoodCategory = FillingFoodCategory(itemStack);
				if (enumFoodCategory != EnumFoodCategory.NoNutrition && enumFoodCategory != EnumFoodCategory.Unknown)
				{
					if (dictionary.ContainsKey(enumFoodCategory))
					{
						dictionary[enumFoodCategory].Add(itemStack);
					}
					else
					{
						int num = 1;
						List<ItemStack> list2 = new List<ItemStack>(num);
						CollectionsMarshal.SetCount(list2, num);
						Span<ItemStack> span = CollectionsMarshal.AsSpan(list2);
						int index = 0;
						span[index] = itemStack;
						dictionary.Add(enumFoodCategory, list2);
					}
				}
			}
			if (inPieProperties != null && inPieProperties.PartType == EnumPiePartType.Crust && inPieProperties.AllowMixing)
			{
				crusts.Add(itemStack);
			}
			if (inPieProperties != null && !inPieProperties.AllowMixing)
			{
				list.Add(itemStack);
			}
		}
		List<CookingRecipe> list3 = new List<CookingRecipe>();
		list3.AddRange(dictionary.Select((KeyValuePair<EnumFoodCategory, List<ItemStack>> entry) => CreateRecipe(api.World, "mixed-" + entry.Key.ToString().ToLowerInvariant(), doughs, entry.Value.ToList(), crusts)));
		list3.AddRange(list.Select(delegate(ItemStack stack)
		{
			IWorldAccessor world = api.World;
			string code = "single-" + stack.Collectible.Code.Path;
			List<ItemStack> doughs2 = doughs;
			int num2 = 1;
			List<ItemStack> list4 = new List<ItemStack>(num2);
			CollectionsMarshal.SetCount(list4, num2);
			Span<ItemStack> span2 = CollectionsMarshal.AsSpan(list4);
			int index2 = 0;
			span2[index2] = stack;
			return CreateRecipe(world, code, doughs2, list4, crusts);
		}));
		return list3;
	}

	private static CookingRecipe CreateRecipe(IWorldAccessor world, string code, List<ItemStack> doughs, List<ItemStack> fillings, List<ItemStack> crusts)
	{
		CookingRecipe cookingRecipe = new CookingRecipe();
		cookingRecipe.Code = code;
		cookingRecipe.Ingredients = new CookingRecipeIngredient[3]
		{
			new CookingRecipeIngredient
			{
				Code = "dough",
				TypeName = "bottomcrust",
				MinQuantity = 1,
				MaxQuantity = 1,
				ValidStacks = doughs.Select((ItemStack dough) => new CookingRecipeStack
				{
					Code = dough.Collectible.Code,
					Type = dough.Collectible.ItemClass,
					Quantity = 2,
					ResolvedItemstack = dough.Clone()
				}).ToArray()
			},
			new CookingRecipeIngredient
			{
				Code = "filling",
				TypeName = "piefilling",
				MinQuantity = 4,
				MaxQuantity = 4,
				ValidStacks = fillings.Select((ItemStack filling) => new CookingRecipeStack
				{
					Code = filling.Collectible.Code,
					Type = filling.Collectible.ItemClass,
					Quantity = 2,
					ResolvedItemstack = filling.Clone()
				}).ToArray()
			},
			new CookingRecipeIngredient
			{
				Code = "crust",
				TypeName = "topcrust",
				MinQuantity = 0,
				MaxQuantity = 1,
				ValidStacks = crusts.Select((ItemStack crust) => new CookingRecipeStack
				{
					Code = crust.Collectible.Code,
					Type = crust.Collectible.ItemClass,
					Quantity = 2,
					ResolvedItemstack = crust.Clone()
				}).ToArray()
			}
		};
		cookingRecipe.PerishableProps = new TransitionableProperties();
		return cookingRecipe;
	}

	public static ItemStack?[] GenerateRandomPie(ICoreAPI api, ref Dictionary<CookingRecipeIngredient, HashSet<ItemStack?>>? cachedValidStacksByIngredient, CookingRecipe recipe, ItemStack? ingredientStack = null)
	{
		if (recipe.Ingredients == null)
		{
			return new ItemStack[6];
		}
		Dictionary<CookingRecipeIngredient, HashSet<ItemStack>> dictionary = cachedValidStacksByIngredient;
		if (cachedValidStacksByIngredient == null)
		{
			dictionary = new Dictionary<CookingRecipeIngredient, HashSet<ItemStack>>();
			CookingRecipeIngredient[] ingredients = recipe.Ingredients;
			foreach (CookingRecipeIngredient cookingRecipeIngredient in ingredients)
			{
				HashSet<ItemStack> hashSet = new HashSet<ItemStack>();
				new List<AssetLocation>();
				foreach (ItemStack item2 in cookingRecipeIngredient.ValidStacks.Select((CookingRecipeStack stack) => stack.ResolvedItemstack))
				{
					ItemStack item = item2.Clone();
					hashSet.Add(item);
				}
				if (cookingRecipeIngredient.MinQuantity <= 0)
				{
					hashSet.Add(null);
				}
				dictionary.Add(cookingRecipeIngredient.Clone(), hashSet);
			}
			cachedValidStacksByIngredient = dictionary;
		}
		if (dictionary == null)
		{
			return new ItemStack[6];
		}
		List<ItemStack> list = new List<ItemStack>();
		while (!recipe.Matches(list.ToArray()))
		{
			Dictionary<CookingRecipeIngredient, List<ItemStack>> dictionary2 = new Dictionary<CookingRecipeIngredient, List<ItemStack>>();
			foreach (KeyValuePair<CookingRecipeIngredient, HashSet<ItemStack>> item3 in dictionary)
			{
				dictionary2.Add(item3.Key.Clone(), item3.Value.ToList());
			}
			dictionary2 = dictionary2.OrderBy((KeyValuePair<CookingRecipeIngredient, List<ItemStack>> x) => api.World.Rand.Next()).ToDictionary((KeyValuePair<CookingRecipeIngredient, List<ItemStack>> keyValuePair) => keyValuePair.Key, (KeyValuePair<CookingRecipeIngredient, List<ItemStack>> keyValuePair) => keyValuePair.Value);
			CookingRecipeIngredient cookingRecipeIngredient2 = null;
			if (ingredientStack != null)
			{
				List<CookingRecipeIngredient> list2 = recipe.Ingredients.Where((CookingRecipeIngredient ingredient) => ingredient.Matches(ingredientStack)).ToList();
				cookingRecipeIngredient2 = list2[api.World.Rand.Next(list2.Count)].Clone();
			}
			list = new List<ItemStack>();
			CookingRecipeIngredient key = dictionary2.Where((KeyValuePair<CookingRecipeIngredient, List<ItemStack>> entry) => entry.Key.Code == "dough").FirstOrDefault().Key;
			List<ItemStack> value = dictionary2.Where((KeyValuePair<CookingRecipeIngredient, List<ItemStack>> entry) => entry.Key.Code == "dough").FirstOrDefault().Value;
			if (key.Code == cookingRecipeIngredient2?.Code)
			{
				ItemStack itemStack = value.First((ItemStack stack) => stack?.Collectible.Code == ingredientStack?.Collectible.Code);
				if (itemStack != null)
				{
					list.Add(itemStack.Clone());
					key.MinQuantity--;
					key.MaxQuantity--;
				}
				cookingRecipeIngredient2 = null;
			}
			while (key.MinQuantity > 0)
			{
				list.Add(value[api.World.Rand.Next(value.Count)]?.Clone());
				key.MinQuantity--;
				key.MaxQuantity--;
			}
			key = dictionary2.Where((KeyValuePair<CookingRecipeIngredient, List<ItemStack>> entry) => entry.Key.Code == "filling").FirstOrDefault().Key;
			value = dictionary2.Where((KeyValuePair<CookingRecipeIngredient, List<ItemStack>> entry) => entry.Key.Code == "filling").FirstOrDefault().Value;
			if (key.Code == cookingRecipeIngredient2?.Code)
			{
				ItemStack itemStack2 = value.First((ItemStack stack) => stack?.Collectible.Code == ingredientStack?.Collectible.Code);
				if (itemStack2 != null)
				{
					list.Add(itemStack2.Clone());
					key.MinQuantity--;
					key.MaxQuantity--;
				}
				cookingRecipeIngredient2 = null;
			}
			while (key.MinQuantity > 0)
			{
				list.Add(value[api.World.Rand.Next(value.Count)]?.Clone());
				key.MinQuantity--;
				key.MaxQuantity--;
			}
			key = dictionary2.Where((KeyValuePair<CookingRecipeIngredient, List<ItemStack>> entry) => entry.Key.Code == "crust").FirstOrDefault().Key;
			value = dictionary2.Where((KeyValuePair<CookingRecipeIngredient, List<ItemStack>> entry) => entry.Key.Code == "crust").FirstOrDefault().Value;
			if (cookingRecipeIngredient2 != null)
			{
				if (key.Code == cookingRecipeIngredient2?.Code)
				{
					ItemStack itemStack3 = value.First((ItemStack stack) => stack?.Collectible.Code == ingredientStack?.Collectible.Code);
					if (itemStack3 != null)
					{
						list.Add(itemStack3.Clone());
						key.MaxQuantity--;
						cookingRecipeIngredient2 = null;
					}
				}
			}
			else if (key.MaxQuantity > 0)
			{
				ItemStack itemStack4 = value[api.World.Rand.Next(value.Count)];
				if (itemStack4 != null)
				{
					list.Add(itemStack4.Clone());
					key.MaxQuantity--;
				}
			}
			while (list.Count < 6)
			{
				list.Add(null);
			}
		}
		return list.ToArray();
	}

	[return: NotNullIfNotNull("pieStack")]
	public static ItemStack? CycleTopCrustType(ItemStack? pieStack)
	{
		if (pieStack == null)
		{
			return null;
		}
		string topCrustType = GetTopCrustType(pieStack);
		pieStack.Attributes.SetString("topCrustType", TopCrustTypes[(TopCrustTypes.IndexOf((PieTopCrustType type) => type.Code.EqualsFast(topCrustType)) + 1) % TopCrustTypes.Length].Code);
		return pieStack;
	}

	[return: NotNullIfNotNull("pieStack")]
	public static string? GetTopCrustType(ItemStack? pieStack)
	{
		if (pieStack == null)
		{
			return null;
		}
		string topCrustType = pieStack.Attributes.GetAsString("topCrustType", "full");
		if (!TopCrustTypes.Any((PieTopCrustType type) => type.Code.EqualsFast(topCrustType)))
		{
			switch (topCrustType.ToInt())
			{
			default:
				topCrustType = "full";
				break;
			case 1:
				topCrustType = "square";
				break;
			case 2:
				topCrustType = "diagonal";
				break;
			}
			pieStack.Attributes.SetString("topCrustType", topCrustType);
		}
		return topCrustType;
	}

	public override string HandbookPageCodeForStack(IWorldAccessor world, ItemStack stack)
	{
		string text = null;
		ItemStack[] contents = GetContents(world, stack);
		if (contents != null && contents.Length > 1)
		{
			ItemStack obj = contents[1];
			text = ((obj == null || obj.ItemAttributes?["inPieProperties"]?.AsObject<InPieProperties>()?.AllowMixing != false) ? ("mixed-" + FillingFoodCategory(contents[1]).ToString().ToLowerInvariant()) : ("single-" + contents[1].Collectible.Code.ToShortString()));
			return "handbook-mealrecipe-" + text + "-pie";
		}
		return "craftinginfo-pie";
	}
}
