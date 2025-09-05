using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;

namespace Vintagestory.API.Common;

public class BagInventory : IReadOnlyCollection<ItemSlot>, IEnumerable<ItemSlot>, IEnumerable
{
	protected ICoreAPI Api;

	protected List<ItemSlot> bagContents = new List<ItemSlot>();

	private ItemSlot[] bagSlots;

	public int Count => bagContents.Count;

	public ItemSlot[] BagSlots
	{
		get
		{
			return bagSlots;
		}
		set
		{
			bagSlots = value;
		}
	}

	public ItemSlot this[int slotId]
	{
		get
		{
			return bagContents[slotId];
		}
		set
		{
			bagContents[slotId] = value;
		}
	}

	public BagInventory(ICoreAPI api, ItemSlot[] bagSlots)
	{
		BagSlots = bagSlots;
		Api = api;
	}

	public void SaveSlotIntoBag(ItemSlotBagContent slot)
	{
		ItemStack itemstack = BagSlots[slot.BagIndex].Itemstack;
		itemstack?.Collectible.GetCollectibleInterface<IHeldBag>().Store(itemstack, slot);
	}

	public void SaveSlotsIntoBags()
	{
		if (BagSlots == null)
		{
			return;
		}
		foreach (ItemSlot bagContent in bagContents)
		{
			SaveSlotIntoBag((ItemSlotBagContent)bagContent);
		}
	}

	public void ReloadBagInventory(InventoryBase parentinv, ItemSlot[] bagSlots)
	{
		BagSlots = bagSlots;
		if (BagSlots == null || BagSlots.Length == 0)
		{
			bagContents.Clear();
			return;
		}
		bagContents.Clear();
		for (int i = 0; i < BagSlots.Length; i++)
		{
			ItemStack itemstack = BagSlots[i].Itemstack;
			if (itemstack != null && itemstack.ItemAttributes != null)
			{
				itemstack.ResolveBlockOrItem(Api.World);
				IHeldBag collectibleInterface = itemstack.Collectible.GetCollectibleInterface<IHeldBag>();
				if (collectibleInterface != null)
				{
					List<ItemSlotBagContent> orCreateSlots = collectibleInterface.GetOrCreateSlots(itemstack, parentinv, i, Api.World);
					bagContents.AddRange(orCreateSlots);
				}
			}
		}
		if (!(Api is ICoreClientAPI coreClientAPI))
		{
			return;
		}
		ItemSlotBagContent currentHoveredSlot = coreClientAPI.World.Player?.InventoryManager.CurrentHoveredSlot as ItemSlotBagContent;
		if (currentHoveredSlot?.Inventory == parentinv)
		{
			ItemSlot itemSlot = bagContents.FirstOrDefault((ItemSlot slot) => (slot as ItemSlotBagContent).SlotIndex == currentHoveredSlot.SlotIndex && (slot as ItemSlotBagContent).BagIndex == currentHoveredSlot.BagIndex);
			if (itemSlot != null)
			{
				coreClientAPI.World.Player.InventoryManager.CurrentHoveredSlot = itemSlot;
			}
		}
	}

	public IEnumerator<ItemSlot> GetEnumerator()
	{
		for (int i = 0; i < Count; i++)
		{
			yield return this[i];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
