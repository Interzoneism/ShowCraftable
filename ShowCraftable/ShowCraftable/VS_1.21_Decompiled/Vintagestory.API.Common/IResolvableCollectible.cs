namespace Vintagestory.API.Common;

public interface IResolvableCollectible
{
	void Resolve(ItemSlot intoslot, IWorldAccessor worldForResolve, bool resolveImports = true);

	BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer);
}
