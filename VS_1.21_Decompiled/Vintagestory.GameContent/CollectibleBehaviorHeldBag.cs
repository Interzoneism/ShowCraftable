using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class CollectibleBehaviorHeldBag : CollectibleBehavior, IHeldBag, IAttachedInteractions, IAttachedListener
{
	public const int PacketIdBitShift = 11;

	private const int defaultFlags = 189;

	public CollectibleBehaviorHeldBag(CollectibleObject collObj)
		: base(collObj)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
	}

	public void Clear(ItemStack backPackStack)
	{
		backPackStack.Attributes.GetTreeAttribute("backpack")["slots"] = new TreeAttribute();
	}

	public ItemStack[] GetContents(ItemStack bagstack, IWorldAccessor world)
	{
		ITreeAttribute treeAttribute = bagstack.Attributes.GetTreeAttribute("backpack");
		if (treeAttribute == null)
		{
			return null;
		}
		List<ItemStack> list = new List<ItemStack>();
		foreach (KeyValuePair<string, IAttribute> item in treeAttribute.GetTreeAttribute("slots").SortedCopy())
		{
			ItemStack itemStack = (ItemStack)(item.Value?.GetValue());
			itemStack?.ResolveBlockOrItem(world);
			list.Add(itemStack);
		}
		return list.ToArray();
	}

	public virtual bool IsEmpty(ItemStack bagstack)
	{
		ITreeAttribute treeAttribute = bagstack.Attributes.GetTreeAttribute("backpack");
		if (treeAttribute == null)
		{
			return true;
		}
		foreach (KeyValuePair<string, IAttribute> item in treeAttribute.GetTreeAttribute("slots"))
		{
			IItemStack itemStack = (IItemStack)(item.Value?.GetValue());
			if (itemStack != null && itemStack.StackSize > 0)
			{
				return false;
			}
		}
		return true;
	}

	public virtual int GetQuantitySlots(ItemStack bagstack)
	{
		if (bagstack == null || bagstack.Collectible.Attributes == null)
		{
			return 0;
		}
		return bagstack.Collectible.Attributes["backpack"]["quantitySlots"].AsInt();
	}

	public void Store(ItemStack bagstack, ItemSlotBagContent slot)
	{
		bagstack.Attributes.GetTreeAttribute("backpack").GetTreeAttribute("slots")["slot-" + slot.SlotIndex] = new ItemstackAttribute(slot.Itemstack);
	}

	public virtual string GetSlotBgColor(ItemStack bagstack)
	{
		return bagstack.ItemAttributes["backpack"]["slotBgColor"].AsString();
	}

	public virtual EnumItemStorageFlags GetStorageFlags(ItemStack bagstack)
	{
		return (EnumItemStorageFlags)bagstack.ItemAttributes["backpack"]["storageFlags"].AsInt(189);
	}

	public List<ItemSlotBagContent> GetOrCreateSlots(ItemStack bagstack, InventoryBase parentinv, int bagIndex, IWorldAccessor world)
	{
		List<ItemSlotBagContent> list = new List<ItemSlotBagContent>();
		string slotBgColor = GetSlotBgColor(bagstack);
		EnumItemStorageFlags storageFlags = GetStorageFlags(bagstack);
		int quantitySlots = GetQuantitySlots(bagstack);
		ITreeAttribute treeAttribute = bagstack.Attributes.GetTreeAttribute("backpack");
		if (treeAttribute == null)
		{
			treeAttribute = new TreeAttribute();
			ITreeAttribute treeAttribute2 = new TreeAttribute();
			for (int i = 0; i < quantitySlots; i++)
			{
				ItemSlotBagContent itemSlotBagContent = new ItemSlotBagContent(parentinv, bagIndex, i, storageFlags);
				itemSlotBagContent.HexBackgroundColor = slotBgColor;
				list.Add(itemSlotBagContent);
				treeAttribute2["slot-" + i] = new ItemstackAttribute(null);
			}
			treeAttribute["slots"] = treeAttribute2;
			bagstack.Attributes["backpack"] = treeAttribute;
		}
		else
		{
			foreach (KeyValuePair<string, IAttribute> item in treeAttribute.GetTreeAttribute("slots"))
			{
				int num = item.Key.Split("-")[1].ToInt();
				ItemSlotBagContent itemSlotBagContent2 = new ItemSlotBagContent(parentinv, bagIndex, num, storageFlags);
				itemSlotBagContent2.HexBackgroundColor = slotBgColor;
				if (item.Value?.GetValue() != null)
				{
					ItemstackAttribute itemstackAttribute = (ItemstackAttribute)item.Value;
					itemSlotBagContent2.Itemstack = itemstackAttribute.value;
					itemSlotBagContent2.Itemstack.ResolveBlockOrItem(world);
				}
				while (list.Count <= num)
				{
					list.Add(null);
				}
				list[num] = itemSlotBagContent2;
			}
		}
		return list;
	}

	public void OnAttached(ItemSlot itemslot, int slotIndex, Entity toEntity, EntityAgent byEntity)
	{
	}

	public void OnDetached(ItemSlot itemslot, int slotIndex, Entity fromEntity, EntityAgent byEntity)
	{
		getOrCreateContainerWorkspace(slotIndex, fromEntity, null).Close((byEntity as EntityPlayer).Player);
	}

	public AttachedContainerWorkspace getOrCreateContainerWorkspace(int slotIndex, Entity onEntity, Action onRequireSave)
	{
		return ObjectCacheUtil.GetOrCreate(onEntity.Api, "att-cont-workspace-" + slotIndex + "-" + onEntity.EntityId + "-" + collObj.Id, () => new AttachedContainerWorkspace(onEntity, onRequireSave));
	}

	public AttachedContainerWorkspace getContainerWorkspace(int slotIndex, Entity onEntity)
	{
		return ObjectCacheUtil.TryGet<AttachedContainerWorkspace>(onEntity.Api, "att-cont-workspace-" + slotIndex + "-" + onEntity.EntityId + "-" + collObj.Id);
	}

	public virtual void OnInteract(ItemSlot bagSlot, int slotIndex, Entity onEntity, EntityAgent byEntity, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled, Action onRequireSave)
	{
		if (!(byEntity.MountedOn?.Controls ?? byEntity.Controls).CtrlKey)
		{
			handled = EnumHandling.PreventDefault;
			if (onEntity.Api.Side == EnumAppSide.Client)
			{
				getOrCreateContainerWorkspace(slotIndex, onEntity, onRequireSave).OnInteract(bagSlot, slotIndex, onEntity, byEntity, hitPosition);
			}
		}
	}

	public void OnReceivedClientPacket(ItemSlot bagSlot, int slotIndex, Entity onEntity, IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled, Action onRequireSave)
	{
		int num = packetid >> 11;
		if (slotIndex == num)
		{
			int num2 = 2047;
			packetid &= num2;
			getOrCreateContainerWorkspace(slotIndex, onEntity, onRequireSave).OnReceivedClientPacket(player, packetid, data, bagSlot, slotIndex, ref handled);
		}
	}

	public bool OnTryAttach(ItemSlot itemslot, int slotIndex, Entity toEntity)
	{
		return true;
	}

	public bool OnTryDetach(ItemSlot itemslot, int slotIndex, Entity fromEntity)
	{
		return IsEmpty(itemslot.Itemstack);
	}

	public void OnEntityDespawn(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityDespawnData despawn)
	{
		if (despawn.Reason == EnumDespawnReason.Death)
		{
			ItemStack[] contents = GetContents(itemslot.Itemstack, onEntity.World);
			foreach (ItemStack itemStack in contents)
			{
				if (itemStack != null)
				{
					onEntity.World.SpawnItemEntity(itemStack, onEntity.Pos.XYZ);
				}
			}
		}
		getContainerWorkspace(slotIndex, onEntity)?.OnDespawn(despawn);
	}

	public void OnEntityDeath(ItemSlot itemslot, int slotIndex, Entity onEntity, DamageSource damageSourceForDeath)
	{
	}
}
