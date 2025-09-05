using System;

public class Packet_NutritionPropertiesSerializer
{
	private const int field = 8;

	public static Packet_NutritionProperties DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_NutritionProperties packet_NutritionProperties = new Packet_NutritionProperties();
		DeserializeLengthDelimited(stream, packet_NutritionProperties);
		return packet_NutritionProperties;
	}

	public static Packet_NutritionProperties DeserializeBuffer(byte[] buffer, int length, Packet_NutritionProperties instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_NutritionProperties Deserialize(CitoMemoryStream stream, Packet_NutritionProperties instance)
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
				instance.FoodCategory = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Saturation = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Health = ProtocolParser.ReadUInt32(stream);
				break;
			case 34:
				instance.EatenStack = ProtocolParser.ReadBytes(stream);
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

	public static Packet_NutritionProperties DeserializeLengthDelimited(CitoMemoryStream stream, Packet_NutritionProperties instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_NutritionProperties result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_NutritionProperties instance)
	{
		if (instance.FoodCategory != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.FoodCategory);
		}
		if (instance.Saturation != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Saturation);
		}
		if (instance.Health != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Health);
		}
		if (instance.EatenStack != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, instance.EatenStack);
		}
	}

	public static int GetSize(Packet_NutritionProperties instance)
	{
		int num = 0;
		if (instance.FoodCategory != 0)
		{
			num += ProtocolParser.GetSize(instance.FoodCategory) + 1;
		}
		if (instance.Saturation != 0)
		{
			num += ProtocolParser.GetSize(instance.Saturation) + 1;
		}
		if (instance.Health != 0)
		{
			num += ProtocolParser.GetSize(instance.Health) + 1;
		}
		if (instance.EatenStack != null)
		{
			num += ProtocolParser.GetSize(instance.EatenStack) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_NutritionProperties instance)
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

	public static byte[] SerializeToBytes(Packet_NutritionProperties instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_NutritionProperties instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
