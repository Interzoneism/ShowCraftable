using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common.Database;

namespace Vintagestory.Server;

internal class ServerSystemCompressChunks : ServerSystem
{
	private long chunkCompressScanTimer;

	private object compactableChunksLock = new object();

	private Queue<long> compactableChunks = new Queue<long>();

	private object compactedChunksLock = new object();

	private Queue<long> compactedChunks = new Queue<long>();

	private object clientIdsLock = new object();

	private List<int> clientIds = new List<int>();

	public ServerSystemCompressChunks(ServerMain server)
		: base(server)
	{
	}

	public override void OnPlayerJoin(ServerPlayer player)
	{
		lock (clientIdsLock)
		{
			clientIds.Add(player.ClientId);
		}
	}

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		lock (clientIdsLock)
		{
			clientIds.Remove(player.ClientId);
		}
	}

	public override int GetUpdateInterval()
	{
		return 10;
	}

	public override void OnServerTick(float dt)
	{
		if (compactableChunks.Count <= 0)
		{
			FreeMemory();
			long elapsedMilliseconds = server.totalUnpausedTime.ElapsedMilliseconds;
			if (elapsedMilliseconds - chunkCompressScanTimer >= 4000)
			{
				chunkCompressScanTimer = elapsedMilliseconds;
				FindFreeableMemory();
			}
		}
	}

	private void FindFreeableMemory()
	{
		List<BlockPos> list = new List<BlockPos>();
		lock (clientIdsLock)
		{
			foreach (int clientId in clientIds)
			{
				if (server.Clients.TryGetValue(clientId, out var value) && value.State == EnumClientState.Playing)
				{
					list.Add(value.ChunkPos);
				}
			}
		}
		int num = 0;
		lock (compactableChunksLock)
		{
			server.loadedChunksLock.AcquireReadLock();
			try
			{
				foreach (KeyValuePair<long, ServerChunk> loadedChunk in server.loadedChunks)
				{
					if (loadedChunk.Value.IsPacked())
					{
						num++;
					}
					else
					{
						if (Environment.TickCount - loadedChunk.Value.lastReadOrWrite <= MagicNum.UncompressedChunkTTL)
						{
							continue;
						}
						bool flag = false;
						ChunkPos chunkPos = server.WorldMap.ChunkPosFromChunkIndex3D(loadedChunk.Key);
						if (!loadedChunk.Value.Empty && chunkPos.Dimension == 0)
						{
							foreach (BlockPos item in list)
							{
								if (Math.Abs(item.X - chunkPos.X) < 2 || Math.Abs(item.Z - chunkPos.Z) < 2)
								{
									flag = true;
									break;
								}
							}
						}
						if (!flag)
						{
							compactableChunks.Enqueue(loadedChunk.Key);
						}
					}
				}
			}
			finally
			{
				server.loadedChunksLock.ReleaseReadLock();
			}
		}
	}

	private void FreeMemory()
	{
		while (compactedChunks.Count > 0)
		{
			ServerChunk serverChunk = null;
			long index3d = 0L;
			lock (compactedChunksLock)
			{
				index3d = compactedChunks.Dequeue();
			}
			server.GetLoadedChunk(index3d)?.TryCommitPackAndFree(MagicNum.UncompressedChunkTTL);
		}
	}

	public override void OnSeparateThreadTick()
	{
		long num = 0L;
		lock (compactableChunksLock)
		{
			if (compactableChunks.Count > 0)
			{
				num = compactableChunks.Dequeue();
			}
		}
		if (num == 0L)
		{
			return;
		}
		ServerChunk loadedChunk = server.GetLoadedChunk(num);
		if (loadedChunk == null)
		{
			return;
		}
		loadedChunk.Pack();
		lock (compactedChunksLock)
		{
			compactedChunks.Enqueue(num);
		}
	}
}
