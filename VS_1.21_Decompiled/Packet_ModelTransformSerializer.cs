using System;

public class Packet_ModelTransformSerializer
{
	private const int field = 8;

	public static Packet_ModelTransform DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ModelTransform packet_ModelTransform = new Packet_ModelTransform();
		DeserializeLengthDelimited(stream, packet_ModelTransform);
		return packet_ModelTransform;
	}

	public static Packet_ModelTransform DeserializeBuffer(byte[] buffer, int length, Packet_ModelTransform instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ModelTransform Deserialize(CitoMemoryStream stream, Packet_ModelTransform instance)
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
				instance.TranslateX = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.TranslateY = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.TranslateZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.RotateX = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.RotateY = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.RotateZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.Rotate = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.OriginX = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.OriginY = ProtocolParser.ReadUInt32(stream);
				break;
			case 88:
				instance.OriginZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 96:
				instance.ScaleX = ProtocolParser.ReadUInt32(stream);
				break;
			case 104:
				instance.ScaleY = ProtocolParser.ReadUInt32(stream);
				break;
			case 112:
				instance.ScaleZ = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ModelTransform DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ModelTransform instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_ModelTransform result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ModelTransform instance)
	{
		if (instance.TranslateX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.TranslateX);
		}
		if (instance.TranslateY != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.TranslateY);
		}
		if (instance.TranslateZ != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.TranslateZ);
		}
		if (instance.RotateX != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.RotateX);
		}
		if (instance.RotateY != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.RotateY);
		}
		if (instance.RotateZ != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.RotateZ);
		}
		if (instance.Rotate != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.Rotate);
		}
		if (instance.OriginX != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.OriginX);
		}
		if (instance.OriginY != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.OriginY);
		}
		if (instance.OriginZ != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.OriginZ);
		}
		if (instance.ScaleX != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.ScaleX);
		}
		if (instance.ScaleY != 0)
		{
			stream.WriteByte(104);
			ProtocolParser.WriteUInt32(stream, instance.ScaleY);
		}
		if (instance.ScaleZ != 0)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt32(stream, instance.ScaleZ);
		}
	}

	public static int GetSize(Packet_ModelTransform instance)
	{
		int num = 0;
		if (instance.TranslateX != 0)
		{
			num += ProtocolParser.GetSize(instance.TranslateX) + 1;
		}
		if (instance.TranslateY != 0)
		{
			num += ProtocolParser.GetSize(instance.TranslateY) + 1;
		}
		if (instance.TranslateZ != 0)
		{
			num += ProtocolParser.GetSize(instance.TranslateZ) + 1;
		}
		if (instance.RotateX != 0)
		{
			num += ProtocolParser.GetSize(instance.RotateX) + 1;
		}
		if (instance.RotateY != 0)
		{
			num += ProtocolParser.GetSize(instance.RotateY) + 1;
		}
		if (instance.RotateZ != 0)
		{
			num += ProtocolParser.GetSize(instance.RotateZ) + 1;
		}
		if (instance.Rotate != 0)
		{
			num += ProtocolParser.GetSize(instance.Rotate) + 1;
		}
		if (instance.OriginX != 0)
		{
			num += ProtocolParser.GetSize(instance.OriginX) + 1;
		}
		if (instance.OriginY != 0)
		{
			num += ProtocolParser.GetSize(instance.OriginY) + 1;
		}
		if (instance.OriginZ != 0)
		{
			num += ProtocolParser.GetSize(instance.OriginZ) + 1;
		}
		if (instance.ScaleX != 0)
		{
			num += ProtocolParser.GetSize(instance.ScaleX) + 1;
		}
		if (instance.ScaleY != 0)
		{
			num += ProtocolParser.GetSize(instance.ScaleY) + 1;
		}
		if (instance.ScaleZ != 0)
		{
			num += ProtocolParser.GetSize(instance.ScaleZ) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ModelTransform instance)
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

	public static byte[] SerializeToBytes(Packet_ModelTransform instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ModelTransform instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
