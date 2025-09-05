using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class BlockAccessorPrefetch : BlockAccessorRelaxed, IBlockAccessorPrefetch, IBlockAccessor
{
	private BlockPos basePos = new BlockPos();

	private int sizeX;

	private int sizeY;

	private int sizeZ;

	private int prefetchBlocksCount;

	private List<Block> prefetchedBlocks = new List<Block>();

	private Block airBlock;

	public BlockAccessorPrefetch(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool relight)
		: base(worldmap, worldAccessor, synchronize, relight)
	{
		airBlock = GetBlock(new AssetLocation("air"));
	}

	public void PrefetchBlocks(BlockPos minPos, BlockPos maxPos)
	{
		prefetchBlocksCount = 0;
		basePos.Set(Math.Min(minPos.X, maxPos.X), Math.Min(minPos.Y, maxPos.Y), Math.Min(minPos.Z, maxPos.Z));
		sizeX = Math.Max(minPos.X, maxPos.X) - basePos.X + 1;
		sizeY = Math.Max(minPos.Y, maxPos.Y) - basePos.Y + 1;
		sizeZ = Math.Max(minPos.Z, maxPos.Z) - basePos.Z + 1;
		prefetchBlocksCount = sizeX * sizeY * sizeZ;
		while (prefetchedBlocks.Count < prefetchBlocksCount)
		{
			prefetchedBlocks.Add(airBlock);
		}
		WalkBlocks(minPos, maxPos, delegate(Block block, int x, int y, int z)
		{
			int num = x - basePos.X;
			int num2 = y - basePos.Y;
			int num3 = z - basePos.Z;
			prefetchedBlocks[(num2 * sizeZ + num3) * sizeX + num] = block;
		});
	}

	public void WalkBlocks(BlockPos minPos, BlockPos maxPos, Action<Block, int, int, int> onBlock)
	{
		int num = GameMath.Clamp(Math.Min(minPos.X, maxPos.X), 0, worldmap.MapSizeX);
		int num2 = GameMath.Clamp(Math.Min(minPos.Y, maxPos.Y), 0, worldmap.MapSizeY);
		int num3 = GameMath.Clamp(Math.Min(minPos.Z, maxPos.Z), 0, worldmap.MapSizeZ);
		int num4 = GameMath.Clamp(Math.Max(minPos.X, maxPos.X), 0, worldmap.MapSizeX);
		int num5 = GameMath.Clamp(Math.Max(minPos.Y, maxPos.Y), 0, worldmap.MapSizeY);
		int num6 = GameMath.Clamp(Math.Max(minPos.Z, maxPos.Z), 0, worldmap.MapSizeZ);
		int num7 = num / 32;
		int num8 = num2 / 32;
		int num9 = num3 / 32;
		int num10 = num4 / 32;
		int num11 = num5 / 32;
		int num12 = num6 / 32;
		int num13 = minPos.dimension * 1024;
		ChunkData[] array = LoadChunksToCache(num7, num8 + num13, num9, num10, num11 + num13, num12, null);
		int num14 = num10 - num7 + 1;
		int num15 = num12 - num9 + 1;
		for (int i = num2; i <= num5; i++)
		{
			int num16 = (i / 32 - num8) * num15 - num9;
			for (int j = num3; j <= num6; j++)
			{
				int num17 = (num16 + j / 32) * num14 - num7;
				int num18 = (i % 32 * 32 + j % 32) * 32;
				for (int k = num; k <= num4; k++)
				{
					ChunkData chunkData = array[num17 + k / 32];
					if (chunkData != null)
					{
						int index3d = num18 + k % 32;
						int solidBlock = chunkData.GetSolidBlock(index3d);
						onBlock(worldmap.Blocks[solidBlock], k, i, j);
					}
				}
			}
		}
	}

	public override int GetBlockId(int posX, int posY, int posZ, int layer)
	{
		if ((posX | posY | posZ) < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			return 0;
		}
		return prefetchedBlocks[((posY - basePos.Y) * sizeZ + (posZ - basePos.Z)) * sizeX + (posX - basePos.X)].BlockId;
	}
}
