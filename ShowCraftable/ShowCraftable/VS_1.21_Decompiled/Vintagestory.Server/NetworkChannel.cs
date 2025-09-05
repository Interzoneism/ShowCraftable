using System;
using System.IO;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class NetworkChannel : NetworkChannelBase, IServerNetworkChannel, INetworkChannel
{
	protected NetworkAPI api;

	internal Action<Packet_CustomPacket, IServerPlayer>[] handlers = new Action<Packet_CustomPacket, IServerPlayer>[256];

	[ThreadStatic]
	private static FastMemoryStream reusableStreamPerThread = new FastMemoryStream();

	private FastMemoryStream reusableStream => reusableStreamPerThread ?? (reusableStreamPerThread = new FastMemoryStream());

	public NetworkChannel(NetworkAPI api, int channelId, string channelName)
		: base(channelId, channelName)
	{
		this.api = api;
	}

	public void OnPacket(Packet_CustomPacket p, IServerPlayer player)
	{
		if (p.MessageId < handlers.Length)
		{
			handlers[p.MessageId]?.Invoke(p, player);
			ServerMain.FrameProfiler.Mark("handlecustom", p.MessageId);
		}
	}

	public new IServerNetworkChannel RegisterMessageType(Type type)
	{
		messageTypes[type] = nextHandlerId++;
		return this;
	}

	public new IServerNetworkChannel RegisterMessageType<T>()
	{
		messageTypes[typeof(T)] = nextHandlerId++;
		return this;
	}

	public virtual IServerNetworkChannel SetMessageHandler<T>(NetworkClientMessageHandler<T> handler)
	{
		if (!messageTypes.TryGetValue(typeof(T), out var value))
		{
			throw new Exception("No such message type " + typeof(T)?.ToString() + " registered. Did you forgot to call RegisterMessageType?");
		}
		handlers[value] = delegate(Packet_CustomPacket p, IServerPlayer player)
		{
			T packet;
			using (MemoryStream memoryStream = new MemoryStream(p.Data))
			{
				packet = Serializer.Deserialize<T>((Stream)memoryStream);
			}
			handler(player, packet);
		};
		return this;
	}

	public virtual void SendPacket<T>(T message, params IServerPlayer[] players)
	{
		if (players == null || players.Length == 0)
		{
			throw new ArgumentNullException("No players supplied to send the packet to");
		}
		api.server.SendArbitraryPacket(GenPacket(message), players);
	}

	public void SendPacket<T>(T message, byte[] data, params IServerPlayer[] players)
	{
		if (players == null || players.Length == 0)
		{
			throw new ArgumentNullException("No players supplied to send the packet to");
		}
		api.server.SendArbitraryPacket(GenPacket(message, data), players);
	}

	public virtual void BroadcastPacket<T>(T message, params IServerPlayer[] exceptPlayers)
	{
		api.server.BroadcastArbitraryPacket(GenPacket(message), exceptPlayers);
	}

	private Packet_Server GenPacket<T>(T message)
	{
		reusableStream.Reset();
		Serializer.Serialize<T>((Stream)reusableStream, message);
		return GenPacket(message, reusableStream.ToArray());
	}

	private Packet_Server GenPacket<T>(T message, byte[] data)
	{
		if (!messageTypes.TryGetValue(typeof(T), out var value))
		{
			throw new Exception("No such message type " + typeof(T)?.ToString() + " registered. Did you forgot to call RegisterMessageType?");
		}
		Packet_CustomPacket packet_CustomPacket = new Packet_CustomPacket
		{
			ChannelId = channelId,
			MessageId = value
		};
		packet_CustomPacket.SetData(data);
		return new Packet_Server
		{
			Id = 55,
			CustomPacket = packet_CustomPacket
		};
	}
}
