using System.Collections.Generic;

namespace Vintagestory.API.Common;

public interface IChunkBlocks
{
	int this[int index3d] { get; set; }

	int Length { get; }

	void ClearBlocks();

	void ClearBlocksAndPrepare();

	void SetBlockBulk(int index3d, int lenX, int lenZ, int value);

	void SetBlockUnsafe(int index3d, int value);

	void SetBlockAir(int index3d);

	void SetFluid(int index3d, int value);

	int GetBlockId(int index3d, int layer);

	int GetFluid(int index3d);

	int GetBlockIdUnsafe(int index3d);

	void TakeBulkReadLock();

	void ReleaseBulkReadLock();

	bool ContainsBlock(int blockId);

	void FuzzyListBlockIds(List<int> reusableList);
}
