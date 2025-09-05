using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class VanillaCookingRecipeNames : ICookingRecipeNamingHelper
{
	protected enum EnumIngredientNameType
	{
		None,
		InsturmentalCase,
		Topping
	}

	public string GetNameForIngredients(IWorldAccessor worldForResolve, string recipeCode, ItemStack[] stacks)
	{
		OrderedDictionary<ItemStack, int> orderedDictionary = new OrderedDictionary<ItemStack, int>();
		orderedDictionary = mergeStacks(worldForResolve, stacks);
		CookingRecipe cookingRecipe = worldForResolve.Api.GetCookingRecipe(recipeCode);
		if (recipeCode == null || cookingRecipe == null || orderedDictionary.Count == 0)
		{
			return Lang.Get("unknown");
		}
		return GetNameForMergedIngredients(worldForResolve, cookingRecipe, orderedDictionary);
	}

	protected virtual string GetNameForMergedIngredients(IWorldAccessor worldForResolve, CookingRecipe recipe, OrderedDictionary<ItemStack, int> quantitiesByStack)
	{
		string code = recipe.Code;
		switch (code)
		{
		case "soup":
		{
			List<string> list9 = new List<string>();
			List<string> list10 = new List<string>();
			CookingRecipeIngredient cookingRecipeIngredient3 = null;
			ItemStack itemStack = null;
			ItemStack itemStack2 = null;
			ItemStack itemStack3 = null;
			string item = string.Empty;
			int num9 = 0;
			foreach (KeyValuePair<ItemStack, int> item2 in quantitiesByStack)
			{
				if (item2.Key.Collectible.Code.Path.Contains("waterportion"))
				{
					continue;
				}
				ItemStack itemStack4 = item2.Key;
				cookingRecipeIngredient3 = recipe.GetIngrendientFor(itemStack4);
				if (cookingRecipeIngredient3?.Code == "cream")
				{
					itemStack2 = itemStack4;
					continue;
				}
				if (cookingRecipeIngredient3?.Code == "stock")
				{
					itemStack = itemStack4;
					continue;
				}
				if (num9 < item2.Value)
				{
					num9 = item2.Value;
					itemStack4 = itemStack3;
					itemStack3 = item2.Key;
				}
				if (itemStack4 == null)
				{
					continue;
				}
				item = ingredientName(itemStack4, EnumIngredientNameType.InsturmentalCase);
				if (getFoodCat(worldForResolve, itemStack4, cookingRecipeIngredient3) == EnumFoodCategory.Vegetable || itemStack4.Collectible.FirstCodePart().Contains("egg"))
				{
					if (!list9.Contains(item))
					{
						list9.Add(item);
					}
				}
				else if (!list10.Contains(item))
				{
					list10.Add(item);
				}
			}
			List<string> list11 = new List<string>();
			string format3 = "{0}";
			if (itemStack2 != null)
			{
				if (itemStack != null)
				{
					item = getMainIngredientName(itemStack, "soup");
				}
				else if (itemStack3 != null)
				{
					item = getMainIngredientName(itemStack3, "soup");
				}
				list11.Add(item);
				list11.Add(getMainIngredientName(itemStack2, "soup", secondary: true));
				format3 = "meal-soup-in-cream-format";
			}
			else if (itemStack != null)
			{
				if (itemStack3 != null)
				{
					item = getMainIngredientName(itemStack3, "soup");
				}
				list11.Add(item);
				list11.Add(getMainIngredientName(itemStack, "soup", secondary: true));
				format3 = "meal-soup-in-stock-format";
			}
			else if (itemStack3 != null)
			{
				list11.Add(getMainIngredientName(itemStack3, "soup"));
			}
			string code4 = "meal-adds-soup-boiled";
			if (list10.Count > 0)
			{
				code4 = ((list9.Count <= 0) ? "meal-adds-soup-stewed" : "meal-adds-soup-boiled-and-stewed");
			}
			return Lang.Get(getMaxMealFormat("meal", "soup", num9), getMainIngredientsString(list11, format3), getMealAddsString(code4, list9, list10)).Trim().UcFirst();
		}
		case "porridge":
		{
			string format4 = "meal";
			List<string> list12 = new List<string>();
			List<string> list13 = new List<string>();
			List<string> list14 = new List<string>();
			string text3 = string.Empty;
			string empty4 = string.Empty;
			int num10 = quantitiesByStack.Where<KeyValuePair<ItemStack, int>>((KeyValuePair<ItemStack, int> val) => recipe.GetIngrendientFor(val.Key)?.Code == "grain-base").Count();
			int num11 = 0;
			foreach (KeyValuePair<ItemStack, int> item3 in quantitiesByStack)
			{
				CookingRecipeIngredient ingrendientFor2 = recipe.GetIngrendientFor(item3.Key);
				if (ingrendientFor2?.Code == "topping")
				{
					text3 = ingredientName(item3.Key, EnumIngredientNameType.Topping);
					continue;
				}
				if (ingrendientFor2?.Code == "grain-base")
				{
					if (num10 < 3)
					{
						if (list12.Count < 2)
						{
							empty4 = getMainIngredientName(item3.Key, code, list12.Count > 0);
							if (!list12.Contains(empty4))
							{
								list12.Add(empty4);
							}
						}
					}
					else
					{
						empty4 = ingredientName(item3.Key);
						if (!list12.Contains(empty4))
						{
							list12.Add(empty4);
						}
					}
					num11 += item3.Value;
					continue;
				}
				empty4 = ingredientName(item3.Key, EnumIngredientNameType.InsturmentalCase);
				if (getFoodCat(worldForResolve, item3.Key, ingrendientFor2) == EnumFoodCategory.Vegetable)
				{
					if (!list13.Contains(empty4))
					{
						list13.Add(empty4);
					}
				}
				else if (!list14.Contains(empty4))
				{
					list14.Add(empty4);
				}
			}
			string code5 = "meal-adds-porridge-mashed";
			if (list14.Count > 0)
			{
				code5 = ((list13.Count <= 0) ? "meal-adds-porridge-fresh" : "meal-adds-porridge-mashed-and-fresh");
			}
			string format5 = "{0}";
			if (list12.Count == 2)
			{
				format5 = "multi-main-ingredients-format";
			}
			format4 = getMaxMealFormat(format4, code, num11);
			format4 = Lang.Get(format4, getMainIngredientsString(list12, format5), getMealAddsString(code5, list13, list14));
			if (text3 != string.Empty)
			{
				format4 = Lang.Get("meal-topping-ingredient-format", text3, format4);
			}
			return format4.Trim().UcFirst();
		}
		case "meatystew":
		case "vegetablestew":
		{
			ItemStack[] array3 = new ItemStack[quantitiesByStack.Count];
			int num6 = 0;
			int num7 = 0;
			CookingRecipeIngredient[] ingredients = recipe.Ingredients;
			foreach (CookingRecipeIngredient cookingRecipeIngredient2 in ingredients)
			{
				if (!cookingRecipeIngredient2.Code.Contains("base"))
				{
					continue;
				}
				for (int l = 0; l < quantitiesByStack.Count; l++)
				{
					ItemStack keyAtIndex3 = quantitiesByStack.GetKeyAtIndex(l);
					if (cookingRecipeIngredient2.Matches(keyAtIndex3) && !array3.Contains(keyAtIndex3))
					{
						array3[l] = keyAtIndex3;
						if (getFoodCat(worldForResolve, keyAtIndex3, cookingRecipeIngredient2) == EnumFoodCategory.Vegetable)
						{
							num6++;
						}
						if (getFoodCat(worldForResolve, keyAtIndex3, cookingRecipeIngredient2) == EnumFoodCategory.Protein)
						{
							num7++;
						}
					}
				}
			}
			List<string> list6 = new List<string>();
			List<string> list7 = new List<string>();
			List<string> list8 = new List<string>();
			string text2 = string.Empty;
			string empty3 = string.Empty;
			EnumFoodCategory enumFoodCategory = EnumFoodCategory.Protein;
			int num8 = 0;
			if (num6 > num7)
			{
				enumFoodCategory = EnumFoodCategory.Vegetable;
			}
			for (int m = 0; m < quantitiesByStack.Count; m++)
			{
				ItemStack keyAtIndex4 = quantitiesByStack.GetKeyAtIndex(m);
				int valueAtIndex2 = quantitiesByStack.GetValueAtIndex(m);
				CookingRecipeIngredient ingrendientFor = recipe.GetIngrendientFor(keyAtIndex4);
				if (ingrendientFor?.Code == "topping")
				{
					text2 = ingredientName(keyAtIndex4, EnumIngredientNameType.Topping);
					continue;
				}
				EnumFoodCategory foodCat = getFoodCat(worldForResolve, array3[m], ingrendientFor);
				bool flag2 = (uint)(foodCat - 1) <= 1u;
				if ((flag2 && quantitiesByStack.Count <= 2) || foodCat == enumFoodCategory)
				{
					num8 += valueAtIndex2;
					if (list6.Count < 2)
					{
						empty3 = getMainIngredientName(keyAtIndex4, "stew", list6.Count > 0);
						if (!list6.Contains(empty3))
						{
							list6.Add(empty3);
						}
						continue;
					}
				}
				empty3 = ingredientName(keyAtIndex4, EnumIngredientNameType.InsturmentalCase);
				if (getFoodCat(worldForResolve, keyAtIndex4, ingrendientFor) == EnumFoodCategory.Vegetable || keyAtIndex4.Collectible.FirstCodePart().Contains("egg"))
				{
					if (!list7.Contains(empty3))
					{
						list7.Add(empty3);
					}
				}
				else if (!list8.Contains(empty3))
				{
					list8.Add(empty3);
				}
			}
			string code3 = "meal-adds-stew-boiled";
			if (list8.Count > 0)
			{
				code3 = ((list7.Count <= 0) ? "meal-adds-stew-stewed" : "meal-adds-stew-boiled-and-stewed");
			}
			string format2 = "{0}";
			if (list6.Count == 2)
			{
				format2 = "multi-main-ingredients-format";
			}
			string maxMealFormat2 = getMaxMealFormat("meal", "stew", num8);
			maxMealFormat2 = Lang.Get(maxMealFormat2, getMainIngredientsString(list6, format2), getMealAddsString(code3, list7, list8));
			if (text2 != string.Empty)
			{
				maxMealFormat2 = Lang.Get("meal-topping-ingredient-format", text2, maxMealFormat2);
			}
			return maxMealFormat2.Trim().UcFirst();
		}
		case "scrambledeggs":
		{
			List<string> list3 = new List<string>();
			List<string> list4 = new List<string>();
			List<string> list5 = new List<string>();
			string empty2 = string.Empty;
			int num5 = 0;
			foreach (KeyValuePair<ItemStack, int> item4 in quantitiesByStack)
			{
				if (recipe.GetIngrendientFor(item4.Key)?.Code == "egg-base")
				{
					empty2 = getMainIngredientName(item4.Key, code);
					if (!list3.Contains(empty2))
					{
						list3.Add(empty2);
					}
					num5 += item4.Value;
					continue;
				}
				empty2 = ingredientName(item4.Key, EnumIngredientNameType.InsturmentalCase);
				if (item4.Key.Collectible.FirstCodePart() == "cheese")
				{
					if (!list5.Contains(empty2))
					{
						list5.Add(empty2);
					}
				}
				else if (!list4.Contains(empty2))
				{
					list4.Add(empty2);
				}
			}
			string code2 = "meal-adds-scrambledeggs-fresh";
			if (list5.Count > 0)
			{
				code2 = ((list4.Count <= 0) ? "meal-adds-scrambledeggs-melted" : "meal-adds-scrambledeggs-melted-and-fresh");
			}
			return Lang.Get(getMaxMealFormat("meal", code, num5), getMainIngredientsString(list3, "{0}"), getMealAddsString(code2, list5, list4)).Trim().UcFirst();
		}
		case "jam":
		{
			ItemStack[] array2 = new ItemStack[2];
			int num4 = 0;
			foreach (KeyValuePair<ItemStack, int> item5 in quantitiesByStack)
			{
				if (recipe.GetIngrendientFor(item5.Key)?.Code != "sweetener")
				{
					array2[num4++] = item5.Key;
					if (num4 == 2)
					{
						break;
					}
				}
			}
			if (array2[0] != null)
			{
				string key = array2[0].Collectible.LastCodePart() + ((array2[1] != null) ? ("-" + array2[1].Collectible.LastCodePart() + "-") : "-") + "jam";
				if (Lang.HasTranslation(key))
				{
					return Lang.Get(key);
				}
				return Lang.Get((array2[1] != null) ? "mealname-mixedjam" : "mealname-singlejam", getInJamName(array2[0]), getInJamName(array2[1]));
			}
			return Lang.Get("unknown");
		}
		default:
		{
			if (Lang.HasTranslation("meal-" + code))
			{
				return Lang.Get("meal-" + code);
			}
			ItemStack[] array = new ItemStack[quantitiesByStack.Count];
			int num = 0;
			bool flag = false;
			CookingRecipeIngredient[] ingredients = recipe.Ingredients;
			foreach (CookingRecipeIngredient cookingRecipeIngredient in ingredients)
			{
				bool num2 = cookingRecipeIngredient.Code.Contains("base");
				if (num2 && !flag)
				{
					flag = true;
					array = new ItemStack[quantitiesByStack.Count];
					num = 0;
				}
				if (!(num2 && flag) && (cookingRecipeIngredient.MinQuantity <= 0 || flag))
				{
					continue;
				}
				for (int j = 0; j < quantitiesByStack.Count; j++)
				{
					ItemStack keyAtIndex = quantitiesByStack.GetKeyAtIndex(j);
					if (cookingRecipeIngredient.Matches(quantitiesByStack.GetKeyAtIndex(j)) && !array.Contains(keyAtIndex))
					{
						array[j] = keyAtIndex;
						num++;
					}
				}
			}
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			string text = string.Empty;
			string empty = string.Empty;
			int num3 = 0;
			for (int k = 0; k < quantitiesByStack.Count; k++)
			{
				ItemStack keyAtIndex2 = quantitiesByStack.GetKeyAtIndex(k);
				int valueAtIndex = quantitiesByStack.GetValueAtIndex(k);
				if (recipe.GetIngrendientFor(keyAtIndex2)?.Code == "topping")
				{
					text = ingredientName(keyAtIndex2, EnumIngredientNameType.Topping);
				}
				else if (array[k] != null)
				{
					if (num < 3)
					{
						if (list.Count < 2)
						{
							empty = getMainIngredientName(keyAtIndex2, code, list.Count > 0);
							if (!list.Contains(empty))
							{
								list.Add(empty);
							}
						}
					}
					else
					{
						empty = ingredientName(keyAtIndex2);
						if (!list.Contains(empty))
						{
							list.Add(empty);
						}
					}
					num3 += valueAtIndex;
				}
				else
				{
					empty = ingredientName(keyAtIndex2, EnumIngredientNameType.InsturmentalCase);
					if (!list2.Contains(empty))
					{
						list2.Add(empty);
					}
				}
			}
			string format = "{0}";
			if (list.Count == 2)
			{
				format = "multi-main-ingredients-format";
			}
			string maxMealFormat = getMaxMealFormat("meal", code, num3);
			maxMealFormat = Lang.Get(maxMealFormat, getMainIngredientsString(list, format), getMealAddsString("meal-adds-generic", list2));
			if (text != string.Empty)
			{
				maxMealFormat = Lang.Get("meal-topping-ingredient-format", text, maxMealFormat);
			}
			return maxMealFormat.Trim().UcFirst();
		}
		}
	}

	protected string getMaxMealFormat(string format, string recipeCode, int max)
	{
		format = max switch
		{
			3 => format + "-hearty-" + recipeCode, 
			4 => format + "-hefty-" + recipeCode, 
			_ => format + "-normal-" + recipeCode, 
		};
		return format;
	}

	protected EnumFoodCategory getFoodCat(IWorldAccessor worldForResolve, ItemStack stack, CookingRecipeIngredient? ingred)
	{
		ItemStack stack2 = ingred?.GetMatchingStack(stack)?.CookedStack?.ResolvedItemstack;
		return (BlockMeal.GetIngredientStackNutritionProperties(worldForResolve, stack2, null) ?? BlockMeal.GetIngredientStackNutritionProperties(worldForResolve, stack, null))?.FoodCategory ?? EnumFoodCategory.Unknown;
	}

	protected string ingredientName(ItemStack stack, EnumIngredientNameType NameType = EnumIngredientNameType.None)
	{
		string text = stack.Collectible.Code?.Domain + ":recipeingredient-" + stack.Class.ToString().ToLowerInvariant() + "-" + stack.Collectible.Code?.Path;
		if (NameType == EnumIngredientNameType.InsturmentalCase)
		{
			text += "-insturmentalcase";
		}
		if (NameType == EnumIngredientNameType.Topping)
		{
			text += "-topping";
		}
		if (Lang.HasTranslation(text))
		{
			return Lang.GetMatching(text);
		}
		text = stack.Collectible.Code?.Domain + ":recipeingredient-" + stack.Class.ToString().ToLowerInvariant() + "-" + stack.Collectible.FirstCodePart();
		if (NameType == EnumIngredientNameType.InsturmentalCase)
		{
			text += "-insturmentalcase";
		}
		if (NameType == EnumIngredientNameType.Topping)
		{
			text += "-topping";
		}
		return Lang.GetMatching(text);
	}

	protected string getMainIngredientName(ItemStack itemstack, string code, bool secondary = false)
	{
		string value = (secondary ? "secondary" : "primary");
		string key = $"meal-ingredient-{code}-{value}-{itemstack.Collectible.Code.Path}";
		if (Lang.HasTranslation(key))
		{
			return Lang.GetMatching(key);
		}
		key = $"meal-ingredient-{code}-{value}-{itemstack.Collectible.FirstCodePart()}";
		return Lang.GetMatching(key);
	}

	protected string getInJamName(ItemStack fruit)
	{
		if (fruit == null)
		{
			return "";
		}
		string key = (fruit.Collectible.Code.Domain + ":" + fruit.Collectible.LastCodePart() + "-in-jam-name").Replace("game:", "");
		if (!Lang.HasTranslation(key))
		{
			return fruit.GetName();
		}
		return Lang.Get(key);
	}

	protected string getMainIngredientsString(List<string> ingredients, string format, bool list = true)
	{
		if (ingredients.Count == 0)
		{
			return "";
		}
		if (ingredients.Count < 3 || !list)
		{
			object[] args = ingredients.ToArray();
			return Lang.Get(format, args);
		}
		return getMealAddsString(format, ingredients);
	}

	protected string getMealAddsString(string code, List<string> ingredients1, List<string>? ingredients2 = null)
	{
		if (ingredients1.Count == 0)
		{
			if (ingredients2 == null || ingredients2.Count == 0)
			{
				return "";
			}
			ingredients1 = ingredients2.ToList();
			ingredients2 = null;
		}
		object[] array = new object[2];
		string key = $"meal-ingredientlist-{ingredients1?.Count ?? 0}";
		object[] args = ingredients1?.ToArray() ?? new string[1] { "" };
		array[0] = Lang.Get(key, args);
		string key2 = $"meal-ingredientlist-{ingredients2?.Count ?? 0}";
		args = ingredients2?.ToArray() ?? new string[1] { "" };
		array[1] = Lang.Get(key2, args);
		return Lang.Get(code, array);
	}

	protected static OrderedDictionary<ItemStack, int> mergeStacks(IWorldAccessor worldForResolve, ItemStack[] stacks)
	{
		OrderedDictionary<ItemStack, int> orderedDictionary = new OrderedDictionary<ItemStack, int>();
		List<ItemStack> list = stacks.ToList();
		while (list.Count > 0)
		{
			ItemStack stack = list[0];
			list.RemoveAt(0);
			if (stack == null)
			{
				continue;
			}
			int num = 1;
			while (true)
			{
				ItemStack itemStack = list.FirstOrDefault((ItemStack ostack) => ostack?.Equals(worldForResolve, stack, GlobalConstants.IgnoredStackAttributes) ?? false);
				if (itemStack == null)
				{
					break;
				}
				list.Remove(itemStack);
				num++;
			}
			orderedDictionary[stack] = num;
		}
		return orderedDictionary;
	}
}
