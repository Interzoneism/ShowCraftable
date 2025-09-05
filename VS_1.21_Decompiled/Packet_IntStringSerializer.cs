using System;

public class Packet_IntStringSerializer
{
	private const int field = 8;

	public static Packet_IntString DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_IntString packet_IntString = new Packet_IntString();
		DeserializeLengthDelimited(stream, packet_IntString);
		return packet_IntString;
	}

	public static Packet_IntString DeserializeBuffer(byte[] buffer, int length, Packet_IntString instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_IntString Deserialize(CitoMemoryStream stream, Packet_IntString instance)
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
				instance.Key_ = ProtocolParser.ReadUInt32(stream);
				break;
			case 18:
				instance.Value_ = ProtocolParser.ReadString(stream);
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

	public static Packet_IntString DeserializeLengthDelimited(CitoMemoryStream stream, Packet_IntString instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_IntString result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_IntString instance)
	{
		if (instance.Key_ != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Key_);
		}
		if (instance.Value_ != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Value_);
		}
	}

	public static int GetSize(Packet_IntString instance)
	{
		int num = 0;
		if (instance.Key_ != 0)
		{
			num += ProtocolParser.GetSize(instance.Key_) + 1;
		}
		if (instance.Value_ != null)
		{
			num += ProtocolParser.GetSize(instance.Value_) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_IntString instance)
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

	public static byte[] SerializeToBytes(Packet_IntString instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_IntString instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
