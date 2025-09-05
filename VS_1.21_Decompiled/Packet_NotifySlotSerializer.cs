using System;

public class Packet_NotifySlotSerializer
{
	private const int field = 8;

	public static Packet_NotifySlot DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_NotifySlot packet_NotifySlot = new Packet_NotifySlot();
		DeserializeLengthDelimited(stream, packet_NotifySlot);
		return packet_NotifySlot;
	}

	public static Packet_NotifySlot DeserializeBuffer(byte[] buffer, int length, Packet_NotifySlot instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_NotifySlot Deserialize(CitoMemoryStream stream, Packet_NotifySlot instance)
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
				instance.InventoryId = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.SlotId = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_NotifySlot DeserializeLengthDelimited(CitoMemoryStream stream, Packet_NotifySlot instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_NotifySlot result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_NotifySlot instance)
	{
		if (instance.InventoryId != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.InventoryId);
		}
		if (instance.SlotId != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.SlotId);
		}
	}

	public static int GetSize(Packet_NotifySlot instance)
	{
		int num = 0;
		if (instance.InventoryId != null)
		{
			num += ProtocolParser.GetSize(instance.InventoryId) + 1;
		}
		if (instance.SlotId != 0)
		{
			num += ProtocolParser.GetSize(instance.SlotId) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_NotifySlot instance)
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

	public static byte[] SerializeToBytes(Packet_NotifySlot instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_NotifySlot instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
