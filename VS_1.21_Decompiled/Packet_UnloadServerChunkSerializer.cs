using System;

public class Packet_UnloadServerChunkSerializer
{
	private const int field = 8;

	public static Packet_UnloadServerChunk DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_UnloadServerChunk packet_UnloadServerChunk = new Packet_UnloadServerChunk();
		DeserializeLengthDelimited(stream, packet_UnloadServerChunk);
		return packet_UnloadServerChunk;
	}

	public static Packet_UnloadServerChunk DeserializeBuffer(byte[] buffer, int length, Packet_UnloadServerChunk instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_UnloadServerChunk Deserialize(CitoMemoryStream stream, Packet_UnloadServerChunk instance)
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
				instance.XAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 16:
				instance.YAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 24:
				instance.ZAdd(ProtocolParser.ReadUInt32(stream));
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

	public static Packet_UnloadServerChunk DeserializeLengthDelimited(CitoMemoryStream stream, Packet_UnloadServerChunk instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_UnloadServerChunk result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_UnloadServerChunk instance)
	{
		if (instance.X != null)
		{
			int[] x = instance.X;
			int xCount = instance.XCount;
			for (int i = 0; i < x.Length && i < xCount; i++)
			{
				stream.WriteByte(8);
				ProtocolParser.WriteUInt32(stream, x[i]);
			}
		}
		if (instance.Y != null)
		{
			int[] y = instance.Y;
			int yCount = instance.YCount;
			for (int j = 0; j < y.Length && j < yCount; j++)
			{
				stream.WriteByte(16);
				ProtocolParser.WriteUInt32(stream, y[j]);
			}
		}
		if (instance.Z != null)
		{
			int[] z = instance.Z;
			int zCount = instance.ZCount;
			for (int k = 0; k < z.Length && k < zCount; k++)
			{
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, z[k]);
			}
		}
	}

	public static int GetSize(Packet_UnloadServerChunk instance)
	{
		int num = 0;
		if (instance.X != null)
		{
			for (int i = 0; i < instance.XCount; i++)
			{
				int v = instance.X[i];
				num += ProtocolParser.GetSize(v) + 1;
			}
		}
		if (instance.Y != null)
		{
			for (int j = 0; j < instance.YCount; j++)
			{
				int v2 = instance.Y[j];
				num += ProtocolParser.GetSize(v2) + 1;
			}
		}
		if (instance.Z != null)
		{
			for (int k = 0; k < instance.ZCount; k++)
			{
				int v3 = instance.Z[k];
				num += ProtocolParser.GetSize(v3) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_UnloadServerChunk instance)
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

	public static byte[] SerializeToBytes(Packet_UnloadServerChunk instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_UnloadServerChunk instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
