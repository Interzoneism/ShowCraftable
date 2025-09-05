using System;

public class Packet_BlockDropSerializer
{
	private const int field = 8;

	public static Packet_BlockDrop DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockDrop packet_BlockDrop = new Packet_BlockDrop();
		DeserializeLengthDelimited(stream, packet_BlockDrop);
		return packet_BlockDrop;
	}

	public static Packet_BlockDrop DeserializeBuffer(byte[] buffer, int length, Packet_BlockDrop instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockDrop Deserialize(CitoMemoryStream stream, Packet_BlockDrop instance)
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
				instance.QuantityAvg = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.QuantityVar = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.QuantityDist = ProtocolParser.ReadUInt32(stream);
				break;
			case 34:
				instance.DroppedStack = ProtocolParser.ReadBytes(stream);
				break;
			case 40:
				instance.Tool = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_BlockDrop DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockDrop instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_BlockDrop result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlockDrop instance)
	{
		if (instance.QuantityAvg != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.QuantityAvg);
		}
		if (instance.QuantityVar != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.QuantityVar);
		}
		if (instance.QuantityDist != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.QuantityDist);
		}
		if (instance.DroppedStack != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, instance.DroppedStack);
		}
		if (instance.Tool != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Tool);
		}
	}

	public static int GetSize(Packet_BlockDrop instance)
	{
		int num = 0;
		if (instance.QuantityAvg != 0)
		{
			num += ProtocolParser.GetSize(instance.QuantityAvg) + 1;
		}
		if (instance.QuantityVar != 0)
		{
			num += ProtocolParser.GetSize(instance.QuantityVar) + 1;
		}
		if (instance.QuantityDist != 0)
		{
			num += ProtocolParser.GetSize(instance.QuantityDist) + 1;
		}
		if (instance.DroppedStack != null)
		{
			num += ProtocolParser.GetSize(instance.DroppedStack) + 1;
		}
		if (instance.Tool != 0)
		{
			num += ProtocolParser.GetSize(instance.Tool) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlockDrop instance)
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

	public static byte[] SerializeToBytes(Packet_BlockDrop instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockDrop instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
