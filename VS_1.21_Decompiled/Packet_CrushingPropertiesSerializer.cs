using System;

public class Packet_CrushingPropertiesSerializer
{
	private const int field = 8;

	public static Packet_CrushingProperties DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CrushingProperties packet_CrushingProperties = new Packet_CrushingProperties();
		DeserializeLengthDelimited(stream, packet_CrushingProperties);
		return packet_CrushingProperties;
	}

	public static Packet_CrushingProperties DeserializeBuffer(byte[] buffer, int length, Packet_CrushingProperties instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CrushingProperties Deserialize(CitoMemoryStream stream, Packet_CrushingProperties instance)
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
				instance.CrushedStack = ProtocolParser.ReadBytes(stream);
				break;
			case 16:
				instance.HardnessTier = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
				if (instance.Quantity == null)
				{
					instance.Quantity = Packet_NatFloatSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_NatFloatSerializer.DeserializeLengthDelimited(stream, instance.Quantity);
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

	public static Packet_CrushingProperties DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CrushingProperties instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_CrushingProperties result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_CrushingProperties instance)
	{
		if (instance.CrushedStack != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.CrushedStack);
		}
		if (instance.HardnessTier != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.HardnessTier);
		}
		if (instance.Quantity != null)
		{
			stream.WriteByte(26);
			Packet_NatFloatSerializer.SerializeWithSize(stream, instance.Quantity);
		}
	}

	public static int GetSize(Packet_CrushingProperties instance)
	{
		int num = 0;
		if (instance.CrushedStack != null)
		{
			num += ProtocolParser.GetSize(instance.CrushedStack) + 1;
		}
		if (instance.HardnessTier != 0)
		{
			num += ProtocolParser.GetSize(instance.HardnessTier) + 1;
		}
		if (instance.Quantity != null)
		{
			int size = Packet_NatFloatSerializer.GetSize(instance.Quantity);
			num += size + ProtocolParser.GetSize(size) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_CrushingProperties instance)
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

	public static byte[] SerializeToBytes(Packet_CrushingProperties instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CrushingProperties instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
