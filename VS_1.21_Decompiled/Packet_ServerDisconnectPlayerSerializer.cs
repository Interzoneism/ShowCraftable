using System;

public class Packet_ServerDisconnectPlayerSerializer
{
	private const int field = 8;

	public static Packet_ServerDisconnectPlayer DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerDisconnectPlayer packet_ServerDisconnectPlayer = new Packet_ServerDisconnectPlayer();
		DeserializeLengthDelimited(stream, packet_ServerDisconnectPlayer);
		return packet_ServerDisconnectPlayer;
	}

	public static Packet_ServerDisconnectPlayer DeserializeBuffer(byte[] buffer, int length, Packet_ServerDisconnectPlayer instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerDisconnectPlayer Deserialize(CitoMemoryStream stream, Packet_ServerDisconnectPlayer instance)
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
				instance.DisconnectReason = ProtocolParser.ReadString(stream);
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

	public static Packet_ServerDisconnectPlayer DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerDisconnectPlayer instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ServerDisconnectPlayer result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerDisconnectPlayer instance)
	{
		if (instance.DisconnectReason != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.DisconnectReason);
		}
	}

	public static int GetSize(Packet_ServerDisconnectPlayer instance)
	{
		int num = 0;
		if (instance.DisconnectReason != null)
		{
			num += ProtocolParser.GetSize(instance.DisconnectReason) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerDisconnectPlayer instance)
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

	public static byte[] SerializeToBytes(Packet_ServerDisconnectPlayer instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerDisconnectPlayer instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
