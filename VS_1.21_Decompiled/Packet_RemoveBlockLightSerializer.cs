using System;

public class Packet_RemoveBlockLightSerializer
{
	private const int field = 8;

	public static Packet_RemoveBlockLight DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_RemoveBlockLight packet_RemoveBlockLight = new Packet_RemoveBlockLight();
		DeserializeLengthDelimited(stream, packet_RemoveBlockLight);
		return packet_RemoveBlockLight;
	}

	public static Packet_RemoveBlockLight DeserializeBuffer(byte[] buffer, int length, Packet_RemoveBlockLight instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_RemoveBlockLight Deserialize(CitoMemoryStream stream, Packet_RemoveBlockLight instance)
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
				instance.PosX = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.PosY = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.PosZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.LightH = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.LightS = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.LightV = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_RemoveBlockLight DeserializeLengthDelimited(CitoMemoryStream stream, Packet_RemoveBlockLight instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_RemoveBlockLight result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_RemoveBlockLight instance)
	{
		if (instance.PosX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.PosX);
		}
		if (instance.PosY != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.PosY);
		}
		if (instance.PosZ != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.PosZ);
		}
		if (instance.LightH != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.LightH);
		}
		if (instance.LightS != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.LightS);
		}
		if (instance.LightV != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.LightV);
		}
	}

	public static int GetSize(Packet_RemoveBlockLight instance)
	{
		int num = 0;
		if (instance.PosX != 0)
		{
			num += ProtocolParser.GetSize(instance.PosX) + 1;
		}
		if (instance.PosY != 0)
		{
			num += ProtocolParser.GetSize(instance.PosY) + 1;
		}
		if (instance.PosZ != 0)
		{
			num += ProtocolParser.GetSize(instance.PosZ) + 1;
		}
		if (instance.LightH != 0)
		{
			num += ProtocolParser.GetSize(instance.LightH) + 1;
		}
		if (instance.LightS != 0)
		{
			num += ProtocolParser.GetSize(instance.LightS) + 1;
		}
		if (instance.LightV != 0)
		{
			num += ProtocolParser.GetSize(instance.LightV) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_RemoveBlockLight instance)
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

	public static byte[] SerializeToBytes(Packet_RemoveBlockLight instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_RemoveBlockLight instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
