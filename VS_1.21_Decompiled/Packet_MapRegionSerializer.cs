using System;

public class Packet_MapRegionSerializer
{
	private const int field = 8;

	public static Packet_MapRegion DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_MapRegion packet_MapRegion = new Packet_MapRegion();
		DeserializeLengthDelimited(stream, packet_MapRegion);
		return packet_MapRegion;
	}

	public static Packet_MapRegion DeserializeBuffer(byte[] buffer, int length, Packet_MapRegion instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_MapRegion Deserialize(CitoMemoryStream stream, Packet_MapRegion instance)
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
				instance.RegionX = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.RegionZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
				if (instance.LandformMap == null)
				{
					instance.LandformMap = Packet_IntMapSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_IntMapSerializer.DeserializeLengthDelimited(stream, instance.LandformMap);
				}
				break;
			case 34:
				if (instance.ForestMap == null)
				{
					instance.ForestMap = Packet_IntMapSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_IntMapSerializer.DeserializeLengthDelimited(stream, instance.ForestMap);
				}
				break;
			case 42:
				if (instance.ClimateMap == null)
				{
					instance.ClimateMap = Packet_IntMapSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_IntMapSerializer.DeserializeLengthDelimited(stream, instance.ClimateMap);
				}
				break;
			case 50:
				if (instance.GeologicProvinceMap == null)
				{
					instance.GeologicProvinceMap = Packet_IntMapSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_IntMapSerializer.DeserializeLengthDelimited(stream, instance.GeologicProvinceMap);
				}
				break;
			case 58:
				instance.GeneratedStructuresAdd(Packet_GeneratedStructureSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 66:
				instance.Moddata = ProtocolParser.ReadBytes(stream);
				break;
			case 74:
				if (instance.OceanMap == null)
				{
					instance.OceanMap = Packet_IntMapSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_IntMapSerializer.DeserializeLengthDelimited(stream, instance.OceanMap);
				}
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

	public static Packet_MapRegion DeserializeLengthDelimited(CitoMemoryStream stream, Packet_MapRegion instance)
	{
		int num = ProtocolParser.ReadUInt32(stream);
		int length = stream.GetLength();
		stream.SetLength(stream.Position() + num);
		Packet_MapRegion result = Deserialize(stream, instance);
		stream.SetLength(length);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_MapRegion instance)
	{
		if (instance.RegionX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.RegionX);
		}
		if (instance.RegionZ != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.RegionZ);
		}
		if (instance.LandformMap != null)
		{
			stream.WriteByte(26);
			Packet_IntMapSerializer.SerializeWithSize(stream, instance.LandformMap);
		}
		if (instance.ForestMap != null)
		{
			stream.WriteByte(34);
			Packet_IntMapSerializer.SerializeWithSize(stream, instance.ForestMap);
		}
		if (instance.ClimateMap != null)
		{
			stream.WriteByte(42);
			Packet_IntMapSerializer.SerializeWithSize(stream, instance.ClimateMap);
		}
		if (instance.GeologicProvinceMap != null)
		{
			stream.WriteByte(50);
			Packet_IntMapSerializer.SerializeWithSize(stream, instance.GeologicProvinceMap);
		}
		if (instance.GeneratedStructures != null)
		{
			Packet_GeneratedStructure[] generatedStructures = instance.GeneratedStructures;
			int generatedStructuresCount = instance.GeneratedStructuresCount;
			for (int i = 0; i < generatedStructures.Length && i < generatedStructuresCount; i++)
			{
				stream.WriteByte(58);
				Packet_GeneratedStructureSerializer.SerializeWithSize(stream, generatedStructures[i]);
			}
		}
		if (instance.Moddata != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteBytes(stream, instance.Moddata);
		}
		if (instance.OceanMap != null)
		{
			stream.WriteByte(74);
			Packet_IntMapSerializer.SerializeWithSize(stream, instance.OceanMap);
		}
	}

	public static int GetSize(Packet_MapRegion instance)
	{
		int num = 0;
		if (instance.RegionX != 0)
		{
			num += ProtocolParser.GetSize(instance.RegionX) + 1;
		}
		if (instance.RegionZ != 0)
		{
			num += ProtocolParser.GetSize(instance.RegionZ) + 1;
		}
		if (instance.LandformMap != null)
		{
			int size = Packet_IntMapSerializer.GetSize(instance.LandformMap);
			num += size + ProtocolParser.GetSize(size) + 1;
		}
		if (instance.ForestMap != null)
		{
			int size2 = Packet_IntMapSerializer.GetSize(instance.ForestMap);
			num += size2 + ProtocolParser.GetSize(size2) + 1;
		}
		if (instance.ClimateMap != null)
		{
			int size3 = Packet_IntMapSerializer.GetSize(instance.ClimateMap);
			num += size3 + ProtocolParser.GetSize(size3) + 1;
		}
		if (instance.GeologicProvinceMap != null)
		{
			int size4 = Packet_IntMapSerializer.GetSize(instance.GeologicProvinceMap);
			num += size4 + ProtocolParser.GetSize(size4) + 1;
		}
		if (instance.GeneratedStructures != null)
		{
			for (int i = 0; i < instance.GeneratedStructuresCount; i++)
			{
				int size5 = Packet_GeneratedStructureSerializer.GetSize(instance.GeneratedStructures[i]);
				num += size5 + ProtocolParser.GetSize(size5) + 1;
			}
		}
		if (instance.Moddata != null)
		{
			num += ProtocolParser.GetSize(instance.Moddata) + 1;
		}
		if (instance.OceanMap != null)
		{
			int size6 = Packet_IntMapSerializer.GetSize(instance.OceanMap);
			num += size6 + ProtocolParser.GetSize(size6) + 1;
		}
		instance.size = num;
		return num;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_MapRegion instance)
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

	public static byte[] SerializeToBytes(Packet_MapRegion instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_MapRegion instance)
	{
		byte[] array = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, array.Length);
		stream.Write(array, 0, array.Length);
	}
}
