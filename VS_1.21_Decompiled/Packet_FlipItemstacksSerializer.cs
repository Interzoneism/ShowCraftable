using System;

public class Packet_FlipItemstacksSerializer
{
	private const int field = 8;

	public static Packet_FlipItemstacks DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_FlipItemstacks packet_FlipItemstacks = new Packet_FlipItemstacks();
		DeserializeLengthDelimited(stream, packet_FlipItemstacks);
		return packet_FlipItemstacks;
	}

	public static Packet_FlipItemstacks DeserializeBuffer(byte[] buffer, int length, Packet_FlipItemstacks instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_FlipItemstacks Deserialize(CitoMemoryStream stream, Packet_FlipItemstacks instance)
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
				instance.SourceInventoryId = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.TargetInventoryId = ProtocolParser.ReadString(stream);
				break;
			case 24:
				instance.SourceSlot = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.TargetSlot = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.SourceLastChanged = ProtocolParser.ReadUInt64(stream);
				break;
			case 48:
				instance.TargetLastChanged = ProtocolParser.ReadUInt64(stream);
				break;
			case 56:
				instance.SourceTabIndex = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.TargetTabIndex = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_FlipItemstacks DeserializeLengthDelimited(CitoMemoryStream stream, Packet_FlipItemstacks instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_FlipItemstacks result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_FlipItemstacks instance)
	{
		if (instance.SourceInventoryId != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.SourceInventoryId);
		}
		if (instance.TargetInventoryId != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.TargetInventoryId);
		}
		if (instance.SourceSlot != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.SourceSlot);
		}
		if (instance.TargetSlot != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.TargetSlot);
		}
		if (instance.SourceLastChanged != 0L)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt64(stream, instance.SourceLastChanged);
		}
		if (instance.TargetLastChanged != 0L)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt64(stream, instance.TargetLastChanged);
		}
		if (instance.SourceTabIndex != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.SourceTabIndex);
		}
		if (instance.TargetTabIndex != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.TargetTabIndex);
		}
	}

	public static int GetSize(Packet_FlipItemstacks instance)
	{
		int num = 0;
		if (instance.SourceInventoryId != null)
		{
			num += ProtocolParser.GetSize(instance.SourceInventoryId) + 1;
		}
		if (instance.TargetInventoryId != null)
		{
			num += ProtocolParser.GetSize(instance.TargetInventoryId) + 1;
		}
		if (instance.SourceSlot != 0)
		{
			num += ProtocolParser.GetSize(instance.SourceSlot) + 1;
		}
		if (instance.TargetSlot != 0)
		{
			num += ProtocolParser.GetSize(instance.TargetSlot) + 1;
		}
		if (instance.SourceLastChanged != 0L)
		{
			num += ProtocolParser.GetSize(instance.SourceLastChanged) + 1;
		}
		if (instance.TargetLastChanged != 0L)
		{
			num += ProtocolParser.GetSize(instance.TargetLastChanged) + 1;
		}
		if (instance.SourceTabIndex != 0)
		{
			num += ProtocolParser.GetSize(instance.SourceTabIndex) + 1;
		}
		if (instance.TargetTabIndex != 0)
		{
			num += ProtocolParser.GetSize(instance.TargetTabIndex) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_FlipItemstacks instance)
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

	public static byte[] SerializeToBytes(Packet_FlipItemstacks instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_FlipItemstacks instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
