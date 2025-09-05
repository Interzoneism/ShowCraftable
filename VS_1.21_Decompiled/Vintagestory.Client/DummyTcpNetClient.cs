using System;
using System.Threading;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class DummyTcpNetClient : NetClient
{
	internal DummyNetwork network;

	public override int CurrentlyReceivingBytes => 0;

	public override void Connect(string ip, int port, Action<ConnectionResult> OnConnectionResult, Action<Exception> OnDisconnected)
	{
	}

	public override NetIncomingMessage ReadMessage()
	{
		NetIncomingMessage netIncomingMessage = null;
		Monitor.Enter(network.ClientReceiveBufferLock);
		if (network.ClientReceiveBuffer.Count > 0)
		{
			netIncomingMessage = new NetIncomingMessage();
			DummyNetworkPacket dummyNetworkPacket = network.ClientReceiveBuffer.Dequeue() as DummyNetworkPacket;
			netIncomingMessage.message = dummyNetworkPacket.Data;
			netIncomingMessage.messageLength = dummyNetworkPacket.Length;
		}
		Monitor.Exit(network.ClientReceiveBufferLock);
		return netIncomingMessage;
	}

	public override void Send(byte[] data)
	{
		Monitor.Enter(network.ServerReceiveBufferLock);
		DummyNetworkPacket dummyNetworkPacket = new DummyNetworkPacket();
		dummyNetworkPacket.Data = data;
		dummyNetworkPacket.Length = data.Length;
		network.ServerReceiveBuffer.Enqueue(dummyNetworkPacket);
		Monitor.Exit(network.ServerReceiveBufferLock);
	}

	public void SetNetwork(DummyNetwork network_)
	{
		network = network_;
	}
}
