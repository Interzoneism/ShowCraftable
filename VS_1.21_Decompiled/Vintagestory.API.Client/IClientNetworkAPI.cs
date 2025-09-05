using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface IClientNetworkAPI : INetworkAPI
{
	new IClientNetworkChannel RegisterChannel(string channelName);

	new IClientNetworkChannel GetChannel(string channelName);

	EnumChannelState GetChannelState(string channelName);

	new IClientNetworkChannel RegisterUdpChannel(string channelName);

	new IClientNetworkChannel GetUdpChannel(string channelName);

	[Obsolete("Not dimension aware, use BlockPos overload instead, otherwise thie BlockEntity will probably not work correctly in other dimensions")]
	void SendBlockEntityPacket(int x, int y, int z, int packetId, byte[] data = null);

	void SendBlockEntityPacket(BlockPos pos, int packetId, byte[] data = null);

	void SendEntityPacket(long entityid, int packetId, byte[] data = null);

	void SendPlayerPositionPacket();

	void SendPlayerMountPositionPacket(Entity mount);

	void SendBlockEntityPacket(int x, int y, int z, object internalPacket);

	void SendEntityPacket(long entityid, object internalPacket);

	void SendEntityPacketWithOffset(long entityid, int packetIdOffset, object internalPacket);

	void SendArbitraryPacket(byte[] data);

	void SendPacketClient(object packetClient);

	void SendHandInteraction(int mouseButton, BlockSelection blockSelection, EntitySelection entitySelection, EnumHandInteract beforeUseType, int state, bool firstEvent, EnumItemUseCancelReason cancelReason);

	void SendPlayerNowReady();

	void SendBlockEntityPacket<T>(BlockPos pos, int packetId, T data = default(T));
}
