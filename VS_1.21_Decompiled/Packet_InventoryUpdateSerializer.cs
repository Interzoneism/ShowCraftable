using System;

public class Packet_InventoryUpdateSerializer
{
	private const int field = 8;

	public static Packet_InventoryUpdate DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_InventoryUpdate packet_InventoryUpdate = new Packet_InventoryUpdate();
		DeserializeLengthDelimited(stream, packet_InventoryUpdate);
		return packet_InventoryUpdate;
	}

	public static Packet_InventoryUpdate DeserializeBuffer(byte[] buffer, int length, Packet_InventoryUpdate instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_InventoryUpdate Deserialize(CitoMemoryStream stream, Packet_InventoryUpdate instance)
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
				instance.InventoryId = ProtocolParser.ReadString(stream);
				break;
			case 24:
				instance.SlotId = ProtocolParser.ReadUInt32(stream);
				break;
			case 34:
				if (instance.ItemStack == null)
				{
					instance.ItemStack = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.ItemStack);
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

	public static Packet_InventoryUpdate DeserializeLengthDelimited(CitoMemoryStream stream, Packet_InventoryUpdate instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_InventoryUpdate result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_InventoryUpdate instance)
	{
		if (instance.ClientId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.InventoryId != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.InventoryId);
		}
		if (instance.SlotId != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.SlotId);
		}
		if (instance.ItemStack != null)
		{
			stream.WriteByte(34);
			Packet_ItemStackSerializer.SerializeWithSize(stream, instance.ItemStack);
		}
	}

	public static int GetSize(Packet_InventoryUpdate instance)
	{
		int num = 0;
		if (instance.ClientId != 0)
		{
			num += ProtocolParser.GetSize(instance.ClientId) + 1;
		}
		if (instance.InventoryId != null)
		{
			num += ProtocolParser.GetSize(instance.InventoryId) + 1;
		}
		if (instance.SlotId != 0)
		{
			num += ProtocolParser.GetSize(instance.SlotId) + 1;
		}
		if (instance.ItemStack != null)
		{
			int size = Packet_ItemStackSerializer.GetSize(instance.ItemStack);
			num += size + ProtocolParser.GetSize(size) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_InventoryUpdate instance)
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

	public static byte[] SerializeToBytes(Packet_InventoryUpdate instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_InventoryUpdate instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
