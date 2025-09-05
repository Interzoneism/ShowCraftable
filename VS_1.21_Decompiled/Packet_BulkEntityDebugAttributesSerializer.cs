using System;

public class Packet_BulkEntityDebugAttributesSerializer
{
	private const int field = 8;

	public static Packet_BulkEntityDebugAttributes DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BulkEntityDebugAttributes packet_BulkEntityDebugAttributes = new Packet_BulkEntityDebugAttributes();
		DeserializeLengthDelimited(stream, packet_BulkEntityDebugAttributes);
		return packet_BulkEntityDebugAttributes;
	}

	public static Packet_BulkEntityDebugAttributes DeserializeBuffer(byte[] buffer, int length, Packet_BulkEntityDebugAttributes instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BulkEntityDebugAttributes Deserialize(CitoMemoryStream stream, Packet_BulkEntityDebugAttributes instance)
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

	public static Packet_BulkEntityDebugAttributes DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BulkEntityDebugAttributes instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_BulkEntityDebugAttributes result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BulkEntityDebugAttributes instance)
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
	}

	public static int GetSize(Packet_BulkEntityDebugAttributes instance)
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
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BulkEntityDebugAttributes instance)
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

	public static byte[] SerializeToBytes(Packet_BulkEntityDebugAttributes instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BulkEntityDebugAttributes instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
