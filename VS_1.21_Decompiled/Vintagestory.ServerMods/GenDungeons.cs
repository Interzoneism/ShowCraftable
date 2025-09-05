using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods;

public class GenDungeons : ModStdWorldGen
{
	private ModSystemTiledDungeonGenerator dungeonGen;

	private IWorldGenBlockAccessor worldgenBlockAccessor;

	private LCGRandom rand;

	private ICoreServerAPI api;

	private Dictionary<long, List<DungeonPlaceTask>> dungeonPlaceTasksByRegion = new Dictionary<long, List<DungeonPlaceTask>>();

	private int regionSize;

	private bool genDungeons;

	public override double ExecuteOrder()
	{
		return 0.12;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		base.StartServerSide(api);
		dungeonGen = api.ModLoader.GetModSystem<ModSystemTiledDungeonGenerator>();
		dungeonGen.init();
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		genDungeons = api.World.Config.GetAsString("loreContent", "true").ToBool(defaultValue: true);
		if (genDungeons)
		{
			worldgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: false);
			rand = new LCGRandom(api.WorldManager.Seed ^ 0x217F464FEL);
			regionSize = api.World.BlockAccessor.RegionSize;
		}
	}

	private void Event_MapRegionLoaded(Vec2i mapCoord, IMapRegion region)
	{
		List<DungeonPlaceTask> moddata = region.GetModdata<List<DungeonPlaceTask>>("dungeonPlaceTasks");
		if (moddata != null)
		{
			dungeonPlaceTasksByRegion[MapRegionIndex2D(mapCoord.X, mapCoord.Y)] = moddata;
		}
	}

	private void Event_MapRegionUnloaded(Vec2i mapCoord, IMapRegion region)
	{
		if (dungeonPlaceTasksByRegion.TryGetValue(MapRegionIndex2D(mapCoord.X, mapCoord.Y), out var value) && value != null)
		{
			region.SetModdata("dungeonPlaceTasks", value);
		}
	}

	private void onMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams = null)
	{
		int num = api.WorldManager.RegionSize;
		LCGRandom lCGRandom = new LCGRandom(api.WorldManager.Seed ^ 0x217F464FEL);
		lCGRandom.InitPositionSeed(regionX * num, regionZ * num);
		long key = MapRegionIndex2D(regionX, regionZ);
		dungeonPlaceTasksByRegion[key] = new List<DungeonPlaceTask>();
		for (int i = 0; i < 3; i++)
		{
			int num2 = regionX * num + lCGRandom.NextInt(num);
			int num3 = regionZ * num + lCGRandom.NextInt(num);
			int num4 = lCGRandom.NextInt(api.World.SeaLevel - 10);
			api.Logger.Event($"Dungeon @: /tp ={num2} {num4} ={num3}");
			TiledDungeon dungeon = dungeonGen.Tcfg.Dungeons[0].Copy();
			DungeonPlaceTask dungeonPlaceTask = dungeonGen.TryPregenerateTiledDungeon(lCGRandom, dungeon, new BlockPos(num2, num4, num3), 5, 50);
			if (dungeonPlaceTask != null)
			{
				dungeonPlaceTasksByRegion[key].Add(dungeonPlaceTask);
			}
		}
	}

	public long MapRegionIndex2D(int regionX, int regionZ)
	{
		return (long)regionZ * (long)(api.WorldManager.MapSizeX / api.WorldManager.RegionSize) + regionX;
	}

	private void onChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		int num = request.ChunkX * 32 / regionSize;
		int num2 = request.ChunkZ * 32 / regionSize;
		int num3 = request.ChunkX * 32;
		int num4 = request.ChunkZ * 32;
		Cuboidi cuboidi = new Cuboidi(num3, 0, num4, num3 + 32, api.World.BlockAccessor.MapSizeY, num4 + 32);
		Cuboidi cuboidi2 = new Cuboidi();
		IMapRegion mapRegion = request.Chunks[0].MapChunk.MapRegion;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (!dungeonPlaceTasksByRegion.TryGetValue(MapRegionIndex2D(num + i, num2 + j), out var value))
				{
					continue;
				}
				foreach (DungeonPlaceTask item in value)
				{
					if (!item.DungeonBoundaries.IntersectsOrTouches(cuboidi))
					{
						continue;
					}
					TilePlaceTask tilePlaceTask = item.TilePlaceTasks[0];
					cuboidi2.Set(tilePlaceTask.Pos, tilePlaceTask.Pos.AddCopy(tilePlaceTask.SizeX, tilePlaceTask.SizeY, tilePlaceTask.SizeZ));
					if (cuboidi.IntersectsOrTouches(cuboidi2))
					{
						if (!dungeonGen.Tcfg.DungeonsByCode.TryGetValue(item.Code, out var value2))
						{
							return;
						}
						int num5 = (api.World.BlockAccessor.GetTerrainMapheightAt(tilePlaceTask.Pos) - tilePlaceTask.Pos.Y) / value2.Stairs[0].SizeY;
						BlockPos blockPos = tilePlaceTask.Pos.AddCopy(tilePlaceTask.SizeX / 2 - value2.Stairs[0].SizeX / 2, tilePlaceTask.SizeY, tilePlaceTask.SizeZ / 2 - value2.Stairs[0].SizeZ / 2);
						for (int k = 0; k < num5; k++)
						{
							value2.Stairs[0].Place(worldgenBlockAccessor, api.World, blockPos);
							blockPos.Y += value2.Stairs[0].SizeY;
						}
						mapRegion.AddGeneratedStructure(new GeneratedStructure
						{
							Code = "dungeon/" + value2.Code + "/" + value2.Stairs[0].FromFileName,
							Group = item.Code,
							Location = new Cuboidi(tilePlaceTask.Pos.AddCopy(0, tilePlaceTask.SizeY, 0), blockPos.AddCopy(value2.Stairs[0].SizeX, 0, value2.Stairs[0].SizeZ)),
							SuppressTreesAndShrubs = true,
							SuppressRivulets = true
						});
					}
					generateDungeonPartial(mapRegion, item, request.Chunks, request.ChunkX, request.ChunkZ);
				}
			}
		}
	}

	private void generateDungeonPartial(IMapRegion region, DungeonPlaceTask dungeonPlaceTask, IServerChunk[] chunks, int chunkX, int chunkZ)
	{
		if (!dungeonGen.Tcfg.DungeonsByCode.TryGetValue(dungeonPlaceTask.Code, out var value))
		{
			return;
		}
		LCGRandom lCGRandom = new LCGRandom(api.WorldManager.Seed ^ 0x217F464FEL);
		int num = api.WorldManager.RegionSize;
		lCGRandom.InitPositionSeed(chunkX / num * num, chunkZ / num * num);
		foreach (TilePlaceTask tilePlaceTask in dungeonPlaceTask.TilePlaceTasks)
		{
			if (value.TilesByCode.TryGetValue(tilePlaceTask.TileCode, out var value2))
			{
				int num2 = lCGRandom.NextInt(value2.ResolvedSchematic.Length);
				BlockSchematicPartial blockSchematicPartial = value2.ResolvedSchematic[num2][tilePlaceTask.Rotation];
				blockSchematicPartial.PlacePartial(chunks, worldgenBlockAccessor, api.World, chunkX, chunkZ, tilePlaceTask.Pos, EnumReplaceMode.ReplaceAll, null, replaceMeta: true, resolveImports: true);
				string code = "dungeon/" + value2.Code + ((blockSchematicPartial == null) ? "" : ("/" + blockSchematicPartial.FromFileName));
				region.AddGeneratedStructure(new GeneratedStructure
				{
					Code = code,
					Group = dungeonPlaceTask.Code,
					Location = new Cuboidi(tilePlaceTask.Pos, tilePlaceTask.Pos.AddCopy(blockSchematicPartial.SizeX, blockSchematicPartial.SizeY, blockSchematicPartial.SizeZ)),
					SuppressTreesAndShrubs = true,
					SuppressRivulets = true
				});
			}
		}
	}
}
