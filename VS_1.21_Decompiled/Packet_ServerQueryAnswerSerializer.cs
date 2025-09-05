using System;

public class Packet_ServerQueryAnswerSerializer
{
	private const int field = 8;

	public static Packet_ServerQueryAnswer DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerQueryAnswer packet_ServerQueryAnswer = new Packet_ServerQueryAnswer();
		DeserializeLengthDelimited(stream, packet_ServerQueryAnswer);
		return packet_ServerQueryAnswer;
	}

	public static Packet_ServerQueryAnswer DeserializeBuffer(byte[] buffer, int length, Packet_ServerQueryAnswer instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerQueryAnswer Deserialize(CitoMemoryStream stream, Packet_ServerQueryAnswer instance)
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
				instance.Name = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.MOTD = ProtocolParser.ReadString(stream);
				break;
			case 24:
				instance.PlayerCount = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.MaxPlayers = ProtocolParser.ReadUInt32(stream);
				break;
			case 42:
				instance.GameMode = ProtocolParser.ReadString(stream);
				break;
			case 48:
				instance.Password = ProtocolParser.ReadBool(stream);
				break;
			case 58:
				instance.ServerVersion = ProtocolParser.ReadString(stream);
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

	public static Packet_ServerQueryAnswer DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerQueryAnswer instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ServerQueryAnswer result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerQueryAnswer instance)
	{
		if (instance.Name != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Name);
		}
		if (instance.MOTD != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.MOTD);
		}
		if (instance.PlayerCount != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.PlayerCount);
		}
		if (instance.MaxPlayers != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.MaxPlayers);
		}
		if (instance.GameMode != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteString(stream, instance.GameMode);
		}
		if (instance.Password)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteBool(stream, instance.Password);
		}
		if (instance.ServerVersion != null)
		{
			stream.WriteByte(58);
			ProtocolParser.WriteString(stream, instance.ServerVersion);
		}
	}

	public static int GetSize(Packet_ServerQueryAnswer instance)
	{
		int num = 0;
		if (instance.Name != null)
		{
			num += ProtocolParser.GetSize(instance.Name) + 1;
		}
		if (instance.MOTD != null)
		{
			num += ProtocolParser.GetSize(instance.MOTD) + 1;
		}
		if (instance.PlayerCount != 0)
		{
			num += ProtocolParser.GetSize(instance.PlayerCount) + 1;
		}
		if (instance.MaxPlayers != 0)
		{
			num += ProtocolParser.GetSize(instance.MaxPlayers) + 1;
		}
		if (instance.GameMode != null)
		{
			num += ProtocolParser.GetSize(instance.GameMode) + 1;
		}
		if (instance.Password)
		{
			num += 2;
		}
		if (instance.ServerVersion != null)
		{
			num += ProtocolParser.GetSize(instance.ServerVersion) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerQueryAnswer instance)
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

	public static byte[] SerializeToBytes(Packet_ServerQueryAnswer instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerQueryAnswer instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
