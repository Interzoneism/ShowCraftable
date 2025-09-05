using System;

public class Packet_ModIdSerializer
{
	private const int field = 8;

	public static Packet_ModId DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ModId packet_ModId = new Packet_ModId();
		DeserializeLengthDelimited(stream, packet_ModId);
		return packet_ModId;
	}

	public static Packet_ModId DeserializeBuffer(byte[] buffer, int length, Packet_ModId instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ModId Deserialize(CitoMemoryStream stream, Packet_ModId instance)
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
				instance.Modid = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.Name = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.Version = ProtocolParser.ReadString(stream);
				break;
			case 34:
				instance.Networkversion = ProtocolParser.ReadString(stream);
				break;
			case 40:
				instance.RequiredOnClient = ProtocolParser.ReadBool(stream);
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

	public static Packet_ModId DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ModId instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ModId result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ModId instance)
	{
		if (instance.Modid != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Modid);
		}
		if (instance.Name != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Name);
		}
		if (instance.Version != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.Version);
		}
		if (instance.Networkversion != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteString(stream, instance.Networkversion);
		}
		if (instance.RequiredOnClient)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteBool(stream, instance.RequiredOnClient);
		}
	}

	public static int GetSize(Packet_ModId instance)
	{
		int num = 0;
		if (instance.Modid != null)
		{
			num += ProtocolParser.GetSize(instance.Modid) + 1;
		}
		if (instance.Name != null)
		{
			num += ProtocolParser.GetSize(instance.Name) + 1;
		}
		if (instance.Version != null)
		{
			num += ProtocolParser.GetSize(instance.Version) + 1;
		}
		if (instance.Networkversion != null)
		{
			num += ProtocolParser.GetSize(instance.Networkversion) + 1;
		}
		if (instance.RequiredOnClient)
		{
			num += 2;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ModId instance)
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

	public static byte[] SerializeToBytes(Packet_ModId instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ModId instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
