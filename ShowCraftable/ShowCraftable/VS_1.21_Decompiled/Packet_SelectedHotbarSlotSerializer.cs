using System;

public class Packet_SelectedHotbarSlotSerializer
{
	private const int field = 8;

	public static Packet_SelectedHotbarSlot DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_SelectedHotbarSlot packet_SelectedHotbarSlot = new Packet_SelectedHotbarSlot();
		DeserializeLengthDelimited(stream, packet_SelectedHotbarSlot);
		return packet_SelectedHotbarSlot;
	}

	public static Packet_SelectedHotbarSlot DeserializeBuffer(byte[] buffer, int length, Packet_SelectedHotbarSlot instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_SelectedHotbarSlot Deserialize(CitoMemoryStream stream, Packet_SelectedHotbarSlot instance)
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
				instance.SlotNumber = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.ClientId = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
				if (instance.Itemstack == null)
				{
					instance.Itemstack = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.Itemstack);
				}
				break;
			case 34:
				if (instance.OffhandStack == null)
				{
					instance.OffhandStack = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.OffhandStack);
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

	public static Packet_SelectedHotbarSlot DeserializeLengthDelimited(CitoMemoryStream stream, Packet_SelectedHotbarSlot instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_SelectedHotbarSlot result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_SelectedHotbarSlot instance)
	{
		if (instance.SlotNumber != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.SlotNumber);
		}
		if (instance.ClientId != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.Itemstack != null)
		{
			stream.WriteByte(26);
			Packet_ItemStackSerializer.SerializeWithSize(stream, instance.Itemstack);
		}
		if (instance.OffhandStack != null)
		{
			stream.WriteByte(34);
			Packet_ItemStackSerializer.SerializeWithSize(stream, instance.OffhandStack);
		}
	}

	public static int GetSize(Packet_SelectedHotbarSlot instance)
	{
		int num = 0;
		if (instance.SlotNumber != 0)
		{
			num += ProtocolParser.GetSize(instance.SlotNumber) + 1;
		}
		if (instance.ClientId != 0)
		{
			num += ProtocolParser.GetSize(instance.ClientId) + 1;
		}
		if (instance.Itemstack != null)
		{
			int size = Packet_ItemStackSerializer.GetSize(instance.Itemstack);
			num += size + ProtocolParser.GetSize(size) + 1;
		}
		if (instance.OffhandStack != null)
		{
			int size2 = Packet_ItemStackSerializer.GetSize(instance.OffhandStack);
			num += size2 + ProtocolParser.GetSize(size2) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_SelectedHotbarSlot instance)
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

	public static byte[] SerializeToBytes(Packet_SelectedHotbarSlot instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_SelectedHotbarSlot instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
