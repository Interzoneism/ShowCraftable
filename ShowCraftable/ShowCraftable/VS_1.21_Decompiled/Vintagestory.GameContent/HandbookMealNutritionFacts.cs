using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

internal class HandbookMealNutritionFacts
{
	public required HashSet<EnumFoodCategory> Categories;

	public float MinSatiety;

	public float MaxSatiety;

	public float MinHealth;

	public float MaxHealth;
}
