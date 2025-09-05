using System;

public class Packet_QueuePacketSerializer
{
	private const int field = 8;

	public static Packet_QueuePacket DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_QueuePacket packet_QueuePacket = new Packet_QueuePacket();
		DeserializeLengthDelimited(stream, packet_QueuePacket);
		return packet_QueuePacket;
	}

	public static Packet_QueuePacket DeserializeBuffer(byte[] buffer, int length, Packet_QueuePacket instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_QueuePacket Deserialize(CitoMemoryStream stream, Packet_QueuePacket instance)
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
				instance.Position = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_QueuePacket DeserializeLengthDelimited(CitoMemoryStream stream, Packet_QueuePacket instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_QueuePacket result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_QueuePacket instance)
	{
		if (instance.Position != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Position);
		}
	}

	public static int GetSize(Packet_QueuePacket instance)
	{
		int num = 0;
		if (instance.Position != 0)
		{
			num += ProtocolParser.GetSize(instance.Position) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_QueuePacket instance)
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

	public static byte[] SerializeToBytes(Packet_QueuePacket instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_QueuePacket instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
