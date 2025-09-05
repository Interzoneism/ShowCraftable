using System;

public class Packet_LoginTokenQuerySerializer
{
	private const int field = 8;

	public static Packet_LoginTokenQuery DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_LoginTokenQuery packet_LoginTokenQuery = new Packet_LoginTokenQuery();
		DeserializeLengthDelimited(stream, packet_LoginTokenQuery);
		return packet_LoginTokenQuery;
	}

	public static Packet_LoginTokenQuery DeserializeBuffer(byte[] buffer, int length, Packet_LoginTokenQuery instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_LoginTokenQuery Deserialize(CitoMemoryStream stream, Packet_LoginTokenQuery instance)
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

	public static Packet_LoginTokenQuery DeserializeLengthDelimited(CitoMemoryStream stream, Packet_LoginTokenQuery instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_LoginTokenQuery result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_LoginTokenQuery instance)
	{
	}

	public static int GetSize(Packet_LoginTokenQuery instance)
	{
		return instance.size = 0;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_LoginTokenQuery instance)
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

	public static byte[] SerializeToBytes(Packet_LoginTokenQuery instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_LoginTokenQuery instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
