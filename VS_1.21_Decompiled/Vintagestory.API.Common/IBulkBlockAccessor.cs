using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IBulkBlockAccessor : IBlockAccessor
{
	Dictionary<BlockPos, BlockUpdate> StagedBlocks { get; }

	bool ReadFromStagedByDefault { get; set; }

	event Action<IBulkBlockAccessor> BeforeCommit;

	int GetStagedBlockId(int posX, int posY, int posZ);

	int GetStagedBlockId(BlockPos pos);

	void SetChunks(Vec2i chunkCoord, IWorldChunk[] chunksCol);

	void SetDecorsBulk(long chunkIndex, Dictionary<int, Block> newDecors);

	void PostCommitCleanup(List<BlockUpdate> updatedBlocks);
}
