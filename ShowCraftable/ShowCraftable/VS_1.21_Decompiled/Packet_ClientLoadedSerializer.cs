using System;

public class Packet_ClientLoadedSerializer
{
	private const int field = 8;

	public static Packet_ClientLoaded DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientLoaded packet_ClientLoaded = new Packet_ClientLoaded();
		DeserializeLengthDelimited(stream, packet_ClientLoaded);
		return packet_ClientLoaded;
	}

	public static Packet_ClientLoaded DeserializeBuffer(byte[] buffer, int length, Packet_ClientLoaded instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientLoaded Deserialize(CitoMemoryStream stream, Packet_ClientLoaded instance)
	{
		instance.InitializeValues();
		while (true)
		{
			int num = stream.ReadByte();
			if ((num & 0x80) != 0)
			{
				num = ProtocolParser.ReadKeyAsInt(num, stream);
				if ((num & 0x4000) != 0)
				{
					if (num < 0)
					{
						break;
					}
					return null;
				}
			}
			if (num == 0)
			{
				return null;
			}
			ProtocolParser.SkipKey(stream, Key.Create(num));
		}
		return instance;
	}

	public static Packet_ClientLoaded DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientLoaded instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ClientLoaded result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ClientLoaded instance)
	{
	}

	public static int GetSize(Packet_ClientLoaded instance)
	{
		return instance.size = 0;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientLoaded instance)
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

	public static byte[] SerializeToBytes(Packet_ClientLoaded instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientLoaded instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
