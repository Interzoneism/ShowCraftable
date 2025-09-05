using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Common;

public class TCPNetworkConnection : INetworkConnection
{
	private bool connected;

	private bool disconnected;

	public VintageStorySocket tcpSocket;

	public string address;

	private Memory<byte> dataRcvBuf = new byte[8192];

	private Action<Exception> onDisconnected;

	private Queue<byte> received = new Queue<byte>();

	private Queue<byte> tosendBeforeConnected = new Queue<byte>();

	private CancellationTokenSource cts = new CancellationTokenSource();

	public bool Connected => connected;

	public bool Disconnected => disconnected;

	public TCPNetworkConnection(string ip, int port, Action<ConnectionResult> onConnectResult, Action<Exception> onDisconnected)
	{
		TCPNetworkConnection tCPNetworkConnection = this;
		this.onDisconnected = onDisconnected;
		if (IPAddress.TryParse(ip, out IPAddress iPAddress) && iPAddress.AddressFamily == AddressFamily.InterNetworkV6)
		{
			tcpSocket = new VintageStorySocket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		}
		else
		{
			try
			{
				tcpSocket = new VintageStorySocket(SocketType.Stream, ProtocolType.Tcp);
				tcpSocket.DualMode = true;
			}
			catch (NotSupportedException)
			{
				Console.Error.WriteLine("NotSupportedException thrown when trying to init a dual mode socket. Will attempt init ipv4 only socket.");
				tcpSocket = new VintageStorySocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			}
		}
		tcpSocket.NoDelay = true;
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				await tCPNetworkConnection.tcpSocket.ConnectAsync(ip, port, tCPNetworkConnection.cts.Token);
				tCPNetworkConnection.connected = tCPNetworkConnection.tcpSocket.Connected;
				tCPNetworkConnection.address = tCPNetworkConnection.tcpSocket.RemoteEndPoint?.ToString();
				ConnectionResult obj = new ConnectionResult
				{
					connected = tCPNetworkConnection.connected
				};
				onConnectResult(obj);
				if (tCPNetworkConnection.tosendBeforeConnected.Count > 0)
				{
					tCPNetworkConnection.Send(tCPNetworkConnection.tosendBeforeConnected.ToArray(), tCPNetworkConnection.tosendBeforeConnected.Count);
					tCPNetworkConnection.tosendBeforeConnected.Clear();
				}
				Task task = new Task(tCPNetworkConnection.OnBytesReceived, null, tCPNetworkConnection.cts.Token, TaskCreationOptions.LongRunning);
				task.Start();
				await task.WaitAsync(tCPNetworkConnection.cts.Token);
			}
			catch (Exception ex2)
			{
				tCPNetworkConnection.Disconnect();
				ConnectionResult obj2 = new ConnectionResult
				{
					connected = tCPNetworkConnection.tcpSocket.Connected,
					errorMessage = ex2.Message,
					exception = ex2
				};
				onConnectResult(obj2);
			}
		}, cts.Token);
	}

	public void Disconnect()
	{
		if (tcpSocket != null)
		{
			try
			{
				tcpSocket.Shutdown(SocketShutdown.Send);
			}
			catch
			{
			}
			TyronThreadPool.QueueLongDurationTask(delegate
			{
				try
				{
					Thread.Sleep(1000);
					VintageStorySocket vintageStorySocket = tcpSocket;
					if (vintageStorySocket != null && !vintageStorySocket.Disposed)
					{
						tcpSocket.Shutdown(SocketShutdown.Receive);
						tcpSocket.Close();
						cts.Cancel();
						tcpSocket = null;
					}
				}
				catch
				{
				}
			}, "disconnect");
		}
		onDisconnected = null;
		disconnected = true;
		connected = false;
	}

	protected async void OnBytesReceived(object state)
	{
		try
		{
			while (tcpSocket.Connected && !cts.Token.IsCancellationRequested)
			{
				int num = await tcpSocket.ReceiveAsync(dataRcvBuf, cts.Token);
				if (num <= 0)
				{
					disconnected = true;
					if (onDisconnected == null)
					{
						try
						{
							tcpSocket.Close();
							cts.Cancel();
							break;
						}
						catch
						{
							break;
						}
					}
					onDisconnected(new Exception("The server closed down the socket without response. The server may be password protected or whitelisted"));
					break;
				}
				lock (received)
				{
					for (int i = 0; i < num; i++)
					{
						received.Enqueue(dataRcvBuf.Span[i]);
					}
				}
			}
		}
		catch (Exception obj2)
		{
			try
			{
				tcpSocket?.Close();
				cts.Cancel();
			}
			catch
			{
			}
			disconnected = true;
			onDisconnected?.Invoke(obj2);
		}
	}

	public void Receive(byte[] data, int dataLength, out int total)
	{
		total = 0;
		lock (received)
		{
			for (int i = 0; i < dataLength; i++)
			{
				if (received.Count == 0)
				{
					break;
				}
				data[i] = received.Dequeue();
				total++;
			}
		}
	}

	public void Send(byte[] data, int length)
	{
		if (!connected)
		{
			if (!disconnected)
			{
				for (int i = 0; i < length; i++)
				{
					tosendBeforeConnected.Enqueue(data[i]);
				}
			}
			return;
		}
		try
		{
			tcpSocket.SendAsync(data, SocketFlags.None, cts.Token);
		}
		catch (Exception obj)
		{
			disconnected = true;
			onDisconnected?.Invoke(obj);
		}
	}

	public override string ToString()
	{
		return address ?? base.ToString();
	}
}
