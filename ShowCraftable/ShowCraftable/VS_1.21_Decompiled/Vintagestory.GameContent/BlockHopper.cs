using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockHopper : Block, IBlockItemFlow
{
	public bool HasItemFlowConnectorAt(BlockFacing facing)
	{
		return facing == BlockFacing.DOWN;
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
		if (facing != BlockFacing.UP || !(entity is EntityItem entityItem) || world.Side != EnumAppSide.Server || world.Rand.NextDouble() < 0.9)
		{
			return;
		}
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
		if (!entityItem.Alive || !(blockEntity is BlockEntityItemFlow blockEntityItemFlow))
		{
			return;
		}
		WeightedSlot bestSuitedSlot = blockEntityItemFlow.inventory.GetBestSuitedSlot(entityItem.Slot);
		if (bestSuitedSlot.slot != null)
		{
			entityItem.Slot.TryPutInto(api.World, bestSuitedSlot.slot);
			if (entityItem.Slot.StackSize <= 0)
			{
				entityItem.Itemstack = null;
				entityItem.Alive = false;
			}
		}
	}
}
