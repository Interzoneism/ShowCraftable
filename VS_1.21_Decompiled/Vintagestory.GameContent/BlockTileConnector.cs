using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockTileConnector : Block
{
	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = base.OnPickBlock(world, pos);
		BETileConnector blockEntity = world.BlockAccessor.GetBlockEntity<BETileConnector>(pos);
		if (blockEntity != null)
		{
			itemStack.Attributes["constraints"] = new StringAttribute(blockEntity.Constraints);
		}
		return itemStack;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		GetBlockEntity<BETileConnector>(blockSel.Position)?.OnInteract(byPlayer);
		return true;
	}
}
