using System;

public class Packet_BlockDamageSerializer
{
	private const int field = 8;

	public static Packet_BlockDamage DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockDamage packet_BlockDamage = new Packet_BlockDamage();
		DeserializeLengthDelimited(stream, packet_BlockDamage);
		return packet_BlockDamage;
	}

	public static Packet_BlockDamage DeserializeBuffer(byte[] buffer, int length, Packet_BlockDamage instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockDamage Deserialize(CitoMemoryStream stream, Packet_BlockDamage instance)
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
				instance.Facing = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Damage = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_BlockDamage DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockDamage instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_BlockDamage result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlockDamage instance)
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
		if (instance.Facing != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Facing);
		}
		if (instance.Damage != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Damage);
		}
	}

	public static int GetSize(Packet_BlockDamage instance)
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
		if (instance.Facing != 0)
		{
			num += ProtocolParser.GetSize(instance.Facing) + 1;
		}
		if (instance.Damage != 0)
		{
			num += ProtocolParser.GetSize(instance.Damage) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlockDamage instance)
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

	public static byte[] SerializeToBytes(Packet_BlockDamage instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockDamage instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
