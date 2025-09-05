using System;

public class Packet_GeneratedStructureSerializer
{
	private const int field = 8;

	public static Packet_GeneratedStructure DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_GeneratedStructure packet_GeneratedStructure = new Packet_GeneratedStructure();
		DeserializeLengthDelimited(stream, packet_GeneratedStructure);
		return packet_GeneratedStructure;
	}

	public static Packet_GeneratedStructure DeserializeBuffer(byte[] buffer, int length, Packet_GeneratedStructure instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_GeneratedStructure Deserialize(CitoMemoryStream stream, Packet_GeneratedStructure instance)
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
			case 8:
				instance.X1 = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Y1 = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Z1 = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.X2 = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Y2 = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Z2 = ProtocolParser.ReadUInt32(stream);
				break;
			case 58:
				instance.Code = ProtocolParser.ReadString(stream);
				break;
			case 66:
				instance.Group = ProtocolParser.ReadString(stream);
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

	public static Packet_GeneratedStructure DeserializeLengthDelimited(CitoMemoryStream stream, Packet_GeneratedStructure instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_GeneratedStructure result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_GeneratedStructure instance)
	{
		if (instance.X1 != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.X1);
		}
		if (instance.Y1 != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Y1);
		}
		if (instance.Z1 != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Z1);
		}
		if (instance.X2 != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.X2);
		}
		if (instance.Y2 != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Y2);
		}
		if (instance.Z2 != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Z2);
		}
		if (instance.Code != null)
		{
			stream.WriteByte(58);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Group != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteString(stream, instance.Group);
		}
	}

	public static int GetSize(Packet_GeneratedStructure instance)
	{
		int num = 0;
		if (instance.X1 != 0)
		{
			num += ProtocolParser.GetSize(instance.X1) + 1;
		}
		if (instance.Y1 != 0)
		{
			num += ProtocolParser.GetSize(instance.Y1) + 1;
		}
		if (instance.Z1 != 0)
		{
			num += ProtocolParser.GetSize(instance.Z1) + 1;
		}
		if (instance.X2 != 0)
		{
			num += ProtocolParser.GetSize(instance.X2) + 1;
		}
		if (instance.Y2 != 0)
		{
			num += ProtocolParser.GetSize(instance.Y2) + 1;
		}
		if (instance.Z2 != 0)
		{
			num += ProtocolParser.GetSize(instance.Z2) + 1;
		}
		if (instance.Code != null)
		{
			num += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Group != null)
		{
			num += ProtocolParser.GetSize(instance.Group) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_GeneratedStructure instance)
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

	public static byte[] SerializeToBytes(Packet_GeneratedStructure instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_GeneratedStructure instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
