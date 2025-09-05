using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockSimpleCoating : Block
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		bool flag = true;
		bool flag2 = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref handling, ref failureCode);
			if (handling != EnumHandling.PassThrough)
			{
				flag = flag && flag3;
				flag2 = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		if (TryAttachTo(world, blockSel.Position, blockSel.Face))
		{
			return true;
		}
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		for (int j = 0; j < aLLFACES.Length; j++)
		{
			if (TryAttachTo(world, blockSel.Position, aLLFACES[j]))
			{
				return true;
			}
		}
		failureCode = "requireattachable";
		return false;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block block = world.BlockAccessor.GetBlock(CodeWithParts("down"));
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(CodeWithParts("down")));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (!CanBlockStay(world, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	private bool TryAttachTo(IWorldAccessor world, BlockPos blockpos, BlockFacing onBlockFace)
	{
		BlockFacing opposite = onBlockFace.Opposite;
		BlockPos pos = blockpos.AddCopy(opposite);
		if (world.BlockAccessor.GetBlock(pos).CanAttachBlockAt(world.BlockAccessor, this, pos, onBlockFace))
		{
			int blockId = world.BlockAccessor.GetBlock(CodeWithParts(opposite.Code)).BlockId;
			world.BlockAccessor.SetBlock(blockId, blockpos);
			return true;
		}
		return false;
	}

	private bool CanBlockStay(IWorldAccessor world, BlockPos pos)
	{
		BlockFacing blockFacing = BlockFacing.FromCode(Code.Path.Split('-')[^1]);
		return world.BlockAccessor.GetBlock(pos.AddCopy(blockFacing)).CanAttachBlockAt(world.BlockAccessor, this, pos.AddCopy(blockFacing), blockFacing.Opposite);
	}

	public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		return false;
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		if (LastCodePart() == "up" || LastCodePart() == "down")
		{
			return Code;
		}
		BlockFacing blockFacing = BlockFacing.HORIZONTALS_ANGLEORDER[((360 - angle) / 90 + BlockFacing.FromCode(LastCodePart()).HorizontalAngleIndex) % 4];
		return CodeWithParts(blockFacing.Code);
	}

	public override AssetLocation GetVerticallyFlippedBlockCode()
	{
		if (!(LastCodePart() == "up"))
		{
			return CodeWithParts("up");
		}
		return CodeWithParts("down");
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
	{
		BlockFacing blockFacing = BlockFacing.FromCode(LastCodePart());
		if (blockFacing.Axis == axis)
		{
			return CodeWithParts(blockFacing.Opposite.Code);
		}
		return Code;
	}
}
