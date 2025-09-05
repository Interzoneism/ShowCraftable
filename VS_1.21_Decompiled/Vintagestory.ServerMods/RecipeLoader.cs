using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Vintagestory.ServerMods;

public class RecipeLoader : ModSystem
{
	private ICoreServerAPI api;

	private bool classExclusiveRecipes = true;

	public override double ExecuteOrder()
	{
		return 1.0;
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override void AssetsLoaded(ICoreAPI api)
	{
		ICoreServerAPI sapi = api as ICoreServerAPI;
		if (sapi != null)
		{
			this.api = sapi;
			classExclusiveRecipes = sapi.World.Config.GetBool("classExclusiveRecipes", defaultValue: true);
			LoadAlloyRecipes();
			LoadRecipes("smithing recipe", "recipes/smithing", delegate(SmithingRecipe r)
			{
				sapi.RegisterSmithingRecipe(r);
			});
			sapi.World.Logger.StoryEvent(Lang.Get("Burning sparks..."));
			LoadRecipes("clay forming recipe", "recipes/clayforming", delegate(ClayFormingRecipe r)
			{
				sapi.RegisterClayFormingRecipe(r);
			});
			sapi.World.Logger.StoryEvent(Lang.Get("Molded forms..."));
			LoadRecipes("knapping recipe", "recipes/knapping", delegate(KnappingRecipe r)
			{
				sapi.RegisterKnappingRecipe(r);
			});
			sapi.World.Logger.StoryEvent(Lang.Get("Simple tools..."));
			LoadRecipes("barrel recipe", "recipes/barrel", delegate(BarrelRecipe r)
			{
				sapi.RegisterBarrelRecipe(r);
			});
		}
	}

	public void LoadAlloyRecipes()
	{
		Dictionary<AssetLocation, AlloyRecipe> many = api.Assets.GetMany<AlloyRecipe>(api.Server.Logger, "recipes/alloy");
		foreach (KeyValuePair<AssetLocation, AlloyRecipe> item in many)
		{
			if (item.Value.Enabled)
			{
				item.Value.Resolve(api.World, "alloy recipe " + item.Key);
				api.RegisterMetalAlloy(item.Value);
			}
		}
		api.World.Logger.Event("{0} metal alloys loaded", many.Count);
		api.World.Logger.StoryEvent(Lang.Get("Glimmers in the soil..."));
	}

	public void LoadRecipes<T>(string name, string path, Action<T> RegisterMethod) where T : IRecipeBase<T>
	{
		Dictionary<AssetLocation, JToken> many = api.Assets.GetMany<JToken>(api.Server.Logger, path);
		int num = 0;
		int quantityRegistered = 0;
		int quantityIgnored = 0;
		foreach (KeyValuePair<AssetLocation, JToken> item in many)
		{
			if (item.Value is JObject)
			{
				LoadGenericRecipe(name, item.Key, item.Value.ToObject<T>(item.Key.Domain), RegisterMethod, ref quantityRegistered, ref quantityIgnored);
				num++;
			}
			if (!(item.Value is JArray))
			{
				continue;
			}
			JToken value = item.Value;
			foreach (JToken item2 in (JArray)((value is JArray) ? value : null))
			{
				LoadGenericRecipe(name, item.Key, item2.ToObject<T>(item.Key.Domain), RegisterMethod, ref quantityRegistered, ref quantityIgnored);
				num++;
			}
		}
		api.World.Logger.Event("{0} {1}s loaded{2}", quantityRegistered, name, (quantityIgnored > 0) ? $" ({quantityIgnored} could not be resolved)" : "");
	}

	private void LoadGenericRecipe<T>(string className, AssetLocation path, T recipe, Action<T> RegisterMethod, ref int quantityRegistered, ref int quantityIgnored) where T : IRecipeBase<T>
	{
		if (!recipe.Enabled)
		{
			return;
		}
		if (recipe.Name == null)
		{
			recipe.Name = path;
		}
		ref T reference = ref recipe;
		T val = default(T);
		if (val == null)
		{
			val = reference;
			reference = ref val;
		}
		IServerWorldAccessor world = api.World;
		Dictionary<string, string[]> nameToCodeMapping = reference.GetNameToCodeMapping(world);
		if (nameToCodeMapping.Count > 0)
		{
			List<T> list = new List<T>();
			int num = 0;
			bool flag = true;
			foreach (KeyValuePair<string, string[]> item in nameToCodeMapping)
			{
				num = ((!flag) ? (num * item.Value.Length) : item.Value.Length);
				flag = false;
			}
			flag = true;
			foreach (KeyValuePair<string, string[]> item2 in nameToCodeMapping)
			{
				string key = item2.Key;
				string[] value = item2.Value;
				for (int i = 0; i < num; i++)
				{
					T val2;
					if (flag)
					{
						list.Add(val2 = recipe.Clone());
					}
					else
					{
						val2 = list[i];
					}
					if (val2.Ingredients != null)
					{
						IRecipeIngredient[] ingredients = val2.Ingredients;
						foreach (IRecipeIngredient recipeIngredient in ingredients)
						{
							if (recipeIngredient.Name == key)
							{
								recipeIngredient.Code = recipeIngredient.Code.CopyWithPath(recipeIngredient.Code.Path.Replace("*", value[i % value.Length]));
							}
						}
					}
					val2.Output.FillPlaceHolder(item2.Key, value[i % value.Length]);
				}
				flag = false;
			}
			if (list.Count == 0)
			{
				api.World.Logger.Warning("{1} file {0} make uses of wildcards, but no blocks or item matching those wildcards were found.", path, className);
			}
			{
				foreach (T item3 in list)
				{
					T current3 = item3;
					ref T reference2 = ref current3;
					val = default(T);
					if (val == null)
					{
						val = reference2;
						reference2 = ref val;
					}
					IServerWorldAccessor world2 = api.World;
					string sourceForErrorLogging = className + " " + path;
					if (!reference2.Resolve(world2, sourceForErrorLogging))
					{
						quantityIgnored++;
						continue;
					}
					RegisterMethod(current3);
					quantityRegistered++;
				}
				return;
			}
		}
		ref T reference3 = ref recipe;
		val = default(T);
		if (val == null)
		{
			val = reference3;
			reference3 = ref val;
		}
		IServerWorldAccessor world3 = api.World;
		string sourceForErrorLogging2 = className + " " + path;
		if (!reference3.Resolve(world3, sourceForErrorLogging2))
		{
			quantityIgnored++;
			return;
		}
		RegisterMethod(recipe);
		quantityRegistered++;
	}
}
