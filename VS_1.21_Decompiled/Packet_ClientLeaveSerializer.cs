using System;

public class Packet_ClientLeaveSerializer
{
	private const int field = 8;

	public static Packet_ClientLeave DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientLeave packet_ClientLeave = new Packet_ClientLeave();
		DeserializeLengthDelimited(stream, packet_ClientLeave);
		return packet_ClientLeave;
	}

	public static Packet_ClientLeave DeserializeBuffer(byte[] buffer, int length, Packet_ClientLeave instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientLeave Deserialize(CitoMemoryStream stream, Packet_ClientLeave instance)
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
				instance.Reason = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ClientLeave DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientLeave instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ClientLeave result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ClientLeave instance)
	{
		if (instance.Reason != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Reason);
		}
	}

	public static int GetSize(Packet_ClientLeave instance)
	{
		int num = 0;
		if (instance.Reason != 0)
		{
			num += ProtocolParser.GetSize(instance.Reason) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientLeave instance)
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

	public static byte[] SerializeToBytes(Packet_ClientLeave instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientLeave instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
