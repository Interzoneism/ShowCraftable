namespace Vintagestory.API.Common;

public interface ICollectibleOnDisplayInteractable
{
	bool OnInteractStart(ItemSlot inSlot, IPlayer byPlayer);

	bool OnInteractStep(float secondsUsed, ItemSlot inSlot, IPlayer byPlayer);

	void OnInteractStop(float secondsUsed, ItemSlot inSlot, IPlayer byPlayer);

	bool OnInteractCancel(float secondsUsed, ItemSlot inSlot, IPlayer byPlayer, EnumItemUseCancelReason cancelReason);
}
