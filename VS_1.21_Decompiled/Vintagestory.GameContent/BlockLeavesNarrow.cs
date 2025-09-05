using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockLeavesNarrow : BlockLeaves
{
	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		string text = Variant["wood"];
		return new ItemStack(world.GetBlock(CodeWithParts("placed", text, "5")));
	}
}
