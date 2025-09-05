using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockToggleCollisionBox : BlockClutter
{
	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BEBehaviorToggleCollisionBox bEBehaviorToggleCollisionBox = blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorToggleCollisionBox>();
		if (bEBehaviorToggleCollisionBox != null && bEBehaviorToggleCollisionBox.Solid)
		{
			return bEBehaviorToggleCollisionBox.CollisionBoxes;
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BEBehaviorToggleCollisionBox bEBehaviorToggleCollisionBox = blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorToggleCollisionBox>();
		if (bEBehaviorToggleCollisionBox != null && bEBehaviorToggleCollisionBox.Solid)
		{
			return bEBehaviorToggleCollisionBox.CollisionBoxes;
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}
}
