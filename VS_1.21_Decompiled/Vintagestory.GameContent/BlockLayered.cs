using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockLayered : Block
{
	public Block GetNextLayer(IWorldAccessor world)
	{
		int.TryParse(Code.Path.Split('-')[1], out var result);
		string text = CodeWithoutParts(1);
		if (result < 7)
		{
			return world.BlockAccessor.GetBlock(CodeWithPath(text + "-" + (result + 1)));
		}
		return world.BlockAccessor.GetBlock(CodeWithPath(text.Replace("layer", "block")));
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
			failureCode = "claimed";
			return false;
		}
		Block block = world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(blockSel.Face.Opposite));
		if (block is BlockLayered)
		{
			Block nextLayer = ((BlockLayered)block).GetNextLayer(world);
			world.BlockAccessor.SetBlock(nextLayer.BlockId, blockSel.Position.AddCopy(blockSel.Face.Opposite));
			return true;
		}
		if (!CanLayerStay(world, blockSel.Position))
		{
			failureCode = "belowblockcannotsupport";
			return false;
		}
		return base.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(CodeWithParts("1")));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (GetBehavior<BlockBehaviorUnstableFalling>() != null)
		{
			base.OnNeighbourBlockChange(world, pos, neibpos);
		}
		else if (!CanLayerStay(world, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	private bool CanLayerStay(IWorldAccessor world, BlockPos pos)
	{
		BlockPos pos2 = pos.DownCopy();
		return world.BlockAccessor.GetBlock(pos2).CanAttachBlockAt(world.BlockAccessor, this, pos2, BlockFacing.UP);
	}

	public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		return false;
	}
}
