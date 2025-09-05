using System.Net;
using Vintagestory.Server;

namespace Vintagestory.Common.Network.Packets;

public struct UdpPacket
{
	public Packet_UdpPacket Packet;

	public ConnectedClient Client;

	public IPEndPoint EndPoint;
}
