using System;

public class Packet_ClientRequestJoinSerializer
{
	private const int field = 8;

	public static Packet_ClientRequestJoin DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientRequestJoin packet_ClientRequestJoin = new Packet_ClientRequestJoin();
		DeserializeLengthDelimited(stream, packet_ClientRequestJoin);
		return packet_ClientRequestJoin;
	}

	public static Packet_ClientRequestJoin DeserializeBuffer(byte[] buffer, int length, Packet_ClientRequestJoin instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientRequestJoin Deserialize(CitoMemoryStream stream, Packet_ClientRequestJoin instance)
	{
		instance.InitializeValues();
		int num;
		while (true)
		{
			num = stream.ReadByte();
			if ((num & 0x80) != 0)
			{
				num = ProtocolParser.ReadKeyAsInt(num, stream);
				if ((num & 0x4000) != 0)
				{
					break;
				}
			}
			switch (num)
			{
			case 0:
				return null;
			case 10:
				instance.Language = ProtocolParser.ReadString(stream);
				break;
			default:
				ProtocolParser.SkipKey(stream, Key.Create(num));
				break;
			}
		}
		if (num >= 0)
		{
			return null;
		}
		return instance;
	}

	public static Packet_ClientRequestJoin DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientRequestJoin instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ClientRequestJoin result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ClientRequestJoin instance)
	{
		if (instance.Language != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Language);
		}
	}

	public static int GetSize(Packet_ClientRequestJoin instance)
	{
		int num = 0;
		if (instance.Language != null)
		{
			num += ProtocolParser.GetSize(instance.Language) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientRequestJoin instance)
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

	public static byte[] SerializeToBytes(Packet_ClientRequestJoin instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientRequestJoin instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
