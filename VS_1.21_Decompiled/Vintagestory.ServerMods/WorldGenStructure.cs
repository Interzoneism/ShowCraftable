using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods;

public class WorldGenStructure : WorldGenStructureBase
{
	[JsonProperty]
	public string Group;

	[JsonProperty]
	public int MinGroupDistance;

	[JsonProperty]
	public float Chance = 0.05f;

	[JsonProperty]
	public int MinTemp = -30;

	[JsonProperty]
	public int MaxTemp = 40;

	[JsonProperty]
	public float MinRain;

	[JsonProperty]
	public float MaxRain = 1f;

	[JsonProperty]
	public AssetLocation[] ReplaceWithBlocklayers;

	[JsonProperty]
	public bool PostPass;

	[JsonProperty]
	public bool SuppressTrees;

	[JsonProperty]
	public bool SuppressWaterfalls;

	[JsonProperty]
	public int StoryMaxFromCenter;

	internal BlockSchematicStructure[][] schematicDatas;

	internal int[] replacewithblocklayersBlockids = Array.Empty<int>();

	internal HashSet<int> insideblockids = new HashSet<int>();

	internal Dictionary<int, Dictionary<int, int>> resolvedRockTypeRemaps;

	private TryGenerateHandler[] Generators;

	private LCGRandom rand;

	private int unscaledMinRain;

	private int unscaledMaxRain;

	private int unscaledMinTemp;

	private int unscaledMaxTemp;

	private GenStructures genStructuresSys;

	private BlockPos tmpPos = new BlockPos();

	public Cuboidi LastPlacedSchematicLocation = new Cuboidi();

	public BlockSchematicStructure LastPlacedSchematic;

	private int climateUpLeft;

	private int climateUpRight;

	private int climateBotLeft;

	private int climateBotRight;

	private BlockPos utestPos = new BlockPos();

	private static Cuboidi tmpLoc = new Cuboidi();

	public WorldGenStructure()
	{
		Generators = new TryGenerateHandler[4] { TryGenerateRuinAtSurface, TryGenerateAtSurface, TryGenerateUnderwater, TryGenerateUnderground };
	}

	public void Init(ICoreServerAPI api, BlockLayerConfig config, RockStrataConfig rockstrata, WorldGenStructuresConfig structureConfig, LCGRandom rand)
	{
		this.rand = rand;
		genStructuresSys = api.ModLoader.GetModSystem<GenStructures>();
		unscaledMinRain = (int)(MinRain * 255f);
		unscaledMaxRain = (int)(MaxRain * 255f);
		unscaledMinTemp = Climate.DescaleTemperature(MinTemp);
		unscaledMaxTemp = Climate.DescaleTemperature(MaxTemp);
		schematicDatas = LoadSchematicsWithRotations<BlockSchematicStructure>(api, this, config, structureConfig, structureConfig.SchematicYOffsets);
		if (ReplaceWithBlocklayers != null)
		{
			replacewithblocklayersBlockids = new int[ReplaceWithBlocklayers.Length];
			for (int i = 0; i < replacewithblocklayersBlockids.Length; i++)
			{
				Block block = api.World.GetBlock(ReplaceWithBlocklayers[i]);
				if (block == null)
				{
					throw new Exception($"Schematic with code {Code} has replace block layer {ReplaceWithBlocklayers[i]} defined, but no such block found!");
				}
				replacewithblocklayersBlockids[i] = block.Id;
			}
		}
		if (InsideBlockCodes != null)
		{
			for (int j = 0; j < InsideBlockCodes.Length; j++)
			{
				Block block2 = api.World.GetBlock(InsideBlockCodes[j]);
				if (block2 == null)
				{
					throw new Exception($"Schematic with code {Code} has inside block {InsideBlockCodes[j]} defined, but no such block found!");
				}
				insideblockids.Add(block2.Id);
			}
		}
		if (RockTypeRemapGroup != null)
		{
			resolvedRockTypeRemaps = structureConfig.resolvedRocktypeRemapGroups[RockTypeRemapGroup];
		}
		if (RockTypeRemaps == null)
		{
			return;
		}
		if (resolvedRockTypeRemaps != null)
		{
			Dictionary<int, Dictionary<int, int>> dictionary = WorldGenStructuresConfigBase.ResolveRockTypeRemaps(RockTypeRemaps, rockstrata, api);
			foreach (KeyValuePair<int, Dictionary<int, int>> resolvedRockTypeRemap in resolvedRockTypeRemaps)
			{
				dictionary[resolvedRockTypeRemap.Key] = resolvedRockTypeRemap.Value;
			}
			resolvedRockTypeRemaps = dictionary;
		}
		else
		{
			resolvedRockTypeRemaps = WorldGenStructuresConfigBase.ResolveRockTypeRemaps(RockTypeRemaps, rockstrata, api);
		}
	}

	internal bool TryGenerate(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos startPos, int climateUpLeft, int climateUpRight, int climateBotLeft, int climateBotRight, string locationCode)
	{
		this.climateUpLeft = climateUpLeft;
		this.climateUpRight = climateUpRight;
		this.climateBotLeft = climateBotLeft;
		this.climateBotRight = climateBotRight;
		int num = GameMath.BiLerpRgbColor((float)(startPos.X % 32) / 32f, (float)(startPos.Z % 32) / 32f, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
		int rainFall = Climate.GetRainFall((num >> 8) & 0xFF, startPos.Y);
		int num2 = Climate.DescaleTemperature(Climate.GetScaledAdjustedTemperature((num >> 16) & 0xFF, startPos.Y - TerraGenConfig.seaLevel));
		if (rainFall < unscaledMinRain || rainFall > unscaledMaxRain || num2 < unscaledMinTemp || num2 > unscaledMaxTemp)
		{
			return false;
		}
		if (num2 < 20 && startPos.Y > worldForCollectibleResolve.SeaLevel + 15)
		{
			return false;
		}
		rand.InitPositionSeed(startPos.X, startPos.Z);
		bool flag = Generators[(int)Placement](blockAccessor, worldForCollectibleResolve, startPos, locationCode);
		if (flag && Placement == EnumStructurePlacement.SurfaceRuin)
		{
			float num3 = Math.Max(0f, (float)(rainFall - 50) / 255f);
			float num4 = Math.Max(0f, (float)(num2 - 50) / 255f);
			float num5 = 1.5f * num3 * num4 + 1f * num3 * GameMath.Clamp((num4 + 0.33f) / 1.33f, 0f, 1f);
			int num6 = (int)(10f * num5 * GameMath.Sqrt(LastPlacedSchematicLocation.SizeXYZ));
			int sizeX = LastPlacedSchematic.SizeX;
			int sizeY = LastPlacedSchematic.SizeY;
			int sizeZ = LastPlacedSchematic.SizeZ;
			BlockPos blockPos = new BlockPos(startPos.dimension);
			Block block = blockAccessor.GetBlock(new AssetLocation("attachingplant-spottymoss"));
			while (num6-- > 0)
			{
				int num7 = rand.NextInt(sizeX);
				int num8 = rand.NextInt(sizeY);
				int num9 = rand.NextInt(sizeZ);
				blockPos.Set(startPos.X + num7, startPos.Y + num8, startPos.Z + num9);
				Block block2 = blockAccessor.GetBlock(blockPos);
				if (block2.BlockMaterial != EnumBlockMaterial.Stone)
				{
					continue;
				}
				for (int i = 0; i < 6; i++)
				{
					BlockFacing blockFacing = BlockFacing.ALLFACES[i];
					if (block2.SideSolid[i] && !blockAccessor.GetBlockOnSide(blockPos, blockFacing).SideSolid[blockFacing.Opposite.Index])
					{
						blockAccessor.SetDecor(block, blockPos, blockFacing);
						break;
					}
				}
			}
		}
		return flag;
	}

	private int FindClearEntranceRotation(IBlockAccessor blockAccessor, BlockSchematicStructure[] schematics, BlockPos pos)
	{
		BlockSchematicStructure blockSchematicStructure = schematics[0];
		int num = GameMath.Clamp(schematics[0].EntranceRotation / 90, 0, 3);
		int num2 = pos.X - 2;
		int num3 = pos.X + blockSchematicStructure.SizeX + 2;
		int num4 = pos.Z - 2;
		int num5 = pos.Z + blockSchematicStructure.SizeZ + 2;
		int num6 = 1;
		int num7 = 1;
		int num8 = 1;
		int num9 = 1;
		int i = num2;
		switch (num)
		{
		case 1:
		case 3:
		{
			for (int num10 = num4; num10 <= num5; num10++)
			{
				int num13 = blockAccessor.GetMapChunk(i / 32, num10 / 32).WorldGenTerrainHeightMap[num10 % 32 * 32 + i % 32];
				num6 += num13;
			}
			i = num3;
			for (int num10 = num4; num10 <= num5; num10++)
			{
				int num14 = blockAccessor.GetMapChunk(i / 32, num10 / 32).WorldGenTerrainHeightMap[num10 % 32 * 32 + i % 32];
				num7 += num14;
			}
			break;
		}
		case 0:
		case 2:
		{
			int num10 = num4;
			for (i = num2; i <= num3; i++)
			{
				int num11 = blockAccessor.GetMapChunk(i / 32, num10 / 32).WorldGenTerrainHeightMap[num10 % 32 * 32 + i % 32];
				num8 += num11;
			}
			num10 = num5;
			for (i = num2; i <= num3; i++)
			{
				int num12 = blockAccessor.GetMapChunk(i / 32, num10 / 32).WorldGenTerrainHeightMap[num10 % 32 * 32 + i % 32];
				num9 += num12;
			}
			break;
		}
		}
		blockSchematicStructure = schematics[1];
		int num15 = GameMath.Clamp(blockSchematicStructure.EntranceRotation / 90, 0, 3);
		num2 = pos.X - 2;
		num3 = pos.X + blockSchematicStructure.SizeX + 2;
		num4 = pos.Z - 2;
		num5 = pos.Z + blockSchematicStructure.SizeZ + 2;
		int num16 = 1;
		int num17 = 1;
		int num18 = 1;
		int num19 = 1;
		switch (num15)
		{
		case 1:
		case 3:
		{
			for (int num10 = num4; num10 <= num5; num10++)
			{
				int num22 = blockAccessor.GetMapChunk(i / 32, num10 / 32).WorldGenTerrainHeightMap[num10 % 32 * 32 + i % 32];
				num16 += num22;
			}
			i = num3;
			for (int num10 = num4; num10 <= num5; num10++)
			{
				int num23 = blockAccessor.GetMapChunk(i / 32, num10 / 32).WorldGenTerrainHeightMap[num10 % 32 * 32 + i % 32];
				num17 += num23;
			}
			break;
		}
		case 0:
		case 2:
		{
			int num10 = num4;
			for (i = num2; i <= num3; i++)
			{
				int num20 = blockAccessor.GetMapChunk(i / 32, num10 / 32).WorldGenTerrainHeightMap[num10 % 32 * 32 + i % 32];
				num18 += num20;
			}
			num10 = num5;
			for (i = num2; i <= num3; i++)
			{
				int num21 = blockAccessor.GetMapChunk(i / 32, num10 / 32).WorldGenTerrainHeightMap[num10 % 32 * 32 + i % 32];
				num19 += num21;
			}
			break;
		}
		}
		int num24 = ((num == 1 || num == 3) ? ((num7 < num6) ? ((num18 >= num19) ? ((num7 < num19) ? 1 : 2) : ((num7 < num18) ? 1 : 0)) : ((num18 >= num19) ? ((num6 < num19) ? 3 : 2) : ((num6 < num18) ? 3 : 0))) : ((num8 < num9) ? ((num17 >= num16) ? ((num8 >= num16) ? 3 : 0) : ((num8 >= num17) ? 1 : 0)) : ((num17 >= num16) ? ((num9 >= num16) ? 3 : 0) : ((num9 >= num17) ? 1 : 2))));
		return (4 + num24 - num) % 4;
	}

	internal bool TryGenerateRuinAtSurface(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos startPos, string locationCode)
	{
		if (schematicDatas.Length == 0)
		{
			return false;
		}
		int num = rand.NextInt(schematicDatas.Length);
		int num2 = rand.NextInt(4);
		BlockSchematicStructure blockSchematicStructure = schematicDatas[num][num2];
		blockSchematicStructure.Unpack(worldForCollectibleResolve.Api, num2);
		startPos = startPos.AddCopy(0, blockSchematicStructure.OffsetY, 0);
		if (blockSchematicStructure.EntranceRotation != -1)
		{
			num2 = FindClearEntranceRotation(blockAccessor, schematicDatas[num], startPos);
			blockSchematicStructure = schematicDatas[num][num2];
			blockSchematicStructure.Unpack(worldForCollectibleResolve.Api, num2);
		}
		int num3 = (int)Math.Ceiling((float)blockSchematicStructure.SizeX / 2f);
		int num4 = (int)Math.Ceiling((float)blockSchematicStructure.SizeZ / 2f);
		int sizeX = blockSchematicStructure.SizeX;
		int sizeZ = blockSchematicStructure.SizeZ;
		tmpPos.Set(startPos.X + num3, 0, startPos.Z + num4);
		int terrainMapheightAt = blockAccessor.GetTerrainMapheightAt(startPos);
		if (terrainMapheightAt < worldForCollectibleResolve.SeaLevel - MaxBelowSealevel)
		{
			return false;
		}
		tmpPos.Set(startPos.X, 0, startPos.Z);
		int terrainMapheightAt2 = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Set(startPos.X + sizeX, 0, startPos.Z);
		int terrainMapheightAt3 = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Set(startPos.X, 0, startPos.Z + sizeZ);
		int terrainMapheightAt4 = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Set(startPos.X + sizeX, 0, startPos.Z + sizeZ);
		int terrainMapheightAt5 = blockAccessor.GetTerrainMapheightAt(tmpPos);
		int num5 = GameMath.Max(terrainMapheightAt, terrainMapheightAt2, terrainMapheightAt3, terrainMapheightAt4, terrainMapheightAt5);
		int num6 = GameMath.Min(terrainMapheightAt, terrainMapheightAt2, terrainMapheightAt3, terrainMapheightAt4, terrainMapheightAt5);
		if (blockSchematicStructure.SizeX >= 30)
		{
			int num7 = (int)((double)blockSchematicStructure.SizeX * 0.15 + 8.0);
			for (int i = num7; i < blockSchematicStructure.SizeX; i += num7)
			{
				tmpPos.Set(startPos.X + i, 0, startPos.Z);
				int terrainMapheightAt6 = blockAccessor.GetTerrainMapheightAt(tmpPos);
				tmpPos.Set(startPos.X + i, 0, startPos.Z + sizeZ);
				int terrainMapheightAt7 = blockAccessor.GetTerrainMapheightAt(tmpPos);
				tmpPos.Set(startPos.X + i, 0, startPos.Z + sizeZ / 2);
				int terrainMapheightAt8 = blockAccessor.GetTerrainMapheightAt(tmpPos);
				num5 = GameMath.Max(num5, terrainMapheightAt6, terrainMapheightAt7, terrainMapheightAt8);
				num6 = GameMath.Min(num6, terrainMapheightAt6, terrainMapheightAt7, terrainMapheightAt8);
			}
		}
		else if (blockSchematicStructure.SizeX >= 15)
		{
			int num8 = blockSchematicStructure.SizeX / 2;
			tmpPos.Set(startPos.X + num8, 0, startPos.Z);
			int terrainMapheightAt9 = blockAccessor.GetTerrainMapheightAt(tmpPos);
			tmpPos.Set(startPos.X + num8, 0, startPos.Z + sizeZ);
			int terrainMapheightAt10 = blockAccessor.GetTerrainMapheightAt(tmpPos);
			tmpPos.Set(startPos.X + num8, 0, startPos.Z + sizeZ / 2);
			int terrainMapheightAt11 = blockAccessor.GetTerrainMapheightAt(tmpPos);
			num5 = GameMath.Max(num5, terrainMapheightAt9, terrainMapheightAt10, terrainMapheightAt11);
			num6 = GameMath.Min(num6, terrainMapheightAt9, terrainMapheightAt10, terrainMapheightAt11);
		}
		if (blockSchematicStructure.SizeZ >= 30)
		{
			int num9 = (int)((double)blockSchematicStructure.SizeZ * 0.15 + 8.0);
			for (int j = num9; j < blockSchematicStructure.SizeZ; j += num9)
			{
				tmpPos.Set(startPos.X + sizeX, 0, startPos.Z + j);
				int terrainMapheightAt12 = blockAccessor.GetTerrainMapheightAt(tmpPos);
				tmpPos.Set(startPos.X, 0, startPos.Z + j);
				int terrainMapheightAt13 = blockAccessor.GetTerrainMapheightAt(tmpPos);
				tmpPos.Set(startPos.X + sizeX / 2, 0, startPos.Z + j);
				int terrainMapheightAt14 = blockAccessor.GetTerrainMapheightAt(tmpPos);
				num5 = GameMath.Max(num5, terrainMapheightAt12, terrainMapheightAt13, terrainMapheightAt14);
				num6 = GameMath.Min(num6, terrainMapheightAt12, terrainMapheightAt13, terrainMapheightAt14);
			}
		}
		else if (blockSchematicStructure.SizeZ >= 15)
		{
			int num10 = blockSchematicStructure.SizeZ / 2;
			tmpPos.Set(startPos.X + sizeX, 0, startPos.Z + num10);
			int terrainMapheightAt15 = blockAccessor.GetTerrainMapheightAt(tmpPos);
			tmpPos.Set(startPos.X, 0, startPos.Z + num10);
			int terrainMapheightAt16 = blockAccessor.GetTerrainMapheightAt(tmpPos);
			tmpPos.Set(startPos.X + sizeX / 2, 0, startPos.Z + num10);
			int terrainMapheightAt17 = blockAccessor.GetTerrainMapheightAt(tmpPos);
			num5 = GameMath.Max(num5, terrainMapheightAt15, terrainMapheightAt16, terrainMapheightAt17);
			num6 = GameMath.Min(num6, terrainMapheightAt15, terrainMapheightAt16, terrainMapheightAt17);
		}
		if (Math.Abs(num5 - num6) > blockSchematicStructure.MaxYDiff)
		{
			return false;
		}
		startPos.Y = num6 + blockSchematicStructure.OffsetY;
		tmpPos.Set(startPos.X, startPos.Y + 1, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + sizeX, startPos.Y + 1, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y + 1, startPos.Z + sizeZ);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + sizeX, startPos.Y + 1, startPos.Z + sizeZ);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		startPos.Y++;
		if (!TestAboveGroundCheckPositions(blockAccessor, startPos, blockSchematicStructure.AbovegroundCheckPositions))
		{
			return false;
		}
		if (!SatisfiesMinDistance(startPos, worldForCollectibleResolve))
		{
			return false;
		}
		if (WouldOverlapAt(blockAccessor, startPos, blockSchematicStructure, locationCode))
		{
			return false;
		}
		LastPlacedSchematicLocation.Set(startPos.X, startPos.Y, startPos.Z, startPos.X + blockSchematicStructure.SizeX, startPos.Y + blockSchematicStructure.SizeY, startPos.Z + blockSchematicStructure.SizeZ);
		LastPlacedSchematic = blockSchematicStructure;
		blockSchematicStructure.PlaceRespectingBlockLayers(blockAccessor, worldForCollectibleResolve, startPos, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, resolvedRockTypeRemaps, replacewithblocklayersBlockids, GenStructures.ReplaceMetaBlocks);
		return true;
	}

	internal bool TryGenerateAtSurface(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos startPos, string locationCode)
	{
		int num = rand.NextInt(schematicDatas.Length);
		int num2 = rand.NextInt(4);
		BlockSchematicStructure blockSchematicStructure = schematicDatas[num][num2];
		blockSchematicStructure.Unpack(worldForCollectibleResolve.Api, num2);
		startPos = startPos.AddCopy(0, blockSchematicStructure.OffsetY, 0);
		if (blockSchematicStructure.EntranceRotation != -1)
		{
			num2 = FindClearEntranceRotation(blockAccessor, schematicDatas[num], startPos);
			blockSchematicStructure = schematicDatas[num][num2];
			blockSchematicStructure.Unpack(worldForCollectibleResolve.Api, num2);
		}
		int num3 = (int)Math.Ceiling((float)blockSchematicStructure.SizeX / 2f);
		int num4 = (int)Math.Ceiling((float)blockSchematicStructure.SizeZ / 2f);
		int sizeX = blockSchematicStructure.SizeX;
		int sizeZ = blockSchematicStructure.SizeZ;
		tmpPos.Set(startPos.X + num3, 0, startPos.Z + num4);
		int terrainMapheightAt = blockAccessor.GetTerrainMapheightAt(tmpPos);
		if (terrainMapheightAt < worldForCollectibleResolve.SeaLevel - MaxBelowSealevel)
		{
			return false;
		}
		tmpPos.Set(startPos.X, 0, startPos.Z);
		int terrainMapheightAt2 = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Set(startPos.X + sizeX, 0, startPos.Z);
		int terrainMapheightAt3 = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Set(startPos.X, 0, startPos.Z + sizeZ);
		int terrainMapheightAt4 = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Set(startPos.X + sizeX, 0, startPos.Z + sizeZ);
		int terrainMapheightAt5 = blockAccessor.GetTerrainMapheightAt(tmpPos);
		if (GameMath.Max(terrainMapheightAt, terrainMapheightAt2, terrainMapheightAt3, terrainMapheightAt4, terrainMapheightAt5) - GameMath.Min(terrainMapheightAt, terrainMapheightAt2, terrainMapheightAt3, terrainMapheightAt4, terrainMapheightAt5) != 0)
		{
			return false;
		}
		startPos.Y = terrainMapheightAt + 1 + blockSchematicStructure.OffsetY;
		tmpPos.Set(startPos.X + num3, startPos.Y - 1, startPos.Z + num4);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y - 1, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + sizeX, startPos.Y - 1, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y - 1, startPos.Z + sizeZ);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + sizeX, startPos.Y - 1, startPos.Z + sizeZ);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + sizeX, startPos.Y, startPos.Z + sizeZ);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + sizeX, startPos.Y, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y, startPos.Z + sizeZ);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + sizeX, startPos.Y, startPos.Z + sizeZ);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y + 1, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + sizeX, startPos.Y + 1, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y + 1, startPos.Z + sizeZ);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + sizeX, startPos.Y + 1, startPos.Z + sizeZ);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		if (!SatisfiesMinDistance(startPos, worldForCollectibleResolve))
		{
			return false;
		}
		if (WouldOverlapAt(blockAccessor, startPos, blockSchematicStructure, locationCode))
		{
			return false;
		}
		LastPlacedSchematicLocation.Set(startPos.X, startPos.Y, startPos.Z, startPos.X + blockSchematicStructure.SizeX, startPos.Y + blockSchematicStructure.SizeY, startPos.Z + blockSchematicStructure.SizeZ);
		LastPlacedSchematic = blockSchematicStructure;
		blockSchematicStructure.PlaceRespectingBlockLayers(blockAccessor, worldForCollectibleResolve, startPos, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, resolvedRockTypeRemaps, replacewithblocklayersBlockids, GenStructures.ReplaceMetaBlocks);
		return true;
	}

	internal bool TryGenerateUnderwater(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos pos, string locationCode)
	{
		return false;
	}

	internal bool TryGenerateUnderground(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos pos, string locationCode)
	{
		int num = rand.NextInt(schematicDatas.Length);
		BlockSchematicStructure[] array = schematicDatas[num];
		BlockPos blockPos = pos.Copy();
		array[0].Unpack(worldForCollectibleResolve.Api, 0);
		if (array[0].PathwayStarts.Length != 0)
		{
			return tryGenerateAttachedToCave(blockAccessor, worldForCollectibleResolve, array, blockPos, locationCode);
		}
		int num2 = rand.NextInt(4);
		BlockSchematicStructure blockSchematicStructure = array[num2];
		blockSchematicStructure.Unpack(worldForCollectibleResolve.Api, num2);
		BlockPos blockPos2 = blockSchematicStructure.AdjustStartPos(blockPos.Copy(), Origin);
		LastPlacedSchematicLocation.Set(blockPos2.X, blockPos2.Y, blockPos2.Z, blockPos2.X + blockSchematicStructure.SizeX, blockPos2.Y + blockSchematicStructure.SizeY, blockPos2.Z + blockSchematicStructure.SizeZ);
		LastPlacedSchematic = blockSchematicStructure;
		if (insideblockids.Count > 0 && !insideblockids.Contains(blockAccessor.GetBlock(blockPos).Id))
		{
			return false;
		}
		if (!TestUndergroundCheckPositions(blockAccessor, blockPos2, blockSchematicStructure.UndergroundCheckPositions))
		{
			return false;
		}
		if (!SatisfiesMinDistance(pos, worldForCollectibleResolve))
		{
			return false;
		}
		if (WouldOverlapAt(blockAccessor, pos, blockSchematicStructure, locationCode))
		{
			return false;
		}
		if (resolvedRockTypeRemaps != null)
		{
			Block block = null;
			int num3 = 0;
			while (block == null && num3 < 10)
			{
				Block blockRaw = blockAccessor.GetBlockRaw(blockPos2.X + rand.NextInt(blockSchematicStructure.SizeX), blockPos2.Y + rand.NextInt(blockSchematicStructure.SizeY), blockPos2.Z + rand.NextInt(blockSchematicStructure.SizeZ), 1);
				if (blockRaw.BlockMaterial == EnumBlockMaterial.Stone)
				{
					block = blockRaw;
				}
				num3++;
			}
			blockSchematicStructure.PlaceReplacingBlocks(blockAccessor, worldForCollectibleResolve, blockPos2, blockSchematicStructure.ReplaceMode, resolvedRockTypeRemaps, block?.Id, GenStructures.ReplaceMetaBlocks);
		}
		else
		{
			blockSchematicStructure.Place(blockAccessor, worldForCollectibleResolve, blockPos);
		}
		return true;
	}

	private bool tryGenerateAttachedToCave(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockSchematicStructure[] schematicStruc, BlockPos targetPos, string locationCode)
	{
		Block block = null;
		Block block2 = blockAccessor.GetBlock(targetPos);
		if (block2.Id != 0)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i <= 4; i++)
		{
			targetPos.Down();
			block2 = blockAccessor.GetBlock(targetPos);
			if (block2.BlockMaterial == EnumBlockMaterial.Stone)
			{
				block = block2;
				targetPos.Up();
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return false;
		}
		BlockSchematicStructure blockSchematicStructure = schematicStruc[0];
		blockSchematicStructure.Unpack(worldForCollectibleResolve.Api, 0);
		int num = rand.NextInt(blockSchematicStructure.PathwayStarts.Length);
		int num2 = -1;
		BlockFacing blockFacing = null;
		BlockPos[] array = null;
		for (int j = 0; j < 4; j++)
		{
			blockSchematicStructure = schematicStruc[j];
			blockSchematicStructure.Unpack(worldForCollectibleResolve.Api, j);
			array = blockSchematicStructure.PathwayOffsets[num];
			blockFacing = blockSchematicStructure.PathwaySides[num];
			num2 = CanPlacePathwayAt(blockAccessor, array, blockFacing, targetPos);
			if (num2 != -1)
			{
				break;
			}
		}
		if (num2 == -1)
		{
			return false;
		}
		BlockPos blockPos = blockSchematicStructure.PathwayStarts[num];
		targetPos.Add(-blockPos.X - blockFacing.Normali.X * num2, -blockPos.Y - blockFacing.Normali.Y * num2 + blockSchematicStructure.OffsetY, -blockPos.Z - blockFacing.Normali.Z * num2);
		if (targetPos.Y <= 0)
		{
			return false;
		}
		if (!TestUndergroundCheckPositions(blockAccessor, targetPos, blockSchematicStructure.UndergroundCheckPositions))
		{
			return false;
		}
		if (WouldOverlapAt(blockAccessor, targetPos, blockSchematicStructure, locationCode))
		{
			return false;
		}
		LastPlacedSchematicLocation.Set(targetPos.X, targetPos.Y, targetPos.Z, targetPos.X + blockSchematicStructure.SizeX, targetPos.Y + blockSchematicStructure.SizeY, targetPos.Z + blockSchematicStructure.SizeZ);
		LastPlacedSchematic = blockSchematicStructure;
		if (resolvedRockTypeRemaps != null)
		{
			blockSchematicStructure.PlaceReplacingBlocks(blockAccessor, worldForCollectibleResolve, targetPos, blockSchematicStructure.ReplaceMode, resolvedRockTypeRemaps, block.Id, GenStructures.ReplaceMetaBlocks);
		}
		else
		{
			blockSchematicStructure.Place(blockAccessor, worldForCollectibleResolve, targetPos);
		}
		ushort blockId = 0;
		for (int k = 0; k < array.Length; k++)
		{
			for (int l = 0; l <= num2; l++)
			{
				tmpPos.Set(targetPos.X + blockPos.X + array[k].X + (l + 1) * blockFacing.Normali.X, targetPos.Y + blockPos.Y + array[k].Y + (l + 1) * blockFacing.Normali.Y, targetPos.Z + blockPos.Z + array[k].Z + (l + 1) * blockFacing.Normali.Z);
				blockAccessor.SetBlock(blockId, tmpPos);
			}
		}
		return true;
	}

	private bool TestUndergroundCheckPositions(IBlockAccessor blockAccessor, BlockPos pos, BlockPos[] testPositionsDelta)
	{
		foreach (BlockPos blockPos in testPositionsDelta)
		{
			utestPos.Set(pos.X + blockPos.X, pos.Y + blockPos.Y, pos.Z + blockPos.Z);
			if (blockAccessor.GetBlock(utestPos).BlockMaterial != EnumBlockMaterial.Stone)
			{
				return false;
			}
		}
		return true;
	}

	private bool TestAboveGroundCheckPositions(IBlockAccessor blockAccessor, BlockPos pos, BlockPos[] testPositionsDelta)
	{
		foreach (BlockPos blockPos in testPositionsDelta)
		{
			utestPos.Set(pos.X + blockPos.X, pos.Y + blockPos.Y, pos.Z + blockPos.Z);
			int terrainMapheightAt = blockAccessor.GetTerrainMapheightAt(utestPos);
			if (utestPos.Y <= terrainMapheightAt)
			{
				return false;
			}
		}
		return true;
	}

	private int CanPlacePathwayAt(IBlockAccessor blockAccessor, BlockPos[] pathway, BlockFacing towardsFacing, BlockPos targetPos)
	{
		BlockPos blockPos = new BlockPos();
		bool flag = rand.NextInt(2) > 0;
		for (int num = 3; num >= 1; num--)
		{
			int num2 = (flag ? (3 - num) : num);
			int num3 = num2 * towardsFacing.Normali.X;
			int num4 = num2 * towardsFacing.Normali.Z;
			int num5 = 0;
			for (int i = 0; i < pathway.Length; i++)
			{
				blockPos.Set(targetPos.X + pathway[i].X + num3, targetPos.Y + pathway[i].Y, targetPos.Z + pathway[i].Z + num4);
				Block block = blockAccessor.GetBlock(blockPos);
				if (block.Id == 0)
				{
					num5++;
				}
				else if (block.BlockMaterial != EnumBlockMaterial.Stone)
				{
					return -1;
				}
			}
			if (num5 > 0 && num5 < pathway.Length)
			{
				return num2;
			}
		}
		return -1;
	}

	private bool WouldOverlapAt(IBlockAccessor blockAccessor, BlockPos pos, BlockSchematicStructure schematic, string locationCode)
	{
		int regionSize = blockAccessor.RegionSize;
		int max = blockAccessor.MapSizeX / regionSize;
		int max2 = blockAccessor.MapSizeZ / regionSize;
		int num = GameMath.Clamp(pos.X / regionSize, 0, max);
		int num2 = GameMath.Clamp(pos.Z / regionSize, 0, max2);
		int num3 = GameMath.Clamp((pos.X + schematic.SizeX) / regionSize, 0, max);
		int num4 = GameMath.Clamp((pos.Z + schematic.SizeZ) / regionSize, 0, max2);
		tmpLoc.Set(pos.X, pos.Y, pos.Z, pos.X + schematic.SizeX, pos.Y + schematic.SizeY, pos.Z + schematic.SizeZ);
		for (int i = num; i <= num3; i++)
		{
			for (int j = num2; j <= num4; j++)
			{
				IMapRegion mapRegion = blockAccessor.GetMapRegion(i, j);
				if (mapRegion == null)
				{
					continue;
				}
				foreach (GeneratedStructure generatedStructure in mapRegion.GeneratedStructures)
				{
					if (generatedStructure.Location.Intersects(tmpLoc))
					{
						return true;
					}
				}
			}
		}
		if (genStructuresSys.WouldSchematicOverlapAt(blockAccessor, pos, tmpLoc, locationCode))
		{
			return true;
		}
		return false;
	}

	public bool SatisfiesMinDistance(BlockPos pos, IWorldAccessor world)
	{
		return SatisfiesMinDistance(pos, world, MinGroupDistance, Group);
	}

	public static bool SatisfiesMinDistance(BlockPos pos, IWorldAccessor world, int mingroupDistance, string group)
	{
		if (mingroupDistance < 1)
		{
			return true;
		}
		int regionSize = world.BlockAccessor.RegionSize;
		int max = world.BlockAccessor.MapSizeX / regionSize;
		int max2 = world.BlockAccessor.MapSizeZ / regionSize;
		int num = pos.X - mingroupDistance;
		int num2 = pos.Z - mingroupDistance;
		int num3 = pos.X + mingroupDistance;
		int num4 = pos.Z + mingroupDistance;
		long num5 = (long)mingroupDistance * (long)mingroupDistance;
		int num6 = GameMath.Clamp(num / regionSize, 0, max);
		int num7 = GameMath.Clamp(num2 / regionSize, 0, max2);
		int num8 = GameMath.Clamp(num3 / regionSize, 0, max);
		int num9 = GameMath.Clamp(num4 / regionSize, 0, max2);
		for (int i = num6; i <= num8; i++)
		{
			for (int j = num7; j <= num9; j++)
			{
				IMapRegion mapRegion = world.BlockAccessor.GetMapRegion(i, j);
				if (mapRegion == null)
				{
					continue;
				}
				foreach (GeneratedStructure generatedStructure in mapRegion.GeneratedStructures)
				{
					if (generatedStructure.Group == group && generatedStructure.Location.Center.SquareDistanceTo(pos.X, pos.Y, pos.Z) < num5)
					{
						return false;
					}
				}
			}
		}
		return true;
	}
}
