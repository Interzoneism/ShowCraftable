using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorIgniteable : BlockBehavior
{
	public BlockBehaviorIgniteable(Block block)
		: base(block)
	{
	}

	public virtual void Ignite(IWorldAccessor world, BlockPos pos)
	{
		if (!(base.block.LastCodePart() == "lit"))
		{
			Block block = world.GetBlock(base.block.CodeWithParts("lit"));
			if (block != null)
			{
				world.BlockAccessor.ExchangeBlock(block.BlockId, pos);
			}
		}
	}

	public void Extinguish(IWorldAccessor world, BlockPos pos)
	{
		if (!(base.block.LastCodePart() == "extinct"))
		{
			Block block = world.GetBlock(base.block.CodeWithParts("extinct"));
			if (block != null)
			{
				world.BlockAccessor.ExchangeBlock(block.BlockId, pos);
			}
		}
	}
}
