using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common.Database;

namespace Vintagestory.Common;

public class BlockAccessorBulkMinimalUpdate : BlockAccessorRelaxedBulkUpdate
{
	protected HashSet<Xyz> dirtyNeighbourChunkPositions;

	public BlockAccessorBulkMinimalUpdate(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool debug)
		: base(worldmap, worldAccessor, synchronize, relight: false, debug)
	{
		base.debug = debug;
		if (worldAccessor.Side == EnumAppSide.Client)
		{
			dirtyNeighbourChunkPositions = new HashSet<Xyz>();
		}
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
		int num = -1;
		int num2 = -1;
		int num3 = -1;
		dirtyChunkPositions.Clear();
		dirtyNeighbourChunkPositions?.Clear();
		WorldMap worldMap = worldmap;
		IList<Block> blocks = worldMap.Blocks;
		foreach (KeyValuePair<BlockPos, BlockUpdate> stagedBlock in StagedBlocks)
		{
			BlockUpdate value = stagedBlock.Value;
			int newSolidBlockId = value.NewSolidBlockId;
			if (newSolidBlockId < 0 && value.NewFluidBlockId < 0)
			{
				continue;
			}
			BlockPos key = stagedBlock.Key;
			int num4 = key.X / 32;
			int num5 = key.Y / 32;
			int num6 = key.Z / 32;
			if (dirtyNeighbourChunkPositions != null)
			{
				if ((key.X + 1) % 32 < 2)
				{
					dirtyNeighbourChunkPositions.Add(new Xyz((key.X % 32 == 0) ? (num4 - 1) : (num4 + 1), num5, num6));
				}
				if ((key.Y + 1) % 32 < 2)
				{
					dirtyNeighbourChunkPositions.Add(new Xyz(num4, (key.Y % 32 == 0) ? (num5 - 1) : (num5 + 1), num6));
				}
				if ((key.Z + 1) % 32 < 2)
				{
					dirtyNeighbourChunkPositions.Add(new Xyz(num4, num5, (key.Z % 32 == 0) ? (num6 - 1) : (num6 + 1)));
				}
			}
			if (num4 != num || num5 != num2 || num6 != num3)
			{
				worldChunk = worldMap.GetChunk(num = num4, num2 = num5, num3 = num6);
				if (worldChunk == null)
				{
					continue;
				}
				worldChunk.Unpack();
				dirtyChunkPositions.Add(new ChunkPosCompact(num4, num5, num6));
				int num7 = (key.Y - 1) / 32;
				if (num7 != num5 && num7 >= 0)
				{
					dirtyChunkPositions.Add(new ChunkPosCompact(num4, num7, num6));
				}
				if (newSolidBlockId > 0 || value.NewFluidBlockId > 0)
				{
					worldChunk.Empty = false;
				}
			}
			if (worldChunk == null)
			{
				continue;
			}
			int index3d = worldMap.ChunkSizedIndex3D(key.X & 0x1F, key.Y & 0x1F, key.Z & 0x1F);
			Block block = null;
			if (value.NewSolidBlockId >= 0)
			{
				int index = (value.OldBlockId = worldChunk.Data[index3d]);
				if (!value.ExchangeOnly)
				{
					blocks[index].OnBlockRemoved(worldAccessor, key);
				}
				worldChunk.Data[index3d] = newSolidBlockId;
				block = blocks[newSolidBlockId];
				if (!value.ExchangeOnly)
				{
					block.OnBlockPlaced(worldAccessor, key);
				}
			}
			if (value.NewFluidBlockId >= 0)
			{
				if (value.NewSolidBlockId < 0)
				{
					value.OldBlockId = worldChunk.Data.GetFluid(index3d);
				}
				worldChunk.Data.SetFluid(index3d, value.NewFluidBlockId);
				if (value.NewFluidBlockId > 0 || block == null)
				{
					block = blocks[value.NewFluidBlockId];
				}
			}
			if (value.ExchangeOnly && block.EntityClass != null)
			{
				worldChunk.GetLocalBlockEntityAtBlockPos(key)?.OnExchanged(block);
			}
			UpdateRainHeightMap(blocks[value.OldBlockId], block, key, worldChunk.MapChunk);
		}
		foreach (ChunkPosCompact dirtyChunkPosition in dirtyChunkPositions)
		{
			worldMap.MarkChunkDirty(dirtyChunkPosition.X, dirtyChunkPosition.Y, dirtyChunkPosition.Z);
		}
		if (dirtyNeighbourChunkPositions != null)
		{
			foreach (Xyz dirtyNeighbourChunkPosition in dirtyNeighbourChunkPositions)
			{
				worldMap.MarkChunkDirty(dirtyNeighbourChunkPosition.X, dirtyNeighbourChunkPosition.Y, dirtyNeighbourChunkPosition.Z, priority: false, sunRelight: false, null, fireDirtyEvent: false, edgeOnly: true);
			}
		}
		if (synchronize)
		{
			worldMap.SendBlockUpdateBulkMinimal(StagedBlocks);
		}
		StagedBlocks.Clear();
		dirtyChunkPositions.Clear();
	}
}
