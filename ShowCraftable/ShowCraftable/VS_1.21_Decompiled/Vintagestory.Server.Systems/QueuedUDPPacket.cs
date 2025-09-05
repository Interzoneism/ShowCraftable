using System;

namespace Vintagestory.Server.Systems;

internal class QueuedUDPPacket
{
	internal Packet_UdpPacket packet;

	internal ConnectedClient client;

	internal long creationTime;

	public QueuedUDPPacket(ConnectedClient client, Packet_UdpPacket udpPacket)
	{
		packet = udpPacket;
		this.client = client;
		creationTime = Environment.TickCount;
	}
}
