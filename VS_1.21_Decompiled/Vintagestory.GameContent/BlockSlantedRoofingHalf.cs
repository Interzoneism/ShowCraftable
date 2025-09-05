using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockSlantedRoofingHalf : Block
{
	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
	{
		string[] array = Code.Path.Split('-');
		BlockFacing blockFacing = BlockFacing.FromCode(array[^2]);
		switch (array[0])
		{
		case "slantedroofinghalfleft":
			if (blockFacing.Axis != axis)
			{
				return new AssetLocation(Code.Path.Replace("left", "right"));
			}
			return new AssetLocation(CodeWithVariant("horizontalorientation", blockFacing.Opposite.Code).Path.Replace("left", "right"));
		case "slantedroofinghalfright":
			if (blockFacing.Axis != axis)
			{
				return new AssetLocation(Code.Path.Replace("right", "left"));
			}
			return new AssetLocation(CodeWithVariant("horizontalorientation", blockFacing.Opposite.Code).Path.Replace("right", "left"));
		case "slantedroofingcornerinner":
		case "slantedroofingcornerouter":
			if (blockFacing.Axis != axis)
			{
				return CodeWithVariant("horizontalorientation", BlockFacing.HORIZONTALS[(blockFacing.Index + 3) % 4].Code);
			}
			return CodeWithVariant("horizontalorientation", BlockFacing.HORIZONTALS[(blockFacing.Index + 1) % 4].Code);
		default:
			if (blockFacing.Axis != axis)
			{
				return CodeWithVariant("horizontalorientation", blockFacing.Opposite.Code);
			}
			return Code;
		}
	}
}
