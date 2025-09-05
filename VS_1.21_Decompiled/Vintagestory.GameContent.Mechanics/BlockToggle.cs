using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockToggle : BlockMPBase
{
	public bool IsOrientedTo(BlockFacing facing)
	{
		string text = LastCodePart();
		if (text[0] != facing.Code[0])
		{
			if (text.Length > 1)
			{
				return text[1] == facing.Code[0];
			}
			return false;
		}
		return true;
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
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing blockFacing in hORIZONTALS)
		{
			BlockPos pos = blockSel.Position.AddCopy(blockFacing);
			if (world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock mechanicalPowerBlock && mechanicalPowerBlock.HasMechPowerConnectorAt(world, pos, blockFacing.Opposite))
			{
				ReadOnlySpan<char> readOnlySpan = FirstCodePart();
				ReadOnlySpan<char> readOnlySpan2 = "-";
				char reference = blockFacing.Opposite.Code[0];
				ReadOnlySpan<char> readOnlySpan3 = new ReadOnlySpan<char>(in reference);
				char reference2 = blockFacing.Code[0];
				AssetLocation blockCode = new AssetLocation(string.Concat(readOnlySpan, readOnlySpan2, readOnlySpan3, new ReadOnlySpan<char>(in reference2)));
				Block block = world.GetBlock(blockCode);
				if (block == null)
				{
					ReadOnlySpan<char> readOnlySpan4 = FirstCodePart();
					ReadOnlySpan<char> readOnlySpan5 = "-";
					reference2 = blockFacing.Code[0];
					ReadOnlySpan<char> readOnlySpan6 = new ReadOnlySpan<char>(in reference2);
					reference = blockFacing.Opposite.Code[0];
					blockCode = new AssetLocation(string.Concat(readOnlySpan4, readOnlySpan5, readOnlySpan6, new ReadOnlySpan<char>(in reference)));
					block = world.GetBlock(blockCode);
				}
				if (block.DoPlaceBlock(world, byPlayer, blockSel, itemstack))
				{
					mechanicalPowerBlock.DidConnectAt(world, pos, blockFacing.Opposite);
					WasPlaced(world, blockSel.Position, blockFacing);
					return true;
				}
			}
		}
		if (base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode))
		{
			WasPlaced(world, blockSel.Position, null);
			return true;
		}
		return false;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		BEBehaviorMPToggle bEBehaviorMPToggle = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPToggle>();
		if (bEBehaviorMPToggle != null && !bEBehaviorMPToggle.IsAttachedToBlock())
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
}
