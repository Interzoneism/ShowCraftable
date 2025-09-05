using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockClutch : Block
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		BlockFacing blockFacing = Block.SuggestedHVOrientation(byPlayer, blockSel)[0];
		BlockFacing blockFacing2 = blockFacing;
		if (!(world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(blockFacing)) is BlockTransmission))
		{
			BlockFacing blockFacing3 = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(blockFacing.HorizontalAngleIndex - 1, 4)];
			if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(blockFacing3)) is BlockTransmission)
			{
				blockFacing2 = blockFacing3;
			}
			else
			{
				BlockFacing opposite = blockFacing3.Opposite;
				if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(opposite)) is BlockTransmission)
				{
					blockFacing2 = opposite;
				}
				else
				{
					BlockFacing opposite2 = blockFacing.Opposite;
					if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(opposite2)) is BlockTransmission)
					{
						blockFacing2 = opposite2;
					}
				}
			}
		}
		return world.BlockAccessor.GetBlock(CodeWithParts(blockFacing2.Code)).DoPlaceBlock(world, byPlayer, blockSel, itemstack);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEClutch bEClutch)
		{
			return bEClutch.OnInteract(byPlayer);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEClutch bEClutch && (activationArgs == null || !activationArgs.HasAttribute("engaged") || activationArgs.GetBool("engaged") != bEClutch.Engaged))
		{
			bEClutch.OnInteract(caller.Player);
		}
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		(world.BlockAccessor.GetBlockEntity(pos) as BEClutch)?.onNeighbourChange(neibpos);
	}
}
