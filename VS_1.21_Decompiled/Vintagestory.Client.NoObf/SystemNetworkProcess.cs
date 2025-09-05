using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;
using Vintagestory.Server.Systems;

namespace Vintagestory.Client.NoObf;

public class SystemNetworkProcess : ClientSystem
{
	private Stack<Packet_ServerChunk> cheapFixChunkQueue = new Stack<Packet_ServerChunk>();

	private int totalByteCount;

	private int deltaByteCount;

	private int totalUdpByteCount;

	private int deltaUdpByteCount;

	private readonly bool packetDebug;

	private bool doBenchmark;

	private readonly SortedDictionary<int, int> packetBenchmark = new SortedDictionary<int, int>();

	private readonly SortedDictionary<int, int> udpPacketBenchmark = new SortedDictionary<int, int>();

	private bool commitingMinimalUpdate;

	private readonly IClientNetworkChannel clientNetworkChannel;

	public static readonly Dictionary<int, string> ServerPacketNames;

	public bool DidReceiveUdp;

	private bool bulkPositions;

	public override string Name => "nwp";

	public int TotalBytesReceivedAndReceiving => totalByteCount + game.MainNetClient.CurrentlyReceivingBytes;

	static SystemNetworkProcess()
	{
		ServerPacketNames = new Dictionary<int, string>();
		FieldInfo[] fields = typeof(Packet_ServerIdEnum).GetFields();
		for (int i = 0; i < fields.Length; i++)
		{
			if ((fields[i].Attributes & FieldAttributes.Literal) != FieldAttributes.PrivateScope && !(fields[i].FieldType != typeof(int)))
			{
				ServerPacketNames[(int)fields[i].GetValue(null)] = fields[i].Name;
			}
		}
	}

	public SystemNetworkProcess(ClientMain game)
		: base(game)
	{
		totalByteCount = 0;
		game.RegisterGameTickListener(UpdatePacketCount, 1000);
		game.RegisterGameTickListener(ClientUdpTick, 15);
		game.PacketHandlers[78] = HandleRequestPositionTcp;
		game.PacketHandlers[79] = EnqueueUdpPacket;
		game.PacketHandlers[80] = HandleEntitySpawnPosition;
		game.PacketHandlers[81] = HandleDidReceiveUdp;
		game.api.ChatCommands.Create("netbenchmark").WithDescription("Toggles network benchmarking").HandleWith(CmdBenchmark);
		clientNetworkChannel = game.api.Network.RegisterChannel("EntityAnims");
		clientNetworkChannel.RegisterMessageType<AnimationPacket>().RegisterMessageType<BulkAnimationPacket>().RegisterMessageType<EntityTagPacket>()
			.SetMessageHandler<AnimationPacket>(HandleAnimationPacket)
			.SetMessageHandler<BulkAnimationPacket>(HandleBulkAnimationPacket)
			.SetMessageHandler<EntityTagPacket>(HandleTagPacket);
		packetDebug = ClientSettings.Inst.Bool["packetDebug"];
	}

	private void UdpConnectionRequestFromServer()
	{
		if (!DidReceiveUdp)
		{
			game.Logger.Notification("UDP: Server send UDP connect");
			DidReceiveUdp = true;
		}
	}

	private void HandleDidReceiveUdp(Packet_Server packet)
	{
		game.UdpTryConnect = false;
		game.Logger.Notification("UDP: Server send DidReceiveUdp");
		Task.Run(async delegate
		{
			for (int i = 0; i < 20; i++)
			{
				await Task.Delay(500);
				if (game.disposed)
				{
					return;
				}
				if (DidReceiveUdp || game.FallBackToTcp)
				{
					break;
				}
			}
			if (!DidReceiveUdp)
			{
				Packet_Client packetClient = new Packet_Client
				{
					Id = 34
				};
				game.Logger.Notification("UDP: Server did not receive any UDP packets and requests position updates over TCP");
				game.SendPacketClient(packetClient);
			}
			else
			{
				game.Logger.Notification("UDP: Client can receive UDP packets");
			}
		});
	}

	private void HandleEntitySpawnPosition(Packet_Server packet)
	{
		HandleSinglePacket(packet.EntityPosition);
	}

	private void EnqueueUdpPacket(Packet_Server packet)
	{
		game.UdpNetClient.EnqueuePacket(packet.UdpPacket);
	}

	private void HandleRequestPositionTcp(Packet_Server packet)
	{
		game.Logger.Notification("UDP: Server requested to fallback to use only TCP");
		game.FallBackToTcp = true;
		game.UdpTryConnect = false;
	}

	public void StartUdpConnectRequest(string token)
	{
		if (ClientSettings.ForceUdpOverTcp)
		{
			game.Logger.Notification("UDP: is disabled in clientsettings: forceUdpOverTcp , using only TCP now");
			Packet_Client packetClient = new Packet_Client
			{
				Id = 34
			};
			game.SendPacketClient(packetClient);
			return;
		}
		game.UdpTryConnect = true;
		game.UdpNetClient.DidReceiveUdpConnectionRequest += UdpConnectionRequestFromServer;
		Task.Run(async delegate
		{
			Packet_ConnectionPacket connectionPacket = new Packet_ConnectionPacket
			{
				LoginToken = token
			};
			Packet_UdpPacket udpPacket = new Packet_UdpPacket
			{
				Id = 1,
				ConnectionPacket = connectionPacket
			};
			game.Logger.Notification("UDP: sending connection requests");
			while (game.UdpTryConnect && !game.disposed)
			{
				game.UdpNetClient.Send(udpPacket);
				if (game.IsSingleplayer)
				{
					game.UdpTryConnect = false;
					DidReceiveUdp = true;
					break;
				}
				await Task.Delay(500);
			}
		});
		if (game.IsSingleplayer)
		{
			return;
		}
		game.Logger.Notification("UDP: set up 10 s keep alive to server");
		Task.Run(async delegate
		{
			Packet_UdpPacket udpPacket2 = new Packet_UdpPacket
			{
				Id = 7
			};
			while (!game.disposed && !game.FallBackToTcp)
			{
				game.UdpNetClient.Send(udpPacket2);
				await Task.Delay(10000);
			}
		});
	}

	public void HandleTagPacket(EntityTagPacket packet)
	{
		Entity entityById = game.GetEntityById(packet.EntityId);
		if (entityById != null)
		{
			entityById.Tags = new EntityTagArray((ulong)packet.TagsBitmask1, (ulong)packet.TagsBitmask2);
		}
	}

	public void HandleAnimationPacket(AnimationPacket packet)
	{
		Entity entityById = game.GetEntityById(packet.entityId);
		if (entityById != null && entityById.Properties?.Client?.LoadedShapeForEntity?.Animations != null)
		{
			float[] array = new float[packet.activeAnimationSpeedsCount];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = CollectibleNet.DeserializeFloatPrecise(packet.activeAnimationSpeeds[i]);
			}
			entityById.OnReceivedServerAnimations(packet.activeAnimations, packet.activeAnimationsCount, array);
		}
	}

	public void HandleBulkAnimationPacket(BulkAnimationPacket bulkPacket)
	{
		if (bulkPacket.Packets == null)
		{
			return;
		}
		for (int i = 0; i < bulkPacket.Packets.Length; i++)
		{
			AnimationPacket animationPacket = bulkPacket.Packets[i];
			Entity entityById = game.GetEntityById(animationPacket.entityId);
			if (entityById != null && entityById.Properties?.Client?.LoadedShapeForEntity?.Animations != null)
			{
				float[] array = new float[animationPacket.activeAnimationSpeedsCount];
				for (int j = 0; j < array.Length; j++)
				{
					array[j] = CollectibleNet.DeserializeFloatPrecise(animationPacket.activeAnimationSpeeds[j]);
				}
				entityById.OnReceivedServerAnimations(animationPacket.activeAnimations, animationPacket.activeAnimationsCount, array);
			}
		}
	}

	private void ClientUdpTick(float obj)
	{
		if (game.UdpNetClient == null)
		{
			return;
		}
		IEnumerable<Packet_UdpPacket> enumerable = game.UdpNetClient.ReadMessage();
		if (enumerable == null)
		{
			return;
		}
		foreach (Packet_UdpPacket packet in enumerable)
		{
			int udpByteCount = packet.Length;
			if (packetDebug)
			{
				ScreenManager.EnqueueMainThreadTask(delegate
				{
					game.Logger.VerboseDebug("Received UDP packet id {0}, dataLength {1}", packet.Id, udpByteCount);
				});
			}
			UpdateUdpStatsAndBenchmark(packet, udpByteCount);
			switch (packet.Id)
			{
			case 4:
				HandleBulkPacket(packet.BulkPositions);
				break;
			case 5:
				HandleSinglePacket(packet.EntityPosition);
				break;
			case 6:
				game.HandleCustomUdpPackets(packet.ChannelPacket);
				break;
			}
		}
	}

	private void UpdateUdpStatsAndBenchmark(Packet_UdpPacket packet, int udpByteCount)
	{
		if (doBenchmark)
		{
			if (udpPacketBenchmark.TryGetValue(packet.Id, out var value))
			{
				udpPacketBenchmark[packet.Id] = value + udpByteCount;
			}
			else
			{
				udpPacketBenchmark[packet.Id] = udpByteCount;
			}
		}
		totalUdpByteCount += udpByteCount;
		deltaUdpByteCount += udpByteCount;
	}

	private TextCommandResult CmdBenchmark(TextCommandCallingArgs textCommandCallingArgs)
	{
		doBenchmark = !doBenchmark;
		if (!doBenchmark)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<int, int> item in packetBenchmark)
			{
				ServerPacketNames.TryGetValue(item.Key, out var value);
				stringBuilder.AppendLine(value + ": " + ((item.Value > 9999) ? (((float)item.Value / 1024f).ToString("#.#") + "kb") : (item.Value + "b")));
			}
			foreach (KeyValuePair<int, int> item2 in udpPacketBenchmark)
			{
				string text = item2.Key.ToString();
				stringBuilder.AppendLine(text + ": " + ((item2.Value > 9999) ? (((float)item2.Value / 1024f).ToString("#.#") + "kb") : (item2.Value + "b")));
			}
			return TextCommandResult.Success(stringBuilder.ToString());
		}
		packetBenchmark.Clear();
		return TextCommandResult.Success("Benchmarking started. Stop it after a while to get results.");
	}

	private void UpdatePacketCount(float dt)
	{
		if (game.extendedDebugInfo)
		{
			string text = ((deltaByteCount > 1024) ? (((float)deltaByteCount / 1024f).ToString("0.0") + "kb/s") : (deltaByteCount + "b/s"));
			string text2 = ((deltaUdpByteCount > 1024) ? (((float)deltaUdpByteCount / 1024f).ToString("0.0") + "kb/s") : (deltaUdpByteCount + "b/s"));
			game.DebugScreenInfo["incomingbytes"] = "Network TCP/UDP: " + ((float)totalByteCount / 1024f).ToString("#.#", GlobalConstants.DefaultCultureInfo) + " kb, " + text + " / " + ((float)totalUdpByteCount / 1024f).ToString("#.#", GlobalConstants.DefaultCultureInfo) + " kb, " + text2;
		}
		else
		{
			game.DebugScreenInfo["incomingbytes"] = "";
		}
		deltaByteCount = 0;
		deltaUdpByteCount = 0;
	}

	public override void OnSeperateThreadGameTick(float dt)
	{
		if (game.MainNetClient == null)
		{
			return;
		}
		while (true)
		{
			NetIncomingMessage netIncomingMessage = game.MainNetClient.ReadMessage();
			if (netIncomingMessage != null)
			{
				totalByteCount += netIncomingMessage.originalMessageLength;
				deltaByteCount += netIncomingMessage.originalMessageLength;
				TryReadPacket(netIncomingMessage.message, netIncomingMessage.messageLength);
				continue;
			}
			break;
		}
	}

	public void TryReadPacket(byte[] data, int dataLength)
	{
		Packet_Server packet = new Packet_Server();
		Packet_ServerSerializer.DeserializeBuffer(data, dataLength, packet);
		if (game.disposed)
		{
			return;
		}
		if (packetDebug)
		{
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				game.Logger.VerboseDebug("Received packet id {0}, dataLength {1}", packet.Id, dataLength);
			});
		}
		if (doBenchmark)
		{
			if (packetBenchmark.TryGetValue(packet.Id, out var value))
			{
				packetBenchmark[packet.Id] = value + data.Length;
			}
			else
			{
				packetBenchmark[packet.Id] = data.Length;
			}
		}
		if (ProcessInBackground(packet))
		{
			return;
		}
		ProcessPacketTask task = new ProcessPacketTask
		{
			game = game,
			packet = packet
		};
		if (packet.Id == 73)
		{
			game.ServerReady = true;
			if (game.IsSingleplayer && game.GameLaunchTasks.Count > 0)
			{
				game.Logger.VerboseDebug("ServerIdentification packet received; will wait until block tesselation is complete to handle it");
			}
		}
		if (false)
		{
			string taskId = "readpacket" + packet.Id;
			game.EnqueueMainThreadTask(delegate
			{
				game.EnqueueMainThreadTask(delegate
				{
					game.EnqueueMainThreadTask(delegate
					{
						game.EnqueueMainThreadTask(delegate
						{
							game.EnqueueMainThreadTask(task.Run, taskId);
						}, taskId);
					}, taskId);
				}, taskId);
			}, taskId);
		}
		else
		{
			game.EnqueueMainThreadTask(task.Run, "readpacket" + packet.Id);
		}
		game.LastReceivedMilliseconds = game.Platform.EllapsedMs;
	}

	public override int SeperateThreadTickIntervalMs()
	{
		return 1;
	}

	private bool ProcessInBackground(Packet_Server packet)
	{
		switch (packet.Id)
		{
		case 4:
			game.WorldMap.ServerChunkSize = packet.LevelInitialize.ServerChunkSize;
			game.WorldMap.MapChunkSize = packet.LevelInitialize.ServerMapChunkSize;
			game.WorldMap.regionSize = packet.LevelInitialize.ServerMapRegionSize;
			game.WorldMap.MaxViewDistance = packet.LevelInitialize.MaxViewDistance;
			return false;
		case 10:
		{
			if (!game.BlocksReceivedAndLoaded)
			{
				for (int num3 = 0; num3 < packet.Chunks.ChunksCount; num3++)
				{
					cheapFixChunkQueue.Push(packet.Chunks.Chunks[num3]);
				}
				return true;
			}
			while (cheapFixChunkQueue.Count > 0)
			{
				Packet_ServerChunk p = cheapFixChunkQueue.Pop();
				game.WorldMap.LoadChunkFromPacket(p);
				RuntimeStats.chunksReceived++;
			}
			for (int num4 = 0; num4 < packet.Chunks.ChunksCount; num4++)
			{
				Packet_ServerChunk p2 = packet.Chunks.Chunks[num4];
				game.WorldMap.LoadChunkFromPacket(p2);
				RuntimeStats.chunksReceived++;
			}
			return true;
		}
		case 19:
			game.PacketHandlers[packet.Id]?.Invoke(packet);
			return true;
		case 21:
			game.EnqueueGameLaunchTask(delegate
			{
				game.PacketHandlers[packet.Id]?.Invoke(packet);
			}, "worldmetadatareceived");
			return true;
		case 17:
		{
			long key = game.WorldMap.MapChunkIndex2D(packet.MapChunk.ChunkX, packet.MapChunk.ChunkZ);
			game.WorldMap.MapChunks.TryGetValue(key, out var value);
			if (value == null)
			{
				value = new ClientMapChunk();
			}
			value.UpdateFromPacket(packet.MapChunk);
			game.WorldMap.MapChunks[key] = value;
			return true;
		}
		case 42:
		{
			long key3 = game.WorldMap.MapRegionIndex2D(packet.MapRegion.RegionX, packet.MapRegion.RegionZ);
			game.WorldMap.MapRegions.TryGetValue(key3, out var region);
			if (region == null)
			{
				region = new ClientMapRegion();
			}
			region.UpdateFromPacket(packet);
			game.WorldMap.MapRegions[key3] = region;
			game.EnqueueMainThreadTask(delegate
			{
				game.api.eventapi.TriggerMapregionLoaded(new Vec2i(packet.MapRegion.RegionX, packet.MapRegion.RegionZ), region);
			}, "mapregionloadedevent");
			return true;
		}
		case 47:
		{
			if (!game.Spawned)
			{
				return true;
			}
			int[] liquidLayer2;
			KeyValuePair<BlockPos[], int[]> pair2 = BlockTypeNet.UnpackSetBlocks(packet.SetBlocks.SetBlocks, out liquidLayer2);
			game.EnqueueMainThreadTask(delegate
			{
				BlockPos[] key4 = pair2.Key;
				int[] value3 = pair2.Value;
				for (int i = 0; i < key4.Length; i++)
				{
					game.WorldMap.BulkBlockAccess.SetBlock(value3[i], key4[i]);
					game.eventManager?.TriggerBlockChanged(game, key4[i], null);
				}
				if (liquidLayer2 != null)
				{
					for (int j = 0; j < key4.Length; j++)
					{
						game.WorldMap.BulkBlockAccess.SetBlock(liquidLayer2[j], key4[j], 2);
					}
					game.WorldMap.BulkBlockAccess.Commit();
				}
			}, "setblocks");
			return true;
		}
		case 63:
		{
			int[] liquidLayer;
			KeyValuePair<BlockPos[], int[]> pair = BlockTypeNet.UnpackSetBlocks(packet.SetBlocks.SetBlocks, out liquidLayer);
			game.EnqueueMainThreadTask(delegate
			{
				BlockPos[] key4 = pair.Key;
				int[] value3 = pair.Value;
				if (game.BlocksReceivedAndLoaded)
				{
					for (int i = 0; i < key4.Length; i++)
					{
						game.WorldMap.NoRelightBulkBlockAccess.SetBlock(value3[i], key4[i]);
						game.eventManager?.TriggerBlockChanged(game, key4[i], null);
					}
				}
				else
				{
					for (int j = 0; j < key4.Length; j++)
					{
						game.WorldMap.NoRelightBulkBlockAccess.SetBlock(value3[j], key4[j]);
					}
				}
				game.WorldMap.NoRelightBulkBlockAccess.Commit();
				if (liquidLayer != null)
				{
					for (int k = 0; k < key4.Length; k++)
					{
						game.WorldMap.NoRelightBulkBlockAccess.SetBlock(liquidLayer[k], key4[k], 2);
					}
					game.WorldMap.NoRelightBulkBlockAccess.Commit();
				}
			}, "setblocksnorelight");
			return true;
		}
		case 70:
		{
			while (commitingMinimalUpdate)
			{
				Thread.Sleep(5);
			}
			int[] liquidsLayer;
			KeyValuePair<BlockPos[], int[]> keyValuePair = BlockTypeNet.UnpackSetBlocks(packet.SetBlocks.SetBlocks, out liquidsLayer);
			BlockPos[] key2 = keyValuePair.Key;
			int[] value2 = keyValuePair.Value;
			for (int num = 0; num < key2.Length; num++)
			{
				BlockPos pos = key2[num];
				if (game.WorldMap.IsPosLoaded(pos))
				{
					game.WorldMap.BulkMinimalBlockAccess.SetBlock(value2[num], pos);
				}
			}
			if (liquidsLayer != null)
			{
				for (int num2 = 0; num2 < key2.Length; num2++)
				{
					game.WorldMap.BulkMinimalBlockAccess.SetBlock(liquidsLayer[num2], key2[num2], 2);
				}
			}
			commitingMinimalUpdate = true;
			game.EnqueueMainThreadTask(delegate
			{
				game.WorldMap.BulkMinimalBlockAccess.Commit();
				commitingMinimalUpdate = false;
			}, "setblocksminimal");
			return true;
		}
		case 71:
		{
			if (!game.Spawned)
			{
				return true;
			}
			long chunkIndex;
			Dictionary<int, Block> newDecors = BlockTypeNet.UnpackSetDecors(packet.SetDecors.SetDecors, game.WorldMap.World, out chunkIndex);
			game.EnqueueMainThreadTask(delegate
			{
				game.WorldMap.BulkBlockAccess.SetDecorsBulk(chunkIndex, newDecors);
			}, "setdecors");
			return true;
		}
		case 74:
			if (!game.Spawned)
			{
				return true;
			}
			game.EnqueueMainThreadTask(delegate
			{
				game.WorldMap.UnloadMapRegion(packet.UnloadMapRegion.RegionX, packet.UnloadMapRegion.RegionZ);
			}, "unloadmapregion");
			return true;
		default:
			return false;
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}

	public void HandleSinglePacket(Packet_EntityPosition packet)
	{
		if (packet == null)
		{
			return;
		}
		Entity entityById = game.GetEntityById(packet.EntityId);
		if (entityById == null)
		{
			return;
		}
		int num = entityById.Attributes.GetInt("tick");
		if (num == 0 && bulkPositions)
		{
			entityById.Attributes.SetInt("tick", packet.Tick);
		}
		else
		{
			if (packet.Tick <= num)
			{
				return;
			}
			entityById.Attributes.SetInt("tickDiff", Math.Min(packet.Tick - num, 5));
			entityById.Attributes.SetInt("tick", packet.Tick);
			entityById.ServerPos.SetFromPacket(packet, entityById);
			if (entityById is EntityAgent entityAgent)
			{
				entityAgent.Controls.FromInt(packet.Controls & 0x210);
				if (entityAgent.EntityId != game.EntityPlayer.EntityId)
				{
					entityAgent.ServerControls.FromInt(packet.Controls);
				}
			}
			((entityById.SidedProperties == null) ? null : entityById.GetInterface<IMountable>()?.ControllingControls)?.FromInt(packet.MountControls);
			entityById.OnReceivedServerPos(packet.Teleport);
			entityById.Tags = new EntityTagArray((ulong)packet.TagsBitmask1, (ulong)packet.TagsBitmask2);
		}
	}

	public void HandleBulkPacket(Packet_BulkEntityPosition bulkPacket)
	{
		if (bulkPacket.EntityPositions != null)
		{
			bulkPositions = true;
			Packet_EntityPosition[] entityPositions = bulkPacket.EntityPositions;
			foreach (Packet_EntityPosition packet in entityPositions)
			{
				HandleSinglePacket(packet);
			}
			bulkPositions = false;
		}
	}
}
