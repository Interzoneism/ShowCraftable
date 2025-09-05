using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCrowfoot : BlockSeaweed
{
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		blocks = new Block[3]
		{
			api.World.BlockAccessor.GetBlock(CodeWithParts("section")),
			api.World.BlockAccessor.GetBlock(CodeWithParts("tip")),
			api.World.BlockAccessor.GetBlock(CodeWithParts("top"))
		};
	}

	public override bool TryPlaceBlockForWorldGenUnderwater(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, int minWaterDepth, int maxWaterDepth, BlockPatchAttributes attributes = null)
	{
		BlockPos blockPos = pos.DownCopy();
		NatFloat heightNatFloat = attributes?.Height ?? NatFloat.createGauss(2f, 2f);
		float flowChance = ((attributes != null && attributes.FlowerChance != -1f) ? attributes.FlowerChance : 0.7f);
		for (int i = 1; i < maxWaterDepth; i++)
		{
			blockPos.Down();
			Block block = blockAccessor.GetBlock(blockPos);
			if (block.Fertility > 0)
			{
				PlaceCrowfoot(blockAccessor, blockPos, i, worldGenRand, heightNatFloat, flowChance);
				return true;
			}
			if (block is BlockWaterPlant || !block.IsLiquid())
			{
				return false;
			}
		}
		return false;
	}

	internal void PlaceCrowfoot(IBlockAccessor blockAccessor, BlockPos pos, int depth, IRandom random, NatFloat heightNatFloat, float flowChance)
	{
		int num = Math.Min(depth, (int)heightNatFloat.nextFloat(1f, random));
		bool flag = random.NextFloat() < flowChance && num == depth;
		while (num-- > 1)
		{
			pos.Up();
			blockAccessor.SetBlock(blocks[0].BlockId, pos);
		}
		pos.Up();
		if (flag)
		{
			blockAccessor.SetBlock(blocks[2].BlockId, pos);
		}
		else
		{
			blockAccessor.SetBlock(blocks[1].BlockId, pos);
		}
	}
}
