using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GridRecipeLoader : ModSystem
{
	private ICoreServerAPI api;

	private bool classExclusiveRecipes = true;

	public override double ExecuteOrder()
	{
		return 1.0;
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void AssetsLoaded(ICoreAPI api)
	{
		if (api is ICoreServerAPI coreServerAPI)
		{
			this.api = coreServerAPI;
			classExclusiveRecipes = coreServerAPI.World.Config.GetBool("classExclusiveRecipes", defaultValue: true);
			LoadGridRecipes();
		}
	}

	public void LoadGridRecipes()
	{
		Dictionary<AssetLocation, JToken> many = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/grid");
		int num = 0;
		foreach (var (assetLocation2, val2) in many)
		{
			if (val2 is JObject)
			{
				LoadRecipe(assetLocation2, val2.ToObject<GridRecipe>(assetLocation2.Domain));
				num++;
			}
			if (!(val2 is JArray))
			{
				continue;
			}
			foreach (JToken item in (JArray)((val2 is JArray) ? val2 : null))
			{
				LoadRecipe(assetLocation2, item.ToObject<GridRecipe>(assetLocation2.Domain));
				num++;
			}
		}
		api.World.Logger.Event($"{num} crafting recipes loaded from {many.Count} files");
		api.World.Logger.StoryEvent(Lang.Get("Grand inventions..."));
	}

	public void LoadRecipe(AssetLocation assetLocation, GridRecipe recipe)
	{
		if (!recipe.Enabled)
		{
			return;
		}
		if (!classExclusiveRecipes)
		{
			recipe.RequiresTrait = null;
		}
		if (recipe.Name == null)
		{
			recipe.Name = assetLocation;
		}
		Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(api.World);
		if (nameToCodeMapping.Count <= 0)
		{
			if (recipe.ResolveIngredients(api.World))
			{
				api.RegisterCraftingRecipe(recipe);
			}
			return;
		}
		List<GridRecipe> list = new List<GridRecipe>();
		int num = 1;
		string key;
		string[] value;
		foreach (KeyValuePair<string, string[]> item in nameToCodeMapping)
		{
			item.Deconstruct(out key, out value);
			string[] array = value;
			num *= array.Length;
		}
		bool flag = true;
		int num2 = 1;
		foreach (KeyValuePair<string, string[]> item2 in nameToCodeMapping)
		{
			item2.Deconstruct(out key, out value);
			string text = key;
			string[] array2 = value;
			if (array2.Length == 0)
			{
				continue;
			}
			for (int i = 0; i < num; i++)
			{
				string text2 = array2[i / num2 % array2.Length];
				GridRecipe gridRecipe;
				if (flag)
				{
					gridRecipe = recipe.Clone();
					list.Add(gridRecipe);
				}
				else
				{
					gridRecipe = list[i];
				}
				foreach (CraftingRecipeIngredient value2 in gridRecipe.Ingredients.Values)
				{
					if (value2.IsBasicWildCard)
					{
						if (value2.Name == text)
						{
							value2.FillPlaceHolder(text, text2);
							value2.Code.Path = value2.Code.Path.Replace("*", text2);
							value2.IsBasicWildCard = false;
						}
					}
					else if (value2.IsAdvancedWildCard)
					{
						value2.FillPlaceHolder(text, text2);
						value2.IsAdvancedWildCard = GridRecipe.IsAdvancedWildcard(value2.Code);
					}
					if (value2.ReturnedStack?.Code != null)
					{
						value2.ReturnedStack.Code.Path = value2.ReturnedStack.Code.Path.Replace("{" + text + "}", text2);
					}
				}
				gridRecipe.Output.FillPlaceHolder(text, text2);
			}
			num2 *= array2.Length;
			flag = false;
		}
		foreach (GridRecipe item3 in list)
		{
			if (item3.ResolveIngredients(api.World))
			{
				api.RegisterCraftingRecipe(item3);
			}
		}
	}
}
