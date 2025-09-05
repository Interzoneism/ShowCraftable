using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IGeometryTester
{
	BlockEntity GetCurrentBlockEntityOnSide(BlockFacing side);

	BlockEntity GetCurrentBlockEntityOnSide(Vec3iAndFacingFlags vec);
}
