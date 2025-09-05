using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class ForestFloorSystem
{
	public const int Range = 16;

	public const int GridRowSize = 33;

	private ICoreServerAPI sapi;

	private IServerWorldAccessor worldAccessor;

	private IBlockAccessor api;

	[ThreadStatic]
	private static short[] outlineThreadSafe;

	private int[] forestBlocks;

	private List<BlockPatch> underTreePatches;

	private List<BlockPatch> onTreePatches;

	private GenVegetationAndPatches genPatchesSystem;

	private BlockPos tmpPos = new BlockPos();

	public ForestFloorSystem(ICoreServerAPI api)
	{
		sapi = api;
		worldAccessor = sapi.World;
		genPatchesSystem = sapi.ModLoader.GetModSystem<GenVegetationAndPatches>();
	}

	internal short[] GetOutline()
	{
		return outlineThreadSafe ?? (outlineThreadSafe = new short[1089]);
	}

	public void SetBlockPatches(BlockPatchConfig bpc)
	{
		forestBlocks = BlockForestFloor.InitialiseForestBlocks(worldAccessor);
		underTreePatches = new List<BlockPatch>();
		onTreePatches = new List<BlockPatch>();
		for (int i = 0; i < bpc.Patches.Length; i++)
		{
			BlockPatch blockPatch = bpc.Patches[i];
			if (blockPatch.Placement == EnumBlockPatchPlacement.UnderTrees || blockPatch.Placement == EnumBlockPatchPlacement.OnSurfacePlusUnderTrees)
			{
				underTreePatches.Add(blockPatch);
			}
			if (blockPatch.Placement == EnumBlockPatchPlacement.OnTrees)
			{
				onTreePatches.Add(blockPatch);
			}
		}
	}

	internal void ClearOutline()
	{
		short[] outline = GetOutline();
		for (int i = 0; i < outline.Length; i++)
		{
			outline[i] = 0;
		}
	}

	internal void CreateForestFloor(IBlockAccessor blockAccessor, TreeGenConfig config, BlockPos pos, IRandom rnd, int treesInChunkGenerated)
	{
		int grassLevelOffset = 0;
		ClimateCondition climateAt = blockAccessor.GetClimateAt(pos, EnumGetClimateMode.WorldGenValues);
		if (climateAt.Temperature > 24f && climateAt.Rainfall > 160f)
		{
			grassLevelOffset = 2;
		}
		short[] outline = GetOutline();
		api = blockAccessor;
		float num = climateAt.ForestDensity * climateAt.ForestDensity * 4f * (climateAt.Fertility + 0.25f);
		if ((double)climateAt.Fertility <= 0.25 || (double)num <= 0.4)
		{
			return;
		}
		for (int i = 0; i < outline.Length; i++)
		{
			outline[i] = (short)((float)outline[i] * num + 0.3f);
		}
		for (int j = 0; j < 7; j++)
		{
			bool flag = true;
			for (int k = 0; k < 16; k++)
			{
				for (int l = 0; l < 16; l++)
				{
					if (k == 0 && l == 0)
					{
						continue;
					}
					int num2 = (16 + l) * 33;
					int num3 = Math.Min((int)outline[num2 + (16 + k)], 162);
					if (num3 != 0)
					{
						int num4 = num2 + 33 + (16 + k);
						int num5 = num2 + (17 + k);
						if (outline[num4] < num3 - 18)
						{
							outline[num4] = (short)(num3 - 18);
							flag = false;
						}
						if (outline[num5] < num3 - 18)
						{
							outline[num5] = (short)(num3 - 18);
							flag = false;
						}
						num2 = (16 - l) * 33;
						num3 = Math.Min((int)outline[num2 + (16 + k)], 162);
						num4 = num2 - 33 + (16 + k);
						num5 = num2 + (17 + k);
						if (outline[num4] < num3 - 18)
						{
							outline[num4] = (short)(num3 - 18);
							flag = false;
						}
						if (outline[num5] < num3 - 18)
						{
							outline[num5] = (short)(num3 - 18);
							flag = false;
						}
					}
				}
				for (int m = 0; m < 16; m++)
				{
					if (k != 0 || m != 0)
					{
						int num6 = (16 + m) * 33;
						int num7 = Math.Min((int)outline[num6 + (16 - k)], 162);
						int num8 = num6 + 33 + (16 - k);
						int num9 = num6 + (15 - k);
						if (outline[num8] < num7 - 18)
						{
							outline[num8] = (short)(num7 - 18);
							flag = false;
						}
						if (outline[num9] < num7 - 18)
						{
							outline[num9] = (short)(num7 - 18);
							flag = false;
						}
						num6 = (16 - m) * 33;
						num7 = Math.Min((int)outline[num6 + (16 - k)], 162);
						num8 = num6 - 33 + (16 - k);
						num9 = num6 + (15 - k);
						if (outline[num8] < num7 - 18)
						{
							outline[num8] = (short)(num7 - 18);
							flag = false;
						}
						if (outline[num9] < num7 - 18)
						{
							outline[num9] = (short)(num7 - 18);
							flag = false;
						}
					}
				}
			}
			if (flag)
			{
				break;
			}
		}
		BlockPos blockPos = new BlockPos();
		for (int n = 0; n < outline.Length; n++)
		{
			int num10 = outline[n];
			if (num10 != 0)
			{
				int num11 = n / 33 - 16;
				int num12 = n % 33 - 16;
				blockPos.Set(pos.X + num12, pos.Y, pos.Z + num11);
				blockPos.Y = blockAccessor.GetTerrainMapheightAt(blockPos);
				if (blockPos.Y - pos.Y < 4)
				{
					CheckAndReplaceForestFloor(blockPos, num10, grassLevelOffset);
				}
			}
		}
		GenPatches(blockAccessor, pos, num, config.Treetype, rnd);
	}

	private void GenPatches(IBlockAccessor blockAccessor, BlockPos pos, float forestNess, EnumTreeType treetype, IRandom rnd)
	{
		BlockPatchConfig bpc = genPatchesSystem.bpc;
		int num = 5;
		int mapSizeY = blockAccessor.MapSizeY;
		int num2 = underTreePatches?.Count ?? 0;
		for (int i = 0; i < num2; i++)
		{
			BlockPatch blockPatch = underTreePatches[i];
			if (blockPatch.TreeType != EnumTreeType.Any && blockPatch.TreeType != treetype)
			{
				continue;
			}
			float num3 = 0.003f * forestNess * blockPatch.Chance * bpc.ChanceMultiplier.nextFloat(1f, rnd);
			while (num3-- > rnd.NextFloat())
			{
				int num4 = rnd.NextInt(2 * num) - num;
				int num5 = rnd.NextInt(2 * num) - num;
				tmpPos.Set(pos.X + num4, 0, pos.Z + num5);
				int terrainMapheightAt = blockAccessor.GetTerrainMapheightAt(tmpPos);
				if (terrainMapheightAt <= 0 || terrainMapheightAt >= mapSizeY - 8)
				{
					continue;
				}
				tmpPos.Y = terrainMapheightAt;
				ClimateCondition climateAt = blockAccessor.GetClimateAt(tmpPos, EnumGetClimateMode.WorldGenValues);
				if (climateAt == null || !bpc.IsPatchSuitableUnderTree(blockPatch, mapSizeY, climateAt, terrainMapheightAt))
				{
					continue;
				}
				int regionX = pos.X / blockAccessor.RegionSize;
				int regionZ = pos.Z / blockAccessor.RegionSize;
				if (blockPatch.MapCode != null && rnd.NextInt(255) > genPatchesSystem.GetPatchDensity(blockPatch.MapCode, tmpPos.X, tmpPos.Z, blockAccessor.GetMapRegion(regionX, regionZ)))
				{
					continue;
				}
				int value = 0;
				bool flag = true;
				if (blockPatch.BlocksByRockType != null)
				{
					flag = false;
					for (int j = 1; j < 5 && terrainMapheightAt - j > 0; j++)
					{
						string key = blockAccessor.GetBlock(tmpPos.X, terrainMapheightAt - j, tmpPos.Z).LastCodePart();
						if (genPatchesSystem.RockBlockIdsByType.TryGetValue(key, out value))
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					new LCGRandom(sapi.WorldManager.Seed + i).InitPositionSeed(tmpPos.X, tmpPos.Z);
					blockPatch.Generate(blockAccessor, rnd, tmpPos.X, tmpPos.Y, tmpPos.Z, value, isStoryPatch: false);
				}
			}
		}
		num2 = onTreePatches?.Count ?? 0;
		for (int k = 0; k < num2; k++)
		{
			BlockPatch blockPatch2 = onTreePatches[k];
			float num6 = 3f * forestNess * blockPatch2.Chance * bpc.ChanceMultiplier.nextFloat(1f, rnd);
			while (num6-- > rnd.NextFloat())
			{
				int num7 = 1 - rnd.NextInt(2) * 2;
				int num8 = rnd.NextInt(5);
				int num9 = 1 - rnd.NextInt(2) * 2;
				tmpPos.Set(pos.X + num7, pos.Y + num8, pos.Z + num9);
				if (api.GetBlock(tmpPos).Id != 0)
				{
					continue;
				}
				BlockFacing blockFacing = null;
				for (int l = 0; l < 4; l++)
				{
					BlockFacing blockFacing2 = BlockFacing.HORIZONTALS[l];
					Block blockOnSide = api.GetBlockOnSide(tmpPos, blockFacing2);
					if (blockOnSide is BlockLog && blockOnSide.Variant["type"] != "resin")
					{
						blockFacing = blockFacing2;
						break;
					}
				}
				if (blockFacing == null)
				{
					break;
				}
				ClimateCondition climateAt2 = blockAccessor.GetClimateAt(tmpPos, EnumGetClimateMode.WorldGenValues);
				if (climateAt2 != null && bpc.IsPatchSuitableUnderTree(blockPatch2, mapSizeY, climateAt2, tmpPos.Y))
				{
					int regionX2 = pos.X / blockAccessor.RegionSize;
					int regionZ2 = pos.Z / blockAccessor.RegionSize;
					if (blockPatch2.MapCode == null || rnd.NextInt(255) <= genPatchesSystem.GetPatchDensity(blockPatch2.MapCode, tmpPos.X, tmpPos.Z, blockAccessor.GetMapRegion(regionX2, regionZ2)))
					{
						int num10 = rnd.NextInt(blockPatch2.Blocks.Length);
						blockPatch2.Blocks[num10].TryPlaceBlockForWorldGen(blockAccessor, tmpPos, blockFacing, rnd);
					}
				}
			}
		}
	}

	private void CheckAndReplaceForestFloor(BlockPos pos, int intensity, int grassLevelOffset)
	{
		if (forestBlocks == null)
		{
			return;
		}
		Block block = api.GetBlock(pos);
		if (!(block is BlockForestFloor) && !(block is BlockSoil))
		{
			return;
		}
		if (block is BlockForestFloor blockForestFloor)
		{
			int num = blockForestFloor.CurrentLevel();
			intensity += num * 18 - 9;
			intensity = Math.Min(intensity, Math.Max(num * 18, (BlockForestFloor.MaxStage - 1) * 18));
		}
		int num2 = grassLevelOffset + intensity / 18;
		int blockId;
		if (num2 >= forestBlocks.Length - 1)
		{
			blockId = forestBlocks[(num2 <= forestBlocks.Length) ? 1u : 0u];
		}
		else
		{
			if (num2 == 0)
			{
				num2 = 1;
			}
			blockId = forestBlocks[forestBlocks.Length - num2];
		}
		api.SetBlock(blockId, pos);
	}

	private int GetRandomBlock(BlockPatch blockPatch)
	{
		return blockPatch.Blocks[0].Id;
	}

	private float GetDistance(ClimateCondition climate, BlockPatch variant)
	{
		float num = Math.Abs(climate.Temperature * 2f - (float)variant.MaxTemp - (float)variant.MinTemp) / (float)(variant.MaxTemp - variant.MinTemp);
		if (num > 1f)
		{
			return 5f;
		}
		float num2 = Math.Abs(climate.Fertility * 2f - variant.MaxFertility - variant.MinFertility) / (variant.MaxFertility - variant.MinFertility);
		if (num2 > 1f)
		{
			return 5f;
		}
		float num3 = Math.Abs(climate.Rainfall * 2f - variant.MaxRain - variant.MinRain) / (variant.MaxRain - variant.MinRain);
		if (num3 > 1.3f)
		{
			return 5f;
		}
		float num4 = Math.Abs((climate.ForestDensity + 0.2f) * 2f - variant.MaxForest - variant.MinForest) / (variant.MaxForest - variant.MinForest);
		return num * num + num2 * num2 + num3 * num3 + num4 * num4;
	}
}
