using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockTransmission : BlockMPBase
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
			if (!(world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock mechanicalPowerBlock))
			{
				continue;
			}
			BlockFacing opposite = blockFacing.Opposite;
			if (!mechanicalPowerBlock.HasMechPowerConnectorAt(world, pos, opposite))
			{
				continue;
			}
			AssetLocation blockCode = ((blockFacing != BlockFacing.EAST && blockFacing != BlockFacing.WEST) ? new AssetLocation(FirstCodePart() + "-ns") : new AssetLocation(FirstCodePart() + "-we"));
			if (world.GetBlock(blockCode).DoPlaceBlock(world, byPlayer, blockSel, itemstack))
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

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
	}

	public override MechanicalNetwork GetNetwork(IWorldAccessor world, BlockPos pos)
	{
		BEBehaviorMPTransmission bEBehaviorMPTransmission = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPTransmission>();
		if (bEBehaviorMPTransmission == null || !bEBehaviorMPTransmission.engaged)
		{
			return null;
		}
		return bEBehaviorMPTransmission.Network;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		(world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPTransmission>())?.CheckEngaged(world.BlockAccessor, updateNetwork: true);
	}
}
