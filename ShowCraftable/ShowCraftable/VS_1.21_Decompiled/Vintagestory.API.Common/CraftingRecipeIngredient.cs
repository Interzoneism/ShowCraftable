using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class CraftingRecipeIngredient : IRecipeIngredient
{
	[DocumentAsJson]
	public EnumItemClass Type;

	[DocumentAsJson]
	public int Quantity = 1;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject Attributes;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject RecipeAttributes;

	[DocumentAsJson]
	public bool IsTool;

	[DocumentAsJson]
	public int ToolDurabilityCost = 1;

	[DocumentAsJson]
	public string[] AllowedVariants;

	[DocumentAsJson]
	public string[] SkipVariants;

	[DocumentAsJson]
	public JsonItemStack ReturnedStack;

	public ItemStack ResolvedItemstack;

	public bool IsWildCard;

	public bool IsBasicWildCard;

	public bool IsAdvancedWildCard;

	public bool IsRegex;

	[DocumentAsJson]
	public AssetLocation Code { get; set; }

	[DocumentAsJson]
	public string Name { get; set; }

	public bool Resolve(IWorldAccessor resolver, string sourceForErrorLogging)
	{
		if (ReturnedStack != null)
		{
			ReturnedStack.Resolve(resolver, sourceForErrorLogging + " recipe with output ", Code);
		}
		if (IsBasicWildCard || IsAdvancedWildCard || IsRegex)
		{
			IsWildCard = true;
			return true;
		}
		if (Code.Path.Contains('*'))
		{
			IsWildCard = true;
			return true;
		}
		if (Type == EnumItemClass.Block)
		{
			Block block = resolver.GetBlock(Code);
			if (block == null || block.IsMissing)
			{
				resolver.Logger.Warning("Failed resolving crafting recipe ingredient with code {0} in {1}", Code, sourceForErrorLogging);
				return false;
			}
			ResolvedItemstack = new ItemStack(block, Quantity);
		}
		else
		{
			Item item = resolver.GetItem(Code);
			if (item == null || item.IsMissing)
			{
				resolver.Logger.Warning("Failed resolving crafting recipe ingredient with code {0} in {1}", Code, sourceForErrorLogging);
				return false;
			}
			ResolvedItemstack = new ItemStack(item, Quantity);
		}
		if (Attributes != null)
		{
			IAttribute attribute = Attributes.ToAttribute();
			if (attribute is ITreeAttribute)
			{
				ResolvedItemstack.Attributes = (ITreeAttribute)attribute;
			}
		}
		return true;
	}

	public bool SatisfiesAsIngredient(ItemStack inputStack, bool checkStacksize = true)
	{
		if (inputStack == null)
		{
			return false;
		}
		if (IsWildCard)
		{
			if (Type != inputStack.Class)
			{
				return false;
			}
			if (!WildcardUtil.Match(Code, inputStack.Collectible.Code, AllowedVariants))
			{
				return false;
			}
			if (SkipVariants != null && WildcardUtil.Match(Code, inputStack.Collectible.Code, SkipVariants))
			{
				return false;
			}
			if (checkStacksize && inputStack.StackSize < Quantity)
			{
				return false;
			}
		}
		else
		{
			if (!ResolvedItemstack.Satisfies(inputStack))
			{
				return false;
			}
			if (checkStacksize && inputStack.StackSize < ResolvedItemstack.StackSize)
			{
				return false;
			}
		}
		return true;
	}

	public CraftingRecipeIngredient Clone()
	{
		return CloneTo<CraftingRecipeIngredient>();
	}

	public T CloneTo<T>() where T : CraftingRecipeIngredient, new()
	{
		T val = new T
		{
			Code = Code.Clone(),
			Type = Type,
			Name = Name,
			Quantity = Quantity,
			IsWildCard = IsWildCard,
			IsBasicWildCard = IsBasicWildCard,
			IsAdvancedWildCard = IsAdvancedWildCard,
			IsRegex = IsRegex,
			IsTool = IsTool,
			ToolDurabilityCost = ToolDurabilityCost,
			AllowedVariants = ((AllowedVariants == null) ? null : ((string[])AllowedVariants.Clone())),
			SkipVariants = ((SkipVariants == null) ? null : ((string[])SkipVariants.Clone())),
			ResolvedItemstack = ResolvedItemstack?.Clone(),
			ReturnedStack = ReturnedStack?.Clone(),
			RecipeAttributes = RecipeAttributes?.Clone()
		};
		if (Attributes != null)
		{
			val.Attributes = Attributes.Clone();
		}
		return val;
	}

	public override string ToString()
	{
		return Type.ToString() + " code " + Code;
	}

	public void FillPlaceHolder(string key, string value)
	{
		Code = Code.CopyWithPath(Code.Path.Replace("{" + key + "}", value));
		Attributes?.FillPlaceHolder(key, value);
		RecipeAttributes?.FillPlaceHolder(key, value);
	}

	public virtual void ToBytes(BinaryWriter writer)
	{
		writer.Write(IsWildCard);
		writer.Write(IsBasicWildCard);
		writer.Write(IsAdvancedWildCard);
		writer.Write(IsRegex);
		writer.Write((int)Type);
		writer.Write(Code.ToShortString());
		writer.Write(Quantity);
		if (!IsWildCard)
		{
			writer.Write(ResolvedItemstack != null);
			ResolvedItemstack?.ToBytes(writer);
		}
		writer.Write(IsTool);
		writer.Write(ToolDurabilityCost);
		writer.Write(AllowedVariants != null);
		if (AllowedVariants != null)
		{
			writer.Write(AllowedVariants.Length);
			for (int i = 0; i < AllowedVariants.Length; i++)
			{
				writer.Write(AllowedVariants[i]);
			}
		}
		writer.Write(SkipVariants != null);
		if (SkipVariants != null)
		{
			writer.Write(SkipVariants.Length);
			for (int j = 0; j < SkipVariants.Length; j++)
			{
				writer.Write(SkipVariants[j]);
			}
		}
		writer.Write(ReturnedStack?.ResolvedItemstack != null);
		if (ReturnedStack?.ResolvedItemstack != null)
		{
			ReturnedStack.ToBytes(writer);
		}
		if (RecipeAttributes != null)
		{
			writer.Write(value: true);
			writer.Write(RecipeAttributes.ToString());
		}
		else
		{
			writer.Write(value: false);
		}
	}

	public virtual void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		IsWildCard = reader.ReadBoolean();
		IsBasicWildCard = reader.ReadBoolean();
		IsAdvancedWildCard = reader.ReadBoolean();
		IsRegex = reader.ReadBoolean();
		Type = (EnumItemClass)reader.ReadInt32();
		Code = new AssetLocation(reader.ReadString());
		Quantity = reader.ReadInt32();
		if (!IsWildCard && reader.ReadBoolean())
		{
			ResolvedItemstack = new ItemStack(reader, resolver);
		}
		IsTool = reader.ReadBoolean();
		ToolDurabilityCost = reader.ReadInt32();
		if (reader.ReadBoolean())
		{
			AllowedVariants = new string[reader.ReadInt32()];
			for (int i = 0; i < AllowedVariants.Length; i++)
			{
				AllowedVariants[i] = reader.ReadString();
			}
		}
		if (reader.ReadBoolean())
		{
			SkipVariants = new string[reader.ReadInt32()];
			for (int j = 0; j < SkipVariants.Length; j++)
			{
				SkipVariants[j] = reader.ReadString();
			}
		}
		if (reader.ReadBoolean())
		{
			ReturnedStack = new JsonItemStack();
			ReturnedStack.FromBytes(reader, resolver.ClassRegistry);
			ReturnedStack.ResolvedItemstack.ResolveBlockOrItem(resolver);
		}
		if (reader.ReadBoolean())
		{
			RecipeAttributes = new JsonObject(JToken.Parse(reader.ReadString()));
		}
	}
}
