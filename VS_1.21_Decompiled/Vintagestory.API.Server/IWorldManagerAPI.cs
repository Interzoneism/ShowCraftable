using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Server;

public interface IWorldManagerAPI
{
	Dictionary<long, IMapChunk> AllLoadedMapchunks { get; }

	Dictionary<long, IMapRegion> AllLoadedMapRegions { get; }

	Dictionary<long, IServerChunk> AllLoadedChunks { get; }

	int CurrentGeneratingChunkCount { get; }

	int ChunkDeletionsInQueue { get; }

	ISaveGame SaveGame { get; }

	PlayStyle CurrentPlayStyle { get; }

	List<LandClaim> LandClaims { get; }

	bool AutoGenerateChunks { get; set; }

	bool SendChunks { get; set; }

	int MapSizeX { get; }

	int MapSizeY { get; }

	int MapSizeZ { get; }

	int RegionSize { get; }

	int ChunkSize { get; }

	int Seed { get; }

	string CurrentWorldName { get; }

	int[] DefaultSpawnPosition { get; }

	void SetBlockLightLevels(float[] lightLevels);

	void SetSunLightLevels(float[] lightLevels);

	void SetSunBrightness(int lightlevel);

	void SetSeaLevel(int sealevel);

	IMapRegion GetMapRegion(int regionX, int regionZ);

	IMapRegion GetMapRegion(long index2d);

	IServerMapChunk GetMapChunk(int chunkX, int chunkZ);

	IMapChunk GetMapChunk(long index2d);

	IServerChunk GetChunk(int chunkX, int chunkY, int chunkZ);

	IServerChunk GetChunk(BlockPos pos);

	long ChunkIndex3D(int chunkX, int chunkY, int chunkZ);

	long MapRegionIndex2D(int regionX, int regionZ);

	long MapRegionIndex2DByBlockPos(int posX, int posZ);

	Vec3i MapRegionPosFromIndex2D(long index2d);

	Vec2i MapChunkPosFromChunkIndex2D(long index2d);

	long MapChunkIndex2D(int chunkX, int chunkZ);

	IServerChunk GetChunk(long index3d);

	long GetNextUniqueId();

	[Obsolete("Use LoadChunkColumnPriority()")]
	void LoadChunkColumnFast(int chunkX, int chunkZ, ChunkLoadOptions options = null);

	void LoadChunkColumnPriority(int chunkX, int chunkZ, ChunkLoadOptions options = null);

	[Obsolete("Use LoadChunkColumnPriority()")]
	void LoadChunkColumnFast(int chunkX1, int chunkZ1, int chunkX2, int chunkZ2, ChunkLoadOptions options = null);

	void LoadChunkColumnPriority(int chunkX1, int chunkZ1, int chunkX2, int chunkZ2, ChunkLoadOptions options = null);

	void LoadChunkColumn(int chunkX, int chunkZ, bool keepLoaded = false);

	void PeekChunkColumn(int chunkX, int chunkZ, ChunkPeekOptions options);

	void TestChunkExists(int chunkX, int chunkY, int chunkZ, Action<bool> onTested);

	void TestMapChunkExists(int chunkX, int chunkZ, Action<bool> onTested);

	void TestMapRegionExists(int regionX, int regionZ, Action<bool> onTested);

	void BroadcastChunk(int chunkX, int chunkY, int chunkZ, bool onlyIfInRange = true);

	bool HasChunk(int chunkX, int chunkY, int chunkZ, IServerPlayer player);

	void SendChunk(int chunkX, int chunkY, int chunkZ, IServerPlayer player, bool onlyIfInRange = true);

	void ResendMapChunk(int chunkX, int chunkZ, bool onlyIfInRange);

	void UnloadChunkColumn(int chunkX, int chunkZ);

	void DeleteChunkColumn(int chunkX, int chunkZ);

	void DeleteMapRegion(int regionX, int regionZ);

	int? GetSurfacePosY(int posX, int posZ);

	void SetDefaultSpawnPosition(int x, int y, int z);

	int GetBlockId(AssetLocation name);

	void SunFloodChunkColumnForWorldGen(IWorldChunk[] chunks, int chunkX, int chunkZ);

	void SunFloodChunkColumnNeighboursForWorldGen(IWorldChunk[] chunks, int chunkX, int chunkZ);

	void FullRelight(BlockPos minPos, BlockPos maxPos);

	void FullRelight(BlockPos minPos, BlockPos maxPos, bool sendToClients);

	[Obsolete("Use api.World.GetBlockAccessor instead")]
	IBlockAccessor GetBlockAccessor(bool synchronize, bool relight, bool strict, bool debug = false);

	[Obsolete("Use api.World.GetBlockAccessorBulkUpdate instead")]
	IBulkBlockAccessor GetBlockAccessorBulkUpdate(bool synchronize, bool relight, bool debug = false);

	[Obsolete("Use api.World.GetBlockAccessorRevertable instead")]
	IBlockAccessorRevertable GetBlockAccessorRevertable(bool synchronize, bool relight, bool debug = false);

	[Obsolete("Use api.World.GetBlockAccessorPrefetch instead")]
	IBlockAccessorPrefetch GetBlockAccessorPrefetch(bool synchronize, bool relight);

	[Obsolete("Use api.World.GetCachingBlockAccessor instead")]
	ICachingBlockAccessor GetCachingBlockAccessor(bool synchronize, bool relight);

	void CreateChunkColumnForDimension(int cx, int cz, int dim);

	void LoadChunkColumnForDimension(int cx, int cz, int dim);

	void ForceSendChunkColumn(IServerPlayer player, int cx, int cz, int dimension);

	bool BlockingTestMapRegionExists(int regionX, int regionZ);

	bool BlockingTestMapChunkExists(int chunkX, int chunkZ);

	IServerChunk[] BlockingLoadChunkColumn(int chunkX, int chunkZ);
}
