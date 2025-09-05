using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public interface IWorldAccessor
{
	ITreeAttribute Config { get; }

	EntityPos DefaultSpawnPosition { get; }

	FrameProfilerUtil FrameProfiler { get; }

	ICoreAPI Api { get; }

	IChunkProvider ChunkProvider { get; }

	ILandClaimAPI Claims { get; }

	long[] LoadedChunkIndices { get; }

	long[] LoadedMapChunkIndices { get; }

	float[] BlockLightLevels { get; }

	float[] SunLightLevels { get; }

	int SeaLevel { get; }

	int Seed { get; }

	string SavegameIdentifier { get; }

	int SunBrightness { get; }

	bool EntityDebugMode { get; }

	IAssetManager AssetManager { get; }

	ILogger Logger { get; }

	EnumAppSide Side { get; }

	IBlockAccessor BlockAccessor { get; }

	IBulkBlockAccessor BulkBlockAccessor { get; }

	IClassRegistryAPI ClassRegistry { get; }

	IGameCalendar Calendar { get; }

	CollisionTester CollisionTester { get; }

	Random Rand { get; }

	long ElapsedMilliseconds { get; }

	List<CollectibleObject> Collectibles { get; }

	IList<Block> Blocks { get; }

	IList<Item> Items { get; }

	List<EntityProperties> EntityTypes { get; }

	List<string> EntityTypeCodes { get; }

	List<GridRecipe> GridRecipes { get; }

	int DefaultEntityTrackingRange { get; }

	IPlayer[] AllOnlinePlayers { get; }

	IPlayer[] AllPlayers { get; }

	AABBIntersectionTest InteresectionTester { get; }

	RecipeRegistryBase GetRecipeRegistry(string code);

	Item GetItem(int itemId);

	Block GetBlock(int blockId);

	Block[] SearchBlocks(AssetLocation wildcard);

	Item[] SearchItems(AssetLocation wildcard);

	Item GetItem(AssetLocation itemCode);

	Block GetBlock(AssetLocation blockCode);

	EntityProperties GetEntityType(AssetLocation entityCode);

	Entity SpawnItemEntity(ItemStack itemstack, Vec3d position, Vec3d velocity = null);

	Entity SpawnItemEntity(ItemStack itemstack, BlockPos pos, Vec3d velocity = null);

	void SpawnEntity(Entity entity);

	void SpawnPriorityEntity(Entity entity);

	bool LoadEntity(Entity entity, long fromChunkIndex3d);

	void UpdateEntityChunk(Entity entity, long newChunkIndex3d);

	Entity[] GetEntitiesAround(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null);

	Entity[] GetEntitiesInsideCuboid(BlockPos startPos, BlockPos endPos, ActionConsumable<Entity> matches = null);

	IPlayer[] GetPlayersAround(Vec3d position, float horRange, float vertRange, ActionConsumable<IPlayer> matches = null);

	Entity GetNearestEntity(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null);

	Entity GetEntityById(long entityId);

	Entity[] GetIntersectingEntities(BlockPos basePos, Cuboidf[] collisionBoxes, ActionConsumable<Entity> matches = null);

	IPlayer NearestPlayer(double x, double y, double z);

	IPlayer PlayerByUid(string playerUid);

	void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f);

	void PlaySoundAt(AssetLocation location, BlockPos pos, double yOffsetFromCenter, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f);

	void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f);

	void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer dualCallByPlayer, float pitch, float range = 32f, float volume = 1f);

	void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, float pitch, float range = 32f, float volume = 1f);

	void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, EnumSoundType soundType, float pitch, float range = 32f, float volume = 1f);

	void PlaySoundAt(AssetLocation location, IPlayer atPlayer, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f);

	void PlaySoundFor(AssetLocation location, IPlayer forPlayer, bool randomizePitch = true, float range = 32f, float volume = 1f);

	void PlaySoundFor(AssetLocation location, IPlayer forPlayer, float pitch, float range = 32f, float volume = 1f);

	void SpawnParticles(float quantity, int color, Vec3d minPos, Vec3d maxPos, Vec3f minVelocity, Vec3f maxVelocity, float lifeLength, float gravityEffect, float scale = 1f, EnumParticleModel model = EnumParticleModel.Quad, IPlayer dualCallByPlayer = null);

	void SpawnParticles(IParticlePropertiesProvider particlePropertiesProvider, IPlayer dualCallByPlayer = null);

	void SpawnCubeParticles(BlockPos blockPos, Vec3d pos, float radius, int quantity, float scale = 1f, IPlayer dualCallByPlayer = null, Vec3f velocity = null);

	void SpawnCubeParticles(Vec3d pos, ItemStack item, float radius, int quantity, float scale = 1f, IPlayer dualCallByPlayer = null, Vec3f velocity = null);

	void RayTraceForSelection(Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null);

	void RayTraceForSelection(IWorldIntersectionSupplier supplier, Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null);

	void RayTraceForSelection(Vec3d fromPos, float pitch, float yaw, float range, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null);

	void RayTraceForSelection(Ray ray, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter filter = null, EntityFilter efilter = null);

	long RegisterGameTickListener(Action<float> onGameTick, int millisecondInterval, int initialDelayOffsetMs = 0);

	void UnregisterGameTickListener(long listenerId);

	long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay);

	long RegisterCallbackUnique(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval);

	long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay);

	bool PlayerHasPrivilege(int clientid, string privilege);

	void UnregisterCallback(long listenerId);

	void HighlightBlocks(IPlayer player, int highlightSlotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1f);

	void HighlightBlocks(IPlayer player, int highlightSlotId, List<BlockPos> blocks, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary);

	IBlockAccessor GetBlockAccessor(bool synchronize, bool relight, bool strict, bool debug = false);

	IBulkBlockAccessor GetBlockAccessorBulkUpdate(bool synchronize, bool relight, bool debug = false);

	IBulkBlockAccessor GetBlockAccessorBulkMinimalUpdate(bool synchronize, bool debug = false);

	IBulkBlockAccessor GetBlockAccessorMapChunkLoading(bool synchronize, bool debug = false);

	IBlockAccessorRevertable GetBlockAccessorRevertable(bool synchronize, bool relight, bool debug = false);

	IBlockAccessorPrefetch GetBlockAccessorPrefetch(bool synchronize, bool relight);

	ICachingBlockAccessor GetCachingBlockAccessor(bool synchronize, bool relight);

	IBlockAccessor GetLockFreeBlockAccessor();
}
