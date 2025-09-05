using System;

public class Packet_RuntimeSettingSerializer
{
	private const int field = 8;

	public static Packet_RuntimeSetting DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_RuntimeSetting packet_RuntimeSetting = new Packet_RuntimeSetting();
		DeserializeLengthDelimited(stream, packet_RuntimeSetting);
		return packet_RuntimeSetting;
	}

	public static Packet_RuntimeSetting DeserializeBuffer(byte[] buffer, int length, Packet_RuntimeSetting instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_RuntimeSetting Deserialize(CitoMemoryStream stream, Packet_RuntimeSetting instance)
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
				instance.ImmersiveFpMode = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.ItemCollectMode = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_RuntimeSetting DeserializeLengthDelimited(CitoMemoryStream stream, Packet_RuntimeSetting instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_RuntimeSetting result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_RuntimeSetting instance)
	{
		if (instance.ImmersiveFpMode != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ImmersiveFpMode);
		}
		if (instance.ItemCollectMode != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.ItemCollectMode);
		}
	}

	public static int GetSize(Packet_RuntimeSetting instance)
	{
		int num = 0;
		if (instance.ImmersiveFpMode != 0)
		{
			num += ProtocolParser.GetSize(instance.ImmersiveFpMode) + 1;
		}
		if (instance.ItemCollectMode != 0)
		{
			num += ProtocolParser.GetSize(instance.ItemCollectMode) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_RuntimeSetting instance)
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

	public static byte[] SerializeToBytes(Packet_RuntimeSetting instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_RuntimeSetting instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
