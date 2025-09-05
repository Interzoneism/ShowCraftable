using Vintagestory.API.Common.Entities;

namespace Vintagestory.Server;

public sealed class EntityDespawn
{
	public int ForClientId;

	public long EntityId;

	public EntityDespawnData DespawnData;
}
