using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common.Database;

namespace Vintagestory.Server;

internal class ServerSystemSendChunks : ServerSystem
{
	private Packet_ServerChunk[] chunkPackets = new Packet_ServerChunk[2048];

	private int chunksSent;

	private FastList<ServerChunkWithCoord> chunksToSend = new FastList<ServerChunkWithCoord>();

	private FastList<ServerMapChunkWithCoord> mapChunksToSend = new FastList<ServerMapChunkWithCoord>();

	private FastList<long> toRemove = new FastList<long>();

	private Dictionary<long, Packet_ServerChunk> packetsWithEntities = new Dictionary<long, Packet_ServerChunk>();

	private Dictionary<long, Packet_ServerChunk> packetsWithoutEntities = new Dictionary<long, Packet_ServerChunk>();

	public override void Dispose()
	{
		chunkPackets = null;
		chunksToSend = null;
		mapChunksToSend = null;
		toRemove = null;
	}

	public override int GetUpdateInterval()
	{
		if (!server.IsDedicatedServer)
		{
			return 0;
		}
		return MagicNum.ChunkRequestTickTime;
	}

	public ServerSystemSendChunks(ServerMain server)
		: base(server)
	{
		server.clientAwarenessEvents[EnumClientAwarenessEvent.ChunkTransition].Add(OnClientLeaveChunk);
	}

	public override void OnServerTick(float dt)
	{
		if (server.RunPhase != EnumServerRunPhase.RunGame)
		{
			return;
		}
		packetsWithEntities.Clear();
		packetsWithoutEntities.Clear();
		IPlayer[] allOnlinePlayers = server.AllOnlinePlayers;
		foreach (IMiniDimension value in server.LoadedMiniDimensions.Values)
		{
			value.CollectChunksForSending(allOnlinePlayers);
		}
		foreach (ConnectedClient value2 in server.Clients.Values)
		{
			if (value2.State == EnumClientState.Connected || value2.State == EnumClientState.Playing)
			{
				sendAndEnqueueChunks(value2);
			}
		}
		packetsWithEntities.Clear();
		packetsWithoutEntities.Clear();
	}

	private void OnClientLeaveChunk(ClientStatistics clientstats)
	{
		clientstats.client.CurrentChunkSentRadius = 0;
	}

	private void sendAndEnqueueChunks(ConnectedClient client)
	{
		int val = (int)Math.Ceiling((float)client.WorldData.Viewdistance / (float)MagicNum.ServerChunkSize);
		int num = Math.Min(server.Config.MaxChunkRadius, val);
		if (client.CurrentChunkSentRadius > num && client.forceSendChunks.Count == 0 && client.forceSendMapChunks.Count == 0)
		{
			return;
		}
		chunksToSend.Clear();
		mapChunksToSend.Clear();
		toRemove.Clear();
		int num2 = MagicNum.ChunksToSendPerTick * ((!client.IsLocalConnection) ? 1 : 8);
		List<long> list = new List<long>(1);
		foreach (long forceSendMapChunk in client.forceSendMapChunks)
		{
			Vec2i vec2i = server.WorldMap.MapChunkPosFromChunkIndex2D(forceSendMapChunk);
			server.loadedMapChunks.TryGetValue(forceSendMapChunk, out var value);
			if (value != null)
			{
				server.SendPacketFast(client.Id, value.ToPacket(vec2i.X, vec2i.Y));
			}
			else
			{
				list.Add(forceSendMapChunk);
			}
		}
		client.forceSendMapChunks.Clear();
		foreach (long item2 in list)
		{
			client.forceSendMapChunks.Add(item2);
		}
		foreach (long forceSendChunk in client.forceSendChunks)
		{
			ServerChunk loadedChunk = server.GetLoadedChunk(forceSendChunk);
			if (loadedChunk != null)
			{
				if (num2 <= 0)
				{
					break;
				}
				ChunkPos pos = server.WorldMap.ChunkPosFromChunkIndex3D(forceSendChunk);
				chunksToSend.Add(new ServerChunkWithCoord
				{
					chunk = loadedChunk,
					pos = pos,
					withEntities = true
				});
				num2--;
				toRemove.Add(forceSendChunk);
			}
		}
		foreach (long item3 in toRemove)
		{
			client.forceSendChunks.Remove(item3);
		}
		if (num2 > 0 && server.SendChunks && client.CurrentChunkSentRadius < num && loadSendableChunksAtCurrentRadius(client, num2, client.Player.Entity.Pos.Dimension) == 0 && ++client.CurrentChunkSentRadius <= num && loadSendableChunksAtCurrentRadius(client, num2, client.Player.Entity.Pos.Dimension) == 0)
		{
			client.CurrentChunkSentRadius++;
		}
		foreach (ServerMapChunkWithCoord item4 in mapChunksToSend)
		{
			int chunkX = item4.chunkX;
			int chunkZ = item4.chunkZ;
			int num3 = chunkX / MagicNum.ChunkRegionSizeInChunks;
			int num4 = chunkZ / MagicNum.ChunkRegionSizeInChunks;
			if (!client.DidSendMapRegion(server.WorldMap.MapRegionIndex2D(num3, num4)))
			{
				server.SendPacketFast(client.Id, item4.mapchunk.MapRegion.ToPacket(num3, num4));
				client.SetMapRegionSent(server.WorldMap.MapRegionIndex2D(num3, num4));
			}
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					int regionX = num3 + i;
					int regionZ = num4 + j;
					long num5 = server.WorldMap.MapRegionIndex2D(regionX, regionZ);
					if (!client.DidSendMapRegion(num5) && server.loadedMapRegions.TryGetValue(num5, out var value2))
					{
						server.SendPacketFast(client.Id, value2.ToPacket(regionX, regionZ));
						client.SetMapRegionSent(num5);
					}
				}
			}
			server.SendPacketFast(client.Id, item4.mapchunk.ToPacket(chunkX, chunkZ));
			client.SetMapChunkSent(server.WorldMap.MapChunkIndex2D(chunkX, chunkZ));
		}
		int num6 = 0;
		foreach (ServerChunkWithCoord item5 in chunksToSend)
		{
			chunkPackets[num6++] = collectChunk(item5.chunk, item5.pos.X, item5.pos.Y + item5.pos.Dimension * 1024, item5.pos.Z, client, item5.withEntities);
			if (num6 >= 2048)
			{
				Packet_ServerChunks packet_ServerChunks = new Packet_ServerChunks();
				packet_ServerChunks.SetChunks(chunkPackets, num6, num6);
				server.SendPacketFast(client.Id, new Packet_Server
				{
					Id = 10,
					Chunks = packet_ServerChunks
				});
				num6 = 0;
			}
		}
		if (num6 > 0)
		{
			Packet_ServerChunks packet_ServerChunks2 = new Packet_ServerChunks();
			packet_ServerChunks2.SetChunks(chunkPackets, num6, num6);
			server.SendPacketFast(client.Id, new Packet_Server
			{
				Id = 10,
				Chunks = packet_ServerChunks2
			});
		}
	}

	private int loadSendableChunksAtCurrentRadius(ConnectedClient client, int countChunks, int dimension)
	{
		int num = MagicNum.ChunksColumnsToRequestPerTick * ((!client.IsLocalConnection) ? 1 : 4);
		Vec2i[] octagonPoints = ShapeUtil.GetOctagonPoints((int)client.Position.X / MagicNum.ServerChunkSize, (int)client.Position.Z / MagicNum.ServerChunkSize, client.CurrentChunkSentRadius);
		int num2 = 0;
		int num3 = dimension * 1024;
		for (int i = 0; i < octagonPoints.Length; i++)
		{
			int x = octagonPoints[i].X;
			int y = octagonPoints[i].Y;
			bool flag = false;
			long num4 = server.WorldMap.MapChunkIndex2D(x, y);
			if (!server.WorldMap.IsValidChunkPos(x, num3, y))
			{
				continue;
			}
			for (int j = 0; j < server.WorldMap.ChunkMapSizeY; j++)
			{
				long num5 = server.WorldMap.ChunkIndex3D(x, j + num3, y);
				if (client.DidSendChunk(num5) || toRemove.Contains(num5))
				{
					continue;
				}
				ServerChunk loadedChunk = server.GetLoadedChunk(num5);
				if (loadedChunk != null)
				{
					if (countChunks > 0)
					{
						chunksToSend.Add(new ServerChunkWithCoord
						{
							chunk = loadedChunk,
							pos = new ChunkPos(x, j, y, dimension)
						});
						countChunks--;
						if (!flag)
						{
							if (!client.DidSendMapChunk(num4))
							{
								mapChunksToSend.Add(new ServerMapChunkWithCoord
								{
									chunkX = x,
									chunkZ = y,
									mapchunk = (loadedChunk.MapChunk as ServerMapChunk),
									index2d = num4
								});
							}
							flag = true;
						}
					}
					num2++;
				}
				else
				{
					if (num <= 0)
					{
						continue;
					}
					if (!server.ChunkColumnRequested.ContainsKey(num4) && server.AutoGenerateChunks)
					{
						server.ChunkColumnRequested[num4] = 1;
						lock (server.requestedChunkColumnsLock)
						{
							server.requestedChunkColumns.Enqueue(num4);
						}
						num--;
					}
					num2++;
				}
			}
		}
		return num2;
	}

	private Packet_ServerChunk collectChunk(ServerChunk serverChunk, int chunkX, int chunkY, int chunkZ, ConnectedClient client, bool withEntities)
	{
		long num = server.WorldMap.ChunkIndex3D(chunkX, chunkY, chunkZ);
		client.SetChunkSent(num);
		chunksSent++;
		Dictionary<long, Packet_ServerChunk> dictionary = (withEntities ? packetsWithEntities : packetsWithoutEntities);
		if (dictionary.TryGetValue(num, out var value))
		{
			return value;
		}
		return dictionary[num] = serverChunk.ToPacket(chunkX, chunkY, chunkZ, withEntities);
	}

	public static string performanceTest(ServerMain server)
	{
		Stopwatch stopwatch = null;
		int num = 5;
		int value = 1023;
		int num2 = 15650;
		int num3 = 15640;
		int num4 = 3;
		Process currentProcess = Process.GetCurrentProcess();
		if (RuntimeEnv.OS == OS.Mac)
		{
			ServerMain.Logger.Warning("Cannot set a processor to run the performance test on Mac, performance test may not show max capable");
		}
		else
		{
			value = ((IntPtr)currentProcess.ProcessorAffinity).ToInt32();
			currentProcess.ProcessorAffinity = new IntPtr(2);
		}
		currentProcess.PriorityClass = ProcessPriorityClass.High;
		Thread.CurrentThread.Priority = ThreadPriority.Highest;
		stopwatch = Stopwatch.StartNew();
		ServerChunk loadedChunk = server.GetLoadedChunk(server.WorldMap.ChunkIndex3D(num2, num4, num3));
		if (loadedChunk != null)
		{
			while (--num >= 0)
			{
				Packet_ServerChunk packet_ServerChunk = loadedChunk.ToPacket(num2, num4, num3, withEntities: true);
				Packet_ServerChunks packet_ServerChunks = new Packet_ServerChunks();
				packet_ServerChunks.SetChunks(new Packet_ServerChunk[1] { packet_ServerChunk }, 1, 1);
				server.Serialize_(new Packet_Server
				{
					Id = 10,
					Chunks = packet_ServerChunks
				});
			}
		}
		stopwatch.Stop();
		if (RuntimeEnv.OS != OS.Mac)
		{
			Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(value);
		}
		Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
		Thread.CurrentThread.Priority = ThreadPriority.Normal;
		return "-ServerPacketSending: " + stopwatch.ElapsedTicks;
	}
}
