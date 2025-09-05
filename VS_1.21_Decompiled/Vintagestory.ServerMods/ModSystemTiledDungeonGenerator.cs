using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common.Collectible.Block;

namespace Vintagestory.ServerMods;

public class ModSystemTiledDungeonGenerator : ModSystem
{
	protected ICoreServerAPI api;

	public TiledDungeonConfig Tcfg;

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		base.StartServerSide(api);
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("debug").BeginSub("tiledd").WithDesc("Tiled dungeon generator debugger/tester")
			.RequiresPrivilege(Privilege.controlserver)
			.WithArgs(parsers.Word("tiled dungeon code"), parsers.Int("amount of tiles"))
			.HandleWith(OnCmdTiledCungeonCode)
			.EndSub()
			.BeginSub("tileddd")
			.WithDesc("Tiled dungeon generator debugger/tester")
			.RequiresPrivilege(Privilege.controlserver)
			.WithArgs(parsers.Word("tiled dungeon code"))
			.HandleWith(OnCmdTiledCungeonTest)
			.EndSub();
	}

	private TextCommandResult OnCmdTiledCungeonTest(TextCommandCallingArgs args)
	{
		api.Assets.Reload(AssetCategory.worldgen);
		init();
		string code = (string)args[0];
		TiledDungeon tiledDungeon = Tcfg.Dungeons.FirstOrDefault((TiledDungeon td) => td.Code == code).Copy();
		if (tiledDungeon == null)
		{
			return TextCommandResult.Error("No such dungeon defined");
		}
		BlockPos asBlockPos = args.Caller.Pos.AsBlockPos;
		asBlockPos.Y = api.World.BlockAccessor.GetTerrainMapheightAt(asBlockPos) + 30;
		IBlockAccessor blockAccessor = api.World.BlockAccessor;
		int blockId = api.World.BlockAccessor.GetBlock(new AssetLocation("meta-connector")).BlockId;
		int x = asBlockPos.X;
		foreach (KeyValuePair<string, DungeonTile> item in tiledDungeon.TilesByCode)
		{
			item.Deconstruct(out var _, out var value);
			DungeonTile dungeonTile = value;
			for (int num = 0; num < 4; num++)
			{
				List<BlockPosFacing> pathwayBlocksUnpacked = dungeonTile.ResolvedSchematic[0][num].PathwayBlocksUnpacked;
				dungeonTile.ResolvedSchematic[0][num].Place(blockAccessor, api.World, asBlockPos);
				dungeonTile.ResolvedSchematic[0][num].PlaceEntitiesAndBlockEntities(blockAccessor, api.World, asBlockPos, new Dictionary<int, AssetLocation>(), new Dictionary<int, AssetLocation>());
				foreach (BlockPosFacing item2 in pathwayBlocksUnpacked)
				{
					blockAccessor.SetBlock(blockId, asBlockPos + item2.Position);
				}
				asBlockPos.X += 30;
			}
			asBlockPos.Z += 30;
			asBlockPos.X = x;
		}
		return TextCommandResult.Success("dungeon generated");
	}

	internal void init()
	{
		IAsset asset = api.Assets.Get("worldgen/tileddungeons.json");
		Tcfg = asset.ToObject<TiledDungeonConfig>();
		Tcfg.Init(api);
	}

	private TextCommandResult OnCmdTiledCungeonCode(TextCommandCallingArgs args)
	{
		api.Assets.Reload(AssetCategory.worldgen);
		init();
		string code = (string)args[0];
		int num = (int)args[1];
		TiledDungeon tiledDungeon = Tcfg.Dungeons.FirstOrDefault((TiledDungeon td) => td.Code == code).Copy();
		if (tiledDungeon == null)
		{
			return TextCommandResult.Error("No such dungeon defined");
		}
		BlockPos asBlockPos = args.Caller.Pos.AsBlockPos;
		asBlockPos.Y = api.World.BlockAccessor.GetTerrainMapheightAt(asBlockPos) + 30;
		IBlockAccessor blockAccessor = api.World.BlockAccessor;
		int regionSize = api.WorldManager.RegionSize;
		LCGRandom lCGRandom = new LCGRandom(api.WorldManager.Seed ^ 0x217F464FEL);
		lCGRandom.InitPositionSeed(asBlockPos.X / regionSize * regionSize, asBlockPos.Z / regionSize * regionSize);
		for (int num2 = 0; num2 < 50; num2++)
		{
			if (TryPlaceTiledDungeon(blockAccessor, lCGRandom, tiledDungeon, asBlockPos, num, num))
			{
				return TextCommandResult.Success("dungeon generated");
			}
		}
		return TextCommandResult.Error("Unable to generate dungeon of this size after 50 attempts");
	}

	public DungeonPlaceTask TryPregenerateTiledDungeon(IRandom rnd, TiledDungeon dungeon, BlockPos startPos, int minTiles, int maxTiles)
	{
		int rot = rnd.NextInt(4);
		Queue<BlockPosFacing> queue = new Queue<BlockPosFacing>();
		List<TilePlaceTask> list = new List<TilePlaceTask>();
		List<GeneratedStructure> list2 = new List<GeneratedStructure>();
		BlockSchematicPartial[] schematics = ((dungeon.Start != null) ? dungeon.Start : dungeon.TilesByCode["4way"].ResolvedSchematic[0]);
		string text = ((dungeon.Start != null) ? dungeon.start : "4way");
		Cuboidi location = place(schematics, text, rot, startPos, queue, list);
		list2.Add(new GeneratedStructure
		{
			Code = "dungeon-" + text,
			Location = location,
			SuppressRivulets = true
		});
		int num = minTiles * 10;
		while (num-- > 0 && queue.Count > 0)
		{
			BlockPosFacing openside = queue.Dequeue();
			dungeon.Tiles.Shuffle(rnd);
			float num2 = (float)rnd.NextDouble() * dungeon.totalChance;
			int count = dungeon.Tiles.Count;
			int num3 = 0;
			bool flag = list.Count >= maxTiles;
			if (flag)
			{
				num2 = 0f;
			}
			for (int i = 0; i < count + num3; i++)
			{
				DungeonTile tile = dungeon.Tiles[i % count];
				if (!tile.IgnoreMaxTiles && flag)
				{
					continue;
				}
				num2 -= tile.Chance;
				if (num2 > 0f)
				{
					num3++;
				}
				else
				{
					if (!tile.ResolvedSchematic[0].Any((BlockSchematicPartial s) => s.PathwayBlocksUnpacked.Any((BlockPosFacing p) => openside.Facing.Opposite == p.Facing && WildcardUtil.Match(openside.Constraints, tile.Code))))
					{
						continue;
					}
					int num4 = rnd.NextInt(4);
					rot = 0;
					BlockFacing attachingFace = openside.Facing.Opposite;
					bool flag2 = false;
					BlockPos blockPos = null;
					BlockSchematicPartial blockSchematicPartial = null;
					for (int num5 = 0; num5 < 4; num5++)
					{
						rot = (num4 + num5) % 4;
						blockSchematicPartial = tile.ResolvedSchematic[0][rot];
						if (blockSchematicPartial.PathwayBlocksUnpacked.Any((BlockPosFacing p) => p.Facing == attachingFace && WildcardUtil.Match(openside.Constraints, tile.Code)))
						{
							blockPos = blockSchematicPartial.PathwayBlocksUnpacked.First((BlockPosFacing p) => p.Facing == attachingFace && WildcardUtil.Match(openside.Constraints, tile.Code)).Position;
							flag2 = true;
							break;
						}
					}
					if (flag2)
					{
						BlockPos blockPos2 = openside.Position.Copy();
						blockPos2 = blockPos2.AddCopy(openside.Facing) - blockPos;
						Cuboidi newloc = new Cuboidi(blockPos2.X, blockPos2.Y, blockPos2.Z, blockPos2.X + blockSchematicPartial.SizeX, blockPos2.Y + blockSchematicPartial.SizeY, blockPos2.Z + blockSchematicPartial.SizeZ);
						if (!intersects(list2, newloc))
						{
							location = place(tile, rot, blockPos2, queue, list, openside.Facing.Opposite);
							list2.Add(new GeneratedStructure
							{
								Code = tile.Code,
								Location = location,
								SuppressRivulets = true
							});
							break;
						}
					}
				}
			}
		}
		if (list.Count >= minTiles)
		{
			return new DungeonPlaceTask
			{
				Code = dungeon.Code,
				TilePlaceTasks = list
			}.GenBoundaries();
		}
		return null;
	}

	public bool TryPlaceTiledDungeon(IBlockAccessor ba, IRandom rnd, TiledDungeon dungeon, BlockPos startPos, int minTiles, int maxTiles)
	{
		DungeonPlaceTask dungeonPlaceTask = TryPregenerateTiledDungeon(rnd, dungeon, startPos, minTiles, maxTiles);
		if (dungeonPlaceTask != null)
		{
			foreach (TilePlaceTask tilePlaceTask in dungeonPlaceTask.TilePlaceTasks)
			{
				if (dungeon.TilesByCode.TryGetValue(tilePlaceTask.TileCode, out var value))
				{
					int num = rnd.NextInt(value.ResolvedSchematic.Length);
					value.ResolvedSchematic[num][tilePlaceTask.Rotation].Place(ba, api.World, tilePlaceTask.Pos);
				}
			}
			return true;
		}
		return false;
	}

	protected bool intersects(List<GeneratedStructure> gennedStructures, Cuboidi newloc)
	{
		for (int i = 0; i < gennedStructures.Count; i++)
		{
			if (gennedStructures[i].Location.Intersects(newloc))
			{
				return true;
			}
		}
		return false;
	}

	protected Cuboidi place(DungeonTile tile, int rot, BlockPos startPos, Queue<BlockPosFacing> openSet, List<TilePlaceTask> placeTasks, BlockFacing attachingFace = null)
	{
		BlockSchematicPartial[] schematics = tile.ResolvedSchematic[0];
		return place(schematics, tile.Code, rot, startPos, openSet, placeTasks, attachingFace);
	}

	protected Cuboidi place(BlockSchematicPartial[] schematics, string code, int rot, BlockPos startPos, Queue<BlockPosFacing> openSet, List<TilePlaceTask> placeTasks, BlockFacing attachingFace = null)
	{
		BlockSchematicPartial blockSchematicPartial = schematics[rot];
		placeTasks.Add(new TilePlaceTask
		{
			TileCode = code,
			Rotation = rot,
			Pos = startPos.Copy(),
			SizeX = blockSchematicPartial.SizeX,
			SizeY = blockSchematicPartial.SizeY,
			SizeZ = blockSchematicPartial.SizeZ
		});
		foreach (BlockPosFacing item in blockSchematicPartial.PathwayBlocksUnpacked)
		{
			if (item.Facing != attachingFace)
			{
				openSet.Enqueue(new BlockPosFacing(item.Position + startPos, item.Facing, item.Constraints));
			}
		}
		return new Cuboidi(startPos.X, startPos.Y, startPos.Z, startPos.X + blockSchematicPartial.SizeX, startPos.Y + blockSchematicPartial.SizeY, startPos.Z + blockSchematicPartial.SizeZ);
	}

	private string[][] rotate(int rot, string[][] constraints)
	{
		return new string[6][]
		{
			constraints[(-rot + 4) % 4],
			constraints[(1 - rot + 4) % 4],
			constraints[(2 - rot + 4) % 4],
			constraints[(3 - rot + 4) % 4],
			constraints[4],
			constraints[5]
		};
	}
}
