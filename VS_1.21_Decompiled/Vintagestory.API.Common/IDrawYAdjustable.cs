using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IDrawYAdjustable
{
	float AdjustYPosition(BlockPos pos, Block[] chunkExtBlocks, int extIndex3d);
}
