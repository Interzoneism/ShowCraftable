using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent.Mechanics;

public class BlockArchimedesScrew : BlockMPBase, IBlockItemFlow
{
	public bool IsOrientedTo(BlockFacing facing)
	{
		return facing.Axis == EnumAxis.Y;
	}

	public bool HasItemFlowConnectorAt(BlockFacing facing)
	{
		if (Variant["type"] == "ported-north")
		{
			return facing == BlockFacing.NORTH;
		}
		if (Variant["type"] == "ported-east")
		{
			return facing == BlockFacing.EAST;
		}
		if (Variant["type"] == "ported-south")
		{
			return facing == BlockFacing.SOUTH;
		}
		if (Variant["type"] == "ported-west")
		{
			return facing == BlockFacing.WEST;
		}
		return false;
	}

	public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		return IsOrientedTo(face);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		BlockArchimedesScrew blockArchimedesScrew = this;
		BlockFacing[] array = Block.SuggestedHVOrientation(byPlayer, blockSel);
		if (Variant["type"].StartsWithOrdinal("ported"))
		{
			blockArchimedesScrew = api.World.GetBlock(CodeWithVariant("type", "ported-" + array[0].Opposite.Code)) as BlockArchimedesScrew;
		}
		BlockFacing[] vERTICALS = BlockFacing.VERTICALS;
		foreach (BlockFacing blockFacing in vERTICALS)
		{
			BlockPos pos = blockSel.Position.AddCopy(blockFacing);
			if (world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock mechanicalPowerBlock && mechanicalPowerBlock.HasMechPowerConnectorAt(world, pos, blockFacing.Opposite) && blockArchimedesScrew.DoPlaceBlock(world, byPlayer, blockSel, itemstack))
			{
				mechanicalPowerBlock.DidConnectAt(world, pos, blockFacing.Opposite);
				WasPlaced(world, blockSel.Position, blockFacing);
				return true;
			}
		}
		if (blockArchimedesScrew.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode) && blockArchimedesScrew.DoPlaceBlock(world, byPlayer, blockSel, itemstack))
		{
			blockArchimedesScrew.WasPlaced(world, blockSel.Position, null);
			return true;
		}
		return false;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		BEBehaviorMPArchimedesScrew bEBehaviorMPArchimedesScrew = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPArchimedesScrew>();
		if (bEBehaviorMPArchimedesScrew != null && !bEBehaviorMPArchimedesScrew.IsAttachedToBlock())
		{
			BlockFacing[] vERTICALS = BlockFacing.VERTICALS;
			foreach (BlockFacing blockFacing in vERTICALS)
			{
				if (world.BlockAccessor.GetBlock(pos.AddCopy(blockFacing)) is BlockAngledGears blockAngledGears && blockAngledGears.Facings.Contains(blockFacing.Opposite) && blockAngledGears.Facings.Length == 1)
				{
					world.BlockAccessor.BreakBlock(pos.AddCopy(blockFacing), null);
				}
			}
		}
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
	}
}
