using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorBreakIfFloating : BlockBehavior
{
	public bool AllowFallingBlocks;

	public BlockBehaviorBreakIfFloating(Block block)
		: base(block)
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		AllowFallingBlocks = api.World.Config.GetBool("allowFallingBlocks");
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
	{
		if (world.Side != EnumAppSide.Client && AllowFallingBlocks)
		{
			handled = EnumHandling.PassThrough;
			if (IsSurroundedByNonSolid(world, pos))
			{
				world.BlockAccessor.BreakBlock(pos, null);
			}
			base.OnNeighbourBlockChange(world, pos, neibpos, ref handled);
		}
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
	{
		if (IsSurroundedByNonSolid(world, pos))
		{
			handled = EnumHandling.PreventSubsequent;
			return new ItemStack[1]
			{
				new ItemStack(block)
			};
		}
		handled = EnumHandling.PassThrough;
		return null;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
	}

	public bool IsSurroundedByNonSolid(IWorldAccessor world, BlockPos pos)
	{
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing in aLLFACES)
		{
			if (world.BlockAccessor.IsSideSolid(pos.X + blockFacing.Normali.X, pos.InternalY + blockFacing.Normali.Y, pos.Z + blockFacing.Normali.Z, blockFacing.Opposite))
			{
				return false;
			}
		}
		return true;
	}
}
