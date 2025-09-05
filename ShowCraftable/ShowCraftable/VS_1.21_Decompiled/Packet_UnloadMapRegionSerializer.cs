using System;

public class Packet_UnloadMapRegionSerializer
{
	private const int field = 8;

	public static Packet_UnloadMapRegion DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_UnloadMapRegion packet_UnloadMapRegion = new Packet_UnloadMapRegion();
		DeserializeLengthDelimited(stream, packet_UnloadMapRegion);
		return packet_UnloadMapRegion;
	}

	public static Packet_UnloadMapRegion DeserializeBuffer(byte[] buffer, int length, Packet_UnloadMapRegion instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_UnloadMapRegion Deserialize(CitoMemoryStream stream, Packet_UnloadMapRegion instance)
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
				instance.RegionX = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.RegionZ = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_UnloadMapRegion DeserializeLengthDelimited(CitoMemoryStream stream, Packet_UnloadMapRegion instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_UnloadMapRegion result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_UnloadMapRegion instance)
	{
		if (instance.RegionX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.RegionX);
		}
		if (instance.RegionZ != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.RegionZ);
		}
	}

	public static int GetSize(Packet_UnloadMapRegion instance)
	{
		int num = 0;
		if (instance.RegionX != 0)
		{
			num += ProtocolParser.GetSize(instance.RegionX) + 1;
		}
		if (instance.RegionZ != 0)
		{
			num += ProtocolParser.GetSize(instance.RegionZ) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_UnloadMapRegion instance)
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

	public static byte[] SerializeToBytes(Packet_UnloadMapRegion instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_UnloadMapRegion instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
