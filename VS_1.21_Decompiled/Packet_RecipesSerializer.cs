using System;

public class Packet_RecipesSerializer
{
	private const int field = 8;

	public static Packet_Recipes DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Recipes packet_Recipes = new Packet_Recipes();
		DeserializeLengthDelimited(stream, packet_Recipes);
		return packet_Recipes;
	}

	public static Packet_Recipes DeserializeBuffer(byte[] buffer, int length, Packet_Recipes instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Recipes Deserialize(CitoMemoryStream stream, Packet_Recipes instance)
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
				instance.Code = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.Quantity = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
				instance.Data = ProtocolParser.ReadBytes(stream);
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

	public static Packet_Recipes DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Recipes instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_Recipes result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Recipes instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Quantity != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Quantity);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_Recipes instance)
	{
		int num = 0;
		if (instance.Code != null)
		{
			num += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Quantity != 0)
		{
			num += ProtocolParser.GetSize(instance.Quantity) + 1;
		}
		if (instance.Data != null)
		{
			num += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Recipes instance)
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

	public static byte[] SerializeToBytes(Packet_Recipes instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Recipes instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
