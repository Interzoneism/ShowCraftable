using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class GenRockStrataNew : ModStdWorldGen
{
	private ICoreServerAPI api;

	private int regionSize;

	private int regionChunkSize;

	public int rockBlockId;

	internal RockStrataConfig strata;

	internal SimplexNoise distort2dx;

	internal SimplexNoise distort2dz;

	internal MapLayerCustomPerlin[] strataNoises;

	private int regionMapSize;

	private Dictionary<int, LerpedWeightedIndex2DMap> ProvinceMapByRegion = new Dictionary<int, LerpedWeightedIndex2DMap>(10);

	private float[] rockGroupMaxThickness = new float[4];

	private int[] rockGroupCurrentThickness = new int[4];

	private IMapChunk mapChunk;

	private ushort[] heightMap;

	private int rdx;

	private int rdz;

	private LerpedWeightedIndex2DMap map;

	private float lerpMapInv;

	private float chunkInRegionX;

	private float chunkInRegionZ;

	private GeologicProvinces provinces = NoiseGeoProvince.provinces;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.1;
	}

	internal void setApi(ICoreServerAPI api)
	{
		this.api = api;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		api.Event.InitWorldGenerator(initWorldGen, "standard");
		api.Event.ChunkColumnGeneration(GenChunkColumn, EnumWorldGenPass.Terrain, "standard");
		api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
	}

	public void initWorldGen()
	{
		initWorldGen(0);
	}

	public void initWorldGen(int seedDiff)
	{
		IAsset asset = api.Assets.Get("worldgen/rockstrata.json");
		strata = asset.ToObject<RockStrataConfig>();
		for (int i = 0; i < strata.Variants.Length; i++)
		{
			strata.Variants[i].Init(api.World);
		}
		LoadGlobalConfig(api);
		regionSize = api.WorldManager.RegionSize;
		regionChunkSize = regionSize / 32;
		int num = regionSize / TerraGenConfig.geoProvMapScale;
		regionMapSize = api.WorldManager.MapSizeX / (32 * num);
		rockBlockId = (ushort)api.WorldManager.GetBlockId(new AssetLocation("rock-granite"));
		distort2dx = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 9876 + seedDiff);
		distort2dz = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 9877 + seedDiff);
		strataNoises = new MapLayerCustomPerlin[strata.Variants.Length];
		for (int j = 0; j < strataNoises.Length; j++)
		{
			RockStratum obj = strata.Variants[j];
			double[] array = (double[])obj.Amplitudes.Clone();
			double[] array2 = (double[])obj.Frequencies.Clone();
			double[] array3 = (double[])obj.Thresholds.Clone();
			if (array.Length != array2.Length || array.Length != array3.Length)
			{
				throw new ArgumentException($"Bug in rockstrata.json, variant {j}: The list of amplitudes ({array.Length} elements), frequencies ({array2.Length} elements) and thresholds ({array3.Length} elements) are not of the same length!");
			}
			for (int k = 0; k < array2.Length; k++)
			{
				array2[k] /= TerraGenConfig.rockStrataOctaveScale;
				array[k] *= api.WorldManager.MapSizeY;
				array3[k] *= api.WorldManager.MapSizeY;
			}
			strataNoises[j] = new MapLayerCustomPerlin(api.World.Seed + 23423 + j + seedDiff, array, array2, array3);
		}
		api.Logger.VerboseDebug("Initialised GenRockStrata");
	}

	private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams = null)
	{
		int num = api.WorldManager.RegionSize / TerraGenConfig.rockStrataScale;
		int num2 = 2;
		mapRegion.RockStrata = new IntDataMap2D[strata.Variants.Length];
		for (int i = 0; i < strata.Variants.Length; i++)
		{
			IntDataMap2D intDataMap2D = new IntDataMap2D();
			mapRegion.RockStrata[i] = intDataMap2D;
			intDataMap2D.Data = strataNoises[i].GenLayer(regionX * num - num2, regionZ * num - num2, num + 2 * num2, num + 2 * num2);
			intDataMap2D.Size = num + 2 * num2;
			intDataMap2D.TopLeftPadding = (intDataMap2D.BottomRightPadding = num2);
		}
	}

	internal void GenChunkColumn(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		preLoad(chunks, chunkX, chunkZ);
		for (int i = 0; i < 32; i++)
		{
			for (int j = 0; j < 32; j++)
			{
				genBlockColumn(chunks, chunkX, chunkZ, i, j);
			}
		}
	}

	public void preLoad(IServerChunk[] chunks, int chunkX, int chunkZ)
	{
		mapChunk = chunks[0].MapChunk;
		heightMap = mapChunk.WorldGenTerrainHeightMap;
		rdx = chunkX % regionChunkSize;
		rdz = chunkZ % regionChunkSize;
		map = GetOrLoadLerpedProvinceMap(chunks[0].MapChunk, chunkX, chunkZ);
		lerpMapInv = 1f / (float)TerraGenConfig.geoProvMapScale;
		chunkInRegionX = (float)(chunkX % regionChunkSize) * lerpMapInv * 32f;
		chunkInRegionZ = (float)(chunkZ % regionChunkSize) * lerpMapInv * 32f;
		provinces = NoiseGeoProvince.provinces;
	}

	public void genBlockColumn(IServerChunk[] chunks, int chunkX, int chunkZ, int lx, int lz)
	{
		ushort num = heightMap[lz * 32 + lx];
		int num2 = 1;
		int num3 = num;
		int num4 = rockBlockId;
		rockGroupMaxThickness[0] = (rockGroupMaxThickness[1] = (rockGroupMaxThickness[2] = (rockGroupMaxThickness[3] = 0f)));
		rockGroupCurrentThickness[0] = (rockGroupCurrentThickness[1] = (rockGroupCurrentThickness[2] = (rockGroupCurrentThickness[3] = 0)));
		float[] array = new float[provinces.Variants.Length];
		map.WeightsAt(chunkInRegionX + (float)lx * lerpMapInv, chunkInRegionZ + (float)lz * lerpMapInv, array);
		for (int i = 0; i < array.Length; i++)
		{
			float num5 = array[i];
			if (num5 != 0f)
			{
				GeologicProvinceRockStrata[] rockStrataIndexed = provinces.Variants[i].RockStrataIndexed;
				rockGroupMaxThickness[0] += rockStrataIndexed[0].ScaledMaxThickness * num5;
				rockGroupMaxThickness[1] += rockStrataIndexed[1].ScaledMaxThickness * num5;
				rockGroupMaxThickness[2] += rockStrataIndexed[2].ScaledMaxThickness * num5;
				rockGroupMaxThickness[3] += rockStrataIndexed[3].ScaledMaxThickness * num5;
			}
		}
		float num6 = (float)distort2dx.Noise(chunkX * 32 + lx, chunkZ * 32 + lz);
		float num7 = (float)distort2dz.Noise(chunkX * 32 + lx, chunkZ * 32 + lz);
		float num8 = GameMath.Clamp((num6 + num7) / 30f, 0.9f, 1.1f);
		int num9 = -1;
		RockStratum rockStratum = null;
		int num10 = 0;
		float num11 = 0f;
		while (num2 <= num3)
		{
			if ((num11 -= 1f) <= 0f)
			{
				num9++;
				if (num9 >= strata.Variants.Length || num9 >= mapChunk.MapRegion.RockStrata.Length)
				{
					break;
				}
				rockStratum = strata.Variants[num9];
				IntDataMap2D intDataMap2D = mapChunk.MapRegion.RockStrata[num9];
				float num12 = (float)intDataMap2D.InnerSize / (float)regionChunkSize;
				num10 = (int)rockStratum.RockGroup;
				float val = rockGroupMaxThickness[num10] * num8 - (float)rockGroupCurrentThickness[num10];
				float val2 = (float)rdx * num12 + num12 * ((float)lx + num6) / 32f;
				float val3 = (float)rdz * num12 + num12 * ((float)lz + num7) / 32f;
				val2 = Math.Max(val2, -1.499f);
				val3 = Math.Max(val3, -1.499f);
				num11 = Math.Min(val, intDataMap2D.GetIntLerpedCorrectly(val2, val3));
				if (rockStratum.RockGroup == EnumRockGroup.Sedimentary)
				{
					num11 -= (float)Math.Max(0, num3 - TerraGenConfig.seaLevel) * 0.5f;
				}
				if (num11 < 2f)
				{
					num11 = -1f;
					continue;
				}
				if (rockStratum.BlockId == num4)
				{
					int num13 = (int)num11;
					rockGroupCurrentThickness[num10] += num13;
					if (rockStratum.GenDir == EnumStratumGenDir.BottomUp)
					{
						num2 += num13;
					}
					else
					{
						num3 -= num13;
					}
					continue;
				}
			}
			rockGroupCurrentThickness[num10]++;
			if (rockStratum.GenDir == EnumStratumGenDir.BottomUp)
			{
				int num14 = num2 / 32;
				int num15 = num2 - num14 * 32;
				int index3d = (32 * num15 + lz) * 32 + lx;
				IChunkBlocks data = chunks[num14].Data;
				if (data.GetBlockIdUnsafe(index3d) == num4)
				{
					data.SetBlockUnsafe(index3d, rockStratum.BlockId);
				}
				num2++;
			}
			else
			{
				int num16 = num3 / 32;
				int num17 = num3 - num16 * 32;
				int index3d2 = (32 * num17 + lz) * 32 + lx;
				IChunkBlocks data2 = chunks[num16].Data;
				if (data2.GetBlockIdUnsafe(index3d2) == num4)
				{
					data2.SetBlockUnsafe(index3d2, rockStratum.BlockId);
				}
				num3--;
			}
		}
	}

	private LerpedWeightedIndex2DMap GetOrLoadLerpedProvinceMap(IMapChunk mapchunk, int chunkX, int chunkZ)
	{
		int key = chunkZ / regionChunkSize * regionMapSize + chunkX / regionChunkSize;
		ProvinceMapByRegion.TryGetValue(key, out var value);
		if (value != null)
		{
			return value;
		}
		return CreateLerpedProvinceMap(mapchunk.MapRegion.GeologicProvinceMap, chunkX / regionChunkSize, chunkZ / regionChunkSize);
	}

	private LerpedWeightedIndex2DMap CreateLerpedProvinceMap(IntDataMap2D geoMap, int regionX, int regionZ)
	{
		int key = regionZ * regionMapSize + regionX;
		return ProvinceMapByRegion[key] = new LerpedWeightedIndex2DMap(geoMap.Data, geoMap.Size, TerraGenConfig.geoProvSmoothingRadius, geoMap.TopLeftPadding, geoMap.BottomRightPadding);
	}
}
