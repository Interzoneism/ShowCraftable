using System;

public class Packet_BlockEntityMessageSerializer
{
	private const int field = 8;

	public static Packet_BlockEntityMessage DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockEntityMessage packet_BlockEntityMessage = new Packet_BlockEntityMessage();
		DeserializeLengthDelimited(stream, packet_BlockEntityMessage);
		return packet_BlockEntityMessage;
	}

	public static Packet_BlockEntityMessage DeserializeBuffer(byte[] buffer, int length, Packet_BlockEntityMessage instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockEntityMessage Deserialize(CitoMemoryStream stream, Packet_BlockEntityMessage instance)
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
				instance.X = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Y = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Z = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.PacketId = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_BlockEntityMessage DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockEntityMessage instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_BlockEntityMessage result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlockEntityMessage instance)
	{
		if (instance.X != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.X);
		}
		if (instance.Y != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Y);
		}
		if (instance.Z != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Z);
		}
		if (instance.PacketId != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.PacketId);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_BlockEntityMessage instance)
	{
		int num = 0;
		if (instance.X != 0)
		{
			num += ProtocolParser.GetSize(instance.X) + 1;
		}
		if (instance.Y != 0)
		{
			num += ProtocolParser.GetSize(instance.Y) + 1;
		}
		if (instance.Z != 0)
		{
			num += ProtocolParser.GetSize(instance.Z) + 1;
		}
		if (instance.PacketId != 0)
		{
			num += ProtocolParser.GetSize(instance.PacketId) + 1;
		}
		if (instance.Data != null)
		{
			num += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlockEntityMessage instance)
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

	public static byte[] SerializeToBytes(Packet_BlockEntityMessage instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockEntityMessage instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
