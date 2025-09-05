using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockAxle : BlockMPBase
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
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing in aLLFACES)
		{
			BlockPos pos = blockSel.Position.AddCopy(blockFacing);
			if (!(world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock mechanicalPowerBlock))
			{
				continue;
			}
			BlockFacing opposite = blockFacing.Opposite;
			if (!mechanicalPowerBlock.HasMechPowerConnectorAt(world, pos, opposite))
			{
				continue;
			}
			ReadOnlySpan<char> readOnlySpan = FirstCodePart();
			ReadOnlySpan<char> readOnlySpan2 = "-";
			char reference = opposite.Code[0];
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
				reference = opposite.Code[0];
				blockCode = new AssetLocation(string.Concat(readOnlySpan4, readOnlySpan5, readOnlySpan6, new ReadOnlySpan<char>(in reference)));
				block = world.GetBlock(blockCode);
			}
			if (block.DoPlaceBlock(world, byPlayer, blockSel, itemstack))
			{
				mechanicalPowerBlock.DidConnectAt(world, pos, opposite);
				WasPlaced(world, blockSel.Position, blockFacing);
				pos = blockSel.Position.AddCopy(opposite);
				if (world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock mechanicalPowerBlock2 && mechanicalPowerBlock2.HasMechPowerConnectorAt(world, pos, blockFacing))
				{
					mechanicalPowerBlock2.DidConnectAt(world, pos, blockFacing);
					WasPlaced(world, blockSel.Position, opposite);
				}
				return true;
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
		BEBehaviorMPAxle bEBehaviorMPAxle = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPAxle>();
		if (bEBehaviorMPAxle != null && !BEBehaviorMPAxle.IsAttachedToBlock(world.BlockAccessor, bEBehaviorMPAxle.Block, pos))
		{
			bool flag = false;
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing blockFacing in aLLFACES)
			{
				BlockPos pos2 = pos.AddCopy(blockFacing);
				IMechanicalPowerBlock mechanicalPowerBlock = world.BlockAccessor.GetBlock(pos2) as IMechanicalPowerBlock;
				bool flag2 = flag;
				if (mechanicalPowerBlock != null && mechanicalPowerBlock.HasMechPowerConnectorAt(world, pos, blockFacing.Opposite))
				{
					BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
					if (blockEntity != null && blockEntity.GetBehavior<BEBehaviorMPBase>()?.disconnected == false)
					{
						flag = true;
					}
				}
				if (mechanicalPowerBlock is BlockAngledGears blockAngledGears && blockAngledGears.Facings.Contains(blockFacing.Opposite) && blockAngledGears.Facings.Length == 1)
				{
					world.BlockAccessor.BreakBlock(pos2, null);
					flag = flag2;
				}
			}
			if (!flag)
			{
				world.BlockAccessor.BreakBlock(pos, null);
			}
		}
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
	}
}
