using System;

public class Packet_ChatLineSerializer
{
	private const int field = 8;

	public static Packet_ChatLine DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ChatLine packet_ChatLine = new Packet_ChatLine();
		DeserializeLengthDelimited(stream, packet_ChatLine);
		return packet_ChatLine;
	}

	public static Packet_ChatLine DeserializeBuffer(byte[] buffer, int length, Packet_ChatLine instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ChatLine Deserialize(CitoMemoryStream stream, Packet_ChatLine instance)
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
				instance.Message = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.Groupid = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.ChatType = ProtocolParser.ReadUInt32(stream);
				break;
			case 34:
				instance.Data = ProtocolParser.ReadString(stream);
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

	public static Packet_ChatLine DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ChatLine instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ChatLine result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ChatLine instance)
	{
		if (instance.Message != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Message);
		}
		if (instance.Groupid != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Groupid);
		}
		if (instance.ChatType != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.ChatType);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteString(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_ChatLine instance)
	{
		int num = 0;
		if (instance.Message != null)
		{
			num += ProtocolParser.GetSize(instance.Message) + 1;
		}
		if (instance.Groupid != 0)
		{
			num += ProtocolParser.GetSize(instance.Groupid) + 1;
		}
		if (instance.ChatType != 0)
		{
			num += ProtocolParser.GetSize(instance.ChatType) + 1;
		}
		if (instance.Data != null)
		{
			num += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ChatLine instance)
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

	public static byte[] SerializeToBytes(Packet_ChatLine instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ChatLine instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
