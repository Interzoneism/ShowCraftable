using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Vintagestory.Server.Network;

public class TcpNetConnection : NetConnection
{
	public static HashSet<string> blockedIps = new HashSet<string>();

	public static bool TemporaryIpBlockList = false;

	public const int ClientSocketBufferSize = 4096;

	public static int MaxPacketClientId = DetermineMaxPacketId();

	public const int clientIdentificationPacketId = 8;

	public const int pingPacketId = 18;

	public Socket TcpSocket;

	public string Address;

	public IPEndPoint IpEndpoint;

	public bool Connected;

	private bool _disposed;

	private Memory<byte> dataRcvBuf = new byte[4096];

	private CancellationTokenSource cts;

	public int MaxPacketSize = 5000;

	public event OnReceivedMessageDelegate ReceivedMessage;

	public event TcpConnectionDelegate Disconnected;

	public void SetLengthLimit(bool isCreative)
	{
		MaxPacketSize = (isCreative ? int.MaxValue : 1000000);
	}

	public void StartReceiving()
	{
		cts = new CancellationTokenSource();
		Task.Run((Func<Task?>)ReceiveData);
	}

	private async Task ReceiveData()
	{
		try
		{
			FastMemoryStream receivedBytes = null;
			while (TcpSocket.Connected && !cts.Token.IsCancellationRequested)
			{
				int num;
				try
				{
					num = await TcpSocket.ReceiveAsync(dataRcvBuf, cts.Token);
				}
				catch
				{
					InvokeDisconnected();
					break;
				}
				if (num <= 0)
				{
					InvokeDisconnected();
					break;
				}
				if ((base.client == null || base.client.IsNewClient) && num > 4 && (receivedBytes == null || receivedBytes.Position == 0L))
				{
					int num2 = dataRcvBuf.Span[4];
					if (num2 != 8 && num2 != 18)
					{
						DisconnectForBadPacket("Client " + Address + " disconnected, invalid packet received");
						break;
					}
				}
				if (receivedBytes == null)
				{
					receivedBytes = new FastMemoryStream(512);
				}
				receivedBytes.Write(dataRcvBuf.Span.Slice(0, num));
				while (receivedBytes.Position >= 4)
				{
					byte[] buffer = receivedBytes.GetBuffer();
					int num3 = NetIncomingMessage.ReadInt(buffer);
					bool flag = num3 < 0;
					num3 &= 0x7FFFFFFF;
					if (num3 > MaxPacketSize)
					{
						DisconnectForBadPacket($"Client {Address} disconnected, too large packet of {num3} bytes received");
						return;
					}
					if (num3 == 0)
					{
						receivedBytes.RemoveFromStart(4);
						continue;
					}
					if (receivedBytes.Position < 4 + num3)
					{
						break;
					}
					byte[] array;
					if (flag)
					{
						array = Compression.Decompress(buffer, 4, num3);
					}
					else
					{
						array = new byte[num3];
						for (int i = 0; i < num3; i++)
						{
							array[i] = buffer[4 + i];
						}
					}
					receivedBytes.RemoveFromStart(4 + num3);
					int num4 = ProtocolParser.PeekPacketId(array);
					if (num4 <= 0 || num4 >= (MaxPacketClientId + 1) * 8)
					{
						DisconnectForBadPacket("Client " + Address + " disconnected, send packet with invalid client packet id: " + num4);
						return;
					}
					this.ReceivedMessage(array, this);
				}
			}
		}
		catch
		{
			InvokeDisconnected();
		}
	}

	private void DisconnectForBadPacket(string msg)
	{
		if (TemporaryIpBlockList)
		{
			blockedIps.Add(((IPEndPoint)TcpSocket.RemoteEndPoint).Address.ToString());
		}
		InvokeDisconnected();
		ServerMain.Logger.Notification(msg);
	}

	public override EnumSendResult Send(byte[] data, bool compressedFlag)
	{
		try
		{
			int num = data.Length;
			byte[] array = new byte[num + 4];
			NetIncomingMessage.WriteInt(array, num | (int)((compressedFlag ? 1u : 0u) << 31));
			for (int i = 0; i < num; i++)
			{
				array[4 + i] = data[i];
			}
			TcpSocket.SendAsync(array, SocketFlags.None, cts.Token);
			return EnumSendResult.Ok;
		}
		catch
		{
			InvokeDisconnected();
			return EnumSendResult.Disconnected;
		}
	}

	public EnumSendResult SendPreparedBytes(byte[] dataWithLength, int length, bool compressedFlag)
	{
		if (cts == null)
		{
			return EnumSendResult.Disconnected;
		}
		try
		{
			NetIncomingMessage.WriteInt(dataWithLength, length | (int)((compressedFlag ? 1u : 0u) << 31));
			TcpSocket.SendAsync(dataWithLength, SocketFlags.None, cts.Token);
			return EnumSendResult.Ok;
		}
		catch
		{
			InvokeDisconnected();
			return EnumSendResult.Disconnected;
		}
	}

	public override string ToString()
	{
		if (Address != null)
		{
			return Address;
		}
		return base.ToString();
	}

	public TcpNetConnection(Socket tcpSocket)
	{
		TcpSocket = tcpSocket;
		if (tcpSocket.RemoteEndPoint is IPEndPoint iPEndPoint)
		{
			IpEndpoint = iPEndPoint;
			Address = iPEndPoint.Address.ToString();
		}
		else
		{
			IpEndpoint = new IPEndPoint(0L, 0);
			Address = "0.0.0.0";
		}
	}

	public override IPEndPoint RemoteEndPoint()
	{
		return IpEndpoint;
	}

	public override EnumSendResult HiPerformanceSend(BoxedPacket box, ILogger Logger, bool compressionAllowed)
	{
		bool compressed;
		byte[] packet = PreparePacketForSending(box, compressionAllowed, out compressed);
		return SendPreparedPacket(packet, compressed, Logger);
	}

	public override byte[] PreparePacketForSending(BoxedPacket box, bool compressionAllowed, out bool compressed)
	{
		int num = box.Length;
		compressed = false;
		byte[] array;
		if (num > 1460 && compressionAllowed)
		{
			array = Compression.CompressOffset4(box.buffer, num);
			num = array.Length - 4;
			compressed = true;
		}
		else
		{
			array = box.Clone(4);
		}
		box.LengthSent = num;
		return array;
	}

	public override EnumSendResult SendPreparedPacket(byte[] packet, bool compressed, ILogger Logger)
	{
		try
		{
			return SendPreparedBytes(packet, packet.Length - 4, compressed);
		}
		catch (Exception e)
		{
			Logger.Error("Network exception:");
			Logger.Error(e);
			return EnumSendResult.Error;
		}
	}

	public override bool EqualsConnection(NetConnection connection)
	{
		return TcpSocket == ((TcpNetConnection)connection).TcpSocket;
	}

	public override void Shutdown()
	{
		if (TcpSocket == null)
		{
			return;
		}
		try
		{
			TcpSocket.Shutdown(SocketShutdown.Both);
		}
		catch
		{
		}
	}

	public override void Close()
	{
		try
		{
			cts?.Cancel();
		}
		catch
		{
		}
		try
		{
			TcpSocket?.Close();
		}
		catch
		{
		}
		Dispose();
	}

	internal void InvokeDisconnected()
	{
		if (!_disposed)
		{
			try
			{
				cts.Cancel();
			}
			catch
			{
			}
			try
			{
				TcpSocket.Close();
			}
			catch
			{
			}
			if (this.Disconnected != null && TcpSocket != null && Connected)
			{
				this.Disconnected(this);
				Connected = false;
			}
			Dispose();
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			TcpSocket.Dispose();
			cts.Dispose();
			TcpSocket = null;
			cts = null;
		}
	}

	public static int DetermineMaxPacketId()
	{
		MemberInfo[] members = typeof(Packet_ClientIdEnum).GetMembers();
		int num = 0;
		MemberInfo[] array = members;
		foreach (MemberInfo memberInfo in array)
		{
			if (memberInfo.MemberType == MemberTypes.Field && memberInfo is FieldInfo fieldInfo && fieldInfo.FieldType.Name == "Int32" && fieldInfo.GetValue(fieldInfo) is int num2 && num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}
}
