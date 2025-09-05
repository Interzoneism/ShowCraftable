using System;

public class Packet_GrindingPropertiesSerializer
{
	private const int field = 8;

	public static Packet_GrindingProperties DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_GrindingProperties packet_GrindingProperties = new Packet_GrindingProperties();
		DeserializeLengthDelimited(stream, packet_GrindingProperties);
		return packet_GrindingProperties;
	}

	public static Packet_GrindingProperties DeserializeBuffer(byte[] buffer, int length, Packet_GrindingProperties instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_GrindingProperties Deserialize(CitoMemoryStream stream, Packet_GrindingProperties instance)
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
				instance.GroundStack = ProtocolParser.ReadBytes(stream);
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

	public static Packet_GrindingProperties DeserializeLengthDelimited(CitoMemoryStream stream, Packet_GrindingProperties instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_GrindingProperties result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_GrindingProperties instance)
	{
		if (instance.GroundStack != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.GroundStack);
		}
	}

	public static int GetSize(Packet_GrindingProperties instance)
	{
		int num = 0;
		if (instance.GroundStack != null)
		{
			num += ProtocolParser.GetSize(instance.GroundStack) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_GrindingProperties instance)
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

	public static byte[] SerializeToBytes(Packet_GrindingProperties instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_GrindingProperties instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
