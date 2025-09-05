using System;

public class Packet_CreateItemstackSerializer
{
	private const int field = 8;

	public static Packet_CreateItemstack DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CreateItemstack packet_CreateItemstack = new Packet_CreateItemstack();
		DeserializeLengthDelimited(stream, packet_CreateItemstack);
		return packet_CreateItemstack;
	}

	public static Packet_CreateItemstack DeserializeBuffer(byte[] buffer, int length, Packet_CreateItemstack instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CreateItemstack Deserialize(CitoMemoryStream stream, Packet_CreateItemstack instance)
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
				instance.TargetInventoryId = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.TargetSlot = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.TargetLastChanged = ProtocolParser.ReadUInt64(stream);
				break;
			case 34:
				if (instance.Itemstack == null)
				{
					instance.Itemstack = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.Itemstack);
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

	public static Packet_CreateItemstack DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CreateItemstack instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_CreateItemstack result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_CreateItemstack instance)
	{
		if (instance.TargetInventoryId != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.TargetInventoryId);
		}
		if (instance.TargetSlot != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.TargetSlot);
		}
		if (instance.TargetLastChanged != 0L)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt64(stream, instance.TargetLastChanged);
		}
		if (instance.Itemstack != null)
		{
			stream.WriteByte(34);
			Packet_ItemStackSerializer.SerializeWithSize(stream, instance.Itemstack);
		}
	}

	public static int GetSize(Packet_CreateItemstack instance)
	{
		int num = 0;
		if (instance.TargetInventoryId != null)
		{
			num += ProtocolParser.GetSize(instance.TargetInventoryId) + 1;
		}
		if (instance.TargetSlot != 0)
		{
			num += ProtocolParser.GetSize(instance.TargetSlot) + 1;
		}
		if (instance.TargetLastChanged != 0L)
		{
			num += ProtocolParser.GetSize(instance.TargetLastChanged) + 1;
		}
		if (instance.Itemstack != null)
		{
			int size = Packet_ItemStackSerializer.GetSize(instance.Itemstack);
			num += size + ProtocolParser.GetSize(size) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_CreateItemstack instance)
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

	public static byte[] SerializeToBytes(Packet_CreateItemstack instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CreateItemstack instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
