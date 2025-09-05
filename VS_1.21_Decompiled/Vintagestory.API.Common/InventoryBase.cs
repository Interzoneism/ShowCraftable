using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public abstract class InventoryBase : IInventory, IReadOnlyCollection<ItemSlot>, IEnumerable<ItemSlot>, IEnumerable
{
	public ICoreAPI Api;

	public BlockPos Pos;

	protected float baseWeight;

	protected string className;

	protected string instanceID;

	public long lastChangedSinceServerStart;

	public HashSet<string> openedByPlayerGUIds;

	public IInventoryNetworkUtil InvNetworkUtil;

	public HashSet<int> dirtySlots = new HashSet<int>();

	public virtual Size3f MaxContentDimensions { get; set; }

	public string InventoryID => className + "-" + instanceID;

	public string ClassName => className;

	public long LastChanged => lastChangedSinceServerStart;

	public abstract int Count { get; }

	public virtual int CountForNetworkPacket => Count;

	public abstract ItemSlot this[int slotId] { get; set; }

	public virtual bool IsDirty => dirtySlots.Count > 0;

	public HashSet<int> DirtySlots => dirtySlots;

	public virtual bool TakeLocked { get; set; }

	public virtual bool PutLocked { get; set; }

	public virtual bool RemoveOnClose => true;

	public virtual bool Empty
	{
		get
		{
			using (IEnumerator<ItemSlot> enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (!enumerator.Current.Empty)
					{
						return false;
					}
				}
			}
			return true;
		}
	}

	public ItemSlot FirstNonEmptySlot
	{
		get
		{
			using (IEnumerator<ItemSlot> enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ItemSlot current = enumerator.Current;
					if (!current.Empty)
					{
						return current;
					}
				}
			}
			return null;
		}
	}

	public virtual bool AuditLogAccess { get; set; }

	public event Action<int> SlotModified;

	public event Action<int> SlotNotified;

	public event OnInventoryOpenedDelegate OnInventoryOpened;

	public event OnInventoryClosedDelegate OnInventoryClosed;

	public event CustomGetTransitionSpeedMulDelegate OnAcquireTransitionSpeed;

	public InventoryBase(string className, string instanceID, ICoreAPI api)
	{
		openedByPlayerGUIds = new HashSet<string>();
		this.instanceID = instanceID;
		this.className = className;
		Api = api;
		if (api != null)
		{
			InvNetworkUtil = api.ClassRegistry.CreateInvNetworkUtil(this, api);
		}
	}

	public InventoryBase(string inventoryID, ICoreAPI api)
	{
		openedByPlayerGUIds = new HashSet<string>();
		if (inventoryID != null)
		{
			string[] array = inventoryID.Split('-', 2);
			className = array[0];
			instanceID = array[1];
		}
		Api = api;
		if (api != null)
		{
			InvNetworkUtil = api.ClassRegistry.CreateInvNetworkUtil(this, api);
		}
	}

	public virtual void LateInitialize(string inventoryID, ICoreAPI api)
	{
		Api = api;
		string[] array = inventoryID.Split('-', 2);
		className = array[0];
		instanceID = array[1];
		if (InvNetworkUtil == null)
		{
			InvNetworkUtil = api.ClassRegistry.CreateInvNetworkUtil(this, api);
		}
		else
		{
			InvNetworkUtil.Api = api;
		}
		AfterBlocksLoaded(api.World);
	}

	public virtual void AfterBlocksLoaded(IWorldAccessor world)
	{
		ResolveBlocksOrItems();
	}

	public virtual void ResolveBlocksOrItems()
	{
		using IEnumerator<ItemSlot> enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			ItemSlot current = enumerator.Current;
			if (current.Itemstack != null && !current.Itemstack.ResolveBlockOrItem(Api.World))
			{
				current.Itemstack = null;
			}
		}
	}

	public virtual int GetSlotId(ItemSlot slot)
	{
		for (int i = 0; i < Count; i++)
		{
			if (this[i] == slot)
			{
				return i;
			}
		}
		return -1;
	}

	[Obsolete("Use GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null) instead")]
	public WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, List<ItemSlot> skipSlots)
	{
		return GetBestSuitedSlot(sourceSlot, null, skipSlots);
	}

	public virtual WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op = null, List<ItemSlot> skipSlots = null)
	{
		WeightedSlot weightedSlot = new WeightedSlot();
		if (PutLocked || sourceSlot.Inventory == this)
		{
			return weightedSlot;
		}
		using (IEnumerator<ItemSlot> enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				ItemSlot current = enumerator.Current;
				if ((skipSlots == null || !skipSlots.Contains(current)) && current.Itemstack != null && current.CanTakeFrom(sourceSlot))
				{
					float suitability = GetSuitability(sourceSlot, current, isMerge: true);
					if (weightedSlot.slot == null || weightedSlot.weight < suitability)
					{
						weightedSlot.slot = current;
						weightedSlot.weight = suitability;
					}
				}
			}
		}
		using IEnumerator<ItemSlot> enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			ItemSlot current2 = enumerator.Current;
			if ((skipSlots == null || !skipSlots.Contains(current2)) && current2.Itemstack == null && current2.CanTakeFrom(sourceSlot))
			{
				float suitability2 = GetSuitability(sourceSlot, current2, isMerge: false);
				if (weightedSlot.slot == null || weightedSlot.weight < suitability2)
				{
					weightedSlot.slot = current2;
					weightedSlot.weight = suitability2;
				}
			}
		}
		return weightedSlot;
	}

	public virtual float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
	{
		float num = ((targetSlot is ItemSlotBackpack && (sourceSlot.Itemstack.Collectible.GetStorageFlags(sourceSlot.Itemstack) & EnumItemStorageFlags.Backpack) > (EnumItemStorageFlags)0) ? 2 : 0);
		return baseWeight + num + (float)((!isMerge) ? 1 : 3);
	}

	public virtual bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot)
	{
		return MaxContentDimensions?.CanContain(sourceSlot.Itemstack.Collectible.Dimensions) ?? true;
	}

	public object TryFlipItems(int targetSlotId, ItemSlot itemSlot)
	{
		ItemSlot itemSlot2 = this[targetSlotId];
		if (itemSlot2 != null && itemSlot2.TryFlipWith(itemSlot))
		{
			return InvNetworkUtil.GetFlipSlotsPacket(itemSlot.Inventory, itemSlot.Inventory.GetSlotId(itemSlot), targetSlotId);
		}
		return null;
	}

	public virtual bool CanPlayerAccess(IPlayer player, EntityPos position)
	{
		return true;
	}

	public virtual bool CanPlayerModify(IPlayer player, EntityPos position)
	{
		if (CanPlayerAccess(player, position))
		{
			return HasOpened(player);
		}
		return false;
	}

	public virtual void OnSearchTerm(string text)
	{
	}

	public virtual object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		object activateSlotPacket = InvNetworkUtil.GetActivateSlotPacket(slotId, op);
		if (op.ShiftDown)
		{
			sourceSlot = this[slotId];
			string text = sourceSlot.Itemstack?.GetName();
			string text2 = sourceSlot.Inventory?.InventoryID;
			StringBuilder stringBuilder = new StringBuilder();
			op.RequestedQuantity = sourceSlot.StackSize;
			op.ActingPlayer.InventoryManager.TryTransferAway(sourceSlot, ref op, onlyPlayerInventory: false, stringBuilder);
			Api.World.Logger.Audit("{0} shift clicked slot {1} in {2}. Moved {3}x{4} to ({5})", op.ActingPlayer?.PlayerName, slotId, text2, op.MovedQuantity, text, stringBuilder.ToString());
		}
		else
		{
			this[slotId].ActivateSlot(sourceSlot, ref op);
		}
		return activateSlotPacket;
	}

	public virtual void OnItemSlotModified(ItemSlot slot)
	{
	}

	public virtual void DidModifyItemSlot(ItemSlot slot, ItemStack extractedStack = null)
	{
		int slotId = GetSlotId(slot);
		if (slotId < 0)
		{
			throw new ArgumentException($"Supplied slot is not part of this inventory ({InventoryID})!");
		}
		MarkSlotDirty(slotId);
		OnItemSlotModified(slot);
		this.SlotModified?.Invoke(slotId);
		slot.Itemstack?.Collectible?.OnModifiedInInventorySlot(Api.World, slot, extractedStack);
	}

	public virtual void PerformNotifySlot(int slotId)
	{
		ItemSlot itemSlot = this[slotId];
		if (itemSlot != null && itemSlot.Inventory == this)
		{
			this.SlotNotified?.Invoke(slotId);
		}
	}

	public abstract void FromTreeAttributes(ITreeAttribute tree);

	public abstract void ToTreeAttributes(ITreeAttribute tree);

	public virtual bool TryFlipItemStack(IPlayer owningPlayer, string[] invIds, int[] slotIds, long[] lastChanged)
	{
		ItemSlot[] slotsIfExists = GetSlotsIfExists(owningPlayer, invIds, slotIds);
		if (slotsIfExists[0] == null || slotsIfExists[1] == null)
		{
			return false;
		}
		return ((InventoryBase)owningPlayer.InventoryManager.GetInventory(invIds[1])).TryFlipItems(slotIds[1], slotsIfExists[0]) != null;
	}

	public virtual bool TryMoveItemStack(IPlayer player, string[] invIds, int[] slotIds, ref ItemStackMoveOperation op)
	{
		ItemSlot[] slotsIfExists = GetSlotsIfExists(player, invIds, slotIds);
		if (slotsIfExists[0] == null || slotsIfExists[1] == null)
		{
			return false;
		}
		slotsIfExists[0].TryPutInto(slotsIfExists[1], ref op);
		return op.MovedQuantity == op.RequestedQuantity;
	}

	public virtual ItemSlot[] GetSlotsIfExists(IPlayer player, string[] invIds, int[] slotIds)
	{
		ItemSlot[] array = new ItemSlot[2];
		InventoryBase inventoryBase = (InventoryBase)player.InventoryManager.GetInventory(invIds[0]);
		InventoryBase inventoryBase2 = (InventoryBase)player.InventoryManager.GetInventory(invIds[1]);
		if (inventoryBase == null || inventoryBase2 == null)
		{
			return array;
		}
		if (!inventoryBase.CanPlayerModify(player, player.Entity.Pos) || !inventoryBase2.CanPlayerModify(player, player.Entity.Pos))
		{
			return array;
		}
		array[0] = inventoryBase[slotIds[0]];
		array[1] = inventoryBase2[slotIds[1]];
		return array;
	}

	public virtual ItemSlot[] SlotsFromTreeAttributes(ITreeAttribute tree, ItemSlot[] slots = null, List<ItemSlot> modifiedSlots = null)
	{
		if (tree == null)
		{
			return slots;
		}
		if (slots == null)
		{
			slots = new ItemSlot[tree.GetInt("qslots")];
			for (int i = 0; i < slots.Length; i++)
			{
				slots[i] = NewSlot(i);
			}
		}
		for (int j = 0; j < slots.Length; j++)
		{
			ItemStack itemStack = tree.GetTreeAttribute("slots")?.GetItemstack(j.ToString() ?? "");
			slots[j].Itemstack = itemStack;
			if (Api?.World == null)
			{
				continue;
			}
			itemStack?.ResolveBlockOrItem(Api.World);
			if (modifiedSlots != null)
			{
				ItemStack itemstack = slots[j].Itemstack;
				bool num = itemStack != null && !itemStack.Equals(Api.World, itemstack);
				bool flag = itemstack != null && !itemstack.Equals(Api.World, itemStack);
				if (num || flag)
				{
					modifiedSlots.Add(slots[j]);
				}
			}
		}
		return slots;
	}

	public void SlotsToTreeAttributes(ItemSlot[] slots, ITreeAttribute tree)
	{
		tree.SetInt("qslots", slots.Length);
		TreeAttribute treeAttribute = new TreeAttribute();
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i].Itemstack != null)
			{
				treeAttribute.SetItemstack(i.ToString() ?? "", slots[i].Itemstack.Clone());
			}
		}
		tree["slots"] = treeAttribute;
	}

	public ItemSlot[] GenEmptySlots(int quantity)
	{
		ItemSlot[] array = new ItemSlot[quantity];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = NewSlot(i);
		}
		return array;
	}

	protected virtual ItemSlot NewSlot(int i)
	{
		return new ItemSlot(this);
	}

	public virtual void MarkSlotDirty(int slotId)
	{
		if (slotId < 0)
		{
			throw new Exception("Negative slotid?!");
		}
		dirtySlots.Add(slotId);
	}

	public virtual void DiscardAll()
	{
		for (int i = 0; i < Count; i++)
		{
			if (this[i].Itemstack != null)
			{
				dirtySlots.Add(i);
			}
			this[i].Itemstack = null;
		}
	}

	public virtual void DropSlotIfHot(ItemSlot slot, IPlayer player = null)
	{
		if (Api.Side != EnumAppSide.Client && !slot.Empty && (player == null || player.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			JsonObject attributes = slot.Itemstack.Collectible.Attributes;
			if ((attributes == null || !attributes.IsTrue("allowHotCrafting")) && slot.Itemstack.Collectible.GetTemperature(Api.World, slot.Itemstack) > 300f && !hasHeatResistantHandGear(player))
			{
				(Api as ICoreServerAPI).SendIngameError(player as IServerPlayer, "requiretongs", Lang.Get("Requires tongs to hold"));
				player.Entity.ReceiveDamage(new DamageSource
				{
					DamageTier = 0,
					Source = EnumDamageSource.Player,
					SourceEntity = player.Entity,
					Type = EnumDamageType.Fire
				}, 0.25f);
				player.InventoryManager.DropItem(slot, fullStack: true);
			}
		}
	}

	private bool hasHeatResistantHandGear(IPlayer player)
	{
		if (player == null)
		{
			return false;
		}
		ItemSlot leftHandItemSlot = player.Entity.LeftHandItemSlot;
		if (leftHandItemSlot == null)
		{
			return false;
		}
		return leftHandItemSlot.Itemstack?.Collectible.Attributes?.IsTrue("heatResistant") == true;
	}

	public virtual void DropSlots(Vec3d pos, params int[] slotsIds)
	{
		foreach (int num in slotsIds)
		{
			if (num < 0)
			{
				throw new Exception("Negative slotid?!");
			}
			ItemSlot itemSlot = this[num];
			if (itemSlot.Itemstack != null)
			{
				Api.World.SpawnItemEntity(itemSlot.Itemstack, pos);
				itemSlot.Itemstack = null;
				itemSlot.MarkDirty();
			}
		}
	}

	public virtual void DropAll(Vec3d pos, int maxStackSize = 0)
	{
		using IEnumerator<ItemSlot> enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			ItemSlot current = enumerator.Current;
			if (current.Itemstack == null)
			{
				continue;
			}
			if (maxStackSize > 0)
			{
				while (current.StackSize > 0)
				{
					ItemStack itemstack = current.TakeOut(GameMath.Clamp(current.StackSize, 1, maxStackSize));
					Api.World.SpawnItemEntity(itemstack, pos);
				}
			}
			else
			{
				Api.World.SpawnItemEntity(current.Itemstack, pos);
			}
			current.Itemstack = null;
			current.MarkDirty();
		}
	}

	public void Clear()
	{
		using IEnumerator<ItemSlot> enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			enumerator.Current.Itemstack = null;
		}
	}

	public virtual void OnOwningEntityDeath(Vec3d pos)
	{
		DropAll(pos);
	}

	public virtual float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
	{
		float defaultTransitionSpeedMul = GetDefaultTransitionSpeedMul(transType);
		return InvokeTransitionSpeedDelegates(transType, stack, defaultTransitionSpeedMul);
	}

	public float InvokeTransitionSpeedDelegates(EnumTransitionType transType, ItemStack stack, float mul)
	{
		if (this.OnAcquireTransitionSpeed != null)
		{
			Delegate[] invocationList = this.OnAcquireTransitionSpeed.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				CustomGetTransitionSpeedMulDelegate customGetTransitionSpeedMulDelegate = (CustomGetTransitionSpeedMulDelegate)invocationList[i];
				mul *= customGetTransitionSpeedMulDelegate(transType, stack, mul);
			}
		}
		return mul;
	}

	protected virtual float GetDefaultTransitionSpeedMul(EnumTransitionType transitionType)
	{
		return transitionType switch
		{
			EnumTransitionType.Perish => 1, 
			EnumTransitionType.Dry => 1, 
			EnumTransitionType.Cure => 1, 
			EnumTransitionType.Ripen => 1, 
			EnumTransitionType.Melt => 1, 
			EnumTransitionType.Harden => 1, 
			_ => 0, 
		};
	}

	public virtual object Open(IPlayer player)
	{
		object result = InvNetworkUtil.DidOpen(player);
		openedByPlayerGUIds.Add(player.PlayerUID);
		this.OnInventoryOpened?.Invoke(player);
		if (AuditLogAccess)
		{
			Api.World.Logger.Audit("{0} opened inventory {1}", player.PlayerName, InventoryID);
		}
		return result;
	}

	public virtual object Close(IPlayer player)
	{
		object result = InvNetworkUtil.DidClose(player);
		openedByPlayerGUIds.Remove(player.PlayerUID);
		this.OnInventoryClosed?.Invoke(player);
		if (AuditLogAccess)
		{
			Api.World.Logger.Audit("{0} closed inventory {1}", player.PlayerName, InventoryID);
		}
		return result;
	}

	public virtual bool HasOpened(IPlayer player)
	{
		return openedByPlayerGUIds.Contains(player.PlayerUID);
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

	public virtual ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
	{
		return GetBestSuitedSlot(fromSlot).slot;
	}

	public virtual ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
	{
		return null;
	}
}
