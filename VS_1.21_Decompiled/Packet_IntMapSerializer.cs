using System;

public class Packet_IntMapSerializer
{
	private const int field = 8;

	public static Packet_IntMap DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_IntMap packet_IntMap = new Packet_IntMap();
		DeserializeLengthDelimited(stream, packet_IntMap);
		return packet_IntMap;
	}

	public static Packet_IntMap DeserializeBuffer(byte[] buffer, int length, Packet_IntMap instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_IntMap Deserialize(CitoMemoryStream stream, Packet_IntMap instance)
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
				instance.DataAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 16:
				instance.Size = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.TopLeftPadding = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.BottomRightPadding = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_IntMap DeserializeLengthDelimited(CitoMemoryStream stream, Packet_IntMap instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_IntMap result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_IntMap instance)
	{
		if (instance.Data != null)
		{
			int[] data = instance.Data;
			int dataCount = instance.DataCount;
			for (int i = 0; i < data.Length && i < dataCount; i++)
			{
				stream.WriteByte(8);
				ProtocolParser.WriteUInt32(stream, data[i]);
			}
		}
		if (instance.Size != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Size);
		}
		if (instance.TopLeftPadding != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.TopLeftPadding);
		}
		if (instance.BottomRightPadding != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.BottomRightPadding);
		}
	}

	public static int GetSize(Packet_IntMap instance)
	{
		int num = 0;
		if (instance.Data != null)
		{
			for (int i = 0; i < instance.DataCount; i++)
			{
				int v = instance.Data[i];
				num += ProtocolParser.GetSize(v) + 1;
			}
		}
		if (instance.Size != 0)
		{
			num += ProtocolParser.GetSize(instance.Size) + 1;
		}
		if (instance.TopLeftPadding != 0)
		{
			num += ProtocolParser.GetSize(instance.TopLeftPadding) + 1;
		}
		if (instance.BottomRightPadding != 0)
		{
			num += ProtocolParser.GetSize(instance.BottomRightPadding) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_IntMap instance)
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

	public static byte[] SerializeToBytes(Packet_IntMap instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_IntMap instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
