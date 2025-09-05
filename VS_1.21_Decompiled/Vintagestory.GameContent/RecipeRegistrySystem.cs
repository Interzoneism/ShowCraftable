using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class RecipeRegistrySystem : ModSystem
{
	public static bool canRegister = true;

	public List<CookingRecipe> CookingRecipes = new List<CookingRecipe>();

	public List<BarrelRecipe> BarrelRecipes = new List<BarrelRecipe>();

	public List<AlloyRecipe> MetalAlloys = new List<AlloyRecipe>();

	public List<SmithingRecipe> SmithingRecipes = new List<SmithingRecipe>();

	public List<KnappingRecipe> KnappingRecipes = new List<KnappingRecipe>();

	public List<ClayFormingRecipe> ClayFormingRecipes = new List<ClayFormingRecipe>();

	public override double ExecuteOrder()
	{
		return 0.6;
	}

	public override void StartPre(ICoreAPI api)
	{
		canRegister = true;
	}

	public override void Start(ICoreAPI api)
	{
		CookingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<CookingRecipe>>("cookingrecipes").Recipes;
		MetalAlloys = api.RegisterRecipeRegistry<RecipeRegistryGeneric<AlloyRecipe>>("alloyrecipes").Recipes;
		SmithingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<SmithingRecipe>>("smithingrecipes").Recipes;
		KnappingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<KnappingRecipe>>("knappingrecipes").Recipes;
		ClayFormingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<ClayFormingRecipe>>("clayformingrecipes").Recipes;
		BarrelRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<BarrelRecipe>>("barrelrecipes").Recipes;
	}

	public override void AssetsLoaded(ICoreAPI api)
	{
		if (!(api is ICoreServerAPI coreServerAPI))
		{
			return;
		}
		Dictionary<AssetLocation, JToken> many = coreServerAPI.Assets.GetMany<JToken>(coreServerAPI.Server.Logger, "recipes/cooking");
		foreach (KeyValuePair<AssetLocation, JToken> item in many)
		{
			if (item.Value is JObject)
			{
				loadRecipe(coreServerAPI, item.Key, item.Value);
			}
			if (!(item.Value is JArray))
			{
				continue;
			}
			JToken value = item.Value;
			foreach (JToken item2 in (JArray)((value is JArray) ? value : null))
			{
				loadRecipe(coreServerAPI, item.Key, item2);
			}
		}
		coreServerAPI.World.Logger.Event("{0} cooking recipes loaded", many.Count);
		coreServerAPI.World.Logger.StoryEvent(Lang.Get("Taste and smell..."));
	}

	private void loadRecipe(ICoreServerAPI sapi, AssetLocation loc, JToken jrec)
	{
		CookingRecipe cookingRecipe = jrec.ToObject<CookingRecipe>(loc.Domain);
		if (cookingRecipe.Enabled)
		{
			cookingRecipe.Resolve(sapi.World, "cooking recipe " + loc);
			RegisterCookingRecipe(cookingRecipe);
		}
	}

	public void RegisterCookingRecipe(CookingRecipe recipe)
	{
		if (!canRegister)
		{
			throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
		}
		CookingRecipes.Add(recipe);
	}

	public void RegisterBarrelRecipe(BarrelRecipe recipe)
	{
		if (!canRegister)
		{
			throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
		}
		if (recipe.Code == null)
		{
			throw new ArgumentException("Barrel recipes must have a non-null code! (choose freely)");
		}
		BarrelRecipeIngredient[] ingredients = recipe.Ingredients;
		foreach (BarrelRecipeIngredient barrelRecipeIngredient in ingredients)
		{
			if (barrelRecipeIngredient.ConsumeQuantity.HasValue && barrelRecipeIngredient.ConsumeQuantity > barrelRecipeIngredient.Quantity)
			{
				throw new ArgumentException("Barrel recipe with code {0} has an ingredient with ConsumeQuantity > Quantity. Not a valid recipe!");
			}
		}
		BarrelRecipes.Add(recipe);
	}

	public void RegisterMetalAlloy(AlloyRecipe alloy)
	{
		if (!canRegister)
		{
			throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
		}
		MetalAlloys.Add(alloy);
	}

	public void RegisterClayFormingRecipe(ClayFormingRecipe recipe)
	{
		if (!canRegister)
		{
			throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
		}
		recipe.RecipeId = ClayFormingRecipes.Count + 1;
		ClayFormingRecipes.Add(recipe);
	}

	public void RegisterSmithingRecipe(SmithingRecipe recipe)
	{
		if (!canRegister)
		{
			throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
		}
		recipe.RecipeId = SmithingRecipes.Count + 1;
		SmithingRecipes.Add(recipe);
	}

	public void RegisterKnappingRecipe(KnappingRecipe recipe)
	{
		if (!canRegister)
		{
			throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
		}
		recipe.RecipeId = KnappingRecipes.Count + 1;
		KnappingRecipes.Add(recipe);
	}
}
