using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Vintagestory.Server.Network;

public class ServerNetManager : IDisposable
{
	public Socket Socket;

	private readonly CancellationToken cancellationToken;

	private bool disposed;

	public event TcpConnectionDelegate Connected;

	public event OnReceivedMessageDelegate ReceivedMessage;

	public event TcpConnectionDelegate Disconnected;

	public ServerNetManager(CancellationToken cts)
	{
		cancellationToken = cts;
	}

	public void StartServer(int port, string ipAddress = null)
	{
		bool oSSupportsIPv = Socket.OSSupportsIPv6;
		IPAddress iPAddress = (oSSupportsIPv ? IPAddress.IPv6Any : IPAddress.Any);
		bool flag = oSSupportsIPv;
		if (ipAddress != null)
		{
			iPAddress = IPAddress.Parse(ipAddress);
			flag = false;
		}
		try
		{
			Socket = new Socket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			if (flag)
			{
				Socket.DualMode = true;
			}
		}
		catch (NotSupportedException)
		{
			Console.Error.WriteLine("NotSupportedException thrown when trying to init a dual mode socket, maybe due to ipv6 being disabled on this system. Will attempt init ipv4 only socket.");
			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}
		Socket.NoDelay = true;
		Socket.Bind(new IPEndPoint(iPAddress, port));
		Socket.Listen(10);
		Socket.ReceiveTimeout = 5000;
		Task.Run((Func<Task?>)OnConnectRequest, cancellationToken);
	}

	private async Task OnConnectRequest()
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				Socket socket = await Socket.AcceptAsync(cancellationToken);
				string text = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
				if (TcpNetConnection.TemporaryIpBlockList && TcpNetConnection.blockedIps.Contains(text))
				{
					ServerMain.Logger.Notification("Client " + text + " disconnected, blacklisted");
					socket.Disconnect(reuseSocket: false);
					continue;
				}
				socket.ReceiveBufferSize = 4096;
				TcpNetConnection tcpNetConnection = new TcpNetConnection(socket);
				tcpNetConnection.ReceivedMessage += NewConnReceivedMessage;
				tcpNetConnection.Disconnected += NewConnDisconnected;
				tcpNetConnection.StartReceiving();
			}
			catch
			{
			}
		}
	}

	private void NewConnDisconnected(TcpNetConnection tcpConnection)
	{
		try
		{
			this.Disconnected(tcpConnection);
		}
		catch
		{
		}
	}

	private void NewConnReceivedMessage(byte[] data, TcpNetConnection tcpConnection)
	{
		try
		{
			if (!tcpConnection.Connected && this.Connected != null)
			{
				tcpConnection.Connected = true;
				this.Connected(tcpConnection);
			}
			this.ReceivedMessage(data, tcpConnection);
		}
		catch
		{
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing && Socket != null)
			{
				Socket.Dispose();
				Socket = null;
			}
			disposed = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
