using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockWaterPlant : BlockPlant
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		Block block = this;
		Block block2 = world.BlockAccessor.GetBlock(blockSel.Position, 2);
		if (block2.IsLiquid() && block2.LiquidLevel == 7 && block2.LiquidCode.Contains("water"))
		{
			block = world.GetBlock(CodeWithParts("water"));
			if (block == null)
			{
				block = this;
			}
		}
		else if (LastCodePart() != "free")
		{
			failureCode = "requirefullwater";
			return false;
		}
		if ((block != null && skipPlantCheck) || CanPlantStay(world.BlockAccessor, blockSel.Position))
		{
			world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
			return true;
		}
		return false;
	}

	public override bool TryPlaceBlockForWorldGenUnderwater(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, int minWaterDepth, int maxWaterDepth, BlockPatchAttributes attributes = null)
	{
		BlockPos blockPos = pos.DownCopy();
		for (int i = 1; i < maxWaterDepth; i++)
		{
			blockPos.Down();
			Block block = blockAccessor.GetBlock(blockPos);
			if (block is BlockWaterPlant)
			{
				return false;
			}
			if (block.Fertility > 0)
			{
				blockAccessor.SetBlock(BlockId, blockPos.Up());
				return true;
			}
			if (!block.IsLiquid())
			{
				return false;
			}
		}
		return false;
	}
}
