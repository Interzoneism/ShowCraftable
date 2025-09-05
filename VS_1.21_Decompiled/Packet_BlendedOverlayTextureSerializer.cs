using System;

public class Packet_BlendedOverlayTextureSerializer
{
	private const int field = 8;

	public static Packet_BlendedOverlayTexture DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlendedOverlayTexture packet_BlendedOverlayTexture = new Packet_BlendedOverlayTexture();
		DeserializeLengthDelimited(stream, packet_BlendedOverlayTexture);
		return packet_BlendedOverlayTexture;
	}

	public static Packet_BlendedOverlayTexture DeserializeBuffer(byte[] buffer, int length, Packet_BlendedOverlayTexture instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlendedOverlayTexture Deserialize(CitoMemoryStream stream, Packet_BlendedOverlayTexture instance)
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
				instance.Base = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.Mode = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_BlendedOverlayTexture DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlendedOverlayTexture instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_BlendedOverlayTexture result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlendedOverlayTexture instance)
	{
		if (instance.Base != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Base);
		}
		if (instance.Mode != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Mode);
		}
	}

	public static int GetSize(Packet_BlendedOverlayTexture instance)
	{
		int num = 0;
		if (instance.Base != null)
		{
			num += ProtocolParser.GetSize(instance.Base) + 1;
		}
		if (instance.Mode != 0)
		{
			num += ProtocolParser.GetSize(instance.Mode) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlendedOverlayTexture instance)
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

	public static byte[] SerializeToBytes(Packet_BlendedOverlayTexture instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlendedOverlayTexture instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
