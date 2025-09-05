using System;

public class Packet_TagsSerializer
{
	private const int field = 8;

	public static Packet_Tags DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Tags packet_Tags = new Packet_Tags();
		DeserializeLengthDelimited(stream, packet_Tags);
		return packet_Tags;
	}

	public static Packet_Tags DeserializeBuffer(byte[] buffer, int length, Packet_Tags instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Tags Deserialize(CitoMemoryStream stream, Packet_Tags instance)
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
				instance.EntityTagsAdd(ProtocolParser.ReadString(stream));
				break;
			case 18:
				instance.BlockTagsAdd(ProtocolParser.ReadString(stream));
				break;
			case 26:
				instance.ItemTagsAdd(ProtocolParser.ReadString(stream));
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

	public static Packet_Tags DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Tags instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_Tags result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Tags instance)
	{
		if (instance.EntityTags != null)
		{
			string[] entityTags = instance.EntityTags;
			int entityTagsCount = instance.EntityTagsCount;
			for (int i = 0; i < entityTags.Length && i < entityTagsCount; i++)
			{
				stream.WriteByte(10);
				ProtocolParser.WriteString(stream, entityTags[i]);
			}
		}
		if (instance.BlockTags != null)
		{
			string[] blockTags = instance.BlockTags;
			int blockTagsCount = instance.BlockTagsCount;
			for (int j = 0; j < blockTags.Length && j < blockTagsCount; j++)
			{
				stream.WriteByte(18);
				ProtocolParser.WriteString(stream, blockTags[j]);
			}
		}
		if (instance.ItemTags != null)
		{
			string[] itemTags = instance.ItemTags;
			int itemTagsCount = instance.ItemTagsCount;
			for (int k = 0; k < itemTags.Length && k < itemTagsCount; k++)
			{
				stream.WriteByte(26);
				ProtocolParser.WriteString(stream, itemTags[k]);
			}
		}
	}

	public static int GetSize(Packet_Tags instance)
	{
		int num = 0;
		if (instance.EntityTags != null)
		{
			for (int i = 0; i < instance.EntityTagsCount; i++)
			{
				string s = instance.EntityTags[i];
				num += ProtocolParser.GetSize(s) + 1;
			}
		}
		if (instance.BlockTags != null)
		{
			for (int j = 0; j < instance.BlockTagsCount; j++)
			{
				string s2 = instance.BlockTags[j];
				num += ProtocolParser.GetSize(s2) + 1;
			}
		}
		if (instance.ItemTags != null)
		{
			for (int k = 0; k < instance.ItemTagsCount; k++)
			{
				string s3 = instance.ItemTags[k];
				num += ProtocolParser.GetSize(s3) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Tags instance)
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

	public static byte[] SerializeToBytes(Packet_Tags instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Tags instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
