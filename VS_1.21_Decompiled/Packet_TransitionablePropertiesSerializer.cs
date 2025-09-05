using System;

public class Packet_TransitionablePropertiesSerializer
{
	private const int field = 8;

	public static Packet_TransitionableProperties DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_TransitionableProperties packet_TransitionableProperties = new Packet_TransitionableProperties();
		DeserializeLengthDelimited(stream, packet_TransitionableProperties);
		return packet_TransitionableProperties;
	}

	public static Packet_TransitionableProperties DeserializeBuffer(byte[] buffer, int length, Packet_TransitionableProperties instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_TransitionableProperties Deserialize(CitoMemoryStream stream, Packet_TransitionableProperties instance)
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
				if (instance.FreshHours == null)
				{
					instance.FreshHours = Packet_NatFloatSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_NatFloatSerializer.DeserializeLengthDelimited(stream, instance.FreshHours);
				}
				break;
			case 18:
				if (instance.TransitionHours == null)
				{
					instance.TransitionHours = Packet_NatFloatSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_NatFloatSerializer.DeserializeLengthDelimited(stream, instance.TransitionHours);
				}
				break;
			case 26:
				instance.TransitionedStack = ProtocolParser.ReadBytes(stream);
				break;
			case 32:
				instance.TransitionRatio = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Type = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_TransitionableProperties DeserializeLengthDelimited(CitoMemoryStream stream, Packet_TransitionableProperties instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_TransitionableProperties result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_TransitionableProperties instance)
	{
		if (instance.FreshHours != null)
		{
			stream.WriteByte(10);
			Packet_NatFloatSerializer.SerializeWithSize(stream, instance.FreshHours);
		}
		if (instance.TransitionHours != null)
		{
			stream.WriteByte(18);
			Packet_NatFloatSerializer.SerializeWithSize(stream, instance.TransitionHours);
		}
		if (instance.TransitionedStack != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.TransitionedStack);
		}
		if (instance.TransitionRatio != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.TransitionRatio);
		}
		if (instance.Type != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Type);
		}
	}

	public static int GetSize(Packet_TransitionableProperties instance)
	{
		int num = 0;
		if (instance.FreshHours != null)
		{
			int size = Packet_NatFloatSerializer.GetSize(instance.FreshHours);
			num += size + ProtocolParser.GetSize(size) + 1;
		}
		if (instance.TransitionHours != null)
		{
			int size2 = Packet_NatFloatSerializer.GetSize(instance.TransitionHours);
			num += size2 + ProtocolParser.GetSize(size2) + 1;
		}
		if (instance.TransitionedStack != null)
		{
			num += ProtocolParser.GetSize(instance.TransitionedStack) + 1;
		}
		if (instance.TransitionRatio != 0)
		{
			num += ProtocolParser.GetSize(instance.TransitionRatio) + 1;
		}
		if (instance.Type != 0)
		{
			num += ProtocolParser.GetSize(instance.Type) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_TransitionableProperties instance)
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

	public static byte[] SerializeToBytes(Packet_TransitionableProperties instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_TransitionableProperties instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
