using System;

public class Packet_CombustiblePropertiesSerializer
{
	private const int field = 8;

	public static Packet_CombustibleProperties DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CombustibleProperties packet_CombustibleProperties = new Packet_CombustibleProperties();
		DeserializeLengthDelimited(stream, packet_CombustibleProperties);
		return packet_CombustibleProperties;
	}

	public static Packet_CombustibleProperties DeserializeBuffer(byte[] buffer, int length, Packet_CombustibleProperties instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CombustibleProperties Deserialize(CitoMemoryStream stream, Packet_CombustibleProperties instance)
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
				instance.BurnTemperature = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.BurnDuration = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.HeatResistance = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.MeltingPoint = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.MeltingDuration = ProtocolParser.ReadUInt32(stream);
				break;
			case 50:
				instance.SmeltedStack = ProtocolParser.ReadBytes(stream);
				break;
			case 56:
				instance.SmeltedRatio = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.RequiresContainer = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.MeltingType = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.MaxTemperature = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_CombustibleProperties DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CombustibleProperties instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_CombustibleProperties result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_CombustibleProperties instance)
	{
		if (instance.BurnTemperature != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.BurnTemperature);
		}
		if (instance.BurnDuration != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.BurnDuration);
		}
		if (instance.HeatResistance != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.HeatResistance);
		}
		if (instance.MeltingPoint != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.MeltingPoint);
		}
		if (instance.MeltingDuration != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.MeltingDuration);
		}
		if (instance.SmeltedStack != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteBytes(stream, instance.SmeltedStack);
		}
		if (instance.SmeltedRatio != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.SmeltedRatio);
		}
		if (instance.RequiresContainer != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.RequiresContainer);
		}
		if (instance.MeltingType != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.MeltingType);
		}
		if (instance.MaxTemperature != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.MaxTemperature);
		}
	}

	public static int GetSize(Packet_CombustibleProperties instance)
	{
		int num = 0;
		if (instance.BurnTemperature != 0)
		{
			num += ProtocolParser.GetSize(instance.BurnTemperature) + 1;
		}
		if (instance.BurnDuration != 0)
		{
			num += ProtocolParser.GetSize(instance.BurnDuration) + 1;
		}
		if (instance.HeatResistance != 0)
		{
			num += ProtocolParser.GetSize(instance.HeatResistance) + 1;
		}
		if (instance.MeltingPoint != 0)
		{
			num += ProtocolParser.GetSize(instance.MeltingPoint) + 1;
		}
		if (instance.MeltingDuration != 0)
		{
			num += ProtocolParser.GetSize(instance.MeltingDuration) + 1;
		}
		if (instance.SmeltedStack != null)
		{
			num += ProtocolParser.GetSize(instance.SmeltedStack) + 1;
		}
		if (instance.SmeltedRatio != 0)
		{
			num += ProtocolParser.GetSize(instance.SmeltedRatio) + 1;
		}
		if (instance.RequiresContainer != 0)
		{
			num += ProtocolParser.GetSize(instance.RequiresContainer) + 1;
		}
		if (instance.MeltingType != 0)
		{
			num += ProtocolParser.GetSize(instance.MeltingType) + 1;
		}
		if (instance.MaxTemperature != 0)
		{
			num += ProtocolParser.GetSize(instance.MaxTemperature) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_CombustibleProperties instance)
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

	public static byte[] SerializeToBytes(Packet_CombustibleProperties instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CombustibleProperties instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
