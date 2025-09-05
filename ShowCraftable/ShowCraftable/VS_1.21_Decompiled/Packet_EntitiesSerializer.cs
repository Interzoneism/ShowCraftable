using System;

public class Packet_EntitiesSerializer
{
	private const int field = 8;

	public static Packet_Entities DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Entities packet_Entities = new Packet_Entities();
		DeserializeLengthDelimited(stream, packet_Entities);
		return packet_Entities;
	}

	public static Packet_Entities DeserializeBuffer(byte[] buffer, int length, Packet_Entities instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Entities Deserialize(CitoMemoryStream stream, Packet_Entities instance)
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
				instance.EntitiesAdd(Packet_EntitySerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_Entities DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Entities instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_Entities result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Entities instance)
	{
		if (instance.Entities != null)
		{
			Packet_Entity[] entities = instance.Entities;
			int entitiesCount = instance.EntitiesCount;
			for (int i = 0; i < entities.Length && i < entitiesCount; i++)
			{
				stream.WriteByte(10);
				Packet_EntitySerializer.SerializeWithSize(stream, entities[i]);
			}
		}
	}

	public static int GetSize(Packet_Entities instance)
	{
		int num = 0;
		if (instance.Entities != null)
		{
			for (int i = 0; i < instance.EntitiesCount; i++)
			{
				int size = Packet_EntitySerializer.GetSize(instance.Entities[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Entities instance)
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

	public static byte[] SerializeToBytes(Packet_Entities instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Entities instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
