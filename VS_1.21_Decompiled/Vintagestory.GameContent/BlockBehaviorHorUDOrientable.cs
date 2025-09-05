using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorHorUDOrientable : BlockBehavior
{
	public BlockBehaviorHorUDOrientable(Block block)
		: base(block)
	{
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing[] array = Block.SuggestedHVOrientation(byPlayer, blockSel);
		if (array[1] == null)
		{
			array[1] = BlockFacing.UP;
		}
		AssetLocation code = base.block.CodeWithParts(array[1].Code, array[0].Code);
		Block block = world.BlockAccessor.GetBlock(code);
		if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			block.DoPlaceBlock(world, byPlayer, blockSel, null);
			return true;
		}
		return false;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		return new ItemStack[1]
		{
			new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts("up", "north")))
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		return new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts("up", "north")));
	}

	public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		BlockFacing blockFacing = BlockFacing.HORIZONTALS_ANGLEORDER[(angle / 90 + BlockFacing.FromCode(block.LastCodePart()).HorizontalAngleIndex) % 4];
		return block.CodeWithParts(blockFacing.Code);
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing blockFacing = BlockFacing.FromCode(block.LastCodePart());
		if (blockFacing.Axis == axis)
		{
			return block.CodeWithParts(blockFacing.Opposite.Code);
		}
		return block.Code;
	}

	public override AssetLocation GetVerticallyFlippedBlockCode(ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		if (!(block.LastCodePart(1) == "up"))
		{
			return block.CodeWithParts("up", block.LastCodePart());
		}
		return block.CodeWithParts("down", block.LastCodePart());
	}
}
