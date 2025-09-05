using System;

public class Packet_BlockSoundSetSerializer
{
	private const int field = 8;

	public static Packet_BlockSoundSet DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockSoundSet packet_BlockSoundSet = new Packet_BlockSoundSet();
		DeserializeLengthDelimited(stream, packet_BlockSoundSet);
		return packet_BlockSoundSet;
	}

	public static Packet_BlockSoundSet DeserializeBuffer(byte[] buffer, int length, Packet_BlockSoundSet instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockSoundSet Deserialize(CitoMemoryStream stream, Packet_BlockSoundSet instance)
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
				instance.Walk = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.Break = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.Place = ProtocolParser.ReadString(stream);
				break;
			case 34:
				instance.Hit = ProtocolParser.ReadString(stream);
				break;
			case 42:
				instance.Inside = ProtocolParser.ReadString(stream);
				break;
			case 50:
				instance.Ambient = ProtocolParser.ReadString(stream);
				break;
			case 72:
				instance.AmbientBlockCount = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.AmbientSoundType = ProtocolParser.ReadUInt32(stream);
				break;
			case 88:
				instance.AmbientMaxDistanceMerge = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.ByToolToolAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 66:
				instance.ByToolSoundAdd(DeserializeLengthDelimitedNew(stream));
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

	public static Packet_BlockSoundSet DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockSoundSet instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_BlockSoundSet result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlockSoundSet instance)
	{
		if (instance.Walk != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Walk);
		}
		if (instance.Break != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Break);
		}
		if (instance.Place != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.Place);
		}
		if (instance.Hit != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteString(stream, instance.Hit);
		}
		if (instance.Inside != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteString(stream, instance.Inside);
		}
		if (instance.Ambient != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteString(stream, instance.Ambient);
		}
		if (instance.AmbientBlockCount != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.AmbientBlockCount);
		}
		if (instance.AmbientSoundType != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.AmbientSoundType);
		}
		if (instance.AmbientMaxDistanceMerge != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.AmbientMaxDistanceMerge);
		}
		if (instance.ByToolTool != null)
		{
			int[] byToolTool = instance.ByToolTool;
			int byToolToolCount = instance.ByToolToolCount;
			for (int i = 0; i < byToolTool.Length && i < byToolToolCount; i++)
			{
				stream.WriteByte(56);
				ProtocolParser.WriteUInt32(stream, byToolTool[i]);
			}
		}
		if (instance.ByToolSound != null)
		{
			Packet_BlockSoundSet[] byToolSound = instance.ByToolSound;
			int byToolSoundCount = instance.ByToolSoundCount;
			for (int j = 0; j < byToolSound.Length && j < byToolSoundCount; j++)
			{
				stream.WriteByte(66);
				SerializeWithSize(stream, byToolSound[j]);
			}
		}
	}

	public static int GetSize(Packet_BlockSoundSet instance)
	{
		int num = 0;
		if (instance.Walk != null)
		{
			num += ProtocolParser.GetSize(instance.Walk) + 1;
		}
		if (instance.Break != null)
		{
			num += ProtocolParser.GetSize(instance.Break) + 1;
		}
		if (instance.Place != null)
		{
			num += ProtocolParser.GetSize(instance.Place) + 1;
		}
		if (instance.Hit != null)
		{
			num += ProtocolParser.GetSize(instance.Hit) + 1;
		}
		if (instance.Inside != null)
		{
			num += ProtocolParser.GetSize(instance.Inside) + 1;
		}
		if (instance.Ambient != null)
		{
			num += ProtocolParser.GetSize(instance.Ambient) + 1;
		}
		if (instance.AmbientBlockCount != 0)
		{
			num += ProtocolParser.GetSize(instance.AmbientBlockCount) + 1;
		}
		if (instance.AmbientSoundType != 0)
		{
			num += ProtocolParser.GetSize(instance.AmbientSoundType) + 1;
		}
		if (instance.AmbientMaxDistanceMerge != 0)
		{
			num += ProtocolParser.GetSize(instance.AmbientMaxDistanceMerge) + 1;
		}
		if (instance.ByToolTool != null)
		{
			for (int i = 0; i < instance.ByToolToolCount; i++)
			{
				int v = instance.ByToolTool[i];
				num += ProtocolParser.GetSize(v) + 1;
			}
		}
		if (instance.ByToolSound != null)
		{
			for (int j = 0; j < instance.ByToolSoundCount; j++)
			{
				int size = GetSize(instance.ByToolSound[j]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlockSoundSet instance)
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

	public static byte[] SerializeToBytes(Packet_BlockSoundSet instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockSoundSet instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
