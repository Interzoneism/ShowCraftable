using System;
using System.IO;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.NoObf;

public class UdpNetworkChannel : NetworkChannel
{
	[ThreadStatic]
	private static FastMemoryStream reusableStream;

	internal Action<Packet_CustomPacket>[] handlersUdp = new Action<Packet_CustomPacket>[128];

	public UdpNetworkChannel(NetworkAPI api, int channelId, string channelName)
		: base(api, channelId, channelName)
	{
	}

	public override IClientNetworkChannel SetMessageHandler<T>(NetworkServerMessageHandler<T> handler)
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
		handlersUdp[value] = delegate(Packet_CustomPacket p)
		{
			T packet = default(T);
			if (p.Data != null)
			{
				using FastMemoryStream fastMemoryStream = new FastMemoryStream(p.Data, p.Data.Length);
				packet = Serializer.Deserialize<T>((Stream)fastMemoryStream);
			}
			handler(packet);
		};
		return this;
	}

	public override void SendPacket<T>(T message)
	{
		if (!base.Connected)
		{
			throw new Exception("Attempting to send data to a not connected udp channel. For optionally dependent network channels test if your channel is Connected before sending data.");
		}
		if (!messageTypes.TryGetValue(typeof(T), out var value))
		{
			throw new Exception("No such message type " + typeof(T)?.ToString() + " registered. Did you forgot to call RegisterMessageType?");
		}
		using FastMemoryStream fastMemoryStream = reusableStream ?? (reusableStream = new FastMemoryStream());
		fastMemoryStream.Reset();
		Serializer.Serialize<T>((Stream)fastMemoryStream, message);
		Packet_CustomPacket channelPacket = new Packet_CustomPacket
		{
			ChannelId = channelId,
			MessageId = value,
			Data = fastMemoryStream.ToArray()
		};
		Packet_UdpPacket udpPacket = new Packet_UdpPacket
		{
			Id = 6,
			ChannelPacket = channelPacket
		};
		if (api.game.FallBackToTcp)
		{
			Packet_Client packetClient = new Packet_Client
			{
				Id = 35,
				UdpPacket = udpPacket
			};
			api.game.SendPacketClient(packetClient);
		}
		else
		{
			api.game.UdpNetClient.Send(udpPacket);
		}
	}

	public new void OnPacket(Packet_CustomPacket packet)
	{
		if (packet.MessageId < handlersUdp.Length)
		{
			handlersUdp[packet.MessageId]?.Invoke(packet);
		}
	}
}
