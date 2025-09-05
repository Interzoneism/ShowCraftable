using System;

public class Packet_BehaviorSerializer
{
	private const int field = 8;

	public static Packet_Behavior DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Behavior packet_Behavior = new Packet_Behavior();
		DeserializeLengthDelimited(stream, packet_Behavior);
		return packet_Behavior;
	}

	public static Packet_Behavior DeserializeBuffer(byte[] buffer, int length, Packet_Behavior instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Behavior Deserialize(CitoMemoryStream stream, Packet_Behavior instance)
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
			case 18:
				instance.Attributes = ProtocolParser.ReadString(stream);
				break;
			case 24:
				instance.ClientSideOptional = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_Behavior DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Behavior instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_Behavior result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Behavior instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Attributes != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Attributes);
		}
		if (instance.ClientSideOptional != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.ClientSideOptional);
		}
	}

	public static int GetSize(Packet_Behavior instance)
	{
		int num = 0;
		if (instance.Code != null)
		{
			num += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Attributes != null)
		{
			num += ProtocolParser.GetSize(instance.Attributes) + 1;
		}
		if (instance.ClientSideOptional != 0)
		{
			num += ProtocolParser.GetSize(instance.ClientSideOptional) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Behavior instance)
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

	public static byte[] SerializeToBytes(Packet_Behavior instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Behavior instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
