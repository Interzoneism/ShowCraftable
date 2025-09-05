using System;

public class Packet_StringListSerializer
{
	private const int field = 8;

	public static Packet_StringList DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_StringList packet_StringList = new Packet_StringList();
		DeserializeLengthDelimited(stream, packet_StringList);
		return packet_StringList;
	}

	public static Packet_StringList DeserializeBuffer(byte[] buffer, int length, Packet_StringList instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_StringList Deserialize(CitoMemoryStream stream, Packet_StringList instance)
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
				instance.ItemsAdd(ProtocolParser.ReadString(stream));
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

	public static Packet_StringList DeserializeLengthDelimited(CitoMemoryStream stream, Packet_StringList instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_StringList result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_StringList instance)
	{
		if (instance.Items != null)
		{
			string[] items = instance.Items;
			int itemsCount = instance.ItemsCount;
			for (int i = 0; i < items.Length && i < itemsCount; i++)
			{
				stream.WriteByte(10);
				ProtocolParser.WriteString(stream, items[i]);
			}
		}
	}

	public static int GetSize(Packet_StringList instance)
	{
		int num = 0;
		if (instance.Items != null)
		{
			for (int i = 0; i < instance.ItemsCount; i++)
			{
				string s = instance.Items[i];
				num += ProtocolParser.GetSize(s) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_StringList instance)
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

	public static byte[] SerializeToBytes(Packet_StringList instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_StringList instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
