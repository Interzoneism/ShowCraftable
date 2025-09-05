using System.IO;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class GridRecipeIngredient : CraftingRecipeIngredient
{
	public string PatternCode;

	public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(PatternCode);
	}

	public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		base.FromBytes(reader, resolver);
		PatternCode = reader.ReadString().DeDuplicate();
	}
}
