using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class BlockRiftWard : Block
{
	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntityRiftWard blockEntity = GetBlockEntity<BlockEntityRiftWard>(blockSel);
		if (blockEntity != null && blockEntity.OnInteract(blockSel, byPlayer))
		{
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}
}
