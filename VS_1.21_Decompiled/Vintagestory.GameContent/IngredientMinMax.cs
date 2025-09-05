namespace Vintagestory.GameContent;

internal class IngredientMinMax
{
	public required string Code;

	public int ExtraSlots;

	public float MinSat;

	public float MaxSat;

	public float MinHP;

	public float MaxHP;

	public IngredientMinMax Clone()
	{
		return new IngredientMinMax
		{
			Code = Code,
			ExtraSlots = ExtraSlots,
			MinSat = MinSat,
			MaxSat = MaxSat,
			MinHP = MinHP,
			MaxHP = MaxHP
		};
	}
}
