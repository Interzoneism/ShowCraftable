using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCharcoalPile : BlockLayeredSlowDig
{
	public override bool CanAcceptFallOnto(IWorldAccessor world, BlockPos pos, Block fallingBlock, TreeAttribute blockEntityAttributes)
	{
		if (fallingBlock is BlockCharcoalPile)
		{
			if (world.BlockAccessor.GetMostSolidBlock(pos) is BlockCharcoalPile blockCharcoalPile)
			{
				return blockCharcoalPile.CountLayers() < 8;
			}
			return false;
		}
		return false;
	}

	public override bool OnFallOnto(IWorldAccessor world, BlockPos pos, Block block, TreeAttribute blockEntityAttributes)
	{
		BlockCharcoalPile blockCharcoalPile = world.BlockAccessor.GetMostSolidBlock(pos) as BlockCharcoalPile;
		BlockCharcoalPile blockCharcoalPile2 = block as BlockCharcoalPile;
		if (blockCharcoalPile2 != null && blockCharcoalPile != null && blockCharcoalPile.CountLayers() < 8)
		{
			while (blockCharcoalPile.CountLayers() < 8 && blockCharcoalPile2 != null)
			{
				blockCharcoalPile = blockCharcoalPile.GetNextLayer(world) as BlockCharcoalPile;
				blockCharcoalPile2 = blockCharcoalPile2.GetPrevLayer(world) as BlockCharcoalPile;
			}
			int num = 0;
			while (num == 0)
			{
				num = world.BlockAccessor.GetMostSolidBlock(pos.Down()).BlockId;
				if (num != 0)
				{
					pos.Up();
				}
			}
			world.BlockAccessor.SetBlock(blockCharcoalPile.BlockId, pos);
			if (blockCharcoalPile2 != null)
			{
				BlockPos pos2 = pos.UpCopy();
				Block mostSolidBlock = world.BlockAccessor.GetMostSolidBlock(pos2);
				if (mostSolidBlock.BlockId == 0)
				{
					world.BlockAccessor.SetBlock(blockCharcoalPile2.BlockId, pos2);
				}
				else
				{
					mostSolidBlock.OnFallOnto(world, pos, blockCharcoalPile2, blockEntityAttributes);
				}
			}
			world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
			return true;
		}
		return base.OnFallOnto(world, pos, block, blockEntityAttributes);
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		BlockPos pos2 = pos.UpCopy();
		world.BlockAccessor.GetBlock(pos2).OnNeighbourBlockChange(world, pos2, pos.Copy());
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block block = world.BlockAccessor.GetBlock(pos);
		BlockPos blockPos = pos.UpCopy();
		Block block2 = null;
		while (blockPos.Y < world.BlockAccessor.MapSizeY)
		{
			block2 = world.BlockAccessor.GetBlock(blockPos);
			if (block2.FirstCodePart() != block.FirstCodePart())
			{
				break;
			}
			blockPos.Up();
		}
		if (blockPos == pos.UpCopy() || block2 == null || (byPlayer != null && byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative))
		{
			base.OnBlockBroken(world, pos, byPlayer, 1f);
			world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
		}
		else if (block2.FirstCodePart() == block.FirstCodePart())
		{
			block2.OnBlockBroken(world, blockPos, byPlayer);
		}
		else
		{
			BlockPos pos2 = blockPos.DownCopy();
			world.BlockAccessor.GetBlock(pos2).OnBlockBroken(world, pos2, byPlayer);
		}
	}

	public override float RandomSoundPitch(IWorldAccessor world)
	{
		return (float)world.Rand.NextDouble() * 0.24f + 0.88f;
	}
}
