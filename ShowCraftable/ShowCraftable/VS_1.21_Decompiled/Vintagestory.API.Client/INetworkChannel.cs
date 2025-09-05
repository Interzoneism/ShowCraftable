using System;

namespace Vintagestory.API.Client;

public interface INetworkChannel
{
	string ChannelName { get; }

	INetworkChannel RegisterMessageType(Type type);

	INetworkChannel RegisterMessageType<T>();
}
