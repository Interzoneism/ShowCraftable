using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiHandbookMealRecipePage : GuiHandbookPage
{
	public CookingRecipe Recipe;

	public string pageCode;

	public string Title;

	private readonly string titleCached;

	protected float secondsVisible;

	protected const int TinyPadding = 2;

	protected const int TinyIndent = 2;

	protected const int MarginBottom = 3;

	protected const int SmallPadding = 7;

	protected const int MediumPadding = 14;

	public LoadedTexture? Texture;

	public InventoryBase unspoilableInventory;

	public DummySlot dummySlot;

	private HandbookMealNutritionFacts? cachedNutritionFacts;

	private Dictionary<string, List<ItemStack>> cachedIngredientStacks;

	private Dictionary<CookingRecipeIngredient, HashSet<ItemStack?>>? cachedValidStacks;

	private bool isPie;

	private int slots;

	private ItemStack[] allStacks;

	private ElementBounds? scissorBounds;

	public override string PageCode => pageCode;

	public override string CategoryCode => "stack";

	public override bool IsDuplicate => false;

	public GuiHandbookMealRecipePage(ICoreClientAPI capi, CookingRecipe recipe, ItemStack[] allstacks, int slots = 4, bool isPie = false)
	{
		Recipe = recipe;
		pageCode = "handbook-mealrecipe-" + recipe.Code + (isPie ? "-pie" : "");
		this.isPie = isPie;
		this.slots = slots;
		allStacks = allstacks;
		unspoilableInventory = new CreativeInventoryTab(1, "not-used", null);
		if (isPie)
		{
			dummySlot = new DummySlot(new ItemStack(capi.World.BlockAccessor.GetBlock("pie-perfect")), unspoilableInventory);
			dummySlot.Itemstack.Attributes.SetInt("pieSize", 4);
			dummySlot.Itemstack.Attributes.SetString("topCrustType", BlockPie.TopCrustTypes[capi.World.Rand.Next(BlockPie.TopCrustTypes.Length)].Code);
			dummySlot.Itemstack.Attributes.SetInt("bakeLevel", 2);
		}
		else
		{
			dummySlot = new DummySlot(new ItemStack(BlockMeal.RandomMealBowl(capi)), unspoilableInventory);
		}
		Title = Lang.Get(isPie ? ("pie-" + recipe.Code + "-perfect") : ("mealrecipe-name-" + recipe.Code));
		titleCached = Lang.Get(Title).ToSearchFriendly();
		cachedIngredientStacks = new Dictionary<string, List<ItemStack>>();
		cachedValidStacks = null;
	}

	[MemberNotNull(new string[] { "Texture", "scissorBounds" })]
	public void Recompose(ICoreClientAPI capi)
	{
		Texture?.Dispose();
		Texture = new TextTextureUtil(capi).GenTextTexture(Title, CairoFont.WhiteSmallText());
		scissorBounds = ElementBounds.FixedSize(50.0, 50.0);
		scissorBounds.ParentBounds = capi.Gui.WindowBounds;
	}

	public override void RenderListEntryTo(ICoreClientAPI capi, float dt, double x, double y, double cellWidth, double cellHeight)
	{
		float num = (float)GuiElement.scaled(25.0);
		float num2 = (float)GuiElement.scaled(10.0);
		IBlockMealContainer blockMealContainer = dummySlot.Itemstack?.Collectible as IBlockMealContainer;
		if ((secondsVisible -= dt) <= 0f)
		{
			secondsVisible = 1f;
			if (isPie)
			{
				dummySlot.Itemstack?.Attributes.SetString("topCrustType", BlockPie.TopCrustTypes[capi.World.Rand.Next(BlockPie.TopCrustTypes.Length)].Code);
			}
			else
			{
				dummySlot.Itemstack = new ItemStack(BlockMeal.RandomMealBowl(capi));
			}
			blockMealContainer?.SetContents(Recipe.Code, dummySlot.Itemstack, isPie ? BlockPie.GenerateRandomPie(capi, ref cachedValidStacks, Recipe) : Recipe.GenerateRandomMeal(capi, ref cachedValidStacks, allStacks, slots));
		}
		if (Texture == null || scissorBounds == null)
		{
			Recompose(capi);
		}
		scissorBounds.fixedX = ((double)num2 + x - (double)(num / 2f)) / (double)RuntimeEnv.GUIScale;
		scissorBounds.fixedY = (y - (double)(num / 2f)) / (double)RuntimeEnv.GUIScale;
		scissorBounds.CalcWorldBounds();
		if (!(scissorBounds.InnerWidth <= 0.0) && !(scissorBounds.InnerHeight <= 0.0))
		{
			capi.Render.PushScissor(scissorBounds, stacking: true);
			capi.Render.RenderItemstackToGui(dummySlot, x + (double)num2 + (double)(num / 2f), y + (double)(num / 2f), 100.0, num, -1, shading: true, rotate: false, showStackSize: false);
			capi.Render.PopScissor();
			capi.Render.Render2DTexturePremultipliedAlpha(Texture.TextureId, x + (double)num + GuiElement.scaled(25.0), y + (double)(num / 4f) - GuiElement.scaled(3.0), Texture.Width, Texture.Height);
		}
	}

	public override void Dispose()
	{
		Texture?.Dispose();
		Texture = null;
	}

	public override void ComposePage(GuiComposer detailViewGui, ElementBounds textBounds, ItemStack[] allstacks, ActionConsumable<string> openDetailPageFor)
	{
		RichTextComponentBase[] pageText = GetPageText(detailViewGui.Api, allstacks, openDetailPageFor);
		detailViewGui.AddRichtext(pageText, textBounds, "richtext");
	}

	protected virtual RichTextComponentBase[] GetPageText(ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor)
	{
		List<RichTextComponentBase> list = new List<RichTextComponentBase>();
		addGeneralInfo(capi, allStacks, list);
		addIngredientLists(capi, allStacks, list, openDetailPageFor);
		addCookingDirections(capi, list);
		addRecipeNotes(capi, list);
		return list.ToArray();
	}

	protected void addGeneralInfo(ICoreClientAPI capi, ItemStack[] allStacks, List<RichTextComponentBase> components)
	{
		ItemStack mealBlock = dummySlot.Itemstack.Clone();
		components.Add(new MealstackTextComponent(capi, ref cachedValidStacks, mealBlock, Recipe, 100.0, EnumFloat.Left, allStacks, null, slots, isPie)
		{
			PaddingRight = GuiElement.scaled(10.0),
			offX = -8.0
		});
		components.AddRange(VtmlUtil.Richtextify(capi, Title + "\n", CairoFont.WhiteSmallishText()));
		if (capi.Settings.Bool["extendedDebugInfo"])
		{
			CairoFont cairoFont = CairoFont.WhiteDetailText();
			cairoFont.Color[3] = 0.5;
			components.AddRange(VtmlUtil.Richtextify(capi, "Page code:" + pageCode + "\n", cairoFont));
		}
		HandbookMealNutritionFacts nutritionFacts = getNutritionFacts(capi, allStacks, slots);
		float num = (float)Math.Round(nutritionFacts.MinSatiety);
		float num2 = (float)Math.Round(nutritionFacts.MaxSatiety);
		float minHealth = nutritionFacts.MinHealth;
		float maxHealth = nutritionFacts.MaxHealth;
		string text = "";
		if (nutritionFacts.Categories.Count > 0)
		{
			text = Lang.Get("handbook-mealrecipe-nutritionfacts-satietycategories", string.Join(", ", nutritionFacts.Categories.Select((EnumFoodCategory category) => Lang.Get("foodcategory-" + category.ToString().ToLowerInvariant()))));
		}
		bool num3 = num != 0f || num2 != 0f;
		bool flag = minHealth > 0f || maxHealth > 0f;
		if (num3 || flag)
		{
			components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get("handbook-mealrecipe-nutritionfacts") + "\n", CairoFont.WhiteSmallText()));
		}
		if (num3)
		{
			components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get("handbook-mealrecipe-nutritionfactsline-satiety", num, num2, text) + "\n", CairoFont.WhiteDetailText()));
		}
		if (flag)
		{
			components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get("handbook-mealrecipe-nutritionfactsline-health", ((minHealth >= 0f) ? "+" : "") + minHealth, ((maxHealth >= 0f) ? "+" : "") + maxHealth) + "\n", CairoFont.WhiteDetailText()));
		}
	}

	private void addIngredientLists(ICoreClientAPI capi, ItemStack[] allStacks, List<RichTextComponentBase> components, ActionConsumable<string> openDetailPageFor)
	{
		List<CookingRecipeIngredient> source = (isPie ? Recipe.Ingredients.ToList() : CombinedIngredientsList());
		bool haveText = components.Count > 0;
		CollectibleBehaviorHandbookTextAndExtraInfo.AddHeading(components, capi, "handbook-mealrecipe-requiredingredients", ref haveText);
		foreach (CookingRecipeIngredient item in source.Where((CookingRecipeIngredient ingredient) => ingredient.MinQuantity > 0))
		{
			List<ItemStack> ingredientStacks = getIngredientStacks(capi, item, allStacks);
			if (ingredientStacks.Count > 0)
			{
				addIngredientHeading(components, capi, item, "handbook-mealrecipe" + (isPie ? "-pie" : "-") + "ingredients", ingredientStacks, openDetailPageFor);
			}
		}
		IEnumerable<CookingRecipeIngredient> enumerable = source.Where((CookingRecipeIngredient ingredient) => ingredient.MinQuantity == 0);
		if (enumerable.Count() > 0)
		{
			haveText = components.Count > 0;
			CollectibleBehaviorHandbookTextAndExtraInfo.AddHeading(components, capi, "handbook-mealrecipe-optionalingredients", ref haveText);
		}
		foreach (CookingRecipeIngredient item2 in enumerable)
		{
			List<ItemStack> ingredientStacks2 = getIngredientStacks(capi, item2, allStacks);
			if (ingredientStacks2.Count > 0)
			{
				addIngredientHeading(components, capi, item2, "handbook-mealrecipe" + (isPie ? "-pie" : "-") + "ingredients", ingredientStacks2, openDetailPageFor);
			}
		}
	}

	private void addIngredientHeading(List<RichTextComponentBase> components, ICoreClientAPI capi, CookingRecipeIngredient ingredient, string ingredientHeader, List<ItemStack> ingredientStacks, ActionConsumable<string> openDetailPageFor)
	{
		ClearFloatTextComponent item = new ClearFloatTextComponent(capi, 3f);
		int num = ingredient.MinQuantity;
		int maxQuantity = ingredient.MaxQuantity;
		if (maxQuantity == num)
		{
			num = 0;
		}
		ingredientHeader = Lang.Get(ingredientHeader, num, maxQuantity, Lang.Get("handbook-mealingredient-" + ingredient.TypeName));
		components.Add(item);
		CollectibleBehaviorHandbookTextAndExtraInfo.AddSubHeading(components, capi, null, ingredientHeader, null);
		int num2 = 2;
		while (ingredientStacks.Count > 0)
		{
			ItemStack itemStack = ingredientStacks[0];
			ingredientStacks.RemoveAt(0);
			if (itemStack != null)
			{
				SlideshowItemstackTextComponent slideshowItemstackTextComponent = new SlideshowItemstackTextComponent(capi, itemStack, ingredientStacks, 30.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				slideshowItemstackTextComponent.PaddingLeft = num2;
				slideshowItemstackTextComponent.ShowStackSize = true;
				num2 = 0;
				components.Add(slideshowItemstackTextComponent);
			}
		}
		components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
	}

	protected void addCookingDirections(ICoreClientAPI capi, List<RichTextComponentBase> components)
	{
		string text = Lang.GetMatchingIfExists("handbook-mealrecipe-directionstext-" + Recipe.Code);
		if (isPie)
		{
			if (text == null)
			{
				text = Lang.GetMatchingIfExists("handbook-mealrecipe-directionstext-" + Recipe.Code + "-pie");
			}
			if (text == null)
			{
				text = Lang.GetMatchingIfExists("handbook-mealrecipe-directionstext-pie");
			}
		}
		if (text == null)
		{
			text = Lang.Get("handbook-mealrecipe-directionstext");
		}
		components.Add(new ClearFloatTextComponent(capi, 14f));
		components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get("handbook-mealrecipe" + (isPie ? "-pie" : "-") + "directions", text) + "\n", CairoFont.WhiteSmallText()));
	}

	protected void addRecipeNotes(ICoreClientAPI capi, List<RichTextComponentBase> components)
	{
		string matchingIfExists = Lang.GetMatchingIfExists("handbook-mealrecipe-notestext-" + Recipe.Code);
		if (isPie)
		{
			if (matchingIfExists == null)
			{
				matchingIfExists = Lang.GetMatchingIfExists("handbook-mealrecipe-notestext-" + Recipe.Code + "-pie");
			}
			if (matchingIfExists == null)
			{
				matchingIfExists = Lang.GetMatchingIfExists("handbook-mealrecipe-notestext-pie");
			}
		}
		if (matchingIfExists != null)
		{
			components.Add(new ClearFloatTextComponent(capi, 14f));
			components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get("handbook-mealrecipe-notes", matchingIfExists), CairoFont.WhiteSmallText()));
		}
	}

	private HandbookMealNutritionFacts getNutritionFacts(ICoreClientAPI capi, ItemStack[] allStacks, int slots = 4)
	{
		if (cachedNutritionFacts == null)
		{
			float num2;
			float num3;
			float num4;
			float num = (num2 = (num3 = (num4 = 0f)));
			List<IngredientMinMax> list = new List<IngredientMinMax>();
			HashSet<EnumFoodCategory> hashSet = new HashSet<EnumFoodCategory>();
			List<CookingRecipeIngredient> list2 = new List<CookingRecipeIngredient>();
			foreach (CookingRecipeIngredient item in isPie ? Recipe.Ingredients.ToList() : CombinedIngredientsList())
			{
				CookingRecipeIngredient cookingRecipeIngredient = item.Clone();
				List<ItemStack> ingredientStacks = getIngredientStacks(capi, cookingRecipeIngredient, allStacks);
				float num6;
				float num5 = (num6 = float.MaxValue);
				float num8;
				float num7 = (num8 = float.MinValue);
				bool flag = true;
				while (ingredientStacks.Count > 0)
				{
					ItemStack itemStack = ingredientStacks[0];
					ingredientStacks.RemoveAt(0);
					if (itemStack == null)
					{
						continue;
					}
					float num9 = itemStack.StackSize;
					itemStack = Recipe.GetIngrendientFor(itemStack, list2.ToArray())?.GetMatchingStack(itemStack)?.CookedStack?.ResolvedItemstack.Clone() ?? itemStack;
					itemStack.StackSize = (int)num9;
					FoodNutritionProperties ingredientStackNutritionProperties = BlockMeal.GetIngredientStackNutritionProperties(capi.World, itemStack, capi.World.Player.Entity);
					WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(itemStack);
					if (containableProps != null)
					{
						num9 = num9 / containableProps.ItemsPerLitre / cookingRecipeIngredient.PortionSizeLitres;
					}
					if (ingredientStackNutritionProperties != null)
					{
						num5 = GameMath.Min(num5, ingredientStackNutritionProperties.Satiety * num9);
						num7 = GameMath.Max(num7, ingredientStackNutritionProperties.Satiety * num9);
						num6 = GameMath.Min(num6, ingredientStackNutritionProperties.Health * num9);
						num8 = GameMath.Max(num8, ingredientStackNutritionProperties.Health * num9);
					}
					flag = flag && ingredientStackNutritionProperties == null;
					EnumFoodCategory? enumFoodCategory = ingredientStackNutritionProperties?.FoodCategory;
					if (enumFoodCategory.HasValue)
					{
						EnumFoodCategory valueOrDefault = enumFoodCategory.GetValueOrDefault();
						if (valueOrDefault != EnumFoodCategory.NoNutrition && valueOrDefault != EnumFoodCategory.Unknown)
						{
							hashSet.Add(ingredientStackNutritionProperties.FoodCategory);
						}
					}
				}
				slots -= cookingRecipeIngredient.MinQuantity;
				if (!flag)
				{
					int num10 = cookingRecipeIngredient.MinQuantity;
					int num11 = cookingRecipeIngredient.MaxQuantity - num10;
					while (num10 > 0)
					{
						num += num5;
						num2 += num7;
						num3 += num6;
						num4 += num8;
						num10--;
					}
					if (num11 > 0)
					{
						list.Add(new IngredientMinMax
						{
							Code = cookingRecipeIngredient.Code,
							ExtraSlots = num11,
							MinSat = num5,
							MaxSat = num7,
							MinHP = num6,
							MaxHP = num8
						});
					}
					list2.Add(item);
				}
			}
			if (list.Count > 0 && slots > 0)
			{
				List<IngredientMinMax> list3 = (from ingredient in list
					orderby ingredient.MinSat
					select ingredient.Clone()).ToList();
				List<IngredientMinMax> list4 = (from ingredient in list
					orderby ingredient.MaxSat
					select ingredient.Clone()).ToList();
				List<IngredientMinMax> list5 = (from ingredient in list
					orderby ingredient.MinHP
					select ingredient.Clone()).ToList();
				List<IngredientMinMax> list6 = (from ingredient in list
					orderby ingredient.MaxHP
					select ingredient.Clone()).ToList();
				while (slots > 0)
				{
					float num13;
					float num14;
					float num15;
					float num12 = (num13 = (num14 = (num15 = 0f)));
					if (list3.First().MinSat < 0f)
					{
						IngredientMinMax ingredientMinMax = list3.First();
						num12 = ingredientMinMax.MinSat;
						ingredientMinMax.ExtraSlots--;
						if (ingredientMinMax.ExtraSlots == 0)
						{
							list3.Remove(ingredientMinMax);
						}
					}
					if (list4.Last().MaxSat > 0f)
					{
						IngredientMinMax ingredientMinMax2 = list4.Last();
						num14 = ingredientMinMax2.MaxSat;
						ingredientMinMax2.ExtraSlots--;
						if (ingredientMinMax2.ExtraSlots == 0)
						{
							list4.Remove(ingredientMinMax2);
						}
					}
					if (list5.First().MinHP < 0f)
					{
						IngredientMinMax ingredientMinMax3 = list5.First();
						num13 = ingredientMinMax3.MinHP;
						ingredientMinMax3.ExtraSlots--;
						if (ingredientMinMax3.ExtraSlots == 0)
						{
							list5.Remove(ingredientMinMax3);
						}
					}
					if (list6.Last().MaxHP > 0f)
					{
						IngredientMinMax ingredientMinMax4 = list6.Last();
						num15 = ingredientMinMax4.MaxHP;
						ingredientMinMax4.ExtraSlots--;
						if (ingredientMinMax4.ExtraSlots == 0)
						{
							list6.Remove(ingredientMinMax4);
						}
					}
					if (num12 < 0f)
					{
						num += num12;
					}
					if (num14 > 0f)
					{
						num2 += num14;
					}
					if (num13 < 0f)
					{
						num3 += num13;
					}
					if (num15 > 0f)
					{
						num4 += num15;
					}
					slots--;
				}
			}
			cachedNutritionFacts = new HandbookMealNutritionFacts
			{
				Categories = hashSet,
				MinSatiety = num,
				MaxSatiety = num2,
				MinHealth = num3,
				MaxHealth = num4
			};
		}
		return cachedNutritionFacts;
	}

	private List<ItemStack> getIngredientStacks(ICoreClientAPI capi, CookingRecipeIngredient ingredient, ItemStack[] allStacks)
	{
		if (!cachedIngredientStacks.TryGetValue(ingredient.Code, out List<ItemStack> value))
		{
			HashSet<ItemStack> hashSet = new HashSet<ItemStack>();
			foreach (ItemStack itemStack in allStacks)
			{
				CookingRecipeStack matchingStack = ingredient.GetMatchingStack(itemStack);
				if (matchingStack != null)
				{
					ItemStack itemStack2 = itemStack.Clone();
					itemStack2.StackSize = matchingStack.StackSize;
					WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(itemStack2);
					if (containableProps != null)
					{
						itemStack2.StackSize = (int)(containableProps.ItemsPerLitre * ingredient.PortionSizeLitres) * matchingStack.StackSize;
					}
					hashSet.Add(itemStack2);
				}
			}
			cachedIngredientStacks.Add(ingredient.Code, hashSet.ToList());
			return hashSet.Select((ItemStack stack) => stack.Clone()).ToList();
		}
		return value.Select((ItemStack stack) => stack.Clone()).ToList();
	}

	protected List<CookingRecipeIngredient> CombinedIngredientsList()
	{
		List<CookingRecipeIngredient> list = new List<CookingRecipeIngredient>();
		CookingRecipeIngredient[] ingredients = Recipe.Ingredients;
		foreach (CookingRecipeIngredient cookingRecipeIngredient in ingredients)
		{
			if (list.Count > 0)
			{
				List<AssetLocation> list2 = new List<AssetLocation>();
				CookingRecipeStack[] validStacks = cookingRecipeIngredient.ValidStacks;
				foreach (CookingRecipeStack cookingRecipeStack in validStacks)
				{
					list2.Add(cookingRecipeStack.Code);
				}
				bool flag = true;
				foreach (CookingRecipeIngredient item in list)
				{
					List<AssetLocation> list3 = new List<AssetLocation>();
					validStacks = item.ValidStacks;
					foreach (CookingRecipeStack cookingRecipeStack2 in validStacks)
					{
						list3.Add(cookingRecipeStack2.Code);
					}
					list2.Sort();
					list3.Sort();
					bool flag2 = true;
					if (list2.Count != list3.Count)
					{
						flag2 = false;
					}
					for (int k = 0; k < list3.Count; k++)
					{
						if (k < list2.Count && list2[k] != list3[k])
						{
							flag2 = false;
						}
					}
					if (flag2)
					{
						item.MinQuantity += cookingRecipeIngredient.MinQuantity;
						item.MaxQuantity += cookingRecipeIngredient.MaxQuantity;
						flag = false;
					}
				}
				if (flag)
				{
					list.Add(cookingRecipeIngredient.Clone());
				}
			}
			else
			{
				list.Add(cookingRecipeIngredient.Clone());
			}
		}
		return list;
	}

	public override float GetTextMatchWeight(string searchText)
	{
		string text = Lang.Get("handbook-mealrecipe-" + (isPie ? "pie" : "meal") + "searchkeywords");
		if (titleCached.Equals(searchText, StringComparison.InvariantCultureIgnoreCase))
		{
			return 4f;
		}
		if (titleCached.StartsWith(searchText + " ", StringComparison.InvariantCultureIgnoreCase))
		{
			return 3.5f;
		}
		if (titleCached.StartsWith(searchText, StringComparison.InvariantCultureIgnoreCase))
		{
			return 3f;
		}
		if (titleCached.CaseInsensitiveContains(searchText))
		{
			return 2.75f;
		}
		if (text.CaseInsensitiveContains(searchText))
		{
			return 2.5f;
		}
		return 0f;
	}
}
