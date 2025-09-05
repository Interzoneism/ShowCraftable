using System;

public class Packet_BlockEntitySerializer
{
	private const int field = 8;

	public static Packet_BlockEntity DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockEntity packet_BlockEntity = new Packet_BlockEntity();
		DeserializeLengthDelimited(stream, packet_BlockEntity);
		return packet_BlockEntity;
	}

	public static Packet_BlockEntity DeserializeBuffer(byte[] buffer, int length, Packet_BlockEntity instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockEntity Deserialize(CitoMemoryStream stream, Packet_BlockEntity instance)
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
				instance.Classname = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.PosX = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.PosY = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.PosZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 42:
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

	public static Packet_BlockEntity DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockEntity instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_BlockEntity result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlockEntity instance)
	{
		if (instance.Classname != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Classname);
		}
		if (instance.PosX != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.PosX);
		}
		if (instance.PosY != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.PosY);
		}
		if (instance.PosZ != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.PosZ);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_BlockEntity instance)
	{
		int num = 0;
		if (instance.Classname != null)
		{
			num += ProtocolParser.GetSize(instance.Classname) + 1;
		}
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
		if (instance.Data != null)
		{
			num += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlockEntity instance)
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

	public static byte[] SerializeToBytes(Packet_BlockEntity instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockEntity instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
