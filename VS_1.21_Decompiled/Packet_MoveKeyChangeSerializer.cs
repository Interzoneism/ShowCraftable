using System;

public class Packet_MoveKeyChangeSerializer
{
	private const int field = 8;

	public static Packet_MoveKeyChange DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_MoveKeyChange packet_MoveKeyChange = new Packet_MoveKeyChange();
		DeserializeLengthDelimited(stream, packet_MoveKeyChange);
		return packet_MoveKeyChange;
	}

	public static Packet_MoveKeyChange DeserializeBuffer(byte[] buffer, int length, Packet_MoveKeyChange instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_MoveKeyChange Deserialize(CitoMemoryStream stream, Packet_MoveKeyChange instance)
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
				instance.Key = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Down = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_MoveKeyChange DeserializeLengthDelimited(CitoMemoryStream stream, Packet_MoveKeyChange instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_MoveKeyChange result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_MoveKeyChange instance)
	{
		if (instance.Key != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Key);
		}
		if (instance.Down != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Down);
		}
	}

	public static int GetSize(Packet_MoveKeyChange instance)
	{
		int num = 0;
		if (instance.Key != 0)
		{
			num += ProtocolParser.GetSize(instance.Key) + 1;
		}
		if (instance.Down != 0)
		{
			num += ProtocolParser.GetSize(instance.Down) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_MoveKeyChange instance)
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

	public static byte[] SerializeToBytes(Packet_MoveKeyChange instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_MoveKeyChange instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
