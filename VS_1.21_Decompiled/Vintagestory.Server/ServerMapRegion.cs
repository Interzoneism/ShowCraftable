using System;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

[ProtoContract]
public class ServerMapRegion : IMapRegion
{
	[ProtoMember(1)]
	public IntDataMap2D LandformMap;

	[ProtoMember(2)]
	public IntDataMap2D ForestMap;

	[ProtoMember(3)]
	public IntDataMap2D ClimateMap;

	[ProtoMember(4)]
	public IntDataMap2D GeologicProvinceMap;

	[ProtoMember(5)]
	public IntDataMap2D BushMap;

	[ProtoMember(6)]
	public IntDataMap2D FlowerMap;

	[ProtoMember(8)]
	public Dictionary<string, IntDataMap2D> OreMaps;

	[ProtoMember(9)]
	public Dictionary<string, byte[]> ModData;

	[ProtoMember(10)]
	public Dictionary<string, IntDataMap2D> ModMaps;

	[ProtoMember(11)]
	public List<GeneratedStructure> GeneratedStructures;

	[ProtoMember(12)]
	public IntDataMap2D[] RockStrata;

	[ProtoMember(15)]
	public IntDataMap2D BeachMap;

	[ProtoMember(17)]
	public IntDataMap2D UpheavelMap;

	[ProtoMember(18)]
	public IntDataMap2D OceanMap;

	[ProtoMember(19)]
	public int worldgenVersion;

	public bool DirtyForSaving;

	public bool NeighbourRegionsChecked;

	public long loadedTotalMs;

	[ProtoMember(13)]
	public IntDataMap2D OreMapVerticalDistortTop { get; set; }

	[ProtoMember(14)]
	public IntDataMap2D OreMapVerticalDistortBottom { get; set; }

	[ProtoMember(16)]
	public Dictionary<string, IntDataMap2D> BlockPatchMaps { get; set; }

	IntDataMap2D IMapRegion.ClimateMap
	{
		get
		{
			return ClimateMap;
		}
		set
		{
			ClimateMap = value;
		}
	}

	IntDataMap2D IMapRegion.LandformMap
	{
		get
		{
			return LandformMap;
		}
		set
		{
			LandformMap = value;
		}
	}

	IntDataMap2D IMapRegion.ForestMap
	{
		get
		{
			return ForestMap;
		}
		set
		{
			ForestMap = value;
		}
	}

	IntDataMap2D IMapRegion.BeachMap
	{
		get
		{
			return BeachMap;
		}
		set
		{
			BeachMap = value;
		}
	}

	IntDataMap2D IMapRegion.UpheavelMap
	{
		get
		{
			return UpheavelMap;
		}
		set
		{
			UpheavelMap = value;
		}
	}

	IntDataMap2D IMapRegion.OceanMap
	{
		get
		{
			return OceanMap;
		}
		set
		{
			OceanMap = value;
		}
	}

	IntDataMap2D IMapRegion.ShrubMap
	{
		get
		{
			return BushMap;
		}
		set
		{
			BushMap = value;
		}
	}

	IntDataMap2D IMapRegion.FlowerMap
	{
		get
		{
			return FlowerMap;
		}
		set
		{
			FlowerMap = value;
		}
	}

	IntDataMap2D IMapRegion.GeologicProvinceMap
	{
		get
		{
			return GeologicProvinceMap;
		}
		set
		{
			GeologicProvinceMap = value;
		}
	}

	IntDataMap2D[] IMapRegion.RockStrata
	{
		get
		{
			return RockStrata;
		}
		set
		{
			RockStrata = value;
		}
	}

	bool IMapRegion.DirtyForSaving
	{
		get
		{
			return DirtyForSaving;
		}
		set
		{
			DirtyForSaving = value;
		}
	}

	Dictionary<string, byte[]> IMapRegion.ModData => ModData;

	Dictionary<string, IntDataMap2D> IMapRegion.ModMaps => ModMaps;

	Dictionary<string, IntDataMap2D> IMapRegion.OreMaps => OreMaps;

	List<GeneratedStructure> IMapRegion.GeneratedStructures => GeneratedStructures;

	public static ServerMapRegion CreateNew()
	{
		return new ServerMapRegion
		{
			LandformMap = IntDataMap2D.CreateEmpty(),
			UpheavelMap = IntDataMap2D.CreateEmpty(),
			ForestMap = IntDataMap2D.CreateEmpty(),
			BushMap = IntDataMap2D.CreateEmpty(),
			FlowerMap = IntDataMap2D.CreateEmpty(),
			ClimateMap = IntDataMap2D.CreateEmpty(),
			BeachMap = IntDataMap2D.CreateEmpty(),
			OreMapVerticalDistortTop = IntDataMap2D.CreateEmpty(),
			OreMapVerticalDistortBottom = IntDataMap2D.CreateEmpty(),
			GeologicProvinceMap = IntDataMap2D.CreateEmpty(),
			OreMaps = new Dictionary<string, IntDataMap2D>(),
			ModMaps = new Dictionary<string, IntDataMap2D>(),
			ModData = new Dictionary<string, byte[]>(),
			GeneratedStructures = new List<GeneratedStructure>(),
			BlockPatchMaps = new Dictionary<string, IntDataMap2D>(),
			OceanMap = IntDataMap2D.CreateEmpty(),
			worldgenVersion = 3,
			DirtyForSaving = true
		};
	}

	public void AddGeneratedStructure(GeneratedStructure newStructure)
	{
		List<GeneratedStructure> list = new List<GeneratedStructure>(GeneratedStructures.Count + 1);
		foreach (GeneratedStructure generatedStructure in GeneratedStructures)
		{
			list.Add(generatedStructure);
		}
		list.Add(newStructure);
		GeneratedStructures = list;
		DirtyForSaving = true;
	}

	public static ServerMapRegion FromBytes(byte[] serializedMapRegion)
	{
		ServerMapRegion serverMapRegion = SerializerUtil.Deserialize<ServerMapRegion>(serializedMapRegion);
		if (serverMapRegion.OreMaps == null)
		{
			serverMapRegion.OreMaps = new Dictionary<string, IntDataMap2D>();
		}
		if (serverMapRegion.ModMaps == null)
		{
			serverMapRegion.ModMaps = new Dictionary<string, IntDataMap2D>();
		}
		if (serverMapRegion.ModData == null)
		{
			serverMapRegion.ModData = new Dictionary<string, byte[]>();
		}
		if (serverMapRegion.GeneratedStructures == null)
		{
			serverMapRegion.GeneratedStructures = new List<GeneratedStructure>();
		}
		if (serverMapRegion.BeachMap == null)
		{
			serverMapRegion.BeachMap = IntDataMap2D.CreateEmpty();
		}
		if (serverMapRegion.BlockPatchMaps == null)
		{
			serverMapRegion.BlockPatchMaps = new Dictionary<string, IntDataMap2D>();
		}
		if (serverMapRegion.OceanMap == null)
		{
			serverMapRegion.OceanMap = new IntDataMap2D();
		}
		return serverMapRegion;
	}

	public byte[] ToBytes()
	{
		using FastMemoryStream ms = new FastMemoryStream();
		return ToBytes(ms);
	}

	public byte[] ToBytes(FastMemoryStream ms)
	{
		return SerializerUtil.Serialize(this, ms);
	}

	public Packet_Server ToPacket(int regionX, int regionZ)
	{
		Packet_MapRegion packet_MapRegion = new Packet_MapRegion
		{
			ClimateMap = ToPacket(ClimateMap),
			ForestMap = ToPacket(ForestMap),
			GeologicProvinceMap = ToPacket(GeologicProvinceMap),
			OceanMap = ToPacket(OceanMap),
			LandformMap = ToPacket(LandformMap),
			RegionX = regionX,
			RegionZ = regionZ
		};
		packet_MapRegion.SetGeneratedStructures(ToPacket(GeneratedStructures));
		packet_MapRegion.SetModdata(SerializerUtil.Serialize(ModData));
		return new Packet_Server
		{
			Id = 42,
			MapRegion = packet_MapRegion
		};
	}

	private static Packet_GeneratedStructure[] ToPacket(List<GeneratedStructure> generatedStructures)
	{
		Packet_GeneratedStructure[] array = new Packet_GeneratedStructure[generatedStructures.Count];
		for (int i = 0; i < array.Length; i++)
		{
			GeneratedStructure generatedStructure = generatedStructures[i];
			Packet_GeneratedStructure obj = (array[i] = new Packet_GeneratedStructure());
			obj.X1 = generatedStructure.Location.X1;
			obj.Y1 = generatedStructure.Location.Y1;
			obj.Z1 = generatedStructure.Location.Z1;
			obj.X2 = generatedStructure.Location.X2;
			obj.Y2 = generatedStructure.Location.Y2;
			obj.Z2 = generatedStructure.Location.Z2;
			obj.Code = generatedStructure.Code;
			obj.Group = generatedStructure.Group;
		}
		return array;
	}

	public static Packet_IntMap ToPacket(IntDataMap2D map)
	{
		if (map?.Data == null)
		{
			return new Packet_IntMap
			{
				Data = Array.Empty<int>(),
				DataCount = 0,
				DataLength = 0,
				Size = 0
			};
		}
		return new Packet_IntMap
		{
			Data = map.Data,
			DataCount = map.Data.Length,
			DataLength = map.Data.Length,
			Size = map.Size,
			BottomRightPadding = map.BottomRightPadding,
			TopLeftPadding = map.TopLeftPadding
		};
	}

	public void SetModdata(string key, byte[] data)
	{
		ModData[key] = data;
		DirtyForSaving = true;
	}

	public void RemoveModdata(string key)
	{
		if (ModData.Remove(key))
		{
			DirtyForSaving = true;
		}
	}

	public byte[] GetModdata(string key)
	{
		ModData.TryGetValue(key, out var value);
		return value;
	}

	public void SetModdata<T>(string key, T data)
	{
		SetModdata(key, SerializerUtil.Serialize(data));
	}

	public T GetModdata<T>(string key)
	{
		byte[] moddata = GetModdata(key);
		if (moddata != null)
		{
			return SerializerUtil.Deserialize<T>(moddata);
		}
		return default(T);
	}
}
