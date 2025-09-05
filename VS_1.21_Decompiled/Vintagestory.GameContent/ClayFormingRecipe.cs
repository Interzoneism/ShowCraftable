using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class ClayFormingRecipe : LayeredVoxelRecipe<ClayFormingRecipe>, IByteSerializable
{
	public override int QuantityLayers => 16;

	public override string RecipeCategoryCode => "clay forming";

	public override ClayFormingRecipe Clone()
	{
		ClayFormingRecipe clayFormingRecipe = new ClayFormingRecipe();
		clayFormingRecipe.Pattern = new string[Pattern.Length][];
		for (int i = 0; i < clayFormingRecipe.Pattern.Length; i++)
		{
			clayFormingRecipe.Pattern[i] = (string[])Pattern[i].Clone();
		}
		clayFormingRecipe.Ingredient = base.Ingredient.Clone();
		clayFormingRecipe.Output = Output.Clone();
		clayFormingRecipe.Name = base.Name;
		return clayFormingRecipe;
	}

	void IByteSerializable.ToBytes(BinaryWriter writer)
	{
		ToBytes(writer);
	}

	void IByteSerializable.FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		FromBytes(reader, resolver);
	}
}
