using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class AlloyRecipe : IByteSerializable
{
	[DocumentAsJson]
	public MetalAlloyIngredient[] Ingredients;

	[DocumentAsJson]
	public JsonItemStack Output;

	[DocumentAsJson]
	public bool Enabled = true;

	public bool Matches(ItemStack[] inputStacks, bool useSmeltedWhereApplicable = true)
	{
		List<MatchedSmeltableStackAlloy> list = mergeAndCompareStacks(inputStacks, useSmeltedWhereApplicable);
		if (list == null)
		{
			return false;
		}
		double num = 0.0;
		foreach (MatchedSmeltableStackAlloy item in list)
		{
			num += item.stackSize;
		}
		foreach (MatchedSmeltableStackAlloy item2 in list)
		{
			int num2 = (int)Math.Round(item2.stackSize / num * 10000.0);
			int num3 = (int)Math.Round(item2.ingred.MinRatio * 10000f);
			int num4 = (int)Math.Round(item2.ingred.MaxRatio * 10000f);
			if (num2 < num3 || num2 > num4)
			{
				return false;
			}
		}
		return true;
	}

	public void Resolve(IServerWorldAccessor world, string sourceForErrorLogging)
	{
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i].Resolve(world, sourceForErrorLogging);
		}
		Output.Resolve(world, sourceForErrorLogging);
	}

	public double GetTotalOutputQuantity(ItemStack[] stacks, bool useSmeltedWhereAppicable = true)
	{
		List<MatchedSmeltableStackAlloy> list = mergeAndCompareStacks(stacks, useSmeltedWhereAppicable);
		if (list == null)
		{
			return 0.0;
		}
		double num = 0.0;
		foreach (MatchedSmeltableStackAlloy item in list)
		{
			num += item.stackSize;
		}
		return num;
	}

	private List<MatchedSmeltableStackAlloy> mergeAndCompareStacks(ItemStack[] inputStacks, bool useSmeltedWhereApplicable)
	{
		List<MatchedSmeltableStackAlloy> list = new List<MatchedSmeltableStackAlloy>();
		List<MetalAlloyIngredient> list2 = new List<MetalAlloyIngredient>(Ingredients);
		for (int i = 0; i < inputStacks.Length; i++)
		{
			if (inputStacks[i] == null)
			{
				continue;
			}
			ItemStack itemStack = inputStacks[i];
			float num = itemStack.StackSize;
			if (useSmeltedWhereApplicable && itemStack.Collectible.CombustibleProps?.SmeltedStack != null)
			{
				num /= (float)itemStack.Collectible.CombustibleProps.SmeltedRatio;
				itemStack = itemStack.Collectible.CombustibleProps.SmeltedStack.ResolvedItemstack;
			}
			bool flag = false;
			for (int j = 0; j < list.Count; j++)
			{
				if (itemStack.Class == list[j].stack.Class && itemStack.Id == list[j].stack.Id)
				{
					list[j].stackSize = Math.Round(list[j].stackSize + (double)num, 2);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				MetalAlloyIngredient igrendientFor = getIgrendientFor(itemStack, list2);
				if (igrendientFor == null)
				{
					return null;
				}
				list.Add(new MatchedSmeltableStackAlloy
				{
					stack = itemStack.Clone(),
					ingred = igrendientFor,
					stackSize = num
				});
			}
		}
		if (list2.Count > 0)
		{
			return null;
		}
		return list;
	}

	private MetalAlloyIngredient getIgrendientFor(ItemStack stack, List<MetalAlloyIngredient> ingredients)
	{
		if (stack == null)
		{
			return null;
		}
		for (int i = 0; i < ingredients.Count; i++)
		{
			ItemStack resolvedItemstack = ingredients[i].ResolvedItemstack;
			if (resolvedItemstack.Class == stack.Class && resolvedItemstack.Id == stack.Id)
			{
				MetalAlloyIngredient result = ingredients[i];
				ingredients.Remove(ingredients[i]);
				return result;
			}
		}
		return null;
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Ingredients.Length);
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i].ToBytes(writer);
		}
		Output.ToBytes(writer);
	}

	public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		Ingredients = new MetalAlloyIngredient[reader.ReadInt32()];
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i] = new MetalAlloyIngredient();
			Ingredients[i].FromBytes(reader, resolver.ClassRegistry);
			Ingredients[i].Resolve(resolver, "[FromBytes]");
		}
		Output = new JsonItemStack();
		Output.FromBytes(reader, resolver.ClassRegistry);
		Output.Resolve(resolver, "[FromBytes]");
	}
}
