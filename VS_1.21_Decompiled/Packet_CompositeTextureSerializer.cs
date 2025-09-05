using System;

public class Packet_CompositeTextureSerializer
{
	private const int field = 8;

	public static Packet_CompositeTexture DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CompositeTexture packet_CompositeTexture = new Packet_CompositeTexture();
		DeserializeLengthDelimited(stream, packet_CompositeTexture);
		return packet_CompositeTexture;
	}

	public static Packet_CompositeTexture DeserializeBuffer(byte[] buffer, int length, Packet_CompositeTexture instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CompositeTexture Deserialize(CitoMemoryStream stream, Packet_CompositeTexture instance)
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
			case 18:
				instance.OverlaysAdd(Packet_BlendedOverlayTextureSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 26:
				instance.AlternatesAdd(DeserializeLengthDelimitedNew(stream));
				break;
			case 32:
				instance.Rotation = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Alpha = ProtocolParser.ReadUInt32(stream);
				break;
			case 50:
				instance.TilesAdd(DeserializeLengthDelimitedNew(stream));
				break;
			case 56:
				instance.TilesWidth = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_CompositeTexture DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CompositeTexture instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_CompositeTexture result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_CompositeTexture instance)
	{
		if (instance.Base != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Base);
		}
		if (instance.Overlays != null)
		{
			Packet_BlendedOverlayTexture[] overlays = instance.Overlays;
			int overlaysCount = instance.OverlaysCount;
			for (int i = 0; i < overlays.Length && i < overlaysCount; i++)
			{
				stream.WriteByte(18);
				Packet_BlendedOverlayTextureSerializer.SerializeWithSize(stream, overlays[i]);
			}
		}
		if (instance.Alternates != null)
		{
			Packet_CompositeTexture[] alternates = instance.Alternates;
			int alternatesCount = instance.AlternatesCount;
			for (int j = 0; j < alternates.Length && j < alternatesCount; j++)
			{
				stream.WriteByte(26);
				SerializeWithSize(stream, alternates[j]);
			}
		}
		if (instance.Rotation != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Rotation);
		}
		if (instance.Alpha != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Alpha);
		}
		if (instance.Tiles != null)
		{
			Packet_CompositeTexture[] tiles = instance.Tiles;
			int tilesCount = instance.TilesCount;
			for (int k = 0; k < tiles.Length && k < tilesCount; k++)
			{
				stream.WriteByte(50);
				SerializeWithSize(stream, tiles[k]);
			}
		}
		if (instance.TilesWidth != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.TilesWidth);
		}
	}

	public static int GetSize(Packet_CompositeTexture instance)
	{
		int num = 0;
		if (instance.Base != null)
		{
			num += ProtocolParser.GetSize(instance.Base) + 1;
		}
		if (instance.Overlays != null)
		{
			for (int i = 0; i < instance.OverlaysCount; i++)
			{
				int size = Packet_BlendedOverlayTextureSerializer.GetSize(instance.Overlays[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		if (instance.Alternates != null)
		{
			for (int j = 0; j < instance.AlternatesCount; j++)
			{
				int size2 = GetSize(instance.Alternates[j]);
				num += size2 + ProtocolParser.GetSize(size2) + 1;
			}
		}
		if (instance.Rotation != 0)
		{
			num += ProtocolParser.GetSize(instance.Rotation) + 1;
		}
		if (instance.Alpha != 0)
		{
			num += ProtocolParser.GetSize(instance.Alpha) + 1;
		}
		if (instance.Tiles != null)
		{
			for (int k = 0; k < instance.TilesCount; k++)
			{
				int size3 = GetSize(instance.Tiles[k]);
				num += size3 + ProtocolParser.GetSize(size3) + 1;
			}
		}
		if (instance.TilesWidth != 0)
		{
			num += ProtocolParser.GetSize(instance.TilesWidth) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_CompositeTexture instance)
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

	public static byte[] SerializeToBytes(Packet_CompositeTexture instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CompositeTexture instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
