using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IBlockAccessor
{
	[Obsolete("Use GlobalConstants.ChunkSize instead.  Fetching a property in inner-loop code is needlessly inefficient!")]
	int ChunkSize { get; }

	int RegionSize { get; }

	int MapSizeX { get; }

	int MapSizeY { get; }

	int MapSizeZ { get; }

	int RegionMapSizeX { get; }

	int RegionMapSizeY { get; }

	int RegionMapSizeZ { get; }

	bool UpdateSnowAccumMap { get; set; }

	Vec3i MapSize { get; }

	IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ);

	IWorldChunk GetChunk(long chunkIndex3D);

	IMapRegion GetMapRegion(int regionX, int regionZ);

	IWorldChunk GetChunkAtBlockPos(BlockPos pos);

	int GetBlockId(BlockPos pos);

	Block GetBlock(BlockPos pos);

	Block GetBlockRaw(int x, int y, int z, int layer = 0);

	Block GetBlock(BlockPos pos, int layer);

	Block GetMostSolidBlock(BlockPos pos);

	[Obsolete("Please use BlockPos version instead, for dimension awareness")]
	IWorldChunk GetChunkAtBlockPos(int posX, int posY, int posZ);

	[Obsolete("Please use BlockPos version instead, for dimension awareness")]
	int GetBlockId(int posX, int posY, int posZ);

	[Obsolete("Please use BlockPos version instead, for dimension awareness")]
	Block GetBlockOrNull(int x, int y, int z, int layer = 4);

	[Obsolete("Please use BlockPos version instead, for dimension awareness")]
	Block GetBlock(int x, int y, int z, int layer);

	[Obsolete("Please use BlockPos version instead, for dimension awareness")]
	Block GetBlock(int posX, int posY, int posZ);

	[Obsolete("Please use BlockPos version instead, for dimension awareness")]
	Block GetMostSolidBlock(int x, int y, int z);

	void WalkBlocks(BlockPos minPos, BlockPos maxPos, Action<Block, int, int, int> onBlock, bool centerOrder = false);

	void SearchBlocks(BlockPos minPos, BlockPos maxPos, ActionConsumable<Block, BlockPos> onBlock, Action<int, int, int> onChunkMissing = null);

	void SearchFluidBlocks(BlockPos minPos, BlockPos maxPos, ActionConsumable<Block, BlockPos> onBlock, Action<int, int, int> onChunkMissing = null);

	void WalkStructures(BlockPos pos, Action<GeneratedStructure> onStructure);

	void WalkStructures(BlockPos minpos, BlockPos maxpos, Action<GeneratedStructure> onStructure);

	void SetBlock(int blockId, BlockPos pos);

	void SetBlock(int blockId, BlockPos pos, int layer);

	void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack);

	void ExchangeBlock(int blockId, BlockPos pos);

	void BreakBlock(BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f);

	void DamageBlock(BlockPos pos, BlockFacing facing, float damage);

	Block GetBlock(int blockId);

	Block GetBlock(AssetLocation code);

	void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null);

	void SpawnBlockEntity(BlockEntity be);

	void RemoveBlockEntity(BlockPos position);

	BlockEntity GetBlockEntity(BlockPos position);

	T GetBlockEntity<T>(BlockPos position) where T : BlockEntity;

	[Obsolete("Please use BlockPos version instead, for dimension awareness")]
	bool IsValidPos(int posX, int posY, int posZ);

	bool IsValidPos(BlockPos pos);

	[Obsolete("Better to use dimension-aware version")]
	bool IsNotTraversable(double x, double y, double z);

	bool IsNotTraversable(double x, double y, double z, int dim);

	bool IsNotTraversable(BlockPos pos);

	List<BlockUpdate> Commit();

	void Rollback();

	void MarkBlockEntityDirty(BlockPos pos);

	void TriggerNeighbourBlockUpdate(BlockPos pos);

	void MarkBlockDirty(BlockPos pos, IPlayer skipPlayer = null);

	void MarkBlockModified(BlockPos pos);

	void MarkBlockDirty(BlockPos pos, Action OnRetesselated);

	int GetLightLevel(BlockPos pos, EnumLightLevelType type);

	int GetLightLevel(int x, int y, int z, EnumLightLevelType type);

	Vec4f GetLightRGBs(int posX, int posY, int posZ);

	Vec4f GetLightRGBs(BlockPos pos);

	int GetLightRGBsAsInt(int posX, int posY, int posZ);

	int GetTerrainMapheightAt(BlockPos pos);

	int GetRainMapHeightAt(BlockPos pos);

	int GetDistanceToRainFall(BlockPos pos, int horziontalSearchWidth = 4, int verticalSearchWidth = 1);

	int GetRainMapHeightAt(int posX, int posZ);

	IMapChunk GetMapChunk(Vec2i chunkPos);

	IMapChunk GetMapChunk(int chunkX, int chunkZ);

	IMapChunk GetMapChunkAtBlockPos(BlockPos pos);

	ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.NowValues, double totalDays = 0.0);

	ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition baseClimate, EnumGetClimateMode mode, double totalDays);

	ClimateCondition GetClimateAt(BlockPos pos, int climate);

	Vec3d GetWindSpeedAt(Vec3d pos);

	Vec3d GetWindSpeedAt(BlockPos pos);

	void MarkAbsorptionChanged(int oldAbsorption, int newAbsorption, BlockPos pos);

	void RemoveBlockLight(byte[] oldLightHsV, BlockPos pos);

	bool SetDecor(Block block, BlockPos position, BlockFacing onFace);

	bool SetDecor(Block block, BlockPos position, int decorIndex);

	[Obsolete("Use Dictionary<int, Block> GetSubDecors(BlockPos position)")]
	Block[] GetDecors(BlockPos position);

	Dictionary<int, Block> GetSubDecors(BlockPos position);

	Block GetDecor(BlockPos pos, int decorIndex);

	bool BreakDecor(BlockPos pos, BlockFacing side = null, int? decorIndex = null);

	void MarkChunkDecorsModified(BlockPos pos);

	bool IsSideSolid(int x, int y, int z, BlockFacing facing);

	IMiniDimension CreateMiniDimension(Vec3d position);
}
