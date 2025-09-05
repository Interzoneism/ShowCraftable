using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

internal class DecorSelectionBox : Cuboidf
{
	public Vec3i PosAdjust;

	public DecorSelectionBox(float x1, float y1, float z1, float x2, float y2, float z2)
		: base(x1, y1, z1, x2, y2, z2)
	{
	}
}
