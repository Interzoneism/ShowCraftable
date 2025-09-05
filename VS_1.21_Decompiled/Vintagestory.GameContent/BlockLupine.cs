using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockLupine : BlockPlant
{
	private Block[] uncommonVariants;

	private Block[] rareVariants;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		uncommonVariants = new Block[2]
		{
			api.World.GetBlock(CodeWithVariant("color", "white")),
			api.World.GetBlock(CodeWithVariant("color", "red"))
		};
		rareVariants = new Block[1] { api.World.GetBlock(CodeWithVariant("color", "orange")) };
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (!base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldGenRand, attributes))
		{
			return false;
		}
		double num = worldGenRand.NextDouble();
		if (num < 1.0 / 300.0)
		{
			GenRareColorPatch(blockAccessor, pos, rareVariants[worldGenRand.NextInt(rareVariants.Length)], worldGenRand);
		}
		else if (num < 1.0 / 120.0)
		{
			GenRareColorPatch(blockAccessor, pos, uncommonVariants[worldGenRand.NextInt(uncommonVariants.Length)], worldGenRand);
		}
		return true;
	}

	private void GenRareColorPatch(IBlockAccessor blockAccessor, BlockPos pos, Block block, IRandom worldGenRand)
	{
		int num = 2 + worldGenRand.NextInt(6);
		int num2 = 30;
		BlockPos blockPos = pos.Copy();
		while (num > 0 && num2-- > 0)
		{
			blockPos.Set(pos).Add(worldGenRand.NextInt(5) - 2, 0, worldGenRand.NextInt(5) - 2);
			blockPos.Y = blockAccessor.GetTerrainMapheightAt(blockPos) + 1;
			Block block2 = blockAccessor.GetBlock(blockPos);
			if ((block2.IsReplacableBy(block) || block2 is BlockLupine) && CanPlantStay(blockAccessor, blockPos))
			{
				blockAccessor.SetBlock(block.BlockId, blockPos);
				num--;
			}
		}
	}
}
