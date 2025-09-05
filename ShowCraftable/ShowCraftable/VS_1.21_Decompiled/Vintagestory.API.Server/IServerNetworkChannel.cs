using System;
using Vintagestory.API.Client;

namespace Vintagestory.API.Server;

public interface IServerNetworkChannel : INetworkChannel
{
	new IServerNetworkChannel RegisterMessageType(Type type);

	new IServerNetworkChannel RegisterMessageType<T>();

	IServerNetworkChannel SetMessageHandler<T>(NetworkClientMessageHandler<T> messageHandler);

	void SendPacket<T>(T message, params IServerPlayer[] players);

	void SendPacket<T>(T message, byte[] data, params IServerPlayer[] players);

	void BroadcastPacket<T>(T message, params IServerPlayer[] exceptPlayers);
}
