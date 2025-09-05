using System;
using System.Collections;
using System.Collections.Generic;

namespace Vintagestory.API.Common;

public interface IInventory : IReadOnlyCollection<ItemSlot>, IEnumerable<ItemSlot>, IEnumerable
{
	bool Empty { get; }

	bool RemoveOnClose { get; }

	bool TakeLocked { get; }

	bool PutLocked { get; }

	long LastChanged { get; }

	ItemSlot this[int slotId] { get; set; }

	string ClassName { get; }

	string InventoryID { get; }

	HashSet<int> DirtySlots { get; }

	event Action<int> SlotModified;

	event Action<int> SlotNotified;

	object Open(IPlayer player);

	object Close(IPlayer player);

	bool HasOpened(IPlayer player);

	WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op = null, List<ItemSlot> skipSlots = null);

	[Obsolete("Use GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null) instead")]
	WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, List<ItemSlot> skipSlots);

	object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op);

	object TryFlipItems(int targetSlotId, ItemSlot sourceSlot);

	int GetSlotId(ItemSlot slot);

	void MarkSlotDirty(int slotId);
}
