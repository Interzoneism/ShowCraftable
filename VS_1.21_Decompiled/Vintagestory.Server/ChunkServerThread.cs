using System;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ChunkServerThread : ServerThread, IChunkProvider
{
	internal GameDatabase gameDatabase;

	internal ConcurrentIndexedFifoQueue<ChunkColumnLoadRequest> requestedChunkColumns;

	internal IndexedFifoQueue<ChunkColumnLoadRequest> peekingChunkColumns;

	internal ServerSystemSupplyChunks loadsavechunks;

	internal ServerSystemLoadAndSaveGame loadsavegame;

	internal IBlockAccessor worldgenBlockAccessor;

	public bool runOffThreadSaveNow;

	public bool BackupInProgress;

	public bool peekMode;

	public int additionalWorldGenThreadsCount;

	private bool additionalThreadsPaused;

	public ILogger Logger => ServerMain.Logger;

	public ChunkServerThread(ServerMain server, string threadname, CancellationToken cancellationToken)
		: base(server, threadname, cancellationToken)
	{
		int val = 5;
		additionalWorldGenThreadsCount = Math.Min(val, MagicNum.MaxWorldgenThreads - 1);
		if (server.ReducedServerThreads)
		{
			additionalWorldGenThreadsCount = 0;
		}
		if (additionalWorldGenThreadsCount < 0)
		{
			additionalWorldGenThreadsCount = 0;
		}
	}

	protected override void UpdatePausedStatus(bool newpause)
	{
		if (ShouldPause != additionalThreadsPaused)
		{
			TogglePause(!additionalThreadsPaused);
		}
		base.UpdatePausedStatus(newpause);
	}

	private void TogglePause(bool paused)
	{
		ServerSystemSupplyChunks serverSystemSupplyChunks = (ServerSystemSupplyChunks)serversystems[0];
		if (paused)
		{
			serverSystemSupplyChunks.PauseAllWorldgenThreads(1500);
			serverSystemSupplyChunks.FullyClearGeneratingQueue();
		}
		else
		{
			serverSystemSupplyChunks.ResumeAllWorldgenThreads();
			if (additionalWorldGenThreadsCount > 0)
			{
				ServerMain.Logger.VerboseDebug("Un-pausing all worldgen threads.");
			}
		}
		additionalThreadsPaused = paused;
	}

	public ServerChunk GetGeneratingChunkAtPos(int posX, int posY, int posZ)
	{
		return GetGeneratingChunk(posX / MagicNum.ServerChunkSize, posY / MagicNum.ServerChunkSize, posZ / MagicNum.ServerChunkSize);
	}

	public ServerChunk GetGeneratingChunkAtPos(BlockPos pos)
	{
		return GetGeneratingChunk(pos.X / MagicNum.ServerChunkSize, pos.Y / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize);
	}

	public ChunkColumnLoadRequest GetChunkRequestAtPos(int posX, int posZ)
	{
		long index = server.WorldMap.MapChunkIndex2D(posX / MagicNum.ServerChunkSize, posZ / MagicNum.ServerChunkSize);
		if (!peekMode)
		{
			return requestedChunkColumns.GetByIndex(index);
		}
		return peekingChunkColumns.GetByIndex(index);
	}

	internal ServerChunk GetGeneratingChunk(int chunkX, int chunkY, int chunkZ)
	{
		long index = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		ChunkColumnLoadRequest chunkColumnLoadRequest = (peekMode ? peekingChunkColumns.GetByIndex(index) : requestedChunkColumns.GetByIndex(index));
		if (chunkColumnLoadRequest != null && chunkColumnLoadRequest.CurrentIncompletePass > EnumWorldGenPass.None && chunkY >= 0 && chunkY < chunkColumnLoadRequest.Chunks.Length)
		{
			return chunkColumnLoadRequest.Chunks[chunkY];
		}
		return null;
	}

	internal ServerMapChunk GetMapChunk(long index2d)
	{
		if (!server.loadedMapChunks.TryGetValue(index2d, out var value))
		{
			return (peekMode ? peekingChunkColumns.GetByIndex(index2d) : requestedChunkColumns.GetByIndex(index2d))?.MapChunk;
		}
		return value;
	}

	internal ServerMapRegion GetMapRegion(int regionX, int regionZ)
	{
		if (!server.loadedMapRegions.TryGetValue(server.WorldMap.MapRegionIndex2D(regionX, regionZ), out var value))
		{
			int num = regionX * server.WorldMap.RegionSize;
			int num2 = regionZ * server.WorldMap.RegionSize;
			int chunkX = num / 32;
			int chunkZ = num2 / 32;
			long index = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
			return (peekMode ? peekingChunkColumns.GetByIndex(index) : requestedChunkColumns.GetByIndex(index))?.MapChunk?.MapRegion;
		}
		return value;
	}

	IWorldChunk IChunkProvider.GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		return GetGeneratingChunk(chunkX, chunkY, chunkZ);
	}

	IWorldChunk IChunkProvider.GetUnpackedChunkFast(int chunkX, int chunkY, int chunkZ, bool notRecentlyAccessed)
	{
		ServerChunk generatingChunk = GetGeneratingChunk(chunkX, chunkY, chunkZ);
		if (generatingChunk != null)
		{
			((IWorldChunk)generatingChunk).Unpack();
			return generatingChunk;
		}
		return generatingChunk;
	}

	internal bool addChunkColumnRequest(long index2d, int chunkX, int chunkZ, int clientid, EnumWorldGenPass untilPass = EnumWorldGenPass.Done, ITreeAttribute chunkLoadParams = null)
	{
		return addChunkColumnRequest(new ChunkColumnLoadRequest(index2d, chunkX, chunkZ, clientid, (int)untilPass, server)
		{
			chunkGenParams = chunkLoadParams
		});
	}

	internal bool addChunkColumnRequest(ChunkColumnLoadRequest chunkRequest)
	{
		ChunkColumnLoadRequest orAdd = requestedChunkColumns.elementsByIndex.GetOrAdd(chunkRequest.mapIndex2d, chunkRequest);
		if (orAdd != chunkRequest)
		{
			if (orAdd.untilPass < chunkRequest.untilPass)
			{
				orAdd.untilPass = chunkRequest.untilPass;
			}
			if (orAdd.CurrentIncompletePass < chunkRequest.CurrentIncompletePass)
			{
				orAdd.Chunks = chunkRequest.Chunks;
			}
			if (orAdd.creationTime < chunkRequest.creationTime)
			{
				orAdd.creationTime = chunkRequest.creationTime;
			}
			if (chunkRequest.blockingRequest && !orAdd.blockingRequest)
			{
				orAdd.blockingRequest = true;
			}
		}
		else
		{
			requestedChunkColumns.EnqueueWithoutAddingToIndex(chunkRequest);
		}
		return !orAdd.Disposed;
	}

	internal bool EnsureMinimumWorldgenPassAt(long index2d, int chunkX, int chunkZ, int minPass, long requirorTime)
	{
		server.loadedMapChunks.TryGetValue(index2d, out var value);
		if (value != null && value.CurrentIncompletePass == EnumWorldGenPass.Done)
		{
			return true;
		}
		ChunkColumnLoadRequest orAdd = requestedChunkColumns.elementsByIndex.GetOrAdd(index2d, (long index2d2) => new ChunkColumnLoadRequest(index2d2, chunkX, chunkZ, server.serverConsoleId, -1, server));
		if (orAdd.CurrentIncompletePass_AsInt < minPass)
		{
			if (orAdd.untilPass < minPass)
			{
				if (orAdd.untilPass < 0)
				{
					requestedChunkColumns.EnqueueWithoutAddingToIndex(orAdd);
				}
				orAdd.untilPass = minPass;
				if (orAdd.creationTime < requirorTime)
				{
					orAdd.creationTime = requirorTime;
				}
			}
			return false;
		}
		return true;
	}

	public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ)
	{
		return ((long)chunkY * (long)server.WorldMap.index3dMulZ + chunkZ) * server.WorldMap.index3dMulX + chunkX;
	}

	public long ChunkIndex3D(EntityPos pos)
	{
		return server.WorldMap.ChunkIndex3D(pos);
	}
}
