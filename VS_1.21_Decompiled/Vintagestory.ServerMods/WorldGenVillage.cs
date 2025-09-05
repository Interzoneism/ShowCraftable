using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class WorldGenVillage
{
	[JsonProperty]
	public string Code;

	[JsonProperty]
	public string Name;

	[JsonProperty]
	public string Group;

	[JsonProperty]
	public int MinGroupDistance;

	[JsonProperty]
	public VillageSchematic[] Schematics;

	[JsonProperty]
	public float Chance = 0.05f;

	[JsonProperty]
	public NatFloat QuantityStructures = NatFloat.createGauss(7f, 7f);

	[JsonProperty]
	public AssetLocation[] ReplaceWithBlocklayers;

	[JsonProperty]
	public bool BuildProtected;

	[JsonProperty]
	public bool PostPass;

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
	public Dictionary<AssetLocation, AssetLocation> RockTypeRemaps;

	[JsonProperty]
	public string RockTypeRemapGroup;

	[JsonProperty]
	public int MaxYDiff = 3;

	internal int[] replaceblockids = Array.Empty<int>();

	internal Dictionary<int, Dictionary<int, int>> resolvedRockTypeRemaps;

	private LCGRandom rand;

	public void Init(ICoreServerAPI api, BlockLayerConfig blockLayerConfig, WorldGenStructuresConfig structureConfig, Dictionary<string, Dictionary<int, Dictionary<int, int>>> resolvedRocktypeRemapGroups, Dictionary<string, int> schematicYOffsets, int? defaultOffsetY, RockStrataConfig rockstrata, LCGRandom rand)
	{
		this.rand = rand;
		for (int i = 0; i < Schematics.Length; i++)
		{
			List<BlockSchematicStructure> list = new List<BlockSchematicStructure>();
			VillageSchematic villageSchematic = Schematics[i];
			IAsset[] array = ((!villageSchematic.Path.EndsWith('*')) ? new IAsset[1] { api.Assets.Get("worldgen/schematics/" + Schematics[i].Path + ".json") } : api.Assets.GetManyInCategory("worldgen", "schematics/" + villageSchematic.Path.Substring(0, villageSchematic.Path.Length - 1)).ToArray());
			for (int j = 0; j < array.Length; j++)
			{
				int offsetY = WorldGenStructureBase.getOffsetY(schematicYOffsets, defaultOffsetY, array[j]);
				BlockSchematicStructure[] array2 = WorldGenStructureBase.LoadSchematic<BlockSchematicStructure>(api, array[j], blockLayerConfig, structureConfig, null, offsetY);
				if (array2 != null)
				{
					list.AddRange(array2);
				}
			}
			villageSchematic.Structures = list.ToArray();
			if (villageSchematic.Structures.Length == 0)
			{
				throw new Exception($"villages.json, village with code {Code} has a schematic definition at index {i} that resolves into zero schematics. Please fix or remove this entry");
			}
		}
		if (ReplaceWithBlocklayers != null)
		{
			replaceblockids = new int[ReplaceWithBlocklayers.Length];
			for (int k = 0; k < replaceblockids.Length; k++)
			{
				Block block = api.World.GetBlock(ReplaceWithBlocklayers[k]);
				if (block == null)
				{
					throw new Exception($"Schematic with code {Code} has replace block layer {ReplaceWithBlocklayers[k]} defined, but no such block found!");
				}
				replaceblockids[k] = (ushort)block.Id;
			}
		}
		if (RockTypeRemapGroup != null)
		{
			resolvedRockTypeRemaps = resolvedRocktypeRemapGroups[RockTypeRemapGroup];
		}
		if (RockTypeRemaps != null)
		{
			resolvedRockTypeRemaps = WorldGenStructuresConfigBase.ResolveRockTypeRemaps(RockTypeRemaps, rockstrata, api);
		}
	}

	public bool TryGenerate(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos pos, int climateUpLeft, int climateUpRight, int climateBotLeft, int climateBotRight, DidGenerate didGenerateStructure, BlockPos spawnPos)
	{
		if (!WorldGenStructure.SatisfiesMinDistance(pos, worldForCollectibleResolve, MinGroupDistance, Group))
		{
			return false;
		}
		rand.InitPositionSeed(pos.X, pos.Z);
		float num = QuantityStructures.nextFloat(1f, rand);
		int num2 = (int)num;
		BlockPos blockPos = pos.Copy();
		Cuboidi cuboidi = new Cuboidi();
		List<GeneratableStructure> list = new List<GeneratableStructure>();
		List<VillageSchematic> list2 = new List<VillageSchematic>();
		List<VillageSchematic> list3 = new List<VillageSchematic>();
		for (int i = 0; i < Schematics.Length; i++)
		{
			VillageSchematic villageSchematic = Schematics[i];
			villageSchematic.NowQuantity = 0;
			if (villageSchematic.MinQuantity > 0)
			{
				for (int j = 0; j < villageSchematic.MinQuantity; j++)
				{
					list2.Add(villageSchematic);
				}
			}
			if (villageSchematic.MaxQuantity > villageSchematic.MinQuantity)
			{
				list3.Add(villageSchematic);
			}
		}
		while (num-- > 0f && (!(num < 1f) || !(rand.NextFloat() > num)))
		{
			int num3 = 30;
			int num4 = 0;
			double totalWeight = getTotalWeight(list3);
			while (num3-- > 0)
			{
				int num5 = Math.Min(16 + num4++ / 2, 24);
				blockPos.Set(pos);
				blockPos.Add(rand.NextInt(2 * num5) - num5, 0, rand.NextInt(2 * num5) - num5);
				blockPos.Y = blockAccessor.GetTerrainMapheightAt(blockPos);
				if (blockPos.Y == 0)
				{
					continue;
				}
				VillageSchematic villageSchematic2 = null;
				bool flag = list2.Count > 0;
				if (flag)
				{
					villageSchematic2 = list2[list2.Count - 1];
				}
				else
				{
					double num6 = rand.NextDouble() * totalWeight;
					int num7 = 0;
					while (num6 > 0.0)
					{
						villageSchematic2 = list3[num7++];
						if (villageSchematic2.ShouldGenerate)
						{
							num6 -= villageSchematic2.Weight;
						}
					}
				}
				if (!BlockSchematicStructure.SatisfiesMinSpawnDistance(villageSchematic2.MinSpawnDistance, pos, spawnPos))
				{
					if (flag)
					{
						break;
					}
					continue;
				}
				int num8 = rand.NextInt(villageSchematic2.Structures.Length);
				BlockSchematicStructure blockSchematicStructure = villageSchematic2.Structures[num8];
				cuboidi.Set(blockPos.X - blockSchematicStructure.SizeX / 2, blockPos.Y, blockPos.Z - blockSchematicStructure.SizeZ / 2, blockPos.X + (int)Math.Ceiling((float)blockSchematicStructure.SizeX / 2f), blockPos.Y + blockSchematicStructure.SizeY, blockPos.Z + (int)Math.Ceiling((float)blockSchematicStructure.SizeZ / 2f));
				bool flag2 = false;
				for (int k = 0; k < list.Count; k++)
				{
					if (cuboidi.IntersectsOrTouches(list[k].Location))
					{
						flag2 = true;
						break;
					}
				}
				if (flag2)
				{
					continue;
				}
				blockSchematicStructure.Unpack(worldForCollectibleResolve.Api, num8 % 4);
				if (CanGenerateStructureAt(blockSchematicStructure, blockAccessor, cuboidi))
				{
					if (flag)
					{
						list2.RemoveAt(list2.Count - 1);
					}
					villageSchematic2.NowQuantity++;
					list.Add(new GeneratableStructure
					{
						Structure = blockSchematicStructure,
						StartPos = cuboidi.Start.AsBlockPos,
						Location = cuboidi.Clone()
					});
					num3 = 0;
				}
			}
		}
		if (list.Count >= num2 && list2.Count == 0)
		{
			foreach (GeneratableStructure item in list)
			{
				item.Structure.PlaceRespectingBlockLayers(blockAccessor, worldForCollectibleResolve, item.StartPos, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, resolvedRockTypeRemaps, replaceblockids, GenStructures.ReplaceMetaBlocks);
				didGenerateStructure(item.Location, item.Structure);
			}
			return true;
		}
		return false;
	}

	private double getTotalWeight(List<VillageSchematic> canGenerate)
	{
		double num = 0.0;
		for (int i = 0; i < canGenerate.Count; i++)
		{
			VillageSchematic villageSchematic = canGenerate[i];
			if (villageSchematic.ShouldGenerate)
			{
				num += villageSchematic.Weight;
			}
		}
		return num;
	}

	protected bool CanGenerateStructureAt(BlockSchematicStructure schematic, IBlockAccessor ba, Cuboidi location)
	{
		BlockPos blockPos = new BlockPos(location.CenterX, location.Y1 + schematic.OffsetY, location.CenterZ);
		BlockPos blockPos2 = new BlockPos();
		int terrainMapheightAt = ba.GetTerrainMapheightAt(blockPos2.Set(location.X1, 0, location.Z1));
		int terrainMapheightAt2 = ba.GetTerrainMapheightAt(blockPos2.Set(location.X2, 0, location.Z1));
		int terrainMapheightAt3 = ba.GetTerrainMapheightAt(blockPos2.Set(location.X1, 0, location.Z2));
		int terrainMapheightAt4 = ba.GetTerrainMapheightAt(blockPos2.Set(location.X2, 0, location.Z2));
		int y = location.Y1;
		int num = GameMath.Max(y, terrainMapheightAt, terrainMapheightAt2, terrainMapheightAt3, terrainMapheightAt4);
		int num2 = GameMath.Min(y, terrainMapheightAt, terrainMapheightAt2, terrainMapheightAt3, terrainMapheightAt4);
		if (num - num2 > 2)
		{
			return false;
		}
		location.Y1 = num2 + schematic.OffsetY + 1;
		location.Y2 = location.Y1 + schematic.SizeY;
		if (!testUndergroundCheckPositions(ba, location.Start.AsBlockPos, schematic.UndergroundCheckPositions))
		{
			return false;
		}
		blockPos2.Set(location.X1, blockPos.Y - 1, location.Z1);
		if (ba.GetBlock(blockPos2, 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(blockPos2.Up(), 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(blockPos2.Up(), 2).IsLiquid())
		{
			return false;
		}
		blockPos2.Set(location.X2, blockPos.Y - 1, location.Z1);
		if (ba.GetBlock(blockPos2, 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(blockPos2.Up(), 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(blockPos2.Up(), 2).IsLiquid())
		{
			return false;
		}
		blockPos2.Set(location.X1, blockPos.Y - 1, location.Z2);
		if (ba.GetBlock(blockPos2, 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(blockPos2.Up(), 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(blockPos2.Up(), 2).IsLiquid())
		{
			return false;
		}
		blockPos2.Set(location.X2, blockPos.Y - 1, location.Z2);
		if (ba.GetBlock(blockPos2, 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(blockPos2.Up(), 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(blockPos2.Up(), 2).IsLiquid())
		{
			return false;
		}
		if (overlapsExistingStructure(ba, location))
		{
			return false;
		}
		return true;
	}

	protected bool testUndergroundCheckPositions(IBlockAccessor blockAccessor, BlockPos pos, BlockPos[] testPositionsDelta)
	{
		int x = pos.X;
		int y = pos.Y;
		int z = pos.Z;
		foreach (BlockPos blockPos in testPositionsDelta)
		{
			pos.Set(x + blockPos.X, y + blockPos.Y, z + blockPos.Z);
			EnumBlockMaterial blockMaterial = blockAccessor.GetBlock(pos, 1).BlockMaterial;
			if (blockMaterial != EnumBlockMaterial.Stone && blockMaterial != EnumBlockMaterial.Soil)
			{
				return false;
			}
		}
		return true;
	}

	protected bool overlapsExistingStructure(IBlockAccessor ba, Cuboidi cuboid)
	{
		int regionSize = ba.RegionSize;
		IMapRegion mapRegion = ba.GetMapRegion(cuboid.CenterX / regionSize, cuboid.CenterZ / regionSize);
		if (mapRegion == null)
		{
			return false;
		}
		foreach (GeneratedStructure generatedStructure in mapRegion.GeneratedStructures)
		{
			if (generatedStructure.Location.Intersects(cuboid))
			{
				return true;
			}
		}
		return false;
	}
}
