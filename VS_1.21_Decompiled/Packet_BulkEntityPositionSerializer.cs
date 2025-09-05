using System;

public class Packet_BulkEntityPositionSerializer
{
	private const int field = 8;

	public static Packet_BulkEntityPosition DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BulkEntityPosition packet_BulkEntityPosition = new Packet_BulkEntityPosition();
		DeserializeLengthDelimited(stream, packet_BulkEntityPosition);
		return packet_BulkEntityPosition;
	}

	public static Packet_BulkEntityPosition DeserializeBuffer(byte[] buffer, int length, Packet_BulkEntityPosition instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BulkEntityPosition Deserialize(CitoMemoryStream stream, Packet_BulkEntityPosition instance)
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
				instance.EntityPositionsAdd(Packet_EntityPositionSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_BulkEntityPosition DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BulkEntityPosition instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_BulkEntityPosition result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BulkEntityPosition instance)
	{
		if (instance.EntityPositions != null)
		{
			Packet_EntityPosition[] entityPositions = instance.EntityPositions;
			int entityPositionsCount = instance.EntityPositionsCount;
			for (int i = 0; i < entityPositions.Length && i < entityPositionsCount; i++)
			{
				stream.WriteByte(10);
				Packet_EntityPositionSerializer.SerializeWithSize(stream, entityPositions[i]);
			}
		}
	}

	public static int GetSize(Packet_BulkEntityPosition instance)
	{
		int num = 0;
		if (instance.EntityPositions != null)
		{
			for (int i = 0; i < instance.EntityPositionsCount; i++)
			{
				int size = Packet_EntityPositionSerializer.GetSize(instance.EntityPositions[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BulkEntityPosition instance)
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

	public static byte[] SerializeToBytes(Packet_BulkEntityPosition instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BulkEntityPosition instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
