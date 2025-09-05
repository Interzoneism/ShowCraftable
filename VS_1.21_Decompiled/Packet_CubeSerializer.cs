using System;

public class Packet_CubeSerializer
{
	private const int field = 8;

	public static Packet_Cube DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Cube packet_Cube = new Packet_Cube();
		DeserializeLengthDelimited(stream, packet_Cube);
		return packet_Cube;
	}

	public static Packet_Cube DeserializeBuffer(byte[] buffer, int length, Packet_Cube instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Cube Deserialize(CitoMemoryStream stream, Packet_Cube instance)
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
				instance.Minx = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Miny = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Minz = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.Maxx = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Maxy = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Maxz = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_Cube DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Cube instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_Cube result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Cube instance)
	{
		if (instance.Minx != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Minx);
		}
		if (instance.Miny != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Miny);
		}
		if (instance.Minz != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Minz);
		}
		if (instance.Maxx != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Maxx);
		}
		if (instance.Maxy != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Maxy);
		}
		if (instance.Maxz != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Maxz);
		}
	}

	public static int GetSize(Packet_Cube instance)
	{
		int num = 0;
		if (instance.Minx != 0)
		{
			num += ProtocolParser.GetSize(instance.Minx) + 1;
		}
		if (instance.Miny != 0)
		{
			num += ProtocolParser.GetSize(instance.Miny) + 1;
		}
		if (instance.Minz != 0)
		{
			num += ProtocolParser.GetSize(instance.Minz) + 1;
		}
		if (instance.Maxx != 0)
		{
			num += ProtocolParser.GetSize(instance.Maxx) + 1;
		}
		if (instance.Maxy != 0)
		{
			num += ProtocolParser.GetSize(instance.Maxy) + 1;
		}
		if (instance.Maxz != 0)
		{
			num += ProtocolParser.GetSize(instance.Maxz) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Cube instance)
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

	public static byte[] SerializeToBytes(Packet_Cube instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Cube instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
