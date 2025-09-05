using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods;

public class BlockSchematicStructure : BlockSchematic
{
	public Dictionary<int, AssetLocation> BlockCodesTmpForRemap = new Dictionary<int, AssetLocation>();

	public string FromFileName;

	public Block[,,] blocksByPos;

	public Dictionary<int, Block> FluidBlocksByPos;

	public BlockLayerConfig blockLayerConfig;

	private int mapheight;

	private PlaceBlockDelegate handler;

	internal GenBlockLayers genBlockLayers;

	public int MaxYDiff = 3;

	public int MaxBelowSealevel = 20;

	public int? StoryLocationMaxAmount;

	public int OffsetY { get; set; } = -1;

	public static bool SatisfiesMinSpawnDistance(int minSpawnDistance, BlockPos pos, BlockPos spawnPos)
	{
		if (minSpawnDistance <= 0)
		{
			return true;
		}
		return spawnPos.HorDistanceSqTo(pos.X, pos.Z) > (float)(minSpawnDistance * minSpawnDistance);
	}

	public override void Init(IBlockAccessor blockAccessor)
	{
		base.Init(blockAccessor);
		mapheight = blockAccessor.MapSizeY;
		blocksByPos = new Block[SizeX + 1, SizeY + 1, SizeZ + 1];
		FluidBlocksByPos = new Dictionary<int, Block>();
		for (int i = 0; i < Indices.Count; i++)
		{
			uint num = Indices[i];
			int key = BlockIds[i];
			int num2 = (int)(num & 0x3FF);
			int num3 = (int)((num >> 20) & 0x3FF);
			int num4 = (int)((num >> 10) & 0x3FF);
			Block block = blockAccessor.GetBlock(BlockCodes[key]);
			if (block != null)
			{
				if (block.ForFluidsLayer)
				{
					FluidBlocksByPos.Add((int)num, block);
				}
				else
				{
					blocksByPos[num2, num3, num4] = block;
				}
			}
		}
		handler = null;
		switch (ReplaceMode)
		{
		case EnumReplaceMode.ReplaceAll:
			handler = PlaceReplaceAll;
			break;
		case EnumReplaceMode.Replaceable:
			handler = PlaceReplaceable;
			break;
		case EnumReplaceMode.ReplaceAllNoAir:
			handler = PlaceReplaceAllNoAir;
			break;
		case EnumReplaceMode.ReplaceOnlyAir:
			handler = PlaceReplaceOnlyAir;
			break;
		}
	}

	public int PlaceRespectingBlockLayers(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos startPos, int climateUpLeft, int climateUpRight, int climateBotLeft, int climateBotRight, Dictionary<int, Dictionary<int, int>> replaceBlocks, int[] replaceWithBlockLayersBlockids, bool replaceMetaBlocks = true, bool replaceBlockEntities = false, bool suppressSoilIfAirBelow = false, bool displaceWater = false)
	{
		Unpack(worldForCollectibleResolve.Api);
		if (genBlockLayers == null)
		{
			genBlockLayers = worldForCollectibleResolve.Api.ModLoader.GetModSystem<GenBlockLayers>();
		}
		BlockPos blockPos = new BlockPos(startPos.dimension);
		BlockPos blockPos2 = new BlockPos(startPos.dimension);
		BlockPos blockPos3 = new BlockPos(startPos.dimension);
		int num = 0;
		int num2 = startPos.X / 32 * 32;
		int num3 = startPos.Z / 32 * 32;
		blockPos.Set(SizeX / 2 + startPos.X, startPos.Y, SizeZ / 2 + startPos.Z);
		IMapChunk mapChunkAtBlockPos = blockAccessor.GetMapChunkAtBlockPos(blockPos);
		int num4 = mapChunkAtBlockPos.TopRockIdMap[blockPos.Z % 32 * 32 + blockPos.X % 32];
		IWorldAccessor worldAccessor;
		if (!(blockAccessor is IWorldGenBlockAccessor worldGenBlockAccessor))
		{
			worldAccessor = worldForCollectibleResolve;
		}
		else
		{
			IWorldAccessor worldgenWorldAccessor = worldGenBlockAccessor.WorldgenWorldAccessor;
			worldAccessor = worldgenWorldAccessor;
		}
		IWorldAccessor world = worldAccessor;
		resolveReplaceRemapsForBlockEntities(blockAccessor, worldForCollectibleResolve, replaceBlocks, num4);
		Dictionary<BlockPos, Block> dictionary = new Dictionary<BlockPos, Block>();
		for (int i = 0; i < SizeX; i++)
		{
			for (int j = 0; j < SizeZ; j++)
			{
				blockPos.Set(i + startPos.X, startPos.Y, j + startPos.Z);
				if (!blockAccessor.IsValidPos(blockPos))
				{
					continue;
				}
				mapChunkAtBlockPos = blockAccessor.GetMapChunkAtBlockPos(blockPos);
				int rockBlockId = mapChunkAtBlockPos.TopRockIdMap[blockPos.Z % 32 * 32 + blockPos.X % 32];
				int num5 = mapChunkAtBlockPos.WorldGenTerrainHeightMap[blockPos.Z % 32 * 32 + blockPos.X % 32] - (SizeY + startPos.Y);
				int num6 = -1;
				int num7 = -1;
				Block blockAbove = blockAccessor.GetBlockAbove(blockPos, SizeY, 2);
				if (blockAbove != null && blockAbove.IsLiquid())
				{
					num7++;
				}
				bool flag = true;
				for (int num8 = SizeY - 1; num8 >= 0; num8--)
				{
					num5++;
					blockPos.Set(i + startPos.X, num8 + startPos.Y, j + startPos.Z);
					if (!blockAccessor.IsValidPos(blockPos))
					{
						continue;
					}
					blockPos2.Set(i, num8, j);
					Block block = blocksByPos[i, num8, j];
					FluidBlocksByPos.TryGetValue(blockPos2.ToSchematicIndex(), out var value);
					if (block == null)
					{
						block = value;
					}
					blockAbove = blockAccessor.GetBlock(blockPos, 2);
					if (blockAbove != null && blockAbove.IsLiquid())
					{
						num7++;
					}
					if (block == null || (replaceMetaBlocks && (block.Id == BlockSchematic.UndergroundBlockId || block.Id == BlockSchematic.AbovegroundBlockId)))
					{
						continue;
					}
					if (block.Replaceable < 1000 && num5 >= 0 && (replaceWithBlockLayersBlockids.Contains(block.BlockId) || block.CustomBlockLayerHandler))
					{
						if (suppressSoilIfAirBelow && (num8 == 0 || blocksByPos[i, num8 - 1, j] == null) && blockAccessor.GetBlockBelow(blockPos, 1, 1).Replaceable > 3000)
						{
							for (int k = num8 + 1; k < SizeY; k++)
							{
								Block block2 = blocksByPos[i, k, j];
								if (block2 == null || !replaceWithBlockLayersBlockids.Contains(block2.BlockId))
								{
									break;
								}
								blockAccessor.SetBlock(0, blockPos3.Set(blockPos.X, startPos.Y + k, blockPos.Z), 1);
							}
							continue;
						}
						if (num5 == 0 && replaceWithBlockLayersBlockids.Length > 1)
						{
							Block blockAbove2 = blockAccessor.GetBlockAbove(blockPos, 1, 1);
							if (blockAbove2.SideSolid[BlockFacing.DOWN.Index] && blockAbove2.BlockMaterial != EnumBlockMaterial.Wood && blockAbove2.BlockMaterial != EnumBlockMaterial.Snow && blockAbove2.BlockMaterial != EnumBlockMaterial.Ice)
							{
								num5++;
							}
						}
						int num9 = GameMath.BiLerpRgbColor(GameMath.Clamp((float)(blockPos.X - num2) / 32f, 0f, 1f), GameMath.Clamp((float)(blockPos.Z - num3) / 32f, 0f, 1f), climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
						Block blockLayerBlock = GetBlockLayerBlock((num9 >> 8) & 0xFF, (num9 >> 16) & 0xFF, blockPos.Y - 1, rockBlockId, num5, block, worldForCollectibleResolve.Blocks, blockPos, num7);
						if (block.CustomBlockLayerHandler && blockLayerBlock != block)
						{
							dictionary[blockPos.Copy()] = blockLayerBlock;
						}
						else
						{
							block = blockLayerBlock;
						}
					}
					if (replaceBlocks != null && replaceBlocks.TryGetValue(block.Id, out var value2) && value2.TryGetValue(num4, out var value3))
					{
						block = blockAccessor.GetBlock(value3);
					}
					if (block.ForFluidsLayer)
					{
						blockAccessor.SetBlock(0, blockPos, 1);
					}
					int num10 = handler(blockAccessor, blockPos, block, replaceMeta: true);
					if (value != null && !block.Equals(value))
					{
						handler(blockAccessor, blockPos, value, replaceMeta: true);
					}
					if (num10 > 0)
					{
						if (displaceWater)
						{
							blockAccessor.SetBlock(0, blockPos, 2);
						}
						else if (block.Id != 0 && !block.SideSolid.All)
						{
							blockAbove = blockAccessor.GetBlockAbove(blockPos, 1, 2);
							if (blockAbove.Id != 0)
							{
								blockAccessor.SetBlock(blockAbove.BlockId, blockPos, 2);
							}
						}
						if (flag)
						{
							Block blockAbove3 = blockAccessor.GetBlockAbove(blockPos, 1, 1);
							if (blockAbove3.Id > 0)
							{
								blockAbove3.OnNeighbourBlockChange(world, blockPos.UpCopy(), blockPos);
							}
							flag = false;
						}
						num += num10;
						if (!block.RainPermeable)
						{
							if (IsFillerOrPath(block))
							{
								int num11 = blockPos.X % 32;
								int num12 = blockPos.Z % 32;
								if (mapChunkAtBlockPos.RainHeightMap[num12 * 32 + num11] == blockPos.Y)
								{
									mapChunkAtBlockPos.RainHeightMap[num12 * 32 + num11]--;
								}
							}
							else
							{
								num6 = Math.Max(blockPos.Y, num6);
							}
						}
					}
					if (block.GetLightHsv(blockAccessor, blockPos)[2] > 0 && blockAccessor is IWorldGenBlockAccessor)
					{
						Block block3 = blockAccessor.GetBlock(blockPos);
						((IWorldGenBlockAccessor)blockAccessor).ScheduleBlockLightUpdate(blockPos, block3.BlockId, block.BlockId);
					}
				}
				if (num6 >= 0)
				{
					int num13 = blockPos.X % 32;
					int num14 = blockPos.Z % 32;
					int val = mapChunkAtBlockPos.RainHeightMap[num14 * 32 + num13];
					mapChunkAtBlockPos.RainHeightMap[num14 * 32 + num13] = (ushort)Math.Max(val, num6);
				}
			}
		}
		PlaceDecors(blockAccessor, startPos);
		PlaceEntitiesAndBlockEntities(blockAccessor, worldForCollectibleResolve, startPos, BlockCodesTmpForRemap, ItemCodes, replaceBlockEntities, replaceBlocks, num4, dictionary, replaceMetaBlocks);
		return num;
	}

	private void resolveReplaceRemapsForBlockEntities(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid)
	{
		if (replaceBlocks == null)
		{
			BlockCodesTmpForRemap = BlockCodes;
			return;
		}
		foreach (KeyValuePair<int, AssetLocation> blockCode in BlockCodes)
		{
			Block block = worldForCollectibleResolve.GetBlock(blockCode.Value);
			if (block != null)
			{
				BlockCodesTmpForRemap[blockCode.Key] = blockCode.Value;
				if (replaceBlocks.TryGetValue(block.Id, out var value) && value.TryGetValue(centerrockblockid, out var value2))
				{
					BlockCodesTmpForRemap[blockCode.Key] = blockAccessor.GetBlock(value2).Code;
				}
			}
		}
	}

	public virtual int PlaceReplacingBlocks(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos startPos, EnumReplaceMode mode, Dictionary<int, Dictionary<int, int>> replaceBlocks, int? rockBlockId, bool replaceMetaBlocks = true)
	{
		Unpack(worldForCollectibleResolve.Api);
		BlockPos blockPos = new BlockPos(startPos.dimension);
		int num = 0;
		blockPos.Set(SizeX / 2 + startPos.X, startPos.Y, SizeZ / 2 + startPos.Z);
		IMapChunk mapChunkAtBlockPos = blockAccessor.GetMapChunkAtBlockPos(blockPos);
		int num2 = rockBlockId ?? mapChunkAtBlockPos.TopRockIdMap[blockPos.Z % 32 * 32 + blockPos.X % 32];
		resolveReplaceRemapsForBlockEntities(blockAccessor, worldForCollectibleResolve, replaceBlocks, num2);
		PlaceBlockDelegate placeBlockDelegate = null;
		switch (ReplaceMode)
		{
		case EnumReplaceMode.ReplaceAll:
			placeBlockDelegate = PlaceReplaceAll;
			break;
		case EnumReplaceMode.Replaceable:
			placeBlockDelegate = PlaceReplaceable;
			break;
		case EnumReplaceMode.ReplaceAllNoAir:
			placeBlockDelegate = PlaceReplaceAllNoAir;
			break;
		case EnumReplaceMode.ReplaceOnlyAir:
			placeBlockDelegate = PlaceReplaceOnlyAir;
			break;
		}
		for (int i = 0; i < Indices.Count; i++)
		{
			uint num3 = Indices[i];
			int key = BlockIds[i];
			int num4 = (int)(num3 & 0x3FF);
			int num5 = (int)((num3 >> 20) & 0x3FF);
			int num6 = (int)((num3 >> 10) & 0x3FF);
			AssetLocation code = BlockCodes[key];
			Block block = blockAccessor.GetBlock(code);
			if (block == null || (replaceMetaBlocks && (block.Id == BlockSchematic.UndergroundBlockId || block.Id == BlockSchematic.AbovegroundBlockId)))
			{
				continue;
			}
			blockPos.Set(num4 + startPos.X, num5 + startPos.Y, num6 + startPos.Z);
			if (blockAccessor.IsValidPos(blockPos))
			{
				if (replaceBlocks.TryGetValue(block.Id, out var value) && value.TryGetValue(num2, out var value2))
				{
					block = blockAccessor.GetBlock(value2);
				}
				if (block.ForFluidsLayer && num3 != Indices[i - 1])
				{
					blockAccessor.SetBlock(0, blockPos, 1);
				}
				num += placeBlockDelegate(blockAccessor, blockPos, block, replaceMeta: true);
				if (block.LightHsv[2] > 0 && blockAccessor is IWorldGenBlockAccessor)
				{
					Block block2 = blockAccessor.GetBlock(blockPos);
					((IWorldGenBlockAccessor)blockAccessor).ScheduleBlockLightUpdate(blockPos, block2.BlockId, block.BlockId);
				}
			}
		}
		if (!(blockAccessor is IBlockAccessorRevertable))
		{
			PlaceDecors(blockAccessor, startPos);
			PlaceEntitiesAndBlockEntities(blockAccessor, worldForCollectibleResolve, startPos, BlockCodesTmpForRemap, ItemCodes, replaceBlockEntities: false, null, num2, null, GenStructures.ReplaceMetaBlocks);
		}
		return num;
	}

	internal Block GetBlockLayerBlock(int unscaledRain, int unscaledTemp, int posY, int rockBlockId, int forDepth, Block defaultBlock, IList<Block> blocks, BlockPos pos, int underWaterDepth)
	{
		if (blockLayerConfig == null)
		{
			return defaultBlock;
		}
		posY -= forDepth;
		float num = (float)genBlockLayers.distort2dx.Noise(pos.X, pos.Z);
		float scaledAdjustedTemperatureFloat = Climate.GetScaledAdjustedTemperatureFloat(unscaledTemp, posY - TerraGenConfig.seaLevel + (int)(num / 5f));
		float num2 = (float)Climate.GetRainFall(unscaledRain, posY) / 255f;
		float posYRel = ((float)posY - (float)TerraGenConfig.seaLevel) / ((float)mapheight - (float)TerraGenConfig.seaLevel);
		float fertilityRel = (float)Climate.GetFertilityFromUnscaledTemp((int)(num2 * 255f), unscaledTemp, posYRel) / 255f;
		double num3 = (double)GameMath.MurmurHash3(pos.X, 1, pos.Z) / 2147483647.0;
		num3 = (num3 + 1.0) * (double)blockLayerConfig.blockLayerTransitionSize;
		for (int i = 0; i < blockLayerConfig.Blocklayers.Length; i++)
		{
			if (underWaterDepth < 0)
			{
				BlockLayer blockLayer = blockLayerConfig.Blocklayers[i];
				float num4 = blockLayer.CalcYDistance(posY, mapheight);
				if (!((double)(blockLayer.CalcTrfDistance(scaledAdjustedTemperatureFloat, num2, fertilityRel) + num4) > num3))
				{
					int blockId = blockLayer.GetBlockId(num3, scaledAdjustedTemperatureFloat, num2, fertilityRel, rockBlockId, pos, mapheight);
					if (blockId != 0 && forDepth-- <= 0)
					{
						return blocks[blockId];
					}
				}
			}
			else if (i < blockLayerConfig.LakeBedLayer.BlockCodeByMin.Length)
			{
				LakeBedBlockCodeByMin lakeBedBlockCodeByMin = blockLayerConfig.LakeBedLayer.BlockCodeByMin[i];
				if (lakeBedBlockCodeByMin.Suitable(scaledAdjustedTemperatureFloat, num2, (float)posY / (float)mapheight, (float)num3) && underWaterDepth-- <= 0)
				{
					return blocks[lakeBedBlockCodeByMin.GetBlockForMotherRock(rockBlockId)];
				}
			}
		}
		return defaultBlock;
	}

	public override BlockSchematic ClonePacked()
	{
		return new BlockSchematicStructure
		{
			SizeX = SizeX,
			SizeY = SizeY,
			SizeZ = SizeZ,
			OffsetY = OffsetY,
			MaxYDiff = MaxYDiff,
			MaxBelowSealevel = MaxBelowSealevel,
			GameVersion = GameVersion,
			FromFileName = FromFileName,
			BlockCodes = new Dictionary<int, AssetLocation>(BlockCodes),
			ItemCodes = new Dictionary<int, AssetLocation>(ItemCodes),
			Indices = new List<uint>(Indices),
			BlockIds = new List<int>(BlockIds),
			BlockEntities = new Dictionary<uint, string>(BlockEntities),
			Entities = new List<string>(Entities),
			DecorIndices = new List<uint>(DecorIndices),
			DecorIds = new List<long>(DecorIds),
			ReplaceMode = ReplaceMode,
			EntranceRotation = EntranceRotation,
			OriginalPos = OriginalPos
		};
	}

	public void Unpack(ICoreAPI api)
	{
		if (blocksByPos == null)
		{
			Init(api.World.BlockAccessor);
			LoadMetaInformationAndValidate(api.World.BlockAccessor, api.World, FromFileName);
		}
	}

	public void Unpack(ICoreAPI api, int orientation)
	{
		if (orientation > 0 && blocksByPos == null)
		{
			TransformWhilePacked(api.World, EnumOrigin.BottomCenter, orientation * 90, null, PathwayBlocksUnpacked != null);
		}
		Unpack(api);
	}
}
