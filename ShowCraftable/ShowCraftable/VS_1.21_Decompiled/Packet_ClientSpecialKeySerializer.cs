using System;

public class Packet_ClientSpecialKeySerializer
{
	private const int field = 8;

	public static Packet_ClientSpecialKey DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientSpecialKey packet_ClientSpecialKey = new Packet_ClientSpecialKey();
		DeserializeLengthDelimited(stream, packet_ClientSpecialKey);
		return packet_ClientSpecialKey;
	}

	public static Packet_ClientSpecialKey DeserializeBuffer(byte[] buffer, int length, Packet_ClientSpecialKey instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientSpecialKey Deserialize(CitoMemoryStream stream, Packet_ClientSpecialKey instance)
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

	public static Packet_ClientSpecialKey DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientSpecialKey instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ClientSpecialKey result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ClientSpecialKey instance)
	{
		if (instance.Key_ != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Key_);
		}
	}

	public static int GetSize(Packet_ClientSpecialKey instance)
	{
		int num = 0;
		if (instance.Key_ != 0)
		{
			num += ProtocolParser.GetSize(instance.Key_) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientSpecialKey instance)
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

	public static byte[] SerializeToBytes(Packet_ClientSpecialKey instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientSpecialKey instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
