using System;

public class Packet_ServerSetBlocksSerializer
{
	private const int field = 8;

	public static Packet_ServerSetBlocks DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerSetBlocks packet_ServerSetBlocks = new Packet_ServerSetBlocks();
		DeserializeLengthDelimited(stream, packet_ServerSetBlocks);
		return packet_ServerSetBlocks;
	}

	public static Packet_ServerSetBlocks DeserializeBuffer(byte[] buffer, int length, Packet_ServerSetBlocks instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerSetBlocks Deserialize(CitoMemoryStream stream, Packet_ServerSetBlocks instance)
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
				instance.SetBlocks = ProtocolParser.ReadBytes(stream);
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

	public static Packet_ServerSetBlocks DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerSetBlocks instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ServerSetBlocks result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerSetBlocks instance)
	{
		if (instance.SetBlocks != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.SetBlocks);
		}
	}

	public static int GetSize(Packet_ServerSetBlocks instance)
	{
		int num = 0;
		if (instance.SetBlocks != null)
		{
			num += ProtocolParser.GetSize(instance.SetBlocks) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerSetBlocks instance)
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

	public static byte[] SerializeToBytes(Packet_ServerSetBlocks instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerSetBlocks instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
