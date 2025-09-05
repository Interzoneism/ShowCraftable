using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenRivulets : ModStdWorldGen
{
	private ICoreServerAPI api;

	private LCGRandom rnd;

	private IWorldGenBlockAccessor blockAccessor;

	private int regionsize;

	private int chunkMapSizeY;

	private BlockPos chunkBase = new BlockPos();

	private BlockPos chunkend = new BlockPos();

	private List<Cuboidi> structuresIntersectingChunk = new List<Cuboidi>();

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.9;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(initWorldGen, "standard");
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Vegetation, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		blockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: true);
		regionsize = blockAccessor.RegionSize;
	}

	private void initWorldGen()
	{
		LoadGlobalConfig(api);
		rnd = new LCGRandom(api.WorldManager.Seed);
		chunkMapSizeY = api.WorldManager.MapSizeY / 32;
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		blockAccessor.BeginColumn();
		IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
		int num = api.WorldManager.RegionSize / 32;
		float num2 = (float)climateMap.InnerSize / (float)num;
		int num3 = chunkX % num;
		int num4 = chunkZ % num;
		int unpaddedInt = climateMap.GetUnpaddedInt((int)((float)num3 * num2), (int)((float)num4 * num2));
		int unpaddedInt2 = climateMap.GetUnpaddedInt((int)((float)num3 * num2 + num2), (int)((float)num4 * num2));
		int unpaddedInt3 = climateMap.GetUnpaddedInt((int)((float)num3 * num2), (int)((float)num4 * num2 + num2));
		int unpaddedInt4 = climateMap.GetUnpaddedInt((int)((float)num3 * num2 + num2), (int)((float)num4 * num2 + num2));
		int num5 = GameMath.BiLerpRgbColor(0.5f, 0.5f, unpaddedInt, unpaddedInt2, unpaddedInt3, unpaddedInt4);
		structuresIntersectingChunk.Clear();
		api.World.BlockAccessor.WalkStructures(chunkBase.Set(chunkX * 32, 0, chunkZ * 32), chunkend.Set(chunkX * 32 + 32, chunkMapSizeY * 32, chunkZ * 32 + 32), delegate(GeneratedStructure struc)
		{
			if (struc.SuppressRivulets)
			{
				structuresIntersectingChunk.Add(struc.Location.Clone().GrowBy(1, 1, 1));
			}
		});
		int num6 = (num5 >> 8) & 0xFF;
		int num7 = num5 & 0xFF;
		int num8 = (num5 >> 16) & 0xFF;
		int geologicActivity = getGeologicActivity(chunkX * 32 + 16, chunkZ * 32 + 16);
		float num9 = (float)getGeologicActivity(chunkX * 32 + 16, chunkZ * 32 + 16) / 2f * (float)api.World.BlockAccessor.MapSizeY / 256f;
		int num10 = 2 * ((int)((float)(160 * (num6 + num7)) / 255f) * (api.WorldManager.MapSizeY / 32) - Math.Max(0, 100 - num8));
		int num11 = (int)((float)(500 * geologicActivity) / 255f * (float)(api.WorldManager.MapSizeY / 32));
		float scaledAdjustedTemperatureFloat = Climate.GetScaledAdjustedTemperatureFloat(num8, 0);
		rnd.InitPositionSeed(chunkX, chunkZ);
		if (scaledAdjustedTemperatureFloat >= -15f)
		{
			while (num10-- > 0)
			{
				tryGenRivulet(chunks, chunkX, chunkZ, num9, lava: false);
			}
		}
		while (num11-- > 0)
		{
			tryGenRivulet(chunks, chunkX, chunkZ, num9 + 10f, lava: true);
		}
	}

	private void tryGenRivulet(IServerChunk[] chunks, int chunkX, int chunkZ, float geoActivityYThreshold, bool lava)
	{
		IMapChunk mapChunk = chunks[0].MapChunk;
		int num = (int)((float)TerraGenConfig.seaLevel * 1.1f);
		int max = api.WorldManager.MapSizeY - num;
		int num2 = 1 + rnd.NextInt(30);
		int num3 = Math.Min(1 + rnd.NextInt(num) + rnd.NextInt(max) * rnd.NextInt(max), api.WorldManager.MapSizeY - 2);
		int num4 = 1 + rnd.NextInt(30);
		ushort num5 = mapChunk.WorldGenTerrainHeightMap[num4 * 32 + num2];
		if ((num3 > num5 && rnd.NextInt(2) == 0) || ((float)num3 < geoActivityYThreshold && !lava) || ((float)num3 > geoActivityYThreshold && lava))
		{
			return;
		}
		int num6 = 0;
		int num7 = 0;
		for (int i = 0; i < 6; i++)
		{
			BlockFacing blockFacing = BlockFacing.ALLFACES[i];
			int num8 = num2 + blockFacing.Normali.X;
			int num9 = num3 + blockFacing.Normali.Y;
			int num10 = num4 + blockFacing.Normali.Z;
			Block block = api.World.Blocks[chunks[num9 / 32].Data.GetBlockIdUnsafe((32 * (num9 % 32) + num10) * 32 + num8)];
			bool flag = block.BlockMaterial == EnumBlockMaterial.Stone;
			num6 += (flag ? 1 : 0);
			num7 += ((block.BlockMaterial == EnumBlockMaterial.Air) ? 1 : 0);
			if (flag)
			{
				continue;
			}
			if (blockFacing == BlockFacing.UP)
			{
				num6 = 0;
			}
			else if (blockFacing == BlockFacing.DOWN)
			{
				num9 = num3 + 1;
				block = api.World.Blocks[chunks[num9 / 32].Data.GetBlockIdUnsafe((32 * (num9 % 32) + num10) * 32 + num8)];
				if (block.BlockMaterial != EnumBlockMaterial.Stone)
				{
					num6 = 0;
				}
			}
		}
		if (num6 != 5 || num7 != 1)
		{
			return;
		}
		BlockPos blockPos = new BlockPos(chunkX * 32 + num2, num3, chunkZ * 32 + num4);
		for (int j = 0; j < structuresIntersectingChunk.Count; j++)
		{
			if (structuresIntersectingChunk[j].Contains(blockPos))
			{
				return;
			}
		}
		if (GetIntersectingStructure(blockPos, ModStdWorldGen.SkipRivuletsgHashCode) == null)
		{
			IServerChunk serverChunk = chunks[num3 / 32];
			int num11 = (32 * (num3 % 32) + num4) * 32 + num2;
			if (api.World.GetBlock(serverChunk.Data.GetBlockId(num11, 1)).EntityClass != null)
			{
				serverChunk.RemoveBlockEntity(blockPos);
			}
			serverChunk.Data.SetBlockAir(num11);
			serverChunk.Data.SetFluid(num11, ((float)num3 < geoActivityYThreshold) ? GlobalConfig.lavaBlockId : GlobalConfig.waterBlockId);
			blockAccessor.ScheduleBlockUpdate(blockPos);
		}
	}

	private int getGeologicActivity(int posx, int posz)
	{
		IntDataMap2D intDataMap2D = blockAccessor.GetMapRegion(posx / regionsize, posz / regionsize)?.ClimateMap;
		if (intDataMap2D == null)
		{
			return 0;
		}
		int num = regionsize / 32;
		float num2 = (float)intDataMap2D.InnerSize / (float)num;
		int num3 = posx / 32 % num;
		int num4 = posz / 32 % num;
		return intDataMap2D.GetUnpaddedInt((int)((float)num3 * num2), (int)((float)num4 * num2)) & 0xFF;
	}
}
