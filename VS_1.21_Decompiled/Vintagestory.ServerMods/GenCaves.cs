using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenCaves : GenPartial
{
	internal LCGRandom caveRand;

	private IWorldGenBlockAccessor worldgenBlockAccessor;

	private NormalizedSimplexNoise basaltNoise;

	private NormalizedSimplexNoise heightvarNoise;

	private int regionsize;

	protected override int chunkRange => 5;

	public override double ExecuteOrder()
	{
		return 0.3;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.ChunkColumnGeneration(GenChunkColumn, EnumWorldGenPass.Terrain, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
			api.ChatCommands.GetOrCreate("dev").BeginSubCommand("gencaves").WithDescription("Cave generator test tool. Deletes all chunks in the area and generates inverse caves around the world middle")
				.RequiresPrivilege(Privilege.controlserver)
				.HandleWith(CmdCaveGenTest)
				.EndSubCommand();
			api.Event.MapChunkGeneration(OnMapChunkGen, "standard");
			api.Event.MapChunkGeneration(OnMapChunkGen, "superflat");
			api.Event.InitWorldGenerator(initWorldGen, "superflat");
		}
	}

	public override void initWorldGen()
	{
		base.initWorldGen();
		caveRand = new LCGRandom(api.WorldManager.Seed + 123128);
		basaltNoise = NormalizedSimplexNoise.FromDefaultOctaves(2, 0.2857142984867096, 0.8999999761581421, api.World.Seed + 12);
		heightvarNoise = NormalizedSimplexNoise.FromDefaultOctaves(3, 0.05000000074505806, 0.8999999761581421, api.World.Seed + 12);
		regionsize = api.World.BlockAccessor.RegionSize;
	}

	private void OnMapChunkGen(IMapChunk mapChunk, int chunkX, int chunkZ)
	{
		mapChunk.CaveHeightDistort = new byte[1024];
		for (int i = 0; i < 32; i++)
		{
			for (int j = 0; j < 32; j++)
			{
				double num = heightvarNoise.Noise(32 * chunkX + i, 32 * chunkZ + j) - 0.5;
				num = ((num > 0.0) ? Math.Max(0.0, num - 0.07) : Math.Min(0.0, num + 0.07));
				mapChunk.CaveHeightDistort[j * 32 + i] = (byte)(128.0 * num + 127.0);
			}
		}
	}

	private TextCommandResult CmdCaveGenTest(TextCommandCallingArgs args)
	{
		caveRand = new LCGRandom(api.WorldManager.Seed + 123128);
		initWorldGen();
		airBlockId = api.World.GetBlock(new AssetLocation("rock-granite")).BlockId;
		int num = api.World.BlockAccessor.MapSizeX / 2 / 32;
		int num2 = api.World.BlockAccessor.MapSizeZ / 2 / 32;
		for (int i = -5; i <= 5; i++)
		{
			for (int j = -5; j <= 5; j++)
			{
				int chunkX = num + i;
				int chunkZ = num2 + j;
				IServerChunk[] chunkColumn = GetChunkColumn(chunkX, chunkZ);
				for (int k = 0; k < chunkColumn.Length; k++)
				{
					if (chunkColumn[k] == null)
					{
						return TextCommandResult.Success("Cannot generate 10x10 area of caves, chunks are not loaded that far yet.");
					}
				}
				OnMapChunkGen(chunkColumn[0].MapChunk, chunkX, chunkZ);
			}
		}
		for (int l = -5; l <= 5; l++)
		{
			for (int m = -5; m <= 5; m++)
			{
				int num3 = num + l;
				int num4 = num2 + m;
				IServerChunk[] chunkColumn2 = GetChunkColumn(num3, num4);
				ClearChunkColumn(chunkColumn2);
				for (int n = -chunkRange; n <= chunkRange; n++)
				{
					for (int num5 = -chunkRange; num5 <= chunkRange; num5++)
					{
						chunkRand.InitPositionSeed(num3 + n, num4 + num5);
						GeneratePartial(chunkColumn2, num3, num4, n, num5);
					}
				}
				MarkDirty(num3, num4, chunkColumn2);
			}
		}
		airBlockId = 0;
		return TextCommandResult.Success("Generated and chunks force resend flags set");
	}

	private IServerChunk[] GetChunkColumn(int chunkX, int chunkZ)
	{
		int num = api.World.BlockAccessor.MapSizeY / 32;
		IServerChunk[] array = new IServerChunk[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = api.WorldManager.GetChunk(chunkX, i, chunkZ);
		}
		return array;
	}

	private void MarkDirty(int chunkX, int chunkZ, IServerChunk[] chunks)
	{
		for (int i = 0; i < chunks.Length; i++)
		{
			chunks[i].MarkModified();
			api.WorldManager.BroadcastChunk(chunkX, i, chunkZ);
		}
	}

	private bool ClearChunkColumn(IServerChunk[] chunks)
	{
		foreach (IServerChunk serverChunk in chunks)
		{
			if (serverChunk == null)
			{
				return false;
			}
			serverChunk.Unpack();
			serverChunk.Data.ClearBlocks();
			serverChunk.MarkModified();
		}
		return true;
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		worldgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: false);
	}

	public override void GeneratePartial(IServerChunk[] chunks, int chunkX, int chunkZ, int cdx, int cdz)
	{
		if (GetIntersectingStructure(chunkX * 32 + 16, chunkZ * 32 + 16, ModStdWorldGen.SkipCavesgHashCode) != null)
		{
			return;
		}
		worldgenBlockAccessor.BeginColumn();
		LCGRandom lCGRandom = chunkRand;
		int num = (((double)lCGRandom.NextInt(100) < TerraGenConfig.CavesPerChunkColumn * 100.0) ? 1 : 0);
		int max = 1024 * (worldheight - 20);
		while (num-- > 0)
		{
			int num2 = lCGRandom.NextInt(max);
			int num3 = cdx * 32 + num2 % 32;
			num2 /= 32;
			int num4 = cdz * 32 + num2 % 32;
			num2 /= 32;
			int num5 = num2 + 8;
			float horAngle = lCGRandom.NextFloat() * ((float)Math.PI * 2f);
			float vertAngle = (lCGRandom.NextFloat() - 0.5f) * 0.25f;
			float horizontalSize = lCGRandom.NextFloat() * 2f + lCGRandom.NextFloat();
			float verticalSize = 0.75f + lCGRandom.NextFloat() * 0.4f;
			num2 = lCGRandom.NextInt(500000000);
			if (num2 % 100 < 4)
			{
				horizontalSize = lCGRandom.NextFloat() * 2f + lCGRandom.NextFloat() + lCGRandom.NextFloat();
				verticalSize = 0.25f + lCGRandom.NextFloat() * 0.2f;
			}
			else if (num2 % 100 == 4)
			{
				horizontalSize = 0.75f + lCGRandom.NextFloat();
				verticalSize = lCGRandom.NextFloat() * 2f + lCGRandom.NextFloat();
			}
			num2 /= 100;
			bool extraBranchy = num5 < TerraGenConfig.seaLevel / 2 && num2 % 50 == 0;
			num2 /= 50;
			int num6 = num2 % 1000;
			num2 /= 1000;
			bool largeNearLavaLayer = num6 % 10 < 3;
			float curviness = ((num2 == 0) ? 0.035f : ((num6 < 30) ? 0.5f : 0.1f));
			int num7 = chunkRange * 32 - 16;
			num7 -= lCGRandom.NextInt(num7 / 4);
			caveRand.SetWorldSeed(lCGRandom.NextInt(10000000));
			caveRand.InitPositionSeed(chunkX + cdx, chunkZ + cdz);
			CarveTunnel(chunks, chunkX, chunkZ, num3, num5, num4, horAngle, vertAngle, horizontalSize, verticalSize, 0, num7, 0, extraBranchy, curviness, largeNearLavaLayer);
		}
	}

	private void CarveTunnel(IServerChunk[] chunks, int chunkX, int chunkZ, double posX, double posY, double posZ, float horAngle, float vertAngle, float horizontalSize, float verticalSize, int currentIteration, int maxIterations, int branchLevel, bool extraBranchy = false, float curviness = 0.1f, bool largeNearLavaLayer = false)
	{
		LCGRandom lCGRandom = caveRand;
		ushort[] worldGenTerrainHeightMap = chunks[0].MapChunk.WorldGenTerrainHeightMap;
		ushort[] rainHeightMap = chunks[0].MapChunk.RainHeightMap;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		float num8 = 0f;
		float num9 = 0f;
		float num10 = 0f;
		float num11 = 0.15f;
		float num12 = 0f;
		int num13 = (branchLevel + 1) * (extraBranchy ? 12 : 25);
		while (currentIteration++ < maxIterations)
		{
			float num14 = (float)currentIteration / (float)maxIterations;
			float num15 = 1.5f + GameMath.FastSin(num14 * (float)Math.PI) * horizontalSize + num5;
			num15 = Math.Min(num15, Math.Max(1f, num15 - num6));
			float num16 = 1.5f + GameMath.FastSin(num14 * (float)Math.PI) * (verticalSize + num6 / 4f) + num9;
			num16 = Math.Min(num16, Math.Max(0.6f, num16 - num10));
			float num17 = GameMath.FastCos(vertAngle);
			float val = GameMath.FastSin(vertAngle);
			if (largeNearLavaLayer)
			{
				float num18 = 1f + Math.Max(0f, 1f - (float)Math.Abs(posY - 12.0) / 10f);
				num15 *= num18;
				num16 *= num18;
			}
			if (num16 < 1f)
			{
				vertAngle *= 0.1f;
			}
			posX += (double)(GameMath.FastCos(horAngle) * num17);
			posY += (double)GameMath.Clamp(val, 0f - num16, num16);
			posZ += (double)(GameMath.FastSin(horAngle) * num17);
			vertAngle *= 0.8f;
			int num19 = lCGRandom.NextInt(800000);
			if (num19 / 10000 == 0)
			{
				num12 = lCGRandom.NextFloat() * lCGRandom.NextFloat() / 2f;
			}
			bool genHotSpring = false;
			int num20 = num19 % 10000;
			if ((num20 -= 30) <= 0)
			{
				horAngle = lCGRandom.NextFloat() * ((float)Math.PI * 2f);
			}
			else if ((num20 -= 76) <= 0)
			{
				horAngle += lCGRandom.NextFloat() * (float)Math.PI - (float)Math.PI / 2f;
			}
			else if ((num20 -= 60) <= 0)
			{
				num3 = lCGRandom.NextFloat() * lCGRandom.NextFloat() * 3.5f;
			}
			else if ((num20 -= 60) <= 0)
			{
				num4 = lCGRandom.NextFloat() * lCGRandom.NextFloat() * 10f;
			}
			else if ((num20 -= 50) <= 0)
			{
				if (posY < (double)(TerraGenConfig.seaLevel - 10))
				{
					num8 = lCGRandom.NextFloat() * lCGRandom.NextFloat() * 12f;
					num3 = Math.Max(num3, lCGRandom.NextFloat() * lCGRandom.NextFloat() * 3f);
				}
			}
			else if ((num20 -= 9) <= 0)
			{
				if (posY < (double)(TerraGenConfig.seaLevel - 20))
				{
					num3 = 1f + lCGRandom.NextFloat() * lCGRandom.NextFloat() * 5f;
				}
			}
			else if ((num20 -= 9) <= 0)
			{
				num7 = 2f + lCGRandom.NextFloat() * lCGRandom.NextFloat() * 7f;
			}
			else if ((num20 -= 100) <= 0 && posY < 19.0)
			{
				num7 = 2f + lCGRandom.NextFloat() * lCGRandom.NextFloat() * 5f;
				num3 = 4f + lCGRandom.NextFloat() * lCGRandom.NextFloat() * 9f;
			}
			if (posY > -5.0 && posY < 16.0 && num15 > 4f && num16 > 2f)
			{
				genHotSpring = true;
			}
			num11 = Math.Max(0.1f, num11 + num12 * 0.05f);
			num12 -= 0.02f;
			num5 = Math.Max(0f, num5 + num3 * num11);
			num3 -= 0.45f;
			num6 = Math.Max(0f, num6 + num4 * num11);
			num4 -= 0.4f;
			num9 = Math.Max(0f, num9 + num7 * num11);
			num7 -= 0.45f;
			num10 = Math.Max(0f, num10 + num8 * num11);
			num8 -= 0.4f;
			horAngle += curviness * num;
			vertAngle += curviness * num2;
			num2 = 0.9f * num2 + lCGRandom.NextFloatMinusToPlusOne() * lCGRandom.NextFloat() * 3f;
			num = 0.9f * num + lCGRandom.NextFloatMinusToPlusOne() * lCGRandom.NextFloat();
			if (num19 % 140 == 0)
			{
				num *= lCGRandom.NextFloat() * 6f;
			}
			int max = num13 + 2 * Math.Max(0, (int)posY - (TerraGenConfig.seaLevel - 20));
			if (branchLevel < 3 && (num16 > 1f || num15 > 1f) && lCGRandom.NextInt(max) == 0)
			{
				CarveTunnel(chunks, chunkX, chunkZ, posX, posY + (double)(num9 / 2f), posZ, horAngle + (lCGRandom.NextFloat() + lCGRandom.NextFloat() - 1f) + (float)Math.PI, vertAngle + (lCGRandom.NextFloat() - 0.5f) * (lCGRandom.NextFloat() - 0.5f), horizontalSize, verticalSize + num9, currentIteration, maxIterations - (int)((double)lCGRandom.NextFloat() * 0.5 * (double)maxIterations), branchLevel + 1);
			}
			if (branchLevel < 1 && num15 > 3f && posY > 60.0 && lCGRandom.NextInt(60) == 0)
			{
				CarveShaft(chunks, chunkX, chunkZ, posX, posY + (double)(num9 / 2f), posZ, horAngle + (lCGRandom.NextFloat() + lCGRandom.NextFloat() - 1f) + (float)Math.PI, -1.6707964f + 0.2f * lCGRandom.NextFloat(), Math.Min(3.5f, num15 - 1f), verticalSize + num9, currentIteration, maxIterations - (int)((double)lCGRandom.NextFloat() * 0.5 * (double)maxIterations) + (int)(posY / 5.0 * (double)(0.5f + 0.5f * lCGRandom.NextFloat())), branchLevel);
				branchLevel++;
			}
			if ((!(num15 >= 2f) || num19 % 5 != 0) && !(posX <= (double)((0f - num15) * 2f)) && !(posX >= (double)(32f + num15 * 2f)) && !(posZ <= (double)((0f - num15) * 2f)) && !(posZ >= (double)(32f + num15 * 2f)))
			{
				SetBlocks(chunks, num15, num16 + num9, posX, posY + (double)(num9 / 2f), posZ, worldGenTerrainHeightMap, rainHeightMap, chunkX, chunkZ, genHotSpring);
			}
		}
	}

	private void CarveShaft(IServerChunk[] chunks, int chunkX, int chunkZ, double posX, double posY, double posZ, float horAngle, float vertAngle, float horizontalSize, float verticalSize, int caveCurrentIteration, int maxIterations, int branchLevel)
	{
		float num = 0f;
		ushort[] worldGenTerrainHeightMap = chunks[0].MapChunk.WorldGenTerrainHeightMap;
		ushort[] rainHeightMap = chunks[0].MapChunk.RainHeightMap;
		int num2 = 0;
		while (num2++ < maxIterations)
		{
			float num3 = (float)num2 / (float)maxIterations;
			float num4 = horizontalSize * (1f - num3 * 0.33f);
			float num5 = num4 * verticalSize;
			float num6 = GameMath.FastCos(vertAngle);
			float val = GameMath.FastSin(vertAngle);
			if (num5 < 1f)
			{
				vertAngle *= 0.1f;
			}
			posX += (double)(GameMath.FastCos(horAngle) * num6);
			posY += (double)GameMath.Clamp(val, 0f - num5, num5);
			posZ += (double)(GameMath.FastSin(horAngle) * num6);
			vertAngle += 0.1f * num;
			num = 0.9f * num + (caveRand.NextFloat() - caveRand.NextFloat()) * caveRand.NextFloat() / 3f;
			if (maxIterations - num2 < 10)
			{
				int num7 = 3 + caveRand.NextInt(4);
				for (int i = 0; i < num7; i++)
				{
					CarveTunnel(chunks, chunkX, chunkZ, posX, posY, posZ, caveRand.NextFloat() * ((float)Math.PI * 2f), (caveRand.NextFloat() - 0.5f) * 0.25f, horizontalSize + 1f, verticalSize, caveCurrentIteration, maxIterations, 1);
				}
				break;
			}
			if ((caveRand.NextInt(5) != 0 || !(num4 >= 2f)) && !(posX <= (double)((0f - num4) * 2f)) && !(posX >= (double)(32f + num4 * 2f)) && !(posZ <= (double)((0f - num4) * 2f)) && !(posZ >= (double)(32f + num4 * 2f)))
			{
				SetBlocks(chunks, num4, num5, posX, posY, posZ, worldGenTerrainHeightMap, rainHeightMap, chunkX, chunkZ, genHotSpring: false);
			}
		}
	}

	private bool SetBlocks(IServerChunk[] chunks, float horRadius, float vertRadius, double centerX, double centerY, double centerZ, ushort[] terrainheightmap, ushort[] rainheightmap, int chunkX, int chunkZ, bool genHotSpring)
	{
		IMapChunk mapChunk = chunks[0].MapChunk;
		horRadius += 1f;
		vertRadius += 2f;
		int num = (int)GameMath.Clamp(centerX - (double)horRadius, 0.0, 31.0);
		int num2 = (int)GameMath.Clamp(centerX + (double)horRadius + 1.0, 0.0, 31.0);
		int num3 = (int)GameMath.Clamp(centerY - (double)(vertRadius * 0.7f), 1.0, worldheight - 1);
		int num4 = (int)GameMath.Clamp(centerY + (double)vertRadius + 1.0, 1.0, worldheight - 1);
		int num5 = (int)GameMath.Clamp(centerZ - (double)horRadius, 0.0, 31.0);
		int num6 = (int)GameMath.Clamp(centerZ + (double)horRadius + 1.0, 0.0, 31.0);
		double num7 = horRadius * horRadius;
		double num8 = vertRadius * vertRadius;
		double num9 = GameMath.Clamp((double)vertRadius / 4.0, 0.0, 0.1);
		for (int i = num; i <= num2; i++)
		{
			double num10 = ((double)i - centerX) * ((double)i - centerX) / num7;
			for (int j = num5; j <= num6; j++)
			{
				double num11 = ((double)j - centerZ) * ((double)j - centerZ) / num7;
				double num12 = (double)(mapChunk.CaveHeightDistort[j * 32 + i] - 127) * num9;
				for (int k = num3; k <= num4 + 10; k++)
				{
					double num13 = (double)k - centerY;
					double num14 = ((num13 > 0.0) ? (num12 * num12) : 0.0);
					double num15 = num13 * num13 / (num8 + num14);
					if (!(num10 + num15 + num11 > 1.0) && k <= worldheight - 1)
					{
						int num16 = k % 32;
						if (api.World.Blocks[chunks[k / 32].Data.GetFluid((num16 * 32 + j) * 32 + i)].LiquidCode != null)
						{
							return false;
						}
					}
				}
			}
		}
		horRadius -= 1f;
		vertRadius -= 2f;
		int num17 = (int)GameMath.Clamp(centerX - (double)horRadius, 0.0, 31.0);
		num2 = (int)GameMath.Clamp(centerX + (double)horRadius + 1.0, 0.0, 31.0);
		num5 = (int)GameMath.Clamp(centerZ - (double)horRadius, 0.0, 31.0);
		num6 = (int)GameMath.Clamp(centerZ + (double)horRadius + 1.0, 0.0, 31.0);
		num3 = (int)GameMath.Clamp(centerY - (double)(vertRadius * 0.7f), 1.0, worldheight - 1);
		num4 = (int)GameMath.Clamp(centerY + (double)vertRadius + 1.0, 1.0, worldheight - 1);
		num7 = horRadius * horRadius;
		num8 = vertRadius * vertRadius;
		int geologicActivity = getGeologicActivity(chunkX * 32 + (int)centerX, chunkZ * 32 + (int)centerZ);
		genHotSpring = genHotSpring && geologicActivity > 128;
		if (genHotSpring && centerX >= 0.0 && centerX < 32.0 && centerZ >= 0.0 && centerZ < 32.0)
		{
			Dictionary<Vec3i, HotSpringGenData> dictionary = mapChunk.GetModdata<Dictionary<Vec3i, HotSpringGenData>>("hotspringlocations");
			if (dictionary == null)
			{
				dictionary = new Dictionary<Vec3i, HotSpringGenData>();
			}
			dictionary[new Vec3i((int)centerX, (int)centerY, (int)centerZ)] = new HotSpringGenData
			{
				horRadius = horRadius
			};
			mapChunk.SetModdata("hotspringlocations", dictionary);
		}
		int num18 = geologicActivity * 16 / 128;
		for (int l = num17; l <= num2; l++)
		{
			double num10 = ((double)l - centerX) * ((double)l - centerX) / num7;
			for (int m = num5; m <= num6; m++)
			{
				double num11 = ((double)m - centerZ) * ((double)m - centerZ) / num7;
				double num19 = (double)(mapChunk.CaveHeightDistort[m * 32 + l] - 127) * num9;
				int num20 = terrainheightmap[m * 32 + l];
				for (int num21 = num4 + 10; num21 >= num3; num21--)
				{
					double num22 = (double)num21 - centerY;
					double num23 = ((num22 > 0.0) ? (num19 * num19 * Math.Min(1.0, (double)Math.Abs(num21 - num20) / 10.0)) : 0.0);
					double num15 = num22 * num22 / (num8 + num23);
					if (num21 <= worldheight - 1 && !(num10 + num15 + num11 > 1.0))
					{
						if (terrainheightmap[m * 32 + l] == num21)
						{
							terrainheightmap[m * 32 + l] = (ushort)(num21 - 1);
							rainheightmap[m * 32 + l]--;
						}
						IChunkBlocks data = chunks[num21 / 32].Data;
						int num24 = (num21 % 32 * 32 + m) * 32 + l;
						if (num21 == 11)
						{
							if (basaltNoise.Noise(chunkX * 32 + l, chunkZ * 32 + m) > 0.65)
							{
								data[num24] = GlobalConfig.basaltBlockId;
								terrainheightmap[m * 32 + l] = Math.Max(terrainheightmap[m * 32 + l], (ushort)11);
								rainheightmap[m * 32 + l] = Math.Max(rainheightmap[m * 32 + l], (ushort)11);
							}
							else
							{
								data[num24] = 0;
								if (num21 > num18)
								{
									data[num24] = GlobalConfig.basaltBlockId;
								}
								else
								{
									data.SetFluid(num24, GlobalConfig.lavaBlockId);
								}
								if (num21 <= num18)
								{
									worldgenBlockAccessor.ScheduleBlockLightUpdate(new BlockPos(chunkX * 32 + l, num21, chunkZ * 32 + m), airBlockId, GlobalConfig.lavaBlockId);
								}
							}
						}
						else if (num21 < 12)
						{
							data[num24] = 0;
							if (num21 > num18)
							{
								data[num24] = GlobalConfig.basaltBlockId;
							}
							else
							{
								data.SetFluid(num24, GlobalConfig.lavaBlockId);
							}
						}
						else
						{
							data.SetBlockAir(num24);
						}
					}
				}
			}
		}
		return true;
	}

	private int getGeologicActivity(int posx, int posz)
	{
		IntDataMap2D intDataMap2D = worldgenBlockAccessor.GetMapRegion(posx / regionsize, posz / regionsize)?.ClimateMap;
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
