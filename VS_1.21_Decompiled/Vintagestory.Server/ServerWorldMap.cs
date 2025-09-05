using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Server;

public sealed class ServerWorldMap : WorldMap, IChunkProvider, ILandClaimAPI
{
	internal ServerMain server;

	private Vec3i mapsize = new Vec3i();

	public ChunkIlluminator chunkIlluminatorWorldGen;

	public ChunkIlluminator chunkIlluminatorMainThread;

	public IBlockAccessor StrictBlockAccess;

	public IBlockAccessor RelaxedBlockAccess;

	public BlockAccessorRelaxedBulkUpdate BulkBlockAccess;

	public IBlockAccessor RawRelaxedBlockAccess;

	public BlockAccessorPrefetch PrefetchBlockAccess;

	private int prevChunkX = -1;

	private int prevChunkY = -1;

	private int prevChunkZ = -1;

	private IWorldChunk prevChunk;

	public object LightingTasksLock = new object();

	public Queue<UpdateLightingTask> LightingTasks = new Queue<UpdateLightingTask>();

	private int regionMapSizeX;

	private int regionMapSizeY;

	private int regionMapSizeZ;

	private List<LandClaim> landClaims;

	public override ILogger Logger => ServerMain.Logger;

	ILogger IChunkProvider.Logger => ServerMain.Logger;

	public override IList<Block> Blocks => server.Blocks;

	public override Dictionary<AssetLocation, Block> BlocksByCode => server.BlocksByCode;

	public override int ChunkSize => 32;

	public override int RegionSize => 32 * MagicNum.ChunkRegionSizeInChunks;

	public override Vec3i MapSize => mapsize;

	public override int MapSizeX => mapsize.X;

	public override int MapSizeY => mapsize.Y;

	public override int MapSizeZ => mapsize.Z;

	public override int RegionMapSizeX => regionMapSizeX;

	public override int RegionMapSizeY => regionMapSizeY;

	public override int RegionMapSizeZ => regionMapSizeZ;

	public override IWorldAccessor World => server;

	public override int ChunkSizeMask => 31;

	public override List<LandClaim> All => landClaims;

	public override bool DebugClaimPrivileges => server.DebugPrivileges;

	public ServerWorldMap(ServerMain server)
	{
		this.server = server;
		chunkIlluminatorWorldGen = new ChunkIlluminator(null, new BlockAccessorRelaxed(this, server, synchronize: false, relight: false), MagicNum.ServerChunkSize);
		chunkIlluminatorMainThread = new ChunkIlluminator(this, new BlockAccessorRelaxed(this, server, synchronize: false, relight: false), MagicNum.ServerChunkSize);
		RelaxedBlockAccess = new BlockAccessorRelaxed(this, server, synchronize: true, relight: true);
		RawRelaxedBlockAccess = new BlockAccessorRelaxed(this, server, synchronize: false, relight: false);
		StrictBlockAccess = new BlockAccessorStrict(this, server, synchronize: true, relight: true, debug: false);
		BulkBlockAccess = new BlockAccessorRelaxedBulkUpdate(this, server, synchronize: true, relight: true, debug: false);
		PrefetchBlockAccess = new BlockAccessorPrefetch(this, server, synchronize: true, relight: true);
	}

	public void Init(int sizex, int sizey, int sizez)
	{
		mapsize = new Vec3i(sizex, sizey, sizez);
		chunkMapSizeY = sizey / 32;
		index3dMulX = 2097152;
		index3dMulZ = 2097152;
		chunkIlluminatorWorldGen.InitForWorld(server.Blocks, (ushort)server.sunBrightness, sizex, sizey, sizez);
		chunkIlluminatorMainThread.InitForWorld(server.Blocks, (ushort)server.sunBrightness, sizex, sizey, sizez);
		if (GameVersion.IsAtLeastVersion(server.SaveGameData.CreatedGameVersion, "1.12.9"))
		{
			regionMapSizeX = (int)Math.Ceiling((double)mapsize.X / (double)MagicNum.MapRegionSize);
			regionMapSizeY = (int)Math.Ceiling((double)mapsize.Y / (double)MagicNum.MapRegionSize);
			regionMapSizeZ = (int)Math.Ceiling((double)mapsize.Z / (double)MagicNum.MapRegionSize);
		}
		else
		{
			regionMapSizeX = mapsize.X / MagicNum.MapRegionSize;
			regionMapSizeY = mapsize.Y / MagicNum.MapRegionSize;
			regionMapSizeZ = mapsize.Z / MagicNum.MapRegionSize;
		}
		landClaims = new List<LandClaim>(server.SaveGameData.LandClaims);
		RebuildLandClaimPartitions();
	}

	public ChunkPos MapRegionPosFromIndex2D(long index)
	{
		return new ChunkPos((int)index, 0, (int)(index >> 32), 0);
	}

	public void MapRegionPosFromIndex2D(long index, out int x, out int z)
	{
		x = (int)index;
		z = (int)(index >> 32);
	}

	public Vec2i MapChunkPosFromChunkIndex2D(long chunkIndex2d)
	{
		return new Vec2i((int)(chunkIndex2d % base.ChunkMapSizeX), (int)(chunkIndex2d / base.ChunkMapSizeX));
	}

	public Dictionary<long, WorldChunk> PositionsToUniqueChunks(List<BlockPos> positions)
	{
		FastSetOfLongs fastSetOfLongs = new FastSetOfLongs();
		foreach (BlockPos position in positions)
		{
			fastSetOfLongs.Add(ChunkIndex3D(position.X / 32, position.InternalY / 32, position.Z / 32));
		}
		Dictionary<long, WorldChunk> dictionary = new Dictionary<long, WorldChunk>(fastSetOfLongs.Count);
		foreach (long item in fastSetOfLongs)
		{
			dictionary.Add(item, GetChunk(item) as WorldChunk);
		}
		return dictionary;
	}

	public override IWorldChunk GetChunkAtPos(int posX, int posY, int posZ)
	{
		return GetServerChunk(posX / MagicNum.ServerChunkSize, posY / MagicNum.ServerChunkSize, posZ / MagicNum.ServerChunkSize);
	}

	public override WorldChunk GetChunk(BlockPos pos)
	{
		return GetServerChunk(pos.X / MagicNum.ServerChunkSize, pos.InternalY / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize);
	}

	public override IWorldChunk GetChunk(long index3d)
	{
		return GetServerChunk(index3d);
	}

	public ServerChunk GetServerChunk(int chunkX, int chunkY, int chunkZ)
	{
		return GetServerChunk(ChunkIndex3D(chunkX, chunkY, chunkZ));
	}

	public ServerChunk GetServerChunk(long chunkIndex3d)
	{
		return server.GetLoadedChunk(chunkIndex3d);
	}

	public override IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		return GetServerChunk(ChunkIndex3D(chunkX, chunkY, chunkZ));
	}

	public IWorldChunk GetUnpackedChunkFast(int chunkX, int chunkY, int chunkZ, bool notRecentlyAccessed = false)
	{
		ServerChunk serverChunk = null;
		lock (server.loadedChunks)
		{
			if (chunkX == prevChunkX && chunkY == prevChunkY && chunkZ == prevChunkZ)
			{
				if (!notRecentlyAccessed)
				{
					return prevChunk;
				}
				serverChunk = (ServerChunk)prevChunk;
			}
			else
			{
				prevChunkX = chunkX;
				prevChunkY = chunkY;
				prevChunkZ = chunkZ;
				serverChunk = (ServerChunk)(prevChunk = server.GetLoadedChunk(ChunkIndex3D(chunkX, chunkY, chunkZ)));
			}
		}
		serverChunk?.Unpack();
		return serverChunk;
	}

	public override IWorldChunk GetChunkNonLocking(int chunkX, int chunkY, int chunkZ)
	{
		server.loadedChunks.TryGetValue(ChunkIndex3D(chunkX, chunkY, chunkZ), out var value);
		return value;
	}

	public override IMapRegion GetMapRegion(int regionX, int regionZ)
	{
		server.loadedMapRegions.TryGetValue(MapRegionIndex2D(regionX, regionZ), out var value);
		return value;
	}

	public IMapRegion GetMapRegion(BlockPos pos)
	{
		server.loadedMapRegions.TryGetValue(MapRegionIndex2D(pos.X / RegionSize, pos.Z / RegionSize), out var value);
		return value;
	}

	public override IMapChunk GetMapChunk(int chunkX, int chunkZ)
	{
		server.loadedMapChunks.TryGetValue(MapChunkIndex2D(chunkX, chunkZ), out var value);
		return value;
	}

	public override void SendSetBlock(int blockId, int posX, int posY, int posZ)
	{
		server.SendSetBlock(blockId, posX, posY, posZ);
	}

	public override void SendExchangeBlock(int blockId, int posX, int posY, int posZ)
	{
		server.SendSetBlock(blockId, posX, posY, posZ, -1, exchangeOnly: true);
	}

	public override void SendDecorUpdateBulk(IEnumerable<BlockPos> updatedDecorPositions)
	{
		foreach (BlockPos updatedDecorPosition in updatedDecorPositions)
		{
			MarkDecorsDirty(updatedDecorPosition);
		}
	}

	public override void SendBlockUpdateBulk(IEnumerable<BlockPos> blockUpdates, bool doRelight)
	{
		foreach (BlockPos blockUpdate in blockUpdates)
		{
			MarkBlockModified(blockUpdate, doRelight);
		}
	}

	public override void SendBlockUpdateBulkMinimal(Dictionary<BlockPos, BlockUpdate> updates)
	{
		foreach (KeyValuePair<BlockPos, BlockUpdate> update in updates)
		{
			server.ModifiedBlocksMinimal.Add(update.Key);
		}
	}

	public void SendBlockUpdateExcept(int blockId, int posX, int posY, int posZ, int clientId)
	{
		server.SendSetBlock(blockId, posX, posY, posZ, clientId);
	}

	public int GetTerrainGenSurfacePosY(int posX, int posZ)
	{
		long chunkIndex3d = server.WorldMap.ChunkIndex3D(posX / MagicNum.ServerChunkSize, 0, posZ / MagicNum.ServerChunkSize);
		ServerChunk serverChunk = GetServerChunk(chunkIndex3d);
		if (serverChunk == null || serverChunk.MapChunk == null)
		{
			return 0;
		}
		return serverChunk.MapChunk.WorldGenTerrainHeightMap[posZ % MagicNum.ServerChunkSize * MagicNum.ServerChunkSize + posX % MagicNum.ServerChunkSize] + 1;
	}

	public void MarkChunksDirty(BlockPos blockPos, int blockRange)
	{
		int num = (blockPos.X - blockRange) / MagicNum.ServerChunkSize;
		int num2 = (blockPos.X + blockRange) / MagicNum.ServerChunkSize;
		int num3 = (blockPos.Y - blockRange) / MagicNum.ServerChunkSize;
		int num4 = (blockPos.Y + blockRange) / MagicNum.ServerChunkSize;
		int num5 = (blockPos.Z - blockRange) / MagicNum.ServerChunkSize;
		int num6 = (blockPos.Z + blockRange) / MagicNum.ServerChunkSize;
		for (int i = num; i <= num2; i++)
		{
			for (int j = num3; j <= num4; j++)
			{
				for (int k = num5; k <= num6; k++)
				{
					GetServerChunk(i, j, k)?.MarkModified();
				}
			}
		}
	}

	public override void MarkChunkDirty(int chunkX, int chunkY, int chunkZ, bool priority = false, bool sunRelight = false, Action OnRetesselated = null, bool fireDirtyEvent = true, bool edgeOnly = false)
	{
		ServerChunk serverChunk = GetServerChunk(chunkX, chunkY, chunkZ);
		if (serverChunk != null)
		{
			serverChunk.MarkModified();
			if (fireDirtyEvent)
			{
				server.api.eventapi.TriggerChunkDirty(new Vec3i(chunkX, chunkY, chunkZ), serverChunk, EnumChunkDirtyReason.MarkedDirty);
			}
		}
	}

	public override void UpdateLighting(int oldblockid, int newblockid, BlockPos pos)
	{
		long key = server.WorldMap.MapChunkIndex2D(pos.X / 32, pos.Z / 32);
		server.loadedMapChunks.TryGetValue(key, out var value);
		if (value == null)
		{
			return;
		}
		value.MarkFresh();
		lock (LightingTasksLock)
		{
			LightingTasks.Enqueue(new UpdateLightingTask
			{
				oldBlockId = oldblockid,
				newBlockId = newblockid,
				pos = pos.Copy()
			});
		}
	}

	public override void RemoveBlockLight(byte[] oldLightHsV, BlockPos pos)
	{
		lock (LightingTasksLock)
		{
			LightingTasks.Enqueue(new UpdateLightingTask
			{
				removeLightHsv = oldLightHsV,
				pos = pos.Copy()
			});
		}
		server.BroadcastPacket(new Packet_Server
		{
			Id = 72,
			RemoveBlockLight = new Packet_RemoveBlockLight
			{
				PosX = pos.X,
				PosY = pos.Y,
				PosZ = pos.Z,
				LightH = oldLightHsV[0],
				LightS = oldLightHsV[1],
				LightV = oldLightHsV[2]
			}
		});
	}

	public override void UpdateLightingAfterAbsorptionChange(int oldAbsorption, int newAbsorption, BlockPos pos)
	{
		long key = server.WorldMap.MapChunkIndex2D(pos.X / 32, pos.Z / 32);
		server.loadedMapChunks.TryGetValue(key, out var value);
		if (value == null)
		{
			return;
		}
		value.MarkFresh();
		lock (LightingTasksLock)
		{
			LightingTasks.Enqueue(new UpdateLightingTask
			{
				oldBlockId = 0,
				newBlockId = 0,
				oldAbsorb = (byte)oldAbsorption,
				newAbsorb = (byte)newAbsorption,
				pos = pos.Copy(),
				absorbUpdate = true
			});
		}
	}

	public override void UpdateLightingBulk(Dictionary<BlockPos, BlockUpdate> blockUpdates)
	{
		foreach (KeyValuePair<BlockPos, BlockUpdate> blockUpdate in blockUpdates)
		{
			int num = blockUpdate.Value.NewFluidBlockId;
			if (num < 0)
			{
				num = blockUpdate.Value.NewSolidBlockId;
			}
			if (num >= 0)
			{
				UpdateLighting(blockUpdate.Value.OldBlockId, num, blockUpdate.Key);
			}
		}
	}

	public float? GetMaxTimeAwareLightLevelAt(int posX, int posY, int posZ)
	{
		if (!IsValidPos(posX, posY, posZ))
		{
			return server.SunBrightness;
		}
		IWorldChunk chunkAtPos = GetChunkAtPos(posX, posY, posZ);
		if (chunkAtPos == null)
		{
			return null;
		}
		ushort num = chunkAtPos.Unpack_AndReadLight(ChunkSizedIndex3D(posX % 32, posY % 32, posZ % 32));
		float dayLightStrength = server.Calendar.GetDayLightStrength(posX, posZ);
		return Math.Max((float)(num & 0x1F) * dayLightStrength, (num >> 5) & 0x1F);
	}

	public override void PrintChunkMap(Vec2i markChunkPos = null)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		SKBitmap val = new SKBitmap(server.WorldMap.ChunkMapSizeX, server.WorldMap.ChunkMapSizeZ, false);
		SKColor val2 = default(SKColor);
		((SKColor)(ref val2))._002Ector((byte)0, byte.MaxValue, (byte)0, byte.MaxValue);
		server.loadedChunksLock.AcquireReadLock();
		try
		{
			foreach (long key in server.loadedChunks.Keys)
			{
				ChunkPos chunkPos = server.WorldMap.ChunkPosFromChunkIndex3D(key);
				if (chunkPos.Dimension <= 0)
				{
					val.SetPixel(chunkPos.X, chunkPos.Z, val2);
				}
			}
		}
		finally
		{
			server.loadedChunksLock.ReleaseReadLock();
		}
		int num = 0;
		while (File.Exists("serverchunks" + num + ".png"))
		{
			num++;
		}
		if (markChunkPos != null)
		{
			val.SetPixel(markChunkPos.X, markChunkPos.Y, new SKColor(byte.MaxValue, (byte)0, (byte)0, byte.MaxValue));
		}
		val.Save("serverchunks" + num + ".png");
	}

	IWorldChunk IChunkProvider.GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		return GetServerChunk(chunkX, chunkY, chunkZ);
	}

	public override BlockEntity GetBlockEntity(BlockPos position)
	{
		return GetChunk(position)?.GetLocalBlockEntityAtBlockPos(position);
	}

	public override void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null)
	{
		WorldChunk chunk = GetChunk(position);
		if (chunk != null)
		{
			if (chunk.GetLocalBlockEntityAtBlockPos(position) != null)
			{
				RemoveBlockEntity(position);
			}
			Block localBlockAtBlockPos = chunk.GetLocalBlockAtBlockPos(server, position);
			BlockEntity blockEntity = ServerMain.ClassRegistry.CreateBlockEntity(classname);
			blockEntity.Pos = position.Copy();
			blockEntity.CreateBehaviors(localBlockAtBlockPos, server);
			blockEntity.Initialize(server.api);
			chunk.AddBlockEntity(blockEntity);
			blockEntity.OnBlockPlaced(byItemStack);
			chunk.MarkModified();
			MarkBlockEntityDirty(blockEntity.Pos);
		}
	}

	public override void SpawnBlockEntity(BlockEntity be)
	{
		WorldChunk chunk = GetChunk(be.Pos);
		if (chunk != null)
		{
			if (chunk.GetLocalBlockEntityAtBlockPos(be.Pos) != null)
			{
				RemoveBlockEntity(be.Pos);
			}
			chunk.AddBlockEntity(be);
			chunk.MarkModified();
			MarkBlockEntityDirty(be.Pos);
		}
	}

	public override void RemoveBlockEntity(BlockPos pos)
	{
		WorldChunk chunk = GetChunk(pos);
		if (chunk != null)
		{
			BlockEntity blockEntity = GetBlockEntity(pos);
			chunk.RemoveBlockEntity(pos);
			blockEntity?.OnBlockRemoved();
			chunk.MarkModified();
		}
	}

	public override void MarkBlockModified(BlockPos pos, bool doRelight = true)
	{
		if (doRelight)
		{
			server.ModifiedBlocks.Enqueue(pos);
		}
		else
		{
			server.ModifiedBlocksNoRelight.Enqueue(pos);
		}
	}

	public override void MarkBlockDirty(BlockPos pos, Action onRetesselated)
	{
		server.DirtyBlocks.Enqueue(new Vec4i(pos, -1));
	}

	public override void MarkBlockDirty(BlockPos pos, IPlayer skipPlayer = null)
	{
		server.DirtyBlocks.Enqueue(new Vec4i(pos, (skipPlayer == null) ? (-1) : (skipPlayer as ServerPlayer).ClientId));
	}

	public override void MarkBlockEntityDirty(BlockPos pos)
	{
		server.DirtyBlockEntities.Enqueue(pos.Copy());
		GetServerChunk(pos.X / 32, pos.InternalY / 32, pos.Z / 32)?.MarkModified();
	}

	public override void MarkDecorsDirty(BlockPos pos)
	{
		server.ModifiedDecors.Enqueue(pos.Copy());
	}

	public override void TriggerNeighbourBlockUpdate(BlockPos pos)
	{
		server.UpdatedBlocks.Enqueue(pos.Copy());
	}

	public override ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.NowValues, double totalDays = 0.0)
	{
		ClimateCondition climate = getWorldGenClimateAt(pos, mode >= EnumGetClimateMode.ForSuppliedDate_TemperatureOnly);
		if (climate == null)
		{
			if (mode != EnumGetClimateMode.ForSuppliedDate_TemperatureOnly)
			{
				return null;
			}
			return new ClimateCondition
			{
				Temperature = 4f,
				WorldGenTemperature = 4f
			};
		}
		if (mode == EnumGetClimateMode.NowValues)
		{
			totalDays = server.Calendar.TotalDays;
		}
		server.EventManager.TriggerOnGetClimate(ref climate, pos, mode, totalDays);
		return climate;
	}

	public override ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition baseClimate, EnumGetClimateMode mode, double totalDays)
	{
		baseClimate.Temperature = baseClimate.WorldGenTemperature;
		baseClimate.Rainfall = baseClimate.WorldgenRainfall;
		server.EventManager.TriggerOnGetClimate(ref baseClimate, pos, mode, totalDays);
		return baseClimate;
	}

	public override ClimateCondition GetClimateAt(BlockPos pos, int climate)
	{
		float scaledAdjustedTemperatureFloat = Climate.GetScaledAdjustedTemperatureFloat((climate >> 16) & 0xFF, pos.Y - server.seaLevel);
		float num = (float)Climate.GetRainFall((climate >> 8) & 0xFF, pos.Y) / 255f;
		float posYRel = ((float)pos.Y - (float)server.seaLevel) / ((float)MapSizeY - (float)server.seaLevel);
		ClimateCondition climate2 = new ClimateCondition
		{
			Temperature = scaledAdjustedTemperatureFloat,
			Rainfall = num,
			Fertility = (float)Climate.GetFertility((int)num, scaledAdjustedTemperatureFloat, posYRel) / 255f
		};
		server.EventManager.TriggerOnGetClimate(ref climate2, pos, EnumGetClimateMode.NowValues, server.Calendar.TotalDays);
		return climate2;
	}

	public override Vec3d GetWindSpeedAt(BlockPos pos)
	{
		return GetWindSpeedAt(new Vec3d(pos.X, pos.Y, pos.Z));
	}

	public override Vec3d GetWindSpeedAt(Vec3d pos)
	{
		Vec3d windSpeed = new Vec3d();
		server.EventManager.TriggerOnGetWindSpeed(pos, ref windSpeed);
		return windSpeed;
	}

	public ClimateCondition getWorldGenClimateAt(BlockPos pos, bool temperatureRainfallOnly)
	{
		if (!IsValidPos(pos))
		{
			return null;
		}
		IMapRegion mapRegion = GetMapRegion(pos);
		if (mapRegion?.ClimateMap?.Data == null || mapRegion.ClimateMap.Size == 0)
		{
			return null;
		}
		float x = (float)((double)pos.X / (double)RegionSize % 1.0);
		float z = (float)((double)pos.Z / (double)RegionSize % 1.0);
		int unpaddedColorLerpedForNormalizedPos = mapRegion.ClimateMap.GetUnpaddedColorLerpedForNormalizedPos(x, z);
		float scaledAdjustedTemperatureFloat = Climate.GetScaledAdjustedTemperatureFloat((unpaddedColorLerpedForNormalizedPos >> 16) & 0xFF, pos.Y - server.seaLevel);
		float num = Climate.GetRainFall((unpaddedColorLerpedForNormalizedPos >> 8) & 0xFF, pos.Y);
		int rain = (int)num;
		num /= 255f;
		ClimateCondition climateCondition = new ClimateCondition
		{
			Temperature = scaledAdjustedTemperatureFloat,
			Rainfall = num,
			WorldgenRainfall = num,
			WorldGenTemperature = scaledAdjustedTemperatureFloat
		};
		if (!temperatureRainfallOnly)
		{
			float posYRel = ((float)pos.Y - (float)server.seaLevel) / ((float)MapSizeY - (float)server.seaLevel);
			climateCondition.Fertility = (float)Climate.GetFertilityFromUnscaledTemp(rain, (unpaddedColorLerpedForNormalizedPos >> 16) & 0xFF, posYRel) / 255f;
			climateCondition.GeologicActivity = (float)(unpaddedColorLerpedForNormalizedPos & 0xFF) / 255f;
			AddWorldGenForestShrub(climateCondition, mapRegion, pos);
		}
		return climateCondition;
	}

	public void AddWorldGenForestShrub(ClimateCondition conds, IMapRegion mapregion, BlockPos pos)
	{
		float x = (float)((double)pos.X / (double)RegionSize % 1.0);
		float z = (float)((double)pos.Z / (double)RegionSize % 1.0);
		int unpaddedColorLerpedForNormalizedPos = mapregion.ForestMap.GetUnpaddedColorLerpedForNormalizedPos(x, z);
		conds.ForestDensity = (float)unpaddedColorLerpedForNormalizedPos / 255f;
		int num = mapregion.ShrubMap.GetUnpaddedColorLerpedForNormalizedPos(x, z) & 0xFF;
		conds.ShrubDensity = (float)num / 255f;
	}

	public long ChunkIndex3dToIndex2d(long index3d)
	{
		long num = index3d % index3dMulX;
		return index3d / index3dMulX % index3dMulZ * base.ChunkMapSizeX + num;
	}

	public override void DamageBlock(BlockPos pos, BlockFacing facing, float damage, IPlayer dualCallByPlayer = null)
	{
		Packet_Server packet = new Packet_Server
		{
			Id = 64,
			BlockDamage = new Packet_BlockDamage
			{
				PosX = pos.X,
				PosY = pos.Y,
				PosZ = pos.Z,
				Damage = CollectibleNet.SerializeFloat(damage),
				Facing = facing.Index
			}
		};
		foreach (ConnectedClient value in server.Clients.Values)
		{
			if (value.ShouldReceiveUpdatesForPos(pos) && (dualCallByPlayer == null || value.Id != dualCallByPlayer.ClientId))
			{
				server.SendPacket(value.Id, packet);
			}
		}
	}

	public void Add(LandClaim claim)
	{
		HashSet<long> hashSet = new HashSet<long>();
		int regionSize = server.WorldMap.RegionSize;
		foreach (Cuboidi area in claim.Areas)
		{
			int num = area.MinX / regionSize;
			int num2 = area.MaxX / regionSize;
			int num3 = area.MinZ / regionSize;
			int num4 = area.MaxZ / regionSize;
			for (int i = num; i <= num2; i++)
			{
				for (int j = num3; j <= num4; j++)
				{
					hashSet.Add(server.WorldMap.MapRegionIndex2D(i, j));
				}
			}
		}
		foreach (long item in hashSet)
		{
			if (!LandClaimByRegion.TryGetValue(item, out var value))
			{
				value = (LandClaimByRegion[item] = new List<LandClaim>());
			}
			value.Add(claim);
		}
		landClaims.Add(claim);
		BroadcastClaims(null, new LandClaim[1] { claim });
	}

	public bool Remove(LandClaim claim)
	{
		foreach (KeyValuePair<long, List<LandClaim>> item in LandClaimByRegion)
		{
			item.Value.Remove(claim);
		}
		bool num = landClaims.Remove(claim);
		if (num)
		{
			BroadcastClaims(landClaims, null);
		}
		return num;
	}

	public void UpdateClaim(LandClaim oldClaim, LandClaim newClaim)
	{
		Remove(oldClaim);
		Add(newClaim);
		BroadcastClaims(landClaims, null);
	}

	public void BroadcastClaims(IEnumerable<LandClaim> allClaims, IEnumerable<LandClaim> addClaims)
	{
		Packet_LandClaims packet_LandClaims = new Packet_LandClaims();
		if (allClaims != null)
		{
			packet_LandClaims.SetAllclaims(allClaims.Select(delegate(LandClaim claim)
			{
				Packet_LandClaim packet_LandClaim = new Packet_LandClaim();
				packet_LandClaim.SetData(SerializerUtil.Serialize(claim));
				return packet_LandClaim;
			}).ToArray());
		}
		if (addClaims != null)
		{
			packet_LandClaims.SetAddclaims(addClaims.Select(delegate(LandClaim claim)
			{
				Packet_LandClaim packet_LandClaim = new Packet_LandClaim();
				packet_LandClaim.SetData(SerializerUtil.Serialize(claim));
				return packet_LandClaim;
			}).ToArray());
		}
		server.BroadcastPacket(new Packet_Server
		{
			Id = 75,
			LandClaims = packet_LandClaims
		});
	}

	public void SendClaims(IServerPlayer player, IEnumerable<LandClaim> allClaims, IEnumerable<LandClaim> addClaims)
	{
		Packet_LandClaims packet_LandClaims = new Packet_LandClaims();
		if (allClaims != null)
		{
			packet_LandClaims.SetAllclaims(allClaims.Select(delegate(LandClaim claim)
			{
				Packet_LandClaim packet_LandClaim = new Packet_LandClaim();
				packet_LandClaim.SetData(SerializerUtil.Serialize(claim));
				return packet_LandClaim;
			}).ToArray());
		}
		if (addClaims != null)
		{
			packet_LandClaims.SetAddclaims(addClaims.Select(delegate(LandClaim claim)
			{
				Packet_LandClaim packet_LandClaim = new Packet_LandClaim();
				packet_LandClaim.SetData(SerializerUtil.Serialize(claim));
				return packet_LandClaim;
			}).ToArray());
		}
		server.SendPacket(player, new Packet_Server
		{
			Id = 75,
			LandClaims = packet_LandClaims
		});
	}
}
