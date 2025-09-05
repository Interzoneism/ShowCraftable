using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class GridRecipe : IByteSerializable
{
	[DocumentAsJson]
	public bool Enabled = true;

	[DocumentAsJson]
	public string IngredientPattern;

	[DocumentAsJson]
	public Dictionary<string, CraftingRecipeIngredient> Ingredients;

	[DocumentAsJson]
	public int Width = 3;

	[DocumentAsJson]
	public int Height = 3;

	[DocumentAsJson]
	public int RecipeGroup;

	[DocumentAsJson]
	public bool ShowInCreatedBy = true;

	[DocumentAsJson]
	public CraftingRecipeIngredient Output;

	[DocumentAsJson]
	public bool Shapeless;

	[DocumentAsJson]
	public AssetLocation Name;

	[JsonConverter(typeof(JsonAttributesConverter))]
	[DocumentAsJson]
	public JsonObject Attributes;

	[DocumentAsJson]
	public string RequiresTrait;

	[DocumentAsJson]
	public bool AverageDurability = true;

	[DocumentAsJson]
	public string[] MergeAttributesFrom = Array.Empty<string>();

	[DocumentAsJson]
	public Dictionary<string, string[]> AllowedVariants = new Dictionary<string, string[]>();

	[DocumentAsJson]
	public Dictionary<string, string[]> SkipVariants = new Dictionary<string, string[]>();

	public GridRecipeIngredient[] resolvedIngredients;

	protected IWorldAccessor World;

	[DocumentAsJson]
	public string CopyAttributesFrom { get; set; }

	public bool ResolveIngredients(IWorldAccessor world)
	{
		World = world;
		IngredientPattern = IngredientPattern.Replace(",", "").Replace("\t", "").Replace("\r", "")
			.Replace("\n", "")
			.DeDuplicate();
		if (IngredientPattern == null)
		{
			world.Logger.Error("Grid Recipe with output {0} has no ingredient pattern.", Output);
			return false;
		}
		if (Width * Height != IngredientPattern.Length)
		{
			world.Logger.Error("Grid Recipe with output {0} has and incorrect ingredient pattern length. Ignoring recipe.", Output);
			return false;
		}
		resolvedIngredients = new GridRecipeIngredient[Width * Height];
		for (int i = 0; i < IngredientPattern.Length; i++)
		{
			char c = IngredientPattern[i];
			if (c != ' ' && c != '_')
			{
				string text = c.ToString();
				if (!Ingredients.TryGetValue(text, out var value))
				{
					world.Logger.Error("Grid Recipe with output {0} contains an ingredient pattern code {1} but supplies no ingredient for it.", Output, text);
					return false;
				}
				if (!value.Resolve(world, "Grid recipe"))
				{
					world.Logger.Error("Grid Recipe with output {0} contains an ingredient that cannot be resolved: {1}", Output, value);
					return false;
				}
				GridRecipeIngredient gridRecipeIngredient = value.CloneTo<GridRecipeIngredient>();
				gridRecipeIngredient.PatternCode = text;
				resolvedIngredients[i] = gridRecipeIngredient;
			}
		}
		if (!Output.Resolve(world, "Grid recipe"))
		{
			world.Logger.Error("Grid Recipe '{0}': Output {1} cannot be resolved", Name, Output);
			return false;
		}
		return true;
	}

	public virtual void FreeRAMServer()
	{
		IngredientPattern = null;
		Ingredients = null;
	}

	public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
	{
		Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();
		foreach (var (_, craftingRecipeIngredient2) in Ingredients)
		{
			if (IsRegex(craftingRecipeIngredient2.Code.Path))
			{
				craftingRecipeIngredient2.IsRegex = true;
			}
			else if (IsAdvancedWildcard(craftingRecipeIngredient2.Code.Path))
			{
				GetNameToCodeMappingForAdvancedWildcard(world, craftingRecipeIngredient2, dictionary);
				craftingRecipeIngredient2.IsAdvancedWildCard = true;
			}
			else if (IsBasicWildcard(craftingRecipeIngredient2.Code.Path) && craftingRecipeIngredient2.Name != null)
			{
				GetNameToCodeMappingForBasicWildcard(world, craftingRecipeIngredient2, dictionary);
				craftingRecipeIngredient2.IsBasicWildCard = true;
			}
		}
		return dictionary;
	}

	public static bool IsAdvancedWildcard(string code)
	{
		if (code.Contains('{'))
		{
			return code.Contains('}');
		}
		return false;
	}

	public static bool IsBasicWildcard(string code)
	{
		return code.Contains('*');
	}

	public static bool IsRegex(string code)
	{
		return code.StartsWith('@');
	}

	protected void GetNameToCodeMappingForBasicWildcard(IWorldAccessor world, CraftingRecipeIngredient ingredient, Dictionary<string, string[]> mappings)
	{
		int num = ingredient.Code.Path.IndexOf('*');
		int num2 = ingredient.Code.Path.Length - num - 1;
		List<string> list = new List<string>();
		if (ingredient.Type == EnumItemClass.Block)
		{
			foreach (Block block in world.Blocks)
			{
				if (!block.IsMissing && (ingredient.SkipVariants == null || !WildcardUtil.MatchesVariants(ingredient.Code, block.Code, ingredient.SkipVariants)) && WildcardUtil.Match(ingredient.Code, block.Code, ingredient.AllowedVariants))
				{
					string text = block.Code.Path.Substring(num);
					string item = text.Substring(0, text.Length - num2).DeDuplicate();
					list.Add(item);
				}
			}
		}
		else
		{
			foreach (Item item3 in world.Items)
			{
				if (!(item3?.Code == null) && !item3.IsMissing && (ingredient.SkipVariants == null || !WildcardUtil.MatchesVariants(ingredient.Code, item3.Code, ingredient.SkipVariants)) && WildcardUtil.Match(ingredient.Code, item3.Code, ingredient.AllowedVariants))
				{
					string text2 = item3.Code.Path.Substring(num);
					string item2 = text2.Substring(0, text2.Length - num2).DeDuplicate();
					list.Add(item2);
				}
			}
		}
		mappings[ingredient.Name] = list.ToArray();
	}

	protected void GetNameToCodeMappingForAdvancedWildcard(IWorldAccessor world, CraftingRecipeIngredient ingredient, Dictionary<string, string[]> mappings)
	{
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
		List<string> variants;
		Regex regex = new Regex(ReplaceVariantsToRegex(ingredient.Code.Path, out variants));
		if (ingredient.Type == EnumItemClass.Block)
		{
			foreach (Block block in world.Blocks)
			{
				if (!block.IsMissing)
				{
					MatchCollectibleCode(block.Code, regex, variants, dictionary);
				}
			}
		}
		else
		{
			foreach (Item item in world.Items)
			{
				if (!(item?.Code == null) && !item.IsMissing)
				{
					MatchCollectibleCode(item.Code, regex, variants, dictionary);
				}
			}
		}
		foreach (var (key, list2) in dictionary)
		{
			if (mappings.ContainsKey(key))
			{
				List<string> list3 = new List<string>();
				foreach (string item2 in list2)
				{
					if (mappings[key].Contains(item2))
					{
						list3.Add(item2);
					}
				}
				mappings[key] = list3.ToArray();
			}
			else
			{
				mappings[key] = list2.ToArray();
			}
		}
	}

	protected static string ReplaceVariantsToRegex(string value, out List<string> variants)
	{
		variants = new List<string>();
		string text = value;
		while (text.Contains("{") && text.Contains("}"))
		{
			int num = text.IndexOf('{');
			int num2 = text.IndexOf('}');
			string text2 = text.Substring(num + 1, num2 - num - 1);
			text = text.Replace("{" + text2 + "}", "@");
			variants.Add(text2);
		}
		text = WildCardToRegex(text);
		return text.Replace("@", "(\\w+)");
	}

	protected static string WildCardToRegex(string value)
	{
		return Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*");
	}

	protected void MatchCollectibleCode(AssetLocation code, Regex regex, List<string> variants, Dictionary<string, List<string>> variantCodes)
	{
		string path = code.Path;
		Match match = regex.Match(path);
		if (!match.Success)
		{
			return;
		}
		for (int i = 0; i < variants.Count; i++)
		{
			string value = match.Groups[i + 1].Value;
			string key = variants[i];
			if ((!AllowedVariants.ContainsKey(key) || AllowedVariants[key].Contains(value)) && (!SkipVariants.ContainsKey(key) || !SkipVariants[key].Contains(value)))
			{
				if (!variantCodes.ContainsKey(key))
				{
					variantCodes[key] = new List<string>();
				}
				variantCodes[key].Add(value);
			}
		}
	}

	public bool ConsumeInput(IPlayer byPlayer, ItemSlot[] inputSlots, int gridWidth)
	{
		if (Shapeless)
		{
			return ConsumeInputShapeLess(byPlayer, inputSlots);
		}
		int num = inputSlots.Length / gridWidth;
		if (gridWidth < Width || num < Height)
		{
			return false;
		}
		int num2 = 0;
		for (int i = 0; i <= gridWidth - Width; i++)
		{
			for (int j = 0; j <= num - Height; j++)
			{
				if (MatchesAtPosition(i, j, inputSlots, gridWidth))
				{
					return ConsumeInputAt(byPlayer, inputSlots, gridWidth, i, j);
				}
				num2++;
			}
		}
		return false;
	}

	protected bool ConsumeInputShapeLess(IPlayer byPlayer, ItemSlot[] inputSlots)
	{
		List<CraftingRecipeIngredient> list = new List<CraftingRecipeIngredient>();
		List<CraftingRecipeIngredient> list2 = new List<CraftingRecipeIngredient>();
		for (int i = 0; i < resolvedIngredients.Length; i++)
		{
			CraftingRecipeIngredient craftingRecipeIngredient = resolvedIngredients[i];
			if (craftingRecipeIngredient == null)
			{
				continue;
			}
			if (craftingRecipeIngredient.IsWildCard || craftingRecipeIngredient.IsTool)
			{
				list2.Add(craftingRecipeIngredient.Clone());
				continue;
			}
			ItemStack resolvedItemstack = craftingRecipeIngredient.ResolvedItemstack;
			bool flag = false;
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j].ResolvedItemstack.Satisfies(resolvedItemstack))
				{
					list[j].ResolvedItemstack.StackSize += resolvedItemstack.StackSize;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Add(craftingRecipeIngredient.Clone());
			}
		}
		for (int k = 0; k < inputSlots.Length; k++)
		{
			ItemStack itemstack = inputSlots[k].Itemstack;
			if (itemstack == null)
			{
				continue;
			}
			for (int l = 0; l < list.Count; l++)
			{
				if (list[l].ResolvedItemstack.Satisfies(itemstack))
				{
					int num = Math.Min(list[l].ResolvedItemstack.StackSize, itemstack.StackSize);
					itemstack.Collectible.OnConsumedByCrafting(inputSlots, inputSlots[k], this, list[l], byPlayer, num);
					list[l].ResolvedItemstack.StackSize -= num;
					if (list[l].ResolvedItemstack.StackSize <= 0)
					{
						list.RemoveAt(l);
					}
					break;
				}
			}
			for (int m = 0; m < list2.Count; m++)
			{
				CraftingRecipeIngredient craftingRecipeIngredient2 = list2[m];
				if (craftingRecipeIngredient2.Type != itemstack.Class || !WildcardUtil.Match(craftingRecipeIngredient2.Code, itemstack.Collectible.Code, craftingRecipeIngredient2.AllowedVariants))
				{
					continue;
				}
				int num2 = Math.Min(craftingRecipeIngredient2.Quantity, itemstack.StackSize);
				itemstack.Collectible.OnConsumedByCrafting(inputSlots, inputSlots[k], this, craftingRecipeIngredient2, byPlayer, num2);
				if (craftingRecipeIngredient2.IsTool)
				{
					list2.RemoveAt(m);
					break;
				}
				craftingRecipeIngredient2.Quantity -= num2;
				if (craftingRecipeIngredient2.Quantity <= 0)
				{
					list2.RemoveAt(m);
				}
				break;
			}
		}
		return list.Count == 0;
	}

	protected bool ConsumeInputAt(IPlayer byPlayer, ItemSlot[] inputSlots, int gridWidth, int colStart, int rowStart)
	{
		int num = inputSlots.Length / gridWidth;
		for (int i = 0; i < gridWidth; i++)
		{
			for (int j = 0; j < num; j++)
			{
				ItemSlot elementInGrid = GetElementInGrid(j, i, inputSlots, gridWidth);
				CraftingRecipeIngredient elementInGrid2 = GetElementInGrid(j - rowStart, i - colStart, resolvedIngredients, Width);
				if (elementInGrid2 != null)
				{
					if (elementInGrid.Itemstack == null)
					{
						return false;
					}
					int quantity = (elementInGrid2.IsWildCard ? elementInGrid2.Quantity : elementInGrid2.ResolvedItemstack.StackSize);
					elementInGrid.Itemstack.Collectible.OnConsumedByCrafting(inputSlots, elementInGrid, this, elementInGrid2, byPlayer, quantity);
				}
			}
		}
		return true;
	}

	public bool Matches(IPlayer forPlayer, ItemSlot[] ingredients, int gridWidth)
	{
		if (!forPlayer.Entity.Api.Event.TriggerMatchesRecipe(forPlayer, this, ingredients, gridWidth))
		{
			return false;
		}
		if (Shapeless)
		{
			return MatchesShapeLess(ingredients, gridWidth);
		}
		int num = ingredients.Length / gridWidth;
		if (gridWidth < Width || num < Height)
		{
			return false;
		}
		for (int i = 0; i <= gridWidth - Width; i++)
		{
			for (int j = 0; j <= num - Height; j++)
			{
				if (MatchesAtPosition(i, j, ingredients, gridWidth))
				{
					return true;
				}
			}
		}
		return false;
	}

	protected bool MatchesShapeLess(ItemSlot[] suppliedSlots, int gridWidth)
	{
		int num = suppliedSlots.Length / gridWidth;
		if (gridWidth < Width || num < Height)
		{
			return false;
		}
		List<KeyValuePair<ItemStack, CraftingRecipeIngredient>> list = new List<KeyValuePair<ItemStack, CraftingRecipeIngredient>>();
		List<ItemStack> list2 = new List<ItemStack>();
		for (int i = 0; i < suppliedSlots.Length; i++)
		{
			if (suppliedSlots[i].Itemstack == null)
			{
				continue;
			}
			bool flag = false;
			for (int j = 0; j < list2.Count; j++)
			{
				if (list2[j].Satisfies(suppliedSlots[i].Itemstack))
				{
					list2[j].StackSize += suppliedSlots[i].Itemstack.StackSize;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list2.Add(suppliedSlots[i].Itemstack.Clone());
			}
		}
		for (int k = 0; k < resolvedIngredients.Length; k++)
		{
			CraftingRecipeIngredient craftingRecipeIngredient = resolvedIngredients[k];
			if (craftingRecipeIngredient == null)
			{
				continue;
			}
			if (craftingRecipeIngredient.IsWildCard)
			{
				bool flag2 = false;
				int num2 = 0;
				while (!flag2 && num2 < list2.Count)
				{
					ItemStack itemStack = list2[num2];
					flag2 = craftingRecipeIngredient.Type == itemStack.Class && WildcardUtil.Match(craftingRecipeIngredient.Code, itemStack.Collectible.Code, craftingRecipeIngredient.AllowedVariants) && itemStack.StackSize >= craftingRecipeIngredient.Quantity;
					flag2 &= itemStack.Collectible.MatchesForCrafting(itemStack, this, craftingRecipeIngredient);
					num2++;
				}
				if (!flag2)
				{
					return false;
				}
				list2.RemoveAt(num2 - 1);
				continue;
			}
			ItemStack resolvedItemstack = craftingRecipeIngredient.ResolvedItemstack;
			bool flag3 = false;
			for (int l = 0; l < list.Count; l++)
			{
				if (list[l].Key.Equals(World, resolvedItemstack, GlobalConstants.IgnoredStackAttributes) && craftingRecipeIngredient.RecipeAttributes == null)
				{
					list[l].Key.StackSize += resolvedItemstack.StackSize;
					flag3 = true;
					break;
				}
			}
			if (!flag3)
			{
				list.Add(new KeyValuePair<ItemStack, CraftingRecipeIngredient>(resolvedItemstack.Clone(), craftingRecipeIngredient));
			}
		}
		if (list.Count != list2.Count)
		{
			return false;
		}
		bool flag4 = true;
		int num3 = 0;
		while (flag4 && num3 < list.Count)
		{
			bool flag5 = false;
			int num4 = 0;
			while (!flag5 && num4 < list2.Count)
			{
				flag5 = list[num3].Key.Satisfies(list2[num4]) && list[num3].Key.StackSize <= list2[num4].StackSize && list2[num4].Collectible.MatchesForCrafting(list2[num4], this, list[num3].Value);
				if (flag5)
				{
					list2.RemoveAt(num4);
				}
				num4++;
			}
			flag4 = flag4 && flag5;
			num3++;
		}
		return flag4;
	}

	public bool MatchesAtPosition(int colStart, int rowStart, ItemSlot[] inputSlots, int gridWidth)
	{
		int num = inputSlots.Length / gridWidth;
		for (int i = 0; i < gridWidth; i++)
		{
			for (int j = 0; j < num; j++)
			{
				ItemStack itemStack = GetElementInGrid(j, i, inputSlots, gridWidth)?.Itemstack;
				CraftingRecipeIngredient elementInGrid = GetElementInGrid(j - rowStart, i - colStart, resolvedIngredients, Width);
				if ((itemStack == null) ^ (elementInGrid == null))
				{
					return false;
				}
				if (itemStack != null)
				{
					if (!elementInGrid.SatisfiesAsIngredient(itemStack))
					{
						return false;
					}
					if (!itemStack.Collectible.MatchesForCrafting(itemStack, this, elementInGrid))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public ItemStack GetInputStackForPatternCode(string patternCode, ItemSlot[] inputSlots)
	{
		GridRecipeIngredient gridRecipeIngredient = resolvedIngredients.FirstOrDefault((GridRecipeIngredient ig) => ig?.PatternCode == patternCode);
		if (gridRecipeIngredient == null)
		{
			return null;
		}
		foreach (ItemSlot itemSlot in inputSlots)
		{
			if (!itemSlot.Empty)
			{
				ItemStack itemstack = itemSlot.Itemstack;
				if (itemstack != null && gridRecipeIngredient.SatisfiesAsIngredient(itemstack) && itemstack.Collectible.MatchesForCrafting(itemstack, this, gridRecipeIngredient))
				{
					return itemstack;
				}
			}
		}
		return null;
	}

	public void GenerateOutputStack(ItemSlot[] inputSlots, ItemSlot outputSlot)
	{
		ItemStack itemStack = (outputSlot.Itemstack = Output.ResolvedItemstack.Clone());
		ItemStack itemStack3 = itemStack;
		if (CopyAttributesFrom != null)
		{
			ItemStack inputStackForPatternCode = GetInputStackForPatternCode(CopyAttributesFrom, inputSlots);
			if (inputStackForPatternCode != null)
			{
				ITreeAttribute treeAttribute = inputStackForPatternCode.Attributes.Clone();
				treeAttribute.MergeTree(itemStack3.Attributes);
				itemStack3.Attributes = treeAttribute;
			}
		}
		if (MergeAttributesFrom.Length != 0)
		{
			string[] mergeAttributesFrom = MergeAttributesFrom;
			foreach (string patternCode in mergeAttributesFrom)
			{
				ItemStack inputStackForPatternCode2 = GetInputStackForPatternCode(patternCode, inputSlots);
				if (inputStackForPatternCode2 != null)
				{
					ITreeAttribute treeAttribute2 = inputStackForPatternCode2.Attributes.Clone();
					treeAttribute2.MergeTree(itemStack3.Attributes);
					itemStack3.Attributes = treeAttribute2;
				}
			}
		}
		outputSlot.Itemstack.Collectible.OnCreatedByCrafting(inputSlots, outputSlot, this);
	}

	public T GetElementInGrid<T>(int row, int col, T[] stacks, int gridwidth)
	{
		int num = stacks.Length / gridwidth;
		if (row < 0 || col < 0 || row >= num || col >= gridwidth)
		{
			return default(T);
		}
		return stacks[row * gridwidth + col];
	}

	public int GetGridIndex<T>(int row, int col, T[] stacks, int gridwidth)
	{
		int num = stacks.Length / gridwidth;
		if (row < 0 || col < 0 || row >= num || col >= gridwidth)
		{
			return -1;
		}
		return row * gridwidth + col;
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Width);
		writer.Write(Height);
		Output.ToBytes(writer);
		writer.Write(Shapeless);
		for (int i = 0; i < resolvedIngredients.Length; i++)
		{
			if (resolvedIngredients[i] == null)
			{
				writer.Write(value: true);
				continue;
			}
			writer.Write(value: false);
			resolvedIngredients[i].ToBytes(writer);
		}
		writer.Write(Name.ToShortString());
		writer.Write(Attributes == null);
		if (Attributes != null)
		{
			writer.Write(((object)Attributes.Token).ToString());
		}
		writer.Write(RequiresTrait != null);
		if (RequiresTrait != null)
		{
			writer.Write(RequiresTrait);
		}
		writer.Write(RecipeGroup);
		writer.Write(AverageDurability);
		writer.Write(CopyAttributesFrom != null);
		if (CopyAttributesFrom != null)
		{
			writer.Write(CopyAttributesFrom);
		}
		writer.Write(ShowInCreatedBy);
		writer.Write(Ingredients.Count);
		foreach (KeyValuePair<string, CraftingRecipeIngredient> ingredient in Ingredients)
		{
			writer.Write(ingredient.Key);
			ingredient.Value.ToBytes(writer);
		}
		writer.Write(IngredientPattern);
		writer.Write(AllowedVariants.Count);
		string[] value;
		foreach (KeyValuePair<string, string[]> allowedVariant in AllowedVariants)
		{
			writer.Write(allowedVariant.Key);
			writer.Write(allowedVariant.Value.Length);
			value = allowedVariant.Value;
			foreach (string value2 in value)
			{
				writer.Write(value2);
			}
		}
		writer.Write(SkipVariants.Count);
		foreach (KeyValuePair<string, string[]> skipVariant in SkipVariants)
		{
			writer.Write(skipVariant.Key);
			writer.Write(skipVariant.Value.Length);
			value = skipVariant.Value;
			foreach (string value3 in value)
			{
				writer.Write(value3);
			}
		}
		writer.Write(MergeAttributesFrom.Length);
		value = MergeAttributesFrom;
		foreach (string value4 in value)
		{
			writer.Write(value4);
		}
	}

	public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		Width = reader.ReadInt32();
		Height = reader.ReadInt32();
		Output = new CraftingRecipeIngredient();
		Output.FromBytes(reader, resolver);
		Shapeless = reader.ReadBoolean();
		resolvedIngredients = new GridRecipeIngredient[Width * Height];
		for (int i = 0; i < resolvedIngredients.Length; i++)
		{
			if (!reader.ReadBoolean())
			{
				resolvedIngredients[i] = new GridRecipeIngredient();
				resolvedIngredients[i].FromBytes(reader, resolver);
			}
		}
		Name = new AssetLocation(reader.ReadString());
		if (!reader.ReadBoolean())
		{
			string text = reader.ReadString();
			Attributes = new JsonObject(JToken.Parse(text));
		}
		if (reader.ReadBoolean())
		{
			RequiresTrait = reader.ReadString();
		}
		RecipeGroup = reader.ReadInt32();
		AverageDurability = reader.ReadBoolean();
		if (reader.ReadBoolean())
		{
			CopyAttributesFrom = reader.ReadString();
		}
		ShowInCreatedBy = reader.ReadBoolean();
		int num = reader.ReadInt32();
		Ingredients = new Dictionary<string, CraftingRecipeIngredient>();
		for (int j = 0; j < num; j++)
		{
			string key = reader.ReadString();
			CraftingRecipeIngredient craftingRecipeIngredient = new CraftingRecipeIngredient();
			craftingRecipeIngredient.FromBytes(reader, resolver);
			Ingredients[key] = craftingRecipeIngredient;
		}
		IngredientPattern = reader.ReadString();
		int num2 = reader.ReadInt32();
		AllowedVariants = new Dictionary<string, string[]>();
		for (int k = 0; k < num2; k++)
		{
			string key2 = reader.ReadString();
			int num3 = reader.ReadInt32();
			string[] array = new string[num3];
			for (int l = 0; l < num3; l++)
			{
				array[l] = reader.ReadString();
			}
			AllowedVariants[key2] = array;
		}
		int num4 = reader.ReadInt32();
		SkipVariants = new Dictionary<string, string[]>();
		for (int m = 0; m < num4; m++)
		{
			string key3 = reader.ReadString();
			int num5 = reader.ReadInt32();
			string[] array2 = new string[num5];
			for (int n = 0; n < num5; n++)
			{
				array2[n] = reader.ReadString();
			}
			SkipVariants[key3] = array2;
		}
		int num6 = reader.ReadInt32();
		MergeAttributesFrom = new string[num6];
		for (int num7 = 0; num7 < num6; num7++)
		{
			MergeAttributesFrom[num7] = reader.ReadString();
		}
	}

	public GridRecipe Clone()
	{
		GridRecipe gridRecipe = new GridRecipe();
		gridRecipe.RecipeGroup = RecipeGroup;
		gridRecipe.Width = Width;
		gridRecipe.Height = Height;
		gridRecipe.IngredientPattern = IngredientPattern;
		gridRecipe.Ingredients = new Dictionary<string, CraftingRecipeIngredient>();
		if (Ingredients != null)
		{
			foreach (KeyValuePair<string, CraftingRecipeIngredient> ingredient in Ingredients)
			{
				gridRecipe.Ingredients[ingredient.Key] = ingredient.Value.Clone();
			}
		}
		if (resolvedIngredients != null)
		{
			gridRecipe.resolvedIngredients = new GridRecipeIngredient[resolvedIngredients.Length];
			for (int i = 0; i < resolvedIngredients.Length; i++)
			{
				gridRecipe.resolvedIngredients[i] = resolvedIngredients[i]?.CloneTo<GridRecipeIngredient>();
			}
		}
		gridRecipe.Shapeless = Shapeless;
		gridRecipe.Output = Output.Clone();
		gridRecipe.Name = Name;
		gridRecipe.Attributes = Attributes?.Clone();
		gridRecipe.RequiresTrait = RequiresTrait;
		gridRecipe.AverageDurability = AverageDurability;
		gridRecipe.CopyAttributesFrom = CopyAttributesFrom;
		gridRecipe.ShowInCreatedBy = ShowInCreatedBy;
		gridRecipe.AllowedVariants = new Dictionary<string, string[]>();
		foreach (KeyValuePair<string, string[]> allowedVariant in AllowedVariants)
		{
			gridRecipe.AllowedVariants[allowedVariant.Key] = allowedVariant.Value.ToArray();
		}
		gridRecipe.SkipVariants = new Dictionary<string, string[]>();
		foreach (KeyValuePair<string, string[]> skipVariant in SkipVariants)
		{
			gridRecipe.SkipVariants[skipVariant.Key] = skipVariant.Value.ToArray();
		}
		gridRecipe.MergeAttributesFrom = (string[])MergeAttributesFrom.Clone();
		return gridRecipe;
	}
}
