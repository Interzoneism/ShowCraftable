using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IWorldChunk
{
	bool Empty { get; set; }

	IMapChunk MapChunk { get; }

	IChunkBlocks Data { get; }

	[Obsolete("Use Data field")]
	IChunkBlocks Blocks { get; }

	IChunkLight Lighting { get; }

	IChunkBlocks MaybeBlocks { get; }

	Entity[] Entities { get; }

	int EntitiesCount { get; }

	Dictionary<BlockPos, BlockEntity> BlockEntities { get; set; }

	HashSet<int> LightPositions { get; set; }

	bool Disposed { get; }

	Dictionary<string, object> LiveModData { get; set; }

	void Unpack();

	bool Unpack_ReadOnly();

	int UnpackAndReadBlock(int index, int layer);

	ushort Unpack_AndReadLight(int index);

	ushort Unpack_AndReadLight(int index, out int lightSat);

	void MarkModified();

	void MarkFresh();

	void AddEntity(Entity entity);

	bool RemoveEntity(long entityId);

	void SetModdata(string key, byte[] data);

	void RemoveModdata(string key);

	byte[] GetModdata(string key);

	void SetModdata<T>(string key, T data);

	T GetModdata<T>(string key, T defaultValue = default(T));

	Block GetLocalBlockAtBlockPos(IWorldAccessor world, BlockPos position);

	Block GetLocalBlockAtBlockPos(IWorldAccessor world, int posX, int posY, int posZ, int layer);

	Block GetLocalBlockAtBlockPos_LockFree(IWorldAccessor world, BlockPos position, int layer = 0);

	BlockEntity GetLocalBlockEntityAtBlockPos(BlockPos pos);

	bool SetDecor(Block block, int index3d, BlockFacing onFace);

	bool SetDecor(Block block, int index3d, int faceAndSubposition);

	bool BreakDecor(IWorldAccessor world, BlockPos pos, BlockFacing side = null, int? decorIndex = null);

	void BreakAllDecorFast(IWorldAccessor world, BlockPos pos, int index3d, bool callOnBrokenAsDecor = true);

	Block[] GetDecors(IBlockAccessor blockAccessor, BlockPos pos);

	Dictionary<int, Block> GetSubDecors(IBlockAccessor blockAccessor, BlockPos position);

	Block GetDecor(IBlockAccessor blockAccessor, BlockPos pos, int decorIndex);

	void SetDecors(Dictionary<int, Block> newDecors);

	Cuboidf[] AdjustSelectionBoxForDecor(IBlockAccessor blockAccessor, BlockPos pos, Cuboidf[] orig);

	void FinishLightDoubleBuffering();

	int GetLightAbsorptionAt(int index3d, BlockPos blockPos, IList<Block> blockTypes);

	void AcquireBlockReadLock();

	void ReleaseBlockReadLock();

	void Dispose();
}
