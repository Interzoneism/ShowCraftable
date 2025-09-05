using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockAnvilPart : Block
{
	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor world, BlockPos pos)
	{
		BlockEntityAnvilPart blockEntityAnvilPart = world.GetBlockEntity(pos) as BlockEntityAnvilPart;
		if (blockEntityAnvilPart?.Inventory != null && blockEntityAnvilPart.Inventory[2].Empty)
		{
			return new Cuboidf[1] { CollisionBoxes[0] };
		}
		return base.GetCollisionBoxes(world, pos);
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
	{
		BlockEntityAnvilPart blockEntityAnvilPart = world.GetBlockEntity(pos) as BlockEntityAnvilPart;
		if (blockEntityAnvilPart?.Inventory != null && blockEntityAnvilPart.Inventory[2].Empty)
		{
			return new Cuboidf[1] { CollisionBoxes[0] };
		}
		return base.GetSelectionBoxes(world, pos);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		(world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityAnvilPart)?.OnInteract(byPlayer);
		return true;
	}
}
