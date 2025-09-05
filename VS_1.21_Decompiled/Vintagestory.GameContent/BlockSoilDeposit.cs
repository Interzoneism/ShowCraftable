using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockSoilDeposit : BlockSoil
{
	private int soilBlockId;

	protected override int MaxStage => 1;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		string text = Attributes?["placeBelowBlockCode"].AsString();
		if (text != null)
		{
			Block block = api.World.GetBlock(new AssetLocation(text));
			if (block != null)
			{
				soilBlockId = block.BlockId;
			}
		}
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		blockAccessor.SetBlock(BlockId, pos);
		if (soilBlockId > 0 && blockAccessor.GetBlockBelow(pos, 1, 1).BlockMaterial == EnumBlockMaterial.Stone)
		{
			blockAccessor.SetBlock(soilBlockId, pos.DownCopy());
		}
		return true;
	}

	public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		base.OnServerGameTick(world, pos, extra);
		GrassTick grassTick = extra as GrassTick;
		world.BlockAccessor.SetBlock(grassTick.Grass.BlockId, pos);
		if (grassTick.TallGrass != null && world.BlockAccessor.GetBlock(pos.UpCopy()).BlockId == 0)
		{
			world.BlockAccessor.SetBlock(grassTick.TallGrass.BlockId, pos.UpCopy());
		}
	}

	public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
	{
		extra = null;
		bool flag = false;
		BlockPos blockPos = pos.UpCopy();
		Block block;
		if (world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.MaxLight) < growthLightLevel || isSmotheringBlock(world, blockPos))
		{
			block = tryGetBlockForDying(world);
		}
		else
		{
			flag = true;
			block = tryGetBlockForGrowing(world, pos);
		}
		if (block != null)
		{
			extra = new GrassTick
			{
				Grass = block,
				TallGrass = (flag ? getTallGrassBlock(world, blockPos, offThreadRandom) : null)
			};
		}
		return extra != null;
	}
}
