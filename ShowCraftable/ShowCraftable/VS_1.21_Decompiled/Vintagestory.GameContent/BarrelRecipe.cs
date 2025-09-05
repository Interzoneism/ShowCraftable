using System.Collections.Generic;
using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BarrelRecipe : IByteSerializable, IRecipeBase<BarrelRecipe>
{
	[DocumentAsJson]
	public int RecipeId;

	[DocumentAsJson]
	public BarrelRecipeIngredient[] Ingredients;

	[DocumentAsJson]
	public BarrelOutputStack Output;

	[DocumentAsJson]
	public string Code;

	[DocumentAsJson]
	public double SealHours;

	[DocumentAsJson]
	public AssetLocation Name { get; set; }

	[DocumentAsJson]
	public bool Enabled { get; set; } = true;

	IRecipeIngredient[] IRecipeBase<BarrelRecipe>.Ingredients => Ingredients;

	IRecipeOutput IRecipeBase<BarrelRecipe>.Output => Output;

	public bool Matches(ItemSlot[] inputSlots, out int outputStackSize)
	{
		outputStackSize = 0;
		List<KeyValuePair<ItemSlot, BarrelRecipeIngredient>> list = pairInput(inputSlots);
		if (list == null)
		{
			return false;
		}
		outputStackSize = getOutputSize(list);
		return outputStackSize >= 0;
	}

	private List<KeyValuePair<ItemSlot, BarrelRecipeIngredient>> pairInput(ItemSlot[] inputStacks)
	{
		List<BarrelRecipeIngredient> list = new List<BarrelRecipeIngredient>(Ingredients);
		Queue<ItemSlot> queue = new Queue<ItemSlot>();
		foreach (ItemSlot itemSlot in inputStacks)
		{
			if (!itemSlot.Empty)
			{
				queue.Enqueue(itemSlot);
			}
		}
		if (queue.Count != Ingredients.Length)
		{
			return null;
		}
		List<KeyValuePair<ItemSlot, BarrelRecipeIngredient>> list2 = new List<KeyValuePair<ItemSlot, BarrelRecipeIngredient>>();
		while (queue.Count > 0)
		{
			ItemSlot itemSlot2 = queue.Dequeue();
			bool flag = false;
			for (int j = 0; j < list.Count; j++)
			{
				BarrelRecipeIngredient barrelRecipeIngredient = list[j];
				if (barrelRecipeIngredient.SatisfiesAsIngredient(itemSlot2.Itemstack))
				{
					list2.Add(new KeyValuePair<ItemSlot, BarrelRecipeIngredient>(itemSlot2, barrelRecipeIngredient));
					flag = true;
					list.RemoveAt(j);
					break;
				}
			}
			if (!flag)
			{
				return null;
			}
		}
		if (list.Count > 0)
		{
			return null;
		}
		return list2;
	}

	private int getOutputSize(List<KeyValuePair<ItemSlot, BarrelRecipeIngredient>> matched)
	{
		int num = -1;
		foreach (KeyValuePair<ItemSlot, BarrelRecipeIngredient> item in matched)
		{
			ItemSlot key = item.Key;
			BarrelRecipeIngredient value = item.Value;
			if (!value.ConsumeQuantity.HasValue)
			{
				num = key.StackSize / value.Quantity;
			}
		}
		if (num == -1)
		{
			return -1;
		}
		foreach (KeyValuePair<ItemSlot, BarrelRecipeIngredient> item2 in matched)
		{
			ItemSlot key2 = item2.Key;
			BarrelRecipeIngredient value2 = item2.Value;
			if (!value2.ConsumeQuantity.HasValue)
			{
				if (key2.StackSize % value2.Quantity != 0)
				{
					return -1;
				}
				if (num != key2.StackSize / value2.Quantity)
				{
					return -1;
				}
			}
			else if (key2.StackSize < value2.Quantity * num)
			{
				return -1;
			}
		}
		return Output.StackSize * num;
	}

	public bool TryCraftNow(ICoreAPI api, double nowSealedHours, ItemSlot[] inputslots)
	{
		if (SealHours > 0.0 && nowSealedHours < SealHours)
		{
			return false;
		}
		List<KeyValuePair<ItemSlot, BarrelRecipeIngredient>> list = pairInput(inputslots);
		ItemStack itemStack = Output.ResolvedItemstack.Clone();
		itemStack.StackSize = getOutputSize(list);
		if (itemStack.StackSize < 0)
		{
			return false;
		}
		TransitionableProperties[] transitionableProperties = itemStack.Collectible.GetTransitionableProperties(api.World, itemStack, null);
		TransitionableProperties transitionableProperties2 = ((transitionableProperties != null && transitionableProperties.Length != 0) ? transitionableProperties[0] : null);
		if (transitionableProperties2 != null)
		{
			CollectibleObject.CarryOverFreshness(api, inputslots, new ItemStack[1] { itemStack }, transitionableProperties2);
		}
		ItemStack itemStack2 = null;
		foreach (KeyValuePair<ItemSlot, BarrelRecipeIngredient> item in list)
		{
			if (item.Value.ConsumeQuantity.HasValue)
			{
				itemStack2 = item.Key.Itemstack;
				itemStack2.StackSize -= item.Value.ConsumeQuantity.Value * (itemStack.StackSize / Output.StackSize);
				if (itemStack2.StackSize <= 0)
				{
					itemStack2 = null;
				}
				break;
			}
		}
		if (shouldBeInLiquidSlot(itemStack))
		{
			inputslots[0].Itemstack = itemStack2;
			inputslots[1].Itemstack = itemStack;
		}
		else
		{
			inputslots[1].Itemstack = itemStack2;
			inputslots[0].Itemstack = itemStack;
		}
		inputslots[0].MarkDirty();
		inputslots[1].MarkDirty();
		return true;
	}

	public bool shouldBeInLiquidSlot(ItemStack stack)
	{
		if (stack == null)
		{
			return false;
		}
		return stack.ItemAttributes?["waterTightContainerProps"].Exists == true;
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Code);
		writer.Write(Ingredients.Length);
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i].ToBytes(writer);
		}
		Output.ToBytes(writer);
		writer.Write(SealHours);
	}

	public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		Code = reader.ReadString();
		Ingredients = new BarrelRecipeIngredient[reader.ReadInt32()];
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i] = new BarrelRecipeIngredient();
			Ingredients[i].FromBytes(reader, resolver);
			Ingredients[i].Resolve(resolver, "Barrel Recipe (FromBytes)");
		}
		Output = new BarrelOutputStack();
		Output.FromBytes(reader, resolver.ClassRegistry);
		Output.Resolve(resolver, "Barrel Recipe (FromBytes)");
		SealHours = reader.ReadDouble();
	}

	public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
	{
		Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();
		if (Ingredients == null || Ingredients.Length == 0)
		{
			return dictionary;
		}
		BarrelRecipeIngredient[] ingredients = Ingredients;
		foreach (BarrelRecipeIngredient barrelRecipeIngredient in ingredients)
		{
			if (!barrelRecipeIngredient.Code.Path.Contains('*'))
			{
				continue;
			}
			int num = barrelRecipeIngredient.Code.Path.IndexOf('*');
			int num2 = barrelRecipeIngredient.Code.Path.Length - num - 1;
			List<string> list = new List<string>();
			if (barrelRecipeIngredient.Type == EnumItemClass.Block)
			{
				foreach (Block block in world.Blocks)
				{
					if (!block.IsMissing && WildcardUtil.Match(barrelRecipeIngredient.Code, block.Code))
					{
						string text = block.Code.Path.Substring(num);
						string text2 = text.Substring(0, text.Length - num2);
						if (barrelRecipeIngredient.AllowedVariants == null || barrelRecipeIngredient.AllowedVariants.Contains(text2))
						{
							list.Add(text2);
						}
					}
				}
			}
			else
			{
				foreach (Item item in world.Items)
				{
					if (!(item.Code == null) && !item.IsMissing && WildcardUtil.Match(barrelRecipeIngredient.Code, item.Code))
					{
						string text3 = item.Code.Path.Substring(num);
						string text4 = text3.Substring(0, text3.Length - num2);
						if (barrelRecipeIngredient.AllowedVariants == null || barrelRecipeIngredient.AllowedVariants.Contains(text4))
						{
							list.Add(text4);
						}
					}
				}
			}
			dictionary[barrelRecipeIngredient.Name ?? ("wildcard" + dictionary.Count)] = list.ToArray();
		}
		return dictionary;
	}

	public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
	{
		bool flag = true;
		for (int i = 0; i < Ingredients.Length; i++)
		{
			BarrelRecipeIngredient barrelRecipeIngredient = Ingredients[i];
			bool flag2 = barrelRecipeIngredient.Resolve(world, sourceForErrorLogging);
			flag = flag && flag2;
			if (!flag2)
			{
				continue;
			}
			WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(barrelRecipeIngredient.ResolvedItemstack);
			if (containableProps == null)
			{
				continue;
			}
			if (barrelRecipeIngredient.Litres < 0f)
			{
				if (barrelRecipeIngredient.Quantity > 0)
				{
					world.Logger.Warning("Barrel recipe {0}, ingredient {1} does not define a litres attribute but a quantity, will assume quantity=litres for backwards compatibility.", sourceForErrorLogging, barrelRecipeIngredient.Code);
					barrelRecipeIngredient.Litres = barrelRecipeIngredient.Quantity;
					barrelRecipeIngredient.ConsumeLitres = barrelRecipeIngredient.ConsumeQuantity;
				}
				else
				{
					barrelRecipeIngredient.Litres = 1f;
				}
			}
			barrelRecipeIngredient.Quantity = (int)(containableProps.ItemsPerLitre * barrelRecipeIngredient.Litres);
			if (barrelRecipeIngredient.ConsumeLitres.HasValue)
			{
				barrelRecipeIngredient.ConsumeQuantity = (int)(containableProps.ItemsPerLitre * barrelRecipeIngredient.ConsumeLitres).Value;
			}
		}
		flag &= Output.Resolve(world, sourceForErrorLogging);
		if (flag)
		{
			WaterTightContainableProps containableProps2 = BlockLiquidContainerBase.GetContainableProps(Output.ResolvedItemstack);
			if (containableProps2 != null)
			{
				if (Output.Litres < 0f)
				{
					if (Output.Quantity > 0)
					{
						world.Logger.Warning("Barrel recipe {0}, output {1} does not define a litres attribute but a stacksize, will assume stacksize=litres for backwards compatibility.", sourceForErrorLogging, Output.Code);
						Output.Litres = Output.Quantity;
					}
					else
					{
						Output.Litres = 1f;
					}
				}
				Output.Quantity = (int)(containableProps2.ItemsPerLitre * Output.Litres);
			}
		}
		return flag;
	}

	public BarrelRecipe Clone()
	{
		BarrelRecipeIngredient[] array = new BarrelRecipeIngredient[Ingredients.Length];
		for (int i = 0; i < Ingredients.Length; i++)
		{
			array[i] = Ingredients[i].Clone();
		}
		return new BarrelRecipe
		{
			SealHours = SealHours,
			Output = Output.Clone(),
			Code = Code,
			Enabled = Enabled,
			Name = Name,
			RecipeId = RecipeId,
			Ingredients = array
		};
	}
}
