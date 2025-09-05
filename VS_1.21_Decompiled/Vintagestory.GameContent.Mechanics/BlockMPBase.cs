using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public abstract class BlockMPBase : Block, IMechanicalPowerBlock
{
	public abstract void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face);

	public abstract bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face);

	public virtual void WasPlaced(IWorldAccessor world, BlockPos ownPos, BlockFacing connectedOnFacing)
	{
		if (connectedOnFacing != null)
		{
			(world.BlockAccessor.GetBlockEntity(ownPos)?.GetBehavior<BEBehaviorMPBase>())?.tryConnect(connectedOnFacing);
		}
	}

	public virtual bool tryConnect(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, BlockFacing face)
	{
		if (world.BlockAccessor.GetBlock(pos.AddCopy(face)) is IMechanicalPowerBlock mechanicalPowerBlock && mechanicalPowerBlock.HasMechPowerConnectorAt(world, pos, face.Opposite))
		{
			mechanicalPowerBlock.DidConnectAt(world, pos.AddCopy(face), face.Opposite);
			WasPlaced(world, pos, face);
			return true;
		}
		return false;
	}

	public virtual MechanicalNetwork GetNetwork(IWorldAccessor world, BlockPos pos)
	{
		return ((IMechanicalPowerDevice)(world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPBase>()))?.Network;
	}

	internal void ExchangeBlockAt(IWorldAccessor world, BlockPos pos)
	{
		world.BlockAccessor.ExchangeBlock(BlockId, pos);
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
		BEBehaviorMPBase bEBehaviorMPBase = blockEntity?.GetBehavior<BEBehaviorMPBase>();
		if (bEBehaviorMPBase != null)
		{
			bEBehaviorMPBase.SetOrientations();
			bEBehaviorMPBase.Shape = Shape;
			blockEntity.MarkDirty();
		}
	}
}
