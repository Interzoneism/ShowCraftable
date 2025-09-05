using System;
using System.Collections.Concurrent;
using System.Threading;
using Vintagestory.Common;
using Vintagestory.Server.Network;

namespace Vintagestory.Server;

public class TcpNetServer : NetServer
{
	protected ServerNetManager server;

	private ConcurrentQueue<NetIncomingMessage> messages;

	private int Port;

	private string Ip;

	public CancellationTokenSource cts = new CancellationTokenSource();

	private bool disposed;

	public override string LocalEndpoint => Ip;

	public TcpNetServer()
	{
		messages = new ConcurrentQueue<NetIncomingMessage>();
		server = new ServerNetManager(cts.Token);
	}

	public override void Start()
	{
		server.StartServer(Port, Ip);
		server.Connected += ServerConnected;
		server.ReceivedMessage += ServerReceivedMessage;
		server.Disconnected += ServerDisconnected;
	}

	private void ServerConnected(TcpNetConnection tcpConnection)
	{
		NetIncomingMessage netIncomingMessage = new NetIncomingMessage();
		netIncomingMessage.Type = NetworkMessageType.Connect;
		netIncomingMessage.SenderConnection = tcpConnection;
		messages.Enqueue(netIncomingMessage);
	}

	private void ServerDisconnected(TcpNetConnection tcpConnection)
	{
		NetIncomingMessage netIncomingMessage = new NetIncomingMessage();
		netIncomingMessage.Type = NetworkMessageType.Disconnect;
		netIncomingMessage.SenderConnection = tcpConnection;
		messages.Enqueue(netIncomingMessage);
	}

	private void ServerReceivedMessage(byte[] data, TcpNetConnection tcpConnection)
	{
		NetIncomingMessage netIncomingMessage = new NetIncomingMessage();
		netIncomingMessage.Type = NetworkMessageType.Data;
		netIncomingMessage.message = data;
		netIncomingMessage.messageLength = data.Length;
		netIncomingMessage.SenderConnection = tcpConnection;
		messages.Enqueue(netIncomingMessage);
	}

	public override NetIncomingMessage ReadMessage()
	{
		if (messages.TryDequeue(out var result))
		{
			return result;
		}
		return null;
	}

	public override void SetIpAndPort(string ip, int port)
	{
		Ip = ip;
		Port = port;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				cts.Cancel();
				cts.Dispose();
				server.Dispose();
				messages.Clear();
			}
			disposed = true;
		}
	}

	public override void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
