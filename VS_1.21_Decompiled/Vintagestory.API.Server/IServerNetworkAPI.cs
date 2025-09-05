using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Server;

public interface IServerNetworkAPI : INetworkAPI
{
	new IServerNetworkChannel RegisterChannel(string channelName);

	new IServerNetworkChannel GetChannel(string channelName);

	new IServerNetworkChannel RegisterUdpChannel(string channelName);

	new IServerNetworkChannel GetUdpChannel(string channelName);

	void SendBlockEntityPacket(IServerPlayer player, BlockPos pos, int packetId, byte[] data = null);

	void SendEntityPacket(IServerPlayer player, long entityid, int packetId, byte[] data = null);

	void BroadcastEntityPacket(long entityid, int packetId, byte[] data = null);

	void BroadcastBlockEntityPacket(BlockPos pos, int packetId, byte[] data = null);

	void BroadcastBlockEntityPacket(BlockPos pos, int packetId, byte[] data = null, params IServerPlayer[] skipPlayers);

	void SendArbitraryPacket(byte[] data, params IServerPlayer[] players);

	void SendArbitraryPacket(object packet, params IServerPlayer[] players);

	void BroadcastArbitraryPacket(byte[] data, params IServerPlayer[] exceptPlayers);

	void BroadcastArbitraryPacket(object packet, params IServerPlayer[] exceptPlayers);

	void SendBlockEntityPacket<T>(IServerPlayer player, BlockPos pos, int packetId, T data = default(T));

	void BroadcastBlockEntityPacket<T>(BlockPos pos, int packetId, T data = default(T));
}
