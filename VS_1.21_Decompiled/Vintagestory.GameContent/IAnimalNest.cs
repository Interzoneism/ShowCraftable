using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public interface IAnimalNest : IPointOfInterest
{
	float DistanceWeighting { get; }

	bool IsSuitableFor(Entity entity, string[] nestTypes);

	bool Occupied(Entity entity);

	void SetOccupier(Entity entity);

	bool TryAddEgg(ItemStack egg);
}
