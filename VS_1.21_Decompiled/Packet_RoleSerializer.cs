using System;

public class Packet_RoleSerializer
{
	private const int field = 8;

	public static Packet_Role DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Role packet_Role = new Packet_Role();
		DeserializeLengthDelimited(stream, packet_Role);
		return packet_Role;
	}

	public static Packet_Role DeserializeBuffer(byte[] buffer, int length, Packet_Role instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Role Deserialize(CitoMemoryStream stream, Packet_Role instance)
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
				instance.PrivilegeLevel = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_Role DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Role instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_Role result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Role instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.PrivilegeLevel != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.PrivilegeLevel);
		}
	}

	public static int GetSize(Packet_Role instance)
	{
		int num = 0;
		if (instance.Code != null)
		{
			num += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.PrivilegeLevel != 0)
		{
			num += ProtocolParser.GetSize(instance.PrivilegeLevel) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Role instance)
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

	public static byte[] SerializeToBytes(Packet_Role instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Role instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
