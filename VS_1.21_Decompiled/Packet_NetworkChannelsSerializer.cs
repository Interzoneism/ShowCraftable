using System;

public class Packet_NetworkChannelsSerializer
{
	private const int field = 8;

	public static Packet_NetworkChannels DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_NetworkChannels packet_NetworkChannels = new Packet_NetworkChannels();
		DeserializeLengthDelimited(stream, packet_NetworkChannels);
		return packet_NetworkChannels;
	}

	public static Packet_NetworkChannels DeserializeBuffer(byte[] buffer, int length, Packet_NetworkChannels instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_NetworkChannels Deserialize(CitoMemoryStream stream, Packet_NetworkChannels instance)
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
				instance.ChannelIdsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 18:
				instance.ChannelNamesAdd(ProtocolParser.ReadString(stream));
				break;
			case 24:
				instance.ChannelUdpIdsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 34:
				instance.ChannelUdpNamesAdd(ProtocolParser.ReadString(stream));
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

	public static Packet_NetworkChannels DeserializeLengthDelimited(CitoMemoryStream stream, Packet_NetworkChannels instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_NetworkChannels result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_NetworkChannels instance)
	{
		if (instance.ChannelIds != null)
		{
			int[] channelIds = instance.ChannelIds;
			int channelIdsCount = instance.ChannelIdsCount;
			for (int i = 0; i < channelIds.Length && i < channelIdsCount; i++)
			{
				stream.WriteByte(8);
				ProtocolParser.WriteUInt32(stream, channelIds[i]);
			}
		}
		if (instance.ChannelNames != null)
		{
			string[] channelNames = instance.ChannelNames;
			int channelNamesCount = instance.ChannelNamesCount;
			for (int j = 0; j < channelNames.Length && j < channelNamesCount; j++)
			{
				stream.WriteByte(18);
				ProtocolParser.WriteString(stream, channelNames[j]);
			}
		}
		if (instance.ChannelUdpIds != null)
		{
			int[] channelUdpIds = instance.ChannelUdpIds;
			int channelUdpIdsCount = instance.ChannelUdpIdsCount;
			for (int k = 0; k < channelUdpIds.Length && k < channelUdpIdsCount; k++)
			{
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, channelUdpIds[k]);
			}
		}
		if (instance.ChannelUdpNames != null)
		{
			string[] channelUdpNames = instance.ChannelUdpNames;
			int channelUdpNamesCount = instance.ChannelUdpNamesCount;
			for (int l = 0; l < channelUdpNames.Length && l < channelUdpNamesCount; l++)
			{
				stream.WriteByte(34);
				ProtocolParser.WriteString(stream, channelUdpNames[l]);
			}
		}
	}

	public static int GetSize(Packet_NetworkChannels instance)
	{
		int num = 0;
		if (instance.ChannelIds != null)
		{
			for (int i = 0; i < instance.ChannelIdsCount; i++)
			{
				int v = instance.ChannelIds[i];
				num += ProtocolParser.GetSize(v) + 1;
			}
		}
		if (instance.ChannelNames != null)
		{
			for (int j = 0; j < instance.ChannelNamesCount; j++)
			{
				string s = instance.ChannelNames[j];
				num += ProtocolParser.GetSize(s) + 1;
			}
		}
		if (instance.ChannelUdpIds != null)
		{
			for (int k = 0; k < instance.ChannelUdpIdsCount; k++)
			{
				int v2 = instance.ChannelUdpIds[k];
				num += ProtocolParser.GetSize(v2) + 1;
			}
		}
		if (instance.ChannelUdpNames != null)
		{
			for (int l = 0; l < instance.ChannelUdpNamesCount; l++)
			{
				string s2 = instance.ChannelUdpNames[l];
				num += ProtocolParser.GetSize(s2) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_NetworkChannels instance)
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

	public static byte[] SerializeToBytes(Packet_NetworkChannels instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_NetworkChannels instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
