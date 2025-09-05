using System;

public class Packet_ServerChunksSerializer
{
	private const int field = 8;

	public static Packet_ServerChunks DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerChunks packet_ServerChunks = new Packet_ServerChunks();
		DeserializeLengthDelimited(stream, packet_ServerChunks);
		return packet_ServerChunks;
	}

	public static Packet_ServerChunks DeserializeBuffer(byte[] buffer, int length, Packet_ServerChunks instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerChunks Deserialize(CitoMemoryStream stream, Packet_ServerChunks instance)
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
				instance.ChunksAdd(Packet_ServerChunkSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_ServerChunks DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerChunks instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ServerChunks result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerChunks instance)
	{
		if (instance.Chunks != null)
		{
			Packet_ServerChunk[] chunks = instance.Chunks;
			int chunksCount = instance.ChunksCount;
			for (int i = 0; i < chunks.Length && i < chunksCount; i++)
			{
				stream.WriteByte(10);
				Packet_ServerChunkSerializer.SerializeWithSize(stream, chunks[i]);
			}
		}
	}

	public static int GetSize(Packet_ServerChunks instance)
	{
		int num = 0;
		if (instance.Chunks != null)
		{
			for (int i = 0; i < instance.ChunksCount; i++)
			{
				int size = Packet_ServerChunkSerializer.GetSize(instance.Chunks[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerChunks instance)
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

	public static byte[] SerializeToBytes(Packet_ServerChunks instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerChunks instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
