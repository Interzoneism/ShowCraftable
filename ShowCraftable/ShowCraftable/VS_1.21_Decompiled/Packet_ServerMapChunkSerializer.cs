using System;

public class Packet_ServerMapChunkSerializer
{
	private const int field = 8;

	public static Packet_ServerMapChunk DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerMapChunk packet_ServerMapChunk = new Packet_ServerMapChunk();
		DeserializeLengthDelimited(stream, packet_ServerMapChunk);
		return packet_ServerMapChunk;
	}

	public static Packet_ServerMapChunk DeserializeBuffer(byte[] buffer, int length, Packet_ServerMapChunk instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerMapChunk Deserialize(CitoMemoryStream stream, Packet_ServerMapChunk instance)
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
				instance.ChunkX = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.ChunkZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Ymax = ProtocolParser.ReadUInt32(stream);
				break;
			case 42:
				instance.RainHeightMap = ProtocolParser.ReadBytes(stream);
				break;
			case 58:
				instance.TerrainHeightMap = ProtocolParser.ReadBytes(stream);
				break;
			case 50:
				instance.Structures = ProtocolParser.ReadBytes(stream);
				break;
			case 66:
				instance.Moddata = ProtocolParser.ReadBytes(stream);
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

	public static Packet_ServerMapChunk DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerMapChunk instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ServerMapChunk result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerMapChunk instance)
	{
		if (instance.ChunkX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ChunkX);
		}
		if (instance.ChunkZ != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.ChunkZ);
		}
		if (instance.Ymax != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Ymax);
		}
		if (instance.RainHeightMap != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteBytes(stream, instance.RainHeightMap);
		}
		if (instance.TerrainHeightMap != null)
		{
			stream.WriteByte(58);
			ProtocolParser.WriteBytes(stream, instance.TerrainHeightMap);
		}
		if (instance.Structures != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteBytes(stream, instance.Structures);
		}
		if (instance.Moddata != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteBytes(stream, instance.Moddata);
		}
	}

	public static int GetSize(Packet_ServerMapChunk instance)
	{
		int num = 0;
		if (instance.ChunkX != 0)
		{
			num += ProtocolParser.GetSize(instance.ChunkX) + 1;
		}
		if (instance.ChunkZ != 0)
		{
			num += ProtocolParser.GetSize(instance.ChunkZ) + 1;
		}
		if (instance.Ymax != 0)
		{
			num += ProtocolParser.GetSize(instance.Ymax) + 1;
		}
		if (instance.RainHeightMap != null)
		{
			num += ProtocolParser.GetSize(instance.RainHeightMap) + 1;
		}
		if (instance.TerrainHeightMap != null)
		{
			num += ProtocolParser.GetSize(instance.TerrainHeightMap) + 1;
		}
		if (instance.Structures != null)
		{
			num += ProtocolParser.GetSize(instance.Structures) + 1;
		}
		if (instance.Moddata != null)
		{
			num += ProtocolParser.GetSize(instance.Moddata) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerMapChunk instance)
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

	public static byte[] SerializeToBytes(Packet_ServerMapChunk instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerMapChunk instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
