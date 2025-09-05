using System;

public class Packet_ClientIdentificationSerializer
{
	private const int field = 8;

	public static Packet_ClientIdentification DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientIdentification packet_ClientIdentification = new Packet_ClientIdentification();
		DeserializeLengthDelimited(stream, packet_ClientIdentification);
		return packet_ClientIdentification;
	}

	public static Packet_ClientIdentification DeserializeBuffer(byte[] buffer, int length, Packet_ClientIdentification instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientIdentification Deserialize(CitoMemoryStream stream, Packet_ClientIdentification instance)
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
				instance.MdProtocolVersion = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.Playername = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.MpToken = ProtocolParser.ReadString(stream);
				break;
			case 34:
				instance.ServerPassword = ProtocolParser.ReadString(stream);
				break;
			case 50:
				instance.PlayerUID = ProtocolParser.ReadString(stream);
				break;
			case 56:
				instance.ViewDistance = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.RenderMetaBlocks = ProtocolParser.ReadUInt32(stream);
				break;
			case 74:
				instance.NetworkVersion = ProtocolParser.ReadString(stream);
				break;
			case 82:
				instance.ShortGameVersion = ProtocolParser.ReadString(stream);
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

	public static Packet_ClientIdentification DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientIdentification instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ClientIdentification result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ClientIdentification instance)
	{
		if (instance.MdProtocolVersion != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.MdProtocolVersion);
		}
		if (instance.Playername != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Playername);
		}
		if (instance.MpToken != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.MpToken);
		}
		if (instance.ServerPassword != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteString(stream, instance.ServerPassword);
		}
		if (instance.PlayerUID != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteString(stream, instance.PlayerUID);
		}
		if (instance.ViewDistance != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.ViewDistance);
		}
		if (instance.RenderMetaBlocks != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.RenderMetaBlocks);
		}
		if (instance.NetworkVersion != null)
		{
			stream.WriteByte(74);
			ProtocolParser.WriteString(stream, instance.NetworkVersion);
		}
		if (instance.ShortGameVersion != null)
		{
			stream.WriteByte(82);
			ProtocolParser.WriteString(stream, instance.ShortGameVersion);
		}
	}

	public static int GetSize(Packet_ClientIdentification instance)
	{
		int num = 0;
		if (instance.MdProtocolVersion != null)
		{
			num += ProtocolParser.GetSize(instance.MdProtocolVersion) + 1;
		}
		if (instance.Playername != null)
		{
			num += ProtocolParser.GetSize(instance.Playername) + 1;
		}
		if (instance.MpToken != null)
		{
			num += ProtocolParser.GetSize(instance.MpToken) + 1;
		}
		if (instance.ServerPassword != null)
		{
			num += ProtocolParser.GetSize(instance.ServerPassword) + 1;
		}
		if (instance.PlayerUID != null)
		{
			num += ProtocolParser.GetSize(instance.PlayerUID) + 1;
		}
		if (instance.ViewDistance != 0)
		{
			num += ProtocolParser.GetSize(instance.ViewDistance) + 1;
		}
		if (instance.RenderMetaBlocks != 0)
		{
			num += ProtocolParser.GetSize(instance.RenderMetaBlocks) + 1;
		}
		if (instance.NetworkVersion != null)
		{
			num += ProtocolParser.GetSize(instance.NetworkVersion) + 1;
		}
		if (instance.ShortGameVersion != null)
		{
			num += ProtocolParser.GetSize(instance.ShortGameVersion) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientIdentification instance)
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

	public static byte[] SerializeToBytes(Packet_ClientIdentification instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientIdentification instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
