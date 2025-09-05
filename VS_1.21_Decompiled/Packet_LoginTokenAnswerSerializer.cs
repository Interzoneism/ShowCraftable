using System;

public class Packet_LoginTokenAnswerSerializer
{
	private const int field = 8;

	public static Packet_LoginTokenAnswer DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_LoginTokenAnswer packet_LoginTokenAnswer = new Packet_LoginTokenAnswer();
		DeserializeLengthDelimited(stream, packet_LoginTokenAnswer);
		return packet_LoginTokenAnswer;
	}

	public static Packet_LoginTokenAnswer DeserializeBuffer(byte[] buffer, int length, Packet_LoginTokenAnswer instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_LoginTokenAnswer Deserialize(CitoMemoryStream stream, Packet_LoginTokenAnswer instance)
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
				instance.Token = ProtocolParser.ReadString(stream);
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

	public static Packet_LoginTokenAnswer DeserializeLengthDelimited(CitoMemoryStream stream, Packet_LoginTokenAnswer instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_LoginTokenAnswer result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_LoginTokenAnswer instance)
	{
		if (instance.Token != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Token);
		}
	}

	public static int GetSize(Packet_LoginTokenAnswer instance)
	{
		int num = 0;
		if (instance.Token != null)
		{
			num += ProtocolParser.GetSize(instance.Token) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_LoginTokenAnswer instance)
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

	public static byte[] SerializeToBytes(Packet_LoginTokenAnswer instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_LoginTokenAnswer instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
