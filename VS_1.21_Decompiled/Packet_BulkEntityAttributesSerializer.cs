using System;

public class Packet_BulkEntityAttributesSerializer
{
	private const int field = 8;

	public static Packet_BulkEntityAttributes DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BulkEntityAttributes packet_BulkEntityAttributes = new Packet_BulkEntityAttributes();
		DeserializeLengthDelimited(stream, packet_BulkEntityAttributes);
		return packet_BulkEntityAttributes;
	}

	public static Packet_BulkEntityAttributes DeserializeBuffer(byte[] buffer, int length, Packet_BulkEntityAttributes instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BulkEntityAttributes Deserialize(CitoMemoryStream stream, Packet_BulkEntityAttributes instance)
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
				instance.FullUpdatesAdd(Packet_EntityAttributesSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 18:
				instance.PartialUpdatesAdd(Packet_EntityAttributeUpdateSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_BulkEntityAttributes DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BulkEntityAttributes instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_BulkEntityAttributes result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BulkEntityAttributes instance)
	{
		if (instance.FullUpdates != null)
		{
			Packet_EntityAttributes[] fullUpdates = instance.FullUpdates;
			int fullUpdatesCount = instance.FullUpdatesCount;
			for (int i = 0; i < fullUpdates.Length && i < fullUpdatesCount; i++)
			{
				stream.WriteByte(10);
				Packet_EntityAttributesSerializer.SerializeWithSize(stream, fullUpdates[i]);
			}
		}
		if (instance.PartialUpdates != null)
		{
			Packet_EntityAttributeUpdate[] partialUpdates = instance.PartialUpdates;
			int partialUpdatesCount = instance.PartialUpdatesCount;
			for (int j = 0; j < partialUpdates.Length && j < partialUpdatesCount; j++)
			{
				stream.WriteByte(18);
				Packet_EntityAttributeUpdateSerializer.SerializeWithSize(stream, partialUpdates[j]);
			}
		}
	}

	public static int GetSize(Packet_BulkEntityAttributes instance)
	{
		int num = 0;
		if (instance.FullUpdates != null)
		{
			for (int i = 0; i < instance.FullUpdatesCount; i++)
			{
				int size = Packet_EntityAttributesSerializer.GetSize(instance.FullUpdates[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		if (instance.PartialUpdates != null)
		{
			for (int j = 0; j < instance.PartialUpdatesCount; j++)
			{
				int size2 = Packet_EntityAttributeUpdateSerializer.GetSize(instance.PartialUpdates[j]);
				num += size2 + ProtocolParser.GetSize(size2) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BulkEntityAttributes instance)
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

	public static byte[] SerializeToBytes(Packet_BulkEntityAttributes instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BulkEntityAttributes instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
