using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Vintagestory.ServerMods;

public class GenDevastationLayer : ModStdWorldGen
{
	private ICoreServerAPI api;

	private StoryStructureLocation devastationLocation;

	public IWorldGenBlockAccessor worldgenBlockAccessor;

	public SimplexNoise distDistort;

	public NormalizedSimplexNoise devastationDensity;

	private byte[] noisemap;

	private int cellnoiseWidth;

	private int cellnoiseHeight;

	private const float fullHeightDist = 0.3f;

	private const float flatHeightDist = 0.4f;

	public static int[] DevastationBlockIds;

	private int growthBlockId;

	private int dim2Size;

	private const int Dim2HeightOffset = 9;

	private BlockPos tmpPos = new BlockPos();

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override double ExecuteOrder()
	{
		return 0.399;
	}

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("devastation").RegisterMessageType<DevaLocation>();
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		api.Event.InitWorldGenerator(InitWorldGen, "standard");
		api.Event.PlayerJoin += Event_PlayerJoin;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.ChunkColumnGeneration(OnChunkColumnGeneration, EnumWorldGenPass.Terrain, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
		}
		distDistort = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 20980);
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		if (devastationLocation != null)
		{
			api.Network.GetChannel("devastation").SendPacket(new DevaLocation
			{
				Pos = devastationLocation.CenterPos,
				Radius = devastationLocation.GenerationRadius
			}, byPlayer);
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		worldgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: false);
	}

	private void InitWorldGen()
	{
		LoadGlobalConfig(api);
		distDistort = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 20980);
		devastationDensity = new NormalizedSimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.04, 0.08, 0.16, 0.3076923076923077 }, api.World.SeaLevel + 20981);
		modSys.storyStructureInstances.TryGetValue("devastationarea", out devastationLocation);
		if (devastationLocation != null)
		{
			Timeswitch modSystem = api.ModLoader.GetModSystem<Timeswitch>();
			modSystem.SetPos(devastationLocation.CenterPos);
			dim2Size = modSystem.SetupDim2TowerGeneration(devastationLocation, modSys);
		}
		ModSystemDevastationEffects modSystem2 = api.ModLoader.GetModSystem<ModSystemDevastationEffects>();
		modSystem2.DevaLocationPresent = devastationLocation?.CenterPos.ToVec3d();
		modSystem2.DevaLocationPast = devastationLocation?.CenterPos.Copy().SetDimension(2).ToVec3d();
		modSystem2.EffectRadius = devastationLocation?.GenerationRadius ?? 0;
		BitmapRef bitmapRef = BitmapCreateFromPng(api.Assets.TryGet("worldgen/devastationcracks.png"));
		int[] pixels = bitmapRef.Pixels;
		noisemap = new byte[pixels.Length];
		for (int i = 0; i < pixels.Length; i++)
		{
			noisemap[i] = (byte)(pixels[i] & 0xFF);
		}
		cellnoiseWidth = bitmapRef.Width;
		cellnoiseHeight = bitmapRef.Height;
		DevastationBlockIds = new int[11]
		{
			GetBlockId("devastatedsoil-0"),
			GetBlockId("devastatedsoil-1"),
			GetBlockId("devastatedsoil-2"),
			GetBlockId("devastatedsoil-3"),
			GetBlockId("devastatedsoil-4"),
			GetBlockId("devastatedsoil-5"),
			GetBlockId("devastatedsoil-6"),
			GetBlockId("devastatedsoil-7"),
			GetBlockId("devastatedsoil-8"),
			GetBlockId("devastatedsoil-9"),
			GetBlockId("devastatedsoil-10")
		};
		growthBlockId = GetBlockId("devastationgrowth-normal");
		api.ModLoader.GetModSystem<GenStructures>().OnPreventSchematicPlaceAt += OnPreventSchematicPlaceAt;
	}

	private int GetBlockId(string code)
	{
		return api.World.GetBlock(new AssetLocation(code))?.BlockId ?? GlobalConfig.defaultRockId;
	}

	public BitmapRef BitmapCreateFromPng(IAsset asset)
	{
		return new BitmapExternal(asset.Data, asset.Data.Length, api.Logger);
	}

	private void OnChunkColumnGeneration(IChunkColumnGenerateRequest request)
	{
		if (devastationLocation == null)
		{
			return;
		}
		BlockPos centerPos = devastationLocation.CenterPos;
		int generationRadius = devastationLocation.GenerationRadius;
		int num = request.ChunkX * 32 + 16;
		int num2 = request.ChunkZ * 32 + 16;
		if ((double)centerPos.HorDistanceSqTo(num, num2) >= (double)((generationRadius + 200) * (generationRadius + 200)))
		{
			return;
		}
		Random rand = api.World.Rand;
		IServerChunk[] chunks = request.Chunks;
		IMapChunk mapChunk = chunks[0].MapChunk;
		if (ShouldGenerateDim2Terrain(request.ChunkX, request.ChunkZ))
		{
			api.WorldManager.CreateChunkColumnForDimension(request.ChunkX, request.ChunkZ, 2);
			GenerateDim2ChunkColumn(request.ChunkX, request.ChunkZ, mapChunk.WorldGenTerrainHeightMap);
		}
		float num3 = (float)DevastationBlockIds.Length - 1.01f;
		float num4 = DevastationBlockIds.Length;
		float num5 = (float)DevastationBlockIds.Length * 2f;
		for (int i = 0; i < 32; i++)
		{
			for (int j = 0; j < 32; j++)
			{
				int num6 = request.ChunkX * 32 + i;
				int num7 = request.ChunkZ * 32 + j;
				double num8 = GameMath.Clamp(devastationDensity.Noise(num6, num7) * (double)num5 - (double)num4, 0.0, num3);
				double num9 = distDistort.Noise(num6, num7);
				double num10 = (double)centerPos.HorDistanceSqTo(num6, num7) / (double)(generationRadius * generationRadius);
				double num11 = num10 + num9 / 30.0;
				if (num11 > 1.0)
				{
					continue;
				}
				double num12 = GameMath.Map(GameMath.Clamp(num10 + num9 / 1000.0, 0.30000001192092896, 0.4000000059604645), 0.30000001192092896, 0.4000000059604645, 0.0, 1.0);
				double a = GameMath.Clamp((1.0 - num12) * 10.0, 0.0, 10.0);
				double num13 = GameMath.Clamp((0.6000000238418579 - num11) * 20.0, 0.0, 10.0);
				double num14 = GameMath.Max(a, num13 * GameMath.Clamp(num12 + 0.2, 0.0, 0.8));
				int num15 = j * 32 + i;
				int num16 = mapChunk.WorldGenTerrainHeightMap[num15];
				int num17 = num6 - centerPos.X + cellnoiseWidth / 2;
				int num18 = num7 - centerPos.Z + cellnoiseHeight / 2;
				int num19 = 0;
				if (num17 >= 0 && num18 >= 0 && num17 < cellnoiseWidth && num18 < cellnoiseHeight)
				{
					num19 = noisemap[num18 * cellnoiseWidth + num17];
				}
				int num20 = (int)Math.Round(num14 - (double)((float)num19 / 30f));
				for (int k = num20 - 10; k <= num20; k++)
				{
					int num21 = (num16 + k) / 32;
					int num22 = (num16 + k) % 32;
					int index3d = (32 * num22 + j) * 32 + i;
					chunks[num21].Data.SetBlockUnsafe(index3d, DevastationBlockIds[(int)Math.Round(num8)]);
					chunks[num21].Data.SetFluid(index3d, 0);
				}
				if (num20 < 0)
				{
					for (int l = num20; l <= 0; l++)
					{
						int num23 = (num16 + l) / 32;
						int num24 = (num16 + l) % 32;
						int index3d2 = (32 * num24 + j) * 32 + i;
						chunks[num23].Data.SetBlockUnsafe(index3d2, 0);
						chunks[num23].Data.SetFluid(index3d2, 0);
					}
				}
				ushort num25 = (ushort)(num16 + num20);
				mapChunk.WorldGenTerrainHeightMap[num15] = num25;
				ushort num26 = Math.Max(num25, mapChunk.RainHeightMap[num15]);
				mapChunk.RainHeightMap[num15] = num26;
				if (rand.NextDouble() - 0.1 < num8 && num26 == num25)
				{
					int num27 = (num16 + num20 + 1) / 32;
					int num28 = (num16 + num20 + 1) % 32;
					int index3d3 = (32 * num28 + j) * 32 + i;
					chunks[num27].Data.SetBlockUnsafe(index3d3, growthBlockId);
				}
			}
		}
		api.ModLoader.GetModSystem<Timeswitch>().AttemptGeneration(worldgenBlockAccessor);
	}

	private bool OnPreventSchematicPlaceAt(IBlockAccessor blockAccessor, BlockPos pos, Cuboidi schematicLocation, string locationCode)
	{
		if (locationCode == "devastationarea" && !HasDevastationSoil(blockAccessor, pos, schematicLocation.SizeX, schematicLocation.SizeZ))
		{
			return true;
		}
		return false;
	}

	private bool HasDevastationSoil(IBlockAccessor blockAccessor, BlockPos startPos, int wdt, int len)
	{
		tmpPos.Set(startPos.X, startPos.Y + 1, startPos.Z);
		int terrainMapheightAt = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Y = terrainMapheightAt;
		if (!DevastationBlockIds.Contains(blockAccessor.GetBlockId(tmpPos)))
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt, startPos.Y + 1, startPos.Z);
		terrainMapheightAt = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Y = terrainMapheightAt;
		if (!DevastationBlockIds.Contains(blockAccessor.GetBlockId(tmpPos)))
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y + 1, startPos.Z + len);
		terrainMapheightAt = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Y = terrainMapheightAt;
		if (!DevastationBlockIds.Contains(blockAccessor.GetBlockId(tmpPos)))
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt, startPos.Y + 1, startPos.Z + len);
		terrainMapheightAt = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Y = terrainMapheightAt;
		if (!DevastationBlockIds.Contains(blockAccessor.GetBlockId(tmpPos)))
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt / 2, startPos.Y + 1, startPos.Z + len / 2);
		terrainMapheightAt = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Y = terrainMapheightAt;
		if (!DevastationBlockIds.Contains(blockAccessor.GetBlockId(tmpPos)))
		{
			return false;
		}
		return true;
	}

	public override void Dispose()
	{
		DevastationBlockIds = null;
	}

	private bool ShouldGenerateDim2Terrain(int cx, int cz)
	{
		int num = dim2Size;
		int num2 = devastationLocation.CenterPos.X / 32;
		int num3 = devastationLocation.CenterPos.Z / 32;
		int num4 = Math.Abs(cx - num2);
		int num5 = Math.Abs(cz - num3);
		if (num4 + num5 == 0)
		{
			devastationLocation.DidGenerateAdditional = false;
		}
		if (num4 <= num)
		{
			return num5 <= num;
		}
		return false;
	}

	private void GenerateDim2ChunkColumn(int cx, int cz, ushort[] heightmap)
	{
		int defaultRockId = GlobalConfig.defaultRockId;
		int blockId = GetBlockId("soil-medium-none");
		int blockId2 = GetBlockId("soil-medium-normal");
		int blockId3 = GetBlockId("tallgrass-medium-free");
		int blockId4 = GetBlockId("tallgrass-tall-free");
		int num = api.World.BlockAccessor.MapSizeY - 1;
		int num2 = 0;
		for (int i = 0; i < heightmap.Length; i++)
		{
			int num3 = heightmap[i] + 9;
			if (num3 < num)
			{
				num = num3;
			}
			if (num3 > num2)
			{
				num2 = num3;
			}
		}
		int num4 = 2048;
		IWorldChunk chunk = api.World.BlockAccessor.GetChunk(cx, num4, cz);
		if (chunk == null)
		{
			return;
		}
		chunk.Unpack();
		IChunkBlocks data = chunk.Data;
		data.SetBlockBulk(0, 32, 32, GlobalConfig.mantleBlockId);
		int j;
		for (j = 1; j < num - 3; j++)
		{
			if (j % 32 == 0)
			{
				num4++;
				chunk = api.World.BlockAccessor.GetChunk(cx, num4, cz);
				if (chunk == null)
				{
					break;
				}
				data = chunk.Data;
			}
			data.SetBlockBulk(j % 32 * 32 * 32, 32, 32, defaultRockId);
		}
		num2++;
		for (int k = j; k <= num2; k++)
		{
			if (k % 32 == 0)
			{
				num4++;
				chunk = api.World.BlockAccessor.GetChunk(cx, num4, cz);
				if (chunk == null)
				{
					break;
				}
				data = chunk.Data;
			}
			for (int l = 0; l < 32; l++)
			{
				for (int m = 0; m < 32; m++)
				{
					int num5 = heightmap[l * 32 + m] + 9;
					int ly = k % 32;
					if (k < num5 - 2)
					{
						data[ChunkIndex3D(m, ly, l)] = defaultRockId;
					}
					else if (k < num5)
					{
						data[ChunkIndex3D(m, ly, l)] = blockId;
					}
					else if (k == num5)
					{
						data[ChunkIndex3D(m, ly, l)] = blockId2;
					}
					else if (k == num5 + 1)
					{
						int num6 = GameMath.oaatHash(m + cx * 32, l + cz * 32);
						if (num6 % 21 < 3)
						{
							data[ChunkIndex3D(m, ly, l)] = ((num6 % 21 == 0) ? blockId4 : blockId3);
						}
					}
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ChunkIndex3D(int lx, int ly, int lz)
	{
		return (ly * 32 + lz) * 32 + lx;
	}
}
