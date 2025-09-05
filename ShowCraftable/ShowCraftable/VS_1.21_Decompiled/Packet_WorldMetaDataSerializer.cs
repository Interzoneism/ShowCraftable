using System;

public class Packet_WorldMetaDataSerializer
{
	private const int field = 8;

	public static Packet_WorldMetaData DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_WorldMetaData packet_WorldMetaData = new Packet_WorldMetaData();
		DeserializeLengthDelimited(stream, packet_WorldMetaData);
		return packet_WorldMetaData;
	}

	public static Packet_WorldMetaData DeserializeBuffer(byte[] buffer, int length, Packet_WorldMetaData instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_WorldMetaData Deserialize(CitoMemoryStream stream, Packet_WorldMetaData instance)
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
				instance.SunBrightness = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.BlockLightlevelsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 24:
				instance.SunLightlevelsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 34:
				instance.WorldConfiguration = ProtocolParser.ReadBytes(stream);
				break;
			case 40:
				instance.SeaLevel = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_WorldMetaData DeserializeLengthDelimited(CitoMemoryStream stream, Packet_WorldMetaData instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_WorldMetaData result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_WorldMetaData instance)
	{
		if (instance.SunBrightness != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.SunBrightness);
		}
		if (instance.BlockLightlevels != null)
		{
			int[] blockLightlevels = instance.BlockLightlevels;
			int blockLightlevelsCount = instance.BlockLightlevelsCount;
			for (int i = 0; i < blockLightlevels.Length && i < blockLightlevelsCount; i++)
			{
				stream.WriteByte(16);
				ProtocolParser.WriteUInt32(stream, blockLightlevels[i]);
			}
		}
		if (instance.SunLightlevels != null)
		{
			int[] sunLightlevels = instance.SunLightlevels;
			int sunLightlevelsCount = instance.SunLightlevelsCount;
			for (int j = 0; j < sunLightlevels.Length && j < sunLightlevelsCount; j++)
			{
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, sunLightlevels[j]);
			}
		}
		if (instance.WorldConfiguration != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, instance.WorldConfiguration);
		}
		if (instance.SeaLevel != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.SeaLevel);
		}
	}

	public static int GetSize(Packet_WorldMetaData instance)
	{
		int num = 0;
		if (instance.SunBrightness != 0)
		{
			num += ProtocolParser.GetSize(instance.SunBrightness) + 1;
		}
		if (instance.BlockLightlevels != null)
		{
			for (int i = 0; i < instance.BlockLightlevelsCount; i++)
			{
				int v = instance.BlockLightlevels[i];
				num += ProtocolParser.GetSize(v) + 1;
			}
		}
		if (instance.SunLightlevels != null)
		{
			for (int j = 0; j < instance.SunLightlevelsCount; j++)
			{
				int v2 = instance.SunLightlevels[j];
				num += ProtocolParser.GetSize(v2) + 1;
			}
		}
		if (instance.WorldConfiguration != null)
		{
			num += ProtocolParser.GetSize(instance.WorldConfiguration) + 1;
		}
		if (instance.SeaLevel != 0)
		{
			num += ProtocolParser.GetSize(instance.SeaLevel) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_WorldMetaData instance)
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

	public static byte[] SerializeToBytes(Packet_WorldMetaData instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_WorldMetaData instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
