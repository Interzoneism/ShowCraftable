using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class CookingRecipeIngredient
{
	[DocumentAsJson]
	public required string Code;

	[DocumentAsJson]
	public int MinQuantity;

	[DocumentAsJson]
	public int MaxQuantity;

	[DocumentAsJson]
	public float PortionSizeLitres;

	[DocumentAsJson]
	public string TypeName = "unknown";

	[DocumentAsJson]
	public required CookingRecipeStack[] ValidStacks;

	public IWorldAccessor? world;

	[MemberNotNull(new string[] { "Code", "ValidStacks" })]
	public virtual void FromBytes(BinaryReader reader, IClassRegistryAPI instancer)
	{
		Code = reader.ReadString();
		MinQuantity = reader.ReadInt32();
		MaxQuantity = reader.ReadInt32();
		PortionSizeLitres = reader.ReadSingle();
		TypeName = reader.ReadString();
		int num = reader.ReadInt32();
		ValidStacks = new CookingRecipeStack[num];
		for (int i = 0; i < num; i++)
		{
			ValidStacks[i] = new CookingRecipeStack();
			ValidStacks[i].FromBytes(reader, instancer);
		}
	}

	public virtual void ToBytes(BinaryWriter writer)
	{
		writer.Write(Code);
		writer.Write(MinQuantity);
		writer.Write(MaxQuantity);
		writer.Write(PortionSizeLitres);
		writer.Write(TypeName);
		writer.Write(ValidStacks.Length);
		for (int i = 0; i < ValidStacks.Length; i++)
		{
			ValidStacks[i].ToBytes(writer);
		}
	}

	public CookingRecipeIngredient Clone()
	{
		CookingRecipeIngredient cookingRecipeIngredient = new CookingRecipeIngredient
		{
			Code = Code,
			MinQuantity = MinQuantity,
			MaxQuantity = MaxQuantity,
			PortionSizeLitres = PortionSizeLitres,
			TypeName = TypeName,
			ValidStacks = new CookingRecipeStack[ValidStacks.Length]
		};
		for (int i = 0; i < ValidStacks.Length; i++)
		{
			cookingRecipeIngredient.ValidStacks[i] = ValidStacks[i].Clone();
		}
		return cookingRecipeIngredient;
	}

	public bool Matches(ItemStack inputStack)
	{
		return GetMatchingStack(inputStack) != null;
	}

	public CookingRecipeStack? GetMatchingStack(ItemStack? inputStack)
	{
		if (inputStack == null)
		{
			return null;
		}
		for (int i = 0; i < ValidStacks.Length; i++)
		{
			bool flag = ValidStacks[i].Code.Path.Contains('*');
			if (!flag || !inputStack.Collectible.WildCardMatch(ValidStacks[i].Code))
			{
				string[] array;
				string[] ignoredStackAttributes;
				ReadOnlySpan<string> readOnlySpan;
				int num;
				if (!flag)
				{
					IWorldAccessor? worldForResolve = world;
					ItemStack resolvedItemstack = ValidStacks[i].ResolvedItemstack;
					ignoredStackAttributes = GlobalConstants.IgnoredStackAttributes;
					num = 0;
					array = new string[1 + ignoredStackAttributes.Length];
					readOnlySpan = new ReadOnlySpan<string>(ignoredStackAttributes);
					readOnlySpan.CopyTo(new Span<string>(array).Slice(num, readOnlySpan.Length));
					num += readOnlySpan.Length;
					array[num] = "timeFrozen";
					if (inputStack.Equals(worldForResolve, resolvedItemstack, array))
					{
						goto IL_0142;
					}
				}
				ItemStack itemStack = ValidStacks[i].CookedStack?.ResolvedItemstack;
				if (itemStack == null)
				{
					continue;
				}
				IWorldAccessor? worldForResolve2 = world;
				array = GlobalConstants.IgnoredStackAttributes;
				num = 0;
				ignoredStackAttributes = new string[1 + array.Length];
				readOnlySpan = new ReadOnlySpan<string>(array);
				readOnlySpan.CopyTo(new Span<string>(ignoredStackAttributes).Slice(num, readOnlySpan.Length));
				num += readOnlySpan.Length;
				ignoredStackAttributes[num] = "timeFrozen";
				if (!inputStack.Equals(worldForResolve2, itemStack, ignoredStackAttributes))
				{
					continue;
				}
			}
			goto IL_0142;
			IL_0142:
			return ValidStacks[i];
		}
		return null;
	}

	internal void Resolve(IWorldAccessor world, string sourceForErrorLogging)
	{
		this.world = world;
		List<CookingRecipeStack> list = new List<CookingRecipeStack>();
		for (int i = 0; i < ValidStacks.Length; i++)
		{
			CookingRecipeStack cookingRecipeStack = ValidStacks[i];
			if (cookingRecipeStack.Code.Path.Contains('*'))
			{
				list.Add(cookingRecipeStack);
				continue;
			}
			if (cookingRecipeStack.Resolve(world, sourceForErrorLogging))
			{
				list.Add(cookingRecipeStack);
			}
			cookingRecipeStack.CookedStack?.Resolve(world, sourceForErrorLogging);
		}
		ValidStacks = list.ToArray();
	}
}
