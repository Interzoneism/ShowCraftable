using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockWaterLilyGiant : BlockWaterLily
{
	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (blockAccessor.GetBlockBelow(pos, 4, 2).Id != 0)
		{
			return false;
		}
		bool flag = true;
		BlockPos blockPos = pos.Copy();
		for (int i = -2; i < 3; i++)
		{
			for (int j = -2; j < 3; j++)
			{
				blockPos.Set(pos.X + i, pos.Y, pos.Z + j);
				Block block = blockAccessor.GetBlock(blockPos, 1);
				Block block2 = blockAccessor.GetBlock(blockPos.Down(), 1);
				if (block == null || block.Id != 0 || block2 == null || block2.Id != 0)
				{
					flag = false;
				}
			}
		}
		if (!flag)
		{
			return false;
		}
		if (!CanPlantStay(blockAccessor, pos))
		{
			return false;
		}
		Block block3 = blockAccessor.GetBlock(pos);
		if (block3.IsReplacableBy(this))
		{
			if (block3.EntityClass != null)
			{
				blockAccessor.RemoveBlockEntity(pos);
			}
			blockAccessor.SetBlock(BlockId, pos);
			if (EntityClass != null)
			{
				blockAccessor.SpawnBlockEntity(EntityClass, pos);
			}
			return true;
		}
		return false;
	}
}
