using System;

public class Packet_ServerLevelProgressSerializer
{
	private const int field = 8;

	public static Packet_ServerLevelProgress DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerLevelProgress packet_ServerLevelProgress = new Packet_ServerLevelProgress();
		DeserializeLengthDelimited(stream, packet_ServerLevelProgress);
		return packet_ServerLevelProgress;
	}

	public static Packet_ServerLevelProgress DeserializeBuffer(byte[] buffer, int length, Packet_ServerLevelProgress instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerLevelProgress Deserialize(CitoMemoryStream stream, Packet_ServerLevelProgress instance)
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
			case 16:
				instance.PercentComplete = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
				instance.Status = ProtocolParser.ReadString(stream);
				break;
			case 32:
				instance.PercentCompleteSubitem = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ServerLevelProgress DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerLevelProgress instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ServerLevelProgress result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerLevelProgress instance)
	{
		if (instance.PercentComplete != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.PercentComplete);
		}
		if (instance.Status != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.Status);
		}
		if (instance.PercentCompleteSubitem != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.PercentCompleteSubitem);
		}
	}

	public static int GetSize(Packet_ServerLevelProgress instance)
	{
		int num = 0;
		if (instance.PercentComplete != 0)
		{
			num += ProtocolParser.GetSize(instance.PercentComplete) + 1;
		}
		if (instance.Status != null)
		{
			num += ProtocolParser.GetSize(instance.Status) + 1;
		}
		if (instance.PercentCompleteSubitem != 0)
		{
			num += ProtocolParser.GetSize(instance.PercentCompleteSubitem) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerLevelProgress instance)
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

	public static byte[] SerializeToBytes(Packet_ServerLevelProgress instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerLevelProgress instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
