using System;
using System.IO;
using ProtoBuf;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class UdpNetworkChannel : NetworkChannel
{
	internal Action<Packet_CustomPacket, IServerPlayer>[] handlersUdp = new Action<Packet_CustomPacket, IServerPlayer>[256];

	[ThreadStatic]
	private static FastMemoryStream reusableStreamPerThread = new FastMemoryStream();

	private FastMemoryStream reusableStream => reusableStreamPerThread ?? (reusableStreamPerThread = new FastMemoryStream());

	public UdpNetworkChannel(NetworkAPI api, int channelId, string channelName)
		: base(api, channelId, channelName)
	{
	}

	public new void OnPacket(Packet_CustomPacket packet, IServerPlayer player)
	{
		if (packet.MessageId < handlersUdp.Length)
		{
			handlersUdp[packet.MessageId]?.Invoke(packet, player);
		}
	}

	public override IServerNetworkChannel SetMessageHandler<T>(NetworkClientMessageHandler<T> handler)
	{
		if (!messageTypes.TryGetValue(typeof(T), out var value))
		{
			throw new Exception("No such message type " + typeof(T)?.ToString() + " registered. Did you forgot to call RegisterMessageType?");
		}
		if (typeof(T).IsArray)
		{
			throw new ArgumentException("Please do not use array messages, they seem to cause serialization problems in rare cases. Pack that array into its own class.");
		}
		Serializer.PrepareSerializer<T>();
		handlersUdp[value] = delegate(Packet_CustomPacket p, IServerPlayer player)
		{
			T packet;
			using (FastMemoryStream fastMemoryStream = new FastMemoryStream(p.Data, p.Data.Length))
			{
				packet = Serializer.Deserialize<T>((Stream)fastMemoryStream);
			}
			handler(player, packet);
		};
		return this;
	}

	public override void BroadcastPacket<T>(T message, params IServerPlayer[] exceptPlayers)
	{
		if (!messageTypes.TryGetValue(typeof(T), out var value))
		{
			throw new Exception("No such message type " + typeof(T)?.ToString() + " registered. Did you forgot to call RegisterMessageType?");
		}
		reusableStream.Reset();
		Serializer.Serialize<T>((Stream)reusableStream, message);
		Packet_CustomPacket channelPacket = new Packet_CustomPacket
		{
			ChannelId = channelId,
			MessageId = value,
			Data = reusableStream.ToArray()
		};
		Packet_UdpPacket data = new Packet_UdpPacket
		{
			Id = 6,
			ChannelPacket = channelPacket
		};
		api.server.BroadcastArbitraryUdpPacket(data, exceptPlayers);
	}

	public override void SendPacket<T>(T message, params IServerPlayer[] players)
	{
		if (players == null || players.Length == 0)
		{
			throw new ArgumentNullException("No players supplied to send the packet to");
		}
		if (!messageTypes.TryGetValue(typeof(T), out var value))
		{
			throw new Exception("No such message type " + typeof(T)?.ToString() + " registered. Did you forgot to call RegisterMessageType?");
		}
		reusableStream.Reset();
		Serializer.Serialize<T>((Stream)reusableStream, message);
		Packet_CustomPacket channelPacket = new Packet_CustomPacket
		{
			ChannelId = channelId,
			MessageId = value,
			Data = reusableStream.ToArray()
		};
		Packet_UdpPacket packet = new Packet_UdpPacket
		{
			Id = 6,
			ChannelPacket = channelPacket
		};
		api.server.SendArbitraryUdpPacket(packet, players);
	}
}
