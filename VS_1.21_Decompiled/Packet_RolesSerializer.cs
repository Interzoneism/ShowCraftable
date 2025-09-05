using System;

public class Packet_RolesSerializer
{
	private const int field = 8;

	public static Packet_Roles DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Roles packet_Roles = new Packet_Roles();
		DeserializeLengthDelimited(stream, packet_Roles);
		return packet_Roles;
	}

	public static Packet_Roles DeserializeBuffer(byte[] buffer, int length, Packet_Roles instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Roles Deserialize(CitoMemoryStream stream, Packet_Roles instance)
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
				instance.RolesAdd(Packet_RoleSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_Roles DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Roles instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_Roles result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Roles instance)
	{
		if (instance.Roles != null)
		{
			Packet_Role[] roles = instance.Roles;
			int rolesCount = instance.RolesCount;
			for (int i = 0; i < roles.Length && i < rolesCount; i++)
			{
				stream.WriteByte(10);
				Packet_RoleSerializer.SerializeWithSize(stream, roles[i]);
			}
		}
	}

	public static int GetSize(Packet_Roles instance)
	{
		int num = 0;
		if (instance.Roles != null)
		{
			for (int i = 0; i < instance.RolesCount; i++)
			{
				int size = Packet_RoleSerializer.GetSize(instance.Roles[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Roles instance)
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

	public static byte[] SerializeToBytes(Packet_Roles instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Roles instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
