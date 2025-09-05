using System;

public class Packet_InventoryDoubleUpdateSerializer
{
	private const int field = 8;

	public static Packet_InventoryDoubleUpdate DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_InventoryDoubleUpdate packet_InventoryDoubleUpdate = new Packet_InventoryDoubleUpdate();
		DeserializeLengthDelimited(stream, packet_InventoryDoubleUpdate);
		return packet_InventoryDoubleUpdate;
	}

	public static Packet_InventoryDoubleUpdate DeserializeBuffer(byte[] buffer, int length, Packet_InventoryDoubleUpdate instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_InventoryDoubleUpdate Deserialize(CitoMemoryStream stream, Packet_InventoryDoubleUpdate instance)
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
			case 18:
				instance.InventoryId1 = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.InventoryId2 = ProtocolParser.ReadString(stream);
				break;
			case 32:
				instance.SlotId1 = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.SlotId2 = ProtocolParser.ReadUInt32(stream);
				break;
			case 50:
				if (instance.ItemStack1 == null)
				{
					instance.ItemStack1 = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.ItemStack1);
				}
				break;
			case 58:
				if (instance.ItemStack2 == null)
				{
					instance.ItemStack2 = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.ItemStack2);
				}
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

	public static Packet_InventoryDoubleUpdate DeserializeLengthDelimited(CitoMemoryStream stream, Packet_InventoryDoubleUpdate instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_InventoryDoubleUpdate result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_InventoryDoubleUpdate instance)
	{
		if (instance.ClientId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.InventoryId1 != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.InventoryId1);
		}
		if (instance.InventoryId2 != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.InventoryId2);
		}
		if (instance.SlotId1 != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.SlotId1);
		}
		if (instance.SlotId2 != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.SlotId2);
		}
		if (instance.ItemStack1 != null)
		{
			stream.WriteByte(50);
			Packet_ItemStackSerializer.SerializeWithSize(stream, instance.ItemStack1);
		}
		if (instance.ItemStack2 != null)
		{
			stream.WriteByte(58);
			Packet_ItemStackSerializer.SerializeWithSize(stream, instance.ItemStack2);
		}
	}

	public static int GetSize(Packet_InventoryDoubleUpdate instance)
	{
		int num = 0;
		if (instance.ClientId != 0)
		{
			num += ProtocolParser.GetSize(instance.ClientId) + 1;
		}
		if (instance.InventoryId1 != null)
		{
			num += ProtocolParser.GetSize(instance.InventoryId1) + 1;
		}
		if (instance.InventoryId2 != null)
		{
			num += ProtocolParser.GetSize(instance.InventoryId2) + 1;
		}
		if (instance.SlotId1 != 0)
		{
			num += ProtocolParser.GetSize(instance.SlotId1) + 1;
		}
		if (instance.SlotId2 != 0)
		{
			num += ProtocolParser.GetSize(instance.SlotId2) + 1;
		}
		if (instance.ItemStack1 != null)
		{
			int size = Packet_ItemStackSerializer.GetSize(instance.ItemStack1);
			num += size + ProtocolParser.GetSize(size) + 1;
		}
		if (instance.ItemStack2 != null)
		{
			int size2 = Packet_ItemStackSerializer.GetSize(instance.ItemStack2);
			num += size2 + ProtocolParser.GetSize(size2) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_InventoryDoubleUpdate instance)
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

	public static byte[] SerializeToBytes(Packet_InventoryDoubleUpdate instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_InventoryDoubleUpdate instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
