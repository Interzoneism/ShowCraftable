using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;

namespace Vintagestory.Server.Systems;

public class ServerUdpNetwork : ServerSystem
{
	public readonly Dictionary<string, ConnectedClient> connectingClients = new Dictionary<string, ConnectedClient>();

	public PhysicsManager physicsManager;

	internal ServerUdpQueue ImmediateUdpQueue;

	public ServerUdpNetwork(ServerMain server)
		: base(server)
	{
		server.RegisterGameTickListener(ServerTickUdp, 15);
		physicsManager = new PhysicsManager(server.api, this);
		server.PacketHandlers[35] = EnqueueUdpPacket;
	}

	private void EnqueueUdpPacket(Packet_Client packet, ConnectedClient player)
	{
		UdpPacket udpPacket = new UdpPacket
		{
			Packet = packet.UdpPacket,
			Client = player
		};
		server.UdpSockets[1].EnqueuePacket(udpPacket);
	}

	private void ServerTickUdp(float obj)
	{
		UNetServer[] udpSockets = server.UdpSockets;
		foreach (UNetServer uNetServer in udpSockets)
		{
			UdpPacket[] array = uNetServer?.ReadMessage();
			if (array == null)
			{
				continue;
			}
			UdpPacket[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				UdpPacket udpPacket = array2[j];
				server.TotalReceivedBytesUdp += udpPacket.Packet.Length;
				switch (udpPacket.Packet.Id)
				{
				case 1:
					HandleConnectionRequest(udpPacket, uNetServer);
					break;
				case 2:
					HandlePlayerPosition(udpPacket.Packet.EntityPosition, udpPacket.Client.Player);
					break;
				case 3:
					HandleMountPosition(udpPacket.Packet.EntityPosition, udpPacket.Client.Player);
					break;
				case 6:
					server.HandleCustomUdpPackets(udpPacket.Packet.ChannelPacket, udpPacket.Client.Player);
					break;
				case 7:
					udpPacket.Client.Ping.OnReceiveUdp(server.ElapsedMilliseconds);
					break;
				}
			}
		}
	}

	public override void OnBeginInitialization()
	{
		physicsManager.Init();
	}

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		KeyValuePair<string, ConnectedClient> keyValuePair = connectingClients.FirstOrDefault((KeyValuePair<string, ConnectedClient> c) => c.Value.Player?.Equals(player) ?? false);
		if (keyValuePair.Key != null)
		{
			connectingClients.Remove(keyValuePair.Key);
		}
		if (player.client.IsSinglePlayerClient)
		{
			server.UdpSockets[0].Remove(player);
		}
		else
		{
			server.UdpSockets[1].Remove(player);
		}
		server.api.Logger.Notification("UDP: client disconnected " + player.PlayerName);
	}

	private void HandleConnectionRequest(UdpPacket udpPacket, UNetServer uNetServer)
	{
		try
		{
			Packet_ConnectionPacket connectionPacket = udpPacket.Packet.ConnectionPacket;
			if (connectionPacket == null)
			{
				return;
			}
			connectingClients.TryGetValue(connectionPacket?.LoginToken, out var client);
			if (client == null || uNetServer.EndPoints.ContainsKey(udpPacket.EndPoint))
			{
				return;
			}
			connectingClients.Remove(connectionPacket.LoginToken);
			uNetServer.Add(udpPacket.EndPoint, client.Id);
			client.ServerDidReceiveUdp = true;
			server.api.Logger.Notification($"UDP: Client {client.Id} connected via: {udpPacket.EndPoint}");
			Packet_Server packet = new Packet_Server
			{
				Id = 81
			};
			server.SendPacket(client.Id, packet);
			string clientLoginToken = client.LoginToken;
			if (client.IsSinglePlayerClient)
			{
				return;
			}
			Task.Run(async delegate
			{
				Packet_UdpPacket con = new Packet_UdpPacket
				{
					Id = 1,
					ConnectionPacket = new Packet_ConnectionPacket
					{
						LoginToken = clientLoginToken
					}
				};
				for (int i = 0; i < 20; i++)
				{
					server.ServerUdpNetwork.SendPacket_Threadsafe(client, con);
					await Task.Delay(500);
					if (client.State == EnumClientState.Offline || client.FallBackToTcp)
					{
						break;
					}
				}
			});
		}
		catch (Exception e)
		{
			server.api.Logger.Warning($"Error when connecting UDP client from {udpPacket.EndPoint}");
			server.api.Logger.Warning(e);
		}
	}

	public void HandlePlayerPosition(Packet_EntityPosition packet, ServerPlayer player)
	{
		if (packet == null)
		{
			return;
		}
		EntityPlayer entity = player.Entity;
		int num = entity.WatchedAttributes.GetInt("positionVersionNumber");
		if (packet.PositionVersion < num)
		{
			return;
		}
		player.LastReceivedClientPosition = server.ElapsedMilliseconds;
		int num2 = entity.Attributes.GetInt("tick");
		num2++;
		entity.Attributes.SetInt("tick", num2);
		entity.ServerPos.SetFromPacket(packet, entity);
		entity.Pos.SetFromPacket(packet, entity);
		foreach (EntityBehavior behavior in entity.SidedProperties.Behaviors)
		{
			if (behavior is IRemotePhysics remotePhysics)
			{
				remotePhysics.OnReceivedClientPos(num);
				break;
			}
		}
		Packet_EntityPosition entityPositionPacket = ServerPackets.getEntityPositionPacket(entity.ServerPos, entity, num2);
		entityPositionPacket.BodyYaw = CollectibleNet.SerializeFloatPrecise(entity.BodyYawServer);
		Packet_UdpPacket packet2 = new Packet_UdpPacket
		{
			Id = 5,
			EntityPosition = entityPositionPacket
		};
		entity.IsTeleport = false;
		AnimationPacket message = null;
		bool flag = entity.AnimManager?.AnimationsDirty ?? false;
		if (flag)
		{
			message = new AnimationPacket(entity);
			entity.AnimManager.AnimationsDirty = false;
		}
		foreach (ConnectedClient value in server.Clients.Values)
		{
			ServerPlayer player2 = value.Player;
			if (player2 != null && player2 != player && value.TrackedEntities.Contains(entity.EntityId))
			{
				ImmediateUdpQueue.QueuePacket(value, packet2);
				if (flag)
				{
					physicsManager.AnimationsAndTagsChannel.SendPacket(message, player2);
				}
			}
		}
	}

	public void HandleMountPosition(Packet_EntityPosition packet, ServerPlayer player)
	{
		if (packet == null)
		{
			return;
		}
		Entity entityById = server.api.World.GetEntityById(packet.EntityId);
		IMountable mountable = entityById?.GetInterface<IMountable>();
		if (mountable == null || !mountable.IsMountedBy(player.Entity))
		{
			return;
		}
		int num = entityById.WatchedAttributes.GetInt("positionVersionNumber");
		if (packet.PositionVersion < num)
		{
			return;
		}
		int num2 = entityById.Attributes.GetInt("tick");
		num2++;
		entityById.Attributes.SetInt("tick", num2);
		entityById.ServerPos.SetFromPacket(packet, entityById);
		entityById.Pos.SetFromPacket(packet, entityById);
		((entityById.SidedProperties == null) ? null : entityById.GetInterface<IMountable>()?.ControllingControls)?.FromInt(packet.MountControls);
		foreach (EntityBehavior behavior in entityById.SidedProperties.Behaviors)
		{
			if (behavior is IRemotePhysics remotePhysics)
			{
				remotePhysics.OnReceivedClientPos(num);
				break;
			}
		}
		Packet_EntityPosition entityPositionPacket = ServerPackets.getEntityPositionPacket(entityById.ServerPos, entityById, num2);
		Packet_UdpPacket packet2 = new Packet_UdpPacket
		{
			Id = 5,
			EntityPosition = entityPositionPacket
		};
		entityById.IsTeleport = false;
		AnimationPacket animationPacket = null;
		IAnimationManager animManager = entityById.AnimManager;
		if (animManager != null && animManager.AnimationsDirty)
		{
			animationPacket = new AnimationPacket(entityById);
			entityById.AnimManager.AnimationsDirty = false;
		}
		foreach (ConnectedClient value in server.Clients.Values)
		{
			if (value.Player != null && value.TrackedEntities.Contains(entityById.EntityId))
			{
				ImmediateUdpQueue.QueuePacket(value, packet2);
				if (animationPacket != null)
				{
					physicsManager.AnimationsAndTagsChannel.SendPacket(animationPacket, value.Player);
				}
			}
		}
	}

	public void SendPacket_Threadsafe(ConnectedClient client, Packet_BulkEntityPosition contentPacket)
	{
		Packet_UdpPacket packet = new Packet_UdpPacket
		{
			Id = 4,
			BulkPositions = contentPacket
		};
		ImmediateUdpQueue.QueuePacket(client, packet);
	}

	public void SendPacket_Threadsafe(ConnectedClient client, Packet_CustomPacket prePacked)
	{
		Packet_UdpPacket packet = new Packet_UdpPacket
		{
			Id = 6,
			ChannelPacket = prePacked
		};
		ImmediateUdpQueue.QueuePacket(client, packet);
	}

	public void SendPacket_Threadsafe(ConnectedClient client, Packet_UdpPacket prePacked)
	{
		ImmediateUdpQueue.QueuePacket(client, prePacked);
	}
}
