using System;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public interface IMapRegion
{
	Dictionary<string, IntDataMap2D> BlockPatchMaps { get; set; }

	IntDataMap2D FlowerMap { get; set; }

	IntDataMap2D ShrubMap { get; set; }

	IntDataMap2D ForestMap { get; set; }

	IntDataMap2D BeachMap { get; set; }

	IntDataMap2D OceanMap { get; set; }

	IntDataMap2D UpheavelMap { get; set; }

	IntDataMap2D LandformMap { get; set; }

	IntDataMap2D ClimateMap { get; set; }

	IntDataMap2D GeologicProvinceMap { get; set; }

	IntDataMap2D[] RockStrata { get; set; }

	[Obsolete("Use Get/Set/RemoveModData instead")]
	Dictionary<string, byte[]> ModData { get; }

	Dictionary<string, IntDataMap2D> ModMaps { get; }

	Dictionary<string, IntDataMap2D> OreMaps { get; }

	IntDataMap2D OreMapVerticalDistortTop { get; }

	IntDataMap2D OreMapVerticalDistortBottom { get; }

	List<GeneratedStructure> GeneratedStructures { get; }

	bool DirtyForSaving { get; set; }

	void SetModdata(string key, byte[] data);

	void RemoveModdata(string key);

	byte[] GetModdata(string key);

	void SetModdata<T>(string key, T data);

	T GetModdata<T>(string key);

	void AddGeneratedStructure(GeneratedStructure generatedStructure);
}
