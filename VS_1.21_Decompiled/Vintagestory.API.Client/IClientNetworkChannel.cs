using System;

namespace Vintagestory.API.Client;

public interface IClientNetworkChannel : INetworkChannel
{
	bool Connected { get; }

	new IClientNetworkChannel RegisterMessageType(Type type);

	new IClientNetworkChannel RegisterMessageType<T>();

	IClientNetworkChannel SetMessageHandler<T>(NetworkServerMessageHandler<T> handler);

	void SendPacket<T>(T message);
}
