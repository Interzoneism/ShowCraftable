using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenSnowLayer : ModStdWorldGen
{
	private ICoreServerAPI api;

	private Random rnd;

	private int worldheight;

	private IWorldGenBlockAccessor blockAccessor;

	private BlockLayerConfig blockLayerConfig;

	private int transSize;

	private int maxTemp;

	private int minTemp;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.0;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(initWorldGen, "standard");
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.NeighbourSunLightFlood, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
		}
	}

	private void initWorldGen()
	{
		LoadGlobalConfig(api);
		IAsset asset = api.Assets.Get("worldgen/blocklayers.json");
		blockLayerConfig = asset.ToObject<BlockLayerConfig>();
		blockLayerConfig.SnowLayer.BlockId = api.WorldManager.GetBlockId(blockLayerConfig.SnowLayer.BlockCode);
		rnd = new Random(api.WorldManager.Seed);
		worldheight = api.WorldManager.MapSizeY;
		transSize = blockLayerConfig.SnowLayer.TransitionSize;
		maxTemp = blockLayerConfig.SnowLayer.MaxTemp;
		minTemp = maxTemp - transSize;
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		blockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: true);
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		blockAccessor.BeginColumn();
		IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
		ushort[] rainHeightMap = chunks[0].MapChunk.RainHeightMap;
		int num = api.WorldManager.RegionSize / 32;
		int num2 = chunkX % num;
		int num3 = chunkZ % num;
		float num4 = (float)climateMap.InnerSize / (float)num;
		int unpaddedInt = climateMap.GetUnpaddedInt((int)((float)num2 * num4), (int)((float)num3 * num4));
		int unpaddedInt2 = climateMap.GetUnpaddedInt((int)((float)num2 * num4 + num4), (int)((float)num3 * num4));
		int unpaddedInt3 = climateMap.GetUnpaddedInt((int)((float)num2 * num4), (int)((float)num3 * num4 + num4));
		int unpaddedInt4 = climateMap.GetUnpaddedInt((int)((float)num2 * num4 + num4), (int)((float)num3 * num4 + num4));
		for (int i = 0; i < 32; i++)
		{
			for (int j = 0; j < 32; j++)
			{
				int num5 = rainHeightMap[j * 32 + i];
				float scaledAdjustedTemperatureFloat = Climate.GetScaledAdjustedTemperatureFloat((GameMath.BiLerpRgbColor((float)i / 32f, (float)j / 32f, unpaddedInt, unpaddedInt2, unpaddedInt3, unpaddedInt4) >> 16) & 0xFF, num5 - TerraGenConfig.seaLevel);
				int posY = num5;
				if (PlaceSnowLayer(i, posY, j, chunks, scaledAdjustedTemperatureFloat))
				{
					rainHeightMap[j * 32 + i]++;
				}
			}
		}
	}

	private bool PlaceSnowLayer(int lx, int posY, int lz, IServerChunk[] chunks, float temp)
	{
		float num = temp - (float)minTemp;
		if (temp > (float)maxTemp)
		{
			return false;
		}
		if ((double)num > rnd.NextDouble() * (double)transSize)
		{
			return false;
		}
		while (posY < worldheight - 1 && chunks[(posY + 1) / 32].Data.GetBlockIdUnsafe((32 * ((posY + 1) % 32) + lz) * 32 + lx) != 0)
		{
			posY++;
		}
		if (posY >= worldheight - 1)
		{
			return false;
		}
		int index3d = (32 * (posY % 32) + lz) * 32 + lx;
		IServerChunk serverChunk = chunks[posY / 32];
		int num2 = serverChunk.Data.GetFluid(index3d);
		if (num2 == 0)
		{
			num2 = serverChunk.Data.GetBlockIdUnsafe(index3d);
		}
		if (api.World.Blocks[num2].SideSolid[BlockFacing.UP.Index])
		{
			chunks[(posY + 1) / 32].Data[(32 * ((posY + 1) % 32) + lz) * 32 + lx] = blockLayerConfig.SnowLayer.BlockId;
			return true;
		}
		return false;
	}
}
