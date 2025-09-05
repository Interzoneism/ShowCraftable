using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class GenVegetationAndPatches : ModStdWorldGen
{
	private ICoreServerAPI sapi;

	private LCGRandom rnd;

	private IWorldGenBlockAccessor blockAccessor;

	private WgenTreeSupplier treeSupplier;

	private int worldheight;

	private int chunkMapSizeY;

	private int regionChunkSize;

	public Dictionary<string, int> RockBlockIdsByType;

	public BlockPatchConfig bpc;

	public Dictionary<string, BlockPatchConfig> StoryStructurePatches;

	private float forestMod;

	private float shrubMod;

	public Dictionary<string, MapLayerBase> blockPatchMapGens = new Dictionary<string, MapLayerBase>();

	private int noiseSizeDensityMap;

	private int regionSize;

	private const int subSeed = 87698;

	private ushort[] heightmap;

	private int forestUpLeft;

	private int forestUpRight;

	private int forestBotLeft;

	private int forestBotRight;

	private int shrubUpLeft;

	private int shrubUpRight;

	private int shrubBotLeft;

	private int shrubBotRight;

	private int climateUpLeft;

	private int climateUpRight;

	private int climateBotLeft;

	private int climateBotRight;

	private BlockPos tmpPos = new BlockPos();

	private BlockPos chunkBase = new BlockPos();

	private BlockPos chunkend = new BlockPos();

	private List<Cuboidi> structuresIntersectingChunk = new List<Cuboidi>();

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.5;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(initWorldGen, "standard");
			api.Event.InitWorldGenerator(initWorldGenForSuperflat, "superflat");
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Vegetation, "standard");
			api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
			api.Event.MapRegionGeneration(OnMapRegionGen, "superflat");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
		}
	}

	private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams = null)
	{
		int num = sapi.WorldManager.RegionSize / TerraGenConfig.blockPatchesMapScale;
		foreach (KeyValuePair<string, MapLayerBase> blockPatchMapGen in blockPatchMapGens)
		{
			IntDataMap2D intDataMap2D = IntDataMap2D.CreateEmpty();
			intDataMap2D.Size = num + 1;
			intDataMap2D.BottomRightPadding = 1;
			intDataMap2D.Data = blockPatchMapGen.Value.GenLayer(regionX * num, regionZ * num, num + 1, num + 1);
			mapRegion.BlockPatchMaps[blockPatchMapGen.Key] = intDataMap2D;
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		treeSupplier = new WgenTreeSupplier(sapi);
		blockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: true);
	}

	private void initWorldGenForSuperflat()
	{
		treeSupplier.LoadTrees();
	}

	public void initWorldGen()
	{
		regionSize = sapi.WorldManager.RegionSize;
		noiseSizeDensityMap = regionSize / TerraGenConfig.blockPatchesMapScale;
		LoadGlobalConfig(sapi);
		rnd = new LCGRandom(sapi.WorldManager.Seed - 87698);
		treeSupplier.LoadTrees();
		worldheight = sapi.WorldManager.MapSizeY;
		chunkMapSizeY = sapi.WorldManager.MapSizeY / 32;
		regionChunkSize = sapi.WorldManager.RegionSize / 32;
		RockBlockIdsByType = new Dictionary<string, int>();
		RockStrataConfig rockStrataConfig = sapi.Assets.Get("worldgen/rockstrata.json").ToObject<RockStrataConfig>();
		for (int i = 0; i < rockStrataConfig.Variants.Length; i++)
		{
			Block block = sapi.World.GetBlock(rockStrataConfig.Variants[i].BlockCode);
			RockBlockIdsByType[block.LastCodePart()] = block.BlockId;
		}
		IAsset asset = sapi.Assets.Get("worldgen/blockpatches.json");
		bpc = asset.ToObject<BlockPatchConfig>();
		IOrderedEnumerable<KeyValuePair<AssetLocation, BlockPatch[]>> orderedEnumerable = from b in sapi.Assets.GetMany<BlockPatch[]>(sapi.World.Logger, "worldgen/blockpatches/")
			orderby b.Key.ToString()
			select b;
		List<BlockPatch> list = new List<BlockPatch>();
		foreach (KeyValuePair<AssetLocation, BlockPatch[]> item in orderedEnumerable)
		{
			list.AddRange(item.Value);
		}
		bpc.Patches = list.ToArray();
		bpc.ResolveBlockIds(sapi, rockStrataConfig, rnd);
		treeSupplier.treeGenerators.forestFloorSystem.SetBlockPatches(bpc);
		ITreeAttribute worldConfiguration = sapi.WorldManager.SaveGame.WorldConfiguration;
		forestMod = worldConfiguration.GetString("globalForestation").ToFloat();
		blockPatchMapGens.Clear();
		BlockPatch[] patches = bpc.Patches;
		foreach (BlockPatch blockPatch in patches)
		{
			if (blockPatch.MapCode != null && !blockPatchMapGens.ContainsKey(blockPatch.MapCode))
			{
				int hashCode = blockPatch.MapCode.GetHashCode();
				int num2 = sapi.World.Seed + 112897 + hashCode;
				blockPatchMapGens[blockPatch.MapCode] = new MapLayerWobbled(num2, 2, 0.9f, TerraGenConfig.forestMapScale, 4000f, -2500);
			}
		}
		if (!sapi.World.Config.GetAsString("loreContent", "true").ToBool(defaultValue: true))
		{
			return;
		}
		WorldGenStoryStructuresConfig scfg = sapi.ModLoader.GetModSystem<GenStoryStructures>().scfg;
		StoryStructurePatches = new Dictionary<string, BlockPatchConfig>();
		WorldGenStoryStructure[] structures = scfg.Structures;
		foreach (WorldGenStoryStructure worldGenStoryStructure in structures)
		{
			string pathBegins = "worldgen/story/" + worldGenStoryStructure.Code + "/blockpatches/";
			List<KeyValuePair<AssetLocation, BlockPatch[]>> list2 = (from b in sapi.Assets.GetMany<BlockPatch[]>(sapi.World.Logger, pathBegins)
				orderby b.Key.ToString()
				select b).ToList();
			if (list2 == null || list2.Count <= 0)
			{
				continue;
			}
			List<BlockPatch> list3 = new List<BlockPatch>();
			foreach (KeyValuePair<AssetLocation, BlockPatch[]> item2 in list2)
			{
				list3.AddRange(item2.Value);
			}
			StoryStructurePatches[worldGenStoryStructure.Code] = new BlockPatchConfig
			{
				Patches = list3.ToArray()
			};
			StoryStructurePatches[worldGenStoryStructure.Code].ResolveBlockIds(sapi, rockStrataConfig, rnd);
		}
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		blockAccessor.BeginColumn();
		rnd.InitPositionSeed(chunkX, chunkZ);
		IMapChunk mapChunk = chunks[0].MapChunk;
		IntDataMap2D forestMap = mapChunk.MapRegion.ForestMap;
		IntDataMap2D shrubMap = mapChunk.MapRegion.ShrubMap;
		IntDataMap2D climateMap = mapChunk.MapRegion.ClimateMap;
		int num = chunkX % regionChunkSize;
		int num2 = chunkZ % regionChunkSize;
		float num3 = (float)shrubMap.InnerSize / (float)regionChunkSize;
		shrubUpLeft = shrubMap.GetUnpaddedInt((int)((float)num * num3), (int)((float)num2 * num3));
		shrubUpRight = shrubMap.GetUnpaddedInt((int)((float)num * num3 + num3), (int)((float)num2 * num3));
		shrubBotLeft = shrubMap.GetUnpaddedInt((int)((float)num * num3), (int)((float)num2 * num3 + num3));
		shrubBotRight = shrubMap.GetUnpaddedInt((int)((float)num * num3 + num3), (int)((float)num2 * num3 + num3));
		float num4 = (float)forestMap.InnerSize / (float)regionChunkSize;
		forestUpLeft = forestMap.GetUnpaddedInt((int)((float)num * num4), (int)((float)num2 * num4));
		forestUpRight = forestMap.GetUnpaddedInt((int)((float)num * num4 + num4), (int)((float)num2 * num4));
		forestBotLeft = forestMap.GetUnpaddedInt((int)((float)num * num4), (int)((float)num2 * num4 + num4));
		forestBotRight = forestMap.GetUnpaddedInt((int)((float)num * num4 + num4), (int)((float)num2 * num4 + num4));
		float num5 = (float)climateMap.InnerSize / (float)regionChunkSize;
		climateUpLeft = climateMap.GetUnpaddedInt((int)((float)num * num5), (int)((float)num2 * num5));
		climateUpRight = climateMap.GetUnpaddedInt((int)((float)num * num5 + num5), (int)((float)num2 * num5));
		climateBotLeft = climateMap.GetUnpaddedInt((int)((float)num * num5), (int)((float)num2 * num5 + num5));
		climateBotRight = climateMap.GetUnpaddedInt((int)((float)num * num5 + num5), (int)((float)num2 * num5 + num5));
		heightmap = chunks[0].MapChunk.RainHeightMap;
		structuresIntersectingChunk.Clear();
		sapi.World.BlockAccessor.WalkStructures(chunkBase.Set(chunkX * 32, 0, chunkZ * 32), chunkend.Set(chunkX * 32 + 32, chunkMapSizeY * 32, chunkZ * 32 + 32), delegate(GeneratedStructure struc)
		{
			if (struc.SuppressTreesAndShrubs)
			{
				structuresIntersectingChunk.Add(struc.Location.Clone().GrowBy(1, 1, 1));
			}
		});
		if (TerraGenConfig.GenerateVegetation)
		{
			genPatches(chunkX, chunkZ, postPass: false);
			genShrubs(chunkX, chunkZ);
			genTrees(chunkX, chunkZ);
			genPatches(chunkX, chunkZ, postPass: true);
		}
	}

	private void genPatches(int chunkX, int chunkZ, bool postPass)
	{
		int mapSizeY = blockAccessor.MapSizeY;
		LCGRandom lCGRandom = new LCGRandom();
		LCGRandom lCGRandom2 = new LCGRandom();
		lCGRandom.SetWorldSeed(sapi.WorldManager.Seed - 87698);
		lCGRandom.InitPositionSeed(chunkX, chunkZ);
		int num = lCGRandom.NextInt(32);
		int num2 = lCGRandom.NextInt(32);
		int x = num + chunkX * 32;
		int z = num2 + chunkZ * 32;
		tmpPos.Set(x, 0, z);
		bool flag = false;
		string intersectingStructure = GetIntersectingStructure(tmpPos, ModStdWorldGen.SkipPatchesgHashCode);
		BlockPatch[] patchesNonTree;
		if (intersectingStructure != null)
		{
			if (!StoryStructurePatches.TryGetValue(intersectingStructure, out var value))
			{
				return;
			}
			patchesNonTree = value.PatchesNonTree;
			flag = true;
		}
		else
		{
			patchesNonTree = bpc.PatchesNonTree;
		}
		IMapRegion mapregion = sapi?.WorldManager.GetMapRegion(chunkX * 32 / regionSize, chunkZ * 32 / regionSize);
		for (int i = 0; i < patchesNonTree.Length; i++)
		{
			BlockPatch blockPatch = patchesNonTree[i];
			if (blockPatch.PostPass != postPass)
			{
				continue;
			}
			lCGRandom.SetWorldSeed(sapi.WorldManager.Seed - 87698 + i);
			lCGRandom.InitPositionSeed(chunkX, chunkZ);
			float num3 = blockPatch.Chance * bpc.ChanceMultiplier.nextFloat(1f, lCGRandom);
			while (num3-- > lCGRandom.NextFloat())
			{
				num = lCGRandom.NextInt(32);
				num2 = lCGRandom.NextInt(32);
				x = num + chunkX * 32;
				z = num2 + chunkZ * 32;
				int num4 = heightmap[num2 * 32 + num];
				if (num4 <= 0 || num4 >= worldheight - 15)
				{
					continue;
				}
				tmpPos.Set(x, num4, z);
				Block block = blockAccessor.GetBlock(tmpPos, 2);
				float num5 = GameMath.BiLerp(forestUpLeft, forestUpRight, forestBotLeft, forestBotRight, (float)num / 32f, (float)num2 / 32f) / 255f;
				num5 = GameMath.Clamp(num5 + forestMod, 0f, 1f);
				float num6 = GameMath.BiLerp(shrubUpLeft, shrubUpRight, shrubBotLeft, shrubBotRight, (float)num / 32f, (float)num2 / 32f) / 255f;
				num6 = GameMath.Clamp(num6 + shrubMod, 0f, 1f);
				int climate = GameMath.BiLerpRgbColor((float)num / 32f, (float)num2 / 32f, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
				if (!bpc.IsPatchSuitableAt(blockPatch, block, mapSizeY, climate, num4, num5, num6))
				{
					continue;
				}
				intersectingStructure = GetIntersectingStructure(tmpPos, ModStdWorldGen.SkipPatchesgHashCode);
				if ((!flag && intersectingStructure != null) || (blockPatch.MapCode != null && lCGRandom.NextInt(255) > GetPatchDensity(blockPatch.MapCode, x, z, mapregion)))
				{
					continue;
				}
				int value2 = 0;
				bool flag2 = true;
				if (blockPatch.BlocksByRockType != null)
				{
					flag2 = false;
					for (int j = 1; j < 5 && num4 - j > 0; j++)
					{
						string key = blockAccessor.GetBlock(x, num4 - j, z).LastCodePart();
						if (RockBlockIdsByType.TryGetValue(key, out value2))
						{
							flag2 = true;
							break;
						}
					}
				}
				if (flag2)
				{
					lCGRandom2.SetWorldSeed(sapi.WorldManager.Seed - 87698 + i);
					lCGRandom2.InitPositionSeed(x, z);
					blockPatch.Generate(blockAccessor, lCGRandom, x, num4, z, value2, flag);
				}
			}
		}
	}

	private void genShrubs(int chunkX, int chunkZ)
	{
		rnd.InitPositionSeed(chunkX, chunkZ);
		int num = (int)treeSupplier.treeGenProps.shrubsPerChunk.nextFloat(1f, rnd);
		LCGRandom lCGRandom = new LCGRandom();
		while (num > 0)
		{
			lCGRandom.SetWorldSeed(sapi.World.Seed - 87698 + num);
			lCGRandom.InitPositionSeed(chunkX, chunkZ);
			num--;
			int num2 = lCGRandom.NextInt(32);
			int num3 = lCGRandom.NextInt(32);
			int x = num2 + chunkX * 32;
			int z = num3 + chunkZ * 32;
			int num4 = heightmap[num3 * 32 + num2];
			if (num4 <= 0 || num4 >= worldheight - 15)
			{
				continue;
			}
			tmpPos.Set(x, num4, z);
			if (blockAccessor.GetBlock(tmpPos).Fertility == 0)
			{
				continue;
			}
			int climate = GameMath.BiLerpRgbColor((float)num2 / 32f, (float)num3 / 32f, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
			float num5 = GameMath.BiLerp(shrubUpLeft, shrubUpRight, shrubBotLeft, shrubBotRight, (float)num2 / 32f, (float)num3 / 32f);
			num5 = GameMath.Clamp(num5 + 255f * forestMod, 0f, 255f);
			if (lCGRandom.NextFloat() > num5 / 255f * (num5 / 255f))
			{
				continue;
			}
			TreeGenInstance randomShrubGenForClimate = treeSupplier.GetRandomShrubGenForClimate(lCGRandom, climate, (int)num5, num4);
			if (randomShrubGenForClimate == null)
			{
				continue;
			}
			bool flag = true;
			for (int i = 0; i < structuresIntersectingChunk.Count; i++)
			{
				if (structuresIntersectingChunk[i].Contains(tmpPos))
				{
					flag = false;
					break;
				}
			}
			if (flag && GetIntersectingStructure(tmpPos, ModStdWorldGen.SkipShurbsgHashCode) == null)
			{
				if (blockAccessor.GetBlock(tmpPos).Replaceable >= 6000)
				{
					tmpPos.Y--;
				}
				randomShrubGenForClimate.skipForestFloor = true;
				randomShrubGenForClimate.GrowTree(blockAccessor, tmpPos, lCGRandom);
			}
		}
	}

	private void genTrees(int chunkX, int chunkZ)
	{
		rnd.InitPositionSeed(chunkX, chunkZ);
		int num = GameMath.BiLerpRgbColor(0.5f, 0.5f, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
		float num2 = (float)Climate.GetRainFall((num >> 8) & 0xFF, heightmap[528]) / 255f;
		float num3 = (float)((num >> 16) & 0xFF) / 255f;
		float num4 = 1f - num2;
		float num5 = 1f - GameMath.Clamp(2f * (num4 - 0.5f + 1.5f * Math.Max(num3 - 0.6f, 0f)), 0f, 0.8f);
		float num6 = 1f + 3f * Math.Max(0f, num2 - 0.75f);
		int num7 = (int)(treeSupplier.treeGenProps.treesPerChunk.nextFloat(1f, rnd) * num5 * num6);
		int num8 = 0;
		EnumHemisphere hemisphere = sapi.World.Calendar.GetHemisphere(new BlockPos(chunkX * 32 + 16, 0, chunkZ * 32 + 16));
		LCGRandom lCGRandom = new LCGRandom();
		while (num7 > 0)
		{
			lCGRandom.SetWorldSeed(sapi.World.Seed - 87698 + num7);
			lCGRandom.InitPositionSeed(chunkX, chunkZ);
			num7--;
			int num9 = lCGRandom.NextInt(32);
			int num10 = lCGRandom.NextInt(32);
			int x = num9 + chunkX * 32;
			int z = num10 + chunkZ * 32;
			int num11 = heightmap[num10 * 32 + num9];
			if (num11 <= 0 || num11 >= worldheight - 15)
			{
				continue;
			}
			bool isUnderwater = false;
			tmpPos.Set(x, num11, z);
			if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
			{
				isUnderwater = true;
				tmpPos.Y--;
				if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
				{
					tmpPos.Y--;
				}
			}
			if (blockAccessor.GetBlock(tmpPos).Fertility == 0)
			{
				continue;
			}
			float num12 = GameMath.BiLerp(forestUpLeft, forestUpRight, forestBotLeft, forestBotRight, (float)num9 / 32f, (float)num10 / 32f);
			num = GameMath.BiLerpRgbColor((float)num9 / 32f, (float)num10 / 32f, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
			num12 = GameMath.Clamp(num12 + forestMod * 255f, 0f, 255f);
			float num13 = num12 / 255f;
			if (lCGRandom.NextFloat() > Math.Max(0.0025f, num13 * num13) || forestMod <= -1f)
			{
				continue;
			}
			TreeGenInstance randomTreeGenForClimate = treeSupplier.GetRandomTreeGenForClimate(lCGRandom, num, (int)num12, num11, isUnderwater);
			if (randomTreeGenForClimate == null)
			{
				continue;
			}
			bool flag = true;
			for (int i = 0; i < structuresIntersectingChunk.Count; i++)
			{
				if (structuresIntersectingChunk[i].Contains(tmpPos))
				{
					flag = false;
					break;
				}
			}
			if (flag && GetIntersectingStructure(tmpPos, ModStdWorldGen.SkipTreesgHashCode) == null)
			{
				if (blockAccessor.GetBlock(tmpPos).Replaceable >= 6000)
				{
					tmpPos.Y--;
				}
				randomTreeGenForClimate.skipForestFloor = false;
				randomTreeGenForClimate.hemisphere = hemisphere;
				randomTreeGenForClimate.treesInChunkGenerated = num8;
				randomTreeGenForClimate.GrowTree(blockAccessor, tmpPos, lCGRandom);
				num8++;
			}
		}
	}

	public int GetPatchDensity(string code, int posX, int posZ, IMapRegion mapregion)
	{
		if (mapregion == null)
		{
			return 0;
		}
		int num = posX % regionSize;
		int num2 = posZ % regionSize;
		mapregion.BlockPatchMaps.TryGetValue(code, out var value);
		if (value != null)
		{
			float x = GameMath.Clamp((float)num / (float)regionSize * (float)noiseSizeDensityMap, 0f, noiseSizeDensityMap - 1);
			float z = GameMath.Clamp((float)num2 / (float)regionSize * (float)noiseSizeDensityMap, 0f, noiseSizeDensityMap - 1);
			return value.GetUnpaddedColorLerped(x, z);
		}
		return 0;
	}
}
