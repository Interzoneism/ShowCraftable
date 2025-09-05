namespace Vintagestory.API.Common;

public interface IInventoryNetworkUtil
{
	ICoreAPI Api { get; set; }

	bool PauseInventoryUpdates { get; set; }

	object GetActivateSlotPacket(int slotId, ItemStackMoveOperation op);

	object GetFlipSlotsPacket(IInventory sourceInv, int sourceSlotId, int targetSlotId);

	void HandleClientPacket(IPlayer byPlayer, int packetId, byte[] data);

	object DidOpen(IPlayer player);

	object DidClose(IPlayer player);
}
