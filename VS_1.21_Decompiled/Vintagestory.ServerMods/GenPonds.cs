using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenPonds : ModStdWorldGen
{
	private ICoreServerAPI api;

	private LCGRandom rand;

	private int mapheight;

	private IWorldGenBlockAccessor blockAccessor;

	private readonly QueueOfInt searchPositionsDeltas = new QueueOfInt();

	private readonly QueueOfInt pondPositions = new QueueOfInt();

	private int searchSize;

	private int mapOffset;

	private int minBoundary;

	private int maxBoundary;

	private int climateUpLeft;

	private int climateUpRight;

	private int climateBotLeft;

	private int climateBotRight;

	private int[] didCheckPosition;

	private int iteration;

	private LakeBedLayerProperties lakebedLayerConfig;

	public override double ExecuteOrder()
	{
		return 0.4;
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(initWorldGen, "standard");
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.TerrainFeatures, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		blockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: true);
	}

	public void initWorldGen()
	{
		LoadGlobalConfig(api);
		rand = new LCGRandom(api.WorldManager.Seed - 12);
		searchSize = 96;
		mapOffset = 32;
		minBoundary = -31;
		maxBoundary = 63;
		mapheight = api.WorldManager.MapSizeY;
		didCheckPosition = new int[searchSize * searchSize];
		BlockLayerConfig instance = BlockLayerConfig.GetInstance(api);
		lakebedLayerConfig = instance.LakeBedLayer;
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		if (GetIntersectingStructure(chunkX * 32 + 16, chunkZ * 32 + 16, ModStdWorldGen.SkipPondgHashCode) != null)
		{
			return;
		}
		blockAccessor.BeginColumn();
		LCGRandom lCGRandom = rand;
		lCGRandom.InitPositionSeed(chunkX, chunkZ);
		int num = mapheight - 1;
		ushort[] rainHeightMap = chunks[0].MapChunk.RainHeightMap;
		IMapChunk mapChunk = chunks[0].MapChunk;
		IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
		int num2 = api.WorldManager.RegionSize / 32;
		float num3 = (float)climateMap.InnerSize / (float)num2;
		int num4 = chunkX % num2;
		int num5 = chunkZ % num2;
		climateUpLeft = climateMap.GetUnpaddedInt((int)((float)num4 * num3), (int)((float)num5 * num3));
		climateUpRight = climateMap.GetUnpaddedInt((int)((float)num4 * num3 + num3), (int)((float)num5 * num3));
		climateBotLeft = climateMap.GetUnpaddedInt((int)((float)num4 * num3), (int)((float)num5 * num3 + num3));
		climateBotRight = climateMap.GetUnpaddedInt((int)((float)num4 * num3 + num3), (int)((float)num5 * num3 + num3));
		int num6 = GameMath.BiLerpRgbColor(0.5f, 0.5f, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
		int num7 = (num6 >> 8) & 0xFF;
		int unscaledTemp = (num6 >> 16) & 0xFF;
		float num8 = Math.Max(0f, (float)(4 * (num7 - 10)) / 255f);
		float scaledAdjustedTemperatureFloat = Climate.GetScaledAdjustedTemperatureFloat(unscaledTemp, 0);
		float num9 = (num8 - Math.Max(0f, 5f - scaledAdjustedTemperatureFloat)) * 10f;
		while (num9-- > 0f && (!(num9 < 1f) || !(lCGRandom.NextFloat() > num9)))
		{
			int num10 = lCGRandom.NextInt(32);
			int num11 = lCGRandom.NextInt(32);
			int num12 = rainHeightMap[num11 * 32 + num10] + 1;
			if (num12 <= 0 || num12 >= num)
			{
				return;
			}
			TryPlacePondAt(num10, num12, num11, chunkX, chunkZ);
		}
		int num13 = 600;
		while (num13-- > 0)
		{
			int num10 = lCGRandom.NextInt(32);
			int num11 = lCGRandom.NextInt(32);
			int num12 = (int)(lCGRandom.NextFloat() * (float)(mapChunk.WorldGenTerrainHeightMap[num11 * 32 + num10] - 1));
			if (num12 <= 0 || num12 >= num)
			{
				break;
			}
			int num14 = num12 / 32;
			int num15 = num12 % 32;
			int blockIdUnsafe = chunks[num14].Data.GetBlockIdUnsafe((num15 * 32 + num11) * 32 + num10);
			while (blockIdUnsafe == 0 && num12 > 20)
			{
				num12--;
				num14 = num12 / 32;
				num15 = num12 % 32;
				blockIdUnsafe = chunks[num14].Data.GetBlockIdUnsafe((num15 * 32 + num11) * 32 + num10);
				if (blockIdUnsafe != 0)
				{
					TryPlacePondAt(num10, num12, num11, chunkX, chunkZ);
				}
			}
		}
	}

	public void TryPlacePondAt(int dx, int pondYPos, int dz, int chunkX, int chunkZ, int depth = 0)
	{
		int num = mapOffset;
		int num2 = searchSize;
		int num3 = minBoundary;
		int num4 = maxBoundary;
		int waterBlockId = GlobalConfig.waterBlockId;
		searchPositionsDeltas.Clear();
		pondPositions.Clear();
		int num5 = chunkX * 32;
		int num6 = chunkZ * 32;
		Vec2i vec2i = new Vec2i();
		int num7 = (dz + num) * num2 + dx + num;
		searchPositionsDeltas.Enqueue(num7);
		pondPositions.Enqueue(num7);
		int num8 = ++iteration;
		didCheckPosition[num7] = num8;
		BlockPos blockPos = new BlockPos();
		while (searchPositionsDeltas.Count > 0)
		{
			int num9 = searchPositionsDeltas.Dequeue();
			int num10 = num9 % num2 - num;
			int num11 = num9 / num2 - num;
			BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
			foreach (BlockFacing blockFacing in hORIZONTALS)
			{
				Vec3i normali = blockFacing.Normali;
				int num12 = num10 + normali.X;
				int num13 = num11 + normali.Z;
				num7 = (num13 + num) * num2 + num12 + num;
				if (didCheckPosition[num7] != num8)
				{
					didCheckPosition[num7] = num8;
					vec2i.Set(num5 + num12, num6 + num13);
					blockPos.Set(vec2i.X, pondYPos - 1, vec2i.Y);
					Block block = blockAccessor.GetBlock(blockPos);
					if (num12 <= num3 || num13 <= num3 || num12 >= num4 || num13 >= num4 || (!((double)block.GetLiquidBarrierHeightOnSide(BlockFacing.UP, blockPos) >= 1.0) && block.BlockId != waterBlockId))
					{
						pondPositions.Clear();
						searchPositionsDeltas.Clear();
						return;
					}
					blockPos.Set(vec2i.X, pondYPos, vec2i.Y);
					if ((double)blockAccessor.GetBlock(blockPos).GetLiquidBarrierHeightOnSide(blockFacing.Opposite, blockPos) < 0.9)
					{
						searchPositionsDeltas.Enqueue(num7);
						pondPositions.Enqueue(num7);
					}
				}
			}
		}
		int num14 = -1;
		int num15 = -1;
		int num16 = api.WorldManager.RegionSize / 32;
		IMapChunk mapChunk = null;
		IServerChunk serverChunk = null;
		IServerChunk serverChunk2 = null;
		int num17 = GameMath.Mod(pondYPos, 32);
		bool flag = rand.NextFloat() > 0.5f;
		bool flag2 = flag || pondPositions.Count > 16;
		while (pondPositions.Count > 0)
		{
			int num18 = pondPositions.Dequeue();
			int num19 = num18 % num2 - num + num5;
			int num20 = num18 / num2 - num + num6;
			int num21 = num19 / 32;
			int num22 = num20 / 32;
			int num23 = GameMath.Mod(num19, 32);
			int num24 = GameMath.Mod(num20, 32);
			if (num21 != num14 || num22 != num15)
			{
				serverChunk = (IServerChunk)blockAccessor.GetChunk(num21, pondYPos / 32, num22);
				if (serverChunk == null)
				{
					serverChunk = api.WorldManager.GetChunk(num21, pondYPos / 32, num22);
				}
				serverChunk.Unpack();
				if (num17 == 0)
				{
					serverChunk2 = (IServerChunk)blockAccessor.GetChunk(num21, (pondYPos - 1) / 32, num22);
					if (serverChunk2 == null)
					{
						return;
					}
					serverChunk2.Unpack();
				}
				else
				{
					serverChunk2 = serverChunk;
				}
				mapChunk = serverChunk.MapChunk;
				IntDataMap2D climateMap = mapChunk.MapRegion.ClimateMap;
				float num25 = (float)climateMap.InnerSize / (float)num16;
				int num26 = num21 % num16;
				int num27 = num22 % num16;
				climateUpLeft = climateMap.GetUnpaddedInt((int)((float)num26 * num25), (int)((float)num27 * num25));
				climateUpRight = climateMap.GetUnpaddedInt((int)((float)num26 * num25 + num25), (int)((float)num27 * num25));
				climateBotLeft = climateMap.GetUnpaddedInt((int)((float)num26 * num25), (int)((float)num27 * num25 + num25));
				climateBotRight = climateMap.GetUnpaddedInt((int)((float)num26 * num25 + num25), (int)((float)num27 * num25 + num25));
				num14 = num21;
				num15 = num22;
				serverChunk2.MarkModified();
				serverChunk.MarkModified();
			}
			if (mapChunk.RainHeightMap[num24 * 32 + num23] < pondYPos)
			{
				mapChunk.RainHeightMap[num24 * 32 + num23] = (ushort)pondYPos;
			}
			int num28 = GameMath.BiLerpRgbColor((float)num23 / 32f, (float)num24 / 32f, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
			float scaledAdjustedTemperatureFloat = Climate.GetScaledAdjustedTemperatureFloat((num28 >> 16) & 0xFF, pondYPos - TerraGenConfig.seaLevel);
			int num29 = (num17 * 32 + num24) * 32 + num23;
			Block block2 = api.World.GetBlock(serverChunk.Data.GetBlockId(num29, 1));
			if (block2.BlockMaterial == EnumBlockMaterial.Plant)
			{
				serverChunk.Data.SetBlockAir(num29);
				if (block2.EntityClass != null)
				{
					blockPos.Set(num21 * 32 + num23, pondYPos, num22 * 32 + num24);
					serverChunk.RemoveBlockEntity(blockPos);
				}
			}
			serverChunk.Data.SetFluid(num29, (scaledAdjustedTemperatureFloat < -5f) ? GlobalConfig.lakeIceBlockId : waterBlockId);
			if (!flag2)
			{
				continue;
			}
			int index3d = ((num17 == 0) ? ((992 + num24) * 32 + num23) : (((num17 - 1) * 32 + num24) * 32 + num23));
			if (api.World.Blocks[serverChunk2.Data.GetFluid(index3d)].IsLiquid())
			{
				continue;
			}
			float rainRel = (float)Climate.GetRainFall((num28 >> 8) & 0xFF, pondYPos) / 255f;
			int num30 = mapChunk.TopRockIdMap[num24 * 32 + num23];
			if (num30 != 0)
			{
				int suitable = lakebedLayerConfig.GetSuitable(scaledAdjustedTemperatureFloat, rainRel, (float)pondYPos / (float)mapheight, rand, num30);
				if (suitable != 0)
				{
					serverChunk2.Data[index3d] = suitable;
				}
			}
		}
		if (flag)
		{
			TryPlacePondAt(dx, pondYPos + 1, dz, chunkX, chunkZ, depth + 1);
		}
	}
}
