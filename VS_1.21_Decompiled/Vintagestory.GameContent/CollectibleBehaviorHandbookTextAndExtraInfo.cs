using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class CollectibleBehaviorHandbookTextAndExtraInfo : CollectibleBehavior
{
	protected const int TinyPadding = 2;

	protected const int TinyIndent = 2;

	protected const int MarginBottom = 3;

	protected const int SmallPadding = 7;

	protected const int MediumPadding = 14;

	public ExtraHandbookSection[] ExtraHandBookSections;

	private ICoreAPI Api;

	private Dictionary<string, Dictionary<CookingRecipeIngredient, HashSet<ItemStack>>> cachedValidStacks;

	private InventorySmelting dummySmeltingInv;

	public CollectibleBehaviorHandbookTextAndExtraInfo(CollectibleObject collObj)
		: base(collObj)
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		Api = api;
		cachedValidStacks = new Dictionary<string, Dictionary<CookingRecipeIngredient, HashSet<ItemStack>>>();
		dummySmeltingInv = new InventorySmelting("smelting-handbook", api);
		JsonObject jsonObject = collObj.Attributes?["handbook"]?["extraSections"];
		if (jsonObject != null && jsonObject.Exists)
		{
			ExtraHandBookSections = jsonObject?.AsObject<ExtraHandbookSection[]>();
		}
	}

	public virtual RichTextComponentBase[] GetHandbookInfo(ItemSlot inSlot, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor)
	{
		ItemStack itemstack = inSlot.Itemstack;
		List<RichTextComponentBase> list = new List<RichTextComponentBase>();
		addGeneralInfo(inSlot, capi, itemstack, list, out var marginTop, out var marginBottom);
		List<ItemStack> list2 = new List<ItemStack>();
		List<ItemStack> list3 = new List<ItemStack>();
		List<ItemStack> list4 = new List<ItemStack>();
		List<ItemStack> list5 = new List<ItemStack>();
		int num = 0;
		while (num < allStacks.Length)
		{
			ItemStack itemStack = allStacks[num];
			JsonObject itemAttributes = itemStack.ItemAttributes;
			if (itemAttributes != null && itemAttributes.KeyExists("cookingContainerSlots"))
			{
				list3.Add(itemStack);
			}
			CombustibleProperties combustibleProps = itemStack.Collectible.CombustibleProps;
			if (combustibleProps != null)
			{
				_ = combustibleProps.BurnDuration;
				if (true)
				{
					goto IL_00a4;
				}
			}
			CombustibleProperties combustibleProps2 = itemStack.Collectible.CombustibleProps;
			if (combustibleProps2 != null)
			{
				_ = combustibleProps2.BurnTemperature;
				if (true)
				{
					goto IL_00a4;
				}
			}
			goto IL_00ad;
			IL_00ad:
			CollectibleObject collectible = itemStack.Collectible;
			if ((collectible is BlockToolMold || collectible is BlockIngotMold) ? true : false)
			{
				list5.Add(itemStack);
			}
			if (itemStack.Block != null)
			{
				BlockDropItemStack[] dropsForHandbook = itemStack.Block.GetDropsForHandbook(itemStack, capi.World.Player);
				if (dropsForHandbook != null)
				{
					for (int i = 0; i < dropsForHandbook.Length; i++)
					{
						_ = dropsForHandbook[i];
						if (dropsForHandbook[i].ResolvedItemstack.Equals(capi.World, itemstack, GlobalConstants.IgnoredStackAttributes))
						{
							list2.Add(itemStack);
						}
					}
				}
			}
			num++;
			continue;
			IL_00a4:
			list4.Add(itemStack);
			goto IL_00ad;
		}
		addDropsInfo(capi, openDetailPageFor, itemstack, list, marginTop, list2);
		bool haveText = addObtainedThroughInfo(capi, allStacks, openDetailPageFor, itemstack, list, marginTop, list2);
		haveText = addFoundInInfo(capi, openDetailPageFor, itemstack, list, marginTop, haveText);
		haveText = addAlloyForInfo(capi, openDetailPageFor, itemstack, list, marginTop, list3, list4, haveText);
		haveText = addAlloyedFromInfo(capi, allStacks, openDetailPageFor, itemstack, list, marginTop, list3, list4, haveText);
		haveText = addProcessesIntoInfo(capi, openDetailPageFor, itemstack, list, marginTop, marginBottom, list3, list4, haveText);
		haveText = addIngredientForInfo(capi, allStacks, openDetailPageFor, itemstack, list, marginTop, list3, list4, list5, haveText);
		haveText = addCreatedByInfo(capi, allStacks, openDetailPageFor, itemstack, list, marginTop, list3, list4, list5, haveText);
		addExtraSections(capi, itemstack, list, marginTop);
		addStorableInfo(capi, allStacks, openDetailPageFor, itemstack, list, marginTop);
		addStoredInInfo(capi, allStacks, openDetailPageFor, itemstack, list, marginTop);
		collObj.GetCollectibleInterface<ICustomHandbookPageContent>()?.OnHandbookPageComposed(list, inSlot, capi, allStacks, openDetailPageFor);
		return list.ToArray();
	}

	protected void addGeneralInfo(ItemSlot inSlot, ICoreClientAPI capi, ItemStack stack, List<RichTextComponentBase> components, out float marginTop, out float marginBottom)
	{
		components.Add(new ItemstackTextComponent(capi, stack, 100.0, 10.0));
		components.AddRange(VtmlUtil.Richtextify(capi, stack.GetName() + "\n", CairoFont.WhiteSmallishText()));
		if (capi.Settings.Bool["extendedDebugInfo"])
		{
			CairoFont cairoFont = CairoFont.WhiteDetailText();
			cairoFont.Color[3] = 0.5;
			components.AddRange(VtmlUtil.Richtextify(capi, "Page code:" + GuiHandbookItemStackPage.PageCodeForStack(stack) + "\n", cairoFont));
		}
		components.AddRange(VtmlUtil.Richtextify(capi, stack.GetDescription(capi.World, inSlot), CairoFont.WhiteSmallText()));
		marginTop = 7f;
		marginBottom = 3f;
	}

	protected void addDropsInfo(ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, List<ItemStack> breakBlocks)
	{
		if (stack.Class != EnumItemClass.Block)
		{
			return;
		}
		BlockDropItemStack[] dropsForHandbook = stack.Block.GetDropsForHandbook(stack, capi.World.Player);
		List<ItemStack[]> list = new List<ItemStack[]>();
		List<EnumTool?> list2 = new List<EnumTool?>();
		List<ItemStack> list3 = new List<ItemStack>();
		if (dropsForHandbook != null)
		{
			BlockDropItemStack[] array = dropsForHandbook;
			foreach (BlockDropItemStack val in array)
			{
				list3.Add(val.ResolvedItemstack);
				object obj;
				if (val.Tool.HasValue)
				{
					ICoreClientAPI api = capi;
					EnumTool? tool = val.Tool;
					obj = ObjectCacheUtil.GetOrCreate(api, "blockhelp-collect-withtool-" + tool.ToString(), delegate
					{
						List<ItemStack> list5 = new List<ItemStack>();
						foreach (CollectibleObject collectible in capi.World.Collectibles)
						{
							if (collectible.Tool == val.Tool)
							{
								list5.Add(new ItemStack(collectible));
							}
						}
						return list5.ToArray();
					});
				}
				else
				{
					obj = null;
				}
				ItemStack[] item = (ItemStack[])obj;
				list2.Add(val.Tool);
				list.Add(item);
			}
		}
		if (list3 != null && list3.Count > 0)
		{
			int j;
			for (j = 0; j < breakBlocks.Count; j++)
			{
				if (list3.Any((ItemStack itemStack3) => itemStack3.Equals(capi.World, breakBlocks[j], GlobalConstants.IgnoredStackAttributes)))
				{
					breakBlocks.RemoveAt(j);
				}
			}
			bool haveText = components.Count > 0;
			AddHeading(components, capi, "Drops when broken", ref haveText);
			components.Add(new ClearFloatTextComponent(capi, 2f));
			int index = 0;
			while (list3.Count > 0)
			{
				ItemStack itemStack = list3[0];
				EnumTool? enumTool = list2[index];
				ItemStack[] array2 = list[index++];
				list3.RemoveAt(0);
				if (itemStack == null)
				{
					continue;
				}
				SlideshowItemstackTextComponent slideshowItemstackTextComponent = new SlideshowItemstackTextComponent(capi, itemStack, list3, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				if (array2 != null)
				{
					slideshowItemstackTextComponent.ExtraTooltipText = "\n\n<font color=\"orange\">" + Lang.Get("break-requires-tool-" + enumTool.ToString().ToLowerInvariant()) + "</font>";
				}
				components.Add(slideshowItemstackTextComponent);
				if (array2 != null)
				{
					slideshowItemstackTextComponent = new SlideshowItemstackTextComponent(capi, array2, 24.0, EnumFloat.Left, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					slideshowItemstackTextComponent.renderOffset.X = 0f - (float)GuiElement.scaled(17.0);
					slideshowItemstackTextComponent.renderOffset.Z = 100f;
					slideshowItemstackTextComponent.ShowTooltip = false;
					components.Add(slideshowItemstackTextComponent);
				}
			}
			components.Add(new ClearFloatTextComponent(capi, 2f));
		}
		List<ItemStack> list4 = new List<ItemStack>();
		BlockDropItemStack[] array3 = stack.Block.GetBehavior<BlockBehaviorHarvestable>()?.harvestedStacks;
		if (array3 != null)
		{
			list4 = array3.Select((BlockDropItemStack hStack) => hStack?.ResolvedItemstack).ToList();
		}
		if (list4 == null || list4.Count <= 0)
		{
			return;
		}
		bool haveText2 = components.Count > 0;
		AddHeading(components, capi, "handbook-dropswhen-harvested", ref haveText2);
		components.Add(new ClearFloatTextComponent(capi, 2f));
		while (list4.Count > 0)
		{
			ItemStack itemStack2 = list4[0];
			list4.RemoveAt(0);
			if (itemStack2 != null)
			{
				SlideshowItemstackTextComponent item2 = new SlideshowItemstackTextComponent(capi, itemStack2, list4, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				components.Add(item2);
			}
		}
		components.Add(new ClearFloatTextComponent(capi, 2f));
	}

	protected bool addObtainedThroughInfo(ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, List<ItemStack> breakBlocks)
	{
		List<ItemStack> list = new List<ItemStack>();
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		HashSet<string> hashSet = new HashSet<string>();
		foreach (EntityProperties entityType in capi.World.EntityTypes)
		{
			if (entityType.Drops == null)
			{
				continue;
			}
			for (int i = 0; i < entityType.Drops.Length; i++)
			{
				if (entityType.Drops[i].ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
				{
					list2.Add(Lang.Get(entityType.Code.Domain + ":item-creature-" + entityType.Code.Path));
				}
			}
			BlockDropItemStack[] array = entityType.Attributes?["harvestableDrops"]?.AsArray<BlockDropItemStack>();
			if (array == null)
			{
				continue;
			}
			BlockDropItemStack[] array2 = array;
			foreach (BlockDropItemStack blockDropItemStack in array2)
			{
				blockDropItemStack.Resolve(Api.World, "handbook info", new AssetLocation());
				if (blockDropItemStack.ResolvedItemstack != null && blockDropItemStack.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
				{
					string text = entityType.Code.Domain + ":item-creature-" + entityType.Code.Path;
					JsonObject attributes = entityType.Attributes;
					if (attributes != null && attributes["handbook"]["groupcode"]?.Exists == true)
					{
						text = entityType.Attributes?["handbook"]["groupcode"].AsString();
					}
					if (!hashSet.Contains(text))
					{
						list3.Add(Lang.Get(text));
						hashSet.Add(text);
					}
					break;
				}
			}
		}
		foreach (ItemStack itemStack in allStacks)
		{
			BlockDropItemStack[] array3 = itemStack.Block?.GetBehavior<BlockBehaviorHarvestable>()?.harvestedStacks;
			if (array3 != null && array3.Any((BlockDropItemStack hStack) => hStack != null && hStack.ResolvedItemstack?.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) == true))
			{
				list.Add(itemStack);
			}
		}
		bool haveText = components.Count > 0;
		if (list2.Count > 0)
		{
			AddHeading(components, capi, "Obtained by killing", ref haveText);
			components.Add(new ClearFloatTextComponent(capi, 2f));
			RichTextComponent richTextComponent = new RichTextComponent(capi, string.Join(", ", list2) + "\n", CairoFont.WhiteSmallText());
			richTextComponent.PaddingLeft = 2.0;
			components.Add(richTextComponent);
		}
		if (list3.Count > 0)
		{
			AddHeading(components, capi, "handbook-obtainedby-killing-harvesting", ref haveText);
			components.Add(new ClearFloatTextComponent(capi, 2f));
			RichTextComponent richTextComponent2 = new RichTextComponent(capi, string.Join(", ", list3) + "\n", CairoFont.WhiteSmallText());
			richTextComponent2.PaddingLeft = 2.0;
			components.Add(richTextComponent2);
		}
		if (breakBlocks.Count > 0)
		{
			AddHeading(components, capi, "Obtained by breaking", ref haveText);
			components.Add(new ClearFloatTextComponent(capi, 2f));
			while (breakBlocks.Count > 0)
			{
				ItemStack itemStack2 = breakBlocks[0];
				breakBlocks.RemoveAt(0);
				if (itemStack2 != null)
				{
					SlideshowItemstackTextComponent item = new SlideshowItemstackTextComponent(capi, itemStack2, breakBlocks, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					components.Add(item);
				}
			}
			components.Add(new ClearFloatTextComponent(capi, 2f));
		}
		if (list.Count > 0)
		{
			AddHeading(components, capi, "handbook-obtainedby-block-harvesting", ref haveText);
			components.Add(new ClearFloatTextComponent(capi, 2f));
			while (list.Count > 0)
			{
				ItemStack itemStack3 = list[0];
				list.RemoveAt(0);
				if (itemStack3 != null)
				{
					SlideshowItemstackTextComponent item2 = new SlideshowItemstackTextComponent(capi, itemStack3, list, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					components.Add(item2);
				}
			}
			components.Add(new ClearFloatTextComponent(capi, 2f));
		}
		return haveText;
	}

	protected bool addFoundInInfo(ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, bool haveText)
	{
		string text = stack.Collectible.Attributes?["handbook"]?["foundIn"]?.AsString();
		if (text != null)
		{
			AddHeading(components, capi, "Found in", ref haveText);
			RichTextComponent richTextComponent = new RichTextComponent(capi, Lang.Get(text), CairoFont.WhiteSmallText());
			richTextComponent.PaddingLeft = 2.0;
			components.Add(richTextComponent);
		}
		JsonObject attributes = collObj.Attributes;
		if (attributes != null && attributes["hostRockFor"].Exists)
		{
			AddHeading(components, capi, "Host rock for", ref haveText);
			int[] array = collObj.Attributes?["hostRockFor"].AsArray<int>();
			OrderedDictionary<string, List<ItemStack>> orderedDictionary = new OrderedDictionary<string, List<ItemStack>>();
			for (int i = 0; i < array.Length; i++)
			{
				Block block = capi.World.Blocks[array[i]];
				string key = block.Code.ToString();
				JsonObject attributes2 = block.Attributes;
				if (attributes2 != null && attributes2["handbook"]["groupBy"].Exists)
				{
					key = block.Attributes["handbook"]["groupBy"].AsArray<string>()[0];
				}
				if (!orderedDictionary.ContainsKey(key))
				{
					orderedDictionary[key] = new List<ItemStack>();
				}
				orderedDictionary[key].Add(new ItemStack(block));
			}
			int num = 2;
			foreach (KeyValuePair<string, List<ItemStack>> item in orderedDictionary)
			{
				SlideshowItemstackTextComponent slideshowItemstackTextComponent = new SlideshowItemstackTextComponent(capi, item.Value.ToArray(), 40.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				slideshowItemstackTextComponent.PaddingLeft = num;
				num = 0;
				components.Add(slideshowItemstackTextComponent);
			}
		}
		JsonObject attributes3 = collObj.Attributes;
		if (attributes3 != null && attributes3["hostRock"].Exists)
		{
			AddHeading(components, capi, "Occurs in host rock", ref haveText);
			ushort[] array2 = collObj.Attributes?["hostRock"].AsArray<ushort>();
			OrderedDictionary<string, List<ItemStack>> orderedDictionary2 = new OrderedDictionary<string, List<ItemStack>>();
			for (int num2 = 0; num2 < array2.Length; num2++)
			{
				Block block2 = capi.World.Blocks[array2[num2]];
				string key2 = block2.Code.FirstCodePart();
				JsonObject attributes4 = block2.Attributes;
				if (attributes4 != null && attributes4["handbook"]["groupBy"].Exists)
				{
					key2 = block2.Attributes["handbook"]["groupBy"].AsArray<string>()[0];
				}
				if (block2.Attributes?["handbook"]?["exclude"]?.AsBool() != true)
				{
					if (!orderedDictionary2.ContainsKey(key2))
					{
						orderedDictionary2[key2] = new List<ItemStack>();
					}
					orderedDictionary2[key2].Add(new ItemStack(block2));
				}
			}
			int num3 = 2;
			foreach (KeyValuePair<string, List<ItemStack>> item2 in orderedDictionary2)
			{
				SlideshowItemstackTextComponent slideshowItemstackTextComponent2 = new SlideshowItemstackTextComponent(capi, item2.Value.ToArray(), 40.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				slideshowItemstackTextComponent2.PaddingLeft = num3;
				num3 = 0;
				components.Add(slideshowItemstackTextComponent2);
			}
		}
		return haveText;
	}

	protected bool addAlloyForInfo(ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, List<ItemStack> containers, List<ItemStack> fuels, bool haveText)
	{
		Dictionary<AssetLocation, ItemStack> dictionary = new Dictionary<AssetLocation, ItemStack>();
		foreach (AlloyRecipe metalAlloy in capi.GetMetalAlloys())
		{
			MetalAlloyIngredient[] ingredients = metalAlloy.Ingredients;
			for (int i = 0; i < ingredients.Length; i++)
			{
				if (ingredients[i].ResolvedItemstack.Equals(capi.World, stack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes) && getCanContainerSmelt(capi, containers, fuels, stack))
				{
					CollectibleObject collectible = metalAlloy.Output.ResolvedItemstack.Collectible;
					AssetLocation assetLocation = new AssetLocation(collectible.Code.Domain, "metalbit-" + collectible.Variant["metal"]);
					dictionary[assetLocation] = new ItemStack(capi.World.GetItem(assetLocation));
				}
			}
		}
		if (dictionary.Count > 0)
		{
			AddHeading(components, capi, "Alloy for", ref haveText);
			int num = 2;
			foreach (KeyValuePair<AssetLocation, ItemStack> item in dictionary)
			{
				ItemstackTextComponent itemstackTextComponent = new ItemstackTextComponent(capi, item.Value, 40.0, 0.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				itemstackTextComponent.PaddingLeft = num;
				num = 0;
				components.Add(itemstackTextComponent);
			}
			haveText = true;
		}
		return haveText;
	}

	protected bool addAlloyedFromInfo(ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, List<ItemStack> containers, List<ItemStack> fuels, bool haveText)
	{
		ItemStack itemStack = stack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;
		if (stack.Collectible.FirstCodePart() != "metalbit" || itemStack == null)
		{
			return haveText;
		}
		Dictionary<AssetLocation, MetalAlloyIngredient[]> dictionary = new Dictionary<AssetLocation, MetalAlloyIngredient[]>();
		foreach (AlloyRecipe metalAlloy in capi.GetMetalAlloys())
		{
			if (metalAlloy.Output.ResolvedItemstack.Equals(capi.World, itemStack, GlobalConstants.IgnoredStackAttributes))
			{
				List<MetalAlloyIngredient> list = metalAlloy.Ingredients.ToList();
				dictionary[metalAlloy.Output.ResolvedItemstack.Collectible.Code] = list.ToArray();
			}
		}
		if (dictionary.Count > 0)
		{
			AddHeading(components, capi, "Alloyed from", ref haveText);
			int num = 2;
			foreach (KeyValuePair<AssetLocation, MetalAlloyIngredient[]> item in dictionary)
			{
				MetalAlloyIngredient[] value = item.Value;
				foreach (MetalAlloyIngredient ingred in value)
				{
					string displayText = " " + Lang.Get("alloy-ratio-from-to", (int)(ingred.MinRatio * 100f), (int)(ingred.MaxRatio * 100f));
					components.Add(new RichTextComponent(capi, displayText, CairoFont.WhiteSmallText()));
					ItemStack[] array = allStacks.Where((ItemStack aStack) => ingred.ResolvedItemstack.Equals(capi.World, aStack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes) && getCanContainerSmelt(capi, containers, fuels, aStack)).ToArray();
					if (array == null || array.Length != 0)
					{
						ItemstackComponentBase itemstackComponentBase = new SlideshowItemstackTextComponent(capi, array, 30.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						itemstackComponentBase.offY = GuiElement.scaled(7.0);
						itemstackComponentBase.PaddingLeft = num;
						num = 0;
						components.Add(itemstackComponentBase);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
		}
		return haveText;
	}

	protected bool addProcessesIntoInfo(ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, float marginBottom, List<ItemStack> containers, List<ItemStack> fuels, bool haveText)
	{
		BakingProperties bakingProperties = collObj.Attributes?["bakingProperties"]?.AsObject<BakingProperties>();
		if (bakingProperties != null && bakingProperties.ResultCode != null)
		{
			Item item = capi.World.GetItem(new AssetLocation(bakingProperties.ResultCode));
			if (item != null)
			{
				AddHeading(components, capi, "smeltdesc-bake-title", ref haveText);
				ItemstackTextComponent itemstackTextComponent = new ItemstackTextComponent(capi, new ItemStack(item), 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				itemstackTextComponent.ShowStacksize = true;
				itemstackTextComponent.PaddingLeft = 2.0;
				components.Add(itemstackTextComponent);
				components.Add(new ClearFloatTextComponent(capi, marginBottom));
			}
		}
		else if (collObj.CombustibleProps?.SmeltedStack?.ResolvedItemstack != null && !collObj.CombustibleProps.SmeltedStack.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && (getCanSmelt(fuels, stack) || getCanBloomerySmelt(stack)))
		{
			string text = collObj.CombustibleProps.SmeltingType.ToString().ToLowerInvariant();
			AddHeading(components, capi, "game:smeltdesc-" + text + "-title", ref haveText);
			ItemstackTextComponent itemstackTextComponent2 = new ItemstackTextComponent(capi, collObj.CombustibleProps.SmeltedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
			{
				openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
			});
			itemstackTextComponent2.ShowStacksize = true;
			itemstackTextComponent2.PaddingLeft = 2.0;
			components.Add(itemstackTextComponent2);
			components.Add(new ClearFloatTextComponent(capi, marginBottom));
		}
		JsonObject attributes = collObj.Attributes;
		if (attributes != null && attributes["beehivekiln"].Exists)
		{
			Dictionary<string, JsonItemStack> dictionary = collObj.Attributes["beehivekiln"].AsObject<Dictionary<string, JsonItemStack>>();
			components.Add(new ClearFloatTextComponent(capi, 7f));
			components.Add(new RichTextComponent(capi, Lang.Get("game:smeltdesc-beehivekiln-title") + "\n", CairoFont.WhiteSmallText().WithWeight((FontWeight)1)));
			foreach (var (text3, jsonItemStack2) in dictionary)
			{
				if (jsonItemStack2 != null && jsonItemStack2.Resolve(capi.World, "beehivekiln-burn"))
				{
					components.Add(new ItemstackTextComponent(capi, jsonItemStack2.ResolvedItemstack.Clone(), 40.0, 0.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					}));
					components.Add(new RichTextComponent(capi, Lang.Get("smeltdesc-beehivekiln-opendoors", text3), CairoFont.WhiteSmallText().WithWeight((FontWeight)1))
					{
						VerticalAlign = EnumVerticalAlign.Middle
					});
					components.Add(new ItemstackTextComponent(capi, new ItemStack(capi.World.GetBlock("cokeovendoor-closed-north")), 40.0, 0.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					}));
					components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText())
					{
						VerticalAlign = EnumVerticalAlign.Middle
					});
				}
			}
		}
		JsonObject attributes2 = collObj.Attributes;
		if (attributes2 != null && attributes2["carburizableProps"]?["carburizedOutput"]?.Exists == true)
		{
			JsonItemStack jsonItemStack3 = stack.ItemAttributes["carburizableProps"]["carburizedOutput"].AsObject<JsonItemStack>(null, stack.Collectible.Code.Domain);
			if (jsonItemStack3 != null && jsonItemStack3.Resolve(Api.World, "carburizable handbook") && !jsonItemStack3.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				AddHeading(components, capi, "carburizesdesc-title", ref haveText);
				ItemstackTextComponent itemstackTextComponent3 = new ItemstackTextComponent(capi, jsonItemStack3.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				itemstackTextComponent3.ShowStacksize = true;
				itemstackTextComponent3.PaddingLeft = 2.0;
				components.Add(itemstackTextComponent3);
				components.Add(new ClearFloatTextComponent(capi, marginBottom));
			}
		}
		if (collObj.CrushingProps?.CrushedStack?.ResolvedItemstack != null && !collObj.CrushingProps.CrushedStack.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
		{
			AddHeading(components, capi, "pulverizesdesc-title", ref haveText);
			ItemstackTextComponent itemstackTextComponent4 = new ItemstackTextComponent(capi, collObj.CrushingProps.CrushedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
			{
				openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
			});
			itemstackTextComponent4.ShowStacksize = true;
			itemstackTextComponent4.PaddingLeft = 2.0;
			components.Add(itemstackTextComponent4);
			components.Add(new ClearFloatTextComponent(capi, marginBottom));
		}
		if (collObj.GrindingProps?.GroundStack?.ResolvedItemstack != null && !collObj.GrindingProps.GroundStack.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
		{
			AddHeading(components, capi, "Grinds into", ref haveText);
			ItemstackTextComponent itemstackTextComponent5 = new ItemstackTextComponent(capi, collObj.GrindingProps.GroundStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
			{
				openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
			});
			itemstackTextComponent5.ShowStacksize = true;
			itemstackTextComponent5.PaddingLeft = 2.0;
			components.Add(itemstackTextComponent5);
			components.Add(new ClearFloatTextComponent(capi, marginBottom));
		}
		JuiceableProperties juiceableProperties = getjuiceableProps(stack);
		double num = stack.Attributes?.GetDouble("juiceableLitresLeft") ?? 0.0;
		if (juiceableProperties != null && (juiceableProperties.LitresPerItem.HasValue || num > 0.0))
		{
			AddHeading(components, capi, "Juices into", ref haveText);
			ItemStack itemStack = juiceableProperties.LiquidStack.ResolvedItemstack.Clone();
			if (juiceableProperties.LitresPerItem.HasValue)
			{
				itemStack.StackSize = (int)(100f * juiceableProperties.LitresPerItem).Value;
			}
			if (num > 0.0)
			{
				itemStack.StackSize = (int)(100.0 * num);
			}
			ItemstackTextComponent itemstackTextComponent6 = new ItemstackTextComponent(capi, itemStack, 40.0, 0.0, EnumFloat.Inline, delegate(ItemStack cs)
			{
				openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
			});
			itemstackTextComponent6.ShowStacksize = juiceableProperties.LitresPerItem.HasValue;
			itemstackTextComponent6.PaddingLeft = 2.0;
			components.Add(itemstackTextComponent6);
			if (juiceableProperties.ReturnStack?.ResolvedItemstack == null)
			{
				if (!stack.Equals(capi.World, juiceableProperties.PressedStack.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes))
				{
					ItemStack itemstack = juiceableProperties.PressedStack.ResolvedItemstack.Clone();
					itemstackTextComponent6 = new ItemstackTextComponent(capi, itemstack, 40.0, 0.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					itemstackTextComponent6.PaddingLeft = 0.0;
					components.Add(itemstackTextComponent6);
				}
			}
			else
			{
				ItemStack itemStack2 = juiceableProperties.ReturnStack.ResolvedItemstack.Clone();
				if (juiceableProperties.LitresPerItem.HasValue)
				{
					itemStack2.StackSize /= (int)(1f / juiceableProperties.LitresPerItem).Value;
				}
				itemstackTextComponent6 = new ItemstackTextComponent(capi, itemStack2, 40.0, 0.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				itemstackTextComponent6.ShowStacksize = juiceableProperties.LitresPerItem.HasValue;
				itemstackTextComponent6.PaddingLeft = 0.0;
				components.Add(itemstackTextComponent6);
			}
			components.Add(new ClearFloatTextComponent(capi, marginBottom));
		}
		CollectibleBehaviorSqueezable collectibleBehavior = stack.Collectible.GetCollectibleBehavior<CollectibleBehaviorSqueezable>(withInheritance: true);
		if (collectibleBehavior != null)
		{
			AddHeading(components, capi, "squeezesdesc-title", ref haveText);
			int num2 = 2;
			if (collectibleBehavior.SqueezedLiquid != null)
			{
				ItemStack itemStack3 = new ItemStack(collectibleBehavior.SqueezedLiquid);
				itemStack3.StackSize = (int)(100f * collectibleBehavior.SqueezedLitres);
				ItemstackTextComponent itemstackTextComponent7 = new ItemstackTextComponent(capi, itemStack3, 40.0, 0.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				itemstackTextComponent7.ShowStacksize = true;
				itemstackTextComponent7.PaddingLeft = num2;
				components.Add(itemstackTextComponent7);
				num2 = 0;
			}
			JsonItemStack[] returnStacks = collectibleBehavior.ReturnStacks;
			foreach (JsonItemStack jsonItemStack4 in returnStacks)
			{
				if (jsonItemStack4?.ResolvedItemstack != null && !stack.Equals(capi.World, jsonItemStack4.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes))
				{
					ItemstackTextComponent itemstackTextComponent8 = new ItemstackTextComponent(capi, jsonItemStack4.ResolvedItemstack, 40.0, 0.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					itemstackTextComponent8.ShowStacksize = true;
					itemstackTextComponent8.PaddingLeft = num2;
					components.Add(itemstackTextComponent8);
					num2 = 0;
				}
			}
			components.Add(new ClearFloatTextComponent(capi, marginBottom));
		}
		DistillationProps distillationProps = getDistillationProps(stack);
		if (distillationProps != null)
		{
			AddHeading(components, capi, "One liter distills into", ref haveText);
			ItemStack itemStack4 = distillationProps.DistilledStack?.ResolvedItemstack.Clone();
			if (distillationProps.Ratio != 0f)
			{
				itemStack4.StackSize = (int)((float)(100 * stack.StackSize) * distillationProps.Ratio);
			}
			ItemstackTextComponent itemstackTextComponent9 = new ItemstackTextComponent(capi, itemStack4, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
			{
				openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
			});
			itemstackTextComponent9.ShowStacksize = distillationProps.Ratio != 0f;
			itemstackTextComponent9.PaddingLeft = 2.0;
			components.Add(itemstackTextComponent9);
			components.Add(new ClearFloatTextComponent(capi, marginBottom));
		}
		TransitionableProperties[] transitionableProperties = collObj.GetTransitionableProperties(capi.World, stack, null);
		if (transitionableProperties != null)
		{
			haveText = true;
			ClearFloatTextComponent item2 = new ClearFloatTextComponent(capi, 14f);
			bool flag = false;
			TransitionableProperties[] array = transitionableProperties;
			foreach (TransitionableProperties transitionableProperties2 in array)
			{
				switch (transitionableProperties2.Type)
				{
				case EnumTransitionType.Cure:
				{
					components.Add(item2);
					flag = true;
					components.Add(new RichTextComponent(capi, Lang.Get("After {0} hours, cures into", transitionableProperties2.FreshHours.avg + transitionableProperties2.TransitionHours.avg) + "\n", CairoFont.WhiteSmallText().WithWeight((FontWeight)1)));
					ItemstackTextComponent itemstackTextComponent15 = new ItemstackTextComponent(capi, transitionableProperties2.TransitionedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					itemstackTextComponent15.PaddingLeft = 2.0;
					components.Add(itemstackTextComponent15);
					break;
				}
				case EnumTransitionType.Ripen:
				{
					components.Add(item2);
					flag = true;
					components.Add(new RichTextComponent(capi, Lang.Get("After {0} hours of open storage, ripens into", transitionableProperties2.FreshHours.avg + transitionableProperties2.TransitionHours.avg) + "\n", CairoFont.WhiteSmallText().WithWeight((FontWeight)1)));
					ItemstackTextComponent itemstackTextComponent11 = new ItemstackTextComponent(capi, transitionableProperties2.TransitionedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					itemstackTextComponent11.PaddingLeft = 2.0;
					components.Add(itemstackTextComponent11);
					break;
				}
				case EnumTransitionType.Dry:
				{
					components.Add(item2);
					flag = true;
					components.Add(new RichTextComponent(capi, Lang.Get("After {0} hours of open storage, dries into", transitionableProperties2.FreshHours.avg + transitionableProperties2.TransitionHours.avg) + "\n", CairoFont.WhiteSmallText().WithWeight((FontWeight)1)));
					ItemstackTextComponent itemstackTextComponent14 = new ItemstackTextComponent(capi, transitionableProperties2.TransitionedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					itemstackTextComponent14.PaddingLeft = 2.0;
					components.Add(itemstackTextComponent14);
					break;
				}
				case EnumTransitionType.Melt:
				{
					components.Add(item2);
					flag = true;
					components.Add(new RichTextComponent(capi, Lang.Get("After {0} hours of open storage, melts into", transitionableProperties2.FreshHours.avg + transitionableProperties2.TransitionHours.avg) + "\n", CairoFont.WhiteSmallText().WithWeight((FontWeight)1)));
					ItemstackTextComponent itemstackTextComponent12 = new ItemstackTextComponent(capi, transitionableProperties2.TransitionedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					itemstackTextComponent12.PaddingLeft = 2.0;
					components.Add(itemstackTextComponent12);
					break;
				}
				case EnumTransitionType.Convert:
				{
					components.Add(item2);
					flag = true;
					components.Add(new RichTextComponent(capi, Lang.Get("handbook-processesinto-convert", transitionableProperties2.FreshHours.avg + transitionableProperties2.TransitionHours.avg) + "\n", CairoFont.WhiteSmallText().WithWeight((FontWeight)1)));
					ItemstackTextComponent itemstackTextComponent13 = new ItemstackTextComponent(capi, transitionableProperties2.TransitionedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					itemstackTextComponent13.PaddingLeft = 2.0;
					components.Add(itemstackTextComponent13);
					break;
				}
				case EnumTransitionType.Perish:
				{
					components.Add(item2);
					flag = true;
					components.Add(new RichTextComponent(capi, Lang.Get("handbook-processesinto-perish", transitionableProperties2.FreshHours.avg + transitionableProperties2.TransitionHours.avg) + "\n", CairoFont.WhiteSmallText().WithWeight((FontWeight)1)));
					ItemstackTextComponent itemstackTextComponent10 = new ItemstackTextComponent(capi, transitionableProperties2.TransitionedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					itemstackTextComponent10.PaddingLeft = 2.0;
					components.Add(itemstackTextComponent10);
					break;
				}
				}
			}
			if (flag)
			{
				components.Add(new ClearFloatTextComponent(capi, marginBottom));
			}
		}
		return haveText;
	}

	protected bool addIngredientForInfo(ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, List<ItemStack> containers, List<ItemStack> fuels, List<ItemStack> molds, bool haveText)
	{
		ItemStack maxstack = stack.Clone();
		maxstack.StackSize = maxstack.Collectible.MaxStackSize * 10;
		List<ItemStack> list = new List<ItemStack>();
		foreach (GridRecipe recval in capi.World.GridRecipes)
		{
			GridRecipeIngredient[] resolvedIngredients = recval.resolvedIngredients;
			foreach (GridRecipeIngredient gridRecipeIngredient in resolvedIngredients)
			{
				CraftingRecipeIngredient craftingRecipeIngredient = gridRecipeIngredient;
				if (craftingRecipeIngredient != null && craftingRecipeIngredient.SatisfiesAsIngredient(maxstack) && !list.Any((ItemStack s) => s.Equals(capi.World, recval.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
				{
					DummySlot dummySlot = new DummySlot();
					DummySlot[] array = new DummySlot[recval.Width * recval.Height];
					for (int num = 0; num < recval.Width; num++)
					{
						for (int num2 = 0; num2 < recval.Height; num2++)
						{
							GridRecipeIngredient elementInGrid = recval.GetElementInGrid(num2, num, recval.resolvedIngredients, recval.Width);
							ItemStack stack2 = elementInGrid?.ResolvedItemstack?.Clone();
							if (elementInGrid == gridRecipeIngredient)
							{
								stack2 = maxstack;
							}
							array[num2 * recval.Width + num] = new DummySlot(stack2);
						}
					}
					GridRecipe gridRecipe = recval;
					ItemSlot[] inputSlots = array;
					gridRecipe.GenerateOutputStack(inputSlots, dummySlot);
					list.Add(dummySlot.Itemstack);
				}
				ItemStack returnedStack = craftingRecipeIngredient?.ReturnedStack?.ResolvedItemstack;
				ItemStack itemStack = returnedStack;
				if (itemStack != null && !itemStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list.Any((ItemStack s) => s.Equals(capi.World, returnedStack, GlobalConstants.IgnoredStackAttributes)) && recval.resolvedIngredients.Any((GridRecipeIngredient ingred) => ingred?.SatisfiesAsIngredient(maxstack) ?? false))
				{
					list.Add(returnedStack);
				}
			}
		}
		foreach (SmithingRecipe val in capi.GetSmithingRecipes())
		{
			if (val.Ingredient.SatisfiesAsIngredient(maxstack) && !list.Any((ItemStack s) => s.Equals(capi.World, val.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)) && getIsAnvilWorkable(stack.Collectible, fuels))
			{
				list.Add(val.Output.ResolvedItemstack);
			}
		}
		foreach (ClayFormingRecipe val2 in capi.GetClayformingRecipes())
		{
			if (val2.Ingredient.SatisfiesAsIngredient(maxstack) && !list.Any((ItemStack s) => s.Equals(capi.World, val2.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
			{
				list.Add(val2.Output.ResolvedItemstack);
			}
		}
		foreach (KnappingRecipe val3 in capi.GetKnappingRecipes())
		{
			if (val3.Ingredient.SatisfiesAsIngredient(maxstack) && !list.Any((ItemStack s) => s.Equals(capi.World, val3.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
			{
				list.Add(val3.Output.ResolvedItemstack);
			}
		}
		foreach (BarrelRecipe recipe in capi.GetBarrelRecipes())
		{
			BarrelRecipeIngredient[] ingredients = recipe.Ingredients;
			for (int i = 0; i < ingredients.Length; i++)
			{
				if (ingredients[i].SatisfiesAsIngredient(maxstack) && !list.Any((ItemStack s) => s.Equals(capi.World, recipe.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
				{
					list.Add(recipe.Output.ResolvedItemstack);
				}
			}
		}
		foreach (CookingRecipe cookingRecipe in capi.GetCookingRecipes())
		{
			if (cookingRecipe.CooksInto?.ResolvedItemstack == null)
			{
				continue;
			}
			CookingRecipeIngredient[] ingredients2 = cookingRecipe.Ingredients;
			for (int i = 0; i < ingredients2.Length; i++)
			{
				if (ingredients2[i].GetMatchingStack(stack) != null)
				{
					list.Add(cookingRecipe.CooksInto.ResolvedItemstack);
				}
			}
		}
		if (stack.Collectible is BlockAnvilPart)
		{
			list.Add(new ItemStack(Api.World.GetBlock(new AssetLocation("anvil-" + stack.Collectible.Variant["metal"]))));
		}
		JsonObject itemAttributes = stack.ItemAttributes;
		if (itemAttributes != null && itemAttributes.IsTrue("isFlux"))
		{
			foreach (Block item3 in capi.World.Blocks.Where((Block block) => block is BlockAnvilPart))
			{
				ItemStack anvil = new ItemStack(Api.World.GetBlock(new AssetLocation("anvil-" + item3.Variant["metal"])));
				if (!list.Any((ItemStack s) => s.Equals(capi.World, anvil, GlobalConstants.IgnoredStackAttributes)))
				{
					list.Add(anvil);
				}
			}
		}
		if (stack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack.Collectible is ItemIngot itemIngot)
		{
			foreach (ItemStack mold in molds)
			{
				if (!getCanContainerSmelt(capi, containers, fuels, stack))
				{
					break;
				}
				ItemStack smeltedStack = stack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;
				if (smeltedStack != null && mold.Collectible is BlockIngotMold && !list.Any((ItemStack s) => s.Equals(capi.World, smeltedStack, GlobalConstants.IgnoredStackAttributes)))
				{
					list.Add(smeltedStack);
					continue;
				}
				string newValue = itemIngot.LastCodePart();
				JsonItemStack dropStack = mold.ItemAttributes?["drop"]?.AsObject<JsonItemStack>()?.Clone();
				if (dropStack != null)
				{
					dropStack.Code.Path = dropStack.Code.Path.Replace("{metal}", newValue);
					dropStack.Resolve(capi.World, "handbookmolds", printWarningOnError: false);
					if (dropStack.ResolvedItemstack != null && !list.Any((ItemStack s) => s.Equals(capi.World, dropStack.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
					{
						list.Add(dropStack.ResolvedItemstack);
					}
					continue;
				}
				JsonItemStack[] array2 = mold.ItemAttributes?["drops"].AsObject<JsonItemStack[]>(null, mold.Collectible.Code.Domain);
				if (array2 == null)
				{
					continue;
				}
				new List<ItemStack>();
				JsonItemStack[] array3 = array2;
				foreach (JsonItemStack dstack in array3)
				{
					dstack.Code.Path = dstack.Code.Path.Replace("{metal}", newValue);
					dstack.Resolve(capi.World, "handbookmolds", printWarningOnError: false);
					if (dstack.ResolvedItemstack != null && !list.Any((ItemStack s) => s.Equals(capi.World, dstack.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
					{
						list.Add(dstack.ResolvedItemstack);
					}
				}
			}
		}
		JuiceableProperties juiceableProperties = getjuiceableProps(stack);
		if (juiceableProperties != null)
		{
			ItemStack pstack = juiceableProperties.PressedStack.ResolvedItemstack.Clone();
			pstack.Attributes.SetDouble("juiceableLitresLeft", 1.0);
			if (juiceableProperties.ReturnStack != null && !stack.Equals(capi.World, pstack, GlobalConstants.IgnoredStackAttributes) && !list.Any((ItemStack s) => s.Equals(capi.World, pstack, GlobalConstants.IgnoredStackAttributes)))
			{
				list.Add(pstack);
			}
		}
		List<CookingRecipe> list2 = new List<CookingRecipe>();
		foreach (CookingRecipe cookingRecipe2 in capi.GetCookingRecipes())
		{
			if (cookingRecipe2.CooksInto?.ResolvedItemstack != null)
			{
				continue;
			}
			CookingRecipeIngredient[] ingredients2 = cookingRecipe2.Ingredients;
			foreach (CookingRecipeIngredient cookingRecipeIngredient in ingredients2)
			{
				if (!list2.Contains(cookingRecipe2) && cookingRecipeIngredient.GetMatchingStack(stack) != null)
				{
					list2.Add(cookingRecipe2);
				}
			}
		}
		List<CookingRecipe> list3 = new List<CookingRecipe>();
		foreach (CookingRecipe handbookRecipe in BlockPie.GetHandbookRecipes(capi, allStacks))
		{
			if (handbookRecipe.CooksInto?.ResolvedItemstack != null)
			{
				continue;
			}
			CookingRecipeIngredient[] ingredients2 = handbookRecipe.Ingredients;
			foreach (CookingRecipeIngredient cookingRecipeIngredient2 in ingredients2)
			{
				if (!list3.Contains(handbookRecipe) && cookingRecipeIngredient2.GetMatchingStack(stack) != null)
				{
					list3.Add(handbookRecipe);
				}
			}
		}
		if (list.Count > 0 || list2.Count > 0 || list3.Count > 0)
		{
			AddHeading(components, capi, "Ingredient for", ref haveText);
			components.Add(new ClearFloatTextComponent(capi, 2f));
			while (list.Count > 0)
			{
				ItemStack itemStack2 = list[0];
				list.RemoveAt(0);
				if (itemStack2 != null)
				{
					SlideshowItemstackTextComponent item = new SlideshowItemstackTextComponent(capi, itemStack2, list, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					components.Add(item);
				}
			}
			while (list2.Count > 0 || list3.Count > 0)
			{
				bool isPie = false;
				int slots = 4;
				CookingRecipe recipe2;
				if (list2.Count > 0)
				{
					recipe2 = list2[0];
					list2.RemoveAt(0);
				}
				else
				{
					recipe2 = list3[0];
					list3.RemoveAt(0);
					slots = 6;
					isPie = true;
				}
				if (recipe2 != null)
				{
					ItemStack itemStack3;
					if (isPie)
					{
						itemStack3 = new ItemStack(capi.World.BlockAccessor.GetBlock("pie-perfect"));
						itemStack3.Attributes.SetInt("pieSize", 4);
						itemStack3.Attributes.SetString("topCrustType", BlockPie.TopCrustTypes[capi.World.Rand.Next(BlockPie.TopCrustTypes.Length)].Code);
						itemStack3.Attributes.SetInt("bakeLevel", 2);
					}
					else
					{
						itemStack3 = new ItemStack(BlockMeal.RandomMealBowl(capi));
					}
					Dictionary<CookingRecipeIngredient, HashSet<ItemStack>> valueOrDefault = cachedValidStacks.GetValueOrDefault(recipe2.Code);
					MealstackTextComponent item2 = new MealstackTextComponent(capi, ref valueOrDefault, itemStack3, recipe2, 40.0, EnumFloat.Inline, allStacks, delegate
					{
						openDetailPageFor("handbook-mealrecipe-" + recipe2.Code + (isPie ? "-pie" : ""));
					}, slots, isPie, maxstack);
					cachedValidStacks[recipe2.Code] = valueOrDefault;
					components.Add(item2);
				}
			}
			components.Add(new ClearFloatTextComponent(capi, 3f));
		}
		return haveText;
	}

	protected bool addCreatedByInfo(ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, List<ItemStack> containers, List<ItemStack> fuels, List<ItemStack> molds, bool haveText)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		List<KnappingRecipe> list = new List<KnappingRecipe>();
		foreach (KnappingRecipe knappingRecipe in capi.GetKnappingRecipes())
		{
			if (knappingRecipe.Output.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				list.Add(knappingRecipe);
			}
		}
		List<ClayFormingRecipe> list2 = new List<ClayFormingRecipe>();
		foreach (ClayFormingRecipe clayformingRecipe in capi.GetClayformingRecipes())
		{
			if (clayformingRecipe.Output.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				list2.Add(clayformingRecipe);
			}
		}
		list2 = list2.OrderBy((ClayFormingRecipe recipe) => recipe.Output.ResolvedItemstack?.StackSize).ToList();
		if (stack.Collectible is BlockAnvil && capi.World.GetBlock(new AssetLocation("anvilpart-base-" + stack.Collectible.Variant["metal"])) != null)
		{
			flag3 = true;
		}
		List<GridRecipe> list3 = new List<GridRecipe>();
		foreach (GridRecipe gridRecipe4 in capi.World.GridRecipes)
		{
			if (!gridRecipe4.ShowInCreatedBy)
			{
				continue;
			}
			ItemStack resolvedItemstack = gridRecipe4.Output.ResolvedItemstack;
			if (resolvedItemstack != null && resolvedItemstack.Satisfies(stack))
			{
				list3.Add(gridRecipe4);
				continue;
			}
			GridRecipeIngredient[] array = gridRecipe4.resolvedIngredients.ToArray();
			foreach (GridRecipeIngredient gridRecipeIngredient in array)
			{
				ItemStack itemStack = gridRecipeIngredient?.ReturnedStack?.ResolvedItemstack;
				if (itemStack != null && itemStack.Satisfies(stack))
				{
					ItemStack resolvedItemstack2 = gridRecipeIngredient.ResolvedItemstack;
					if (resolvedItemstack2 != null && !resolvedItemstack2.Satisfies(stack))
					{
						list3.Add(gridRecipe4);
						break;
					}
				}
			}
		}
		List<CookingRecipe> list4 = new List<CookingRecipe>();
		foreach (CookingRecipe cookingRecipe in capi.GetCookingRecipes())
		{
			if (cookingRecipe.CooksInto?.ResolvedItemstack?.Satisfies(stack) == true)
			{
				list4.Add(cookingRecipe);
			}
		}
		List<SmithingRecipe> list5 = new List<SmithingRecipe>();
		foreach (SmithingRecipe smithingRecipe in capi.GetSmithingRecipes())
		{
			ItemStack resolvedItemstack3 = smithingRecipe.Output.ResolvedItemstack;
			if (resolvedItemstack3 != null && resolvedItemstack3.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && getIsAnvilWorkable(smithingRecipe.Ingredient.ResolvedItemstack?.Collectible, fuels))
			{
				flag = true;
				list5.Add(smithingRecipe);
			}
		}
		list5 = list5.OrderBy((SmithingRecipe recipe) => recipe.Output.ResolvedItemstack?.StackSize).ToList();
		List<ItemStack> list6 = new List<ItemStack>();
		foreach (ItemStack mold in molds)
		{
			string text = stack.Collectible.Variant["metal"];
			if (mold.Collectible is BlockIngotMold && stack.Collectible is ItemIngot)
			{
				list6.Add(mold);
				continue;
			}
			JsonItemStack jsonItemStack = mold.ItemAttributes?["drop"]?.AsObject<JsonItemStack>()?.Clone();
			if (jsonItemStack != null)
			{
				text = stack.Collectible.LastCodePart();
				jsonItemStack.Code.Path = jsonItemStack.Code.Path.Replace("{metal}", text);
				jsonItemStack.Resolve(capi.World, "handbookmolds", printWarningOnError: false);
				ItemStack resolvedItemstack4 = jsonItemStack.ResolvedItemstack;
				if (resolvedItemstack4 != null && resolvedItemstack4.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
				{
					list6.Add(mold);
				}
				continue;
			}
			JsonItemStack[] array2 = mold.ItemAttributes?["drops"].AsObject<JsonItemStack[]>(null, mold.Collectible.Code.Domain);
			if (array2 == null)
			{
				continue;
			}
			new List<ItemStack>();
			JsonItemStack[] array3 = array2;
			foreach (JsonItemStack jsonItemStack2 in array3)
			{
				text = stack.Collectible.LastCodePart();
				jsonItemStack2.Code.Path = jsonItemStack2.Code.Path.Replace("{metal}", text);
				jsonItemStack2.Resolve(capi.World, "handbookmolds", printWarningOnError: false);
				ItemStack resolvedItemstack5 = jsonItemStack2.ResolvedItemstack;
				if (resolvedItemstack5 != null && resolvedItemstack5.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
				{
					list6.Add(mold);
				}
			}
		}
		List<ItemStack> list7 = new List<ItemStack>();
		List<ItemStack> list8 = new List<ItemStack>();
		List<ItemStack> list9 = new List<ItemStack>();
		List<ItemStack> list10 = new List<ItemStack>();
		List<ItemStack> list11 = new List<ItemStack>();
		List<ItemStack> list12 = new List<ItemStack>();
		List<ItemStack> list13 = new List<ItemStack>();
		List<ItemStack> list14 = new List<ItemStack>();
		List<ItemStack> list15 = new List<ItemStack>();
		List<ItemStack> list16 = new List<ItemStack>();
		List<ItemStack> list17 = new List<ItemStack>();
		List<ItemStack> list18 = new List<ItemStack>();
		List<ItemStack> list19 = new List<ItemStack>();
		List<ItemStack> list20 = new List<ItemStack>();
		List<ItemStack> list21 = new List<ItemStack>();
		List<ItemStack> list22 = new List<ItemStack>();
		List<ItemStack> list23 = new List<ItemStack>();
		List<ItemStack> list24 = new List<ItemStack>();
		List<ItemStack> list25 = new List<ItemStack>();
		List<ItemStack> list26 = new List<ItemStack>();
		int num = 0;
		while (num < allStacks.Length)
		{
			ItemStack val = allStacks[num];
			ItemStack itemStack2 = val.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;
			if (itemStack2 != null && itemStack2.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !val.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				if (getCanBloomerySmelt(val) && !list11.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
				{
					ItemStack itemStack3 = val.Clone();
					itemStack3.StackSize = val.Collectible.CombustibleProps.SmeltedRatio;
					list11.Add(itemStack3);
				}
				if (val.Collectible.CombustibleProps.SmeltingType == EnumSmeltType.Fire && !list12.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
				{
					list12.Add(val);
				}
				else if (getCanSmelt(fuels, val) && !list10.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
				{
					ItemStack itemStack4 = val.Clone();
					itemStack4.StackSize = val.Collectible.CombustibleProps.SmeltedRatio;
					list10.Add(itemStack4);
				}
			}
			if (list6.Count > 0 && itemStack2?.Collectible is ItemIngot && itemStack2?.Collectible.Variant["metal"] == stack.Collectible.LastCodePart() && getCanContainerSmelt(capi, containers, fuels, val))
			{
				ItemStack itemStack5 = val.Clone();
				itemStack5.StackSize = (list6.FirstOrDefault()?.ItemAttributes?["requiredUnits"].AsInt(100) ?? 100) / 5;
				list9.Add(itemStack5);
				flag2 = true;
			}
			if (flag && !val.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				IAnvilWorkable collectibleInterface = val.Collectible.GetCollectibleInterface<IAnvilWorkable>();
				if (collectibleInterface != null)
				{
					List<SmithingRecipe> matchingRecipes = collectibleInterface.GetMatchingRecipes(val);
					foreach (SmithingRecipe item3 in list5)
					{
						if (matchingRecipes.Contains(item3))
						{
							ItemStack itemStack6 = val.Clone();
							itemStack6.StackSize = (int)Math.Ceiling((double)item3.Voxels.Cast<bool>().Count((bool voxel) => voxel) / (double)collectibleInterface.VoxelCountForHandbook(val));
							list9.Add(itemStack6);
							break;
						}
					}
				}
			}
			if (list.Count > 0 && !val.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && list.Where((KnappingRecipe recipe) => recipe.Ingredient.SatisfiesAsIngredient(val, checkStacksize: false)).FirstOrDefault() != null)
			{
				list7.Add(val);
			}
			if (list2.Count > 0 && !val.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				ClayFormingRecipe clayFormingRecipe = list2.Where((ClayFormingRecipe recipe) => recipe.Ingredient.SatisfiesAsIngredient(val, checkStacksize: false)).FirstOrDefault();
				if (clayFormingRecipe != null)
				{
					ItemStack itemStack7 = val.Clone();
					itemStack7.StackSize = (int)Math.Ceiling(GameMath.Max(1f, (float)(clayFormingRecipe.Voxels.Cast<bool>().Count((bool voxel) => voxel) - 64) / 25f));
					list8.Add(itemStack7);
				}
			}
			Dictionary<string, JsonItemStack> dictionary = val.ItemAttributes?["beehivekiln"].AsObject<Dictionary<string, JsonItemStack>>();
			if (dictionary != null)
			{
				foreach (JsonItemStack value2 in dictionary.Values)
				{
					if (value2 != null && value2.Resolve(capi.World, "beehivekiln-burn") && value2.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list12.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
					{
						list12.Add(val);
						break;
					}
				}
			}
			JsonItemStack jsonItemStack3 = val.ItemAttributes?["carburizableProps"]?["carburizedOutput"]?.AsObject<JsonItemStack>(null, val.Collectible.Code.Domain);
			if (jsonItemStack3 != null && jsonItemStack3.Resolve(Api.World, "carburizable handbook"))
			{
				ItemStack carburizedStack = jsonItemStack3.ResolvedItemstack;
				if (carburizedStack != null && carburizedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list13.Any((ItemStack s) => s.Equals(capi.World, carburizedStack, GlobalConstants.IgnoredStackAttributes)))
				{
					list13.Add(val);
				}
			}
			ItemStack groundStack = val.Collectible.GrindingProps?.GroundStack.ResolvedItemstack;
			if (groundStack != null && groundStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list14.Any((ItemStack s) => s.Equals(capi.World, groundStack, GlobalConstants.IgnoredStackAttributes)))
			{
				list14.Add(val);
			}
			ItemStack crushedStack = val.Collectible.CrushingProps?.CrushedStack.ResolvedItemstack;
			if (crushedStack != null && crushedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list15.Any((ItemStack s) => s.Equals(capi.World, crushedStack, GlobalConstants.IgnoredStackAttributes)))
			{
				list15.Add(val);
			}
			if (val.Collectible is ItemOre)
			{
				val.ItemAttributes["metalUnits"].AsInt(5);
				string text2 = val.Collectible.Variant["ore"].Replace("quartz_", "").Replace("galena_", "");
				Item item = capi.World.GetItem(new AssetLocation("nugget-" + text2));
				if (item != null)
				{
					ItemStack outStack = new ItemStack(item);
					if (outStack != null && outStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list25.Any((ItemStack s) => s.Equals(capi.World, outStack, GlobalConstants.IgnoredStackAttributes)))
					{
						list25.Add(val);
					}
				}
			}
			JsonObject itemAttributes = val.ItemAttributes;
			ItemStack itemStack9;
			ItemStack itemStack10;
			if (itemAttributes != null && itemAttributes["juiceableProperties"].Exists)
			{
				JuiceableProperties juiceableProperties = getjuiceableProps(val);
				ItemStack itemStack8 = juiceableProperties?.LiquidStack?.ResolvedItemstack;
				itemStack9 = juiceableProperties?.PressedStack?.ResolvedItemstack;
				itemStack10 = juiceableProperties?.ReturnStack?.ResolvedItemstack;
				if (itemStack8 != null && itemStack8.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list22.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
				{
					JuiceableProperties juiceableProperties2 = getjuiceableProps(val);
					if (juiceableProperties2 == null || !juiceableProperties2.LitresPerItem.HasValue)
					{
						ITreeAttribute attributes = val.Attributes;
						if (attributes == null || !(attributes.GetDouble("juiceableLitresLeft") > 0.0))
						{
							goto IL_1241;
						}
					}
					list22.Add(val);
				}
				goto IL_1241;
			}
			goto IL_13ee;
			IL_13ee:
			CollectibleBehaviorSqueezable collectibleBehavior = val.Collectible.GetCollectibleBehavior<CollectibleBehaviorSqueezable>(withInheritance: true);
			if (collectibleBehavior != null && collectibleBehavior.ReturnStacks != null)
			{
				JsonItemStack[] array3 = collectibleBehavior.ReturnStacks;
				foreach (JsonItemStack jsonItemStack4 in array3)
				{
					if (jsonItemStack4?.ResolvedItemstack != null && stack.Equals(capi.World, jsonItemStack4.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes) && !list23.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
					{
						list23.Add(val);
					}
				}
				if (collectibleBehavior.SqueezedLiquid != null)
				{
					ItemStack sourceStack = new ItemStack(collectibleBehavior.SqueezedLiquid);
					if (stack.Equals(capi.World, sourceStack, GlobalConstants.IgnoredStackAttributes) && !list23.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
					{
						list23.Add(val);
					}
				}
			}
			JsonObject itemAttributes2 = val.ItemAttributes;
			if (itemAttributes2 != null && itemAttributes2["distillationProps"].Exists)
			{
				ItemStack itemStack11 = getDistillationProps(val)?.DistilledStack?.ResolvedItemstack;
				if (itemStack11 != null && itemStack11.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list24.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
				{
					list24.Add(val);
				}
			}
			JsonObject itemAttributes3 = val.ItemAttributes;
			if (itemAttributes3 != null && itemAttributes3.IsTrue("isFlux"))
			{
				list26.Add(val);
			}
			TransitionableProperties[] transitionableProperties = val.Collectible.GetTransitionableProperties(capi.World, val, null);
			if (transitionableProperties != null)
			{
				TransitionableProperties[] array4 = transitionableProperties;
				foreach (TransitionableProperties transitionableProperties2 in array4)
				{
					ItemStack transitionedStack = transitionableProperties2.TransitionedStack?.ResolvedItemstack;
					switch (transitionableProperties2.Type)
					{
					case EnumTransitionType.Cure:
						if (transitionedStack != null && transitionedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list16.Any((ItemStack s) => s.Equals(capi.World, transitionedStack, GlobalConstants.IgnoredStackAttributes)))
						{
							list16.Add(val);
						}
						break;
					case EnumTransitionType.Ripen:
						if (transitionedStack != null && transitionedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list16.Any((ItemStack s) => s.Equals(capi.World, transitionedStack, GlobalConstants.IgnoredStackAttributes)))
						{
							list17.Add(val);
						}
						break;
					case EnumTransitionType.Dry:
						if (transitionedStack != null && transitionedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list16.Any((ItemStack s) => s.Equals(capi.World, transitionedStack, GlobalConstants.IgnoredStackAttributes)))
						{
							list18.Add(val);
						}
						break;
					case EnumTransitionType.Melt:
						if (transitionedStack != null && transitionedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list16.Any((ItemStack s) => s.Equals(capi.World, transitionedStack, GlobalConstants.IgnoredStackAttributes)))
						{
							list19.Add(val);
						}
						break;
					case EnumTransitionType.Convert:
						if (transitionedStack != null && transitionedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list16.Any((ItemStack s) => s.Equals(capi.World, transitionedStack, GlobalConstants.IgnoredStackAttributes)))
						{
							list20.Add(val);
						}
						break;
					case EnumTransitionType.Perish:
						if (transitionedStack != null && transitionedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list16.Any((ItemStack s) => s.Equals(capi.World, transitionedStack, GlobalConstants.IgnoredStackAttributes)))
						{
							list21.Add(val);
						}
						break;
					}
				}
			}
			num++;
			continue;
			IL_1241:
			if (itemStack9 != null)
			{
				IClientWorldAccessor world = capi.World;
				ItemStack sourceStack2 = stack;
				string[] ignoredStackAttributes = GlobalConstants.IgnoredStackAttributes;
				int num2 = 0;
				string[] array5 = new string[1 + ignoredStackAttributes.Length];
				ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(ignoredStackAttributes);
				readOnlySpan.CopyTo(new Span<string>(array5).Slice(num2, readOnlySpan.Length));
				num2 += readOnlySpan.Length;
				array5[num2] = "juiceableLitresLeft";
				if (itemStack9.Equals(world, sourceStack2, array5) && !list22.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
				{
					ItemStack itemStack12 = val;
					IClientWorldAccessor world2 = capi.World;
					array5 = GlobalConstants.IgnoredStackAttributes;
					num2 = 0;
					ignoredStackAttributes = new string[1 + array5.Length];
					readOnlySpan = new ReadOnlySpan<string>(array5);
					readOnlySpan.CopyTo(new Span<string>(ignoredStackAttributes).Slice(num2, readOnlySpan.Length));
					num2 += readOnlySpan.Length;
					ignoredStackAttributes[num2] = "juiceableLitresLeft";
					if (!itemStack12.Equals(world2, itemStack9, ignoredStackAttributes))
					{
						list22.Add(val);
					}
				}
			}
			if (itemStack10 != null && itemStack10.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !list22.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
			{
				list22.Add(val);
			}
			goto IL_13ee;
		}
		List<RichTextComponentBase> list27 = BuildBarrelRecipesText(capi, stack, openDetailPageFor);
		string text3 = stack.Collectible.Attributes?["handbook"]?["createdBy"]?.AsString();
		string text4 = collObj.Attributes?["bakingProperties"]?.AsObject<BakingProperties>()?.InitialCode;
		if (list3.Count > 0 || list4.Count > 0 || list9.Count > 0 || list7.Count > 0 || list8.Count > 0 || flag3 || text3 != null || list10.Count > 0 || list11.Count > 0 || list12.Count > 0 || list13.Count > 0 || list27.Count > 0 || list14.Count > 0 || list16.Count > 0 || list17.Count > 0 || list18.Count > 0 || list19.Count > 0 || list20.Count > 0 || list21.Count > 0 || list15.Count > 0 || text4 != null || list22.Count > 0 || list23.Count > 0 || list24.Count > 0 || list25.Count > 0)
		{
			AddHeading(components, capi, "Created by", ref haveText);
			ClearFloatTextComponent clearFloatTextComponent = new ClearFloatTextComponent(capi, 7f);
			ClearFloatTextComponent item2 = new ClearFloatTextComponent(capi, 3f);
			if (text3 != null)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				RichTextComponent richTextComponent = new RichTextComponent(capi, "• ", CairoFont.WhiteSmallText());
				richTextComponent.PaddingLeft = 2.0;
				components.Add(richTextComponent);
				components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get(text3) + "\n", CairoFont.WhiteSmallText()));
			}
			if (list9.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				RichTextComponent richTextComponent2 = new RichTextComponent(capi, "• ", CairoFont.WhiteSmallText());
				richTextComponent2.PaddingLeft = 2.0;
				components.Add(richTextComponent2);
				if (flag)
				{
					components.Add(new LinkTextComponent(capi, Lang.Get("Smithing"), CairoFont.WhiteSmallText(), delegate
					{
						openDetailPageFor("craftinginfo-smithing");
					}));
				}
				if (flag && flag2)
				{
					components.Add(new RichTextComponent(capi, "/", CairoFont.WhiteSmallText())
					{
						PaddingRight = 0.0
					});
				}
				if (flag2)
				{
					components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get("metalmolding"), CairoFont.WhiteSmallText()));
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
				int num3 = 2;
				while (list9.Count > 0)
				{
					ItemStack itemStack13 = list9[0];
					list9.RemoveAt(0);
					if (itemStack13 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent = new SlideshowItemstackTextComponent(capi, itemStack13, list9, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent.ShowStackSize = true;
						slideshowItemstackTextComponent.PaddingLeft = num3;
						num3 = 0;
						components.Add(slideshowItemstackTextComponent);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list7.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Knapping", "craftinginfo-knapping");
				int num4 = 2;
				while (list7.Count > 0)
				{
					ItemStack itemStack14 = list7[0];
					list7.RemoveAt(0);
					if (itemStack14 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent2 = new SlideshowItemstackTextComponent(capi, itemStack14, list7, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent2.ShowStackSize = true;
						slideshowItemstackTextComponent2.PaddingLeft = num4;
						num4 = 0;
						components.Add(slideshowItemstackTextComponent2);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list8.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Clay forming", "craftinginfo-clayforming");
				int num5 = 2;
				while (list8.Count > 0)
				{
					ItemStack itemStack15 = list8[0];
					list8.RemoveAt(0);
					if (itemStack15 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent3 = new SlideshowItemstackTextComponent(capi, itemStack15, list8, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent3.ShowStackSize = true;
						slideshowItemstackTextComponent3.PaddingLeft = num5;
						num5 = 0;
						components.Add(slideshowItemstackTextComponent3);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (flag3)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "handbook-createdby-anvilwelding", "gamemechanicinfo-steelmaking");
				ItemstackTextComponent itemstackTextComponent = new ItemstackTextComponent(capi, new ItemStack(capi.World.GetBlock(new AssetLocation("anvilpart-base-" + stack.Collectible.Variant["metal"]))), 40.0, 0.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				itemstackTextComponent.PaddingLeft = 2.0;
				components.Add(itemstackTextComponent);
				RichTextComponent richTextComponent3 = new RichTextComponent(capi, " + ", CairoFont.WhiteMediumText());
				richTextComponent3.PaddingLeft = 0.0;
				richTextComponent3.VerticalAlign = EnumVerticalAlign.Middle;
				components.Add(richTextComponent3);
				SlideshowItemstackTextComponent slideshowItemstackTextComponent4 = new SlideshowItemstackTextComponent(capi, list26.ToArray(), 40.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				slideshowItemstackTextComponent4.PaddingLeft = 0.0;
				components.Add(slideshowItemstackTextComponent4);
				richTextComponent3 = new RichTextComponent(capi, " + ", CairoFont.WhiteMediumText());
				richTextComponent3.PaddingLeft = 0.0;
				richTextComponent3.VerticalAlign = EnumVerticalAlign.Middle;
				components.Add(richTextComponent3);
				ItemstackTextComponent itemstackTextComponent2 = new ItemstackTextComponent(capi, new ItemStack(capi.World.GetBlock(new AssetLocation("anvilpart-top-" + stack.Collectible.Variant["metal"]))), 40.0, 0.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				itemstackTextComponent2.PaddingLeft = 0.0;
				components.Add(itemstackTextComponent2);
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list13.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "handbook-createdby-carburizing", "gamemechanicinfo-steelmaking");
				int num6 = 2;
				while (list13.Count > 0)
				{
					ItemStack itemStack16 = list13[0];
					list13.RemoveAt(0);
					if (itemStack16 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent5 = new SlideshowItemstackTextComponent(capi, itemStack16, list13, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent5.ShowStackSize = true;
						slideshowItemstackTextComponent5.PaddingLeft = num6;
						num6 = 0;
						components.Add(slideshowItemstackTextComponent5);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list14.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Grinding", null);
				int num7 = 2;
				while (list14.Count > 0)
				{
					ItemStack itemStack17 = list14[0];
					list14.RemoveAt(0);
					if (itemStack17 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent6 = new SlideshowItemstackTextComponent(capi, itemStack17, list14, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent6.ShowStackSize = true;
						slideshowItemstackTextComponent6.PaddingLeft = num7;
						num7 = 0;
						components.Add(slideshowItemstackTextComponent6);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list15.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Crushing", null);
				int num8 = 2;
				while (list15.Count > 0)
				{
					ItemStack itemStack18 = list15[0];
					list15.RemoveAt(0);
					if (itemStack18 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent7 = new SlideshowItemstackTextComponent(capi, itemStack18, list15, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent7.ShowStackSize = true;
						slideshowItemstackTextComponent7.PaddingLeft = num8;
						num8 = 0;
						components.Add(slideshowItemstackTextComponent7);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list25.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "handbook-createdby-smashing", null);
				int num9 = 2;
				while (list25.Count > 0)
				{
					ItemStack itemStack19 = list25[0];
					list25.RemoveAt(0);
					if (itemStack19 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent8 = new SlideshowItemstackTextComponent(capi, itemStack19, list25, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent8.ShowStackSize = true;
						slideshowItemstackTextComponent8.PaddingLeft = num9;
						num9 = 0;
						components.Add(slideshowItemstackTextComponent8);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list16.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Curing", null);
				int num10 = 2;
				while (list16.Count > 0)
				{
					ItemStack itemStack20 = list16[0];
					list16.RemoveAt(0);
					if (itemStack20 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent9 = new SlideshowItemstackTextComponent(capi, itemStack20, list16, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent9.PaddingLeft = num10;
						num10 = 0;
						components.Add(slideshowItemstackTextComponent9);
					}
				}
			}
			if (list17.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Ripening", null);
				int num11 = 2;
				while (list17.Count > 0)
				{
					ItemStack itemStack21 = list17[0];
					list17.RemoveAt(0);
					if (itemStack21 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent10 = new SlideshowItemstackTextComponent(capi, itemStack21, list17, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent10.PaddingLeft = num11;
						num11 = 0;
						components.Add(slideshowItemstackTextComponent10);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list18.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Drying", null);
				int num12 = 2;
				while (list18.Count > 0)
				{
					ItemStack itemStack22 = list18[0];
					list18.RemoveAt(0);
					if (itemStack22 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent11 = new SlideshowItemstackTextComponent(capi, itemStack22, list18, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent11.PaddingLeft = num12;
						num12 = 0;
						components.Add(slideshowItemstackTextComponent11);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list19.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Melting", null);
				int num13 = 2;
				while (list19.Count > 0)
				{
					ItemStack itemStack23 = list19[0];
					list19.RemoveAt(0);
					if (itemStack23 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent12 = new SlideshowItemstackTextComponent(capi, itemStack23, list19, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent12.PaddingLeft = num13;
						num13 = 0;
						components.Add(slideshowItemstackTextComponent12);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list20.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "handbook-createdby-converting", null);
				int num14 = 2;
				while (list20.Count > 0)
				{
					ItemStack itemStack24 = list20[0];
					list20.RemoveAt(0);
					if (itemStack24 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent13 = new SlideshowItemstackTextComponent(capi, itemStack24, list20, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent13.PaddingLeft = num14;
						num14 = 0;
						components.Add(slideshowItemstackTextComponent13);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list21.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "handbook-createdby-perishing", null);
				int num15 = 2;
				while (list21.Count > 0)
				{
					ItemStack itemStack25 = list21[0];
					list21.RemoveAt(0);
					if (itemStack25 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent14 = new SlideshowItemstackTextComponent(capi, itemStack25, list21, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent14.PaddingLeft = num15;
						num15 = 0;
						components.Add(slideshowItemstackTextComponent14);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list10.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Cooking/Smelting/Baking", null);
				int num16 = 2;
				while (list10.Count > 0)
				{
					ItemStack itemStack26 = list10[0];
					list10.RemoveAt(0);
					if (itemStack26 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent15 = new SlideshowItemstackTextComponent(capi, itemStack26, list10, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent15.ShowStackSize = true;
						slideshowItemstackTextComponent15.PaddingLeft = num16;
						num16 = 0;
						components.Add(slideshowItemstackTextComponent15);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list11.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "handbook-createdby-bloomerysmelting", null);
				int num17 = 2;
				while (list11.Count > 0)
				{
					ItemStack itemStack27 = list11[0];
					list11.RemoveAt(0);
					if (itemStack27 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent16 = new SlideshowItemstackTextComponent(capi, itemStack27, list11, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent16.ShowStackSize = true;
						slideshowItemstackTextComponent16.PaddingLeft = num17;
						num17 = 0;
						components.Add(slideshowItemstackTextComponent16);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list12.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "handbook-createdby-kilnfiring", null);
				int num18 = 2;
				while (list12.Count > 0)
				{
					ItemStack itemStack28 = list12[0];
					list12.RemoveAt(0);
					if (itemStack28 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent17 = new SlideshowItemstackTextComponent(capi, itemStack28, list10, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent17.PaddingLeft = num18;
						num18 = 0;
						components.Add(slideshowItemstackTextComponent17);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list22.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Juicing", "block-fruitpress-ns");
				int num19 = 2;
				while (list22.Count > 0)
				{
					ItemStack itemStack29 = list22[0];
					list22.RemoveAt(0);
					if (itemStack29 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent18 = new SlideshowItemstackTextComponent(capi, itemStack29, list22, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent18.PaddingLeft = num19;
						num19 = 0;
						components.Add(slideshowItemstackTextComponent18);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list23.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "handbook-createdby-squeezing", null);
				int num20 = 2;
				while (list23.Count > 0)
				{
					ItemStack itemStack30 = list23[0];
					list23.RemoveAt(0);
					if (itemStack30 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent19 = new SlideshowItemstackTextComponent(capi, itemStack30, list23, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent19.PaddingLeft = num20;
						num20 = 0;
						components.Add(slideshowItemstackTextComponent19);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (list24.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Distillation", "craftinginfo-alcohol");
				int num21 = 2;
				while (list24.Count > 0)
				{
					ItemStack itemStack31 = list24[0];
					list24.RemoveAt(0);
					if (itemStack31 != null)
					{
						SlideshowItemstackTextComponent slideshowItemstackTextComponent20 = new SlideshowItemstackTextComponent(capi, itemStack31, list24, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						slideshowItemstackTextComponent20.PaddingLeft = num21;
						num21 = 0;
						components.Add(slideshowItemstackTextComponent20);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (text4 != null)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Baking (in oven)", null);
				CollectibleObject collectibleObject = capi.World.GetItem(new AssetLocation(text4));
				if (collectibleObject == null)
				{
					collectibleObject = capi.World.GetBlock(new AssetLocation(text4));
				}
				ItemstackTextComponent itemstackTextComponent3 = new ItemstackTextComponent(capi, new ItemStack(collectibleObject), 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				itemstackTextComponent3.PaddingLeft = 2.0;
				components.Add(itemstackTextComponent3);
			}
			if (list4.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "handbook-createdby-potcooking", null);
				foreach (CookingRecipe item4 in list4)
				{
					int num22 = 2;
					for (int num23 = 0; num23 < item4.Ingredients.Length; num23++)
					{
						CookingRecipeIngredient ingred = item4.Ingredients[num23];
						if (num23 > 0)
						{
							RichTextComponent richTextComponent4 = new RichTextComponent(capi, " + ", CairoFont.WhiteMediumText());
							richTextComponent4.VerticalAlign = EnumVerticalAlign.Middle;
							components.Add(richTextComponent4);
						}
						ItemStack[] itemstacks = ingred.ValidStacks.Select(delegate(CookingRecipeStack vs)
						{
							ItemStack itemStack32 = vs.ResolvedItemstack.Clone();
							JsonObject attributes2 = itemStack32.Collectible.Attributes;
							if (attributes2 != null && attributes2["waterTightContainerProps"].Exists)
							{
								WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(itemStack32);
								itemStack32.StackSize *= (int)((containableProps?.ItemsPerLitre ?? 1f) * ingred.PortionSizeLitres);
							}
							return itemStack32;
						}).ToArray();
						for (int num24 = 0; num24 < ingred.MinQuantity; num24++)
						{
							if (num24 > 0)
							{
								RichTextComponent richTextComponent5 = new RichTextComponent(capi, " + ", CairoFont.WhiteMediumText());
								richTextComponent5.VerticalAlign = EnumVerticalAlign.Middle;
								components.Add(richTextComponent5);
							}
							SlideshowItemstackTextComponent slideshowItemstackTextComponent21 = new SlideshowItemstackTextComponent(capi, itemstacks, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
							{
								openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
							});
							slideshowItemstackTextComponent21.ShowStackSize = true;
							slideshowItemstackTextComponent21.PaddingLeft = num22;
							components.Add(slideshowItemstackTextComponent21);
							num22 = 0;
						}
					}
					RichTextComponent richTextComponent6 = new RichTextComponent(capi, " = ", CairoFont.WhiteMediumText());
					richTextComponent6.VerticalAlign = EnumVerticalAlign.Middle;
					components.Add(richTextComponent6);
					ItemstackTextComponent itemstackTextComponent4 = new ItemstackTextComponent(capi, item4.CooksInto.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline);
					itemstackTextComponent4.ShowStacksize = true;
					components.Add(itemstackTextComponent4);
					components.Add(new ClearFloatTextComponent(capi, 10f));
				}
			}
			if (list3.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Crafting", null);
				OrderedDictionary<int, List<GridRecipe>> orderedDictionary = new OrderedDictionary<int, List<GridRecipe>>();
				ItemStack[] array6 = new ItemStack[list3.Count];
				int num25 = 0;
				foreach (GridRecipe item5 in list3)
				{
					if (!orderedDictionary.TryGetValue(item5.RecipeGroup, out var value))
					{
						value = (orderedDictionary[item5.RecipeGroup] = new List<GridRecipe>());
					}
					ItemStack[] array7 = (from gridRecipeIngredient2 in item5.resolvedIngredients
						select gridRecipeIngredient2?.ReturnedStack?.ResolvedItemstack into itemStack32
						where itemStack32 != null
						select itemStack32).ToArray();
					if (array7 != null && array7.Length != 0 && array7.Any((ItemStack rstack) => rstack.Satisfies(stack)))
					{
						value.Add(item5);
						array6[num25++] = array7.FirstOrDefault((ItemStack rstack) => rstack.Satisfies(stack));
					}
					else if (item5.CopyAttributesFrom != null && item5.Ingredients.ContainsKey(item5.CopyAttributesFrom))
					{
						GridRecipe gridRecipe = item5.Clone();
						CraftingRecipeIngredient craftingRecipeIngredient = gridRecipe.Ingredients[item5.CopyAttributesFrom];
						ITreeAttribute treeAttribute = stack.Attributes.Clone();
						treeAttribute.MergeTree(craftingRecipeIngredient.ResolvedItemstack.Attributes);
						craftingRecipeIngredient.Attributes = new JsonObject(JToken.Parse(treeAttribute.ToJsonToken()));
						gridRecipe.ResolveIngredients(capi.World);
						gridRecipe.Output.ResolvedItemstack.Attributes.MergeTree(stack.Attributes);
						value.Add(gridRecipe);
						array6[num25++] = gridRecipe.Output.ResolvedItemstack;
					}
					else if (item5.MergeAttributesFrom.Length != 0)
					{
						GridRecipe gridRecipe2 = item5.Clone();
						ITreeAttribute treeAttribute2 = stack.Attributes.Clone();
						string[] ignoredStackAttributes = item5.MergeAttributesFrom;
						foreach (string key in ignoredStackAttributes)
						{
							if (gridRecipe2.Ingredients.ContainsKey(key))
							{
								CraftingRecipeIngredient craftingRecipeIngredient2 = gridRecipe2.Ingredients[key];
								treeAttribute2.MergeTree(craftingRecipeIngredient2.ResolvedItemstack.Attributes);
							}
						}
						ignoredStackAttributes = item5.MergeAttributesFrom;
						foreach (string key2 in ignoredStackAttributes)
						{
							if (gridRecipe2.Ingredients.ContainsKey(key2))
							{
								gridRecipe2.Ingredients[key2].Attributes = new JsonObject(JToken.Parse(treeAttribute2.ToJsonToken()));
							}
						}
						gridRecipe2.ResolveIngredients(capi.World);
						gridRecipe2.Output.ResolvedItemstack.Attributes.MergeTree(stack.Attributes);
						value.Add(gridRecipe2);
						array6[num25++] = gridRecipe2.Output.ResolvedItemstack;
					}
					else if (item5.CopyAttributesFrom != null && item5.Ingredients.ContainsKey(item5.CopyAttributesFrom) && item5.MergeAttributesFrom.Length != 0)
					{
						GridRecipe gridRecipe3 = item5.Clone();
						ITreeAttribute treeAttribute3 = stack.Attributes.Clone();
						CraftingRecipeIngredient craftingRecipeIngredient3 = gridRecipe3.Ingredients[item5.CopyAttributesFrom];
						treeAttribute3.MergeTree(craftingRecipeIngredient3.ResolvedItemstack.Attributes);
						string[] ignoredStackAttributes = item5.MergeAttributesFrom;
						foreach (string key3 in ignoredStackAttributes)
						{
							if (gridRecipe3.Ingredients.ContainsKey(key3))
							{
								CraftingRecipeIngredient craftingRecipeIngredient4 = gridRecipe3.Ingredients[key3];
								treeAttribute3.MergeTree(craftingRecipeIngredient4.ResolvedItemstack.Attributes);
							}
						}
						ignoredStackAttributes = item5.MergeAttributesFrom;
						foreach (string key4 in ignoredStackAttributes)
						{
							if (gridRecipe3.Ingredients.ContainsKey(key4))
							{
								gridRecipe3.Ingredients[key4].Attributes = new JsonObject(JToken.Parse(treeAttribute3.ToJsonToken()));
							}
						}
						craftingRecipeIngredient3.Attributes = new JsonObject(JToken.Parse(treeAttribute3.ToJsonToken()));
						gridRecipe3.ResolveIngredients(capi.World);
						gridRecipe3.Output.ResolvedItemstack.Attributes.MergeTree(stack.Attributes);
						value.Add(gridRecipe3);
						array6[num25++] = gridRecipe3.Output.ResolvedItemstack;
					}
					else
					{
						value.Add(item5);
						array6[num25++] = item5.Output.ResolvedItemstack;
					}
				}
				int num26 = 0;
				foreach (KeyValuePair<int, List<GridRecipe>> item6 in orderedDictionary)
				{
					if (num26++ % 2 == 0)
					{
						components.Add(clearFloatTextComponent);
					}
					SlideshowGridRecipeTextComponent comp = new SlideshowGridRecipeTextComponent(capi, item6.Value.ToArray(), 40.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					}, allStacks);
					comp.VerticalAlign = EnumVerticalAlign.Top;
					comp.PaddingRight = 8.0;
					comp.UnscaledMarginTop = 8.0;
					comp.PaddingLeft = 4 + (1 - num26 % 2) * 20;
					components.Add(comp);
					RichTextComponent richTextComponent7 = new RichTextComponent(capi, "=", CairoFont.WhiteMediumText());
					richTextComponent7.VerticalAlign = EnumVerticalAlign.FixedOffset;
					richTextComponent7.UnscaledMarginTop = 51.0;
					richTextComponent7.PaddingRight = 5.0;
					SlideshowItemstackTextComponent slideshowItemstackTextComponent22 = new SlideshowItemstackTextComponent(capi, array6, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					slideshowItemstackTextComponent22.overrideCurrentItemStack = () => comp.CurrentVisibleRecipe.Recipe.Output.ResolvedItemstack;
					slideshowItemstackTextComponent22.VerticalAlign = EnumVerticalAlign.FixedOffset;
					slideshowItemstackTextComponent22.UnscaledMarginTop = 50.0;
					slideshowItemstackTextComponent22.ShowStackSize = true;
					SlideshowItemstackTextComponent slideshowItemstackTextComponent23 = new SlideshowItemstackTextComponent(capi, new ItemStack[1], 40.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					slideshowItemstackTextComponent23.overrideCurrentItemStack = () => (from gridRecipeIngredient2 in comp.CurrentVisibleRecipe.Recipe.resolvedIngredients
						select gridRecipeIngredient2?.ReturnedStack?.ResolvedItemstack into itemStack32
						where itemStack32 != null
						select itemStack32).FirstOrDefault();
					slideshowItemstackTextComponent23.VerticalAlign = EnumVerticalAlign.FixedOffset;
					slideshowItemstackTextComponent23.PaddingLeft = -40.0;
					slideshowItemstackTextComponent23.UnscaledMarginTop = 108.0;
					slideshowItemstackTextComponent23.ShowStackSize = true;
					components.Add(richTextComponent7);
					components.Add(slideshowItemstackTextComponent22);
					components.Add(slideshowItemstackTextComponent23);
				}
				components.Add(new ClearFloatTextComponent(capi, 3f));
			}
			if (list27.Count > 0)
			{
				components.Add(item2);
				item2 = clearFloatTextComponent;
				AddSubHeading(components, capi, openDetailPageFor, "Mixing (in Barrel)", null);
				components.AddRange(list27);
			}
		}
		return haveText;
	}

	public static void AddHeading(List<RichTextComponentBase> components, ICoreClientAPI capi, string heading, ref bool haveText)
	{
		if (haveText)
		{
			components.Add(new ClearFloatTextComponent(capi, 14f));
		}
		haveText = true;
		RichTextComponent item = new RichTextComponent(capi, Lang.Get(heading) + "\n", CairoFont.WhiteSmallText().WithWeight((FontWeight)1));
		components.Add(item);
	}

	public static void AddSubHeading(List<RichTextComponentBase> components, ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor, string subheading, string detailpage)
	{
		if (detailpage == null)
		{
			RichTextComponent richTextComponent = new RichTextComponent(capi, "• " + Lang.Get(subheading) + "\n", CairoFont.WhiteSmallText());
			richTextComponent.PaddingLeft = 2.0;
			components.Add(richTextComponent);
			return;
		}
		RichTextComponent richTextComponent2 = new RichTextComponent(capi, "• ", CairoFont.WhiteSmallText());
		richTextComponent2.PaddingLeft = 2.0;
		components.Add(richTextComponent2);
		components.Add(new LinkTextComponent(capi, Lang.Get(subheading) + "\n", CairoFont.WhiteSmallText(), delegate
		{
			openDetailPageFor(detailpage);
		}));
	}

	protected void addExtraSections(ICoreClientAPI capi, ItemStack stack, List<RichTextComponentBase> components, float marginTop)
	{
		if (ExtraHandBookSections != null)
		{
			bool haveText = true;
			for (int i = 0; i < ExtraHandBookSections.Length; i++)
			{
				ExtraHandbookSection extraHandbookSection = ExtraHandBookSections[i];
				AddHeading(components, capi, extraHandbookSection.Title, ref haveText);
				components.Add(new ClearFloatTextComponent(capi, 2f));
				RichTextComponent richTextComponent = new RichTextComponent(capi, "", CairoFont.WhiteSmallText());
				richTextComponent.PaddingLeft = 2.0;
				components.Add(richTextComponent);
				if (extraHandbookSection.TextParts != null)
				{
					components.AddRange(VtmlUtil.Richtextify(capi, string.Join(", ", extraHandbookSection.TextParts) + "\n", CairoFont.WhiteSmallText()));
				}
				else
				{
					components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get(extraHandbookSection.Text) + "\n", CairoFont.WhiteSmallText()));
				}
			}
		}
		string text = collObj.Code.Domain + ":" + stack.Class.Name();
		string text2 = collObj.Code.ToShortString();
		string matchingIfExists = Lang.GetMatchingIfExists(text + "-handbooktitle-" + text2);
		string matchingIfExists2 = Lang.GetMatchingIfExists(text + "-handbooktext-" + text2);
		if (matchingIfExists != null || matchingIfExists2 != null)
		{
			components.Add(new ClearFloatTextComponent(capi, 14f));
			if (matchingIfExists != null)
			{
				components.Add(new RichTextComponent(capi, matchingIfExists + "\n", CairoFont.WhiteSmallText().WithWeight((FontWeight)1)));
				components.Add(new ClearFloatTextComponent(capi, 2f));
			}
			if (matchingIfExists2 != null)
			{
				RichTextComponent richTextComponent2 = new RichTextComponent(capi, "", CairoFont.WhiteSmallText());
				richTextComponent2.PaddingLeft = 2.0;
				components.Add(richTextComponent2);
				components.AddRange(VtmlUtil.Richtextify(capi, matchingIfExists2 + "\n", CairoFont.WhiteSmallText()));
			}
		}
	}

	protected List<RichTextComponentBase> BuildBarrelRecipesText(ICoreClientAPI capi, ItemStack stack, ActionConsumable<string> openDetailPageFor)
	{
		List<RichTextComponentBase> list = new List<RichTextComponentBase>();
		List<BarrelRecipe> barrelRecipes = capi.GetBarrelRecipes();
		if (barrelRecipes.Count == 0)
		{
			return list;
		}
		Dictionary<string, List<BarrelRecipe>> dictionary = new Dictionary<string, List<BarrelRecipe>>();
		foreach (BarrelRecipe item in barrelRecipes)
		{
			ItemStack resolvedItemstack = item.Output.ResolvedItemstack;
			if (resolvedItemstack != null && resolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				if (!dictionary.TryGetValue(item.Code, out var value))
				{
					value = (dictionary[item.Code] = new List<BarrelRecipe>());
				}
				value.Add(item);
			}
		}
		foreach (List<BarrelRecipe> value2 in dictionary.Values)
		{
			int num = value2[0].Ingredients.Length;
			ItemStack[][] array = new ItemStack[num][];
			ItemStack[] array2 = new ItemStack[value2.Count];
			double num2 = 0.0;
			for (int i = 0; i < value2.Count; i++)
			{
				if (value2[i].Ingredients.Length != num)
				{
					throw new Exception("Barrel recipe with same name but different ingredient count! Sorry, this is not supported right now. Please make sure you choose different barrel recipe names if you have different ingredient counts.");
				}
				for (int j = 0; j < num; j++)
				{
					if (i == 0)
					{
						array[j] = new ItemStack[value2.Count];
					}
					array[j][i] = value2[i].Ingredients[j].ResolvedItemstack;
				}
				num2 = value2[i].SealHours;
				array2[i] = value2[i].Output.ResolvedItemstack;
			}
			int num3 = 2;
			for (int k = 0; k < num; k++)
			{
				if (k > 0)
				{
					RichTextComponent richTextComponent = new RichTextComponent(capi, " + ", CairoFont.WhiteMediumText());
					richTextComponent.VerticalAlign = EnumVerticalAlign.Middle;
					list.Add(richTextComponent);
				}
				SlideshowItemstackTextComponent slideshowItemstackTextComponent = new SlideshowItemstackTextComponent(capi, array[k], 40.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				slideshowItemstackTextComponent.ShowStackSize = true;
				slideshowItemstackTextComponent.PaddingLeft = num3;
				num3 = 0;
				list.Add(slideshowItemstackTextComponent);
			}
			RichTextComponent richTextComponent2 = new RichTextComponent(capi, " = ", CairoFont.WhiteMediumText());
			richTextComponent2.VerticalAlign = EnumVerticalAlign.Middle;
			list.Add(richTextComponent2);
			SlideshowItemstackTextComponent slideshowItemstackTextComponent2 = new SlideshowItemstackTextComponent(capi, array2, 40.0, EnumFloat.Inline);
			slideshowItemstackTextComponent2.ShowStackSize = true;
			list.Add(slideshowItemstackTextComponent2);
			string displayText = ((num2 > (double)capi.World.Calendar.HoursPerDay) ? (" " + Lang.Get("{0} days", Math.Round(num2 / (double)capi.World.Calendar.HoursPerDay, 1))) : Lang.Get("{0} hours", Math.Round(num2)));
			RichTextComponent richTextComponent3 = new RichTextComponent(capi, displayText, CairoFont.WhiteSmallText());
			richTextComponent3.VerticalAlign = EnumVerticalAlign.Middle;
			list.Add(richTextComponent3);
			list.Add(new ClearFloatTextComponent(capi, 10f));
		}
		return list;
	}

	protected void addStorableInfo(ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop)
	{
		List<ItemStack> list = new List<ItemStack>();
		List<ItemStack> list2 = new List<ItemStack>();
		List<ItemStack> list3 = new List<ItemStack>();
		bool flag = stack.Collectible.HasBehavior<CollectibleBehaviorGroundStorable>();
		foreach (ItemStack val in allStacks)
		{
			if (!(val.Collectible is BlockShelf) || !BlockEntityShelf.GetShelvableLayout(stack).HasValue)
			{
				if (!(val.Collectible is BlockToolRack))
				{
					goto IL_00e8;
				}
				if (!stack.Collectible.Tool.HasValue)
				{
					JsonObject itemAttributes = stack.ItemAttributes;
					if (itemAttributes == null || !itemAttributes["rackable"].AsBool())
					{
						goto IL_00e8;
					}
				}
			}
			if (!list3.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
			{
				list3.Add(val);
			}
			goto IL_00e8;
			IL_00e8:
			JsonObject itemAttributes2 = stack.ItemAttributes;
			if (itemAttributes2 != null)
			{
				if (((val.Collectible is BlockMoldRack && itemAttributes2["moldrackable"].AsBool()) || (val.Collectible is BlockBookshelf && itemAttributes2["bookshelveable"].AsBool()) || (val.Collectible is BlockScrollRack && itemAttributes2["scrollrackable"].AsBool()) || (itemAttributes2["displaycaseable"].AsBool() && (val.Collectible as BlockDisplayCase)?.height >= itemAttributes2["displaycase"]["minHeight"].AsFloat(0.25f)) || (val.Collectible is BlockAntlerMount && itemAttributes2["antlerMountable"].AsBool())) && !list3.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
				{
					list3.Add(val);
				}
				ILiquidInterface collectibleInterface = val.Collectible.GetCollectibleInterface<ILiquidInterface>();
				if (collectibleInterface != null && collectibleInterface.GetCurrentLitres(val) <= 0f && itemAttributes2["waterTightContainerProps"].Exists && !list2.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
				{
					list2.Add(val);
				}
				if (val.Collectible is BlockCrock && itemAttributes2["crockable"].AsBool() && !list.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
				{
					list.Add(val);
				}
			}
		}
		if (!(list.Count > 0 || list3.Count > 0 || list2.Count > 0 || flag))
		{
			return;
		}
		ClearFloatTextComponent clearFloatTextComponent = new ClearFloatTextComponent(capi, 7f);
		ClearFloatTextComponent item = new ClearFloatTextComponent(capi, 3f);
		bool haveText = components.Count > 0;
		components.Add(item);
		item = clearFloatTextComponent;
		AddHeading(components, capi, "Storable in/on", ref haveText);
		if (flag)
		{
			components.Add(item);
			item = clearFloatTextComponent;
			RichTextComponent richTextComponent = new RichTextComponent(capi, "", CairoFont.WhiteSmallText());
			richTextComponent.PaddingLeft = 2.0;
			components.Add(richTextComponent);
			components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get("handbook-storable-ground") + "\n", CairoFont.WhiteSmallText()));
		}
		if (list3.Count > 0)
		{
			components.Add(item);
			item = clearFloatTextComponent;
			AddSubHeading(components, capi, openDetailPageFor, "handbook-storable-displaycontainers", null);
			int num2 = 2;
			while (list3.Count > 0)
			{
				ItemStack itemStack = list3[0];
				list3.RemoveAt(0);
				if (itemStack != null)
				{
					SlideshowItemstackTextComponent slideshowItemstackTextComponent = new SlideshowItemstackTextComponent(capi, itemStack, list3, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					slideshowItemstackTextComponent.PaddingLeft = num2;
					num2 = 0;
					components.Add(slideshowItemstackTextComponent);
				}
			}
			components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
		}
		if (list2.Count > 0)
		{
			components.Add(item);
			item = clearFloatTextComponent;
			AddSubHeading(components, capi, openDetailPageFor, "handbook-storable-liquidcontainers", null);
			int num3 = 2;
			while (list2.Count > 0)
			{
				ItemStack itemStack2 = list2[0];
				list2.RemoveAt(0);
				if (itemStack2 != null)
				{
					SlideshowItemstackTextComponent slideshowItemstackTextComponent2 = new SlideshowItemstackTextComponent(capi, itemStack2, list2, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					slideshowItemstackTextComponent2.PaddingLeft = num3;
					num3 = 0;
					components.Add(slideshowItemstackTextComponent2);
				}
			}
			components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
		}
		if (list.Count <= 0)
		{
			return;
		}
		components.Add(item);
		item = clearFloatTextComponent;
		AddSubHeading(components, capi, openDetailPageFor, "handbook-storable-foodcontainers", null);
		int num4 = 2;
		while (list.Count > 0)
		{
			ItemStack itemStack3 = list[0];
			list.RemoveAt(0);
			if (itemStack3 != null)
			{
				SlideshowItemstackTextComponent slideshowItemstackTextComponent3 = new SlideshowItemstackTextComponent(capi, itemStack3, list, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				slideshowItemstackTextComponent3.PaddingLeft = num4;
				num4 = 0;
				components.Add(slideshowItemstackTextComponent3);
			}
		}
		components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
	}

	protected void addStoredInInfo(ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop)
	{
		List<ItemStack> list = new List<ItemStack>();
		foreach (ItemStack val in allStacks)
		{
			if (!(stack.Collectible is BlockShelf) || !BlockEntityShelf.GetShelvableLayout(val).HasValue)
			{
				if (!(stack.Collectible is BlockToolRack))
				{
					goto IL_00ce;
				}
				if (!val.Collectible.Tool.HasValue)
				{
					JsonObject itemAttributes = val.ItemAttributes;
					if (itemAttributes == null || !itemAttributes["rackable"].AsBool())
					{
						goto IL_00ce;
					}
				}
			}
			if (!list.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
			{
				list.Add(val);
			}
			goto IL_00ce;
			IL_0235:
			if (!list.Any((ItemStack s) => s.Equals(capi.World, val, GlobalConstants.IgnoredStackAttributes)))
			{
				list.Add(val);
			}
			continue;
			IL_00ce:
			JsonObject itemAttributes2 = val.ItemAttributes;
			if (itemAttributes2 == null)
			{
				continue;
			}
			if ((!(stack.Collectible is BlockMoldRack) || !itemAttributes2["moldrackable"].AsBool()) && (!(stack.Collectible is BlockBookshelf) || !itemAttributes2["bookshelveable"].AsBool()) && (!(stack.Collectible is BlockScrollRack) || !itemAttributes2["scrollrackable"].AsBool()) && (!itemAttributes2["displaycaseable"].AsBool() || !((stack.Collectible as BlockDisplayCase)?.height >= itemAttributes2["displaycase"]["minHeight"].AsFloat(0.25f))) && (!(stack.Collectible is BlockAntlerMount) || !itemAttributes2["antlerMountable"].AsBool()))
			{
				if (stack.Collectible.GetCollectibleInterface<ILiquidInterface>() != null)
				{
					WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(val);
					if (containableProps != null && containableProps.WhenFilled == null)
					{
						goto IL_0235;
					}
				}
				if (!(stack.Collectible is BlockCrock) || !itemAttributes2["crockable"].AsBool())
				{
					continue;
				}
			}
			goto IL_0235;
		}
		if (list.Count <= 0)
		{
			return;
		}
		bool haveText = components.Count > 0;
		components.Add(new ClearFloatTextComponent(capi, 7f));
		AddHeading(components, capi, "handbook-storedin", ref haveText);
		int num = 2;
		while (list.Count > 0)
		{
			ItemStack itemStack = list[0];
			list.RemoveAt(0);
			if (itemStack == null)
			{
				continue;
			}
			if (itemStack.Collectible is BlockPie)
			{
				List<CookingRecipe> list2 = BlockPie.GetHandbookRecipes(capi, allStacks).ToList();
				while (list2.Count > 0)
				{
					CookingRecipe recipe = list2[0];
					list2.RemoveAt(0);
					if (recipe != null)
					{
						ItemStack itemStack2 = itemStack.Clone();
						itemStack2.Attributes.SetInt("pieSize", 4);
						itemStack2.Attributes.SetString("topCrustType", BlockPie.TopCrustTypes[capi.World.Rand.Next(BlockPie.TopCrustTypes.Length)].Code);
						itemStack2.Attributes.SetInt("bakeLevel", 2);
						Dictionary<CookingRecipeIngredient, HashSet<ItemStack>> valueOrDefault = cachedValidStacks.GetValueOrDefault(recipe.Code);
						MealstackTextComponent item = new MealstackTextComponent(capi, ref valueOrDefault, itemStack2, recipe, 40.0, EnumFloat.Inline, allStacks, delegate
						{
							openDetailPageFor("handbook-mealrecipe-" + recipe.Code + "-pie");
						}, 6, isPie: true);
						cachedValidStacks[recipe.Code] = valueOrDefault;
						components.Add(item);
					}
				}
			}
			else
			{
				SlideshowItemstackTextComponent slideshowItemstackTextComponent = new SlideshowItemstackTextComponent(capi, itemStack, list, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				slideshowItemstackTextComponent.PaddingLeft = num;
				num = 0;
				components.Add(slideshowItemstackTextComponent);
			}
		}
		components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		JsonObject attributes = collObj.Attributes;
		if (attributes != null && attributes["pigment"]?["color"].Exists == true)
		{
			dsc.AppendLine(Lang.Get("Pigment: {0}", Lang.Get(collObj.Attributes["pigment"]["name"].AsString())));
		}
		JsonObject jsonObject = collObj.Attributes?["fertilizerProps"];
		if (jsonObject != null && jsonObject.Exists)
		{
			FertilizerProps fertilizerProps = jsonObject.AsObject<FertilizerProps>();
			if (fertilizerProps != null)
			{
				dsc.AppendLine(Lang.Get("Fertilizer: {0}% N, {1}% P, {2}% K", fertilizerProps.N, fertilizerProps.P, fertilizerProps.K));
			}
		}
		JuiceableProperties juiceableProperties = getjuiceableProps(inSlot.Itemstack);
		if (juiceableProperties != null)
		{
			float num = (juiceableProperties.LitresPerItem.HasValue ? (juiceableProperties.LitresPerItem.Value * (float)inSlot.Itemstack.StackSize) : ((float)inSlot.Itemstack.Attributes.GetDecimal("juiceableLitresLeft")));
			if ((double)num > 0.01)
			{
				dsc.AppendLine(Lang.Get("collectibleinfo-juicingproperties", num, juiceableProperties.LiquidStack.ResolvedItemstack.GetName()));
			}
		}
	}

	public JuiceableProperties getjuiceableProps(ItemStack stack)
	{
		JuiceableProperties obj = stack?.ItemAttributes?["juiceableProperties"]?.AsObject<JuiceableProperties>(null, stack.Collectible.Code.Domain);
		obj?.LiquidStack?.Resolve(Api.World, "juiceable properties liquidstack");
		obj?.PressedStack?.Resolve(Api.World, "juiceable properties pressedstack");
		if (obj != null)
		{
			JsonItemStack returnStack = obj.ReturnStack;
			if (returnStack != null)
			{
				returnStack.Resolve(Api.World, "juiceable properties returnstack");
				return obj;
			}
			return obj;
		}
		return obj;
	}

	public DistillationProps getDistillationProps(ItemStack stack)
	{
		DistillationProps obj = stack?.ItemAttributes?["distillationProps"]?.AsObject<DistillationProps>(null, stack.Collectible.Code.Domain);
		if (obj != null)
		{
			JsonItemStack distilledStack = obj.DistilledStack;
			if (distilledStack != null)
			{
				distilledStack.Resolve(Api.World, "distillation props distilled stack");
				return obj;
			}
			return obj;
		}
		return obj;
	}

	private bool getIsAnvilWorkable(CollectibleObject obj, List<ItemStack> fuels)
	{
		if (obj.GetCollectibleInterface<IAnvilWorkable>() != null)
		{
			if (obj.CombustibleProps == null)
			{
				JsonObject attributes = obj.Attributes;
				if (attributes == null || !attributes["workableTemperature"].Exists)
				{
					goto IL_0031;
				}
			}
			float num = (from fuel in fuels
				orderby fuel.Collectible.CombustibleProps.BurnTemperature
				select fuel.Collectible.CombustibleProps.BurnTemperature).LastOrDefault();
			CombustibleProperties combustibleProps = obj.CombustibleProps;
			float num2 = ((combustibleProps != null) ? (combustibleProps.MeltingPoint / 2) : 0);
			return (obj.Attributes?["workableTemperature"]?.AsFloat(num2) ?? num2) <= num;
		}
		goto IL_0031;
		IL_0031:
		return false;
	}

	private bool getCanBloomerySmelt(ItemStack stack)
	{
		if (stack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack == null)
		{
			return false;
		}
		int num = stack.Collectible.CombustibleProps?.MeltingPoint ?? 0;
		if (num < 1500)
		{
			return num >= 1000;
		}
		return false;
	}

	private bool getCanContainerSmelt(ICoreClientAPI capi, List<ItemStack> containers, List<ItemStack> fuels, ItemStack stack)
	{
		int num = (from fuel in fuels
			orderby fuel.Collectible.CombustibleProps.BurnTemperature
			select fuel.Collectible.CombustibleProps.BurnTemperature).LastOrDefault();
		CombustibleProperties combustibleProps = stack.Collectible.CombustibleProps;
		if (combustibleProps == null || combustibleProps.MeltingPoint > num)
		{
			return false;
		}
		ItemStack[] source = containers.Where((ItemStack container) => container.ItemAttributes["maxContentDimensions"].AsObject<Size3f>()?.CanContain(stack.Collectible.Dimensions) ?? true).ToArray();
		ItemStack itemStack = stack.Clone();
		itemStack.StackSize = combustibleProps.SmeltedRatio;
		dummySmeltingInv.Slots[1].Itemstack = itemStack;
		return source.Any((ItemStack container) => container.Collectible.CanSmelt(capi.World, dummySmeltingInv, container, null));
	}

	private bool getCanSmelt(List<ItemStack> fuels, ItemStack stack)
	{
		int num = (from fuel in fuels
			orderby fuel.Collectible.CombustibleProps.BurnTemperature
			select fuel.Collectible.CombustibleProps.BurnTemperature).LastOrDefault();
		CombustibleProperties combustibleProps = stack.Collectible.CombustibleProps;
		if (combustibleProps == null || combustibleProps.MeltingPoint > num)
		{
			return false;
		}
		if (!combustibleProps.RequiresContainer)
		{
			return true;
		}
		return false;
	}
}
