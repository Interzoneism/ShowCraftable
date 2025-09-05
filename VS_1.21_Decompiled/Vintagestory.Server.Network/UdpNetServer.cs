using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;

namespace Vintagestory.Server.Network;

public class UdpNetServer : UNetServer
{
	private UdpClient udpServer;

	private readonly CancellationTokenSource cts = new CancellationTokenSource();

	public static int MaxUdpPacketSize = 5000;

	private readonly Dictionary<int, IPEndPoint> endPointsReverse = new Dictionary<int, IPEndPoint>();

	private readonly CachingConcurrentDictionary<int, ConnectedClient> clients;

	private readonly ConcurrentQueue<UdpPacket> serverPacketQueue = new ConcurrentQueue<UdpPacket>();

	private Task udpListenTask;

	private int port { get; set; }

	private string ip { get; set; }

	public override Dictionary<IPEndPoint, int> EndPoints { get; } = new Dictionary<IPEndPoint, int>();

	public UdpNetServer(CachingConcurrentDictionary<int, ConnectedClient> clients)
	{
		this.clients = clients;
	}

	public override void SetIpAndPort(string ip, int port)
	{
		this.ip = ip;
		this.port = port;
	}

	public override void Start()
	{
		IPAddress iPAddress = (Socket.OSSupportsIPv6 ? IPAddress.IPv6Any : IPAddress.Any);
		bool flag = Socket.OSSupportsIPv6;
		if (ip != null)
		{
			iPAddress = IPAddress.Parse(ip);
			flag = false;
		}
		udpServer = new UdpClient(iPAddress.AddressFamily);
		if (flag)
		{
			udpServer.Client.DualMode = true;
		}
		udpServer.Client.Bind(new IPEndPoint(iPAddress, port));
		udpListenTask = new Task(ListenServer, null, cts.Token, TaskCreationOptions.LongRunning);
		udpListenTask.Start();
	}

	private async void ListenServer(object state)
	{
		while (!cts.IsCancellationRequested)
		{
			try
			{
				UdpReceiveResult udpReceiveResult = await udpServer.ReceiveAsync(cts.Token);
				if (udpReceiveResult.Buffer.Length > MaxUdpPacketSize)
				{
					continue;
				}
				Packet_UdpPacket packet_UdpPacket = new Packet_UdpPacket();
				Packet_UdpPacketSerializer.DeserializeBuffer(udpReceiveResult.Buffer, udpReceiveResult.Buffer.Length, packet_UdpPacket);
				int id = packet_UdpPacket.Id;
				if ((id < 1 || id > 7) ? true : false)
				{
					continue;
				}
				UdpPacket item = new UdpPacket
				{
					Packet = packet_UdpPacket
				};
				packet_UdpPacket.Length = udpReceiveResult.Buffer.Length;
				if (packet_UdpPacket.Id == 1)
				{
					item.EndPoint = udpReceiveResult.RemoteEndPoint;
					serverPacketQueue.Enqueue(item);
					continue;
				}
				int key = EndPoints.Get(udpReceiveResult.RemoteEndPoint, 0);
				if (clients.TryGetValue(key, out var value))
				{
					item.Client = value;
					serverPacketQueue.Enqueue(item);
				}
			}
			catch
			{
			}
		}
	}

	public override UdpPacket[] ReadMessage()
	{
		UdpPacket[] result = null;
		if (!serverPacketQueue.IsEmpty)
		{
			result = serverPacketQueue.ToArray();
			serverPacketQueue.Clear();
		}
		return result;
	}

	public override void Dispose()
	{
		cts.Cancel();
		cts.Dispose();
		udpServer.Dispose();
		udpListenTask.Dispose();
	}

	public override int SendToClient(int clientId, Packet_UdpPacket packet)
	{
		try
		{
			if (endPointsReverse.TryGetValue(clientId, out var value))
			{
				byte[] array = Packet_UdpPacketSerializer.SerializeToBytes(packet);
				udpServer.Send(array, value);
				return array.Length;
			}
		}
		catch
		{
		}
		return 0;
	}

	public override void Remove(IServerPlayer player)
	{
		if (endPointsReverse.TryGetValue(player.ClientId, out var value))
		{
			endPointsReverse.Remove(player.ClientId);
			EndPoints.Remove(value);
		}
	}

	public override void EnqueuePacket(UdpPacket udpPacket)
	{
		serverPacketQueue.Enqueue(udpPacket);
	}

	public override void Add(IPEndPoint endPoint, int clientId)
	{
		EndPoints.Add(endPoint, clientId);
		endPointsReverse.Add(clientId, endPoint);
	}
}
