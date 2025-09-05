using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockRandomizer : Block
{
	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = base.OnPickBlock(world, pos);
		BlockEntityBlockRandomizer blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityBlockRandomizer>(pos);
		if (blockEntity != null)
		{
			itemStack.Attributes["chances"] = new FloatArrayAttribute(blockEntity.Chances);
			blockEntity.Inventory.ToTreeAttributes(itemStack.Attributes);
		}
		return itemStack;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		GetBlockEntity<BlockEntityBlockRandomizer>(blockSel.Position)?.OnInteract(byPlayer);
		return true;
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		CustomBlockLayerHandler = true;
	}
}
