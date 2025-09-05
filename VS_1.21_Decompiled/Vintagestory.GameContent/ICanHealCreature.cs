using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public interface ICanHealCreature
{
	bool CanHeal(Entity target);

	WorldInteraction[] GetHealInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player);
}
