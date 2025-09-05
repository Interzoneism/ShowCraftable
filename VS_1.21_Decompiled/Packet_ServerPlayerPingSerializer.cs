using System;

public class Packet_ServerPlayerPingSerializer
{
	private const int field = 8;

	public static Packet_ServerPlayerPing DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerPlayerPing packet_ServerPlayerPing = new Packet_ServerPlayerPing();
		DeserializeLengthDelimited(stream, packet_ServerPlayerPing);
		return packet_ServerPlayerPing;
	}

	public static Packet_ServerPlayerPing DeserializeBuffer(byte[] buffer, int length, Packet_ServerPlayerPing instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerPlayerPing Deserialize(CitoMemoryStream stream, Packet_ServerPlayerPing instance)
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
				instance.ClientIdsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 16:
				instance.PingsAdd(ProtocolParser.ReadUInt32(stream));
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

	public static Packet_ServerPlayerPing DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerPlayerPing instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ServerPlayerPing result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerPlayerPing instance)
	{
		if (instance.ClientIds != null)
		{
			int[] clientIds = instance.ClientIds;
			int clientIdsCount = instance.ClientIdsCount;
			for (int i = 0; i < clientIds.Length && i < clientIdsCount; i++)
			{
				stream.WriteByte(8);
				ProtocolParser.WriteUInt32(stream, clientIds[i]);
			}
		}
		if (instance.Pings != null)
		{
			int[] pings = instance.Pings;
			int pingsCount = instance.PingsCount;
			for (int j = 0; j < pings.Length && j < pingsCount; j++)
			{
				stream.WriteByte(16);
				ProtocolParser.WriteUInt32(stream, pings[j]);
			}
		}
	}

	public static int GetSize(Packet_ServerPlayerPing instance)
	{
		int num = 0;
		if (instance.ClientIds != null)
		{
			for (int i = 0; i < instance.ClientIdsCount; i++)
			{
				int v = instance.ClientIds[i];
				num += ProtocolParser.GetSize(v) + 1;
			}
		}
		if (instance.Pings != null)
		{
			for (int j = 0; j < instance.PingsCount; j++)
			{
				int v2 = instance.Pings[j];
				num += ProtocolParser.GetSize(v2) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerPlayerPing instance)
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

	public static byte[] SerializeToBytes(Packet_ServerPlayerPing instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerPlayerPing instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
