using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common.Database;

namespace Vintagestory.Server;

internal class ServerSystemUnloadChunks : ServerSystem
{
	private ChunkServerThread chunkthread;

	private bool unloadingPaused;

	private HashSet<long> mapChunkUnloadCandidates = new HashSet<long>();

	private object mapChunkIndicesLock = new object();

	private List<long> mapChunkIndices = new List<long>(800);

	private object dirtyChunksLock = new object();

	private List<ServerChunkWithCoord> dirtyUnloadedChunks = new List<ServerChunkWithCoord>();

	private List<ServerMapChunkWithCoord> dirtyUnloadedMapChunks = new List<ServerMapChunkWithCoord>();

	private object dirtyMapRegionsLock = new object();

	private List<MapRegionAndPos> dirtyMapRegions = new List<MapRegionAndPos>();

	[ThreadStatic]
	private static FastMemoryStream reusableStream;

	private float accum120s;

	private float accum3s;

	public ServerSystemUnloadChunks(ServerMain server, ChunkServerThread chunkthread)
		: base(server)
	{
		this.chunkthread = chunkthread;
		server.api.ChatCommands.GetOrCreate("chunk").BeginSubCommand("unload").WithDescription("Toggle on / off whether the server(and thus in turn the client) should unload chunks")
			.WithAdditionalInformation("Default setting is on. This should normally be left on.")
			.WithArgs(server.api.ChatCommands.Parsers.Bool("setting"))
			.HandleWith(handleToggleUnload)
			.EndSubCommand();
	}

	public override void OnBeginModsAndConfigReady()
	{
		server.clientAwarenessEvents[EnumClientAwarenessEvent.ChunkTransition].Add(OnPlayerLeaveChunk);
	}

	public override void OnBeginShutdown()
	{
		foreach (KeyValuePair<long, ServerChunk> loadedChunk in server.loadedChunks)
		{
			ChunkPos chunkPos = server.WorldMap.ChunkPosFromChunkIndex3D(loadedChunk.Key);
			if (chunkPos.Dimension <= 0)
			{
				server.api.eventapi.TriggerChunkColumnUnloaded(chunkPos.ToVec3i());
			}
		}
		foreach (KeyValuePair<long, ServerMapRegion> loadedMapRegion in server.loadedMapRegions)
		{
			ChunkPos chunkPos2 = server.WorldMap.MapRegionPosFromIndex2D(loadedMapRegion.Key);
			server.api.eventapi.TriggerMapRegionUnloaded(new Vec2i(chunkPos2.X, chunkPos2.Z), loadedMapRegion.Value);
		}
	}

	private void OnPlayerLeaveChunk(ClientStatistics stats)
	{
		if (!unloadingPaused)
		{
			SendOutOfRangeChunkUnloads(stats.client);
		}
	}

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		if (server.Clients.Count == 1)
		{
			ServerMain.Logger.Notification("Last player disconnected, compacting large object heap...");
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect();
		}
	}

	private TextCommandResult handleToggleUnload(TextCommandCallingArgs args)
	{
		unloadingPaused = !(bool)args[0];
		return TextCommandResult.Success("Chunk unloading now " + (unloadingPaused ? "off" : "on"));
	}

	public override int GetUpdateInterval()
	{
		return 200;
	}

	public override void OnServerTick(float dt)
	{
		if (unloadingPaused)
		{
			return;
		}
		ServerMain.FrameProfiler.Enter("unloadchunks-all");
		int count = server.unloadedChunks.Count;
		SendUnloadedChunkUnloads();
		ServerMain.FrameProfiler.Mark("notified-clients:", count);
		accum3s += dt;
		if (accum3s >= 3f)
		{
			accum3s = 0f;
			FindUnloadableChunkColumnCandidates();
			ServerMain.FrameProfiler.Mark("find-chunkcolumns");
			if (mapChunkUnloadCandidates.Count > 0)
			{
				UnloadChunkColumns();
				if (server.Clients.IsEmpty)
				{
					server.serverChunkDataPool.FreeAll();
					GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
					GC.Collect();
					ServerMain.FrameProfiler.Mark("garbagecollector (no clients online)");
				}
			}
		}
		accum120s += dt;
		if (accum120s > 120f)
		{
			accum120s = 0f;
			FindUnusedMapRegions();
			ServerMain.FrameProfiler.Mark("find-mapregions");
		}
		ServerMain.FrameProfiler.Leave();
	}

	private void FindUnusedMapRegions()
	{
		List<long> list = new List<long>();
		List<MapRegionAndPos> list2 = null;
		foreach (KeyValuePair<long, ServerMapRegion> loadedMapRegion in server.loadedMapRegions)
		{
			if (server.ElapsedMilliseconds - loadedMapRegion.Value.loadedTotalMs < 120000)
			{
				continue;
			}
			ChunkPos chunkPos = server.WorldMap.MapRegionPosFromIndex2D(loadedMapRegion.Key);
			int num = chunkPos.X * server.WorldMap.RegionSize;
			int num2 = chunkPos.Z * server.WorldMap.RegionSize;
			int chunkx = num / 32;
			int chunkz = num2 / 32;
			if (server.WorldMap.AnyLoadedChunkInMapRegion(chunkx, chunkz))
			{
				continue;
			}
			list.Add(loadedMapRegion.Key);
			server.api.eventapi.TriggerMapRegionUnloaded(new Vec2i(chunkPos.X, chunkPos.Z), loadedMapRegion.Value);
			if (loadedMapRegion.Value.DirtyForSaving)
			{
				if (list2 == null)
				{
					list2 = new List<MapRegionAndPos>();
				}
				list2.Add(new MapRegionAndPos(chunkPos.ToVec3i(), loadedMapRegion.Value));
			}
		}
		if (list2 != null)
		{
			lock (dirtyMapRegionsLock)
			{
				foreach (MapRegionAndPos item in list2)
				{
					dirtyMapRegions.Add(item);
					item.region.DirtyForSaving = false;
				}
			}
		}
		foreach (long item2 in list)
		{
			server.loadedMapRegions.Remove(item2);
			server.BroadcastUnloadMapRegion(item2);
		}
	}

	public override void OnSeparateThreadTick()
	{
		if (server.RunPhase != EnumServerRunPhase.Shutdown && !unloadingPaused)
		{
			lock (mapChunkIndicesLock)
			{
				mapChunkIndices.Clear();
				mapChunkIndices.AddRange(server.loadedMapChunks.Keys);
			}
			FastMemoryStream ms = reusableStream ?? (reusableStream = new FastMemoryStream());
			SaveDirtyUnloadedChunks(ms);
			SaveDirtyMapRegions(ms);
			UnloadGeneratingChunkColumns(MagicNum.UncompressedChunkTTL);
		}
	}

	private void SaveDirtyMapRegions(FastMemoryStream ms)
	{
		if (dirtyMapRegions.Count <= 0)
		{
			return;
		}
		List<MapRegionAndPos> list = new List<MapRegionAndPos>();
		lock (dirtyMapRegionsLock)
		{
			list.AddRange(dirtyMapRegions);
			dirtyMapRegions.Clear();
		}
		List<DbChunk> list2 = new List<DbChunk>();
		foreach (MapRegionAndPos item in list)
		{
			list2.Add(new DbChunk(new ChunkPos(item.pos), item.region.ToBytes(ms)));
		}
		chunkthread.gameDatabase.SetMapRegions(list2);
	}

	private void SaveDirtyUnloadedChunks(FastMemoryStream ms)
	{
		server.readyToAutoSave = false;
		List<ServerChunkWithCoord> list = new List<ServerChunkWithCoord>();
		List<ServerMapChunkWithCoord> list2 = new List<ServerMapChunkWithCoord>();
		lock (dirtyChunksLock)
		{
			list.AddRange(dirtyUnloadedChunks);
			list2.AddRange(dirtyUnloadedMapChunks);
			dirtyUnloadedChunks.Clear();
			dirtyUnloadedMapChunks.Clear();
		}
		List<DbChunk> list3 = new List<DbChunk>();
		List<DbChunk> list4 = new List<DbChunk>();
		foreach (ServerChunkWithCoord item in list)
		{
			list3.Add(new DbChunk
			{
				Position = item.pos,
				Data = item.chunk.ToBytes(ms)
			});
			item.chunk.Dispose();
		}
		foreach (ServerMapChunkWithCoord item2 in list2)
		{
			list4.Add(new DbChunk
			{
				Position = new ChunkPos
				{
					X = item2.chunkX,
					Y = 0,
					Z = item2.chunkZ
				},
				Data = item2.mapchunk.ToBytes(ms)
			});
		}
		if (list3.Count > 0)
		{
			chunkthread.gameDatabase.SetChunks(list3);
		}
		if (list4.Count > 0)
		{
			chunkthread.gameDatabase.SetMapChunks(list4);
		}
		server.readyToAutoSave = true;
	}

	private void UnloadChunkColumns()
	{
		List<ServerChunkWithCoord> list = new List<ServerChunkWithCoord>();
		List<ServerMapChunkWithCoord> list2 = new List<ServerMapChunkWithCoord>();
		int num = 0;
		foreach (long mapChunkUnloadCandidate in mapChunkUnloadCandidates)
		{
			if (server.forceLoadedChunkColumns.Contains(mapChunkUnloadCandidate))
			{
				continue;
			}
			ChunkPos ret = server.WorldMap.ChunkPosFromChunkIndex2D(mapChunkUnloadCandidate);
			ServerSystemSupplyChunks.UpdateLoadedNeighboursFlags(server.WorldMap, ret.X, ret.Z);
			server.api.eventapi.TriggerChunkColumnUnloaded(ret.ToVec3i());
			for (int i = 0; i < server.WorldMap.ChunkMapSizeY; i++)
			{
				ret.Y = i;
				long num2 = server.WorldMap.ChunkIndex3D(ret.X, i, ret.Z);
				ServerChunk loadedChunk = server.GetLoadedChunk(num2);
				if (loadedChunk != null && TryUnloadChunk(num2, ret, loadedChunk, list, server))
				{
					num++;
				}
			}
			server.loadedMapChunks.TryGetValue(mapChunkUnloadCandidate, out var value);
			if (value != null)
			{
				if (value.DirtyForSaving)
				{
					list2.Add(new ServerMapChunkWithCoord
					{
						chunkX = ret.X,
						chunkZ = ret.Z,
						index2d = mapChunkUnloadCandidate,
						mapchunk = value
					});
				}
				value.DirtyForSaving = false;
				server.loadedMapChunks.Remove(mapChunkUnloadCandidate);
			}
		}
		lock (dirtyChunksLock)
		{
			dirtyUnloadedChunks.AddRange(list);
			dirtyUnloadedMapChunks.AddRange(list2);
		}
		ServerMain.FrameProfiler.Mark("unloaded-chunkcolumns:", mapChunkUnloadCandidates.Count);
		mapChunkUnloadCandidates.Clear();
	}

	public static bool TryUnloadChunk(long posIndex3d, ChunkPos ret, ServerChunk chunk, List<ServerChunkWithCoord> dirtyChunksTmp, ServerMain server)
	{
		bool flag = false;
		if (chunk.DirtyForSaving)
		{
			flag = true;
			dirtyChunksTmp.Add(new ServerChunkWithCoord
			{
				pos = ret,
				chunk = chunk
			});
		}
		chunk.DirtyForSaving = false;
		server.unloadedChunks.Enqueue(posIndex3d);
		long key = server.WorldMap.ChunkIndex3dToIndex2d(posIndex3d);
		server.loadedChunksLock.AcquireWriteLock();
		try
		{
			if (server.loadedChunks.Remove(posIndex3d))
			{
				server.ChunkColumnRequested.Remove(key);
			}
		}
		finally
		{
			server.loadedChunksLock.ReleaseWriteLock();
		}
		chunk.RemoveEntitiesAndBlockEntities(server);
		if (!flag)
		{
			chunk.Dispose();
		}
		return flag;
	}

	internal void UnloadGeneratingChunkColumns(long timeToLive)
	{
		List<ChunkColumnLoadRequest> list = new List<ChunkColumnLoadRequest>();
		int num = 0;
		foreach (ChunkColumnLoadRequest item in chunkthread.requestedChunkColumns.Snapshot())
		{
			if (item.Chunks == null || item.Disposed)
			{
				continue;
			}
			EnumWorldGenPass currentIncompletePass = item.CurrentIncompletePass;
			if (currentIncompletePass < item.GenerateUntilPass || currentIncompletePass == EnumWorldGenPass.Done)
			{
				continue;
			}
			bool flag = true;
			if (server.forceLoadedChunkColumns.Contains(item.mapIndex2d))
			{
				continue;
			}
			for (int i = 0; i < item.Chunks.Length; i++)
			{
				if (Environment.TickCount - item.Chunks[i].lastReadOrWrite < timeToLive)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		List<DbChunk> list2 = new List<DbChunk>();
		List<DbChunk> list3 = new List<DbChunk>();
		using FastMemoryStream ms = new FastMemoryStream();
		foreach (ChunkColumnLoadRequest item2 in list)
		{
			item2.generatingLock.AcquireReadLock();
			try
			{
				for (int j = 0; j < item2.Chunks.Length; j++)
				{
					if (item2.Chunks[j].DirtyForSaving)
					{
						item2.Chunks[j].DirtyForSaving = false;
						list2.Add(new DbChunk
						{
							Position = new ChunkPos(item2.chunkX, j, item2.chunkZ, 0),
							Data = item2.Chunks[j].ToBytes(ms)
						});
					}
				}
				ServerMapChunk mapChunk = item2.MapChunk;
				if (mapChunk != null)
				{
					if (mapChunk.DirtyForSaving)
					{
						list3.Add(new DbChunk
						{
							Position = new ChunkPos(item2.chunkX, 0, item2.chunkZ, 0),
							Data = mapChunk.ToBytes(ms)
						});
					}
					mapChunk.DirtyForSaving = false;
					server.loadedMapChunks.Remove(item2.mapIndex2d);
				}
			}
			finally
			{
				item2.generatingLock.ReleaseReadLock();
			}
			if (!chunkthread.requestedChunkColumns.Remove(item2.mapIndex2d))
			{
				throw new Exception("Chunkrequest no longer in queue? Race condition?");
			}
			server.ChunkColumnRequested.Remove(item2.mapIndex2d);
			num++;
		}
		if (list2.Count > 0)
		{
			chunkthread.gameDatabase.SetChunks(list2);
		}
		if (list3.Count > 0)
		{
			chunkthread.gameDatabase.SetMapChunks(list3);
		}
	}

	private void FindUnloadableChunkColumnCandidates()
	{
		List<long> list = new List<long>();
		foreach (ConnectedClient value2 in server.Clients.Values)
		{
			int allowedChunkRadius = server.GetAllowedChunkRadius(value2);
			int x = ((value2.Position == null) ? (server.WorldMap.MapSizeX / 2) : ((int)value2.Position.X)) / MagicNum.ServerChunkSize;
			int y = ((value2.Position == null) ? (server.WorldMap.MapSizeZ / 2) : ((int)value2.Position.Z)) / MagicNum.ServerChunkSize;
			for (int i = 0; i <= allowedChunkRadius; i++)
			{
				ShapeUtil.LoadOctagonIndices(list, x, y, i, server.WorldMap.ChunkMapSizeX);
			}
		}
		Vec2i vec2i = new Vec2i();
		ServerMapChunk value;
		foreach (long item in list)
		{
			MapUtil.PosInt2d(item, server.WorldMap.ChunkMapSizeX, vec2i);
			long key = server.WorldMap.MapChunkIndex2D(vec2i.X, vec2i.Y);
			server.loadedMapChunks.TryGetValue(key, out value);
			value?.MarkFresh();
		}
		lock (mapChunkIndicesLock)
		{
			foreach (long mapChunkIndex in mapChunkIndices)
			{
				if (!server.forceLoadedChunkColumns.Contains(mapChunkIndex) && server.loadedMapChunks.TryGetValue(mapChunkIndex, out value) && value.CurrentIncompletePass == EnumWorldGenPass.Done)
				{
					if (value.IsOld())
					{
						mapChunkUnloadCandidates.Add(mapChunkIndex);
					}
					else
					{
						value.DoAge();
					}
				}
			}
		}
	}

	private void SendUnloadedChunkUnloads()
	{
		if (server.unloadedChunks.IsEmpty)
		{
			return;
		}
		List<long> list = new List<long>();
		list.AddRange(server.unloadedChunks);
		server.unloadedChunks = new ConcurrentQueue<long>();
		List<Vec3i> list2 = new List<Vec3i>();
		foreach (ConnectedClient value in server.Clients.Values)
		{
			list2.Clear();
			foreach (long item in list)
			{
				if (value.ChunkSent.Contains(item))
				{
					int x = (int)(item % server.WorldMap.index3dMulX);
					int y = (int)(item / server.WorldMap.index3dMulX / server.WorldMap.index3dMulZ);
					int z = (int)(item / server.WorldMap.index3dMulX % server.WorldMap.index3dMulZ);
					list2.Add(new Vec3i(x, y, z));
					value.RemoveChunkSent(item);
					long index2d = server.WorldMap.ChunkIndex3dToIndex2d(item);
					value.RemoveMapChunkSent(index2d);
				}
			}
			if (list2.Count > 0)
			{
				int[] array = new int[list2.Count];
				int[] array2 = new int[list2.Count];
				int[] array3 = new int[list2.Count];
				for (int i = 0; i < array.Length; i++)
				{
					Vec3i vec3i = list2[i];
					array[i] = vec3i.X;
					array2[i] = vec3i.Y;
					array3[i] = vec3i.Z;
				}
				Packet_UnloadServerChunk packet_UnloadServerChunk = new Packet_UnloadServerChunk();
				packet_UnloadServerChunk.SetX(array);
				packet_UnloadServerChunk.SetY(array2);
				packet_UnloadServerChunk.SetZ(array3);
				Packet_Server packet = new Packet_Server
				{
					Id = 11,
					UnloadChunk = packet_UnloadServerChunk
				};
				server.SendPacket(value.Id, packet);
			}
		}
	}

	private void SendOutOfRangeChunkUnloads(ConnectedClient client)
	{
		List<long> list = new List<long>();
		HashSet<long> hashSet = new HashSet<long>();
		int allowedChunkRadius = server.GetAllowedChunkRadius(client);
		int x = ((client.Position == null) ? (server.WorldMap.MapSizeX / 2) : ((int)client.Position.X)) / MagicNum.ServerChunkSize;
		int y = ((client.Position == null) ? (server.WorldMap.MapSizeZ / 2) : ((int)client.Position.Z)) / MagicNum.ServerChunkSize;
		int chunkMapSizeX = server.WorldMap.ChunkMapSizeX;
		for (int i = 0; i <= allowedChunkRadius; i++)
		{
			ShapeUtil.LoadOctagonIndices(hashSet, x, y, i, chunkMapSizeX);
		}
		foreach (long item in client.ChunkSent)
		{
			if ((int)(item / ((long)server.WorldMap.index3dMulX * (long)server.WorldMap.index3dMulZ)) < 128)
			{
				long num = server.WorldMap.ChunkIndex3dToIndex2d(item);
				if (!hashSet.Contains(num))
				{
					list.Add(item);
					client.RemoveMapChunkSent(num);
				}
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		int[] array = new int[list.Count];
		int[] array2 = new int[list.Count];
		int[] array3 = new int[list.Count];
		for (int j = 0; j < array.Length; j++)
		{
			long num2 = list[j];
			client.RemoveChunkSent(num2);
			ServerChunk serverChunk = server.WorldMap.GetServerChunk(num2);
			if (serverChunk != null)
			{
				int entitiesCount = serverChunk.EntitiesCount;
				for (int k = 0; k < entitiesCount; k++)
				{
					client.TrackedEntities.Remove(serverChunk.Entities[k].EntityId);
				}
			}
			array[j] = (int)(num2 % server.WorldMap.index3dMulX);
			array2[j] = (int)(num2 / server.WorldMap.index3dMulX / server.WorldMap.index3dMulZ);
			array3[j] = (int)(num2 / server.WorldMap.index3dMulX % server.WorldMap.index3dMulZ);
		}
		Packet_UnloadServerChunk packet_UnloadServerChunk = new Packet_UnloadServerChunk();
		packet_UnloadServerChunk.SetX(array);
		packet_UnloadServerChunk.SetY(array2);
		packet_UnloadServerChunk.SetZ(array3);
		Packet_Server packet = new Packet_Server
		{
			Id = 11,
			UnloadChunk = packet_UnloadServerChunk
		};
		server.SendPacket(client.Id, packet);
	}
}
