using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class BlockAccessorWorldGen : BlockAccessorBase, IWorldGenBlockAccessor, IBlockAccessor
{
	internal ChunkServerThread chunkdbthread;

	internal ServerMain server;

	[ThreadStatic]
	private static ServerChunk chunkCached;

	[ThreadStatic]
	private static long cachedChunkIndex3d;

	[ThreadStatic]
	private static ServerMapChunk mapchunkCached;

	[ThreadStatic]
	private static long cachedChunkIndex2d;

	private IServerWorldAccessor worldgenWorldAccessor;

	public IServerWorldAccessor WorldgenWorldAccessor => worldgenWorldAccessor ?? (worldgenWorldAccessor = new WorldgenWorldAccessor((IServerWorldAccessor)worldAccessor, this));

	public BlockAccessorWorldGen(ServerMain server, ChunkServerThread chunkdbthread)
		: base(server.WorldMap, null)
	{
		this.chunkdbthread = chunkdbthread;
		this.server = server;
		worldAccessor = server;
	}

	public void ScheduleBlockLightUpdate(BlockPos pos, int oldBlockid, int newBlockId)
	{
		ServerMapChunk serverMapChunk = (ServerMapChunk)GetMapChunk(pos.X / 32, pos.Z / 32);
		if (serverMapChunk == null)
		{
			ServerMain.Logger.Worldgen("Mapchunk was null when scheduling a blocklight update at " + pos);
			return;
		}
		if (serverMapChunk.ScheduledBlockLightUpdates == null)
		{
			serverMapChunk.ScheduledBlockLightUpdates = new List<Vec4i>();
		}
		serverMapChunk.ScheduledBlockLightUpdates.Add(new Vec4i(pos, newBlockId));
	}

	public void RunScheduledBlockLightUpdates(int chunkx, int chunkz)
	{
		ServerMapChunk serverMapChunk = (ServerMapChunk)GetMapChunk(chunkx, chunkz);
		if (serverMapChunk == null)
		{
			ServerMain.Logger.Worldgen("Mapchunk was null when attempting scheduled blocklight updates at " + chunkx + "," + chunkz);
			return;
		}
		List<Vec4i> scheduledBlockLightUpdates = serverMapChunk.ScheduledBlockLightUpdates;
		if (scheduledBlockLightUpdates == null || scheduledBlockLightUpdates.Count == 0)
		{
			return;
		}
		BlockPos blockPos = new BlockPos();
		foreach (Vec4i item in scheduledBlockLightUpdates)
		{
			Block block = server.Blocks[item.W];
			blockPos.SetAndCorrectDimension(item.X, item.Y, item.Z);
			byte[] lightHsv = block.GetLightHsv(this, blockPos);
			if (lightHsv[2] > 0)
			{
				server.WorldMap.chunkIlluminatorWorldGen.PlaceBlockLight(lightHsv, blockPos.X, blockPos.InternalY, blockPos.Z);
			}
		}
		serverMapChunk.ScheduledBlockLightUpdates = null;
	}

	public void ScheduleBlockUpdate(BlockPos pos)
	{
		ChunkColumnLoadRequest chunkRequestAtPos = chunkdbthread.GetChunkRequestAtPos(pos.X, pos.Z);
		if (chunkRequestAtPos?.MapChunk != null)
		{
			chunkRequestAtPos.MapChunk.ScheduledBlockUpdates.Add(pos.Copy());
		}
	}

	public override IMapChunk GetMapChunk(Vec2i chunkPos)
	{
		return GetMapChunk(chunkPos.X, chunkPos.Y);
	}

	public override IMapChunk GetMapChunk(int chunkX, int chunkZ)
	{
		long num = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		if (cachedChunkIndex2d == num)
		{
			return mapchunkCached;
		}
		ServerMapChunk mapChunk = chunkdbthread.GetMapChunk(num);
		if (mapChunk != null)
		{
			cachedChunkIndex2d = num;
			mapchunkCached = mapChunk;
		}
		return mapChunk;
	}

	public override IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		return chunkdbthread.GetGeneratingChunk(chunkX, chunkY, chunkZ);
	}

	[Obsolete("Please use BlockPos version instead for dimension awareness")]
	public override IWorldChunk GetChunkAtBlockPos(int posX, int posY, int posZ)
	{
		return chunkdbthread.GetGeneratingChunk(posX / 32, posY / 32, posZ / 32);
	}

	public override IWorldChunk GetChunkAtBlockPos(BlockPos pos)
	{
		return chunkdbthread.GetGeneratingChunk(pos.X / 32, pos.Y / 32, pos.Z / 32);
	}

	public override IMapRegion GetMapRegion(int regionX, int regionZ)
	{
		return chunkdbthread.GetMapRegion(regionX, regionZ);
	}

	public override int GetBlockId(int posX, int posY, int posZ, int layer)
	{
		long num = worldmap.ChunkIndex3D(posX / 32, posY / 32, posZ / 32);
		ServerChunk serverChunk;
		if (cachedChunkIndex3d == num)
		{
			serverChunk = chunkCached;
		}
		else
		{
			serverChunk = chunkdbthread.GetGeneratingChunkAtPos(posX, posY, posZ);
			if (serverChunk == null)
			{
				serverChunk = worldmap.GetChunkAtPos(posX, posY, posZ) as ServerChunk;
			}
			if (serverChunk != null)
			{
				serverChunk.Unpack();
				cachedChunkIndex3d = num;
				chunkCached = serverChunk;
			}
		}
		if (serverChunk != null)
		{
			return serverChunk.Data.GetBlockId(worldmap.ChunkSizedIndex3D(posX & MagicNum.ServerChunkSizeMask, posY & MagicNum.ServerChunkSizeMask, posZ & MagicNum.ServerChunkSizeMask), layer);
		}
		if (RuntimeEnv.DebugOutOfRangeBlockAccess)
		{
			ServerMain.Logger.Notification("Tried to get block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}). ", posX, posY, posZ, posX / MagicNum.ServerChunkSize, posY / MagicNum.ServerChunkSize, posZ / MagicNum.ServerChunkSize);
			ServerMain.Logger.Notification(new StackTrace()?.ToString() ?? "");
		}
		else
		{
			ServerMain.Logger.Notification("Tried to get block outside generating chunks! Set RuntimeEnv.DebugOutOfRangeBlockAccess to debug.");
		}
		return 0;
	}

	public override Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4)
	{
		if (posX < 0 || posY < 0 || posZ < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			return null;
		}
		ServerChunk generatingChunkAtPos = chunkdbthread.GetGeneratingChunkAtPos(posX, posY, posZ);
		if (generatingChunkAtPos != null)
		{
			generatingChunkAtPos.Unpack();
			return worldmap.Blocks[generatingChunkAtPos.Data[worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F)]];
		}
		return null;
	}

	public override void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack = null)
	{
		Block block = worldmap.Blocks[blockId];
		if (block.ForFluidsLayer)
		{
			SetFluidBlock(blockId, pos);
			return;
		}
		ServerChunk generatingChunkAtPos = chunkdbthread.GetGeneratingChunkAtPos(pos);
		if (generatingChunkAtPos != null)
		{
			SetSolidBlock(generatingChunkAtPos, pos, block, blockId);
		}
		else if (worldmap.GetChunkAtPos(pos.X, pos.Y, pos.Z) is ServerChunk chunk)
		{
			int oldBlockid = SetSolidBlock(chunk, pos, block, blockId);
			if (block.LightHsv[2] > 0)
			{
				ScheduleBlockLightUpdate(pos, oldBlockid, blockId);
			}
		}
		else if (RuntimeEnv.DebugOutOfRangeBlockAccess)
		{
			ServerMain.Logger.Notification("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}) when placing {6}", pos.X, pos.Y, pos.Z, pos.X / MagicNum.ServerChunkSize, pos.Y / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize, worldAccessor.GetBlock(blockId));
			ServerMain.Logger.VerboseDebug("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}) when placing {6}", pos.X, pos.Y, pos.Z, pos.X / MagicNum.ServerChunkSize, pos.Y / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize, worldAccessor.GetBlock(blockId));
			ServerMain.Logger.VerboseDebug(new StackTrace()?.ToString() ?? "");
		}
		else
		{
			ServerMain.Logger.Notification("Tried to set block outside generating chunks! Set RuntimeEnv.DebugOutOfRangeBlockAccess to debug.");
		}
	}

	protected int SetSolidBlock(ServerChunk chunk, BlockPos pos, Block newBlock, int blockId)
	{
		chunk.Unpack();
		int index3d = worldmap.ChunkSizedIndex3D(pos.X & MagicNum.ServerChunkSizeMask, pos.Y & MagicNum.ServerChunkSizeMask, pos.Z & MagicNum.ServerChunkSizeMask);
		int blockId2 = chunk.Data.GetBlockId(index3d, 1);
		if (blockId2 != 0 && worldmap.Blocks[blockId2].EntityClass != null)
		{
			chunk.RemoveBlockEntity(pos);
			((ServerMapChunk)chunk.MapChunk).NewBlockEntities.Remove(pos);
		}
		chunk.Data[index3d] = blockId;
		if (newBlock.DisplacesLiquids(this, pos))
		{
			chunk.Data.SetFluid(index3d, 0);
		}
		chunk.DirtyForSaving = true;
		return blockId2;
	}

	public override void SetBlock(int blockId, BlockPos pos, int layer)
	{
		switch (layer)
		{
		case 2:
			SetFluidBlock(blockId, pos);
			break;
		case 1:
			SetBlock(blockId, pos);
			break;
		default:
			throw new ArgumentException("Layer must be solid or fluid");
		}
	}

	public void SetFluidBlock(int blockId, BlockPos pos)
	{
		ServerChunk generatingChunkAtPos = chunkdbthread.GetGeneratingChunkAtPos(pos);
		if (generatingChunkAtPos != null)
		{
			generatingChunkAtPos.Unpack();
			int index3d = worldmap.ChunkSizedIndex3D(pos.X & MagicNum.ServerChunkSizeMask, pos.Y & MagicNum.ServerChunkSizeMask, pos.Z & MagicNum.ServerChunkSizeMask);
			generatingChunkAtPos.Data.SetFluid(index3d, blockId);
		}
		else if (worldmap.GetChunkAtPos(pos.X, pos.Y, pos.Z) is ServerChunk serverChunk)
		{
			serverChunk.Unpack();
			int index3d2 = worldmap.ChunkSizedIndex3D(pos.X & MagicNum.ServerChunkSizeMask, pos.Y & MagicNum.ServerChunkSizeMask, pos.Z & MagicNum.ServerChunkSizeMask);
			serverChunk.Data.SetFluid(index3d2, blockId);
			serverChunk.DirtyForSaving = true;
		}
		else if (RuntimeEnv.DebugOutOfRangeBlockAccess)
		{
			ServerMain.Logger.Notification("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}) when placing {6}", pos.X, pos.Y, pos.Z, pos.X / MagicNum.ServerChunkSize, pos.Y / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize, worldAccessor.GetBlock(blockId));
			ServerMain.Logger.VerboseDebug("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}) when placing {6}", pos.X, pos.Y, pos.Z, pos.X / MagicNum.ServerChunkSize, pos.Y / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize, worldAccessor.GetBlock(blockId));
			ServerMain.Logger.VerboseDebug(new StackTrace()?.ToString() ?? "");
		}
		else
		{
			ServerMain.Logger.Notification("Tried to set block outside generating chunks! Set RuntimeEnv.DebugOutOfRangeBlockAccess to debug.");
		}
	}

	public override List<BlockUpdate> Commit()
	{
		return null;
	}

	public override void ExchangeBlock(int blockId, BlockPos pos)
	{
		SetBlock(blockId, pos);
	}

	public override void MarkChunkDecorsModified(BlockPos pos)
	{
		if (chunkdbthread.GetGeneratingChunkAtPos(pos) == null)
		{
			base.MarkChunkDecorsModified(pos);
		}
	}

	public override void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null)
	{
		ServerChunk generatingChunkAtPos = chunkdbthread.GetGeneratingChunkAtPos(position);
		if (generatingChunkAtPos != null)
		{
			BlockEntity blockEntity = ServerMain.ClassRegistry.CreateBlockEntity(classname);
			Block localBlockAtBlockPos = generatingChunkAtPos.GetLocalBlockAtBlockPos(server, position);
			blockEntity.CreateBehaviors(localBlockAtBlockPos, server);
			blockEntity.Pos = position.Copy();
			generatingChunkAtPos.AddBlockEntity(blockEntity);
			blockEntity.stackForWorldgen = byItemStack;
			((ServerMapChunk)generatingChunkAtPos.MapChunk).NewBlockEntities.Add(position.Copy());
		}
	}

	public void AddEntity(Entity entity)
	{
		ServerChunk generatingChunkAtPos = chunkdbthread.GetGeneratingChunkAtPos(entity.ServerPos.AsBlockPos);
		if (generatingChunkAtPos != null)
		{
			entity.EntityId = ++server.SaveGameData.LastEntityId;
			generatingChunkAtPos.AddEntity(entity);
		}
	}

	public override BlockEntity GetBlockEntity(BlockPos position)
	{
		return chunkdbthread.GetGeneratingChunkAtPos(position)?.GetLocalBlockEntityAtBlockPos(position);
	}

	public override void RemoveBlockEntity(BlockPos position)
	{
		chunkdbthread.GetGeneratingChunkAtPos(position)?.RemoveBlockEntity(position);
	}

	public void BeginColumn()
	{
		cachedChunkIndex3d = -1L;
		cachedChunkIndex2d = -1L;
	}

	public static void ThreadDispose()
	{
		chunkCached = null;
		mapchunkCached = null;
	}

	protected override ChunkData[] LoadChunksToCache(int mincx, int mincy, int mincz, int maxcx, int maxcy, int maxcz, Action<int, int, int> onChunkMissing)
	{
		int num = maxcx - mincx + 1;
		int num2 = maxcy - mincy + 1;
		int num3 = maxcz - mincz + 1;
		ChunkData[] array = new ChunkData[num * num2 * num3];
		for (int i = mincy; i <= maxcy; i++)
		{
			int num4 = (i - mincy) * num3 - mincz;
			for (int j = mincz; j <= maxcz; j++)
			{
				int num5 = (num4 + j) * num - mincx;
				for (int k = mincx; k <= maxcx; k++)
				{
					IWorldChunk worldChunk = chunkdbthread.GetGeneratingChunk(k, i, j);
					if (worldChunk == null)
					{
						worldChunk = worldmap.GetChunk(k, i, j);
					}
					if (worldChunk == null)
					{
						array[num5 + k] = null;
						onChunkMissing?.Invoke(k, i, j);
					}
					else
					{
						worldChunk.Unpack();
						array[num5 + k] = worldChunk.Data as ChunkData;
					}
				}
			}
		}
		return array;
	}
}
