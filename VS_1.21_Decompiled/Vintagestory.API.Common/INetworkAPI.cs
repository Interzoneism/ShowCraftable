using Vintagestory.API.Client;

namespace Vintagestory.API.Common;

public interface INetworkAPI
{
	INetworkChannel RegisterChannel(string channelName);

	INetworkChannel RegisterUdpChannel(string channelName);

	INetworkChannel GetChannel(string channelName);

	INetworkChannel GetUdpChannel(string channelName);
}
