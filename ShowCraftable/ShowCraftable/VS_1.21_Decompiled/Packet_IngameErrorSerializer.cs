using System;

public class Packet_IngameErrorSerializer
{
	private const int field = 8;

	public static Packet_IngameError DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_IngameError packet_IngameError = new Packet_IngameError();
		DeserializeLengthDelimited(stream, packet_IngameError);
		return packet_IngameError;
	}

	public static Packet_IngameError DeserializeBuffer(byte[] buffer, int length, Packet_IngameError instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_IngameError Deserialize(CitoMemoryStream stream, Packet_IngameError instance)
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
			case 18:
				instance.Message = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.LangParamsAdd(ProtocolParser.ReadString(stream));
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

	public static Packet_IngameError DeserializeLengthDelimited(CitoMemoryStream stream, Packet_IngameError instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_IngameError result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_IngameError instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Message != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Message);
		}
		if (instance.LangParams != null)
		{
			string[] langParams = instance.LangParams;
			int langParamsCount = instance.LangParamsCount;
			for (int i = 0; i < langParams.Length && i < langParamsCount; i++)
			{
				stream.WriteByte(26);
				ProtocolParser.WriteString(stream, langParams[i]);
			}
		}
	}

	public static int GetSize(Packet_IngameError instance)
	{
		int num = 0;
		if (instance.Code != null)
		{
			num += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Message != null)
		{
			num += ProtocolParser.GetSize(instance.Message) + 1;
		}
		if (instance.LangParams != null)
		{
			for (int i = 0; i < instance.LangParamsCount; i++)
			{
				string s = instance.LangParams[i];
				num += ProtocolParser.GetSize(s) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_IngameError instance)
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

	public static byte[] SerializeToBytes(Packet_IngameError instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_IngameError instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
