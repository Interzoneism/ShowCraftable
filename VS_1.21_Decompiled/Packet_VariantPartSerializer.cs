using System;

public class Packet_VariantPartSerializer
{
	private const int field = 8;

	public static Packet_VariantPart DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_VariantPart packet_VariantPart = new Packet_VariantPart();
		DeserializeLengthDelimited(stream, packet_VariantPart);
		return packet_VariantPart;
	}

	public static Packet_VariantPart DeserializeBuffer(byte[] buffer, int length, Packet_VariantPart instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_VariantPart Deserialize(CitoMemoryStream stream, Packet_VariantPart instance)
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
				instance.Code = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.Value = ProtocolParser.ReadString(stream);
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

	public static Packet_VariantPart DeserializeLengthDelimited(CitoMemoryStream stream, Packet_VariantPart instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_VariantPart result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_VariantPart instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Value != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Value);
		}
	}

	public static int GetSize(Packet_VariantPart instance)
	{
		int num = 0;
		if (instance.Code != null)
		{
			num += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Value != null)
		{
			num += ProtocolParser.GetSize(instance.Value) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_VariantPart instance)
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

	public static byte[] SerializeToBytes(Packet_VariantPart instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_VariantPart instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
