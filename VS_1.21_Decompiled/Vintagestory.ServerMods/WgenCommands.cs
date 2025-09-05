using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class WgenCommands : ModSystem
{
	private ICoreServerAPI api;

	private TreeGeneratorsUtil treeGenerators;

	private int _regionSize;

	private long _seed = 1239123912L;

	private int _chunksize;

	private WorldGenStructuresConfig _scfg;

	private int _regionChunkSize;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.33;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		treeGenerators = new TreeGeneratorsUtil(api);
		api.Event.SaveGameLoaded += OnGameWorldLoaded;
		if (this.api.Server.CurrentRunPhase == EnumServerRunPhase.RunGame)
		{
			OnGameWorldLoaded();
		}
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(InitWorldGen, "standard");
		}
		CreateCommands();
		this.api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, delegate
		{
			CommandArgumentParsers parsers = api.ChatCommands.Parsers;
			string[] array = api.World.TreeGenerators.Keys.Select((AssetLocation a) => a.Path).ToArray();
			api.ChatCommands.GetOrCreate("wgen").BeginSubCommand("tree").WithDescription("Generate a tree in front of the player")
				.RequiresPlayer()
				.WithArgs(parsers.WordRange("treeWorldPropertyCode", array), parsers.OptionalFloat("size", 1f), parsers.OptionalFloat("aheadoffset"))
				.HandleWith(OnCmdTree)
				.EndSubCommand()
				.BeginSubCommand("treelineup")
				.WithDescription("treelineup")
				.RequiresPlayer()
				.WithArgs(parsers.Word("treeWorldPropertyCode", array))
				.HandleWith(OnCmdTreelineup)
				.EndSubCommand();
		});
	}

	private void InitWorldGen()
	{
		_chunksize = 32;
		_regionChunkSize = api.WorldManager.RegionSize / _chunksize;
		_scfg = api.ModLoader.GetModSystem<GenStructures>().scfg;
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("wgen").BeginSubCommand("structures").BeginSubCommand("spawn")
			.RequiresPlayer()
			.WithDescription("Spawn a structure from structure.json like during worldgen. Target position will be the selected block or your position. See /dev list <num> command to get the correct index.")
			.WithArgs(parsers.Int("structure_index"), parsers.OptionalInt("schematic_index"), parsers.OptionalIntRange("rotation_index", 0, 3))
			.HandleWith(OnStructuresSpawn)
			.EndSubCommand()
			.BeginSubCommand("list")
			.WithDescription("List structures with their indices for the /dev structure spawn command")
			.WithArgs(parsers.OptionalInt("structure_num"))
			.HandleWith(OnStructuresList)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("resolve-meta")
			.WithDescription("Toggle resolve meta blocks mode during Worldgen. Turn it off to spawn structures as they are. For example, in this mode, instead of traders, their meta spawners will spawn")
			.WithAlias("rm")
			.WithArgs(parsers.OptionalBool("on/off"))
			.HandleWith(handleToggleImpresWgen)
			.EndSubCommand();
	}

	private void OnGameWorldLoaded()
	{
		_regionSize = api.WorldManager.RegionSize;
	}

	private void CreateCommands()
	{
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("wgen").WithDescription("World generator tools").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("decopass")
			.WithDescription("Toggle DoDecorationPass on/off")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalBool("DoDecorationPass"))
			.HandleWith(OnCmdDecopass)
			.EndSubCommand()
			.BeginSubCommand("autogen")
			.WithDescription("Toggle AutoGenerateChunks on/off")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalBool("AutoGenerateChunks"))
			.HandleWith(OnCmdAutogen)
			.EndSubCommand()
			.BeginSubCommand("gt")
			.WithDescription("Toggle GenerateVegetation on/off")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalBool("GenerateVegetation"))
			.HandleWith(OnCmdGt)
			.EndSubCommand()
			.BeginSubCommand("regenk")
			.WithDescription("Regenerate chunks around the player. Keeps the mapregion and so will not regenerate structures use /wgen regen if you want to also regenerate the structures")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50), parsers.OptionalWord("landform"))
			.HandleWith(OnCmdRegenk)
			.EndSubCommand()
			.BeginSubCommand("regen")
			.WithDescription("Regenerate chunks around the player also regenerating the region. Keeps unaffected structures outside of the range and copy them to the new region")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50), parsers.OptionalWord("landform"))
			.HandleWith(OnCmdRegen)
			.EndSubCommand()
			.BeginSubCommand("regenr")
			.WithDescription("Regenerate chunks around the player with random seed")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50), parsers.OptionalWord("landform"))
			.HandleWith(OnCmdRegenr)
			.EndSubCommand()
			.BeginSubCommand("regenc")
			.WithDescription("Regenerate chunks around world center")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50), parsers.OptionalWord("landform"))
			.HandleWith(OnCmdRegenc)
			.EndSubCommand()
			.BeginSubCommand("regenrc")
			.WithDescription("Regenerate chunks around world center with random seed")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50), parsers.OptionalWord("landform"))
			.HandleWith(OnCmdRegenrc)
			.EndSubCommand()
			.BeginSubCommand("pregen")
			.WithDescription("Pregenerate chunks around the player or around world center when executed from console.")
			.WithArgs(parsers.OptionalInt("chunk_range", 2))
			.HandleWith(OnCmdPregen)
			.EndSubCommand()
			.BeginSubCommand("delrock")
			.WithDescription("Delete all rocks in specified chunk range around the player. Good for testing ore generation.")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50))
			.HandleWith(OnCmdDelrock)
			.EndSubCommand()
			.BeginSubCommand("delrockc")
			.WithDescription("Delete all rocks in specified chunk range around the world center. Good for testing ore generation.")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50))
			.HandleWith(OnCmdDelrockc)
			.EndSubCommand()
			.BeginSubCommand("del")
			.WithDescription("Delete chunks around the player")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50), parsers.OptionalWord("landform"))
			.HandleWith(OnCmdDel)
			.EndSubCommand()
			.BeginSubCommand("delr")
			.WithDescription("Delete chunks around the player and the map regions. This will allow that changed terrain can generate for example at story locations.")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50))
			.HandleWith(OnCmdDelr)
			.EndSubCommand()
			.BeginSubCommand("delrange")
			.WithDescription("Delete a range of chunks. Start and end positions are in chunk coordinates. See CTRL + F3")
			.RequiresPlayer()
			.WithArgs(parsers.Int("x_start"), parsers.Int("z_start"), parsers.Int("x_end"), parsers.Int("z_end"))
			.HandleWith(OnCmdDelrange)
			.EndSubCommand()
			.BeginSubCommand("treemap")
			.WithDescription("treemap")
			.HandleWith(OnCmdTreemap)
			.EndSubCommand()
			.BeginSubCommand("testmap")
			.WithDescription("Generate a large noise map, to test noise generation")
			.WithPreCondition(DisallowHosted)
			.BeginSubCommand("climate")
			.WithDescription("Print a climate testmap")
			.HandleWith(OnCmdClimate)
			.EndSubCommand()
			.BeginSubCommand("geoact")
			.WithDescription("Print a geoact testmap")
			.WithArgs(parsers.OptionalInt("size", 512))
			.HandleWith(OnCmdGeoact)
			.EndSubCommand()
			.BeginSubCommand("climater")
			.WithDescription("Print a geoact testmap")
			.HandleWith(OnCmdClimater)
			.EndSubCommand()
			.BeginSubCommand("forest")
			.WithDescription("Print a forest testmap")
			.HandleWith(OnCmdForest)
			.EndSubCommand()
			.BeginSubCommand("upheavel")
			.WithDescription("Print a upheavel testmap")
			.WithArgs(parsers.OptionalInt("size", 512))
			.HandleWith(OnCmdUpheavel)
			.EndSubCommand()
			.BeginSubCommand("ocean")
			.WithDescription("Print a ocean testmap")
			.WithArgs(parsers.OptionalInt("size", 512))
			.HandleWith(OnCmdOcean)
			.EndSubCommand()
			.BeginSubCommand("ore")
			.WithDescription("Print a ore testmap")
			.WithArgs(parsers.OptionalFloat("scaleMul", 1f), parsers.OptionalFloat("contrast", 1f), parsers.OptionalFloat("sub"))
			.HandleWith(OnCmdOre)
			.EndSubCommand()
			.BeginSubCommand("oretopdistort")
			.WithDescription("Print a oretopdistort testmap")
			.HandleWith(OnCmdOretopdistort)
			.EndSubCommand()
			.BeginSubCommand("wind")
			.WithDescription("Print a wind testmap")
			.HandleWith(OnCmdWind)
			.EndSubCommand()
			.BeginSubCommand("gprov")
			.WithDescription("Print a gprov testmap")
			.HandleWith(OnCmdGprov)
			.EndSubCommand()
			.BeginSubCommand("landform")
			.WithDescription("Print a landform testmap")
			.HandleWith(OnCmdLandform)
			.EndSubCommand()
			.BeginSubCommand("rockstrata")
			.WithDescription("Print a rockstrata testmap")
			.HandleWith(OnCmdRockstrata)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("genmap")
			.WithDescription("Generate a large noise map around the players current location")
			.WithPreCondition(DisallowHosted)
			.BeginSubCommand("climate")
			.WithDescription("Generate a climate map")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalFloat("GeologicActivityStrength", 1f))
			.HandleWith(OnCmdGenmapClimate)
			.EndSubCommand()
			.BeginSubCommand("forest")
			.WithDescription("Generate a forest map")
			.RequiresPlayer()
			.HandleWith(OnCmdGenmapForest)
			.EndSubCommand()
			.BeginSubCommand("upheavel")
			.WithDescription("Generate a upheavel map")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalInt("size", 512))
			.HandleWith(OnCmdGenmapUpheavel)
			.EndSubCommand()
			.BeginSubCommand("mushroom")
			.WithDescription("Generate a mushroom map")
			.RequiresPlayer()
			.HandleWith(OnCmdGenmapMushroom)
			.EndSubCommand()
			.BeginSubCommand("ore")
			.WithDescription("Generate a ore map")
			.RequiresPlayer()
			.HandleWith(OnCmdGenmapOre)
			.EndSubCommand()
			.BeginSubCommand("gprov")
			.WithDescription("Generate a gprov map")
			.RequiresPlayer()
			.HandleWith(OnCmdGenmapGprov)
			.EndSubCommand()
			.BeginSubCommand("landform")
			.WithDescription("Generate a landform map")
			.RequiresPlayer()
			.HandleWith(OnCmdGenmapLandform)
			.EndSubCommand()
			.BeginSubCommand("ocean")
			.WithDescription("Generate a ocean map")
			.RequiresPlayer()
			.HandleWith(OnCmdGenmapOcean)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("stitchclimate")
			.WithDescription("Print a 3x3 stitched climate map")
			.RequiresPlayer()
			.HandleWith(OnCmdStitch)
			.EndSubCommand()
			.BeginSubCommand("region")
			.WithDescription("Extract already generated noise map data from the current region")
			.RequiresPlayer()
			.WithArgs(parsers.WordRange("sub_command", "climate", "ore", "forest", "upheavel", "ocean", "oretopdistort", "patches", "rockstrata", "gprov", "gprovi", "landform", "landformi"), parsers.OptionalBool("dolerp"), parsers.OptionalWord("orename"))
			.HandleWith(OnCmdRegion)
			.EndSubCommand()
			.BeginSubCommand("regions")
			.BeginSubCommand("ore")
			.WithDescription("Print a region ore map")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalInt("radius", 1), parsers.OptionalWord("orename"))
			.HandleWith(OnCmdRegionsOre)
			.EndSubCommand()
			.BeginSubCommand("upheavel")
			.WithDescription("Print a region upheavel map")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalInt("radius", 1))
			.HandleWith(OnCmdRegionsUpheavel)
			.EndSubCommand()
			.BeginSubCommand("climate")
			.WithDescription("Print a region climate map")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalInt("radius", 1))
			.HandleWith(OnCmdRegionsClimate)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("pos")
			.WithDescription("Print info for the current position")
			.RequiresPlayer()
			.WithArgs(parsers.WordRange("sub_command", "ymax", "coords", "latitude", "structures", "height", "cavedistort", "gprov", "rockstrata", "landform", "climate"))
			.HandleWith(OnCmdPos)
			.EndSubCommand()
			.BeginSubCommand("testnoise")
			.WithDescription("Testnoise command")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalInt("octaves", 1))
			.HandleWith(OnCmdTestnoise)
			.EndSubCommand()
			.BeginSubCommand("testvillage")
			.WithDescription("Testvillage command")
			.RequiresPlayer()
			.HandleWith(OnCmdTestVillage)
			.EndSubCommand();
	}

	private TextCommandResult handleToggleImpresWgen(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Meta block replacing and Item resolving for worldgen currently " + (GenStructures.ReplaceMetaBlocks ? "on" : "off"));
		}
		bool flag = (bool)args[0];
		GenStructures.ReplaceMetaBlocks = flag;
		return TextCommandResult.Success("Meta block replacing and Item resolving for worldgen now " + (flag ? "on" : "off"));
	}

	private TextCommandResult OnCmdTestVillage(TextCommandCallingArgs args)
	{
		if (api.Server.Config.HostedMode)
		{
			return TextCommandResult.Success(Lang.Get("Can't access this feature, server is in hosted mode"));
		}
		api.Assets.Reload(AssetCategory.worldgen);
		GenStructures modSystem = api.ModLoader.GetModSystem<GenStructures>();
		modSystem.initWorldGen();
		Vec3d pos = args.Caller.Pos;
		int chunkX = (int)pos.X / 32;
		int chunkZ = (int)pos.Z / 32;
		IMapRegion mapRegion = api.World.BlockAccessor.GetMapRegion((int)pos.X / _regionSize, (int)pos.Z / _regionSize);
		for (int i = 0; i < 50; i++)
		{
			int maxValue = modSystem.vcfg.VillageTypes.Length;
			WorldGenVillage struc = modSystem.vcfg.VillageTypes[api.World.Rand.Next(maxValue)];
			if (modSystem.GenVillage(api.World.BlockAccessor, mapRegion, struc, chunkX, chunkZ))
			{
				return TextCommandResult.Success($"Generated after {i + 1} tries");
			}
		}
		return TextCommandResult.Error("Unable to generate, likely not flat enough here.");
	}

	private TextCommandResult DisallowHosted(TextCommandCallingArgs args)
	{
		if (api.Server.Config.HostedMode)
		{
			return TextCommandResult.Error(Lang.Get("Can't access this feature, server is in hosted mode"));
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdRegion(TextCommandCallingArgs args)
	{
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		BlockPos asBlockPos = serverPlayer.Entity.Pos.AsBlockPos;
		IServerChunk chunk = api.WorldManager.GetChunk(asBlockPos);
		if (chunk == null)
		{
			return TextCommandResult.Success("Can't check here, beyond chunk boundaries!");
		}
		IMapRegion mapRegion = chunk.MapChunk.MapRegion;
		int regionX = asBlockPos.X / _regionSize;
		int regionZ = asBlockPos.Z / _regionSize;
		string text = args[0] as string;
		bool lerp = (bool)args[1];
		NoiseBase.Debug = true;
		switch (text)
		{
		case "climate":
			DrawMapRegion(DebugDrawMode.RGB, args.Caller, mapRegion.ClimateMap, "climate", lerp, regionX, regionZ, TerraGenConfig.climateMapScale);
			break;
		case "ore":
		{
			string text2 = (args.Parsers[2].IsMissing ? "limonite" : (args[2] as string));
			if (!mapRegion.OreMaps.ContainsKey(text2))
			{
				serverPlayer.SendMessage(args.Caller.FromChatGroupId, "Mapregion does not contain an ore map for ore " + text2, EnumChatType.CommandError);
			}
			DrawMapRegion(DebugDrawMode.RGB, args.Caller, mapRegion.OreMaps[text2], "ore-" + text2, lerp, regionX, regionZ, TerraGenConfig.oreMapScale);
			break;
		}
		case "forest":
			DrawMapRegion(DebugDrawMode.FirstByteGrayscale, args.Caller, mapRegion.ForestMap, "forest", lerp, regionX, regionZ, TerraGenConfig.forestMapScale);
			break;
		case "upheavel":
			DrawMapRegion(DebugDrawMode.FirstByteGrayscale, args.Caller, mapRegion.UpheavelMap, "upheavel", lerp, regionX, regionZ, TerraGenConfig.geoUpheavelMapScale);
			break;
		case "ocean":
			DrawMapRegion(DebugDrawMode.FirstByteGrayscale, args.Caller, mapRegion.OceanMap, "ocean", lerp, regionX, regionZ, TerraGenConfig.oceanMapScale);
			break;
		case "oretopdistort":
			DrawMapRegion(DebugDrawMode.FirstByteGrayscale, args.Caller, mapRegion.OreMapVerticalDistortTop, "oretopdistort", lerp, regionX, regionZ, TerraGenConfig.depositVerticalDistortScale);
			break;
		case "patches":
			foreach (KeyValuePair<string, IntDataMap2D> blockPatchMap in mapRegion.BlockPatchMaps)
			{
				DrawMapRegion(DebugDrawMode.FirstByteGrayscale, args.Caller, blockPatchMap.Value, blockPatchMap.Key, lerp, regionX, regionZ, TerraGenConfig.forestMapScale);
			}
			serverPlayer.SendMessage(args.Caller.FromChatGroupId, "Patch maps generated", EnumChatType.CommandSuccess);
			break;
		case "rockstrata":
		{
			for (int num3 = 0; num3 < mapRegion.RockStrata.Length; num3++)
			{
				DrawMapRegion(DebugDrawMode.FirstByteGrayscale, args.Caller, mapRegion.RockStrata[num3], "rockstrata" + num3, lerp, regionX, regionZ, TerraGenConfig.rockStrataScale);
			}
			break;
		}
		case "gprov":
			DrawMapRegion(DebugDrawMode.ProvinceRGB, args.Caller, mapRegion.GeologicProvinceMap, "province", lerp, regionX, regionZ, TerraGenConfig.geoProvMapScale);
			break;
		case "gprovi":
		{
			int[] data2 = mapRegion.GeologicProvinceMap.Data;
			int innerSize = mapRegion.GeologicProvinceMap.InnerSize;
			int num2 = (innerSize + TerraGenConfig.geoProvMapPadding - 1) * TerraGenConfig.geoProvMapScale;
			GeologicProvinceVariant[] variants = NoiseGeoProvince.provinces.Variants;
			LerpedWeightedIndex2DMap lerpedWeightedIndex2DMap2 = new LerpedWeightedIndex2DMap(data2, innerSize + 2 * TerraGenConfig.geoProvMapPadding, 2, mapRegion.GeologicProvinceMap.TopLeftPadding, mapRegion.GeologicProvinceMap.BottomRightPadding);
			int[] array3 = new int[num2 * num2];
			for (int l = 0; l < num2; l++)
			{
				for (int m = 0; m < num2; m++)
				{
					WeightedIndex[] array4 = lerpedWeightedIndex2DMap2[(float)l / (float)TerraGenConfig.geoProvMapScale, (float)m / (float)TerraGenConfig.geoProvMapScale];
					for (int n = 0; n < array4.Length; n++)
					{
						array4[n].Index = variants[array4[n].Index].ColorInt;
					}
					lerpedWeightedIndex2DMap2.Split(array4, out var indices2, out var weights2);
					array3[m * num2 + l] = ColorUtil.ColorAverage(indices2, weights2);
				}
			}
			NoiseBase.DebugDrawBitmap(DebugDrawMode.ProvinceRGB, array3, num2, num2, "geoprovince-lerped-" + regionX + "-" + regionZ);
			serverPlayer.SendMessage(args.Caller.FromChatGroupId, "done", EnumChatType.CommandSuccess);
			break;
		}
		case "landform":
			DrawMapRegion(DebugDrawMode.LandformRGB, args.Caller, mapRegion.LandformMap, "landform", lerp, regionX, regionZ, TerraGenConfig.landformMapScale);
			break;
		case "landformi":
		{
			int[] data = mapRegion.LandformMap.Data;
			int num = (mapRegion.LandformMap.InnerSize + TerraGenConfig.landformMapPadding - 1) * TerraGenConfig.landformMapScale;
			LandformVariant[] landFormsByIndex = NoiseLandforms.landforms.LandFormsByIndex;
			LerpedWeightedIndex2DMap lerpedWeightedIndex2DMap = new LerpedWeightedIndex2DMap(data, mapRegion.LandformMap.Size, 1, mapRegion.LandformMap.TopLeftPadding, mapRegion.LandformMap.BottomRightPadding);
			int[] array = new int[num * num];
			for (int i = 0; i < num; i++)
			{
				for (int j = 0; j < num; j++)
				{
					WeightedIndex[] array2 = lerpedWeightedIndex2DMap[(float)i / (float)TerraGenConfig.landformMapScale, (float)j / (float)TerraGenConfig.landformMapScale];
					for (int k = 0; k < array2.Length; k++)
					{
						array2[k].Index = landFormsByIndex[array2[k].Index].ColorInt;
					}
					lerpedWeightedIndex2DMap.Split(array2, out var indices, out var weights);
					array[j * num + i] = ColorUtil.ColorAverage(indices, weights);
				}
			}
			NoiseBase.DebugDrawBitmap(DebugDrawMode.LandformRGB, array, num, num, "landform-lerped-" + regionX + "-" + regionZ);
			serverPlayer.SendMessage(args.Caller.FromChatGroupId, "Landform map done", EnumChatType.CommandSuccess);
			break;
		}
		}
		NoiseBase.Debug = false;
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdDecopass(TextCommandCallingArgs args)
	{
		TerraGenConfig.DoDecorationPass = (bool)args[0];
		return TextCommandResult.Success("Decopass now " + (TerraGenConfig.DoDecorationPass ? "on" : "off"));
	}

	private TextCommandResult OnCmdAutogen(TextCommandCallingArgs args)
	{
		api.WorldManager.AutoGenerateChunks = (bool)args[0];
		return TextCommandResult.Success("Autogen now " + (api.WorldManager.AutoGenerateChunks ? "on" : "off"));
	}

	private TextCommandResult OnCmdGt(TextCommandCallingArgs args)
	{
		TerraGenConfig.GenerateVegetation = (bool)args[0];
		return TextCommandResult.Success("Generate trees now " + (TerraGenConfig.GenerateVegetation ? "on" : "off"));
	}

	private TextCommandResult OnCmdRegenk(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		string landform = args[1] as string;
		return RegenChunks(args.Caller, range, landform, aroundPlayer: true);
	}

	private TextCommandResult OnCmdRegen(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		string landform = args[1] as string;
		return RegenChunks(args.Caller, range, landform, aroundPlayer: true, randomSeed: false, deleteRegion: true);
	}

	private TextCommandResult OnCmdRegenr(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		string landform = args[1] as string;
		return RegenChunks(args.Caller, range, landform, aroundPlayer: true, randomSeed: true, deleteRegion: true);
	}

	private TextCommandResult OnCmdRegenc(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		string landform = args[1] as string;
		return RegenChunks(args.Caller, range, landform, aroundPlayer: false, randomSeed: false, deleteRegion: true);
	}

	private TextCommandResult OnCmdRegenrc(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		string landform = args[1] as string;
		return RegenChunks(args.Caller, range, landform, aroundPlayer: false, randomSeed: true, deleteRegion: true);
	}

	private TextCommandResult OnCmdPregen(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		return PregenerateChunksAroundPlayer(args.Caller, range);
	}

	private TextCommandResult OnCmdDelrock(TextCommandCallingArgs args)
	{
		int rad = (int)args[0];
		DelRock(args.Caller, rad, aroundPlayer: true);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdDelrockc(TextCommandCallingArgs args)
	{
		int rad = (int)args[0];
		DelRock(args.Caller, rad);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdDel(TextCommandCallingArgs args)
	{
		int rad = (int)args[0];
		string landforms = args[1] as string;
		return Regen(args.Caller, rad, onlydelete: true, landforms, aroundPlayer: true);
	}

	private TextCommandResult OnCmdDelr(TextCommandCallingArgs args)
	{
		int rad = (int)args[0];
		return Regen(args.Caller, rad, onlydelete: true, null, aroundPlayer: true, deleteRegion: true);
	}

	private TextCommandResult OnCmdDelrange(TextCommandCallingArgs args)
	{
		int x = (int)args[0];
		int y = (int)args[1];
		int x2 = (int)args[2];
		int y2 = (int)args[3];
		return DelChunkRange(new Vec2i(x, y), new Vec2i(x2, y2));
	}

	private TextCommandResult OnCmdTree(TextCommandCallingArgs args)
	{
		string asset = args[0] as string;
		float size = (float)args[1];
		float aheadoffset = (float)args[2];
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		return TestTree(player, asset, size, aheadoffset);
	}

	private TextCommandResult OnCmdTreelineup(TextCommandCallingArgs args)
	{
		string asset = args[0] as string;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		return TreeLineup(player, asset);
	}

	private TextCommandResult OnCmdGenmapClimate(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int num = api.WorldManager.RegionSize / TerraGenConfig.climateMapScale;
		int num2 = asBlockPos.X / api.WorldManager.RegionSize;
		int num3 = asBlockPos.Z / api.WorldManager.RegionSize;
		int x = num2 * num - 256;
		int z = num3 * num - 256;
		GenMaps modSystem = api.ModLoader.GetModSystem<GenMaps>();
		modSystem.initWorldGen();
		MapLayerBase climateGen = modSystem.climateGen;
		if (!args.Parsers[0].IsMissing)
		{
			float geologicActivityStrength = (float)args[0];
			(((climateGen as MapLayerPerlinWobble).parent as MapLayerClimate).noiseMap as NoiseClimateRealistic).GeologicActivityStrength = geologicActivityStrength;
			climateGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, x, z, "climatemap-" + geologicActivityStrength);
			NoiseBase.Debug = false;
			return TextCommandResult.Success("Geo activity map generated");
		}
		climateGen.DebugDrawBitmap(DebugDrawMode.RGB, x, z, "climatemap");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Climate map generated");
	}

	private TextCommandResult OnCmdGenmapForest(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenMaps modSystem = api.ModLoader.GetModSystem<GenMaps>();
		modSystem.initWorldGen();
		MapLayerBase forestGen = modSystem.forestGen;
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int num = asBlockPos.X / api.WorldManager.RegionSize;
		int num2 = asBlockPos.Z / api.WorldManager.RegionSize;
		int num3 = api.WorldManager.RegionSize / TerraGenConfig.forestMapScale;
		int x = num * num3 - 256;
		int z = num2 * num3 - 256;
		forestGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, x, z, "forestmap");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Forest map generated");
	}

	private TextCommandResult OnCmdGenmapUpheavel(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenMaps modSystem = api.ModLoader.GetModSystem<GenMaps>();
		modSystem.initWorldGen();
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int num = asBlockPos.X / api.WorldManager.RegionSize;
		int num2 = asBlockPos.Z / api.WorldManager.RegionSize;
		MapLayerBase upheavelGen = modSystem.upheavelGen;
		int num3 = api.WorldManager.RegionSize / TerraGenConfig.geoUpheavelMapScale;
		int x = num * num3 - 256;
		int z = num2 * num3 - 256;
		int size = (int)args[0];
		upheavelGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, x, z, size, "upheavelmap");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Upheavel map generated");
	}

	private TextCommandResult OnCmdGenmapMushroom(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		api.ModLoader.GetModSystem<GenMaps>().initWorldGen();
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int num = asBlockPos.X / api.WorldManager.RegionSize;
		int num2 = asBlockPos.Z / api.WorldManager.RegionSize;
		int num3 = api.WorldManager.RegionSize / TerraGenConfig.forestMapScale;
		int x = num * num3 - 256;
		int z = num2 * num3 - 256;
		new MapLayerWobbled(api.World.Seed + 112897, 2, 0.9f, TerraGenConfig.forestMapScale, 4000f, -3000).DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, x, z, "mushroom");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Mushroom maps generated");
	}

	private TextCommandResult OnCmdGenmapOre(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		NoiseBase.Debug = false;
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdGenmapGprov(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		int num = api.WorldManager.RegionSize / TerraGenConfig.geoProvMapScale;
		GenMaps modSystem = api.ModLoader.GetModSystem<GenMaps>();
		modSystem.initWorldGen();
		MapLayerBase geologicprovinceGen = modSystem.geologicprovinceGen;
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int num2 = asBlockPos.X / api.WorldManager.RegionSize;
		int num3 = asBlockPos.Z / api.WorldManager.RegionSize;
		int x = num2 * num - 256;
		int z = num3 * num - 256;
		geologicprovinceGen.DebugDrawBitmap(DebugDrawMode.ProvinceRGB, x, z, "gprovmap");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Province map generated");
	}

	private TextCommandResult OnCmdGenmapLandform(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		int num = api.WorldManager.RegionSize / TerraGenConfig.landformMapScale;
		GenMaps modSystem = api.ModLoader.GetModSystem<GenMaps>();
		modSystem.initWorldGen();
		MapLayerBase landformsGen = modSystem.landformsGen;
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int num2 = asBlockPos.X / api.WorldManager.RegionSize;
		int num3 = asBlockPos.Z / api.WorldManager.RegionSize;
		int x = num2 * num - 256;
		int z = num3 * num - 256;
		landformsGen.DebugDrawBitmap(DebugDrawMode.LandformRGB, x, z, "landformmap");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Landforms map generated");
	}

	private TextCommandResult OnCmdGenmapOcean(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenMaps modSystem = api.ModLoader.GetModSystem<GenMaps>();
		modSystem.initWorldGen();
		MapLayerBase oceanGen = modSystem.oceanGen;
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int num = asBlockPos.X / api.WorldManager.RegionSize;
		int num2 = asBlockPos.Z / api.WorldManager.RegionSize;
		int num3 = api.WorldManager.RegionSize / TerraGenConfig.oceanMapScale;
		int x = num * num3 - 256;
		int z = num2 * num3 - 256;
		oceanGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, x, z, "oceanmap");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Ocean map generated");
	}

	private TextCommandResult OnCmdStitch(TextCommandCallingArgs args)
	{
		BlockPos asBlockPos = args.Caller.Entity.Pos.AsBlockPos;
		IServerChunk chunk = api.WorldManager.GetChunk(asBlockPos);
		if (chunk == null)
		{
			return TextCommandResult.Success("Can't check here, beyond chunk boundaries!");
		}
		IMapRegion mapRegion = chunk.MapChunk.MapRegion;
		int num = asBlockPos.X / _regionSize;
		int num2 = asBlockPos.Z / _regionSize;
		MapLayerBase climateGen = api.ModLoader.GetModSystem<GenMaps>().climateGen;
		NoiseBase.Debug = true;
		int innerSize = mapRegion.ClimateMap.InnerSize;
		int num3 = innerSize * 3;
		int[] array = new int[num3 * num3];
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				IntDataMap2D intDataMap2D = OnMapRegionGen(num + i, num2 + j, climateGen);
				for (int k = 0; k < innerSize; k++)
				{
					for (int l = 0; l < innerSize; l++)
					{
						int unpaddedInt = intDataMap2D.GetUnpaddedInt(k, l);
						int num4 = (j + 1) * innerSize + l;
						int num5 = (i + 1) * innerSize + k;
						array[num4 * num3 + num5] = unpaddedInt;
					}
				}
			}
		}
		NoiseBase.DebugDrawBitmap(DebugDrawMode.RGB, array, num3, "climated-3x3-stitch");
		NoiseBase.Debug = false;
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdRegionsOre(TextCommandCallingArgs args)
	{
		BlockPos asBlockPos = args.Caller.Entity.Pos.AsBlockPos;
		IServerChunk chunk = api.WorldManager.GetChunk(asBlockPos);
		if (chunk == null)
		{
			return TextCommandResult.Success("Can't check here, beyond chunk boundaries!");
		}
		IMapRegion mapRegion = chunk.MapChunk.MapRegion;
		int num = asBlockPos.X / _regionSize;
		int num2 = asBlockPos.Z / _regionSize;
		int num3 = (int)args[0];
		NoiseBase.Debug = false;
		string text = (args.Parsers[1].IsMissing ? "limonite" : (args[1] as string));
		if (!mapRegion.OreMaps.ContainsKey(text))
		{
			return TextCommandResult.Success("Mapregion does not contain an ore map for ore " + text);
		}
		int innerSize = mapRegion.OreMaps[text].InnerSize;
		int num4 = (2 * num3 + 1) * innerSize;
		int[] array = new int[num4 * num4];
		GenDeposits modSystem = api.ModLoader.GetModSystem<GenDeposits>();
		api.ModLoader.GetModSystem<GenDeposits>().initWorldGen();
		for (int i = -num3; i <= num3; i++)
		{
			for (int j = -num3; j <= num3; j++)
			{
				mapRegion = api.World.BlockAccessor.GetMapRegion(num + i, num2 + j);
				if (mapRegion == null)
				{
					continue;
				}
				mapRegion.OreMaps.Clear();
				modSystem.OnMapRegionGen(mapRegion, num + i, num2 + j);
				if (!mapRegion.OreMaps.ContainsKey(text))
				{
					return TextCommandResult.Success("Mapregion does not contain an ore map for ore " + text);
				}
				IntDataMap2D intDataMap2D = mapRegion.OreMaps[text];
				int num5 = (i + num3) * innerSize;
				int num6 = (j + num3) * innerSize;
				for (int k = 0; k < intDataMap2D.InnerSize; k++)
				{
					for (int l = 0; l < intDataMap2D.InnerSize; l++)
					{
						int unpaddedInt = intDataMap2D.GetUnpaddedInt(k, l);
						array[(l + num6) * num4 + k + num5] = unpaddedInt;
					}
				}
			}
		}
		NoiseBase.Debug = true;
		NoiseBase.DebugDrawBitmap(DebugDrawMode.RGB, array, num4, "ore-" + text + "around-" + num + "-" + num2);
		NoiseBase.Debug = false;
		return TextCommandResult.Success(text + " ore map generated.");
	}

	private TextCommandResult OnCmdRegionsClimate(TextCommandCallingArgs args)
	{
		BlockPos asBlockPos = args.Caller.Entity.Pos.AsBlockPos;
		IServerChunk chunk = api.WorldManager.GetChunk(asBlockPos);
		if (chunk == null)
		{
			return TextCommandResult.Success("Can't check here, beyond chunk boundaries!");
		}
		IMapRegion mapRegion = chunk.MapChunk.MapRegion;
		int num = asBlockPos.X / _regionSize;
		int num2 = asBlockPos.Z / _regionSize;
		int num3 = (int)args[0];
		NoiseBase.Debug = false;
		int innerSize = mapRegion.ClimateMap.InnerSize;
		int num4 = (2 * num3 + 1) * innerSize;
		int[] array = new int[num4 * num4];
		for (int i = -num3; i <= num3; i++)
		{
			for (int j = -num3; j <= num3; j++)
			{
				mapRegion = api.World.BlockAccessor.GetMapRegion(num + i, num2 + j);
				if (mapRegion == null)
				{
					continue;
				}
				IntDataMap2D climateMap = mapRegion.ClimateMap;
				int num5 = (i + num3) * innerSize;
				int num6 = (j + num3) * innerSize;
				for (int k = 0; k < climateMap.InnerSize; k++)
				{
					for (int l = 0; l < climateMap.InnerSize; l++)
					{
						int unpaddedInt = climateMap.GetUnpaddedInt(k, l);
						array[(l + num6) * num4 + k + num5] = unpaddedInt;
					}
				}
			}
		}
		NoiseBase.Debug = true;
		NoiseBase.DebugDrawBitmap(DebugDrawMode.RGB, array, num4, "climates-" + num + "-" + num2 + "-" + num3);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("climate map generated.");
	}

	private TextCommandResult OnCmdRegionsUpheavel(TextCommandCallingArgs args)
	{
		BlockPos asBlockPos = args.Caller.Entity.Pos.AsBlockPos;
		IServerChunk chunk = api.WorldManager.GetChunk(asBlockPos);
		if (chunk == null)
		{
			return TextCommandResult.Success("Can't check here, beyond chunk boundaries!");
		}
		IMapRegion mapRegion = chunk.MapChunk.MapRegion;
		int num = asBlockPos.X / _regionSize;
		int num2 = asBlockPos.Z / _regionSize;
		int num3 = (int)args[0];
		NoiseBase.Debug = false;
		int innerSize = mapRegion.UpheavelMap.InnerSize;
		int num4 = (2 * num3 + 1) * innerSize;
		int[] array = new int[num4 * num4];
		for (int i = -num3; i <= num3; i++)
		{
			for (int j = -num3; j <= num3; j++)
			{
				mapRegion = api.World.BlockAccessor.GetMapRegion(num + i, num2 + j);
				if (mapRegion == null)
				{
					continue;
				}
				IntDataMap2D upheavelMap = mapRegion.UpheavelMap;
				int num5 = (i + num3) * innerSize;
				int num6 = (j + num3) * innerSize;
				for (int k = 0; k < upheavelMap.InnerSize; k++)
				{
					for (int l = 0; l < upheavelMap.InnerSize; l++)
					{
						int unpaddedInt = upheavelMap.GetUnpaddedInt(k, l);
						array[(l + num6) * num4 + k + num5] = unpaddedInt;
					}
				}
			}
		}
		NoiseBase.Debug = true;
		NoiseBase.DebugDrawBitmap(DebugDrawMode.RGB, array, num4, "upheavels-" + num + "-" + num2 + "-" + num3);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("upheavel map generated.");
	}

	private TextCommandResult OnCmdPos(TextCommandCallingArgs args)
	{
		//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ff: Expected O, but got Unknown
		//IL_0440: Unknown result type (might be due to invalid IL or missing references)
		int num = 32;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		BlockPos asBlockPos = args.Caller.Entity.Pos.AsBlockPos;
		IServerChunk chunk = api.WorldManager.GetChunk(asBlockPos);
		if (chunk == null)
		{
			return TextCommandResult.Success("Can't check here, beyond chunk boundaries!");
		}
		IMapRegion mapRegion = chunk.MapChunk.MapRegion;
		IMapChunk mapChunk = chunk.MapChunk;
		int num2 = api.WorldManager.RegionSize / num;
		int num3 = asBlockPos.X % num;
		int num4 = asBlockPos.Z % num;
		int num5 = asBlockPos.X / num;
		int num6 = asBlockPos.Z / num;
		int num7 = asBlockPos.X / _regionSize;
		int num8 = asBlockPos.Z / _regionSize;
		switch (args[0] as string)
		{
		case "ymax":
			return TextCommandResult.Success($"YMax: {chunk.MapChunk.YMax}");
		case "coords":
			return TextCommandResult.Success($"Chunk X/Z: {num5}/{num6}, Region X/Z: {num7},{num8}");
		case "latitude":
		{
			double? num21 = api.World.Calendar.OnGetLatitude(asBlockPos.Z);
			return TextCommandResult.Success(string.Format("Latitude: {0:0.##}°, {1}", num21 * 90.0, (num21 < 0.0) ? "Southern Hemisphere" : "Northern Hemisphere"));
		}
		case "structures":
		{
			bool found = false;
			api.World.BlockAccessor.WalkStructures(asBlockPos, delegate(GeneratedStructure struc)
			{
				found = true;
				player.SendMessage(args.Caller.FromChatGroupId, "Structure with code " + struc.Code + " at this position", EnumChatType.CommandSuccess);
			});
			if (!found)
			{
				return TextCommandResult.Success("No structures at this position");
			}
			break;
		}
		case "height":
		{
			string message2 = $"Rain y={chunk.MapChunk.RainHeightMap[num4 * num + num3]}, Worldgen terrain y={chunk.MapChunk.WorldGenTerrainHeightMap[num4 * num + num3]}";
			player.SendMessage(args.Caller.FromChatGroupId, message2, EnumChatType.CommandSuccess);
			break;
		}
		case "cavedistort":
		{
			SKBitmap val = new SKBitmap(num, num, false);
			for (int num19 = 0; num19 < num; num19++)
			{
				for (int num20 = 0; num20 < num; num20++)
				{
					byte b = mapChunk.CaveHeightDistort[num20 * num + num19];
					val.SetPixel(num19, num20, new SKColor((byte)((b >> 16) & 0xFF), (byte)((b >> 8) & 0xFF), (byte)(b & 0xFF)));
				}
			}
			val.Save("cavedistort" + num5 + "-" + num6 + ".png");
			player.SendMessage(args.Caller.FromChatGroupId, "saved bitmap cavedistort" + num5 + "-" + num6 + ".png", EnumChatType.CommandSuccess);
			break;
		}
		case "gprov":
		{
			int innerSize3 = mapRegion.GeologicProvinceMap.InnerSize;
			float x3 = ((float)asBlockPos.X / (float)_regionSize - (float)num7) * (float)innerSize3;
			float z3 = ((float)asBlockPos.Z / (float)_regionSize - (float)num8) * (float)innerSize3;
			GeologicProvinceVariant[] variants = NoiseGeoProvince.provinces.Variants;
			WeightedIndex[] array5 = new LerpedWeightedIndex2DMap(mapRegion.GeologicProvinceMap.Data, mapRegion.GeologicProvinceMap.Size, TerraGenConfig.geoProvSmoothingRadius, mapRegion.GeologicProvinceMap.TopLeftPadding, mapRegion.GeologicProvinceMap.BottomRightPadding)[x3, z3];
			string text2 = "";
			WeightedIndex[] array2 = array5;
			for (int l = 0; l < array2.Length; l++)
			{
				WeightedIndex weightedIndex2 = array2[l];
				if (text2.Length > 0)
				{
					text2 += ", ";
				}
				text2 = text2 + (100f * weightedIndex2.Weight).ToString("#.#") + "% " + variants[weightedIndex2.Index].Code;
			}
			player.SendMessage(args.Caller.FromChatGroupId, text2, EnumChatType.CommandSuccess);
			break;
		}
		case "rockstrata":
		{
			GenRockStrataNew modSystem = api.ModLoader.GetModSystem<GenRockStrataNew>();
			int innerSize2 = mapRegion.GeologicProvinceMap.InnerSize;
			float x2 = ((float)asBlockPos.X / (float)_regionSize - (float)(asBlockPos.X / _regionSize)) * (float)innerSize2;
			float z2 = ((float)asBlockPos.Z / (float)_regionSize - (float)(asBlockPos.Z / _regionSize)) * (float)innerSize2;
			_ = NoiseGeoProvince.provinces.Variants;
			WeightedIndex[] array3 = new LerpedWeightedIndex2DMap(mapRegion.GeologicProvinceMap.Data, mapRegion.GeologicProvinceMap.Size, TerraGenConfig.geoProvSmoothingRadius, mapRegion.GeologicProvinceMap.TopLeftPadding, mapRegion.GeologicProvinceMap.BottomRightPadding)[x2, z2];
			float[] array4 = new float[4];
			array4[0] = (array4[1] = (array4[2] = (array4[3] = 0f)));
			int num9 = num5 % num2;
			int num10 = num6 % num2;
			float num11 = 0f;
			float num12 = (float)modSystem.distort2dx.Noise(asBlockPos.X, asBlockPos.Z);
			float num13 = (float)modSystem.distort2dz.Noise(asBlockPos.X, asBlockPos.Z);
			for (int j = 0; j < array3.Length; j++)
			{
				float weight = array3[j].Weight;
				GeologicProvinceVariant geologicProvinceVariant = NoiseGeoProvince.provinces.Variants[array3[j].Index];
				array4[0] += geologicProvinceVariant.RockStrataIndexed[0].ScaledMaxThickness * weight;
				array4[1] += geologicProvinceVariant.RockStrataIndexed[1].ScaledMaxThickness * weight;
				array4[2] += geologicProvinceVariant.RockStrataIndexed[2].ScaledMaxThickness * weight;
				array4[3] += geologicProvinceVariant.RockStrataIndexed[3].ScaledMaxThickness * weight;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Sedimentary max thickness: " + array4[0]);
			stringBuilder.AppendLine("Metamorphic max thickness: " + array4[1]);
			stringBuilder.AppendLine("Igneous max thickness: " + array4[2]);
			stringBuilder.AppendLine("Volcanic max thickness: " + array4[3]);
			stringBuilder.AppendLine("========");
			for (int k = 0; k < modSystem.strata.Variants.Length; k++)
			{
				IntDataMap2D intDataMap2D = mapChunk.MapRegion.RockStrata[k];
				num11 = (float)intDataMap2D.InnerSize / (float)num2;
				GameMath.Clamp((num12 + num13) / 30f, 0.9f, 1.1f);
				stringBuilder.AppendLine(modSystem.strata.Variants[k].BlockCode.ToShortString() + " max thickness: " + intDataMap2D.GetIntLerpedCorrectly((float)num9 * num11 + num11 * ((float)num3 + num12) / (float)num, (float)num10 * num11 + num11 * ((float)num4 + num13) / (float)num));
			}
			stringBuilder.AppendLine("======");
			int terrainMapheightAt = api.World.BlockAccessor.GetTerrainMapheightAt(asBlockPos);
			int num14 = 1;
			int num15 = terrainMapheightAt;
			int num16 = -1;
			float num17 = 0f;
			RockStratum rockStratum = null;
			OrderedDictionary<int, int> orderedDictionary = new OrderedDictionary<int, int>();
			while (num14 <= num15)
			{
				if ((num17 -= 1f) <= 0f)
				{
					num16++;
					if (num16 >= modSystem.strata.Variants.Length)
					{
						break;
					}
					rockStratum = modSystem.strata.Variants[num16];
					IntDataMap2D intDataMap2D = mapChunk.MapRegion.RockStrata[num16];
					num11 = (float)intDataMap2D.InnerSize / (float)num2;
					int rockGroup = (int)rockStratum.RockGroup;
					float num18 = 1f + GameMath.Clamp((num12 + num13) / 30f, 0.9f, 1.1f);
					num17 = Math.Min(array4[rockGroup] * num18, intDataMap2D.GetIntLerpedCorrectly((float)num9 * num11 + num11 * ((float)num3 + num12) / (float)num, (float)num10 * num11 + num11 * ((float)num4 + num13) / (float)num));
					num17 -= ((rockStratum.RockGroup == EnumRockGroup.Sedimentary) ? ((float)Math.Max(0, num15 - TerraGenConfig.seaLevel) * 0.5f) : 0f);
					if (num17 < 2f)
					{
						num17 = -1f;
						continue;
					}
				}
				if (!orderedDictionary.ContainsKey(rockStratum.BlockId))
				{
					orderedDictionary[rockStratum.BlockId] = 0;
				}
				orderedDictionary[rockStratum.BlockId]++;
				if (rockStratum.GenDir == EnumStratumGenDir.BottomUp)
				{
					num14++;
				}
				else
				{
					num15--;
				}
			}
			foreach (KeyValuePair<int, int> item in orderedDictionary)
			{
				stringBuilder.AppendLine(api.World.Blocks[item.Key].Code.ToShortString() + " : " + item.Value + " blocks");
			}
			player.SendMessage(args.Caller.FromChatGroupId, stringBuilder.ToString(), EnumChatType.CommandSuccess);
			break;
		}
		case "landform":
		{
			int innerSize = mapRegion.LandformMap.InnerSize;
			float x = ((float)asBlockPos.X / (float)_regionSize - (float)(asBlockPos.X / _regionSize)) * (float)innerSize;
			float z = ((float)asBlockPos.Z / (float)_regionSize - (float)(asBlockPos.Z / _regionSize)) * (float)innerSize;
			LandformVariant[] landFormsByIndex = NoiseLandforms.landforms.LandFormsByIndex;
			IntDataMap2D landformMap = mapRegion.LandformMap;
			WeightedIndex[] array = new LerpedWeightedIndex2DMap(landformMap.Data, mapRegion.LandformMap.Size, TerraGenConfig.landFormSmoothingRadius, landformMap.TopLeftPadding, landformMap.BottomRightPadding)[x, z];
			string text = "";
			WeightedIndex[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				WeightedIndex weightedIndex = array2[i];
				if (text.Length > 0)
				{
					text += ", ";
				}
				text = text + (100f * weightedIndex.Weight).ToString("#.#") + "% " + landFormsByIndex[weightedIndex.Index].Code.ToShortString();
			}
			player.SendMessage(args.Caller.FromChatGroupId, text, EnumChatType.CommandSuccess);
			break;
		}
		case "climate":
		{
			ClimateCondition climateAt = api.World.BlockAccessor.GetClimateAt(asBlockPos);
			string message = string.Format("Temperature: {0}°C, Year avg: {1}°C, Avg. Rainfall: {2}%, Geologic Activity: {3}%, Fertility: {4}%, Forest: {5}%, Shrub: {6}%, Sealevel dist: {7}%, Season: {8}, Hemisphere: {9}", climateAt.Temperature.ToString("0.#"), climateAt.WorldGenTemperature.ToString("0.#"), (int)(climateAt.WorldgenRainfall * 100f), (int)(climateAt.GeologicActivity * 100f), (int)(climateAt.Fertility * 100f), (int)(climateAt.ForestDensity * 100f), (int)(climateAt.ShrubDensity * 100f), (int)(100f * (float)asBlockPos.Y / 255f), api.World.Calendar.GetSeason(asBlockPos), api.World.Calendar.GetHemisphere(asBlockPos));
			player.SendMessage(args.Caller.FromChatGroupId, message, EnumChatType.CommandSuccess);
			break;
		}
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdTestnoise(TextCommandCallingArgs args)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		int quantityOctaves = (int)args[0];
		long seed = new Random().Next();
		NormalizedSimplexNoise normalizedSimplexNoise = NormalizedSimplexNoise.FromDefaultOctaves(quantityOctaves, 5.0, 0.7, seed);
		int num = 800;
		SKBitmap val = new SKBitmap(num, num, false);
		int num2 = 0;
		int num3 = 0;
		float val2 = 1f;
		float val3 = 0f;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				double num4 = (flag ? normalizedSimplexNoise.Noise((double)i / (double)num, 0.0, (double)j / (double)num) : normalizedSimplexNoise.Noise((double)i / (double)num, (double)j / (double)num));
				if (num4 < 0.0)
				{
					num2++;
					num4 = 0.0;
				}
				if (num4 > 1.0)
				{
					num3++;
					num4 = 1.0;
				}
				val2 = Math.Min((float)num4, val2);
				val3 = Math.Max((float)num4, val3);
				byte b = (byte)(num4 * 255.0);
				val.SetPixel(i, j, new SKColor(b, b, b, byte.MaxValue));
			}
		}
		val.Save("noise.png");
		string text = (flag ? "3D" : "2D") + " Noise (" + quantityOctaves + " Octaves) saved to noise.png. Overflows: " + num3 + ", Underflows: " + num2;
		text = text + "\nNoise min = " + val2.ToString("0.##") + ", max= " + val3.ToString("0.##");
		return TextCommandResult.Success(text);
	}

	private TextCommandResult OnCmdClimate(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		NoiseClimatePatchy climateNoise = new NoiseClimatePatchy(_seed);
		GenMaps.GetClimateMapGen(_seed, climateNoise);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Patchy climate map generated");
	}

	private TextCommandResult OnCmdGeoact(TextCommandCallingArgs args)
	{
		int polarEquatorDistance = api.WorldManager.SaveGame.WorldConfiguration.GetString("polarEquatorDistance", "50000").ToInt(50000);
		int sizeZ = (int)args[0];
		int spawnMinTemp = 6;
		int spawnMaxTemp = 14;
		NoiseBase.Debug = true;
		NoiseClimateRealistic climateNoise = new NoiseClimateRealistic(_seed, api.World.BlockAccessor.MapSizeZ / TerraGenConfig.climateMapScale / TerraGenConfig.climateMapSubScale, polarEquatorDistance, spawnMinTemp, spawnMaxTemp);
		MapLayerBase climateMapGen = GenMaps.GetClimateMapGen(_seed, climateNoise);
		NoiseBase.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, climateMapGen.GenLayer(0, 0, 128, 2048), 128, sizeZ, "geoactivity");
		return TextCommandResult.Success("Geologic activity map generated");
	}

	private TextCommandResult OnCmdClimater(TextCommandCallingArgs args)
	{
		ITreeAttribute worldConfiguration = api.WorldManager.SaveGame.WorldConfiguration;
		int polarEquatorDistance = worldConfiguration.GetString("polarEquatorDistance", "50000").ToInt(50000);
		int spawnMinTemp = 6;
		int spawnMaxTemp = 14;
		switch (worldConfiguration.GetString("worldClimate", "realistic"))
		{
		case "hot":
			spawnMinTemp = 28;
			spawnMaxTemp = 32;
			break;
		case "warm":
			spawnMinTemp = 19;
			spawnMaxTemp = 23;
			break;
		case "cool":
			spawnMinTemp = -5;
			spawnMaxTemp = 1;
			break;
		case "icy":
			spawnMinTemp = -15;
			spawnMaxTemp = -10;
			break;
		}
		NoiseBase.Debug = true;
		NoiseClimateRealistic climateNoise = new NoiseClimateRealistic(_seed, api.World.BlockAccessor.MapSizeZ / TerraGenConfig.climateMapScale / TerraGenConfig.climateMapSubScale, polarEquatorDistance, spawnMinTemp, spawnMaxTemp);
		MapLayerBase climateMapGen = GenMaps.GetClimateMapGen(_seed, climateNoise);
		NoiseBase.DebugDrawBitmap(DebugDrawMode.RGB, climateMapGen.GenLayer(0, 0, 128, 2048), 128, 2048, "realisticlimate");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Realistic climate map generated");
	}

	private TextCommandResult OnCmdForest(TextCommandCallingArgs args)
	{
		NoiseClimatePatchy climateNoise = new NoiseClimatePatchy(_seed);
		MapLayerBase climateMapGen = GenMaps.GetClimateMapGen(_seed, climateNoise);
		MapLayerBase forestMapGen = GenMaps.GetForestMapGen(_seed + 1, TerraGenConfig.forestMapScale);
		IntDataMap2D inputMap = new IntDataMap2D
		{
			Data = climateMapGen.GenLayer(0, 0, 512, 512),
			Size = 512
		};
		forestMapGen.SetInputMap(inputMap, new IntDataMap2D
		{
			Size = 512
		});
		NoiseBase.Debug = true;
		forestMapGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, 0, 0, "Forest 1 - Forest");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Forest map generated");
	}

	private TextCommandResult OnCmdUpheavel(TextCommandCallingArgs args)
	{
		int size = (int)args[0];
		MapLayerBase geoUpheavelMapGen = GenMaps.GetGeoUpheavelMapGen(_seed + 873, TerraGenConfig.geoUpheavelMapScale);
		NoiseBase.Debug = true;
		geoUpheavelMapGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, 0, 0, size, "Geoupheavel 1");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Geo upheavel map generated");
	}

	private TextCommandResult OnCmdOcean(TextCommandCallingArgs args)
	{
		ITreeAttribute worldConfiguration = api.WorldManager.SaveGame.WorldConfiguration;
		int size = (int)args[0];
		float landcover = worldConfiguration.GetString("landcover", "1").ToFloat(1f);
		float oceanScaleMul = worldConfiguration.GetString("oceanscale", "1").ToFloat(1f);
		int num = 32;
		List<XZ> requireLandAt = api.ModLoader.GetModSystem<GenMaps>().requireLandAt;
		int x = 0;
		int z = 0;
		if (args.Caller.Player != null)
		{
			x = (int)args.Caller.Player.Entity.Pos.X / num;
			z = (int)args.Caller.Player.Entity.Pos.Z / num;
		}
		bool requiresSpawnOffset = GameVersion.IsLowerVersionThan(api.WorldManager.SaveGame.CreatedGameVersion, "1.20.0-pre.14");
		MapLayerBase oceanMapGen = GenMaps.GetOceanMapGen(_seed + 1873, landcover, TerraGenConfig.oceanMapScale, oceanScaleMul, requireLandAt, requiresSpawnOffset);
		NoiseBase.Debug = true;
		oceanMapGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, x, z, size, "Ocean 1-" + x + "-" + z);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Ocean map generated");
	}

	private TextCommandResult OnCmdOre(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		NoiseOre oreNoise = new NoiseOre(_seed);
		float scaleMul = (float)args[0];
		float contrast = (float)args[1];
		float sub = (float)args[2];
		GenMaps.GetOreMap(_seed, oreNoise, scaleMul, contrast, sub);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("ore map generated");
	}

	private TextCommandResult OnCmdOretopdistort(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenMaps.GetDepositVerticalDistort(_seed);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Ore top distort map generated");
	}

	private TextCommandResult OnCmdWind(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenMaps.GetDebugWindMap(_seed);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Wind map generated");
	}

	private TextCommandResult OnCmdGprov(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenMaps.GetGeologicProvinceMapGen(_seed, api);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Province map generated");
	}

	private TextCommandResult OnCmdLandform(TextCommandCallingArgs args)
	{
		ITreeAttribute worldConfiguration = api.WorldManager.SaveGame.WorldConfiguration;
		NoiseBase.Debug = true;
		NoiseClimatePatchy climateNoise = new NoiseClimatePatchy(_seed);
		float landformScale = worldConfiguration.GetString("landformScale", "1").ToFloat(1f);
		GenMaps.GetLandformMapGen(_seed + 1, climateNoise, api, landformScale);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Landforms map generated");
	}

	private TextCommandResult OnCmdRockstrata(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenRockStrataNew modSystem = api.ModLoader.GetModSystem<GenRockStrataNew>();
		for (int i = 0; i < modSystem.strataNoises.Length; i++)
		{
			modSystem.strataNoises[i].DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, 0, 0, "Rockstrata-" + modSystem.strata.Variants[i].BlockCode.ToShortString().Replace(":", "-"));
		}
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Rockstrata maps generated");
	}

	private TextCommandResult OnCmdTreemap(TextCommandCallingArgs args)
	{
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Expected O, but got Unknown
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		int num = 3;
		byte[] array = new byte[131072 * num];
		int num2 = 256;
		for (int i = 0; i < 256; i++)
		{
			for (int j = 0; j < 256; j++)
			{
				array[(j * num2 + i) * num] = byte.MaxValue;
				array[(j * num2 + i) * num + 1] = byte.MaxValue;
				array[(j * num2 + i) * num + 2] = byte.MaxValue;
			}
		}
		WgenTreeSupplier wgenTreeSupplier = new WgenTreeSupplier(api);
		wgenTreeSupplier.LoadTrees();
		TreeVariant[] treeGens = wgenTreeSupplier.treeGenProps.TreeGens;
		Random random = new Random(123);
		int[] array2 = new int[treeGens.Length];
		for (int k = 0; k < array2.Length; k++)
		{
			array2[k] = random.Next() | int.MinValue;
		}
		ImageSurface val = (ImageSurface)ImageSurface.CreateForImage(array, (Format)1, 256, 512);
		Context val2 = new Context((Surface)val);
		((Surface)val).WriteToPng("treecoveragemap.png");
		val2.Dispose();
		((Surface)val).Dispose();
		return TextCommandResult.Success("treecoveragemap.png created.");
	}

	private TextCommandResult DelChunkRange(Vec2i start, Vec2i end)
	{
		for (int i = start.X; i <= end.X; i++)
		{
			for (int j = start.Y; j <= end.Y; j++)
			{
				api.WorldManager.DeleteChunkColumn(i, j);
			}
		}
		return TextCommandResult.Success("Ok, chunk deletions enqueued, might take a while to process. Run command without args to see queue size");
	}

	private void DelRock(Caller caller, int rad, bool aroundPlayer = false)
	{
		IServerPlayer serverPlayer = caller.Player as IServerPlayer;
		serverPlayer.SendMessage(caller.FromChatGroupId, "Deleting rock, this may take a while...", EnumChatType.CommandError);
		int num = api.WorldManager.MapSizeX / 32 / 2;
		int num2 = api.WorldManager.MapSizeZ / 32 / 2;
		if (aroundPlayer)
		{
			num = (int)serverPlayer.Entity.Pos.X / 32;
			num2 = (int)serverPlayer.Entity.Pos.Z / 32;
		}
		List<Vec2i> list = new List<Vec2i>();
		for (int i = -rad; i <= rad; i++)
		{
			for (int j = -rad; j <= rad; j++)
			{
				list.Add(new Vec2i(num + i, num2 + j));
			}
		}
		int num3 = 32;
		IList<Block> blocks = api.World.Blocks;
		foreach (Vec2i item in list)
		{
			for (int k = 0; k < api.WorldManager.MapSizeY / 32; k++)
			{
				IServerChunk chunk = api.WorldManager.GetChunk(item.X, k, item.Y);
				if (chunk == null)
				{
					continue;
				}
				chunk.Unpack();
				for (int l = 0; l < chunk.Data.Length; l++)
				{
					Block block = blocks[chunk.Data[l]];
					if (block.BlockMaterial == EnumBlockMaterial.Stone || block.BlockMaterial == EnumBlockMaterial.Liquid || block.BlockMaterial == EnumBlockMaterial.Soil)
					{
						chunk.Data[l] = 0;
					}
				}
				chunk.MarkModified();
			}
			api.WorldManager.FullRelight(new BlockPos(item.X * num3, 0, item.Y * num3), new BlockPos(item.X * num3, api.WorldManager.MapSizeY, item.Y * num3));
		}
		serverPlayer.CurrentChunkSentRadius = 0;
	}

	private TextCommandResult PregenerateChunksAroundPlayer(Caller caller, int range)
	{
		int num;
		int num2;
		if (caller.Type == EnumCallerType.Console)
		{
			num = api.WorldManager.MapSizeX / 32 / 2;
			num2 = api.WorldManager.MapSizeX / 32 / 2;
		}
		else
		{
			IServerPlayer obj = caller.Player as IServerPlayer;
			num = (int)obj.Entity.Pos.X / 32;
			num2 = (int)obj.Entity.Pos.Z / 32;
		}
		List<Vec2i> list = new List<Vec2i>();
		for (int i = -range; i <= range; i++)
		{
			for (int j = -range; j <= range; j++)
			{
				list.Add(new Vec2i(num + i, num2 + j));
			}
		}
		LoadColumnsSlow(caller, list, 0);
		return TextCommandResult.Success("Type /debug chunk queue to see current generating queue size");
	}

	private void LoadColumnsSlow(Caller caller, List<Vec2i> coords, int startIndex)
	{
		int num = 0;
		IServerPlayer serverPlayer = caller.Player as IServerPlayer;
		if (api.WorldManager.CurrentGeneratingChunkCount < 10)
		{
			int num2 = 200;
			for (int i = startIndex; i < coords.Count; i++)
			{
				num++;
				startIndex++;
				Vec2i vec2i = coords[i];
				api.WorldManager.LoadChunkColumn(vec2i.X, vec2i.Y);
				if (num > num2)
				{
					break;
				}
			}
			if (caller.Type == EnumCallerType.Console)
			{
				api.Logger.Notification("Ok, added {0} columns, {1} left to add, waiting until these are done.", num, coords.Count - startIndex);
			}
			else
			{
				serverPlayer.SendMessage(caller.FromChatGroupId, $"Ok, added {num} columns, {coords.Count - startIndex} left to add, waiting until these are done.", EnumChatType.CommandSuccess);
			}
		}
		if (startIndex < coords.Count)
		{
			api.World.RegisterCallback(delegate
			{
				LoadColumnsSlow(caller, coords, startIndex);
			}, 1000);
		}
		else if (caller.Type == EnumCallerType.Console)
		{
			api.Logger.Notification("Ok, {0} columns, generated!", coords.Count);
		}
		else
		{
			serverPlayer.SendMessage(caller.FromChatGroupId, $"Ok, {coords.Count} columns, generated!", EnumChatType.CommandSuccess);
		}
	}

	private TextCommandResult RegenChunks(Caller caller, int range, string landform = null, bool aroundPlayer = false, bool randomSeed = false, bool deleteRegion = false)
	{
		int num = 0;
		IServerPlayer serverPlayer = caller.Player as IServerPlayer;
		if (randomSeed)
		{
			num = api.World.Rand.Next(100000);
			serverPlayer.SendMessage(GlobalConstants.CurrentChatGroup, "Using random seed diff " + num, EnumChatType.Notification);
		}
		serverPlayer.SendMessage(GlobalConstants.CurrentChatGroup, "Waiting for chunk thread to pause...", EnumChatType.Notification);
		TextCommandResult result;
		if (api.Server.PauseThread("chunkdbthread"))
		{
			api.Assets.Reload(new AssetLocation("worldgen/"));
			api.ModLoader.GetModSystem<ModJsonPatchLoader>().ApplyPatches("worldgen/");
			NoiseLandforms.LoadLandforms(api);
			api.Event.TriggerInitWorldGen();
			result = Regen(caller, range, onlydelete: false, landform, aroundPlayer, deleteRegion);
		}
		else
		{
			result = TextCommandResult.Success("Unable to regenerate chunks. Was not able to pause the chunk gen thread");
		}
		api.Server.ResumeThread("chunkdbthread");
		return result;
	}

	private TextCommandResult Regen(Caller caller, int rad, bool onlydelete, string landforms, bool aroundPlayer = false, bool deleteRegion = false)
	{
		int chunkMidX = api.WorldManager.MapSizeX / 32 / 2;
		int chunkMidZ = api.WorldManager.MapSizeZ / 32 / 2;
		IServerPlayer player = caller.Player as IServerPlayer;
		if (aroundPlayer)
		{
			chunkMidX = (int)player.Entity.Pos.X / 32;
			chunkMidZ = (int)player.Entity.Pos.Z / 32;
		}
		List<Vec2i> coords = new List<Vec2i>();
		HashSet<Vec2i> hashSet = new HashSet<Vec2i>();
		int num = api.WorldManager.RegionSize / 32;
		for (int i = -rad; i <= rad; i++)
		{
			for (int j = -rad; j <= rad; j++)
			{
				coords.Add(new Vec2i(chunkMidX + i, chunkMidZ + j));
				hashSet.Add(new Vec2i((chunkMidX + i) / num, (chunkMidZ + j) / num));
			}
		}
		GenStoryStructures modSys = api.ModLoader.GetModSystem<GenStoryStructures>();
		TreeAttribute treeAttribute = null;
		if (deleteRegion && !onlydelete)
		{
			Dictionary<long, List<GeneratedStructure>> dictionary = new Dictionary<long, List<GeneratedStructure>>();
			int chunkSize = 32;
			foreach (Vec2i coord in coords)
			{
				long num2 = api.WorldManager.MapRegionIndex2D(coord.X / num, coord.Y / num);
				IMapRegion mapRegion = api.WorldManager.GetMapRegion(num2);
				if (mapRegion == null || mapRegion.GeneratedStructures.Count <= 0)
				{
					continue;
				}
				dictionary.TryAdd(num2, mapRegion.GeneratedStructures);
				List<GeneratedStructure> structures = mapRegion.GeneratedStructures.Where((GeneratedStructure s) => coord.X == s.Location.X1 / chunkSize && coord.Y == s.Location.Z1 / chunkSize).ToList();
				foreach (GeneratedStructure item in structures)
				{
					StoryStructureLocation storyStructureAt = modSys.GetStoryStructureAt(item.Location.X1, item.Location.Z1);
					if (storyStructureAt != null && modSys.storyStructureInstances.TryGetValue(storyStructureAt.Code, out var value) && item.Group != null)
					{
						Dictionary<string, int> schematicsSpawned = value.SchematicsSpawned;
						if (schematicsSpawned != null && schematicsSpawned.TryGetValue(item.Group, out var value2))
						{
							value.SchematicsSpawned[item.Group] = Math.Max(0, value2 - 1);
						}
					}
				}
				dictionary[num2].RemoveAll((GeneratedStructure s) => structures.Contains(s));
			}
			treeAttribute = new TreeAttribute();
			treeAttribute.SetBytes("GeneratedStructures", SerializerUtil.Serialize(dictionary));
		}
		foreach (Vec2i item2 in coords)
		{
			api.WorldManager.DeleteChunkColumn(item2.X, item2.Y);
		}
		if (deleteRegion)
		{
			foreach (Vec2i item3 in hashSet)
			{
				api.WorldManager.DeleteMapRegion(item3.X, item3.Y);
			}
		}
		if (!onlydelete)
		{
			if (landforms != null)
			{
				if (treeAttribute == null)
				{
					treeAttribute = new TreeAttribute();
				}
				LandformVariant[] landFormsByIndex = NoiseLandforms.landforms.LandFormsByIndex;
				int num3 = -1;
				for (int num4 = 0; num4 < landFormsByIndex.Length; num4++)
				{
					if (landFormsByIndex[num4].Code.Path.Equals(landforms))
					{
						num3 = num4;
						break;
					}
				}
				if (num3 < 0)
				{
					return TextCommandResult.Success("No such landform exists");
				}
				treeAttribute.SetInt("forceLandform", num3);
			}
			int leftToLoad = coords.Count;
			bool sent = false;
			api.WorldManager.SendChunks = false;
			foreach (Vec2i item4 in coords)
			{
				api.WorldManager.LoadChunkColumnPriority(item4.X, item4.Y, new ChunkLoadOptions
				{
					OnLoaded = delegate
					{
						leftToLoad--;
						if (leftToLoad <= 0 && !sent)
						{
							modSys.FinalizeRegeneration(chunkMidX, chunkMidZ);
							sent = true;
							player.SendMessage(caller.FromChatGroupId, "Regen complete", EnumChatType.CommandSuccess);
							player.CurrentChunkSentRadius = 0;
							api.WorldManager.SendChunks = true;
							foreach (Vec2i item5 in coords)
							{
								for (int k = 0; k < api.WorldManager.MapSizeY / 32; k++)
								{
									api.WorldManager.BroadcastChunk(item5.X, k, item5.Y);
								}
							}
						}
					},
					ChunkGenParams = treeAttribute
				});
			}
		}
		else if (!deleteRegion)
		{
			foreach (Vec2i coord2 in coords)
			{
				long index2d = api.WorldManager.MapRegionIndex2D(coord2.X / num, coord2.Y / num);
				IMapRegion mapRegion2 = api.WorldManager.GetMapRegion(index2d);
				if (mapRegion2 == null || mapRegion2.GeneratedStructures.Count <= 0)
				{
					continue;
				}
				List<GeneratedStructure> generatedStructures = mapRegion2.GeneratedStructures;
				List<GeneratedStructure> structures2 = generatedStructures.Where((GeneratedStructure s) => coord2.X == s.Location.X1 / 32 && coord2.Y == s.Location.Z1 / 32).ToList();
				foreach (GeneratedStructure item6 in structures2)
				{
					StoryStructureLocation storyStructureAt2 = modSys.GetStoryStructureAt(item6.Location.X1, item6.Location.Z1);
					if (storyStructureAt2 != null && modSys.storyStructureInstances.TryGetValue(storyStructureAt2.Code, out var value3) && item6.Group != null)
					{
						Dictionary<string, int> schematicsSpawned2 = value3.SchematicsSpawned;
						if (schematicsSpawned2 != null && schematicsSpawned2.TryGetValue(item6.Group, out var value4))
						{
							value3.SchematicsSpawned[item6.Group] = Math.Max(0, value4 - 1);
						}
					}
				}
				generatedStructures.RemoveAll((GeneratedStructure s) => structures2.Contains(s));
			}
		}
		int num5 = 2 * rad + 1;
		if (onlydelete)
		{
			return TextCommandResult.Success("Deleted " + num5 + "x" + num5 + " columns" + (deleteRegion ? " and regions" : ""));
		}
		return TextCommandResult.Success("Reloaded landforms and regenerating " + num5 + "x" + num5 + " columns" + (deleteRegion ? " and regions" : ""));
	}

	private TextCommandResult TestTree(IServerPlayer player, string asset, float size, float aheadoffset)
	{
		AssetLocation assetLocation = new AssetLocation(asset);
		BlockPos asBlockPos = player.Entity.Pos.HorizontalAheadCopy(aheadoffset).AsBlockPos;
		IBlockAccessor blockAccessorBulkUpdate = api.World.GetBlockAccessorBulkUpdate(synchronize: true, relight: true);
		while (blockAccessorBulkUpdate.GetBlockId(asBlockPos) == 0 && asBlockPos.Y > 1)
		{
			asBlockPos.Down();
		}
		treeGenerators.ReloadTreeGenerators();
		if (treeGenerators.GetGenerator(assetLocation) == null)
		{
			return TextCommandResult.Success("Cannot generate this tree, no such generator found");
		}
		treeGenerators.RunGenerator(assetLocation, blockAccessorBulkUpdate, asBlockPos, new TreeGenParams
		{
			size = size,
			skipForestFloor = true
		});
		blockAccessorBulkUpdate.Commit();
		return TextCommandResult.Success(string.Concat(assetLocation, " size ", size.ToString(), " generated."));
	}

	private TextCommandResult TreeLineup(IServerPlayer player, string asset)
	{
		BlockPos asBlockPos = player.Entity.Pos.HorizontalAheadCopy(25.0).AsBlockPos;
		IBlockAccessor blockAccessorBulkUpdate = api.World.GetBlockAccessorBulkUpdate(synchronize: true, relight: true, debug: true);
		AssetLocation treeName = new AssetLocation(asset);
		int num = 12;
		for (int i = -2 * num; i < 2 * num; i++)
		{
			for (int j = -num; j < num; j++)
			{
				for (int k = 0; k < 2 * num; k++)
				{
					blockAccessorBulkUpdate.SetBlock(0, asBlockPos.AddCopy(i, k, j));
				}
			}
		}
		TreeGenParams treeGenParams = new TreeGenParams
		{
			size = 1f
		};
		treeGenerators.ReloadTreeGenerators();
		treeGenerators.RunGenerator(treeName, blockAccessorBulkUpdate, asBlockPos.AddCopy(0, -1, 0), treeGenParams);
		treeGenerators.RunGenerator(treeName, blockAccessorBulkUpdate, asBlockPos.AddCopy(-9, -1, 0), treeGenParams);
		treeGenerators.RunGenerator(treeName, blockAccessorBulkUpdate, asBlockPos.AddCopy(9, -1, 0), treeGenParams);
		blockAccessorBulkUpdate.Commit();
		return TextCommandResult.Success();
	}

	private IntDataMap2D OnMapRegionGen(int regionX, int regionZ, MapLayerBase climateGen)
	{
		int num = 2;
		int num2 = api.WorldManager.RegionSize / TerraGenConfig.climateMapScale;
		IntDataMap2D obj = new IntDataMap2D
		{
			Data = climateGen.GenLayer(regionX * num2 - num, regionZ * num2 - num, num2 + 2 * num, num2 + 2 * num),
			Size = num2 + 2 * num
		};
		obj.TopLeftPadding = (obj.BottomRightPadding = num);
		return obj;
	}

	private void DrawMapRegion(DebugDrawMode mode, Caller caller, IntDataMap2D map, string prefix, bool lerp, int regionX, int regionZ, int scale)
	{
		IServerPlayer serverPlayer = caller.Player as IServerPlayer;
		if (lerp)
		{
			int[] values = GameMath.BiLerpColorMap(map, scale);
			NoiseBase.DebugDrawBitmap(mode, values, map.InnerSize * scale, prefix + "-" + regionX + "-" + regionZ + "-l");
			serverPlayer.SendMessage(caller.FromChatGroupId, "Lerped " + prefix + " map generated.", EnumChatType.CommandSuccess);
		}
		else
		{
			NoiseBase.DebugDrawBitmap(mode, map.Data, map.Size, prefix + "-" + regionX + "-" + regionZ);
			serverPlayer.SendMessage(caller.FromChatGroupId, "Original " + prefix + " map generated.", EnumChatType.CommandSuccess);
		}
	}

	private TextCommandResult OnStructuresList(TextCommandCallingArgs args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (args.Parsers[0].IsMissing)
		{
			for (int i = 0; i < _scfg.Structures.Length; i++)
			{
				WorldGenStructure worldGenStructure = _scfg.Structures[i];
				string value = worldGenStructure.Schematics.FirstOrDefault()?.Domain;
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(28, 5, stringBuilder2);
				handler.AppendFormatted(i);
				handler.AppendLiteral(": Name: ");
				handler.AppendFormatted(worldGenStructure.Name);
				handler.AppendLiteral(" - Code: ");
				handler.AppendFormatted(value);
				handler.AppendLiteral(":");
				handler.AppendFormatted(worldGenStructure.Code);
				handler.AppendLiteral(" - Group: ");
				handler.AppendFormatted(worldGenStructure.Group);
				stringBuilder3.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(28, 2, stringBuilder2);
				handler.AppendLiteral("     YOff: ");
				handler.AppendFormatted(worldGenStructure.OffsetY);
				handler.AppendLiteral(" - MinGroupDist: ");
				handler.AppendFormatted(worldGenStructure.MinGroupDistance);
				stringBuilder4.AppendLine(ref handler);
			}
			return TextCommandResult.Success(stringBuilder.ToString());
		}
		int num = (int)args[0];
		if (num < 0 || num >= _scfg.Structures.Length)
		{
			return TextCommandResult.Success($"structureNum is out of range: 0-{_scfg.Structures.Length - 1}");
		}
		WorldGenStructure worldGenStructure2 = _scfg.Structures[num];
		for (int j = 0; j < worldGenStructure2.schematicDatas.Length; j++)
		{
			BlockSchematicStructure[] array = worldGenStructure2.schematicDatas[j];
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 2, stringBuilder2);
			handler.AppendFormatted(j);
			handler.AppendLiteral(": File: ");
			handler.AppendFormatted(array[0].FromFileName);
			stringBuilder5.AppendLine(ref handler);
		}
		return TextCommandResult.Success(stringBuilder.ToString());
	}

	private TextCommandResult OnStructuresSpawn(TextCommandCallingArgs args)
	{
		int num = (int)args[0];
		int num2 = (int)args[1];
		int num3 = (int)args[2];
		if (num < 0 || num >= _scfg.Structures.Length)
		{
			return TextCommandResult.Success($"structureNum is out of range: 0-{_scfg.Structures.Length - 1}");
		}
		WorldGenStructure worldGenStructure = _scfg.Structures[num];
		if (num2 < 0 || num2 >= worldGenStructure.schematicDatas.Length)
		{
			return TextCommandResult.Success($"schematicNum is out of range: 0-{worldGenStructure.schematicDatas.Length - 1}");
		}
		BlockPos blockPos = args.Caller.Player.CurrentBlockSelection?.Position.AddCopy(0, worldGenStructure.OffsetY.GetValueOrDefault(), 0) ?? args.Caller.Pos.AsBlockPos.AddCopy(0, worldGenStructure.OffsetY.GetValueOrDefault(), 0);
		BlockSchematicStructure blockSchematicStructure = worldGenStructure.schematicDatas[num2][num3];
		blockSchematicStructure.Unpack(api, num3);
		int num4 = blockPos.X / _chunksize;
		int num5 = blockPos.Z / _chunksize;
		int chunkY = blockPos.Y / _chunksize;
		switch (worldGenStructure.Placement)
		{
		case EnumStructurePlacement.SurfaceRuin:
		case EnumStructurePlacement.Surface:
		{
			IntDataMap2D climateMap = api.WorldManager.GetChunk(num4, chunkY, num5).MapChunk.MapRegion.ClimateMap;
			int num6 = num4 % _regionChunkSize;
			int num7 = num5 % _regionChunkSize;
			float num8 = (float)climateMap.InnerSize / (float)_regionChunkSize;
			int unpaddedInt = climateMap.GetUnpaddedInt((int)((float)num6 * num8), (int)((float)num7 * num8));
			int unpaddedInt2 = climateMap.GetUnpaddedInt((int)((float)num6 * num8 + num8), (int)((float)num7 * num8));
			int unpaddedInt3 = climateMap.GetUnpaddedInt((int)((float)num6 * num8), (int)((float)num7 * num8 + num8));
			int unpaddedInt4 = climateMap.GetUnpaddedInt((int)((float)num6 * num8 + num8), (int)((float)num7 * num8 + num8));
			blockSchematicStructure.PlaceRespectingBlockLayers(api.World.BlockAccessor, api.World, blockPos, unpaddedInt, unpaddedInt2, unpaddedInt3, unpaddedInt4, worldGenStructure.resolvedRockTypeRemaps, worldGenStructure.replacewithblocklayersBlockids, GenStructures.ReplaceMetaBlocks);
			break;
		}
		case EnumStructurePlacement.Underground:
			if (worldGenStructure.resolvedRockTypeRemaps != null)
			{
				blockSchematicStructure.PlaceReplacingBlocks(api.World.BlockAccessor, api.World, blockPos, blockSchematicStructure.ReplaceMode, worldGenStructure.resolvedRockTypeRemaps, null, GenStructures.ReplaceMetaBlocks);
			}
			else
			{
				blockSchematicStructure.Place(api.World.BlockAccessor, api.World, blockPos, GenStructures.ReplaceMetaBlocks);
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case EnumStructurePlacement.Underwater:
			break;
		}
		return TextCommandResult.Success($"placing structure: {worldGenStructure.Name} :: {blockSchematicStructure.FromFileName} placement: {worldGenStructure.Placement}");
	}
}
