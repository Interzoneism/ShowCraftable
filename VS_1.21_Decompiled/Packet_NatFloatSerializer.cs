using System;

public class Packet_NatFloatSerializer
{
	private const int field = 8;

	public static Packet_NatFloat DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_NatFloat packet_NatFloat = new Packet_NatFloat();
		DeserializeLengthDelimited(stream, packet_NatFloat);
		return packet_NatFloat;
	}

	public static Packet_NatFloat DeserializeBuffer(byte[] buffer, int length, Packet_NatFloat instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_NatFloat Deserialize(CitoMemoryStream stream, Packet_NatFloat instance)
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
				instance.Avg = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Var = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Dist = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_NatFloat DeserializeLengthDelimited(CitoMemoryStream stream, Packet_NatFloat instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_NatFloat result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_NatFloat instance)
	{
		if (instance.Avg != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Avg);
		}
		if (instance.Var != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Var);
		}
		if (instance.Dist != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Dist);
		}
	}

	public static int GetSize(Packet_NatFloat instance)
	{
		int num = 0;
		if (instance.Avg != 0)
		{
			num += ProtocolParser.GetSize(instance.Avg) + 1;
		}
		if (instance.Var != 0)
		{
			num += ProtocolParser.GetSize(instance.Var) + 1;
		}
		if (instance.Dist != 0)
		{
			num += ProtocolParser.GetSize(instance.Dist) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_NatFloat instance)
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

	public static byte[] SerializeToBytes(Packet_NatFloat instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_NatFloat instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
