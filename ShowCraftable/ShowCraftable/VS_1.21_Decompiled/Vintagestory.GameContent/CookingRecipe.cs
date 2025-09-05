using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class CookingRecipe : IByteSerializable
{
	[DocumentAsJson]
	public string? Code;

	[DocumentAsJson]
	public CookingRecipeIngredient[]? Ingredients;

	[DocumentAsJson]
	public bool Enabled = true;

	[DocumentAsJson]
	public CompositeShape? Shape;

	[DocumentAsJson]
	public TransitionableProperties? PerishableProps;

	[DocumentAsJson]
	public JsonItemStack? CooksInto;

	[DocumentAsJson]
	public bool IsFood;

	public static Dictionary<string, ICookingRecipeNamingHelper> NamingRegistry = new Dictionary<string, ICookingRecipeNamingHelper>();

	public bool Matches(ItemStack?[] inputStacks)
	{
		int quantityServings = 0;
		return Matches(inputStacks, ref quantityServings);
	}

	public int GetQuantityServings(ItemStack[] stacks)
	{
		int quantityServings = 0;
		Matches(stacks, ref quantityServings);
		return quantityServings;
	}

	public string GetOutputName(IWorldAccessor worldForResolve, ItemStack[] inputStacks)
	{
		if (inputStacks.Any((ItemStack stack) => stack?.Collectible.Code.Path == "rot"))
		{
			return Lang.Get("Rotten Food");
		}
		if (NamingRegistry.TryGetValue(Code, out ICookingRecipeNamingHelper value))
		{
			return value.GetNameForIngredients(worldForResolve, Code, inputStacks);
		}
		return new VanillaCookingRecipeNames().GetNameForIngredients(worldForResolve, Code, inputStacks);
	}

	public bool Matches(ItemStack?[] inputStacks, ref int quantityServings)
	{
		if (Ingredients == null)
		{
			return false;
		}
		List<ItemStack> list = inputStacks.ToList();
		List<CookingRecipeIngredient> list2 = Ingredients.ToList();
		int num = 99999;
		int[] array = new int[list2.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = 0;
		}
		while (list.Count > 0)
		{
			ItemStack itemStack = list[0];
			list.RemoveAt(0);
			if (itemStack == null)
			{
				continue;
			}
			bool flag = false;
			for (int j = 0; j < list2.Count; j++)
			{
				CookingRecipeIngredient cookingRecipeIngredient = list2[j];
				CookingRecipeStack matchingStack = cookingRecipeIngredient.GetMatchingStack(itemStack);
				if (matchingStack != null && array[j] < cookingRecipeIngredient.MaxQuantity)
				{
					int val = itemStack.StackSize / matchingStack.StackSize;
					WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(itemStack);
					if (containableProps != null)
					{
						val = (int)((float)(itemStack.StackSize / matchingStack.StackSize) / containableProps.ItemsPerLitre / cookingRecipeIngredient.PortionSizeLitres);
					}
					num = Math.Min(num, val);
					array[j]++;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		for (int k = 0; k < list2.Count; k++)
		{
			if (array[k] < list2[k].MinQuantity)
			{
				return false;
			}
		}
		quantityServings = num;
		foreach (ItemStack itemStack2 in inputStacks)
		{
			if (itemStack2 == null)
			{
				continue;
			}
			int num2 = GetIngrendientFor(itemStack2)?.GetMatchingStack(itemStack2)?.StackSize ?? 1;
			WaterTightContainableProps containableProps2 = BlockLiquidContainerBase.GetContainableProps(itemStack2);
			if (containableProps2 != null)
			{
				if (itemStack2.StackSize / num2 != (int)((float)quantityServings * containableProps2.ItemsPerLitre * (GetIngrendientFor(itemStack2)?.PortionSizeLitres ?? 100f)))
				{
					quantityServings = -1;
				}
			}
			else if (itemStack2.StackSize / num2 != quantityServings)
			{
				quantityServings = -1;
			}
			if (quantityServings == -1)
			{
				return false;
			}
		}
		return true;
	}

	public CookingRecipeIngredient? GetIngrendientFor(ItemStack? stack, params CookingRecipeIngredient[] ingredsToskip)
	{
		if (stack == null)
		{
			return null;
		}
		for (int i = 0; i < Ingredients.Length; i++)
		{
			if (Ingredients[i].Matches(stack) && !ingredsToskip.Contains<CookingRecipeIngredient>(Ingredients[i]))
			{
				return Ingredients[i];
			}
		}
		return null;
	}

	public void Resolve(IServerWorldAccessor world, string sourceForErrorLogging)
	{
		if (Ingredients != null)
		{
			for (int i = 0; i < Ingredients.Length; i++)
			{
				Ingredients[i].Resolve(world, sourceForErrorLogging);
			}
			CooksInto?.Resolve(world, sourceForErrorLogging);
		}
	}

	public ItemStack?[] GenerateRandomMeal(ICoreAPI api, ref Dictionary<CookingRecipeIngredient, HashSet<ItemStack?>>? cachedValidStacksByIngredient, ItemStack[] allstacks, int slots = 4, ItemStack? ingredientStack = null)
	{
		if (Ingredients == null)
		{
			return new ItemStack[slots];
		}
		Dictionary<CookingRecipeIngredient, HashSet<ItemStack>> dictionary = cachedValidStacksByIngredient;
		if (cachedValidStacksByIngredient == null)
		{
			dictionary = new Dictionary<CookingRecipeIngredient, HashSet<ItemStack>>();
			CookingRecipeIngredient[] ingredients = Ingredients;
			foreach (CookingRecipeIngredient cookingRecipeIngredient in ingredients)
			{
				HashSet<ItemStack> hashSet = new HashSet<ItemStack>();
				new List<AssetLocation>();
				cookingRecipeIngredient.Resolve(api.World, "handbook meal recipes");
				foreach (ItemStack itemStack in allstacks)
				{
					CookingRecipeStack matchingStack = cookingRecipeIngredient.GetMatchingStack(itemStack);
					if (matchingStack != null)
					{
						ItemStack itemStack2 = itemStack.Clone();
						itemStack2.StackSize = matchingStack.StackSize;
						WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(itemStack2);
						if (containableProps != null)
						{
							itemStack2.StackSize = (int)(containableProps.ItemsPerLitre * cookingRecipeIngredient.PortionSizeLitres);
						}
						hashSet.Add(itemStack2);
					}
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
			return new ItemStack[slots];
		}
		List<ItemStack> list = new List<ItemStack>();
		while (!Matches(list.ToArray()))
		{
			Dictionary<CookingRecipeIngredient, List<ItemStack>> dictionary2 = new Dictionary<CookingRecipeIngredient, List<ItemStack>>();
			foreach (KeyValuePair<CookingRecipeIngredient, HashSet<ItemStack>> item in dictionary)
			{
				dictionary2.Add(item.Key.Clone(), item.Value.ToList());
			}
			dictionary2 = dictionary2.OrderBy((KeyValuePair<CookingRecipeIngredient, List<ItemStack>> x) => api.World.Rand.Next()).ToDictionary((KeyValuePair<CookingRecipeIngredient, List<ItemStack>> item) => item.Key, (KeyValuePair<CookingRecipeIngredient, List<ItemStack>> item) => item.Value);
			CookingRecipeIngredient cookingRecipeIngredient2 = null;
			if (ingredientStack != null)
			{
				List<CookingRecipeIngredient> list2 = Ingredients.Where((CookingRecipeIngredient ingredient) => ingredient.Matches(ingredientStack)).ToList();
				cookingRecipeIngredient2 = list2[api.World.Rand.Next(list2.Count)].Clone();
			}
			list = new List<ItemStack>();
			foreach (KeyValuePair<CookingRecipeIngredient, List<ItemStack>> item2 in dictionary2.Where((KeyValuePair<CookingRecipeIngredient, List<ItemStack>> entry) => entry.Key.MinQuantity > 0))
			{
				CookingRecipeIngredient key = item2.Key;
				List<ItemStack> value = item2.Value;
				if (key.Code == cookingRecipeIngredient2?.Code)
				{
					ItemStack itemStack3 = value.First((ItemStack stack) => stack?.Collectible.Code == ingredientStack?.Collectible.Code);
					if (itemStack3 != null)
					{
						list.Add(itemStack3.Clone());
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
				if (key.MaxQuantity > 0)
				{
					value.Add(null);
				}
				else
				{
					dictionary2.Remove(key);
				}
			}
			int num = slots - list.Count;
			int num2 = 0;
			if (cookingRecipeIngredient2 != null)
			{
				num2 = api.World.Rand.Next(num) + 1;
			}
			while (num > 0)
			{
				if (api.World.Rand.NextDouble() > 0.25 || num == num2)
				{
					dictionary2 = dictionary2.OrderBy((KeyValuePair<CookingRecipeIngredient, List<ItemStack>> x) => api.World.Rand.Next()).ToDictionary((KeyValuePair<CookingRecipeIngredient, List<ItemStack>> item) => item.Key, (KeyValuePair<CookingRecipeIngredient, List<ItemStack>> item) => item.Value);
					foreach (KeyValuePair<CookingRecipeIngredient, List<ItemStack>> item3 in dictionary2)
					{
						CookingRecipeIngredient key2 = item3.Key;
						List<ItemStack> value2 = item3.Value;
						if (num == num2)
						{
							if (cookingRecipeIngredient2 != null && key2.Code != cookingRecipeIngredient2?.Code)
							{
								continue;
							}
							if (key2.Code == cookingRecipeIngredient2?.Code)
							{
								ItemStack itemStack4 = value2.First((ItemStack stack) => stack?.Collectible.Code == ingredientStack?.Collectible.Code);
								if (itemStack4 != null)
								{
									list.Add(itemStack4.Clone());
									key2.MaxQuantity--;
									cookingRecipeIngredient2 = null;
									break;
								}
							}
						}
						if (key2.MaxQuantity > 0 && api.World.Rand.NextDouble() < 0.5)
						{
							ItemStack itemStack5 = value2[api.World.Rand.Next(value2.Count)];
							if (itemStack5 != null)
							{
								list.Add(itemStack5.Clone());
								key2.MaxQuantity--;
								break;
							}
						}
					}
				}
				num--;
			}
		}
		list.Shuffle(api.World.Rand);
		while (list.Count < slots)
		{
			list.Add(null);
		}
		return list.ToArray();
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Code);
		writer.Write(Ingredients.Length);
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i].ToBytes(writer);
		}
		writer.Write(Shape == null);
		if (Shape != null)
		{
			writer.Write(Shape.Base.ToString());
		}
		PerishableProps.ToBytes(writer);
		writer.Write(CooksInto != null);
		if (CooksInto != null)
		{
			CooksInto.ToBytes(writer);
		}
		writer.Write(IsFood);
	}

	public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		Code = reader.ReadString();
		Ingredients = new CookingRecipeIngredient[reader.ReadInt32()];
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i] = new CookingRecipeIngredient
			{
				Code = null,
				ValidStacks = null
			};
			Ingredients[i].FromBytes(reader, resolver.ClassRegistry);
			Ingredients[i].Resolve(resolver, "[FromBytes]");
		}
		if (!reader.ReadBoolean())
		{
			Shape = new CompositeShape
			{
				Base = new AssetLocation(reader.ReadString())
			};
		}
		PerishableProps = new TransitionableProperties();
		PerishableProps.FromBytes(reader, resolver.ClassRegistry);
		if (reader.ReadBoolean())
		{
			CooksInto = new JsonItemStack();
			CooksInto.FromBytes(reader, resolver.ClassRegistry);
			CooksInto.Resolve(resolver, "[FromBytes]");
		}
		IsFood = reader.ReadBoolean();
	}
}
