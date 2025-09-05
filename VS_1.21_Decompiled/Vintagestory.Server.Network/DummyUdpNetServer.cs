using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;

namespace Vintagestory.Server.Network;

public class DummyUdpNetServer : UNetServer
{
	private readonly IPEndPoint localEndpoint = new IPEndPoint(0L, 0);

	public ConnectedClient Client = new ConnectedClient(-1);

	private readonly Dictionary<int, IPEndPoint> endPointsReverse = new Dictionary<int, IPEndPoint>();

	private DummyNetwork network;

	private readonly List<UdpPacket> udpPacketList = new List<UdpPacket>();

	public override Dictionary<IPEndPoint, int> EndPoints { get; } = new Dictionary<IPEndPoint, int>();

	public override void SetIpAndPort(string ip, int port)
	{
	}

	public override void Start()
	{
	}

	public override UdpPacket[] ReadMessage()
	{
		Monitor.Enter(network.ServerReceiveBufferLock);
		UdpPacket[] result = null;
		if (network.ServerReceiveBuffer.Count > 0)
		{
			udpPacketList.Clear();
			foreach (Packet_UdpPacket item2 in network.ServerReceiveBuffer)
			{
				if (item2.Id == 1 || Client.Player != null)
				{
					UdpPacket item = new UdpPacket
					{
						Packet = item2,
						Client = Client,
						EndPoint = localEndpoint
					};
					udpPacketList.Add(item);
				}
			}
			result = udpPacketList.ToArray();
			network.ServerReceiveBuffer.Clear();
		}
		Monitor.Exit(network.ServerReceiveBufferLock);
		return result;
	}

	public override void Dispose()
	{
	}

	public override int SendToClient(int clientId, Packet_UdpPacket packet)
	{
		Monitor.Enter(network.ClientReceiveBufferLock);
		network.ClientReceiveBuffer.Enqueue(packet);
		Monitor.Exit(network.ClientReceiveBufferLock);
		return 0;
	}

	public override void Remove(IServerPlayer player)
	{
		IPEndPoint key = endPointsReverse[player.ClientId];
		endPointsReverse.Remove(player.ClientId);
		EndPoints.Remove(key);
	}

	public override void EnqueuePacket(UdpPacket udpPacket)
	{
		throw new NotImplementedException();
	}

	public override void Add(IPEndPoint endPoint, int clientId)
	{
		EndPoints.Add(endPoint, clientId);
		endPointsReverse.Add(clientId, endPoint);
	}

	public void SetNetwork(DummyNetwork dummyNetwork)
	{
		network = dummyNetwork;
	}
}
