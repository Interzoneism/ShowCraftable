using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public abstract class LayeredVoxelRecipe<T> : RecipeBase<T>
{
	[DocumentAsJson]
	public string[][] Pattern;

	public bool[,,] Voxels;

	public abstract int QuantityLayers { get; }

	public abstract string RecipeCategoryCode { get; }

	protected virtual bool RotateRecipe { get; set; }

	public LayeredVoxelRecipe()
	{
		Voxels = new bool[16, QuantityLayers, 16];
	}

	public override bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
	{
		if (Pattern == null || base.Ingredient == null || Output == null)
		{
			world.Logger.Error("{1} Recipe with output {0} has no ingredient pattern or missing ingredient/output. Ignoring recipe.", Output, RecipeCategoryCode);
			return false;
		}
		if (!base.Ingredient.Resolve(world, RecipeCategoryCode + " recipe"))
		{
			world.Logger.Error("{1} Recipe with output {0}: Cannot resolve ingredient in {1}.", Output, sourceForErrorLogging, RecipeCategoryCode);
			return false;
		}
		if (!Output.Resolve(world, sourceForErrorLogging, base.Ingredient.Code))
		{
			return false;
		}
		GenVoxels();
		return true;
	}

	public void GenVoxels()
	{
		int length = Pattern[0][0].Length;
		int num = Pattern[0].Length;
		int num2 = Pattern.Length;
		if (num > 16 || num2 > QuantityLayers || length > 16)
		{
			throw new Exception(string.Format("Invalid {1} recipe {0}! Either Width or length is beyond 16 voxels or height is beyond {2} voxels", base.Name, RecipeCategoryCode, QuantityLayers));
		}
		for (int i = 0; i < Pattern.Length; i++)
		{
			if (Pattern[i].Length != num)
			{
				throw new Exception(string.Format("Invalid {4} recipe {3}! Layer {0} has a width of {1}, which is not the same as the first layer width of {2}. All layers need to be sized equally.", i, Pattern[i].Length, num, base.Name, RecipeCategoryCode));
			}
			for (int j = 0; j < Pattern[i].Length; j++)
			{
				if (Pattern[i][j].Length != length)
				{
					throw new Exception(string.Format("Invalid {5} recipe {3}! Layer {0}, line {4} has a length of {1}, which is not the same as the first layer length of {2}. All layers need to be sized equally.", i, Pattern[i][j].Length, length, base.Name, j, RecipeCategoryCode));
				}
			}
		}
		int num3 = (16 - num) / 2;
		int num4 = (16 - length) / 2;
		for (int k = 0; k < Math.Min(num, 16); k++)
		{
			for (int l = 0; l < Math.Min(num2, QuantityLayers); l++)
			{
				for (int m = 0; m < Math.Min(length, 16); m++)
				{
					if (RotateRecipe)
					{
						Voxels[m + num4, l, k + num3] = Pattern[l][k][m] != '_' && Pattern[l][k][m] != ' ';
					}
					else
					{
						Voxels[k + num3, l, m + num4] = Pattern[l][k][m] != '_' && Pattern[l][k][m] != ' ';
					}
				}
			}
		}
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(RecipeId);
		base.Ingredient.ToBytes(writer);
		writer.Write(Pattern.Length);
		for (int i = 0; i < Pattern.Length; i++)
		{
			writer.WriteArray(Pattern[i]);
		}
		writer.Write(base.Name.ToShortString());
		Output.ToBytes(writer);
	}

	public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		base.Ingredient = new CraftingRecipeIngredient();
		RecipeId = reader.ReadInt32();
		base.Ingredient.FromBytes(reader, resolver);
		int num = reader.ReadInt32();
		Pattern = new string[num][];
		for (int i = 0; i < Pattern.Length; i++)
		{
			Pattern[i] = reader.ReadStringArray();
		}
		base.Name = new AssetLocation(reader.ReadString());
		Output = new JsonItemStack();
		Output.FromBytes(reader, resolver.ClassRegistry);
		Output.Resolve(resolver, "[Voxel recipe FromBytes]", base.Ingredient.Code);
		GenVoxels();
	}

	public override Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
	{
		Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();
		if (base.Ingredient.Name == null || base.Ingredient.Name.Length == 0)
		{
			return dictionary;
		}
		if (!base.Ingredient.Code.Path.Contains('*'))
		{
			return dictionary;
		}
		int num = base.Ingredient.Code.Path.IndexOf('*');
		int num2 = base.Ingredient.Code.Path.Length - num - 1;
		List<string> list = new List<string>();
		if (base.Ingredient.Type == EnumItemClass.Block)
		{
			foreach (Block block in world.Blocks)
			{
				if (!block.IsMissing && WildcardUtil.Match(base.Ingredient.Code, block.Code))
				{
					string text = block.Code.Path.Substring(num);
					string text2 = text.Substring(0, text.Length - num2);
					if (base.Ingredient.AllowedVariants == null || base.Ingredient.AllowedVariants.Contains(text2))
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
				if (!(item?.Code == null) && !item.IsMissing && WildcardUtil.Match(base.Ingredient.Code, item.Code))
				{
					string text3 = item.Code.Path.Substring(num);
					string text4 = text3.Substring(0, text3.Length - num2);
					if (base.Ingredient.AllowedVariants == null || base.Ingredient.AllowedVariants.Contains(text4))
					{
						list.Add(text4);
					}
				}
			}
		}
		dictionary[base.Ingredient.Name] = list.ToArray();
		return dictionary;
	}

	public static bool WildCardMatch(AssetLocation wildCard, AssetLocation blockCode)
	{
		if (blockCode == null || !wildCard.Domain.Equals(blockCode.Domain))
		{
			return false;
		}
		if (wildCard.Equals(blockCode))
		{
			return true;
		}
		string text = Regex.Escape(wildCard.Path).Replace("\\*", "(.*)");
		return Regex.IsMatch(blockCode.Path, "^" + text + "$");
	}
}
