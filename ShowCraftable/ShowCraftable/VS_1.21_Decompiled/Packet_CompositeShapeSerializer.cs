using System;

public class Packet_CompositeShapeSerializer
{
	private const int field = 8;

	public static Packet_CompositeShape DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CompositeShape packet_CompositeShape = new Packet_CompositeShape();
		DeserializeLengthDelimited(stream, packet_CompositeShape);
		return packet_CompositeShape;
	}

	public static Packet_CompositeShape DeserializeBuffer(byte[] buffer, int length, Packet_CompositeShape instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CompositeShape Deserialize(CitoMemoryStream stream, Packet_CompositeShape instance)
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
				instance.Rotatex = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Rotatey = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.Rotatez = ProtocolParser.ReadUInt32(stream);
				break;
			case 42:
				instance.AlternatesAdd(DeserializeLengthDelimitedNew(stream));
				break;
			case 90:
				instance.OverlaysAdd(DeserializeLengthDelimitedNew(stream));
				break;
			case 48:
				instance.VoxelizeShape = ProtocolParser.ReadUInt32(stream);
				break;
			case 58:
				instance.SelectiveElementsAdd(ProtocolParser.ReadString(stream));
				break;
			case 138:
				instance.IgnoreElementsAdd(ProtocolParser.ReadString(stream));
				break;
			case 64:
				instance.QuantityElements = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.QuantityElementsSet = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.Format = ProtocolParser.ReadUInt32(stream);
				break;
			case 96:
				instance.Offsetx = ProtocolParser.ReadUInt32(stream);
				break;
			case 104:
				instance.Offsety = ProtocolParser.ReadUInt32(stream);
				break;
			case 112:
				instance.Offsetz = ProtocolParser.ReadUInt32(stream);
				break;
			case 120:
				instance.InsertBakedTextures = ProtocolParser.ReadBool(stream);
				break;
			case 128:
				instance.ScaleAdjust = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_CompositeShape DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CompositeShape instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_CompositeShape result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_CompositeShape instance)
	{
		if (instance.Base != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Base);
		}
		if (instance.Rotatex != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Rotatex);
		}
		if (instance.Rotatey != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Rotatey);
		}
		if (instance.Rotatez != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Rotatez);
		}
		if (instance.Alternates != null)
		{
			Packet_CompositeShape[] alternates = instance.Alternates;
			int alternatesCount = instance.AlternatesCount;
			for (int i = 0; i < alternates.Length && i < alternatesCount; i++)
			{
				stream.WriteByte(42);
				SerializeWithSize(stream, alternates[i]);
			}
		}
		if (instance.Overlays != null)
		{
			Packet_CompositeShape[] overlays = instance.Overlays;
			int overlaysCount = instance.OverlaysCount;
			for (int j = 0; j < overlays.Length && j < overlaysCount; j++)
			{
				stream.WriteByte(90);
				SerializeWithSize(stream, overlays[j]);
			}
		}
		if (instance.VoxelizeShape != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.VoxelizeShape);
		}
		if (instance.SelectiveElements != null)
		{
			string[] selectiveElements = instance.SelectiveElements;
			int selectiveElementsCount = instance.SelectiveElementsCount;
			for (int k = 0; k < selectiveElements.Length && k < selectiveElementsCount; k++)
			{
				stream.WriteByte(58);
				ProtocolParser.WriteString(stream, selectiveElements[k]);
			}
		}
		if (instance.IgnoreElements != null)
		{
			string[] ignoreElements = instance.IgnoreElements;
			int ignoreElementsCount = instance.IgnoreElementsCount;
			for (int l = 0; l < ignoreElements.Length && l < ignoreElementsCount; l++)
			{
				stream.WriteKey(17, 2);
				ProtocolParser.WriteString(stream, ignoreElements[l]);
			}
		}
		if (instance.QuantityElements != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.QuantityElements);
		}
		if (instance.QuantityElementsSet != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.QuantityElementsSet);
		}
		if (instance.Format != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.Format);
		}
		if (instance.Offsetx != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.Offsetx);
		}
		if (instance.Offsety != 0)
		{
			stream.WriteByte(104);
			ProtocolParser.WriteUInt32(stream, instance.Offsety);
		}
		if (instance.Offsetz != 0)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt32(stream, instance.Offsetz);
		}
		if (instance.InsertBakedTextures)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteBool(stream, instance.InsertBakedTextures);
		}
		if (instance.ScaleAdjust != 0)
		{
			stream.WriteKey(16, 0);
			ProtocolParser.WriteUInt32(stream, instance.ScaleAdjust);
		}
	}

	public static int GetSize(Packet_CompositeShape instance)
	{
		int num = 0;
		if (instance.Base != null)
		{
			num += ProtocolParser.GetSize(instance.Base) + 1;
		}
		if (instance.Rotatex != 0)
		{
			num += ProtocolParser.GetSize(instance.Rotatex) + 1;
		}
		if (instance.Rotatey != 0)
		{
			num += ProtocolParser.GetSize(instance.Rotatey) + 1;
		}
		if (instance.Rotatez != 0)
		{
			num += ProtocolParser.GetSize(instance.Rotatez) + 1;
		}
		if (instance.Alternates != null)
		{
			for (int i = 0; i < instance.AlternatesCount; i++)
			{
				int size = GetSize(instance.Alternates[i]);
				num += size + ProtocolParser.GetSize(size) + 1;
			}
		}
		if (instance.Overlays != null)
		{
			for (int j = 0; j < instance.OverlaysCount; j++)
			{
				int size2 = GetSize(instance.Overlays[j]);
				num += size2 + ProtocolParser.GetSize(size2) + 1;
			}
		}
		if (instance.VoxelizeShape != 0)
		{
			num += ProtocolParser.GetSize(instance.VoxelizeShape) + 1;
		}
		if (instance.SelectiveElements != null)
		{
			for (int k = 0; k < instance.SelectiveElementsCount; k++)
			{
				string s = instance.SelectiveElements[k];
				num += ProtocolParser.GetSize(s) + 1;
			}
		}
		if (instance.IgnoreElements != null)
		{
			for (int l = 0; l < instance.IgnoreElementsCount; l++)
			{
				string s2 = instance.IgnoreElements[l];
				num += ProtocolParser.GetSize(s2) + 2;
			}
		}
		if (instance.QuantityElements != 0)
		{
			num += ProtocolParser.GetSize(instance.QuantityElements) + 1;
		}
		if (instance.QuantityElementsSet != 0)
		{
			num += ProtocolParser.GetSize(instance.QuantityElementsSet) + 1;
		}
		if (instance.Format != 0)
		{
			num += ProtocolParser.GetSize(instance.Format) + 1;
		}
		if (instance.Offsetx != 0)
		{
			num += ProtocolParser.GetSize(instance.Offsetx) + 1;
		}
		if (instance.Offsety != 0)
		{
			num += ProtocolParser.GetSize(instance.Offsety) + 1;
		}
		if (instance.Offsetz != 0)
		{
			num += ProtocolParser.GetSize(instance.Offsetz) + 1;
		}
		if (instance.InsertBakedTextures)
		{
			num += 2;
		}
		if (instance.ScaleAdjust != 0)
		{
			num += ProtocolParser.GetSize(instance.ScaleAdjust) + 2;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_CompositeShape instance)
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

	public static byte[] SerializeToBytes(Packet_CompositeShape instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CompositeShape instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
