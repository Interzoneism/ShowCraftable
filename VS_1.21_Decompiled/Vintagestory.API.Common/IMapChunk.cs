using System;
using System.Collections.Concurrent;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public interface IMapChunk
{
	ConcurrentDictionary<Vec2i, float> SnowAccum { get; }

	IMapRegion MapRegion { get; }

	EnumWorldGenPass CurrentPass { get; set; }

	byte[] CaveHeightDistort { get; set; }

	ushort[] RainHeightMap { get; }

	ushort[] WorldGenTerrainHeightMap { get; }

	int[] TopRockIdMap { get; }

	ushort[] SedimentaryThicknessMap { get; }

	ushort YMax { get; set; }

	[Obsolete("Use SetModData instead")]
	void SetData(string key, byte[] data);

	[Obsolete("Use GetModData instead")]
	byte[] GetData(string key);

	void SetModdata(string key, byte[] data);

	void RemoveModdata(string key);

	byte[] GetModdata(string key);

	void SetModdata<T>(string key, T data);

	T GetModdata<T>(string key, T defaultValue = default(T));

	void MarkFresh();

	void MarkDirty();
}
