using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenCreatures : ModStdWorldGen
{
	private ICoreServerAPI api;

	private Random rnd;

	private int worldheight;

	private IWorldGenBlockAccessor wgenBlockAccessor;

	private Dictionary<EntityProperties, EntityProperties[]> entityTypeGroups = new Dictionary<EntityProperties, EntityProperties[]>();

	private int climateUpLeft;

	private int climateUpRight;

	private int climateBotLeft;

	private int climateBotRight;

	private int forestUpLeft;

	private int forestUpRight;

	private int forestBotLeft;

	private int forestBotRight;

	private int shrubsUpLeft;

	private int shrubsUpRight;

	private int shrubsBotLeft;

	private int shrubsBotRight;

	private List<SpawnOppurtunity> spawnPositions = new List<SpawnOppurtunity>();

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.1;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(initWorldGen, "standard");
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.PreDone, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		wgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: true);
	}

	private void initWorldGen()
	{
		LoadGlobalConfig(api);
		rnd = new Random(api.WorldManager.Seed - 18722);
		worldheight = api.WorldManager.MapSizeY;
		Dictionary<AssetLocation, EntityProperties> dictionary = new Dictionary<AssetLocation, EntityProperties>();
		for (int i = 0; i < api.World.EntityTypes.Count; i++)
		{
			dictionary[api.World.EntityTypes[i].Code] = api.World.EntityTypes[i];
		}
		Dictionary<AssetLocation, Block[]> searchCache = new Dictionary<AssetLocation, Block[]>();
		for (int j = 0; j < api.World.EntityTypes.Count; j++)
		{
			EntityProperties entityProperties = api.World.EntityTypes[j];
			WorldGenSpawnConditions worldGenSpawnConditions = entityProperties.Server?.SpawnConditions?.Worldgen;
			if (worldGenSpawnConditions == null)
			{
				continue;
			}
			List<EntityProperties> list = new List<EntityProperties>();
			list.Add(entityProperties);
			worldGenSpawnConditions.Initialise(api.World, entityProperties.Code.ToShortString(), searchCache);
			AssetLocation[] companions = worldGenSpawnConditions.Companions;
			if (companions == null)
			{
				continue;
			}
			for (int k = 0; k < companions.Length; k++)
			{
				if (dictionary.TryGetValue(companions[k], out var value))
				{
					list.Add(value);
				}
			}
			entityTypeGroups[entityProperties] = list.ToArray();
		}
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		if (GetIntersectingStructure(chunkX * 32 + 16, chunkZ * 32 + 16, ModStdWorldGen.SkipCreaturesgHashCode) != null)
		{
			return;
		}
		wgenBlockAccessor.BeginColumn();
		IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
		ushort[] worldGenTerrainHeightMap = chunks[0].MapChunk.WorldGenTerrainHeightMap;
		int num = api.WorldManager.RegionSize / 32;
		int num2 = chunkX % num;
		int num3 = chunkZ % num;
		float num4 = (float)climateMap.InnerSize / (float)num;
		climateUpLeft = climateMap.GetUnpaddedInt((int)((float)num2 * num4), (int)((float)num3 * num4));
		climateUpRight = climateMap.GetUnpaddedInt((int)((float)num2 * num4 + num4), (int)((float)num3 * num4));
		climateBotLeft = climateMap.GetUnpaddedInt((int)((float)num2 * num4), (int)((float)num3 * num4 + num4));
		climateBotRight = climateMap.GetUnpaddedInt((int)((float)num2 * num4 + num4), (int)((float)num3 * num4 + num4));
		IntDataMap2D forestMap = chunks[0].MapChunk.MapRegion.ForestMap;
		float num5 = (float)forestMap.InnerSize / (float)num;
		forestUpLeft = forestMap.GetUnpaddedInt((int)((float)num2 * num5), (int)((float)num3 * num5));
		forestUpRight = forestMap.GetUnpaddedInt((int)((float)num2 * num5 + num5), (int)((float)num3 * num5));
		forestBotLeft = forestMap.GetUnpaddedInt((int)((float)num2 * num5), (int)((float)num3 * num5 + num5));
		forestBotRight = forestMap.GetUnpaddedInt((int)((float)num2 * num5 + num5), (int)((float)num3 * num5 + num5));
		IntDataMap2D shrubMap = chunks[0].MapChunk.MapRegion.ShrubMap;
		float num6 = (float)shrubMap.InnerSize / (float)num;
		shrubsUpLeft = shrubMap.GetUnpaddedInt((int)((float)num2 * num6), (int)((float)num3 * num6));
		shrubsUpRight = shrubMap.GetUnpaddedInt((int)((float)num2 * num6 + num6), (int)((float)num3 * num6));
		shrubsBotLeft = shrubMap.GetUnpaddedInt((int)((float)num2 * num6), (int)((float)num3 * num6 + num6));
		shrubsBotRight = shrubMap.GetUnpaddedInt((int)((float)num2 * num6 + num6), (int)((float)num3 * num6 + num6));
		Vec3d vec3d = new Vec3d();
		BlockPos blockPos = new BlockPos();
		foreach (KeyValuePair<EntityProperties, EntityProperties[]> entityTypeGroup in entityTypeGroups)
		{
			EntityProperties key = entityTypeGroup.Key;
			float num7 = key.Server.SpawnConditions.Worldgen.TriesPerChunk.nextFloat(1f, rnd);
			if (num7 != 0f)
			{
				RuntimeSpawnConditions runtime = key.Server.SpawnConditions.Runtime;
				if (runtime == null || runtime.Group != "hostile")
				{
					num7 *= GlobalConfig.neutralCreatureSpawnMultiplier;
				}
				while ((double)num7-- > rnd.NextDouble())
				{
					int num8 = rnd.Next(32);
					int num9 = rnd.Next(32);
					blockPos.Set(chunkX * 32 + num8, 0, chunkZ * 32 + num9);
					blockPos.Y = (key.Server.SpawnConditions.Worldgen.TryOnlySurface ? (worldGenTerrainHeightMap[num9 * 32 + num8] + 1) : rnd.Next(worldheight));
					vec3d.Set((double)blockPos.X + 0.5, (double)blockPos.Y + 0.005, (double)blockPos.Z + 0.5);
					TrySpawnGroupAt(blockPos, vec3d, key, entityTypeGroup.Value);
				}
			}
		}
	}

	private void TrySpawnGroupAt(BlockPos origin, Vec3d posAsVec, EntityProperties entityType, EntityProperties[] grouptypes)
	{
		BlockPos blockPos = origin.Copy();
		int num = 0;
		WorldGenSpawnConditions worldgen = entityType.Server.SpawnConditions.Worldgen;
		spawnPositions.Clear();
		int num2 = 0;
		int num3 = 10;
		while (num2 <= 0 && num3-- > 0)
		{
			float num4 = worldgen.HerdSize.nextFloat();
			num2 = (int)num4 + (((double)(num4 - (float)(int)num4) > rnd.NextDouble()) ? 1 : 0);
		}
		for (int i = 0; i < num2 * 4 + 5; i++)
		{
			if (num >= num2)
			{
				break;
			}
			EntityProperties entityProperties = entityType;
			double num5 = ((num == 0) ? 1.0 : Math.Min(0.2, 1f / (float)grouptypes.Length));
			if (grouptypes.Length > 1 && rnd.NextDouble() > num5)
			{
				entityProperties = grouptypes[1 + rnd.Next(grouptypes.Length - 1)];
			}
			IBlockAccessor blockAccessor2;
			if (wgenBlockAccessor.GetChunkAtBlockPos(blockPos) != null)
			{
				IBlockAccessor blockAccessor = wgenBlockAccessor;
				blockAccessor2 = blockAccessor;
			}
			else
			{
				blockAccessor2 = api.World.BlockAccessor;
			}
			IBlockAccessor blockAccessor3 = blockAccessor2;
			IMapChunk mapChunkAtBlockPos = blockAccessor3.GetMapChunkAtBlockPos(blockPos);
			if (mapChunkAtBlockPos != null)
			{
				if (worldgen.TryOnlySurface)
				{
					ushort[] worldGenTerrainHeightMap = mapChunkAtBlockPos.WorldGenTerrainHeightMap;
					blockPos.Y = worldGenTerrainHeightMap[blockPos.Z % 32 * 32 + blockPos.X % 32] + 1;
				}
				if (CanSpawnAtPosition(blockAccessor3, entityProperties, blockPos, worldgen))
				{
					posAsVec.Set((double)blockPos.X + 0.5, (double)blockPos.Y + 0.005, (double)blockPos.Z + 0.5);
					float num6 = (float)(posAsVec.X % 32.0) / 32f;
					float num7 = (float)(posAsVec.Z % 32.0) / 32f;
					int num8 = GameMath.BiLerpRgbColor(num6, num7, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
					float scaledAdjustedTemperatureFloat = Climate.GetScaledAdjustedTemperatureFloat((num8 >> 16) & 0xFF, (int)posAsVec.Y - TerraGenConfig.seaLevel);
					float rain = (float)((num8 >> 8) & 0xFF) / 255f;
					float forestDensity = GameMath.BiLerp(forestUpLeft, forestUpRight, forestBotLeft, forestBotRight, num6, num7) / 255f;
					float shrubsDensity = GameMath.BiLerp(shrubsUpLeft, shrubsUpRight, shrubsBotLeft, shrubsBotRight, num6, num7) / 255f;
					if (CanSpawnAtConditions(blockAccessor3, entityProperties, blockPos, posAsVec, worldgen, rain, scaledAdjustedTemperatureFloat, forestDensity, shrubsDensity))
					{
						spawnPositions.Add(new SpawnOppurtunity
						{
							ForType = entityProperties,
							Pos = posAsVec.Clone()
						});
						num++;
					}
				}
			}
			blockPos.X = origin.X + (rnd.Next(11) - 5 + (rnd.Next(11) - 5)) / 2;
			blockPos.Z = origin.Z + (rnd.Next(11) - 5 + (rnd.Next(11) - 5)) / 2;
		}
		if (spawnPositions.Count < num2)
		{
			return;
		}
		long nextUniqueId = api.WorldManager.GetNextUniqueId();
		foreach (SpawnOppurtunity spawnPosition in spawnPositions)
		{
			Entity entity = CreateEntity(spawnPosition.ForType, spawnPosition.Pos);
			if (entity is EntityAgent)
			{
				(entity as EntityAgent).HerdId = nextUniqueId;
			}
			if (api.Event.TriggerTrySpawnEntity(wgenBlockAccessor, ref spawnPosition.ForType, spawnPosition.Pos, nextUniqueId))
			{
				if (wgenBlockAccessor.GetChunkAtBlockPos(blockPos) == null)
				{
					api.World.SpawnEntity(entity);
				}
				else
				{
					wgenBlockAccessor.AddEntity(entity);
				}
			}
		}
	}

	private Entity CreateEntity(EntityProperties entityType, Vec3d spawnPosition)
	{
		Entity entity = api.ClassRegistry.CreateEntity(entityType);
		entity.ServerPos.SetPosWithDimension(spawnPosition);
		entity.ServerPos.SetYaw((float)rnd.NextDouble() * ((float)Math.PI * 2f));
		entity.Pos.SetFrom(entity.ServerPos);
		entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		entity.Attributes.SetString("origin", "worldgen");
		return entity;
	}

	private bool CanSpawnAtPosition(IBlockAccessor blockAccessor, EntityProperties type, BlockPos pos, BaseSpawnConditions sc)
	{
		if (!blockAccessor.IsValidPos(pos))
		{
			return false;
		}
		Block block = blockAccessor.GetBlock(pos);
		if (!sc.CanSpawnInside(block))
		{
			return false;
		}
		pos.Y--;
		if (!blockAccessor.GetBlock(pos).CanCreatureSpawnOn(blockAccessor, pos, type, sc))
		{
			pos.Y++;
			return false;
		}
		pos.Y++;
		return true;
	}

	private bool CanSpawnAtConditions(IBlockAccessor blockAccessor, EntityProperties type, BlockPos pos, Vec3d posAsVec, BaseSpawnConditions sc, float rain, float temp, float forestDensity, float shrubsDensity)
	{
		float? num = blockAccessor.GetLightLevel(pos, EnumLightLevelType.MaxLight);
		if (!num.HasValue)
		{
			return false;
		}
		if ((float)sc.MinLightLevel > num || (float)sc.MaxLightLevel < num)
		{
			return false;
		}
		if (sc.MinTemp > temp || sc.MaxTemp < temp)
		{
			return false;
		}
		if (sc.MinRain > rain || sc.MaxRain < rain)
		{
			return false;
		}
		if (sc.MinForest > forestDensity || sc.MaxForest < forestDensity)
		{
			return false;
		}
		if (sc.MinShrubs > shrubsDensity || sc.MaxShrubs < shrubsDensity)
		{
			return false;
		}
		if (sc.MinForestOrShrubs > Math.Max(forestDensity, shrubsDensity))
		{
			return false;
		}
		double num2 = ((pos.Y > TerraGenConfig.seaLevel) ? (1.0 + ((double)pos.Y - (double)TerraGenConfig.seaLevel) / (double)(api.World.BlockAccessor.MapSizeY - TerraGenConfig.seaLevel)) : ((double)pos.Y / (double)TerraGenConfig.seaLevel));
		if ((double)sc.MinY > num2 || (double)sc.MaxY < num2)
		{
			return false;
		}
		Cuboidf entityBoxRel = type.SpawnCollisionBox.OmniNotDownGrowBy(0.1f);
		return !IsColliding(entityBoxRel, posAsVec);
	}

	public bool IsColliding(Cuboidf entityBoxRel, Vec3d pos)
	{
		BlockPos blockPos = new BlockPos();
		Cuboidd cuboidd = entityBoxRel.ToDouble().Translate(pos);
		Vec3d vec3d = new Vec3d();
		int num = (int)((double)entityBoxRel.X1 + pos.X);
		int num2 = (int)((double)entityBoxRel.Y1 + pos.Y);
		int num3 = (int)((double)entityBoxRel.Z1 + pos.Z);
		int num4 = (int)Math.Ceiling((double)entityBoxRel.X2 + pos.X);
		int num5 = (int)Math.Ceiling((double)entityBoxRel.Y2 + pos.Y);
		int num6 = (int)Math.Ceiling((double)entityBoxRel.Z2 + pos.Z);
		for (int i = num2; i <= num5; i++)
		{
			for (int j = num; j <= num4; j++)
			{
				for (int k = num3; k <= num6; k++)
				{
					IBlockAccessor blockAccessor = wgenBlockAccessor;
					IWorldChunk chunkAtBlockPos = wgenBlockAccessor.GetChunkAtBlockPos(j, i, k);
					if (chunkAtBlockPos == null)
					{
						chunkAtBlockPos = api.World.BlockAccessor.GetChunkAtBlockPos(j, i, k);
						blockAccessor = api.World.BlockAccessor;
					}
					if (chunkAtBlockPos == null)
					{
						return true;
					}
					int index = (i % 32 * 32 + k % 32) * 32 + j % 32;
					Block block = api.World.Blocks[chunkAtBlockPos.UnpackAndReadBlock(index, 0)];
					blockPos.Set(j, i, k);
					vec3d.Set(j, i, k);
					Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccessor, blockPos);
					int num7 = 0;
					while (collisionBoxes != null && num7 < collisionBoxes.Length)
					{
						Cuboidf cuboidf = collisionBoxes[num7];
						if (cuboidf != null && cuboidd.Intersects(cuboidf, vec3d))
						{
							return true;
						}
						num7++;
					}
				}
			}
		}
		return false;
	}
}
