using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class CreatureDiet
{
	public EnumFoodCategory[] FoodCategories;

	public string[] FoodTags;

	public WeightedFoodTag[] WeightedFoodTags;

	public string[] SkipFoodTags;

	[OnDeserialized]
	private void OnDeserialized(StreamingContext ctx)
	{
		if (FoodTags != null)
		{
			List<WeightedFoodTag> list = new List<WeightedFoodTag>(WeightedFoodTags ?? Array.Empty<WeightedFoodTag>());
			string[] foodTags = FoodTags;
			foreach (string code in foodTags)
			{
				list.Add(new WeightedFoodTag
				{
					Code = code,
					Weight = 1f
				});
			}
			WeightedFoodTags = list.ToArray();
		}
	}

	public bool Matches(EnumFoodCategory foodSourceCategory, string[] foodSourceTags, float foodTagMinWeight = 0f)
	{
		if (SkipFoodTags != null && foodSourceTags != null)
		{
			for (int i = 0; i < foodSourceTags.Length; i++)
			{
				if (SkipFoodTags.Contains(foodSourceTags[i]))
				{
					return false;
				}
			}
		}
		if (FoodCategories != null && FoodCategories.Contains(foodSourceCategory))
		{
			return true;
		}
		if (WeightedFoodTags != null && foodSourceTags != null)
		{
			for (int j = 0; j < foodSourceTags.Length; j++)
			{
				for (int k = 0; k < WeightedFoodTags.Length; k++)
				{
					WeightedFoodTag weightedFoodTag = WeightedFoodTags[k];
					if (weightedFoodTag.Weight >= foodTagMinWeight && weightedFoodTag.Code == foodSourceTags[j])
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public bool Matches(ItemStack itemstack, bool checkCategory = true, float foodTagMinWeight = 0f)
	{
		CollectibleObject collectible = itemstack.Collectible;
		EnumFoodCategory foodSourceCategory = (checkCategory ? (collectible?.NutritionProps?.FoodCategory ?? EnumFoodCategory.NoNutrition) : EnumFoodCategory.NoNutrition);
		string[] foodSourceTags = collectible?.Attributes?["foodTags"].AsArray<string>();
		return Matches(foodSourceCategory, foodSourceTags, foodTagMinWeight);
	}
}
