using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Common;

public class EntityUpdate
{
	public long EntityId = -1L;

	public EntityProperties EntityProperties;

	public EntityPos OldPosition;

	public EntityPos NewPosition;
}
