using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class BlockAccessorMapChunkLoading : BlockAccessorRelaxedBulkUpdate, IBulkBlockAccessor, IBlockAccessor
{
	private int chunkX;

	private int chunkZ;

	private IWorldChunk[] chunks;

	public BlockAccessorMapChunkLoading(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool debug)
		: base(worldmap, worldAccessor, synchronize, relight: false, debug)
	{
		base.debug = debug;
	}

	public new void SetChunks(Vec2i chunkCoord, IWorldChunk[] chunksCol)
	{
		chunks = chunksCol;
		chunkX = chunkCoord.X;
		chunkZ = chunkCoord.Y;
	}

	public override List<BlockUpdate> Commit()
	{
		FastCommit();
		return null;
	}

	public void FastCommit()
	{
		base.ReadFromStagedByDefault = false;
		IWorldChunk worldChunk = null;
		dirtyChunkPositions.Clear();
		int num = -99999;
		foreach (KeyValuePair<BlockPos, BlockUpdate> stagedBlock in StagedBlocks)
		{
			int newSolidBlockId = stagedBlock.Value.NewSolidBlockId;
			BlockPos key = stagedBlock.Key;
			int num2 = key.Y / 32;
			if (num2 != num)
			{
				worldChunk = chunks[num2];
				worldChunk.Unpack();
				worldChunk.MarkModified();
				int num3 = (key.Y - 1) / 32;
				if (num3 != num2 && num3 >= 0)
				{
					chunks[num3].MarkModified();
				}
				if (newSolidBlockId > 0 || stagedBlock.Value.NewFluidBlockId > 0)
				{
					worldChunk.Empty = false;
				}
				num = num2;
			}
			int index3d = worldmap.ChunkSizedIndex3D(key.X & 0x1F, key.Y & 0x1F, key.Z & 0x1F);
			Block block = null;
			if (stagedBlock.Value.NewSolidBlockId >= 0)
			{
				stagedBlock.Value.OldBlockId = worldChunk.Data[index3d];
				worldChunk.Data[index3d] = newSolidBlockId;
				block = worldmap.Blocks[newSolidBlockId];
			}
			if (stagedBlock.Value.NewFluidBlockId >= 0)
			{
				if (stagedBlock.Value.NewSolidBlockId < 0)
				{
					stagedBlock.Value.OldBlockId = worldChunk.Data.GetFluid(index3d);
				}
				worldChunk.Data.SetFluid(index3d, stagedBlock.Value.NewFluidBlockId);
				if (stagedBlock.Value.NewFluidBlockId > 0 || block == null)
				{
					block = worldmap.Blocks[stagedBlock.Value.NewFluidBlockId];
				}
			}
			UpdateRainHeightMap(worldmap.Blocks[stagedBlock.Value.OldBlockId], block, key, worldChunk.MapChunk);
		}
		StagedBlocks.Clear();
	}

	protected override int GetNonStagedBlockId(int posX, int posY, int posZ, int layer)
	{
		if ((posX | posY | posZ) < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			return 0;
		}
		return ((posX / 32 != chunkX || posZ / 32 != chunkZ) ? worldmap.GetChunkAtPos(posX, posY, posZ) : chunks[posY / 32])?.UnpackAndReadBlock(worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F), layer) ?? 0;
	}
}
