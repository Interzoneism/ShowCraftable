namespace Vintagestory.Common;

public class NetIncomingMessage
{
	public NetConnection SenderConnection;

	public NetworkMessageType Type;

	public byte[] message;

	public int messageLength;

	public int originalMessageLength;

	public static int ReadInt(byte[] readBuf)
	{
		return (readBuf[0] << 24) + (readBuf[1] << 16) + (readBuf[2] << 8) + readBuf[3];
	}

	public static void WriteInt(byte[] writeBuf, int n)
	{
		int num = (n >> 24) & 0xFF;
		int num2 = (n >> 16) & 0xFF;
		int num3 = (n >> 8) & 0xFF;
		int num4 = n & 0xFF;
		writeBuf[0] = (byte)num;
		writeBuf[1] = (byte)num2;
		writeBuf[2] = (byte)num3;
		writeBuf[3] = (byte)num4;
	}
}
