using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IBlockAccessorPrefetch : IBlockAccessor
{
	void PrefetchBlocks(BlockPos minPos, BlockPos maxPos);
}
