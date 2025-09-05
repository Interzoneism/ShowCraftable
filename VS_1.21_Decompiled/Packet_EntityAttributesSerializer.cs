using System;

public class Packet_EntityAttributesSerializer
{
	private const int field = 8;

	public static Packet_EntityAttributes DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityAttributes packet_EntityAttributes = new Packet_EntityAttributes();
		DeserializeLengthDelimited(stream, packet_EntityAttributes);
		return packet_EntityAttributes;
	}

	public static Packet_EntityAttributes DeserializeBuffer(byte[] buffer, int length, Packet_EntityAttributes instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityAttributes Deserialize(CitoMemoryStream stream, Packet_EntityAttributes instance)
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
				instance.EntityId = ProtocolParser.ReadUInt64(stream);
				break;
			case 18:
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

	public static Packet_EntityAttributes DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityAttributes instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_EntityAttributes result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_EntityAttributes instance)
	{
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_EntityAttributes instance)
	{
		int num = 0;
		if (instance.EntityId != 0L)
		{
			num += ProtocolParser.GetSize(instance.EntityId) + 1;
		}
		if (instance.Data != null)
		{
			num += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityAttributes instance)
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

	public static byte[] SerializeToBytes(Packet_EntityAttributes instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityAttributes instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
