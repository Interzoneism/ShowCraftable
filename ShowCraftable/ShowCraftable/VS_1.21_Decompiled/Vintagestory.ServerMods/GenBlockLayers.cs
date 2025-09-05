using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenBlockLayers : ModStdWorldGen
{
	private ICoreServerAPI api;

	private List<int> BlockLayersIds = new List<int>();

	private LCGRandom rnd;

	private int mapheight;

	private ClampedSimplexNoise grassDensity;

	private ClampedSimplexNoise grassHeight;

	private int boilingWaterBlockId;

	public int[] layersUnderWaterTmp = new int[1];

	public int[] layersUnderWater = Array.Empty<int>();

	public BlockLayerConfig blockLayerConfig;

	public SimplexNoise distort2dx;

	public SimplexNoise distort2dz;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.4;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		this.api.Event.InitWorldGenerator(InitWorldGen, "standard");
		this.api.Event.InitWorldGenerator(InitWorldGen, "superflat");
		if (TerraGenConfig.DoDecorationPass)
		{
			this.api.Event.ChunkColumnGeneration(OnChunkColumnGeneration, EnumWorldGenPass.Terrain, "standard");
		}
		distort2dx = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 20980);
		distort2dz = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 20981);
	}

	private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams = null)
	{
	}

	public void InitWorldGen()
	{
		LoadGlobalConfig(api);
		blockLayerConfig = BlockLayerConfig.GetInstance(api);
		rnd = new LCGRandom(api.WorldManager.Seed);
		grassDensity = new ClampedSimplexNoise(new double[1] { 4.0 }, new double[1] { 0.5 }, rnd.NextInt());
		grassHeight = new ClampedSimplexNoise(new double[1] { 1.5 }, new double[1] { 0.5 }, rnd.NextInt());
		mapheight = api.WorldManager.MapSizeY;
		boilingWaterBlockId = api.World.GetBlock(new AssetLocation("boilingwater-still-7"))?.Id ?? 0;
	}

	private void OnChunkColumnGeneration(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		rnd.InitPositionSeed(chunkX, chunkZ);
		IntDataMap2D forestMap = chunks[0].MapChunk.MapRegion.ForestMap;
		IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
		IntDataMap2D beachMap = chunks[0].MapChunk.MapRegion.BeachMap;
		ushort[] rainHeightMap = chunks[0].MapChunk.RainHeightMap;
		int num = api.WorldManager.RegionSize / 32;
		int num2 = chunkX % num;
		int num3 = chunkZ % num;
		float num4 = (float)climateMap.InnerSize / (float)num;
		float num5 = (float)forestMap.InnerSize / (float)num;
		float num6 = (float)beachMap.InnerSize / (float)num;
		int unpaddedInt = forestMap.GetUnpaddedInt((int)((float)num2 * num5), (int)((float)num3 * num5));
		int unpaddedInt2 = forestMap.GetUnpaddedInt((int)((float)num2 * num5 + num5), (int)((float)num3 * num5));
		int unpaddedInt3 = forestMap.GetUnpaddedInt((int)((float)num2 * num5), (int)((float)num3 * num5 + num5));
		int unpaddedInt4 = forestMap.GetUnpaddedInt((int)((float)num2 * num5 + num5), (int)((float)num3 * num5 + num5));
		int unpaddedInt5 = beachMap.GetUnpaddedInt((int)((float)num2 * num6), (int)((float)num3 * num6));
		int unpaddedInt6 = beachMap.GetUnpaddedInt((int)((float)num2 * num6 + num6), (int)((float)num3 * num6));
		int unpaddedInt7 = beachMap.GetUnpaddedInt((int)((float)num2 * num6), (int)((float)num3 * num6 + num6));
		int unpaddedInt8 = beachMap.GetUnpaddedInt((int)((float)num2 * num6 + num6), (int)((float)num3 * num6 + num6));
		float blockLayerTransitionSize = blockLayerConfig.blockLayerTransitionSize;
		BlockPos blockPos = new BlockPos();
		for (int i = 0; i < 32; i++)
		{
			for (int j = 0; j < 32; j++)
			{
				blockPos.Set(chunkX * 32 + i, 1, chunkZ * 32 + j);
				double distx;
				double distz;
				int num7 = RandomlyAdjustPosition(blockPos, out distx, out distz);
				double posRand = ((double)GameMath.MurmurHash3(blockPos.X, 1, blockPos.Z) / 2147483647.0 + 1.0) * (double)blockLayerTransitionSize;
				int num8 = rainHeightMap[j * 32 + i];
				if (num8 >= mapheight)
				{
					continue;
				}
				int unpaddedColorLerped = climateMap.GetUnpaddedColorLerped((float)num2 * num4 + num4 * ((float)i + (float)distx) / 32f, (float)num3 * num4 + num4 * ((float)j + (float)distz) / 32f);
				int unscaledTemp = (unpaddedColorLerped >> 16) & 0xFF;
				float scaledAdjustedTemperatureFloat = Climate.GetScaledAdjustedTemperatureFloat(unscaledTemp, num8 - TerraGenConfig.seaLevel + num7);
				float tempRel = (float)Climate.GetAdjustedTemperature(unscaledTemp, num8 - TerraGenConfig.seaLevel + num7) / 255f;
				float num9 = (float)Climate.GetRainFall((unpaddedColorLerped >> 8) & 0xFF, num8 + num7) / 255f;
				float forestRel = GameMath.BiLerp(unpaddedInt, unpaddedInt2, unpaddedInt3, unpaddedInt4, (float)i / 32f, (float)j / 32f) / 255f;
				int num10 = num8;
				int num11 = chunks[0].MapChunk.WorldGenTerrainHeightMap[j * 32 + i];
				int num12 = num11 / 32;
				int num13 = num11 % 32;
				int index3d = (32 * num13 + j) * 32 + i;
				int blockIdUnsafe = chunks[num12].Data.GetBlockIdUnsafe(index3d);
				Block block = api.World.Blocks[blockIdUnsafe];
				if (block.BlockMaterial != EnumBlockMaterial.Stone && block.BlockMaterial != EnumBlockMaterial.Liquid)
				{
					continue;
				}
				if (num11 < TerraGenConfig.seaLevel)
				{
					int num14 = (int)Math.Min(Math.Max(0f, (0.5f - num9) * 40f), TerraGenConfig.seaLevel - num11);
					int val = chunks[0].MapChunk.WorldGenTerrainHeightMap[j * 32 + i];
					chunks[0].MapChunk.WorldGenTerrainHeightMap[j * 32 + i] = (ushort)Math.Max(num11 + num14 - 1, val);
					while (num14-- > 0)
					{
						num12 = num11 / 32;
						num13 = num11 % 32;
						index3d = (32 * num13 + j) * 32 + i;
						IChunkBlocks data = chunks[num12].Data;
						data.SetBlockUnsafe(index3d, blockIdUnsafe);
						data.SetFluid(index3d, 0);
						num11++;
					}
				}
				blockPos.Y = num8;
				int posyoffs = (int)(distort2dx.Noise(-blockPos.X, -blockPos.Z) / 4.0);
				num8 = PutLayers(posRand, i, j, posyoffs, blockPos, chunks, num9, scaledAdjustedTemperatureFloat, unscaledTemp, rainHeightMap);
				if (num10 == TerraGenConfig.seaLevel - 1)
				{
					float beachRel = GameMath.BiLerp(unpaddedInt5, unpaddedInt6, unpaddedInt7, unpaddedInt8, (float)i / 32f, (float)j / 32f) / 255f;
					GenBeach(i, num10, j, chunks, num9, scaledAdjustedTemperatureFloat, beachRel, blockIdUnsafe);
				}
				PlaceTallGrass(i, num10, j, chunks, num9, tempRel, scaledAdjustedTemperatureFloat, forestRel);
				int num15 = 0;
				while (num8 >= TerraGenConfig.seaLevel - 1)
				{
					num12 = num8 / 32;
					num13 = num8 % 32;
					index3d = (32 * num13 + j) * 32 + i;
					if (chunks[num12].Data.GetBlockIdUnsafe(index3d) == 0)
					{
						num15++;
					}
					else
					{
						if (num15 >= 8)
						{
							break;
						}
						num15 = 0;
					}
					num8--;
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

	private int PutLayers(double posRand, int lx, int lz, int posyoffs, BlockPos pos, IServerChunk[] chunks, float rainRel, float temp, int unscaledTemp, ushort[] heightMap)
	{
		int num = 0;
		int num2 = 0;
		bool flag = false;
		bool isOcean = false;
		bool flag2 = false;
		bool flag3 = true;
		int y = pos.Y;
		while (pos.Y > 0)
		{
			int num3 = pos.Y / 32;
			int num4 = pos.Y % 32;
			int index3d = (32 * num4 + lz) * 32 + lx;
			int num5 = chunks[num3].Data.GetBlockIdUnsafe(index3d);
			if (num5 == 0)
			{
				num5 = chunks[num3].Data.GetFluid(index3d);
			}
			pos.Y--;
			if (num5 != 0)
			{
				if (num5 == GlobalConfig.saltWaterBlockId)
				{
					isOcean = true;
					flag = true;
					continue;
				}
				if (num5 == GlobalConfig.waterBlockId || num5 == boilingWaterBlockId)
				{
					flag = true;
					continue;
				}
				if (num5 == GlobalConfig.lakeIceBlockId)
				{
					flag2 = true;
					continue;
				}
				if (heightMap != null && flag3)
				{
					chunks[0].MapChunk.TopRockIdMap[lz * 32 + lx] = num5;
					if (flag2)
					{
						break;
					}
					LoadBlockLayers(posRand, rainRel, temp, unscaledTemp, y + posyoffs, pos, num5, isOcean);
					flag3 = false;
					if (!flag)
					{
						heightMap[lz * 32 + lx] = (ushort)(pos.Y + 1);
					}
				}
				if (num >= BlockLayersIds.Count || (flag && num2 >= layersUnderWater.Length))
				{
					return pos.Y;
				}
				IChunkBlocks data = chunks[num3].Data;
				data.SetBlockUnsafe(index3d, flag ? layersUnderWater[num2++] : BlockLayersIds[num++]);
				data.SetFluid(index3d, 0);
			}
			else if ((num > 0 && temp > -18f) || num2 > 0)
			{
				return pos.Y;
			}
		}
		return pos.Y;
	}

	private void GenBeach(int x, int posY, int z, IServerChunk[] chunks, float rainRel, float temp, float beachRel, int topRockId)
	{
		int value = blockLayerConfig.BeachLayer.BlockId;
		if (blockLayerConfig.BeachLayer.BlockIdMapping != null && !blockLayerConfig.BeachLayer.BlockIdMapping.TryGetValue(topRockId, out value))
		{
			return;
		}
		int index3d = (32 * (posY % 32) + z) * 32 + x;
		if (!((double)beachRel > 0.5))
		{
			return;
		}
		IChunkBlocks data = chunks[posY / 32].Data;
		if (data.GetBlockIdUnsafe(index3d) == 0)
		{
			return;
		}
		int fluid = data.GetFluid(index3d);
		if (fluid != GlobalConfig.waterBlockId && fluid != GlobalConfig.lakeIceBlockId)
		{
			data.SetBlockUnsafe(index3d, value);
			if (fluid != 0)
			{
				data.SetFluid(index3d, 0);
			}
		}
	}

	private void PlaceTallGrass(int x, int posY, int z, IServerChunk[] chunks, float rainRel, float tempRel, float temp, float forestRel)
	{
		double num = (double)blockLayerConfig.Tallgrass.RndWeight * rnd.NextDouble() + (double)blockLayerConfig.Tallgrass.PerlinWeight * grassDensity.Noise(x, z, -0.5);
		double num2 = Math.Max(0.0, (double)(rainRel * tempRel) - 0.25);
		if (num <= GameMath.Clamp((double)forestRel - num2, 0.05, 0.99) || posY >= mapheight - 1 || posY < 1)
		{
			return;
		}
		int index = chunks[posY / 32].Data[(32 * (posY % 32) + z) * 32 + x];
		if (api.World.Blocks[index].Fertility <= rnd.NextInt(100))
		{
			return;
		}
		double num3 = Math.Max(0.0, grassHeight.Noise(x, z) * (double)blockLayerConfig.Tallgrass.BlockCodeByMin.Length - 1.0);
		for (int i = (int)num3 + ((rnd.NextDouble() < num3) ? 1 : 0); i < blockLayerConfig.Tallgrass.BlockCodeByMin.Length; i++)
		{
			TallGrassBlockCodeByMin tallGrassBlockCodeByMin = blockLayerConfig.Tallgrass.BlockCodeByMin[i];
			if (forestRel <= tallGrassBlockCodeByMin.MaxForest && rainRel >= tallGrassBlockCodeByMin.MinRain && temp >= (float)tallGrassBlockCodeByMin.MinTemp)
			{
				chunks[(posY + 1) / 32].Data[(32 * ((posY + 1) % 32) + z) * 32 + x] = tallGrassBlockCodeByMin.BlockId;
				break;
			}
		}
	}

	private void LoadBlockLayers(double posRand, float rainRel, float temperature, int unscaledTemp, int posY, BlockPos pos, int firstBlockId, bool isOcean)
	{
		float posYRel = ((float)posY - (float)TerraGenConfig.seaLevel) / ((float)mapheight - (float)TerraGenConfig.seaLevel);
		float fertilityRel = (float)Climate.GetFertilityFromUnscaledTemp((int)(rainRel * 255f), unscaledTemp, posYRel) / 255f;
		float num = TerraGenConfig.SoilThickness(rainRel, temperature, posY - TerraGenConfig.seaLevel, 1f);
		int num2 = (int)num;
		if (num - (float)num2 > rnd.NextFloat())
		{
			num2++;
		}
		if (temperature < -16f)
		{
			num2 += 10;
		}
		BlockLayersIds.Clear();
		for (int i = 0; i < blockLayerConfig.Blocklayers.Length; i++)
		{
			BlockLayer blockLayer = blockLayerConfig.Blocklayers[i];
			float num3 = blockLayer.CalcYDistance(posY, mapheight);
			float num4 = blockLayer.CalcTrfDistance(temperature, rainRel, fertilityRel);
			if ((double)(num4 + num3) <= posRand)
			{
				int blockId = blockLayer.GetBlockId(posRand, temperature, rainRel, fertilityRel, firstBlockId, pos, mapheight);
				if (blockId != 0)
				{
					BlockLayersIds.Add(blockId);
					if (blockLayer.Thickness > 1)
					{
						for (int j = 1; (float)j < (float)blockLayer.Thickness * (1f - num4 * num3); j++)
						{
							BlockLayersIds.Add(blockId);
							num3 = Math.Abs((float)posY-- / (float)mapheight - GameMath.Min((float)posY-- / (float)mapheight, blockLayer.MaxY));
						}
					}
					posY--;
					temperature = Climate.GetScaledAdjustedTemperatureFloat(unscaledTemp, posY - TerraGenConfig.seaLevel);
					posYRel = ((float)posY - (float)TerraGenConfig.seaLevel) / ((float)api.WorldManager.MapSizeY - (float)TerraGenConfig.seaLevel);
					fertilityRel = (float)Climate.GetFertilityFromUnscaledTemp((int)(rainRel * 255f), unscaledTemp, posYRel) / 255f;
				}
			}
			if (BlockLayersIds.Count >= num2)
			{
				break;
			}
		}
		int num5 = ((!isOcean) ? blockLayerConfig.LakeBedLayer.GetSuitable(temperature, rainRel, (float)pos.Y / (float)api.WorldManager.MapSizeY, rnd, firstBlockId) : blockLayerConfig.OceanBedLayer.GetSuitable(temperature, rainRel, (float)pos.Y / (float)api.WorldManager.MapSizeY, rnd, firstBlockId));
		if (num5 == 0)
		{
			layersUnderWater = Array.Empty<int>();
			return;
		}
		layersUnderWaterTmp[0] = num5;
		layersUnderWater = layersUnderWaterTmp;
	}
}
