using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockLooseGears : Block
{
	private Block[] Variants = new Block[5];

	private Random rand;

	public override void OnLoaded(ICoreAPI api)
	{
		rand = new Random();
		base.OnLoaded(api);
		Variants[0] = api.World.GetBlock(new AssetLocation("loosegears-1"));
		Variants[1] = api.World.GetBlock(new AssetLocation("loosegears-2"));
		Variants[2] = api.World.GetBlock(new AssetLocation("loosegears-3"));
		Variants[3] = api.World.GetBlock(new AssetLocation("loosegears-4"));
		Variants[4] = api.World.GetBlock(new AssetLocation("loosegears-5"));
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		Block block = blockAccessor.GetBlock(pos);
		if (block.Id != 0)
		{
			return false;
		}
		for (int i = 1; i < 5; i++)
		{
			block = blockAccessor.GetBlockBelow(pos, i, 1);
			if (block.SideSolid[BlockFacing.UP.Index])
			{
				blockAccessor.SetBlock(rndGearBlock().BlockId, pos.DownCopy(i - 1));
				return true;
			}
			if (block.Id != 0)
			{
				return false;
			}
		}
		return false;
	}

	private Block rndGearBlock()
	{
		double num = rand.Next();
		if (num < 0.0323)
		{
			return Variants[4];
		}
		if (num < 0.0968)
		{
			return Variants[3];
		}
		if (num < 0.2258)
		{
			return Variants[2];
		}
		if (num < 0.4839)
		{
			return Variants[1];
		}
		return Variants[0];
	}
}
