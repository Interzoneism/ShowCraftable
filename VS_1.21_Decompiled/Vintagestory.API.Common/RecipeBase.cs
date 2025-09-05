using System;
using System.Collections.Generic;
using System.Linq;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public abstract class RecipeBase<T> : IRecipeBase<T>
{
	public int RecipeId;

	[DocumentAsJson]
	public CraftingRecipeIngredient[] Ingredients;

	[DocumentAsJson]
	public JsonItemStack Output;

	[DocumentAsJson]
	public CraftingRecipeIngredient Ingredient
	{
		get
		{
			if (Ingredients == null || Ingredients.Length == 0)
			{
				return null;
			}
			return Ingredients[0];
		}
		set
		{
			Ingredients = new CraftingRecipeIngredient[1] { value };
		}
	}

	[DocumentAsJson]
	public AssetLocation Name { get; set; }

	[DocumentAsJson]
	public bool Enabled { get; set; } = true;

	IRecipeIngredient[] IRecipeBase<T>.Ingredients => ((IEnumerable<CraftingRecipeIngredient>)Ingredients).Select((System.Func<CraftingRecipeIngredient, IRecipeIngredient>)((CraftingRecipeIngredient i) => i)).ToArray();

	IRecipeOutput IRecipeBase<T>.Output => Output;

	public abstract T Clone();

	public abstract Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world);

	public abstract bool Resolve(IWorldAccessor world, string sourceForErrorLogging);
}
