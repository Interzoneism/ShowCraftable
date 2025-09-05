using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class AttachedContainerWorkspace
{
	public enum EntityClientPacketId
	{
		OpenAttachedInventory = 1001
	}

	private const int mouseBuildRepeatDelayMs = 250;

	public Entity entity;

	protected BagInventory bagInv;

	protected GuiDialogCreatureContents dlg;

	protected InventoryGeneric wrapperInv;

	protected Action onRequireSave;

	private long lastMouseActionTime;

	public BagInventory BagInventory => bagInv;

	public InventoryGeneric WrapperInv => wrapperInv;

	public AttachedContainerWorkspace(Entity entity, Action onRequireSave)
	{
		this.entity = entity;
		this.onRequireSave = onRequireSave;
		bagInv = new BagInventory(entity.Api, null);
	}

	public void OnInteract(ItemSlot bagSlot, int slotIndex, Entity onEntity, EntityAgent byEntity, Vec3d hitPosition)
	{
		if (!TryLoadInv(bagSlot, slotIndex, onEntity))
		{
			return;
		}
		EntityPlayer entityPlayer = byEntity as EntityPlayer;
		IPlayer player = onEntity.World.PlayerByUid(entityPlayer.PlayerUID);
		bool flag = false;
		if (onEntity.World.Side == EnumAppSide.Client)
		{
			long tickCount = Environment.TickCount64;
			if (tickCount - lastMouseActionTime < 250)
			{
				return;
			}
			lastMouseActionTime = tickCount;
			if (player.InventoryManager.OpenedInventories.FirstOrDefault((IInventory inv) => inv.InventoryID == wrapperInv.InventoryID) != null)
			{
				Close(player);
				return;
			}
			player.InventoryManager.OpenInventory(wrapperInv);
			dlg = new GuiDialogCreatureContents(wrapperInv, onEntity, onEntity.Api as ICoreClientAPI, "attachedcontainer-" + slotIndex, bagSlot.GetStackName(), new DlgPositioner(entity, slotIndex));
			dlg.packetIdOffset = slotIndex << 11;
			if (dlg.TryOpen())
			{
				ICoreClientAPI obj = onEntity.World.Api as ICoreClientAPI;
				wrapperInv.Open(player);
				obj.Network.SendEntityPacket(onEntity.EntityId, 1001 + dlg.packetIdOffset);
				flag = true;
			}
			dlg.OnClosed += delegate
			{
				Close(player);
			};
		}
		else
		{
			player.InventoryManager?.OpenInventory(wrapperInv);
			flag = true;
		}
		if (flag)
		{
			entity.World.Logger.Audit("{0} opened held bag inventory ({3}) on entity {1}/{2}", player?.PlayerName, entity.EntityId, entity.GetName(), wrapperInv.InventoryID);
		}
	}

	public void Close(IPlayer player)
	{
		if (dlg != null && dlg.IsOpened())
		{
			dlg?.TryClose();
		}
		dlg?.Dispose();
		dlg = null;
		if (player != null && wrapperInv != null)
		{
			player.InventoryManager.CloseInventoryAndSync(wrapperInv);
			entity.World.Logger.Audit("{0} closed held bag inventory {3} on entity {1}/{2}", player?.PlayerName, entity.EntityId, entity.GetName(), wrapperInv.InventoryID);
		}
	}

	public bool TryLoadInv(ItemSlot bagSlot, int slotIndex, Entity entity)
	{
		if (bagSlot.Empty)
		{
			return false;
		}
		IHeldBag collectibleInterface = bagSlot.Itemstack.Collectible.GetCollectibleInterface<IHeldBag>();
		if (collectibleInterface == null || collectibleInterface.GetQuantitySlots(bagSlot.Itemstack) <= 0)
		{
			return false;
		}
		List<ItemSlot> list = new List<ItemSlot> { bagSlot };
		if (wrapperInv != null)
		{
			bagInv.ReloadBagInventory(wrapperInv, list.ToArray());
			return true;
		}
		wrapperInv = new InventoryGeneric(entity.Api);
		bagInv.ReloadBagInventory(wrapperInv, list.ToArray());
		wrapperInv.Init(bagInv.Count, "mountedbaginv", slotIndex + "-" + entity.EntityId, onNewSlot);
		if (entity.World.Side == EnumAppSide.Server)
		{
			wrapperInv.SlotModified += Inv_SlotModified;
		}
		return true;
	}

	private ItemSlot onNewSlot(int slotId, InventoryGeneric self)
	{
		return bagInv[slotId];
	}

	private void Inv_SlotModified(int slotid)
	{
		ItemSlot itemSlot = wrapperInv[slotid];
		bagInv.SaveSlotIntoBag((ItemSlotBagContent)itemSlot);
		onRequireSave?.Invoke();
	}

	public void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ItemSlot bagSlot, int slotIndex, ref EnumHandling handled)
	{
		if (packetid < 1000)
		{
			if (wrapperInv != null && wrapperInv.HasOpened(player))
			{
				wrapperInv.InvNetworkUtil.HandleClientPacket(player, packetid, data);
				handled = EnumHandling.PreventSubsequent;
			}
		}
		else if (packetid == 1001)
		{
			OnInteract(bagSlot, slotIndex, entity, player.Entity, null);
		}
	}

	public void OnDespawn(EntityDespawnData despawn)
	{
		dlg?.TryClose();
		if (wrapperInv == null)
		{
			return;
		}
		foreach (string openedByPlayerGUId in wrapperInv.openedByPlayerGUIds)
		{
			IPlayer player = entity.Api.World.PlayerByUid(openedByPlayerGUId);
			player?.InventoryManager.CloseInventoryAndSync(wrapperInv);
			if (player != null)
			{
				entity.World.Logger.Audit("{0} closed held bag inventory {3} on entity {1}/{2}", player?.PlayerName, entity.EntityId, entity.GetName(), wrapperInv.InventoryID);
			}
		}
	}
}
