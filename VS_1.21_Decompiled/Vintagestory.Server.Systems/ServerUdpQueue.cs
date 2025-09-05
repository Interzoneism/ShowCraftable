using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Vintagestory.Server.Systems;

public class ServerUdpQueue
{
	private const int PacketTTL = 750;

	private readonly ServerMain server;

	private readonly ServerUdpNetwork network;

	private ConcurrentQueue<QueuedUDPPacket> queue = new ConcurrentQueue<QueuedUDPPacket>();

	private bool idle;

	public ServerUdpQueue(ServerMain server, ServerUdpNetwork network)
	{
		this.server = server;
		this.network = network;
		network.ImmediateUdpQueue = this;
	}

	internal void QueuePacket(ConnectedClient client, Packet_UdpPacket packet)
	{
		queue.Enqueue(new QueuedUDPPacket(client, packet));
		if (idle)
		{
			lock (this)
			{
				Monitor.Pulse(this);
			}
		}
	}

	internal void DedicatedThreadLoop()
	{
		while (!server.stopped && !server.exit.exit)
		{
			if (queue.IsEmpty)
			{
				try
				{
					idle = true;
					lock (this)
					{
						Monitor.Wait(this, 10);
					}
					idle = false;
				}
				catch (ThreadInterruptedException)
				{
				}
			}
			long num = Environment.TickCount - 750;
			QueuedUDPPacket result;
			while (queue.TryDequeue(out result))
			{
				if (result.creationTime > num)
				{
					try
					{
						server.SendPacketBlocking(result.client, result.packet);
					}
					catch (Exception e)
					{
						ServerMain.Logger.Error(e);
					}
				}
			}
		}
	}
}
