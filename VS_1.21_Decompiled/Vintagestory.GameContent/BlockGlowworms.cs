using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockGlowworms : Block
{
	public string[] bases = new string[4] { "base1", "base1-short", "base2", "base2-short" };

	public string[] segments = new string[4] { "segment1", null, "segment2", null };

	public string[] ends = new string[4] { "end1", null, "end2", null };

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
	}

	public Block GetBlock(IWorldAccessor world, string rocktype, string thickness)
	{
		return world.GetBlock(CodeWithParts(rocktype, thickness));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (!IsAttached(world, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	public bool IsAttached(IWorldAccessor world, BlockPos pos)
	{
		Block block = world.BlockAccessor.GetBlock(pos.UpCopy());
		if (!block.SideSolid[BlockFacing.DOWN.Index])
		{
			return block is BlockGlowworms;
		}
		return true;
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		bool flag = false;
		if (blockAccessor.GetBlock(pos).Replaceable < 6000)
		{
			return false;
		}
		BlockPos blockPos = pos.Copy();
		for (int i = 0; i < 150 + worldGenRand.NextInt(30); i++)
		{
			blockPos.X = pos.X + worldGenRand.NextInt(11) - 5;
			blockPos.Y = pos.Y + worldGenRand.NextInt(11) - 5;
			blockPos.Z = pos.Z + worldGenRand.NextInt(11) - 5;
			if (blockPos.Y <= api.World.SeaLevel - 10 && blockPos.Y >= 25 && blockAccessor.GetBlock(blockPos).Replaceable >= 6000)
			{
				flag |= TryGenGlowWorm(blockAccessor, blockPos, worldGenRand);
			}
		}
		return flag;
	}

	private bool TryGenGlowWorm(IBlockAccessor blockAccessor, BlockPos pos, IRandom worldGenRand)
	{
		bool result = false;
		for (int i = 0; i < 5; i++)
		{
			Block blockAbove = blockAccessor.GetBlockAbove(pos, i, 1);
			if (blockAbove.SideSolid[BlockFacing.DOWN.Index])
			{
				GenHere(blockAccessor, pos.AddCopy(0, i - 1, 0), worldGenRand);
				break;
			}
			if (blockAbove.Id != 0)
			{
				break;
			}
		}
		return result;
	}

	private void GenHere(IBlockAccessor blockAccessor, BlockPos pos, IRandom worldGenRand)
	{
		int num = worldGenRand.NextInt(bases.Length);
		Block block = api.World.GetBlock(CodeWithVariant("type", bases[num]));
		blockAccessor.SetBlock(block.Id, pos);
		if (segments[num] == null)
		{
			return;
		}
		block = api.World.GetBlock(CodeWithVariant("type", segments[num]));
		int num2 = worldGenRand.NextInt(3);
		while (num2-- > 0)
		{
			pos.Down();
			if (blockAccessor.GetBlock(pos).Replaceable > 6000)
			{
				blockAccessor.SetBlock(block.Id, pos);
			}
		}
		pos.Down();
		block = api.World.GetBlock(CodeWithVariant("type", ends[num]));
		if (blockAccessor.GetBlock(pos).Replaceable > 6000)
		{
			blockAccessor.SetBlock(block.Id, pos);
		}
	}
}
