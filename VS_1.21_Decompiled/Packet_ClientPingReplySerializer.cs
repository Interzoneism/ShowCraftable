using System;

public class Packet_ClientPingReplySerializer
{
	private const int field = 8;

	public static Packet_ClientPingReply DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientPingReply packet_ClientPingReply = new Packet_ClientPingReply();
		DeserializeLengthDelimited(stream, packet_ClientPingReply);
		return packet_ClientPingReply;
	}

	public static Packet_ClientPingReply DeserializeBuffer(byte[] buffer, int length, Packet_ClientPingReply instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientPingReply Deserialize(CitoMemoryStream stream, Packet_ClientPingReply instance)
	{
		instance.InitializeValues();
		while (true)
		{
			int num = stream.ReadByte();
			if ((num & 0x80) != 0)
			{
				num = ProtocolParser.ReadKeyAsInt(num, stream);
				if ((num & 0x4000) != 0)
				{
					if (num < 0)
					{
						break;
					}
					return null;
				}
			}
			if (num == 0)
			{
				return null;
			}
			ProtocolParser.SkipKey(stream, Key.Create(num));
		}
		return instance;
	}

	public static Packet_ClientPingReply DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientPingReply instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ClientPingReply result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ClientPingReply instance)
	{
	}

	public static int GetSize(Packet_ClientPingReply instance)
	{
		return instance.size = 0;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientPingReply instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int num = stream.Position();
		Serialize(stream, instance);
		int num2 = stream.Position() - num;
		if (num2 != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size + " != " + num2);
		}
	}

	public static byte[] SerializeToBytes(Packet_ClientPingReply instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientPingReply instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
