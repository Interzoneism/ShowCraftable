using System;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class FoodNutritionProperties
{
	[DocumentAsJson]
	public EnumFoodCategory FoodCategory;

	[DocumentAsJson]
	public float Satiety;

	[DocumentAsJson]
	public float SaturationLossDelay = 10f;

	[DocumentAsJson]
	public float Health;

	[DocumentAsJson]
	public JsonItemStack EatenStack;

	[DocumentAsJson]
	[Obsolete("Use Satiety instead.")]
	public float Saturation
	{
		get
		{
			return Satiety;
		}
		set
		{
			Satiety = value;
		}
	}

	[DocumentAsJson]
	public float Intoxication { get; set; }

	public FoodNutritionProperties Clone()
	{
		return new FoodNutritionProperties
		{
			FoodCategory = FoodCategory,
			Satiety = Satiety,
			Health = Health,
			Intoxication = Intoxication,
			EatenStack = EatenStack?.Clone()
		};
	}
}
