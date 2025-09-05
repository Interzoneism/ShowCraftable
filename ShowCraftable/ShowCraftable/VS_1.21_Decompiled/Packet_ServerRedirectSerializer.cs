using System;

public class Packet_ServerRedirectSerializer
{
	private const int field = 8;

	public static Packet_ServerRedirect DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerRedirect packet_ServerRedirect = new Packet_ServerRedirect();
		DeserializeLengthDelimited(stream, packet_ServerRedirect);
		return packet_ServerRedirect;
	}

	public static Packet_ServerRedirect DeserializeBuffer(byte[] buffer, int length, Packet_ServerRedirect instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerRedirect Deserialize(CitoMemoryStream stream, Packet_ServerRedirect instance)
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
				instance.Name = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.Host = ProtocolParser.ReadString(stream);
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

	public static Packet_ServerRedirect DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerRedirect instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ServerRedirect result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerRedirect instance)
	{
		if (instance.Name != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Name);
		}
		if (instance.Host != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Host);
		}
	}

	public static int GetSize(Packet_ServerRedirect instance)
	{
		int num = 0;
		if (instance.Name != null)
		{
			num += ProtocolParser.GetSize(instance.Name) + 1;
		}
		if (instance.Host != null)
		{
			num += ProtocolParser.GetSize(instance.Host) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerRedirect instance)
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

	public static byte[] SerializeToBytes(Packet_ServerRedirect instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerRedirect instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
