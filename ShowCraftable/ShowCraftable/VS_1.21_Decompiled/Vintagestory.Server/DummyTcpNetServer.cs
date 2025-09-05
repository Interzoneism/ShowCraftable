using System.Threading;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class DummyTcpNetServer : NetServer
{
	internal DummyNetwork network;

	private DummyNetConnection connectedClient;

	private bool receivedAnyMessage;

	public override string LocalEndpoint => "127.0.0.1";

	public DummyTcpNetServer()
	{
		connectedClient = new DummyNetConnection();
	}

	public override void Start()
	{
	}

	public override NetIncomingMessage ReadMessage()
	{
		NetIncomingMessage netIncomingMessage = null;
		Monitor.Enter(network.ServerReceiveBufferLock);
		if (network.ServerReceiveBuffer.Count > 0)
		{
			if (!receivedAnyMessage)
			{
				receivedAnyMessage = true;
				netIncomingMessage = new NetIncomingMessage();
				netIncomingMessage.Type = NetworkMessageType.Connect;
				netIncomingMessage.SenderConnection = connectedClient;
			}
			else
			{
				netIncomingMessage = new NetIncomingMessage();
				DummyNetworkPacket dummyNetworkPacket = network.ServerReceiveBuffer.Dequeue() as DummyNetworkPacket;
				netIncomingMessage.message = dummyNetworkPacket.Data;
				netIncomingMessage.messageLength = dummyNetworkPacket.Length;
				netIncomingMessage.SenderConnection = connectedClient;
			}
		}
		Monitor.Exit(network.ServerReceiveBufferLock);
		return netIncomingMessage;
	}

	public void SetNetwork(DummyNetwork dummyNetwork)
	{
		network = dummyNetwork;
		connectedClient.network = network;
	}

	public override void SetIpAndPort(string ip, int port)
	{
	}

	public override void Dispose()
	{
	}
}
