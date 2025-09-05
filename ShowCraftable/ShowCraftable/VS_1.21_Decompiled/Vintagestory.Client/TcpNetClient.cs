using System;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class TcpNetClient : NetClient
{
	private INetworkConnection tcpConnection;

	private QueueByte incoming;

	private byte[] data;

	private const int dataLength = 16384;

	private int received;

	public override int CurrentlyReceivingBytes => received + incoming.count;

	public TcpNetClient()
	{
		incoming = new QueueByte();
		data = new byte[16384];
	}

	public override void Dispose()
	{
		if (tcpConnection != null)
		{
			tcpConnection.Disconnect();
		}
	}

	public override void Connect(string host, int port, Action<ConnectionResult> OnConnectionResult, Action<Exception> OnDisconnected)
	{
		tcpConnection = new TCPNetworkConnection(host, port, OnConnectionResult, OnDisconnected);
	}

	public override NetIncomingMessage ReadMessage()
	{
		if (tcpConnection == null)
		{
			return null;
		}
		NetIncomingMessage netIncomingMessage = TryGetMessageFromIncoming();
		if (netIncomingMessage != null)
		{
			return netIncomingMessage;
		}
		for (int i = 0; i < 1; i++)
		{
			received = 0;
			tcpConnection.Receive(data, 16384, out received);
			if (received <= 0)
			{
				break;
			}
			for (int j = 0; j < received; j++)
			{
				incoming.Enqueue(data[j]);
			}
		}
		return TryGetMessageFromIncoming();
	}

	private NetIncomingMessage TryGetMessageFromIncoming()
	{
		if (incoming.count >= 4)
		{
			byte[] readBuf = new byte[4];
			incoming.PeekRange(readBuf, 4);
			int num = NetIncomingMessage.ReadInt(readBuf);
			bool flag = ((uint)num & int.MinValue) > 0;
			int num2 = num & 0x7FFFFFFF;
			if (incoming.count >= 4 + num2)
			{
				incoming.DequeueRange(new byte[4], 4);
				NetIncomingMessage netIncomingMessage = new NetIncomingMessage();
				netIncomingMessage.message = new byte[num2];
				netIncomingMessage.messageLength = num2;
				netIncomingMessage.originalMessageLength = num2;
				incoming.DequeueRange(netIncomingMessage.message, netIncomingMessage.messageLength);
				if (flag)
				{
					netIncomingMessage.message = Compression.Decompress(netIncomingMessage.message);
					netIncomingMessage.messageLength = netIncomingMessage.message.Length;
				}
				return netIncomingMessage;
			}
		}
		return null;
	}

	public override void Send(byte[] data)
	{
		byte[] array = new byte[data.Length + 4];
		NetIncomingMessage.WriteInt(array, data.Length);
		for (int i = 0; i < data.Length; i++)
		{
			array[i + 4] = data[i];
		}
		tcpConnection.Send(array, data.Length + 4);
	}
}
