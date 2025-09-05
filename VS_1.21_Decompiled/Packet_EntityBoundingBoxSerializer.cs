using System;

public class Packet_EntityBoundingBoxSerializer
{
	private const int field = 8;

	public static Packet_EntityBoundingBox DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityBoundingBox packet_EntityBoundingBox = new Packet_EntityBoundingBox();
		DeserializeLengthDelimited(stream, packet_EntityBoundingBox);
		return packet_EntityBoundingBox;
	}

	public static Packet_EntityBoundingBox DeserializeBuffer(byte[] buffer, int length, Packet_EntityBoundingBox instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityBoundingBox Deserialize(CitoMemoryStream stream, Packet_EntityBoundingBox instance)
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
				instance.SizeX = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.SizeY = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.SizeZ = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_EntityBoundingBox DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityBoundingBox instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_EntityBoundingBox result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_EntityBoundingBox instance)
	{
		if (instance.SizeX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.SizeX);
		}
		if (instance.SizeY != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.SizeY);
		}
		if (instance.SizeZ != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.SizeZ);
		}
	}

	public static int GetSize(Packet_EntityBoundingBox instance)
	{
		int num = 0;
		if (instance.SizeX != 0)
		{
			num += ProtocolParser.GetSize(instance.SizeX) + 1;
		}
		if (instance.SizeY != 0)
		{
			num += ProtocolParser.GetSize(instance.SizeY) + 1;
		}
		if (instance.SizeZ != 0)
		{
			num += ProtocolParser.GetSize(instance.SizeZ) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityBoundingBox instance)
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

	public static byte[] SerializeToBytes(Packet_EntityBoundingBox instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityBoundingBox instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
