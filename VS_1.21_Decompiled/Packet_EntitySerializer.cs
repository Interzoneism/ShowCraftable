using System;

public class Packet_EntitySerializer
{
	private const int field = 8;

	public static Packet_Entity DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Entity packet_Entity = new Packet_Entity();
		DeserializeLengthDelimited(stream, packet_Entity);
		return packet_Entity;
	}

	public static Packet_Entity DeserializeBuffer(byte[] buffer, int length, Packet_Entity instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Entity Deserialize(CitoMemoryStream stream, Packet_Entity instance)
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
				instance.EntityType = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.EntityId = ProtocolParser.ReadUInt64(stream);
				break;
			case 24:
				instance.SimulationRange = ProtocolParser.ReadUInt32(stream);
				break;
			case 34:
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

	public static Packet_Entity DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Entity instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_Entity result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Entity instance)
	{
		if (instance.EntityType != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.EntityType);
		}
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.SimulationRange != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.SimulationRange);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_Entity instance)
	{
		int num = 0;
		if (instance.EntityType != null)
		{
			num += ProtocolParser.GetSize(instance.EntityType) + 1;
		}
		if (instance.EntityId != 0L)
		{
			num += ProtocolParser.GetSize(instance.EntityId) + 1;
		}
		if (instance.SimulationRange != 0)
		{
			num += ProtocolParser.GetSize(instance.SimulationRange) + 1;
		}
		if (instance.Data != null)
		{
			num += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Entity instance)
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

	public static byte[] SerializeToBytes(Packet_Entity instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Entity instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
