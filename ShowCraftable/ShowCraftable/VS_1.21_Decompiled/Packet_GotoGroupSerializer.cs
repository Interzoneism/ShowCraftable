using System;

public class Packet_GotoGroupSerializer
{
	private const int field = 8;

	public static Packet_GotoGroup DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_GotoGroup packet_GotoGroup = new Packet_GotoGroup();
		DeserializeLengthDelimited(stream, packet_GotoGroup);
		return packet_GotoGroup;
	}

	public static Packet_GotoGroup DeserializeBuffer(byte[] buffer, int length, Packet_GotoGroup instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_GotoGroup Deserialize(CitoMemoryStream stream, Packet_GotoGroup instance)
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
				instance.GroupId = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_GotoGroup DeserializeLengthDelimited(CitoMemoryStream stream, Packet_GotoGroup instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_GotoGroup result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_GotoGroup instance)
	{
		if (instance.GroupId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.GroupId);
		}
	}

	public static int GetSize(Packet_GotoGroup instance)
	{
		int num = 0;
		if (instance.GroupId != 0)
		{
			num += ProtocolParser.GetSize(instance.GroupId) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_GotoGroup instance)
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

	public static byte[] SerializeToBytes(Packet_GotoGroup instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_GotoGroup instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
