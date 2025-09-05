using System;

public class Packet_HighlightBlocksSerializer
{
	private const int field = 8;

	public static Packet_HighlightBlocks DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_HighlightBlocks packet_HighlightBlocks = new Packet_HighlightBlocks();
		DeserializeLengthDelimited(stream, packet_HighlightBlocks);
		return packet_HighlightBlocks;
	}

	public static Packet_HighlightBlocks DeserializeBuffer(byte[] buffer, int length, Packet_HighlightBlocks instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_HighlightBlocks Deserialize(CitoMemoryStream stream, Packet_HighlightBlocks instance)
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
				instance.Mode = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Shape = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
				instance.Blocks = ProtocolParser.ReadBytes(stream);
				break;
			case 32:
				instance.ColorsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 40:
				instance.Slotid = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Scale = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_HighlightBlocks DeserializeLengthDelimited(CitoMemoryStream stream, Packet_HighlightBlocks instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_HighlightBlocks result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_HighlightBlocks instance)
	{
		if (instance.Mode != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Mode);
		}
		if (instance.Shape != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Shape);
		}
		if (instance.Blocks != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.Blocks);
		}
		if (instance.Colors != null)
		{
			int[] colors = instance.Colors;
			int colorsCount = instance.ColorsCount;
			for (int i = 0; i < colors.Length && i < colorsCount; i++)
			{
				stream.WriteByte(32);
				ProtocolParser.WriteUInt32(stream, colors[i]);
			}
		}
		if (instance.Slotid != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Slotid);
		}
		if (instance.Scale != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Scale);
		}
	}

	public static int GetSize(Packet_HighlightBlocks instance)
	{
		int num = 0;
		if (instance.Mode != 0)
		{
			num += ProtocolParser.GetSize(instance.Mode) + 1;
		}
		if (instance.Shape != 0)
		{
			num += ProtocolParser.GetSize(instance.Shape) + 1;
		}
		if (instance.Blocks != null)
		{
			num += ProtocolParser.GetSize(instance.Blocks) + 1;
		}
		if (instance.Colors != null)
		{
			for (int i = 0; i < instance.ColorsCount; i++)
			{
				int v = instance.Colors[i];
				num += ProtocolParser.GetSize(v) + 1;
			}
		}
		if (instance.Slotid != 0)
		{
			num += ProtocolParser.GetSize(instance.Slotid) + 1;
		}
		if (instance.Scale != 0)
		{
			num += ProtocolParser.GetSize(instance.Scale) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_HighlightBlocks instance)
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

	public static byte[] SerializeToBytes(Packet_HighlightBlocks instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_HighlightBlocks instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
