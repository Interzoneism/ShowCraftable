using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class GenStoryStructures : ModStdWorldGen
{
	internal WorldGenStoryStructuresConfig scfg;

	private LCGRandom strucRand;

	private LCGRandom grassRand;

	public OrderedDictionary<string, StoryStructureLocation> storyStructureInstances = new OrderedDictionary<string, StoryStructureLocation>();

	public bool StoryStructureInstancesDirty;

	private IWorldGenBlockAccessor worldgenBlockAccessor;

	private ICoreServerAPI api;

	private bool genStoryStructures;

	public BlockLayerConfig blockLayerConfig;

	private Cuboidi tmpCuboid = new Cuboidi();

	private int mapheight;

	private ClampedSimplexNoise grassDensity;

	private ClampedSimplexNoise grassHeight;

	public SimplexNoise distort2dx;

	public SimplexNoise distort2dz;

	private bool FailedToGenerateLocation;

	private IServerNetworkChannel serverChannel;

	public override double ExecuteOrder()
	{
		return 0.2;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		base.StartServerSide(api);
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.PlayerJoin += OnPlayerJoined;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(InitWorldGen, "standard");
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Vegetation, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
			api.Event.WorldgenHook(GenerateHookStructure, "standard", "genHookStructure");
		}
		api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, delegate
		{
			if (genStoryStructures)
			{
				api.ChatCommands.GetOrCreate("wgen").BeginSubCommand("story").BeginSubCommand("tp")
					.WithRootAlias("tpstoryloc")
					.WithDescription("Teleport to a story structure instance")
					.RequiresPrivilege(Privilege.controlserver)
					.RequiresPlayer()
					.WithArgs(api.ChatCommands.Parsers.WordRange("code", scfg.Structures.Select((WorldGenStoryStructure s) => s.Code).ToArray()))
					.HandleWith(OnTpStoryLoc)
					.EndSubCommand()
					.EndSubCommand();
			}
		});
		api.ChatCommands.GetOrCreate("wgen").BeginSubCommand("story").BeginSubCommand("setpos")
			.WithRootAlias("setstorystrucpos")
			.WithDescription("Set the location of a story structure")
			.RequiresPrivilege(Privilege.controlserver)
			.WithArgs(api.ChatCommands.Parsers.Word("code"), api.ChatCommands.Parsers.WorldPosition("position"), api.ChatCommands.Parsers.OptionalBool("confirm"))
			.HandleWith(OnSetStoryStructurePos)
			.EndSubCommand()
			.BeginSubCommand("removeschematiccount")
			.WithAlias("rmsc")
			.WithDescription("Remove the story structures schematic count, which allows on regen to spawn them again")
			.RequiresPrivilege(Privilege.controlserver)
			.WithArgs(api.ChatCommands.Parsers.Word("code"))
			.HandleWith(OnRemoveStorySchematics)
			.EndSubCommand()
			.BeginSubCommand("listmissing")
			.WithDescription("List story locations that failed to generate.")
			.RequiresPrivilege(Privilege.controlserver)
			.HandleWith(OnListMissingStructures)
			.EndSubCommand()
			.EndSubCommand();
		serverChannel = api.Network.RegisterChannel("StoryGenFailed");
		serverChannel.RegisterMessageType<StoryGenFailed>();
	}

	private TextCommandResult OnListMissingStructures(TextCommandCallingArgs args)
	{
		List<string> missingStructures = GetMissingStructures();
		if (missingStructures.Count > 0)
		{
			string text = string.Join(",", missingStructures);
			if (args.Caller.Player is IServerPlayer serverPlayer)
			{
				StoryGenFailed message = new StoryGenFailed
				{
					MissingStructures = missingStructures
				};
				serverChannel.SendPacket(message, serverPlayer);
			}
			return TextCommandResult.Success("Missing story locations: " + text);
		}
		return TextCommandResult.Success("No story locations are missing.");
	}

	private List<string> GetMissingStructures()
	{
		List<string> data = api.WorldManager.SaveGame.GetData<List<string>>("attemptedToGenerateStoryLocation");
		List<string> list = new List<string>();
		if (data != null)
		{
			foreach (string item in data)
			{
				if (!storyStructureInstances.ContainsKey(item))
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	private void OnPlayerJoined(IServerPlayer byplayer)
	{
		if (FailedToGenerateLocation && byplayer.HasPrivilege(Privilege.controlserver))
		{
			StoryGenFailed message = new StoryGenFailed
			{
				MissingStructures = GetMissingStructures()
			};
			serverChannel.SendPacket(message, byplayer);
		}
	}

	private TextCommandResult OnRemoveStorySchematics(TextCommandCallingArgs args)
	{
		string text = (string)args[0];
		if (storyStructureInstances.TryGetValue(text, out var value))
		{
			value.SchematicsSpawned = null;
			StoryStructureInstancesDirty = true;
			return TextCommandResult.Success("Ok, removed the story structure locations " + text + " schematics counter.");
		}
		return TextCommandResult.Error("No such story structure exist in assets");
	}

	private TextCommandResult OnSetStoryStructurePos(TextCommandCallingArgs args)
	{
		WorldGenStoryStructure worldGenStoryStructure = scfg.Structures.FirstOrDefault((WorldGenStoryStructure st) => st.Code == (string)args[0]);
		if (worldGenStoryStructure == null)
		{
			return TextCommandResult.Error("No such story structure exist in assets");
		}
		if (!(bool)args[2])
		{
			double num = Math.Ceiling((float)worldGenStoryStructure.LandformRadius / 32f) + 3.0;
			return TextCommandResult.Success($"Ok, will move the story structure location to this position. Make sure that at least {worldGenStoryStructure.LandformRadius + 32} blocks around you are unoccupied.\n Add 'true' to the command to confirm.\n After this is done, you will have to regenerate chunks in this area, \ne.g. via <a href=\"chattype:///wgen delr {num}\">/wgen delr {num}</a> to delete {num * 32.0} blocks in all directions. They will then generate again as you move around.");
		}
		BlockPos asBlockPos = ((Vec3d)args[1]).AsBlockPos;
		asBlockPos.Y = 1;
		GenMaps modSystem = api.ModLoader.GetModSystem<GenMaps>();
		BlockSchematicPartial schematicData = worldGenStoryStructure.schematicData;
		int num2 = asBlockPos.X - schematicData.SizeX / 2;
		int num3 = asBlockPos.Z - schematicData.SizeZ / 2;
		Cuboidi location = new Cuboidi(num2, asBlockPos.Y, num3, num2 + schematicData.SizeX - 1, asBlockPos.Y + schematicData.SizeY - 1, num3 + schematicData.SizeZ - 1);
		storyStructureInstances[worldGenStoryStructure.Code] = new StoryStructureLocation
		{
			Code = worldGenStoryStructure.Code,
			CenterPos = asBlockPos,
			Location = location,
			LandformRadius = worldGenStoryStructure.LandformRadius,
			GenerationRadius = worldGenStoryStructure.GenerationRadius,
			SkipGenerationFlags = worldGenStoryStructure.SkipGenerationFlags
		};
		if (worldGenStoryStructure.RequireLandform != null)
		{
			modSystem.ForceLandformAt(new ForceLandform
			{
				CenterPos = asBlockPos,
				Radius = worldGenStoryStructure.LandformRadius,
				LandformCode = worldGenStoryStructure.RequireLandform
			});
		}
		if (worldGenStoryStructure.ForceTemperature.HasValue || worldGenStoryStructure.ForceRain.HasValue)
		{
			modSystem.ForceClimateAt(new ForceClimate
			{
				Radius = storyStructureInstances[worldGenStoryStructure.Code].LandformRadius,
				CenterPos = storyStructureInstances[worldGenStoryStructure.Code].CenterPos,
				Climate = (Climate.DescaleTemperature(((float?)worldGenStoryStructure.ForceTemperature) ?? 0f) << 16) + (worldGenStoryStructure.ForceRain.GetValueOrDefault() << 8)
			});
		}
		StoryStructureInstancesDirty = true;
		return TextCommandResult.Success("Ok, story structure location moved to this position. Regenerating chunks at the location should make it appear now.");
	}

	public void InitWorldGen()
	{
		genStoryStructures = api.World.Config.GetAsString("loreContent", "true").ToBool(defaultValue: true);
		if (!genStoryStructures)
		{
			return;
		}
		strucRand = new LCGRandom(api.WorldManager.Seed + 1095);
		Dictionary<AssetLocation, WorldGenStoryStructuresConfig> many = api.Assets.GetMany<WorldGenStoryStructuresConfig>(api.Logger, "worldgen/storystructures.json");
		scfg = new WorldGenStoryStructuresConfig();
		scfg.SchematicYOffsets = new Dictionary<string, int>();
		scfg.RocktypeRemapGroups = new Dictionary<string, Dictionary<AssetLocation, AssetLocation>>();
		List<WorldGenStoryStructure> list = new List<WorldGenStoryStructure>();
		foreach (KeyValuePair<AssetLocation, WorldGenStoryStructuresConfig> item in many)
		{
			item.Deconstruct(out var key, out var value);
			WorldGenStoryStructuresConfig worldGenStoryStructuresConfig = value;
			foreach (KeyValuePair<string, Dictionary<AssetLocation, AssetLocation>> rocktypeRemapGroup in worldGenStoryStructuresConfig.RocktypeRemapGroups)
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
			foreach (KeyValuePair<string, int> schematicYOffset in worldGenStoryStructuresConfig.SchematicYOffsets)
			{
				scfg.SchematicYOffsets.TryAdd(schematicYOffset.Key, schematicYOffset.Value);
			}
			list.AddRange(worldGenStoryStructuresConfig.Structures);
		}
		scfg.Structures = list.ToArray();
		grassRand = new LCGRandom(api.WorldManager.Seed);
		grassDensity = new ClampedSimplexNoise(new double[1] { 4.0 }, new double[1] { 0.5 }, grassRand.NextInt());
		grassHeight = new ClampedSimplexNoise(new double[1] { 1.5 }, new double[1] { 0.5 }, grassRand.NextInt());
		distort2dx = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 20980);
		distort2dz = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 20981);
		mapheight = api.WorldManager.MapSizeY;
		blockLayerConfig = BlockLayerConfig.GetInstance(api);
		scfg.Init(api, blockLayerConfig.RockStrata, blockLayerConfig);
		double num = api.World.Config.GetDecimal("storyStructuresDistScaling", 1.0);
		WorldGenStoryStructure[] structures = scfg.Structures;
		foreach (WorldGenStoryStructure obj in structures)
		{
			obj.MinSpawnDistX = (int)((double)obj.MinSpawnDistX * num);
			obj.MinSpawnDistZ = (int)((double)obj.MinSpawnDistZ * num);
			obj.MaxSpawnDistX = (int)((double)obj.MaxSpawnDistX * num);
			obj.MaxSpawnDistZ = (int)((double)obj.MaxSpawnDistZ * num);
		}
		DetermineStoryStructures();
		strucRand.SetWorldSeed(api.WorldManager.Seed + 1095);
	}

	private TextCommandResult OnTpStoryLoc(TextCommandCallingArgs args)
	{
		if (!(args[0] is string text))
		{
			return TextCommandResult.Success();
		}
		if (storyStructureInstances.TryGetValue(text, out var value))
		{
			BlockPos blockPos = value.CenterPos.Copy();
			blockPos.Y = (value.Location.Y1 + value.Location.Y2) / 2;
			args.Caller.Entity.TeleportTo(blockPos);
			return TextCommandResult.Success("Teleporting to " + text);
		}
		return TextCommandResult.Success("No such story structure, " + text);
	}

	public string GetStoryStructureCodeAt(int x, int z, int category)
	{
		if (storyStructureInstances == null)
		{
			return null;
		}
		foreach (KeyValuePair<string, StoryStructureLocation> storyStructureInstance in storyStructureInstances)
		{
			storyStructureInstance.Deconstruct(out var _, out var value);
			StoryStructureLocation storyStructureLocation = value;
			int value2;
			bool flag = storyStructureLocation.SkipGenerationFlags.TryGetValue(category, out value2);
			if (storyStructureLocation.Location.Contains(x, z) && flag)
			{
				return storyStructureLocation.Code;
			}
			if (value2 > 0 && storyStructureLocation.CenterPos.HorDistanceSqTo(x, z) < (float)(value2 * value2))
			{
				return storyStructureLocation.Code;
			}
		}
		return null;
	}

	public StoryStructureLocation GetStoryStructureAt(int x, int z)
	{
		if (storyStructureInstances == null)
		{
			return null;
		}
		foreach (var (_, storyStructureLocation2) in storyStructureInstances)
		{
			if (storyStructureLocation2.Location.Contains(x, z))
			{
				return storyStructureLocation2;
			}
			int landformRadius = storyStructureLocation2.LandformRadius;
			if (landformRadius > 0 && storyStructureLocation2.CenterPos.HorDistanceSqTo(x, z) < (float)(landformRadius * landformRadius))
			{
				return storyStructureLocation2;
			}
		}
		return null;
	}

	public string GetStoryStructureCodeAt(Vec3d position, int skipCategory)
	{
		return GetStoryStructureCodeAt((int)position.X, (int)position.Z, skipCategory);
	}

	public string GetStoryStructureCodeAt(BlockPos position, int skipCategory)
	{
		return GetStoryStructureCodeAt(position.X, position.Z, skipCategory);
	}

	protected void DetermineStoryStructures()
	{
		List<string> list = api.WorldManager.SaveGame.GetData<List<string>>("attemptedToGenerateStoryLocation");
		if (list == null)
		{
			list = new List<string>();
		}
		int num = 0;
		WorldGenStoryStructure[] structures = scfg.Structures;
		foreach (WorldGenStoryStructure worldGenStoryStructure in structures)
		{
			if (storyStructureInstances.TryGetValue(worldGenStoryStructure.Code, out var value))
			{
				value.LandformRadius = worldGenStoryStructure.LandformRadius;
				value.GenerationRadius = worldGenStoryStructure.GenerationRadius;
				value.SkipGenerationFlags = worldGenStoryStructure.SkipGenerationFlags;
				BlockSchematicPartial schematicData = worldGenStoryStructure.schematicData;
				int num2 = value.CenterPos.X - schematicData.SizeX / 2;
				int num3 = value.CenterPos.Z - schematicData.SizeZ / 2;
				Cuboidi location = new Cuboidi(num2, value.CenterPos.Y, num3, num2 + schematicData.SizeX, value.CenterPos.Y + schematicData.SizeY, num3 + schematicData.SizeZ);
				value.Location = location;
			}
			else if (!list.Contains(worldGenStoryStructure.Code))
			{
				strucRand.SetWorldSeed(api.WorldManager.Seed + 1095 + num);
				TryAddStoryLocation(worldGenStoryStructure);
				list.Add(worldGenStoryStructure.Code);
			}
			num++;
		}
		StoryStructureInstancesDirty = true;
		api.WorldManager.SaveGame.StoreData("attemptedToGenerateStoryLocation", list);
		SetupForceLandform();
	}

	private void TryAddStoryLocation(WorldGenStoryStructure storyStructure)
	{
		BlockPos blockPos = null;
		StoryStructureLocation value = null;
		if (!string.IsNullOrEmpty(storyStructure.DependsOnStructure))
		{
			if (storyStructure.DependsOnStructure == "spawn")
			{
				blockPos = new BlockPos(api.World.BlockAccessor.MapSizeX / 2, 0, api.World.BlockAccessor.MapSizeZ / 2, 0);
			}
			else if (storyStructureInstances.TryGetValue(storyStructure.DependsOnStructure, out value))
			{
				blockPos = value.CenterPos.Copy();
			}
		}
		if (blockPos == null)
		{
			FailedToGenerateLocation = true;
			api.Logger.Error("Could not find dependent structure " + storyStructure.DependsOnStructure + " to generate structure: " + storyStructure.Code + ". Make sure that the dependent structure is before this one in the list.");
			api.Logger.Error($"You will need to add them manually by running /wgen story setpos {storyStructure.DependsOnStructure} and /wgen story setpos {storyStructure.Code} at two different locations about at least 1000 blocks apart.");
			return;
		}
		int num = value?.DirX ?? ((!((double)strucRand.NextFloat() > 0.5)) ? 1 : (-1));
		int num2 = ((!((double)strucRand.NextFloat() > 0.5)) ? 1 : (-1));
		int num3 = storyStructure.MinSpawnDistX + strucRand.NextInt(storyStructure.MaxSpawnDistX + 1 - storyStructure.MinSpawnDistX);
		int num4 = storyStructure.MinSpawnDistZ + strucRand.NextInt(storyStructure.MaxSpawnDistZ + 1 - storyStructure.MinSpawnDistZ);
		BlockSchematicPartial schematicData = storyStructure.schematicData;
		EnumStructurePlacement placement = storyStructure.Placement;
		bool flag = (uint)placement <= 1u;
		int y = ((!flag) ? 1 : (api.World.SeaLevel + schematicData.OffsetY));
		BlockPos blockPos2 = new BlockPos(blockPos.X + num3 * num, y, blockPos.Z + num4 * num2, 0);
		int num5 = Math.Max(storyStructure.LandformRadius, storyStructure.GenerationRadius);
		int num6 = blockPos2.X - num5;
		int num7 = blockPos2.X + num5;
		int num8 = blockPos2.Z - num5;
		int num9 = blockPos2.Z + num5;
		int num10 = num6 / api.WorldManager.RegionSize;
		int num11 = num7 / api.WorldManager.RegionSize;
		int num12 = num8 / api.WorldManager.RegionSize;
		int num13 = num9 / api.WorldManager.RegionSize;
		bool flag2 = false;
		for (int i = num10; i <= num11; i++)
		{
			for (int j = num12; j <= num13; j++)
			{
				if (api.WorldManager.BlockingTestMapRegionExists(i, j))
				{
					flag2 = true;
				}
			}
		}
		int num14 = num6 / 32;
		int num15 = num7 / 32;
		int num16 = num8 / 32;
		int num17 = num9 / 32;
		if (flag2)
		{
			for (int k = num14; k <= num15; k++)
			{
				for (int l = num16; l <= num17; l++)
				{
					if (!api.WorldManager.BlockingTestMapChunkExists(k, l))
					{
						continue;
					}
					IServerChunk[] array = api.WorldManager.BlockingLoadChunkColumn(k, l);
					if (array == null)
					{
						continue;
					}
					IServerChunk[] array2 = array;
					foreach (IServerChunk serverChunk in array2)
					{
						if (serverChunk.BlocksPlaced > 0 || serverChunk.BlocksRemoved > 0)
						{
							FailedToGenerateLocation = true;
							api.Logger.Error($"Map chunk in area of story location {storyStructure.Code} contains player edits, can not automatically add it your world. You can add it manually running /wgen story setpos {storyStructure.Code} at a location that seems suitable for you.");
							return;
						}
						serverChunk.Dispose();
					}
				}
			}
		}
		if (flag2)
		{
			for (int n = num10; n <= num11; n++)
			{
				for (int num18 = num12; num18 <= num13; num18++)
				{
					api.WorldManager.DeleteMapRegion(n, num18);
				}
			}
			for (int num19 = num14; num19 <= num15; num19++)
			{
				for (int num20 = num16; num20 <= num17; num20++)
				{
					api.WorldManager.DeleteChunkColumn(num19, num20);
				}
			}
		}
		int num21 = blockPos2.X - schematicData.SizeX / 2;
		int num22 = blockPos2.Z - schematicData.SizeZ / 2;
		Cuboidi location = new Cuboidi(num21, blockPos2.Y, num22, num21 + schematicData.SizeX, blockPos2.Y + schematicData.SizeY, num22 + schematicData.SizeZ);
		storyStructureInstances[storyStructure.Code] = new StoryStructureLocation
		{
			Code = storyStructure.Code,
			CenterPos = blockPos2,
			Location = location,
			LandformRadius = storyStructure.LandformRadius,
			GenerationRadius = storyStructure.GenerationRadius,
			DirX = num,
			SkipGenerationFlags = storyStructure.SkipGenerationFlags
		};
	}

	private void SetupForceLandform()
	{
		GenMaps modSystem = api.ModLoader.GetModSystem<GenMaps>();
		foreach (KeyValuePair<string, StoryStructureLocation> storyStructureInstance in storyStructureInstances)
		{
			storyStructureInstance.Deconstruct(out var key, out var value);
			string code = key;
			StoryStructureLocation storyStructureLocation = value;
			WorldGenStoryStructure worldGenStoryStructure = scfg.Structures.FirstOrDefault((WorldGenStoryStructure s) => s.Code == code);
			if (worldGenStoryStructure == null)
			{
				api.Logger.Warning("Could not find config for story structure: " + code + ". Terrain will not be generated as it should at " + code);
				continue;
			}
			if (worldGenStoryStructure.ForceTemperature.HasValue || worldGenStoryStructure.ForceRain.HasValue)
			{
				modSystem.ForceClimateAt(new ForceClimate
				{
					Radius = storyStructureLocation.LandformRadius,
					CenterPos = storyStructureLocation.CenterPos,
					Climate = (Climate.DescaleTemperature(((float?)worldGenStoryStructure.ForceTemperature) ?? 0f) << 16) + (worldGenStoryStructure.ForceRain.GetValueOrDefault() << 8)
				});
			}
			if (worldGenStoryStructure.RequireLandform != null)
			{
				modSystem.ForceLandformAt(new ForceLandform
				{
					Radius = storyStructureLocation.LandformRadius,
					CenterPos = storyStructureLocation.CenterPos,
					LandformCode = worldGenStoryStructure.RequireLandform
				});
			}
		}
	}

	private void Event_GameWorldSave()
	{
		if (StoryStructureInstancesDirty)
		{
			api.WorldManager.SaveGame.StoreData("storystructurelocations", SerializerUtil.Serialize(storyStructureInstances));
			StoryStructureInstancesDirty = false;
		}
	}

	private void Event_SaveGameLoaded()
	{
		OrderedDictionary<string, StoryStructureLocation> data = api.WorldManager.SaveGame.GetData<OrderedDictionary<string, StoryStructureLocation>>("storystructurelocations");
		if (data != null)
		{
			storyStructureInstances = data;
		}
		if (GameVersion.IsLowerVersionThan(api.WorldManager.SaveGame.CreatedGameVersion, "1.21.0-pre.3") && !api.WorldManager.SaveGame.GetData("storyLocUpgrade-1.21.0-pre.3", defaultValue: false))
		{
			UpdateOldStoryClaims();
		}
	}

	private void UpdateOldStoryClaims()
	{
		foreach (LandClaim item in api.World.Claims.All)
		{
			bool flag = item.ProtectionLevel == 10 && item.AllowUseEveryone;
			if (flag)
			{
				bool flag2;
				switch (item.LastKnownOwnerName)
				{
				case "custommessage-nadiya":
				case "custommessage-tobias":
				case "custommessage-treasurehunter":
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			if (flag)
			{
				item.AllowUseEveryone = false;
				item.AllowTraverseEveryone = true;
			}
		}
		api.WorldManager.SaveGame.StoreData("storyLocUpgrade-1.21.0-pre.3", data: true);
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		worldgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: false);
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		if (!genStoryStructures || storyStructureInstances == null)
		{
			return;
		}
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		tmpCuboid.Set(chunkX * 32, 0, chunkZ * 32, chunkX * 32 + 32, chunks.Length * 32, chunkZ * 32 + 32);
		worldgenBlockAccessor.BeginColumn();
		foreach (KeyValuePair<string, StoryStructureLocation> storyStructureInstance in storyStructureInstances)
		{
			storyStructureInstance.Deconstruct(out var key, out var value);
			string strucCode = key;
			StoryStructureLocation storyStructureLocation = value;
			Cuboidi location = storyStructureLocation.Location;
			if (!location.Intersects(tmpCuboid))
			{
				continue;
			}
			if (!storyStructureLocation.DidGenerate)
			{
				storyStructureLocation.DidGenerate = true;
				StoryStructureInstancesDirty = true;
			}
			BlockPos blockPos = new BlockPos(location.X1, location.Y1, location.Z1, 0);
			WorldGenStoryStructure worldGenStoryStructure = scfg.Structures.First((WorldGenStoryStructure s) => s.Code == strucCode);
			if (worldGenStoryStructure.UseWorldgenHeight)
			{
				int num;
				if (storyStructureLocation.WorldgenHeight >= 0)
				{
					num = storyStructureLocation.WorldgenHeight;
				}
				else
				{
					num = (storyStructureLocation.WorldgenHeight = chunks[0].MapChunk.WorldGenTerrainHeightMap[blockPos.Z % 32 * 32 + blockPos.X % 32]);
					location.Y1 = num + worldGenStoryStructure.schematicData.OffsetY;
					location.Y2 = location.Y1 + worldGenStoryStructure.schematicData.SizeY;
					StoryStructureInstancesDirty = true;
				}
				blockPos.Y = num + worldGenStoryStructure.schematicData.OffsetY;
			}
			else if (worldGenStoryStructure.Placement == EnumStructurePlacement.SurfaceRuin)
			{
				int num2;
				if (storyStructureLocation.WorldgenHeight >= 0)
				{
					num2 = storyStructureLocation.WorldgenHeight;
				}
				else
				{
					num2 = (storyStructureLocation.WorldgenHeight = chunks[0].MapChunk.WorldGenTerrainHeightMap[blockPos.Z % 32 * 32 + blockPos.X % 32]);
					location.Y1 = num2 - worldGenStoryStructure.schematicData.SizeY + worldGenStoryStructure.schematicData.OffsetY;
					location.Y2 = location.Y1 + worldGenStoryStructure.schematicData.SizeY;
					StoryStructureInstancesDirty = true;
				}
				blockPos.Y = num2 - worldGenStoryStructure.schematicData.SizeY + worldGenStoryStructure.schematicData.OffsetY;
			}
			else if (worldGenStoryStructure.Placement == EnumStructurePlacement.Surface)
			{
				location.Y1 = api.World.SeaLevel + worldGenStoryStructure.schematicData.OffsetY;
				location.Y2 = location.Y1 + worldGenStoryStructure.schematicData.SizeY;
				StoryStructureInstancesDirty = true;
				blockPos.Y = location.Y1;
			}
			Block block = null;
			if (worldGenStoryStructure.resolvedRockTypeRemaps != null)
			{
				if (string.IsNullOrEmpty(storyStructureLocation.RockBlockCode))
				{
					strucRand.InitPositionSeed(chunkX, chunkZ);
					int num3 = strucRand.NextInt(32);
					int num4 = strucRand.NextInt(32);
					EnumStructurePlacement placement = worldGenStoryStructure.Placement;
					bool flag = (uint)placement <= 1u;
					int num5 = ((!flag) ? (blockPos.Y + worldGenStoryStructure.schematicData.SizeY / 2 + strucRand.NextInt(worldGenStoryStructure.schematicData.SizeY / 2)) : request.Chunks[0].MapChunk.WorldGenTerrainHeightMap[num4 * 32 + num3]);
					int num6 = 0;
					while (block == null && num6 < 10)
					{
						Block blockRaw = worldgenBlockAccessor.GetBlockRaw(chunkX * 32 + num3, num5, chunkZ * 32 + num4, 1);
						if (blockRaw.BlockMaterial == EnumBlockMaterial.Stone)
						{
							block = blockRaw;
							storyStructureLocation.RockBlockCode = blockRaw.Code.ToString();
							StoryStructureInstancesDirty = true;
						}
						placement = worldGenStoryStructure.Placement;
						flag = (uint)placement <= 1u;
						num5 = ((!flag) ? (blockPos.Y + worldGenStoryStructure.schematicData.SizeY / 2 + strucRand.NextInt(worldGenStoryStructure.schematicData.SizeY / 2)) : (num5 - 1));
						num6++;
					}
					if (string.IsNullOrEmpty(storyStructureLocation.RockBlockCode))
					{
						api.Logger.Warning("Could not find rock block code for " + storyStructureLocation.Code);
					}
				}
				else
				{
					block = worldgenBlockAccessor.GetBlock(new AssetLocation(storyStructureLocation.RockBlockCode));
				}
			}
			int num7 = worldGenStoryStructure.schematicData.PlacePartial(chunks, worldgenBlockAccessor, api.World, chunkX, chunkZ, blockPos, EnumReplaceMode.ReplaceAll, worldGenStoryStructure.Placement, GenStructures.ReplaceMetaBlocks, GenStructures.ReplaceMetaBlocks, worldGenStoryStructure.resolvedRockTypeRemaps, worldGenStoryStructure.replacewithblocklayersBlockids, block, worldGenStoryStructure.DisableSurfaceTerrainBlending);
			if (num7 > 0)
			{
				EnumStructurePlacement placement = worldGenStoryStructure.Placement;
				if ((uint)placement <= 1u)
				{
					UpdateHeightmap(request, worldgenBlockAccessor);
				}
				if (worldGenStoryStructure.GenerateGrass)
				{
					GenerateGrass(request);
				}
			}
			string code = worldGenStoryStructure.Code + ":" + worldGenStoryStructure.Schematics[0];
			IMapRegion mapRegion = chunks[0].MapChunk.MapRegion;
			if (mapRegion.GeneratedStructures.FirstOrDefault((GeneratedStructure struc) => struc.Code.Equals(code)) == null)
			{
				mapRegion.AddGeneratedStructure(new GeneratedStructure
				{
					Code = code,
					Group = worldGenStoryStructure.Group,
					Location = location.Clone()
				});
			}
			if (num7 <= 0 || !worldGenStoryStructure.BuildProtected)
			{
				continue;
			}
			if (!worldGenStoryStructure.ExcludeSchematicSizeProtect)
			{
				LandClaim[] array = api.World.Claims.Get(location.Center.AsBlockPos);
				if (array == null || array.Length == 0)
				{
					api.World.Claims.Add(new LandClaim
					{
						Areas = new List<Cuboidi> { location },
						Description = worldGenStoryStructure.BuildProtectionDesc,
						ProtectionLevel = worldGenStoryStructure.ProtectionLevel,
						LastKnownOwnerName = worldGenStoryStructure.BuildProtectionName,
						AllowUseEveryone = worldGenStoryStructure.AllowUseEveryone,
						AllowTraverseEveryone = worldGenStoryStructure.AllowTraverseEveryone
					});
				}
			}
			if (worldGenStoryStructure.ExtraLandClaimX > 0 && worldGenStoryStructure.ExtraLandClaimZ > 0)
			{
				Cuboidi cuboidi = new Cuboidi(location.Center.X - worldGenStoryStructure.ExtraLandClaimX, 0, location.Center.Z - worldGenStoryStructure.ExtraLandClaimZ, location.Center.X + worldGenStoryStructure.ExtraLandClaimX, api.WorldManager.MapSizeY, location.Center.Z + worldGenStoryStructure.ExtraLandClaimZ);
				LandClaim[] array2 = api.World.Claims.Get(cuboidi.Center.AsBlockPos);
				if (array2 == null || array2.Length == 0)
				{
					api.World.Claims.Add(new LandClaim
					{
						Areas = new List<Cuboidi> { cuboidi },
						Description = worldGenStoryStructure.BuildProtectionDesc,
						ProtectionLevel = worldGenStoryStructure.ProtectionLevel,
						LastKnownOwnerName = worldGenStoryStructure.BuildProtectionName,
						AllowUseEveryone = worldGenStoryStructure.AllowUseEveryone,
						AllowTraverseEveryone = worldGenStoryStructure.AllowTraverseEveryone
					});
				}
			}
			if (worldGenStoryStructure.CustomLandClaims == null)
			{
				continue;
			}
			Cuboidi[] customLandClaims = worldGenStoryStructure.CustomLandClaims;
			for (int num8 = 0; num8 < customLandClaims.Length; num8++)
			{
				Cuboidi cuboidi2 = customLandClaims[num8].Clone();
				cuboidi2.X1 += location.X1;
				cuboidi2.X2 += location.X1;
				cuboidi2.Y1 += location.Y1;
				cuboidi2.Y2 += location.Y1;
				cuboidi2.Z1 += location.Z1;
				cuboidi2.Z2 += location.Z1;
				LandClaim[] array3 = api.World.Claims.Get(cuboidi2.Center.AsBlockPos);
				if (array3 == null || array3.Length == 0)
				{
					api.World.Claims.Add(new LandClaim
					{
						Areas = new List<Cuboidi> { cuboidi2 },
						Description = worldGenStoryStructure.BuildProtectionDesc,
						ProtectionLevel = worldGenStoryStructure.ProtectionLevel,
						LastKnownOwnerName = worldGenStoryStructure.BuildProtectionName,
						AllowUseEveryone = worldGenStoryStructure.AllowUseEveryone,
						AllowTraverseEveryone = worldGenStoryStructure.AllowTraverseEveryone
					});
				}
			}
		}
	}

	private void UpdateHeightmap(IChunkColumnGenerateRequest request, IWorldGenBlockAccessor worldGenBlockAccessor)
	{
		int num = 0;
		int num2 = 0;
		ushort[] rainHeightMap = request.Chunks[0].MapChunk.RainHeightMap;
		ushort[] worldGenTerrainHeightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
		for (int i = 0; i < rainHeightMap.Length; i++)
		{
			rainHeightMap[i] = 0;
			worldGenTerrainHeightMap[i] = 0;
		}
		int mapSizeY = worldgenBlockAccessor.MapSizeY;
		int num3 = 1024;
		for (int j = 0; j < 32; j++)
		{
			for (int k = 0; k < 32; k++)
			{
				int num4 = k * 32 + j;
				bool flag = false;
				bool flag2 = false;
				for (int num5 = mapSizeY - 1; num5 >= 0; num5--)
				{
					int num6 = num5 % 32;
					IServerChunk serverChunk = request.Chunks[num5 / 32];
					int index3d = (num6 * 32 + k) * 32 + j;
					int num7 = serverChunk.Data[index3d];
					if (num7 != 0)
					{
						Block block = worldGenBlockAccessor.GetBlock(num7);
						bool rainPermeable = block.RainPermeable;
						bool num8 = block.SideSolid[BlockFacing.UP.Index];
						if (!rainPermeable && !flag)
						{
							flag = true;
							rainHeightMap[num4] = (ushort)num5;
							num2++;
						}
						if (num8 && !flag2)
						{
							flag2 = true;
							worldGenTerrainHeightMap[num4] = (ushort)num5;
							num++;
						}
						if (num2 >= num3 && num >= num3)
						{
							return;
						}
					}
				}
			}
		}
	}

	private void GenerateGrass(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		grassRand.InitPositionSeed(chunkX, chunkZ);
		IntDataMap2D forestMap = chunks[0].MapChunk.MapRegion.ForestMap;
		IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
		ushort[] rainHeightMap = chunks[0].MapChunk.RainHeightMap;
		int num = api.WorldManager.RegionSize / 32;
		int num2 = chunkX % num;
		int num3 = chunkZ % num;
		float num4 = (float)climateMap.InnerSize / (float)num;
		float num5 = (float)forestMap.InnerSize / (float)num;
		int unpaddedInt = forestMap.GetUnpaddedInt((int)((float)num2 * num5), (int)((float)num3 * num5));
		int unpaddedInt2 = forestMap.GetUnpaddedInt((int)((float)num2 * num5 + num5), (int)((float)num3 * num5));
		int unpaddedInt3 = forestMap.GetUnpaddedInt((int)((float)num2 * num5), (int)((float)num3 * num5 + num5));
		int unpaddedInt4 = forestMap.GetUnpaddedInt((int)((float)num2 * num5 + num5), (int)((float)num3 * num5 + num5));
		BlockPos blockPos = new BlockPos();
		for (int i = 0; i < 32; i++)
		{
			for (int j = 0; j < 32; j++)
			{
				blockPos.Set(chunkX * 32 + i, 1, chunkZ * 32 + j);
				double distx;
				double distz;
				int num6 = RandomlyAdjustPosition(blockPos, out distx, out distz);
				int num7 = rainHeightMap[j * 32 + i];
				if (num7 < mapheight)
				{
					int unpaddedColorLerped = climateMap.GetUnpaddedColorLerped((float)num2 * num4 + num4 * ((float)i + (float)distx) / 32f, (float)num3 * num4 + num4 * ((float)j + (float)distz) / 32f);
					int unscaledTemp = (unpaddedColorLerped >> 16) & 0xFF;
					float scaledAdjustedTemperatureFloat = Climate.GetScaledAdjustedTemperatureFloat(unscaledTemp, num7 - TerraGenConfig.seaLevel + num6);
					float tempRel = (float)Climate.GetAdjustedTemperature(unscaledTemp, num7 - TerraGenConfig.seaLevel + num6) / 255f;
					float rainRel = (float)Climate.GetRainFall((unpaddedColorLerped >> 8) & 0xFF, num7 + num6) / 255f;
					float forestRel = GameMath.BiLerp(unpaddedInt, unpaddedInt2, unpaddedInt3, unpaddedInt4, (float)i / 32f, (float)j / 32f) / 255f;
					ushort num8 = chunks[0].MapChunk.WorldGenTerrainHeightMap[j * 32 + i];
					int num9 = num8 / 32;
					int num10 = num8 % 32;
					int index3d = (32 * num10 + j) * 32 + i;
					int blockIdUnsafe = chunks[num9].Data.GetBlockIdUnsafe(index3d);
					if (api.World.Blocks[blockIdUnsafe].BlockMaterial == EnumBlockMaterial.Soil)
					{
						PlaceTallGrass(i, num7, j, chunks, rainRel, tempRel, scaledAdjustedTemperatureFloat, forestRel);
					}
				}
			}
		}
	}

	public int RandomlyAdjustPosition(BlockPos herePos, out double distx, out double distz)
	{
		distx = distort2dx.Noise(herePos.X, herePos.Z);
		distz = distort2dz.Noise(herePos.X, herePos.Z);
		return (int)(distx / 5.0);
	}

	private void PlaceTallGrass(int x, int posY, int z, IServerChunk[] chunks, float rainRel, float tempRel, float temp, float forestRel)
	{
		double num = (double)blockLayerConfig.Tallgrass.RndWeight * grassRand.NextDouble() + (double)blockLayerConfig.Tallgrass.PerlinWeight * grassDensity.Noise(x, z, -0.5);
		double num2 = Math.Max(0.0, (double)(rainRel * tempRel) - 0.25);
		if (num <= GameMath.Clamp((double)forestRel - num2, 0.05, 0.99) || posY >= mapheight - 1 || posY < 1)
		{
			return;
		}
		int index = chunks[posY / 32].Data[(32 * (posY % 32) + z) * 32 + x];
		if (api.World.Blocks[index].Fertility <= grassRand.NextInt(100))
		{
			return;
		}
		double num3 = Math.Max(0.0, grassHeight.Noise(x, z) * (double)blockLayerConfig.Tallgrass.BlockCodeByMin.Length - 1.0);
		for (int i = (int)num3 + ((grassRand.NextDouble() < num3) ? 1 : 0); i < blockLayerConfig.Tallgrass.BlockCodeByMin.Length; i++)
		{
			TallGrassBlockCodeByMin tallGrassBlockCodeByMin = blockLayerConfig.Tallgrass.BlockCodeByMin[i];
			if (forestRel <= tallGrassBlockCodeByMin.MaxForest && rainRel >= tallGrassBlockCodeByMin.MinRain && temp >= (float)tallGrassBlockCodeByMin.MinTemp)
			{
				chunks[(posY + 1) / 32].Data[(32 * ((posY + 1) % 32) + z) * 32 + x] = tallGrassBlockCodeByMin.BlockId;
				break;
			}
		}
	}

	public void GenerateHookStructure(IBlockAccessor blockAccessor, BlockPos pos, string param)
	{
		AssetLocation assetLocation = new AssetLocation(param);
		api.Logger.VerboseDebug("Worldgen hook generation event fired, with code " + assetLocation);
		IMapChunk mapChunk = blockAccessor.GetMapChunkAtBlockPos(pos);
		IAsset asset = api.Assets.TryGet(assetLocation.WithPathPrefixOnce("worldgen/hookgeneratedstructures/").WithPathAppendixOnce(".json"));
		if (asset == null || mapChunk == null)
		{
			api.Logger.Error("Worldgen hook event failed: " + ((mapChunk == null) ? "bad coordinates" : string.Concat(assetLocation, "* not found")));
			return;
		}
		HookGeneratedStructure hookGeneratedStructure = asset.ToObject<HookGeneratedStructure>();
		int mainsizeX = hookGeneratedStructure.mainsizeX;
		int mainsizeZ = hookGeneratedStructure.mainsizeZ;
		int num = pos.X - mainsizeX / 2 - 2;
		int num2 = pos.X + mainsizeX / 2 + 2;
		int num3 = pos.Z - mainsizeZ / 2 - 2;
		int num4 = pos.Z + mainsizeZ / 2 + 2;
		List<int> list = new List<int>((num2 - num + 1) * (num4 - num3 + 1));
		int num5 = 0;
		int num6 = int.MaxValue;
		int j;
		int i;
		for (i = num; i <= num2; i++)
		{
			for (j = num3; j <= num4; j++)
			{
				mapChunk = blockAccessor.GetMapChunk(i / 32, j / 32);
				int num7 = mapChunk.WorldGenTerrainHeightMap[j % 32 * 32 + i % 32];
				list.Add(num7);
				num5 = Math.Max(num5, num7);
				num6 = Math.Min(num6, num7);
			}
		}
		i = Math.Max(mainsizeX, mainsizeZ);
		num = pos.X - i / 2;
		num2 = pos.X + i / 2;
		num3 = pos.Z - i / 2;
		num4 = pos.Z + i / 2;
		int num8 = 1;
		int num9 = 1;
		int num10 = 1;
		int num11 = 1;
		i = num - 2;
		for (j = num3; j <= num4; j++)
		{
			mapChunk = blockAccessor.GetMapChunk(i / 32, j / 32);
			int num12 = mapChunk.WorldGenTerrainHeightMap[j % 32 * 32 + i % 32];
			num8 += num12;
		}
		i = num2 + 2;
		for (j = num3; j <= num4; j++)
		{
			mapChunk = blockAccessor.GetMapChunk(i / 32, j / 32);
			int num13 = mapChunk.WorldGenTerrainHeightMap[j % 32 * 32 + i % 32];
			num9 += num13;
		}
		j = num3 - 2;
		for (i = num; i <= num2; i++)
		{
			mapChunk = blockAccessor.GetMapChunk(i / 32, j / 32);
			int num14 = mapChunk.WorldGenTerrainHeightMap[j % 32 * 32 + i % 32];
			num10 += num14;
		}
		j = num4 + 2;
		for (i = num; i <= num2; i++)
		{
			mapChunk = blockAccessor.GetMapChunk(i / 32, j / 32);
			int num15 = mapChunk.WorldGenTerrainHeightMap[j % 32 * 32 + i % 32];
			num11 += num15;
		}
		if (hookGeneratedStructure.mainElements.Length != 0)
		{
			pos = pos.AddCopy(hookGeneratedStructure.offsetX, hookGeneratedStructure.offsetY, hookGeneratedStructure.offsetZ);
			Vec3i[] array = new Vec3i[hookGeneratedStructure.mainElements.Length];
			BlockSchematicStructure[] array2 = new BlockSchematicStructure[hookGeneratedStructure.mainElements.Length];
			int[] array3 = new int[hookGeneratedStructure.mainElements.Length];
			int[] array4 = new int[hookGeneratedStructure.mainElements.Length];
			int num16 = 0;
			PathAndOffset[] mainElements = hookGeneratedStructure.mainElements;
			foreach (PathAndOffset pathAndOffset in mainElements)
			{
				IAsset asset2 = api.Assets.TryGet(new AssetLocation(assetLocation.Domain, "worldgen/" + pathAndOffset.path + ".json"));
				if (asset2 == null)
				{
					api.Logger.Notification("Worldgen hook event elements: path not found: " + pathAndOffset.path);
					continue;
				}
				BlockSchematicStructure blockSchematicStructure = asset2.ToObject<BlockSchematicStructure>();
				blockSchematicStructure.Init(blockAccessor);
				array2[num16] = blockSchematicStructure;
				array4[num16] = ((pathAndOffset.maxCount == 0) ? 16384 : pathAndOffset.maxCount);
				array[num16++] = new Vec3i(pathAndOffset.dx, pathAndOffset.dy, pathAndOffset.dz);
			}
			Random rand = api.World.Rand;
			List<int> list2 = new List<int>();
			List<int> list3 = new List<int>();
			int num17 = int.MaxValue;
			list.Sort();
			int num18 = Math.Min(5, list.Count);
			int num19 = 0;
			for (int l = 0; l < num18; l++)
			{
				num19 += list[l];
			}
			num19 = num19 / num18 + hookGeneratedStructure.endOffsetY;
			if (num5 - num6 < 5 && num19 - num6 < 2)
			{
				num19++;
			}
			if (num19 < api.World.SeaLevel)
			{
				num19 = api.World.SeaLevel;
			}
			num19 = Math.Min(num19, api.World.BlockAccessor.MapSizeY - 11);
			for (int m = 0; m < 25; m++)
			{
				list2.Clear();
				for (int n = 0; n < array3.Length; n++)
				{
					array3[n] = 0;
				}
				int num20 = pos.Y;
				while (num20 < num19)
				{
					int num21 = rand.Next(num16);
					if (array3[num21] >= array4[num21])
					{
						continue;
					}
					int num22 = array2[num21].SizeY;
					if (num20 + num22 > num19)
					{
						if (num20 + num22 - num19 > num19 - num20)
						{
							num22 = (num19 - num20) * 2;
						}
						else
						{
							list2.Add(num21);
							array3[num21]++;
						}
						int num23 = num20 + num22 - num19;
						if (num23 >= num17)
						{
							break;
						}
						num17 = num23;
						list3.Clear();
						foreach (int item in list2)
						{
							list3.Add(item);
						}
						if (num17 == 0)
						{
							m = 25;
						}
						break;
					}
					list2.Add(num21);
					array3[num21]++;
					num20 += num22;
				}
			}
			int y = pos.Y;
			int num24 = int.MaxValue;
			int num25 = int.MaxValue;
			int num26 = 0;
			int num27 = 0;
			foreach (int item2 in list3)
			{
				BlockSchematicStructure blockSchematicStructure2 = array2[item2];
				Vec3i vec3i = array[item2];
				BlockPos blockPos = pos.AddCopy(vec3i.X, vec3i.Y, vec3i.Z);
				blockSchematicStructure2.PlaceRespectingBlockLayers(blockAccessor, api.World, blockPos, 0, 0, 0, 0, null, Array.Empty<int>(), GenStructures.ReplaceMetaBlocks, replaceBlockEntities: true, suppressSoilIfAirBelow: false, displaceWater: true);
				pos.Y += blockSchematicStructure2.SizeY;
				num24 = Math.Min(num24, blockPos.X);
				num25 = Math.Min(num25, blockPos.Z);
				num26 = Math.Max(num26, blockPos.X + blockSchematicStructure2.SizeX);
				num27 = Math.Max(num27, blockPos.Z + blockSchematicStructure2.SizeY);
			}
			if (list3.Count > 0)
			{
				IMapRegion mapRegion = blockAccessor.GetMapRegion(pos.X / blockAccessor.RegionSize, pos.Z / blockAccessor.RegionSize);
				Cuboidi cuboidi = new Cuboidi(num24, y, num25, num26, pos.Y, num27);
				mapRegion.AddGeneratedStructure(new GeneratedStructure
				{
					Code = param,
					Group = hookGeneratedStructure.group,
					Location = cuboidi.Clone()
				});
				if (hookGeneratedStructure.buildProtected)
				{
					api.World.Claims.Add(new LandClaim
					{
						Areas = new List<Cuboidi> { cuboidi },
						Description = hookGeneratedStructure.buildProtectionDesc,
						ProtectionLevel = hookGeneratedStructure.ProtectionLevel,
						LastKnownOwnerName = hookGeneratedStructure.buildProtectionName,
						AllowUseEveryone = hookGeneratedStructure.AllowUseEveryone,
						AllowTraverseEveryone = hookGeneratedStructure.AllowTraverseEveryone
					});
				}
			}
		}
		string text = ((num8 < num9) ? ((num8 >= num10 || num8 >= num11) ? ((num11 < num10) ? "s" : "n") : "w") : ((num9 >= num10 || num9 >= num11) ? ((num11 < num10) ? "s" : "n") : "e"));
		if (!hookGeneratedStructure.endElements.TryGetValue(text, out var value))
		{
			api.Logger.Notification("Worldgen hook event incomplete: no end structure for " + text);
			return;
		}
		BlockSchematicStructure blockSchematicStructure3 = api.Assets.Get(new AssetLocation(assetLocation.Domain, value.path))?.ToObject<BlockSchematicStructure>();
		if (blockSchematicStructure3 == null)
		{
			api.Logger.Notification("Worldgen hook event incomplete: " + value.path + " not found");
			return;
		}
		int[] array5;
		if (hookGeneratedStructure.ReplaceWithBlocklayers != null)
		{
			array5 = new int[hookGeneratedStructure.ReplaceWithBlocklayers.Length];
			for (int num28 = 0; num28 < array5.Length; num28++)
			{
				Block block = api.World.GetBlock(hookGeneratedStructure.ReplaceWithBlocklayers[num28]);
				if (block == null)
				{
					api.Logger.Error($"Hook structure with code {assetLocation} has replace block layer {hookGeneratedStructure.ReplaceWithBlocklayers[num28]} defined, but no such block found!");
					return;
				}
				array5[num28] = block.Id;
			}
		}
		else
		{
			array5 = Array.Empty<int>();
		}
		IMapRegion mapRegion2 = mapChunk.MapRegion;
		IntDataMap2D climateMap = mapRegion2.ClimateMap;
		int num29 = api.WorldManager.RegionSize / 32;
		int num30 = pos.X / 32 % num29;
		int num31 = pos.Z / 32 % num29;
		float num32 = (float)climateMap.InnerSize / (float)num29;
		int unpaddedInt = climateMap.GetUnpaddedInt((int)((float)num30 * num32), (int)((float)num31 * num32));
		int unpaddedInt2 = climateMap.GetUnpaddedInt((int)((float)num30 * num32 + num32), (int)((float)num31 * num32));
		int unpaddedInt3 = climateMap.GetUnpaddedInt((int)((float)num30 * num32), (int)((float)num31 * num32 + num32));
		int unpaddedInt4 = climateMap.GetUnpaddedInt((int)((float)num30 * num32 + num32), (int)((float)num31 * num32 + num32));
		blockSchematicStructure3.blockLayerConfig = blockLayerConfig;
		blockSchematicStructure3.Init(blockAccessor);
		pos.Add(value.dx, value.dy, value.dz);
		blockSchematicStructure3.PlaceRespectingBlockLayers(blockAccessor, api.World, pos, unpaddedInt, unpaddedInt2, unpaddedInt3, unpaddedInt4, null, array5, GenStructures.ReplaceMetaBlocks, replaceBlockEntities: true, suppressSoilIfAirBelow: true);
		Cuboidi cuboidi2 = new Cuboidi(pos.X, pos.Y, pos.Z, pos.X + blockSchematicStructure3.SizeX, pos.Y + blockSchematicStructure3.SizeY, pos.Z + blockSchematicStructure3.SizeZ);
		mapRegion2.AddGeneratedStructure(new GeneratedStructure
		{
			Code = hookGeneratedStructure.group,
			Group = hookGeneratedStructure.group,
			Location = cuboidi2.Clone()
		});
	}

	public void FinalizeRegeneration(int chunkMidX, int chunkMidZ)
	{
		api.ModLoader.GetModSystem<Timeswitch>().AttemptGeneration(worldgenBlockAccessor);
	}
}
