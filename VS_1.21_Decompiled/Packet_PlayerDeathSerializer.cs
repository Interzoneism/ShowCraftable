using System;

public class Packet_PlayerDeathSerializer
{
	private const int field = 8;

	public static Packet_PlayerDeath DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerDeath packet_PlayerDeath = new Packet_PlayerDeath();
		DeserializeLengthDelimited(stream, packet_PlayerDeath);
		return packet_PlayerDeath;
	}

	public static Packet_PlayerDeath DeserializeBuffer(byte[] buffer, int length, Packet_PlayerDeath instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerDeath Deserialize(CitoMemoryStream stream, Packet_PlayerDeath instance)
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
				instance.ClientId = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.LivesLeft = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_PlayerDeath DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerDeath instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_PlayerDeath result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerDeath instance)
	{
		if (instance.ClientId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.LivesLeft != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.LivesLeft);
		}
	}

	public static int GetSize(Packet_PlayerDeath instance)
	{
		int num = 0;
		if (instance.ClientId != 0)
		{
			num += ProtocolParser.GetSize(instance.ClientId) + 1;
		}
		if (instance.LivesLeft != 0)
		{
			num += ProtocolParser.GetSize(instance.LivesLeft) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_PlayerDeath instance)
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

	public static byte[] SerializeToBytes(Packet_PlayerDeath instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerDeath instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
