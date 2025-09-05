using System;

public class Packet_ServerSoundSerializer
{
	private const int field = 8;

	public static Packet_ServerSound DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerSound packet_ServerSound = new Packet_ServerSound();
		DeserializeLengthDelimited(stream, packet_ServerSound);
		return packet_ServerSound;
	}

	public static Packet_ServerSound DeserializeBuffer(byte[] buffer, int length, Packet_ServerSound instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerSound Deserialize(CitoMemoryStream stream, Packet_ServerSound instance)
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
				instance.Name = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.X = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Y = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.Z = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Pitch = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Range = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.Volume = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.SoundType = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ServerSound DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerSound instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ServerSound result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerSound instance)
	{
		if (instance.Name != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Name);
		}
		if (instance.X != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.X);
		}
		if (instance.Y != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Y);
		}
		if (instance.Z != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Z);
		}
		if (instance.Pitch != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Pitch);
		}
		if (instance.Range != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Range);
		}
		if (instance.Volume != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.Volume);
		}
		if (instance.SoundType != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.SoundType);
		}
	}

	public static int GetSize(Packet_ServerSound instance)
	{
		int num = 0;
		if (instance.Name != null)
		{
			num += ProtocolParser.GetSize(instance.Name) + 1;
		}
		if (instance.X != 0)
		{
			num += ProtocolParser.GetSize(instance.X) + 1;
		}
		if (instance.Y != 0)
		{
			num += ProtocolParser.GetSize(instance.Y) + 1;
		}
		if (instance.Z != 0)
		{
			num += ProtocolParser.GetSize(instance.Z) + 1;
		}
		if (instance.Pitch != 0)
		{
			num += ProtocolParser.GetSize(instance.Pitch) + 1;
		}
		if (instance.Range != 0)
		{
			num += ProtocolParser.GetSize(instance.Range) + 1;
		}
		if (instance.Volume != 0)
		{
			num += ProtocolParser.GetSize(instance.Volume) + 1;
		}
		if (instance.SoundType != 0)
		{
			num += ProtocolParser.GetSize(instance.SoundType) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerSound instance)
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

	public static byte[] SerializeToBytes(Packet_ServerSound instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerSound instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
