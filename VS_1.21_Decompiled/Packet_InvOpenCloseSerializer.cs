using System;

public class Packet_InvOpenCloseSerializer
{
	private const int field = 8;

	public static Packet_InvOpenClose DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_InvOpenClose packet_InvOpenClose = new Packet_InvOpenClose();
		DeserializeLengthDelimited(stream, packet_InvOpenClose);
		return packet_InvOpenClose;
	}

	public static Packet_InvOpenClose DeserializeBuffer(byte[] buffer, int length, Packet_InvOpenClose instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_InvOpenClose Deserialize(CitoMemoryStream stream, Packet_InvOpenClose instance)
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
				instance.Opened = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_InvOpenClose DeserializeLengthDelimited(CitoMemoryStream stream, Packet_InvOpenClose instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_InvOpenClose result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_InvOpenClose instance)
	{
		if (instance.InventoryId != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.InventoryId);
		}
		if (instance.Opened != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Opened);
		}
	}

	public static int GetSize(Packet_InvOpenClose instance)
	{
		int num = 0;
		if (instance.InventoryId != null)
		{
			num += ProtocolParser.GetSize(instance.InventoryId) + 1;
		}
		if (instance.Opened != 0)
		{
			num += ProtocolParser.GetSize(instance.Opened) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_InvOpenClose instance)
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

	public static byte[] SerializeToBytes(Packet_InvOpenClose instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_InvOpenClose instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
