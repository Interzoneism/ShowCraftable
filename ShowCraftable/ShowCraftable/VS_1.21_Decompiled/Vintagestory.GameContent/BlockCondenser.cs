using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCondenser : BlockLiquidContainerTopOpened
{
	public override bool AllowHeldLiquidTransfer => false;

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = base.OnPickBlock(world, pos);
		SetContents(itemStack, null);
		return itemStack;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntityCondenser obj = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityCondenser;
		if (obj != null && obj.OnBlockInteractStart(byPlayer, blockSel))
		{
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(base.GetPlacedBlockInfo(world, pos, forPlayer));
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCondenser blockEntityCondenser && !blockEntityCondenser.Inventory[1].Empty)
		{
			BlockLiquidContainerBase obj = blockEntityCondenser.Inventory[1].Itemstack.Collectible as BlockLiquidContainerBase;
			stringBuilder.Append(Lang.Get("Container:") + " ");
			obj.GetContentInfo(blockEntityCondenser.Inventory[1], stringBuilder, world);
		}
		return stringBuilder.ToString();
	}
}
