using System;

public class Packet_BlockEntitiesSerializer
{
	private const int field = 8;

	public static Packet_BlockEntities DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockEntities packet_BlockEntities = new Packet_BlockEntities();
		DeserializeLengthDelimited(stream, packet_BlockEntities);
		return packet_BlockEntities;
	}

	public static Packet_BlockEntities DeserializeBuffer(byte[] buffer, int length, Packet_BlockEntities instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockEntities Deserialize(CitoMemoryStream stream, Packet_BlockEntities instance)
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
				instance.BlockEntititesAdd(Packet_BlockEntitySerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_BlockEntities DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockEntities instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_BlockEntities result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlockEntities instance)
	{
		if (instance.BlockEntitites != null)
		{
			Packet_BlockEntity[] blockEntitites = instance.BlockEntitites;
			int blockEntititesCount = instance.BlockEntititesCount;
			for (int i = 0; i < blockEntitites.Length && i < blockEntititesCount; i++)
			{
				stream.WriteByte(10);
				Packet_BlockEntitySerializer.SerializeWithSize(stream, blockEntitites[i]);
			}
		}
	}

	public static int GetSize(Packet_BlockEntities instance)
	{
		int num = 0;
		if (instance.BlockEntitites != null)
		{
			for (int i = 0; i < instance.BlockEntititesCount; i++)
			{
				int size = Packet_BlockEntitySerializer.GetSize(instance.BlockEntitites[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlockEntities instance)
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

	public static byte[] SerializeToBytes(Packet_BlockEntities instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockEntities instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
