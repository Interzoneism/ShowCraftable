using System;

public class Packet_InventoryContentsSerializer
{
	private const int field = 8;

	public static Packet_InventoryContents DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_InventoryContents packet_InventoryContents = new Packet_InventoryContents();
		DeserializeLengthDelimited(stream, packet_InventoryContents);
		return packet_InventoryContents;
	}

	public static Packet_InventoryContents DeserializeBuffer(byte[] buffer, int length, Packet_InventoryContents instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_InventoryContents Deserialize(CitoMemoryStream stream, Packet_InventoryContents instance)
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
				instance.InventoryClass = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.InventoryId = ProtocolParser.ReadString(stream);
				break;
			case 34:
				instance.ItemstacksAdd(Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_InventoryContents DeserializeLengthDelimited(CitoMemoryStream stream, Packet_InventoryContents instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_InventoryContents result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_InventoryContents instance)
	{
		if (instance.ClientId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.InventoryClass != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.InventoryClass);
		}
		if (instance.InventoryId != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.InventoryId);
		}
		if (instance.Itemstacks != null)
		{
			Packet_ItemStack[] itemstacks = instance.Itemstacks;
			int itemstacksCount = instance.ItemstacksCount;
			for (int i = 0; i < itemstacks.Length && i < itemstacksCount; i++)
			{
				stream.WriteByte(34);
				Packet_ItemStackSerializer.SerializeWithSize(stream, itemstacks[i]);
			}
		}
	}

	public static int GetSize(Packet_InventoryContents instance)
	{
		int num = 0;
		if (instance.ClientId != 0)
		{
			num += ProtocolParser.GetSize(instance.ClientId) + 1;
		}
		if (instance.InventoryClass != null)
		{
			num += ProtocolParser.GetSize(instance.InventoryClass) + 1;
		}
		if (instance.InventoryId != null)
		{
			num += ProtocolParser.GetSize(instance.InventoryId) + 1;
		}
		if (instance.Itemstacks != null)
		{
			for (int i = 0; i < instance.ItemstacksCount; i++)
			{
				int size = Packet_ItemStackSerializer.GetSize(instance.Itemstacks[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_InventoryContents instance)
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

	public static byte[] SerializeToBytes(Packet_InventoryContents instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_InventoryContents instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
