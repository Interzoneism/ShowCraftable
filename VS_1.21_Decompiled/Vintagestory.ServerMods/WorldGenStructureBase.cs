using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common.Collectible.Block;

namespace Vintagestory.ServerMods;

public abstract class WorldGenStructureBase
{
	[JsonProperty]
	public string Code;

	[JsonProperty]
	public string Name;

	[JsonProperty]
	public AssetLocation[] Schematics;

	[JsonProperty]
	public EnumStructurePlacement Placement;

	[JsonProperty]
	public NatFloat Depth;

	[JsonProperty]
	public bool BuildProtected;

	[JsonProperty]
	public string BuildProtectionDesc;

	[JsonProperty]
	public string BuildProtectionName;

	[JsonProperty]
	public bool AllowUseEveryone = true;

	[JsonProperty]
	public bool AllowTraverseEveryone = true;

	[JsonProperty]
	public int ProtectionLevel = 10;

	[JsonProperty]
	public string RockTypeRemapGroup;

	[JsonProperty]
	public Dictionary<AssetLocation, AssetLocation> RockTypeRemaps;

	[JsonProperty]
	public AssetLocation[] InsideBlockCodes;

	[JsonProperty]
	public EnumOrigin Origin;

	[JsonProperty]
	public int? OffsetY;

	[JsonProperty]
	public int MaxYDiff = 3;

	[JsonProperty]
	public int? StoryLocationMaxAmount;

	[JsonProperty]
	public int MinSpawnDistance;

	[JsonProperty]
	public int MaxBelowSealevel = 20;

	public const uint PosBitMask = 1023u;

	protected T[][] LoadSchematicsWithRotations<T>(ICoreAPI api, WorldGenStructureBase struc, BlockLayerConfig config, WorldGenStructuresConfig structureConfig, Dictionary<string, int> schematicYOffsets, string pathPrefix = "schematics/", bool isDungeon = false) where T : BlockSchematicStructure
	{
		List<T[]> list = new List<T[]>();
		for (int i = 0; i < struc.Schematics.Length; i++)
		{
			AssetLocation assetLocation = Schematics[i];
			IAsset[] array = ((!struc.Schematics[i].Path.EndsWith('*')) ? new IAsset[1] { api.Assets.Get(assetLocation.Clone().WithPathPrefixOnce("worldgen/" + pathPrefix).WithPathAppendixOnce(".json")) } : api.Assets.GetManyInCategory("worldgen", pathPrefix + assetLocation.Path.Substring(0, assetLocation.Path.Length - 1), assetLocation.Domain).ToArray());
			foreach (IAsset asset in array)
			{
				int offsetY = getOffsetY(schematicYOffsets, struc.OffsetY, asset);
				T[] array2 = LoadSchematic<T>(api, asset, config, structureConfig, struc, offsetY, isDungeon);
				if (array2 != null)
				{
					list.Add(array2);
				}
			}
		}
		return list.ToArray();
	}

	public static int getOffsetY(Dictionary<string, int> schematicYOffsets, int? defaultOffsetY, IAsset asset)
	{
		string key = asset.Location.PathOmittingPrefixAndSuffix("worldgen/schematics/", ".json");
		int value = 0;
		if ((schematicYOffsets == null || !schematicYOffsets.TryGetValue(key, out value)) && defaultOffsetY.HasValue)
		{
			value = defaultOffsetY.Value;
		}
		return value;
	}

	public static T[] LoadSchematic<T>(ICoreAPI api, IAsset asset, BlockLayerConfig config, WorldGenStructuresConfig structureConfig, WorldGenStructureBase struc, int offsety, bool isDungeon = false) where T : BlockSchematicStructure
	{
		string key = asset.Location.ToShortString() + "~" + offsety;
		if (structureConfig != null && structureConfig.LoadedSchematicsCache.TryGetValue(key, out var value) && value is T[] result)
		{
			return result;
		}
		T val = asset.ToObject<T>();
		val.Remap();
		if (isDungeon)
		{
			InitDungeonData(api, val);
		}
		if (val == null)
		{
			api.World.Logger.Warning("Could not load schematic {0}", asset.Location);
			if (structureConfig != null)
			{
				structureConfig.LoadedSchematicsCache[key] = null;
			}
			return null;
		}
		val.OffsetY = offsety;
		val.FromFileName = ((asset.Location.Domain == "game") ? asset.Name : (asset.Location.Domain + ":" + asset.Name));
		val.MaxYDiff = struc?.MaxYDiff ?? 3;
		val.MaxBelowSealevel = struc?.MaxBelowSealevel ?? 3;
		val.StoryLocationMaxAmount = struc?.StoryLocationMaxAmount;
		T[] array = new T[4] { val, null, null, null };
		for (int i = 0; i < 4; i++)
		{
			if (i > 0)
			{
				T val2 = array[0];
				array[i] = val2.ClonePacked() as T;
				if (isDungeon)
				{
					List<BlockPosFacing> list = (array[i].PathwayBlocksUnpacked = new List<BlockPosFacing>());
					List<BlockPosFacing> pathwayBlocksUnpacked = val2.PathwayBlocksUnpacked;
					for (int j = 0; j < pathwayBlocksUnpacked.Count; j++)
					{
						BlockPosFacing blockPosFacing = pathwayBlocksUnpacked[j];
						BlockPos rotatedPos = val2.GetRotatedPos(EnumOrigin.BottomCenter, i * 90, blockPosFacing.Position.X, blockPosFacing.Position.Y, blockPosFacing.Position.Z);
						list.Add(new BlockPosFacing(rotatedPos, blockPosFacing.Facing.GetHorizontalRotated(i * 90), blockPosFacing.Constraints));
					}
				}
			}
			array[i].blockLayerConfig = config;
		}
		if (structureConfig != null)
		{
			Dictionary<string, BlockSchematicStructure[]> loadedSchematicsCache = structureConfig.LoadedSchematicsCache;
			BlockSchematicStructure[] value2 = array;
			loadedSchematicsCache[key] = value2;
		}
		return array;
	}

	private static void InitDungeonData(ICoreAPI api, BlockSchematicStructure schematic)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		int key = schematic.BlockCodes.First((KeyValuePair<int, AssetLocation> s) => s.Value.Path.Equals("meta-connector")).Key;
		schematic.PathwayBlocksUnpacked = new List<BlockPosFacing>();
		List<int> list = new List<int>();
		for (int num = 0; num < schematic.Indices.Count; num++)
		{
			uint num2 = schematic.Indices[num];
			int num3 = (int)(num2 & 0x3FF);
			int y = (int)((num2 >> 20) & 0x3FF);
			int num4 = (int)((num2 >> 10) & 0x3FF);
			if (num3 == 0 && schematic.BlockIds[num] == key)
			{
				flag = true;
				string constraints = ExtractDungeonPathConstraint(schematic, num2);
				schematic.PathwayBlocksUnpacked.Add(new BlockPosFacing(new BlockPos(num3, y, num4), BlockFacing.WEST, constraints));
				list.Add(num);
			}
			if (num4 == 0 && num3 != 0 && schematic.BlockIds[num] == key)
			{
				flag2 = true;
				string constraints2 = ExtractDungeonPathConstraint(schematic, num2);
				schematic.PathwayBlocksUnpacked.Add(new BlockPosFacing(new BlockPos(num3, y, num4), BlockFacing.NORTH, constraints2));
				list.Add(num);
			}
			if (num3 == schematic.SizeX - 1 && schematic.BlockIds[num] == key)
			{
				flag3 = true;
				string constraints3 = ExtractDungeonPathConstraint(schematic, num2);
				schematic.PathwayBlocksUnpacked.Add(new BlockPosFacing(new BlockPos(num3, y, num4), BlockFacing.EAST, constraints3));
				list.Add(num);
			}
			if (num4 == schematic.SizeZ - 1 && num3 != schematic.SizeX - 1 && schematic.BlockIds[num] == key)
			{
				flag4 = true;
				string constraints4 = ExtractDungeonPathConstraint(schematic, num2);
				schematic.PathwayBlocksUnpacked.Add(new BlockPosFacing(new BlockPos(num3, y, num4), BlockFacing.SOUTH, constraints4));
				list.Add(num);
			}
		}
		list.Reverse();
		foreach (int item in list)
		{
			schematic.Indices.RemoveAt(item);
			schematic.BlockIds.RemoveAt(item);
		}
		if (flag3)
		{
			schematic.SizeX--;
		}
		if (flag4)
		{
			schematic.SizeZ--;
		}
		if (flag)
		{
			schematic.SizeX--;
		}
		if (flag2)
		{
			schematic.SizeZ--;
		}
		for (int num5 = 0; num5 < schematic.Indices.Count; num5++)
		{
			if (flag || flag2)
			{
				uint num6 = schematic.Indices[num5];
				int num7 = (int)(num6 & 0x3FF);
				int num8 = (int)((num6 >> 20) & 0x3FF);
				int num9 = (int)((num6 >> 10) & 0x3FF);
				if (flag)
				{
					num7--;
				}
				if (flag2)
				{
					num9--;
				}
				schematic.Indices[num5] = (uint)((num8 << 20) | (num9 << 10) | num7);
			}
		}
		for (int num10 = 0; num10 < schematic.DecorIndices.Count; num10++)
		{
			if (flag || flag2)
			{
				uint num11 = schematic.DecorIndices[num10];
				int num12 = (int)(num11 & 0x3FF);
				int num13 = (int)((num11 >> 20) & 0x3FF);
				int num14 = (int)((num11 >> 10) & 0x3FF);
				if (flag)
				{
					num12--;
				}
				if (flag2)
				{
					num14--;
				}
				schematic.DecorIndices[num10] = (uint)((num13 << 20) | (num14 << 10) | num12);
			}
		}
		Dictionary<uint, string> dictionary = new Dictionary<uint, string>();
		foreach (var (num16, value) in schematic.BlockEntities)
		{
			if (flag || flag2)
			{
				int num17 = (int)(num16 & 0x3FF);
				int num18 = (int)((num16 >> 20) & 0x3FF);
				int num19 = (int)((num16 >> 10) & 0x3FF);
				if (flag)
				{
					num17--;
				}
				if (flag2)
				{
					num19--;
				}
				dictionary[(uint)((num18 << 20) | (num19 << 10) | num17)] = value;
			}
		}
		schematic.BlockEntities = dictionary;
		schematic.EntitiesUnpacked.Clear();
		foreach (string entity2 in schematic.Entities)
		{
			using MemoryStream input = new MemoryStream(Ascii85.Decode(entity2));
			BinaryReader binaryReader = new BinaryReader(input);
			string entityClass = binaryReader.ReadString();
			Entity entity = api.ClassRegistry.CreateEntity(entityClass);
			entity.FromBytes(binaryReader, isSync: false);
			if (flag)
			{
				entity.ServerPos.X--;
				entity.Pos.X--;
				entity.PositionBeforeFalling.X -= 1.0;
			}
			if (flag2)
			{
				entity.ServerPos.Z--;
				entity.Pos.Z--;
				entity.PositionBeforeFalling.Z -= 1.0;
			}
			schematic.EntitiesUnpacked.Add(entity);
		}
		schematic.Entities.Clear();
		if (schematic.EntitiesUnpacked.Count > 0)
		{
			using FastMemoryStream fastMemoryStream = new FastMemoryStream();
			foreach (Entity item2 in schematic.EntitiesUnpacked)
			{
				fastMemoryStream.Reset();
				BinaryWriter binaryWriter = new BinaryWriter(fastMemoryStream);
				binaryWriter.Write(api.ClassRegistry.GetEntityClassName(item2.GetType()));
				item2.ToBytes(binaryWriter, forClient: false);
				schematic.Entities.Add(Ascii85.Encode(fastMemoryStream.ToArray()));
			}
		}
		for (int num20 = 0; num20 < schematic.PathwayBlocksUnpacked.Count; num20++)
		{
			BlockPosFacing blockPosFacing = schematic.PathwayBlocksUnpacked[num20];
			int num21 = 0;
			int num22 = 0;
			if (flag && blockPosFacing.Position.X > 0)
			{
				num21--;
			}
			if (flag2 && blockPosFacing.Position.Z > 0)
			{
				num22--;
			}
			if (flag3 && blockPosFacing.Position.X >= schematic.SizeX)
			{
				num21--;
			}
			if (flag4 && blockPosFacing.Position.Z >= schematic.SizeZ)
			{
				num22--;
			}
			blockPosFacing.Position.X += num21;
			blockPosFacing.Position.Z += num22;
		}
	}

	private static string ExtractDungeonPathConstraint(BlockSchematicStructure schematic, uint index)
	{
		string data = schematic.BlockEntities[index];
		string value = (schematic.DecodeBlockEntityData(data)["constraints"] as StringAttribute).value;
		schematic.BlockEntities.Remove(index);
		return value;
	}

	public T[] LoadSchematics<T>(ICoreAPI api, AssetLocation[] locs, BlockLayerConfig config, string pathPrefix = "schematics/") where T : BlockSchematicStructure
	{
		List<T> list = new List<T>();
		for (int i = 0; i < locs.Length; i++)
		{
			string text = "";
			AssetLocation assetLocation = Schematics[i];
			IAsset[] array = ((!locs[i].Path.EndsWith('*')) ? new IAsset[1] { api.Assets.Get(assetLocation.Clone().WithPathPrefixOnce("worldgen/" + pathPrefix).WithPathAppendixOnce(".json")) } : api.Assets.GetManyInCategory("worldgen", pathPrefix + assetLocation.Path.Substring(0, assetLocation.Path.Length - 1), assetLocation.Domain).ToArray());
			foreach (IAsset asset in array)
			{
				T val = asset.ToObject<T>();
				if (val == null)
				{
					api.World.Logger.Warning("Could not load {0}: {1}", Schematics[i], text);
				}
				else
				{
					val.FromFileName = ((asset.Location.Domain == "game") ? asset.Name : (asset.Location.Domain + ":" + asset.Name));
					list.Add(val);
				}
			}
		}
		return list.ToArray();
	}
}
