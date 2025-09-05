using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockLooseRock : BlockRequireSolidGround
{
	private BlockPos tmpPos = new BlockPos();

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (pos.Y < api.World.SeaLevel)
		{
			int num = 3 + worldGenRand.NextInt(6);
			for (int i = 0; i < num; i++)
			{
				tmpPos.Set(pos.X + worldGenRand.NextInt(7) - 3, pos.Y, pos.Z + worldGenRand.NextInt(7) - 3);
				tryPlace(blockAccessor, tmpPos, worldGenRand);
			}
		}
		return tryPlace(blockAccessor, pos, worldGenRand);
	}

	private bool tryPlace(IBlockAccessor blockAccessor, BlockPos pos, IRandom worldGenRand)
	{
		for (int i = 0; i < 3; i++)
		{
			tmpPos.Set(pos.X, pos.Y - 1 - i, pos.Z);
			Block block = blockAccessor.GetBlock(tmpPos);
			if (block.BlockMaterial == EnumBlockMaterial.Ice || block.BlockMaterial == EnumBlockMaterial.Snow || !block.CanAttachBlockAt(blockAccessor, this, tmpPos, BlockFacing.UP))
			{
				continue;
			}
			tmpPos.Y++;
			if (blockAccessor.GetBlock(tmpPos).Replaceable < 6000)
			{
				continue;
			}
			Block block2 = this;
			if (pos.Y < api.World.SeaLevel)
			{
				if (block.Variant["rock"] == null)
				{
					return false;
				}
				block2 = api.World.GetBlock(CodeWithVariant("rock", block.Variant["rock"]));
				if (block2 == null)
				{
					return false;
				}
			}
			generate(blockAccessor, block2, tmpPos, worldGenRand);
			return true;
		}
		return false;
	}

	protected virtual void generate(IBlockAccessor blockAccessor, Block block, BlockPos pos, IRandom worldGenRand)
	{
		blockAccessor.SetBlock(block.Id, pos);
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		BlockPos pos2 = pos.DownCopy();
		return capi.World.BlockAccessor.GetBlock(pos2).GetColor(capi, pos2);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		BlockPos pos2 = pos.DownCopy();
		return capi.World.BlockAccessor.GetBlock(pos2).GetRandomColor(capi, pos2, facing, rndIndex);
	}
}
