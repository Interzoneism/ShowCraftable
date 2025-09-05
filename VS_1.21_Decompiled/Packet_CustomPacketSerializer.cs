using System;

public class Packet_CustomPacketSerializer
{
	private const int field = 8;

	public static Packet_CustomPacket DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CustomPacket packet_CustomPacket = new Packet_CustomPacket();
		DeserializeLengthDelimited(stream, packet_CustomPacket);
		return packet_CustomPacket;
	}

	public static Packet_CustomPacket DeserializeBuffer(byte[] buffer, int length, Packet_CustomPacket instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CustomPacket Deserialize(CitoMemoryStream stream, Packet_CustomPacket instance)
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
				instance.ChannelId = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.MessageId = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
				instance.Data = ProtocolParser.ReadBytes(stream);
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

	public static Packet_CustomPacket DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CustomPacket instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_CustomPacket result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_CustomPacket instance)
	{
		if (instance.ChannelId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ChannelId);
		}
		if (instance.MessageId != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.MessageId);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_CustomPacket instance)
	{
		int num = 0;
		if (instance.ChannelId != 0)
		{
			num += ProtocolParser.GetSize(instance.ChannelId) + 1;
		}
		if (instance.MessageId != 0)
		{
			num += ProtocolParser.GetSize(instance.MessageId) + 1;
		}
		if (instance.Data != null)
		{
			num += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_CustomPacket instance)
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

	public static byte[] SerializeToBytes(Packet_CustomPacket instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CustomPacket instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
