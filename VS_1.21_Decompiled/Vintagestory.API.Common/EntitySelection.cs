using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class EntitySelection
{
	public Entity Entity;

	public Vec3d Position;

	public BlockFacing Face;

	public Vec3d HitPosition;

	public int SelectionBoxIndex;

	public EntitySelection Clone()
	{
		return new EntitySelection
		{
			Entity = Entity,
			Position = Position.Clone(),
			Face = Face,
			HitPosition = HitPosition.Clone(),
			SelectionBoxIndex = SelectionBoxIndex
		};
	}
}
