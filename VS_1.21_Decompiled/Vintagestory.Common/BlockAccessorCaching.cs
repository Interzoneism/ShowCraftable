using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Common;

public class BlockAccessorCaching : BlockAccessorRelaxed, ICachingBlockAccessor, IBlockAccessor
{
	private long chunkIndex3d = -1L;

	private long chunk2Index3d = -1L;

	private IWorldChunk chunk;

	private IWorldChunk chunk2;

	private IChunkBlocks chunkDataBlocks;

	public bool LastChunkLoaded { get; private set; }

	public BlockAccessorCaching(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool relight)
		: base(worldmap, worldAccessor, synchronize, relight)
	{
		if (worldAccessor.Api is ClientCoreAPI clientCoreAPI)
		{
			clientCoreAPI.eventapi.LeftWorld += Dispose;
		}
	}

	public override int GetBlockId(int posX, int posY, int posZ, int layer)
	{
		if ((posX | posY | posZ) < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			return 0;
		}
		long num = worldmap.ChunkIndex3D(posX / 32, posY / 32, posZ / 32);
		if (chunkIndex3d != num)
		{
			if (chunk2Index3d == num)
			{
				chunk2Index3d = chunkIndex3d;
				chunkIndex3d = num;
				IWorldChunk worldChunk = chunk2;
				chunk2 = chunk;
				chunk = worldChunk;
				if (worldChunk != null)
				{
					LastChunkLoaded = true;
					worldChunk.Unpack();
					chunkDataBlocks = (worldChunk as WorldChunk).Data;
				}
				else
				{
					LastChunkLoaded = false;
				}
			}
			else
			{
				chunk2Index3d = chunkIndex3d;
				chunk2 = chunk;
				chunkIndex3d = num;
				IWorldChunk worldChunk2 = (chunk = worldmap.GetChunk(num));
				if (worldChunk2 != null)
				{
					LastChunkLoaded = true;
					worldChunk2.Unpack();
					chunkDataBlocks = (worldChunk2 as WorldChunk).Data;
				}
				else
				{
					LastChunkLoaded = false;
				}
			}
		}
		IWorldChunk worldChunk3 = chunk;
		if (worldChunk3 != null)
		{
			worldChunk3.MarkFresh();
			int index3d = worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F);
			return (worldChunk3 as WorldChunk).Data.GetBlockId(index3d, layer);
		}
		return 0;
	}

	public override void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack = null)
	{
		if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension == 0 && (pos.X >= worldmap.MapSizeX || pos.Y >= worldmap.MapSizeY || pos.Z >= worldmap.MapSizeZ)))
		{
			return;
		}
		int chunkX = pos.X / 32;
		int chunkY = pos.Y / 32;
		int chunkZ = pos.Z / 32;
		long num = worldmap.ChunkIndex3D(chunkX, chunkY, chunkZ);
		if (chunkIndex3d != num)
		{
			if (chunk2Index3d == num)
			{
				chunk2Index3d = chunkIndex3d;
				chunkIndex3d = num;
				IWorldChunk worldChunk = chunk2;
				chunk2 = chunk;
				chunk = worldChunk;
			}
			else
			{
				chunk2Index3d = chunkIndex3d;
				chunkIndex3d = num;
				chunk2 = chunk;
				chunk = worldmap.GetChunk(chunkIndex3d);
				chunk.Unpack();
			}
			if (chunk != null)
			{
				chunkDataBlocks = (chunk as WorldChunk).Data;
			}
		}
		if (chunk != null)
		{
			LastChunkLoaded = true;
			SetBlockInternal(blockId, pos, chunk, synchronize, relight, 0, byItemstack);
		}
		else
		{
			LastChunkLoaded = false;
		}
	}

	public override void SetBlock(int blockId, BlockPos pos, int layer)
	{
		if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension == 0 && (pos.X >= worldmap.MapSizeX || pos.Y >= worldmap.MapSizeY || pos.Z >= worldmap.MapSizeZ)))
		{
			return;
		}
		int chunkX = pos.X / 32;
		int chunkY = pos.Y / 32;
		int chunkZ = pos.Z / 32;
		long num = worldmap.ChunkIndex3D(chunkX, chunkY, chunkZ);
		if (chunkIndex3d != num)
		{
			if (chunk2Index3d == num)
			{
				chunk2Index3d = chunkIndex3d;
				chunkIndex3d = num;
				IWorldChunk worldChunk = chunk2;
				chunk2 = chunk;
				chunk = worldChunk;
			}
			else
			{
				chunk2Index3d = chunkIndex3d;
				chunkIndex3d = num;
				chunk2 = chunk;
				chunk = worldmap.GetChunk(chunkIndex3d);
				chunk.Unpack();
			}
			if (chunk != null)
			{
				chunkDataBlocks = (chunk as WorldChunk).Data;
			}
		}
		if (chunk != null)
		{
			LastChunkLoaded = true;
			SetFluidBlockInternal(blockId, pos, chunk, synchronize, relight);
		}
		else
		{
			LastChunkLoaded = false;
		}
	}

	public override void ExchangeBlock(int blockId, BlockPos pos)
	{
		if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension == 0 && (pos.X >= worldmap.MapSizeX || pos.Y >= worldmap.MapSizeY || pos.Z >= worldmap.MapSizeZ)))
		{
			return;
		}
		long num = worldmap.ChunkIndex3D(pos.X / 32, pos.InternalY / 32, pos.Z / 32);
		if (chunkIndex3d != num)
		{
			if (chunk2Index3d == num)
			{
				chunk2Index3d = chunkIndex3d;
				chunkIndex3d = num;
				IWorldChunk worldChunk = chunk2;
				chunk2 = chunk;
				chunk = worldChunk;
			}
			else
			{
				chunk2Index3d = chunkIndex3d;
				chunkIndex3d = num;
				chunk2 = chunk;
				chunk = worldmap.GetChunk(chunkIndex3d);
				chunk.Unpack();
			}
			if (chunk != null)
			{
				chunkDataBlocks = (chunk as WorldChunk).Data;
			}
		}
		if (chunk != null)
		{
			int index3d = worldmap.ChunkSizedIndex3D(pos.X & 0x1F, pos.InternalY & 0x1F, pos.Z & 0x1F);
			int oldblockid = chunk.Data[index3d];
			chunk.Data[index3d] = blockId;
			chunk.MarkModified();
			Block block = worldmap.Blocks[blockId];
			if (!block.ForFluidsLayer)
			{
				chunk.GetLocalBlockEntityAtBlockPos(pos)?.OnExchanged(block);
			}
			if (synchronize)
			{
				worldmap.SendExchangeBlock(blockId, pos.X, pos.InternalY, pos.Z);
			}
			if (relight)
			{
				worldmap.UpdateLighting(oldblockid, blockId, pos);
			}
		}
	}

	public void Begin()
	{
		chunkIndex3d = -1L;
		chunk2Index3d = -1L;
	}

	public void Dispose()
	{
		chunk = null;
		chunk2 = null;
		chunkDataBlocks = null;
	}
}
