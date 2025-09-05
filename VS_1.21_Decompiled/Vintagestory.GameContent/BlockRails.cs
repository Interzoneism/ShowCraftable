using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockRails : Block
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		BlockFacing blockFacing = Block.SuggestedHVOrientation(byPlayer, blockSel)[0];
		Block block = null;
		for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
		{
			BlockFacing toFacing = BlockFacing.HORIZONTALS[i];
			if (TryAttachPlaceToHoriontal(world, byPlayer, blockSel.Position, toFacing, blockFacing))
			{
				return true;
			}
		}
		if (block == null)
		{
			block = ((blockFacing.Axis != EnumAxis.Z) ? world.GetBlock(CodeWithParts("flat_we")) : world.GetBlock(CodeWithParts("flat_ns")));
		}
		block.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
		return true;
	}

	private bool TryAttachPlaceToHoriontal(IWorldAccessor world, IPlayer byPlayer, BlockPos position, BlockFacing toFacing, BlockFacing targetFacing)
	{
		BlockPos blockPos = position.AddCopy(toFacing);
		Block block = world.BlockAccessor.GetBlock(blockPos);
		if (!(block is BlockRails))
		{
			return false;
		}
		BlockFacing opposite = toFacing.Opposite;
		BlockFacing[] facingsFromType = getFacingsFromType(block.Variant["type"]);
		if (world.BlockAccessor.GetBlock(blockPos.AddCopy(facingsFromType[0])) is BlockRails && world.BlockAccessor.GetBlock(blockPos.AddCopy(facingsFromType[1])) is BlockRails)
		{
			return false;
		}
		BlockFacing openedEndedFace = getOpenedEndedFace(facingsFromType, world, position.AddCopy(toFacing));
		if (openedEndedFace == null)
		{
			return false;
		}
		Block railBlock = getRailBlock(world, "curved_", toFacing, targetFacing);
		if (railBlock != null)
		{
			if (!placeIfSuitable(world, byPlayer, railBlock, position))
			{
				return false;
			}
			return true;
		}
		string text = block.Variant["type"].Split('_')[1];
		BlockFacing dir = ((text[0] == openedEndedFace.Code[0]) ? BlockFacing.FromFirstLetter(text[1]) : BlockFacing.FromFirstLetter(text[0]));
		Block railBlock2 = getRailBlock(world, "curved_", dir, opposite);
		if (railBlock2 == null)
		{
			return false;
		}
		railBlock2.DoPlaceBlock(world, byPlayer, new BlockSelection
		{
			Position = position.AddCopy(toFacing),
			Face = BlockFacing.UP
		}, null);
		return false;
	}

	private bool placeIfSuitable(IWorldAccessor world, IPlayer byPlayer, Block block, BlockPos pos)
	{
		string failureCode = "";
		BlockSelection blockSel = new BlockSelection
		{
			Position = pos,
			Face = BlockFacing.UP
		};
		if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			block.DoPlaceBlock(world, byPlayer, blockSel, null);
			return true;
		}
		return false;
	}

	private Block getRailBlock(IWorldAccessor world, string prefix, BlockFacing dir0, BlockFacing dir1)
	{
		ReadOnlySpan<char> readOnlySpan = prefix;
		char reference = dir0.Code[0];
		ReadOnlySpan<char> readOnlySpan2 = new ReadOnlySpan<char>(in reference);
		char reference2 = dir1.Code[0];
		Block block = world.GetBlock(CodeWithParts(string.Concat(readOnlySpan, readOnlySpan2, new ReadOnlySpan<char>(in reference2))));
		if (block != null)
		{
			return block;
		}
		ReadOnlySpan<char> readOnlySpan3 = prefix;
		reference2 = dir1.Code[0];
		ReadOnlySpan<char> readOnlySpan4 = new ReadOnlySpan<char>(in reference2);
		reference = dir0.Code[0];
		return world.GetBlock(CodeWithParts(string.Concat(readOnlySpan3, readOnlySpan4, new ReadOnlySpan<char>(in reference))));
	}

	private BlockFacing getOpenedEndedFace(BlockFacing[] dirFacings, IWorldAccessor world, BlockPos blockPos)
	{
		if (!(world.BlockAccessor.GetBlock(blockPos.AddCopy(dirFacings[0])) is BlockRails))
		{
			return dirFacings[0];
		}
		if (!(world.BlockAccessor.GetBlock(blockPos.AddCopy(dirFacings[1])) is BlockRails))
		{
			return dirFacings[1];
		}
		return null;
	}

	private BlockFacing[] getFacingsFromType(string type)
	{
		string text = type.Split('_')[1];
		return new BlockFacing[2]
		{
			BlockFacing.FromFirstLetter(text[0]),
			BlockFacing.FromFirstLetter(text[1])
		};
	}
}
