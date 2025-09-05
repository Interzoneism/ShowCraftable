using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Vintagestory.ServerMods;

public class GenStructures : ModStdWorldGen
{
	public static bool ReplaceMetaBlocks = true;

	private ICoreServerAPI api;

	private int worldheight;

	private int regionChunkSize;

	private ushort[] heightmap;

	private int climateUpLeft;

	private int climateUpRight;

	private int climateBotLeft;

	private int climateBotRight;

	internal WorldGenStructuresConfig scfg;

	public WorldGenVillageConfig vcfg;

	private LCGRandom strucRand;

	private IWorldGenBlockAccessor worldgenBlockAccessor;

	private WorldGenStructure[] shuffledStructures;

	private Dictionary<string, WorldGenStructure[]> StoryStructures;

	private BlockPos spawnPos;

	public event PeventSchematicAtDelegate OnPreventSchematicPlaceAt;

	public override double ExecuteOrder()
	{
		return 0.3;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		base.StartServerSide(api);
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(initWorldGen, "standard");
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.TerrainFeatures, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
			api.ModLoader.GetModSystem<GenStructuresPosPass>().handler = OnChunkColumnGenPostPass;
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		worldgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: false);
	}

	public bool WouldSchematicOverlapAt(IBlockAccessor blockAccessor, BlockPos pos, Cuboidi schematicLocation, string locationCode)
	{
		if (this.OnPreventSchematicPlaceAt != null)
		{
			Delegate[] invocationList = this.OnPreventSchematicPlaceAt.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				if (((PeventSchematicAtDelegate)invocationList[i])(blockAccessor, pos, schematicLocation, locationCode))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void initWorldGen()
	{
		LoadGlobalConfig(api);
		Block block = api.World.BlockAccessor.GetBlock(new AssetLocation("meta-filler"));
		Block block2 = api.World.BlockAccessor.GetBlock(new AssetLocation("meta-pathway"));
		Block block3 = api.World.BlockAccessor.GetBlock(new AssetLocation("meta-underground"));
		Block block4 = api.World.BlockAccessor.GetBlock(new AssetLocation("meta-aboveground"));
		BlockSchematic.FillerBlockId = block.Id;
		BlockSchematic.PathwayBlockId = block2.Id;
		BlockSchematic.UndergroundBlockId = block3.Id;
		BlockSchematic.AbovegroundBlockId = block4.Id;
		worldheight = api.WorldManager.MapSizeY;
		regionChunkSize = api.WorldManager.RegionSize / 32;
		strucRand = new LCGRandom(api.WorldManager.Seed + 1090);
		LoadStructures();
		LoadVillages();
		if (!api.World.Config.GetAsString("loreContent", "true").ToBool(defaultValue: true))
		{
			return;
		}
		WorldGenStoryStructuresConfig worldGenStoryStructuresConfig = api.ModLoader.GetModSystem<GenStoryStructures>().scfg;
		StoryStructures = new Dictionary<string, WorldGenStructure[]>();
		WorldGenStoryStructure[] structures = worldGenStoryStructuresConfig.Structures;
		foreach (WorldGenStoryStructure worldGenStoryStructure in structures)
		{
			string pathBegins = "worldgen/story/" + worldGenStoryStructure.Code + "/structures.json";
			Dictionary<AssetLocation, WorldGenStructuresConfig> many = api.Assets.GetMany<WorldGenStructuresConfig>(api.Logger, pathBegins);
			List<WorldGenStructure> list = new List<WorldGenStructure>();
			foreach (var (_, worldGenStructuresConfig2) in many)
			{
				worldGenStructuresConfig2.Init(api);
				list.AddRange(worldGenStructuresConfig2.Structures);
			}
			if (list.Count > 0)
			{
				StoryStructures[worldGenStoryStructure.Code] = list.ToArray();
			}
		}
		PlayerSpawnPos defaultSpawn = api.WorldManager.SaveGame.DefaultSpawn;
		if (defaultSpawn != null)
		{
			spawnPos = new BlockPos(defaultSpawn.x, defaultSpawn.y.GetValueOrDefault(), defaultSpawn.z);
		}
		else
		{
			spawnPos = api.World.BlockAccessor.MapSize.AsBlockPos / 2;
		}
	}

	private void LoadStructures()
	{
		Dictionary<AssetLocation, WorldGenStructuresConfig> many = api.Assets.GetMany<WorldGenStructuresConfig>(api.Logger, "worldgen/structures.json");
		scfg = new WorldGenStructuresConfig();
		scfg.ChanceMultiplier = many.First((KeyValuePair<AssetLocation, WorldGenStructuresConfig> v) => v.Key.Domain == "game").Value.ChanceMultiplier;
		scfg.SchematicYOffsets = new Dictionary<string, int>();
		scfg.RocktypeRemapGroups = new Dictionary<string, Dictionary<AssetLocation, AssetLocation>>();
		List<WorldGenStructure> list = new List<WorldGenStructure>();
		foreach (KeyValuePair<AssetLocation, WorldGenStructuresConfig> item in many)
		{
			item.Deconstruct(out var key, out var value);
			WorldGenStructuresConfig worldGenStructuresConfig = value;
			foreach (KeyValuePair<string, Dictionary<AssetLocation, AssetLocation>> rocktypeRemapGroup in worldGenStructuresConfig.RocktypeRemapGroups)
			{
				if (scfg.RocktypeRemapGroups.TryGetValue(rocktypeRemapGroup.Key, out var value2))
				{
					foreach (KeyValuePair<AssetLocation, AssetLocation> item2 in rocktypeRemapGroup.Value)
					{
						item2.Deconstruct(out key, out var value3);
						AssetLocation key2 = key;
						AssetLocation value4 = value3;
						value2.TryAdd(key2, value4);
					}
				}
				else
				{
					scfg.RocktypeRemapGroups.TryAdd(rocktypeRemapGroup.Key, rocktypeRemapGroup.Value);
				}
			}
			foreach (KeyValuePair<string, int> schematicYOffset in worldGenStructuresConfig.SchematicYOffsets)
			{
				scfg.SchematicYOffsets.TryAdd(schematicYOffset.Key, schematicYOffset.Value);
			}
			list.AddRange(worldGenStructuresConfig.Structures);
		}
		scfg.Structures = list.ToArray();
		shuffledStructures = new WorldGenStructure[scfg.Structures.Length];
		scfg.Init(api);
	}

	private void LoadVillages()
	{
		Dictionary<AssetLocation, WorldGenVillageConfig> many = api.Assets.GetMany<WorldGenVillageConfig>(api.Logger, "worldgen/villages.json");
		vcfg = new WorldGenVillageConfig();
		vcfg.ChanceMultiplier = many.First((KeyValuePair<AssetLocation, WorldGenVillageConfig> v) => v.Key.Domain == "game").Value.ChanceMultiplier;
		List<WorldGenVillage> list = new List<WorldGenVillage>();
		foreach (var (_, worldGenVillageConfig2) in many)
		{
			list.AddRange(worldGenVillageConfig2.VillageTypes);
		}
		vcfg.VillageTypes = list.ToArray();
		vcfg.Init(api, scfg);
	}

	private void OnChunkColumnGenPostPass(IChunkColumnGenerateRequest request)
	{
		if (TerraGenConfig.GenerateStructures)
		{
			string intersectingStructure = GetIntersectingStructure(request.ChunkX * 32 + 16, request.ChunkZ * 32 + 16, ModStdWorldGen.SkipStructuresgHashCode);
			IServerChunk[] chunks = request.Chunks;
			int chunkX = request.ChunkX;
			int chunkZ = request.ChunkZ;
			worldgenBlockAccessor.BeginColumn();
			IMapRegion mapRegion = chunks[0].MapChunk.MapRegion;
			DoGenStructures(mapRegion, chunkX, chunkZ, postPass: true, intersectingStructure, request.ChunkGenParams);
			TryGenVillages(mapRegion, chunkX, chunkZ, postPass: true, request.ChunkGenParams);
		}
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		if (TerraGenConfig.GenerateStructures)
		{
			string intersectingStructure = GetIntersectingStructure(request.ChunkX * 32 + 16, request.ChunkZ * 32 + 16, ModStdWorldGen.SkipStructuresgHashCode);
			IServerChunk[] chunks = request.Chunks;
			int chunkX = request.ChunkX;
			int chunkZ = request.ChunkZ;
			worldgenBlockAccessor.BeginColumn();
			IMapRegion mapRegion = chunks[0].MapChunk.MapRegion;
			IntDataMap2D climateMap = mapRegion.ClimateMap;
			int num = chunkX % regionChunkSize;
			int num2 = chunkZ % regionChunkSize;
			float num3 = (float)climateMap.InnerSize / (float)regionChunkSize;
			climateUpLeft = climateMap.GetUnpaddedInt((int)((float)num * num3), (int)((float)num2 * num3));
			climateUpRight = climateMap.GetUnpaddedInt((int)((float)num * num3 + num3), (int)((float)num2 * num3));
			climateBotLeft = climateMap.GetUnpaddedInt((int)((float)num * num3), (int)((float)num2 * num3 + num3));
			climateBotRight = climateMap.GetUnpaddedInt((int)((float)num * num3 + num3), (int)((float)num2 * num3 + num3));
			heightmap = chunks[0].MapChunk.WorldGenTerrainHeightMap;
			DoGenStructures(mapRegion, chunkX, chunkZ, postPass: false, intersectingStructure, request.ChunkGenParams);
			if (intersectingStructure == null)
			{
				TryGenVillages(mapRegion, chunkX, chunkZ, postPass: false, request.ChunkGenParams);
			}
		}
	}

	private void DoGenStructures(IMapRegion region, int chunkX, int chunkZ, bool postPass, string locationCode, ITreeAttribute chunkGenParams = null)
	{
		if (locationCode != null)
		{
			if (!StoryStructures.TryGetValue(locationCode, out var value))
			{
				return;
			}
			shuffledStructures = new WorldGenStructure[value.Length];
			for (int i = 0; i < value.Length; i++)
			{
				shuffledStructures[i] = value[i];
			}
		}
		else
		{
			shuffledStructures = new WorldGenStructure[scfg.Structures.Length];
			for (int j = 0; j < shuffledStructures.Length; j++)
			{
				shuffledStructures[j] = scfg.Structures[j];
			}
		}
		BlockPos blockPos = new BlockPos();
		ITreeAttribute treeAttribute = null;
		ITreeAttribute treeAttribute2 = null;
		StoryStructureLocation storyStructureLocation = null;
		if (chunkGenParams?["structureChanceModifier"] != null)
		{
			treeAttribute = chunkGenParams["structureChanceModifier"] as TreeAttribute;
		}
		if (chunkGenParams?["structureMaxCount"] != null)
		{
			treeAttribute2 = chunkGenParams["structureMaxCount"] as TreeAttribute;
		}
		strucRand.InitPositionSeed(chunkX, chunkZ);
		shuffledStructures.Shuffle(strucRand);
		for (int k = 0; k < shuffledStructures.Length; k++)
		{
			WorldGenStructure worldGenStructure = shuffledStructures[k];
			if (worldGenStructure.PostPass != postPass)
			{
				continue;
			}
			float num = worldGenStructure.Chance * scfg.ChanceMultiplier;
			int num2 = 9999;
			if (treeAttribute != null)
			{
				num *= treeAttribute.GetFloat(worldGenStructure.Code);
			}
			if (treeAttribute2 != null)
			{
				num2 = treeAttribute2.GetInt(worldGenStructure.Code, 9999);
			}
			while (num-- > strucRand.NextFloat() && num2 > 0)
			{
				int num3 = strucRand.NextInt(32);
				int num4 = strucRand.NextInt(32);
				int num5 = heightmap[num4 * 32 + num3];
				if (num5 <= 0 || num5 >= worldheight - 15)
				{
					continue;
				}
				if (worldGenStructure.Placement == EnumStructurePlacement.Underground)
				{
					if (worldGenStructure.Depth != null)
					{
						blockPos.Set(chunkX * 32 + num3, num5 - (int)worldGenStructure.Depth.nextFloat(1f, strucRand), chunkZ * 32 + num4);
					}
					else
					{
						blockPos.Set(chunkX * 32 + num3, 8 + strucRand.NextInt(Math.Max(1, num5 - 8 - 5)), chunkZ * 32 + num4);
					}
				}
				else
				{
					blockPos.Set(chunkX * 32 + num3, num5, chunkZ * 32 + num4);
				}
				if (blockPos.Y <= 0 || !BlockSchematicStructure.SatisfiesMinSpawnDistance(worldGenStructure.MinSpawnDistance, blockPos, spawnPos))
				{
					continue;
				}
				if (locationCode != null)
				{
					storyStructureLocation = GetIntersectingStructure(chunkX * 32 + 16, chunkZ * 32 + 16);
					Dictionary<string, int> schematicsSpawned = storyStructureLocation.SchematicsSpawned;
					if ((schematicsSpawned != null && schematicsSpawned.TryGetValue(worldGenStructure.Group, out var value2) && value2 >= worldGenStructure.StoryLocationMaxAmount) || (worldGenStructure.StoryMaxFromCenter != 0 && !blockPos.InRangeHorizontally(storyStructureLocation.CenterPos.X, storyStructureLocation.CenterPos.Z, worldGenStructure.StoryMaxFromCenter)))
					{
						continue;
					}
				}
				if (!worldGenStructure.TryGenerate(worldgenBlockAccessor, api.World, blockPos, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, locationCode))
				{
					continue;
				}
				if (locationCode != null && storyStructureLocation != null)
				{
					Dictionary<string, int> schematicsSpawned2 = storyStructureLocation.SchematicsSpawned;
					if (schematicsSpawned2 != null && schematicsSpawned2.TryGetValue(worldGenStructure.Group, out var value3))
					{
						storyStructureLocation.SchematicsSpawned[worldGenStructure.Group] = value3 + 1;
					}
					else
					{
						StoryStructureLocation storyStructureLocation2 = storyStructureLocation;
						if (storyStructureLocation2.SchematicsSpawned == null)
						{
							storyStructureLocation2.SchematicsSpawned = new Dictionary<string, int>();
						}
						storyStructureLocation.SchematicsSpawned[worldGenStructure.Group] = 1;
					}
				}
				Cuboidi lastPlacedSchematicLocation = worldGenStructure.LastPlacedSchematicLocation;
				string code = ((worldGenStructure.LastPlacedSchematic == null) ? "" : worldGenStructure.LastPlacedSchematic.FromFileName) + "/" + worldGenStructure.Code;
				region.AddGeneratedStructure(new GeneratedStructure
				{
					Code = code,
					Group = worldGenStructure.Group,
					Location = lastPlacedSchematicLocation.Clone(),
					SuppressTreesAndShrubs = worldGenStructure.SuppressTrees,
					SuppressRivulets = worldGenStructure.SuppressWaterfalls
				});
				if (worldGenStructure.BuildProtected)
				{
					api.World.Claims.Add(new LandClaim
					{
						Areas = new List<Cuboidi> { lastPlacedSchematicLocation.Clone() },
						Description = worldGenStructure.BuildProtectionDesc,
						ProtectionLevel = worldGenStructure.ProtectionLevel,
						LastKnownOwnerName = worldGenStructure.BuildProtectionName,
						AllowUseEveryone = worldGenStructure.AllowUseEveryone,
						AllowTraverseEveryone = worldGenStructure.AllowTraverseEveryone
					});
				}
				num2--;
			}
		}
	}

	public void TryGenVillages(IMapRegion region, int chunkX, int chunkZ, bool postPass, ITreeAttribute chunkGenParams = null)
	{
		strucRand.InitPositionSeed(chunkX, chunkZ);
		for (int i = 0; i < vcfg.VillageTypes.Length; i++)
		{
			WorldGenVillage worldGenVillage = vcfg.VillageTypes[i];
			if (worldGenVillage.PostPass == postPass)
			{
				float num = worldGenVillage.Chance * vcfg.ChanceMultiplier;
				while (num-- > strucRand.NextFloat())
				{
					GenVillage(worldgenBlockAccessor, region, worldGenVillage, chunkX, chunkZ);
				}
			}
		}
	}

	public bool GenVillage(IBlockAccessor blockAccessor, IMapRegion region, WorldGenVillage struc, int chunkX, int chunkZ)
	{
		BlockPos blockPos = new BlockPos();
		int num = 16;
		int num2 = 16;
		int num3 = heightmap[num2 * 32 + num];
		if (num3 <= 0 || num3 >= worldheight - 15)
		{
			return false;
		}
		blockPos.Set(chunkX * 32 + num, num3, chunkZ * 32 + num2);
		return struc.TryGenerate(blockAccessor, api.World, blockPos, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, delegate(Cuboidi loc, BlockSchematicStructure schematic)
		{
			string code = ((schematic == null) ? "" : schematic.FromFileName) + "/" + struc.Code;
			region.AddGeneratedStructure(new GeneratedStructure
			{
				Code = code,
				Group = struc.Group,
				Location = loc.Clone()
			});
			if (struc.BuildProtected)
			{
				api.World.Claims.Add(new LandClaim
				{
					Areas = new List<Cuboidi> { loc.Clone() },
					Description = struc.BuildProtectionDesc,
					ProtectionLevel = struc.ProtectionLevel,
					LastKnownOwnerName = struc.BuildProtectionName,
					AllowUseEveryone = struc.AllowUseEveryone,
					AllowTraverseEveryone = struc.AllowTraverseEveryone
				});
			}
		}, spawnPos);
	}
}
