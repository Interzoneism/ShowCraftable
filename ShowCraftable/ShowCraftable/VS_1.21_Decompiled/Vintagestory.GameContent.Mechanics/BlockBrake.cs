using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockBrake : BlockMPBase
{
	public bool IsOrientedTo(BlockFacing facing)
	{
		return facing.Code == Variant["side"];
	}

	public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		BlockFacing blockFacing = BlockFacing.FromCode(Variant["side"]);
		BlockFacing blockFacing2 = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(blockFacing.HorizontalAngleIndex - 1, 4)];
		BlockFacing blockFacing3 = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(blockFacing.HorizontalAngleIndex + 1, 4)];
		if (face != blockFacing2)
		{
			return face == blockFacing3;
		}
		return true;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		BlockFacing[] array = Block.SuggestedHVOrientation(byPlayer, blockSel);
		AssetLocation code = CodeWithParts(array[0].Code);
		Block block = world.BlockAccessor.GetBlock(code);
		BlockFacing blockFacing = BlockFacing.FromCode(block.Variant["side"]);
		BlockFacing blockFacing2 = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(blockFacing.HorizontalAngleIndex - 1, 4)];
		BlockFacing blockFacing3 = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(blockFacing.HorizontalAngleIndex + 1, 4)];
		if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(blockFacing2)) is IMechanicalPowerBlock connectingBlock)
		{
			return DoPlaceMechBlock(world, byPlayer, itemstack, blockSel, block, connectingBlock, blockFacing2);
		}
		if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(blockFacing3)) is IMechanicalPowerBlock connectingBlock2)
		{
			return DoPlaceMechBlock(world, byPlayer, itemstack, blockSel, block, connectingBlock2, blockFacing3);
		}
		BlockFacing blockFacing4 = blockFacing;
		BlockFacing opposite = blockFacing.Opposite;
		Block block2 = world.GetBlock(block.CodeWithVariant("side", blockFacing2.Code));
		if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(blockFacing4)) is IMechanicalPowerBlock connectingBlock3)
		{
			return DoPlaceMechBlock(world, byPlayer, itemstack, blockSel, block2, connectingBlock3, blockFacing4);
		}
		if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(opposite)) is IMechanicalPowerBlock connectingBlock4)
		{
			return DoPlaceMechBlock(world, byPlayer, itemstack, blockSel, block2, connectingBlock4, opposite);
		}
		if (base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode))
		{
			WasPlaced(world, blockSel.Position, null);
			return true;
		}
		return false;
	}

	private bool DoPlaceMechBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, Block block, IMechanicalPowerBlock connectingBlock, BlockFacing connectingFace)
	{
		if (block.DoPlaceBlock(world, byPlayer, blockSel, itemstack))
		{
			connectingBlock.DidConnectAt(world, blockSel.Position.AddCopy(connectingFace), connectingFace.Opposite);
			WasPlaced(world, blockSel.Position, connectingFace);
			return true;
		}
		return false;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		BEBehaviorMPAxle bEBehaviorMPAxle = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPAxle>();
		if (bEBehaviorMPAxle != null && !BEBehaviorMPAxle.IsAttachedToBlock(world.BlockAccessor, bEBehaviorMPAxle.Block, pos))
		{
			BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
			foreach (BlockFacing blockFacing in hORIZONTALS)
			{
				BlockPos pos2 = pos.AddCopy(blockFacing);
				if (world.BlockAccessor.GetBlock(pos2) is BlockAngledGears blockAngledGears && blockAngledGears.Facings.Contains(blockFacing.Opposite) && blockAngledGears.Facings.Length == 1)
				{
					world.BlockAccessor.BreakBlock(pos2, null);
				}
			}
		}
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		return (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBrake)?.OnInteract(byPlayer) ?? false;
	}
}
