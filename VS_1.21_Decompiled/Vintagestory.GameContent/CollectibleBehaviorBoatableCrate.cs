using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class CollectibleBehaviorBoatableCrate : CollectibleBehaviorHeldBag, IAttachedInteractions, IAttachedListener
{
	private ICoreAPI Api;

	public CollectibleBehaviorBoatableCrate(CollectibleObject collObj)
		: base(collObj)
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		Api = api;
		base.OnLoaded(api);
	}

	public override bool IsEmpty(ItemStack bagstack)
	{
		return base.IsEmpty(bagstack);
	}

	public override int GetQuantitySlots(ItemStack bagstack)
	{
		if (!(collObj is BlockCrate blockCrate))
		{
			return 0;
		}
		string type = bagstack.Attributes.GetString("type") ?? blockCrate.Props.DefaultType;
		return blockCrate.Props[type].QuantitySlots;
	}

	public override void OnInteract(ItemSlot bagSlot, int slotIndex, Entity onEntity, EntityAgent byEntity, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled, Action onRequireSave)
	{
		if ((byEntity.MountedOn?.Controls ?? byEntity.Controls).Sprint)
		{
			return;
		}
		bool shiftKey = byEntity.Controls.ShiftKey;
		bool flag = !shiftKey;
		bool ctrlKey = byEntity.Controls.CtrlKey;
		IPlayer player = (byEntity as EntityPlayer).Player;
		AttachedContainerWorkspace orCreateContainerWorkspace = getOrCreateContainerWorkspace(slotIndex, onEntity, onRequireSave);
		BlockFacing uP = BlockFacing.UP;
		Vec3d xYZ = byEntity.Pos.XYZ;
		if (!orCreateContainerWorkspace.TryLoadInv(bagSlot, slotIndex, onEntity))
		{
			return;
		}
		ItemSlot firstNonEmptySlot = orCreateContainerWorkspace.WrapperInv.FirstNonEmptySlot;
		ItemSlot activeHotbarSlot = player.InventoryManager.ActiveHotbarSlot;
		if (flag && firstNonEmptySlot != null)
		{
			ItemStack itemStack = (ctrlKey ? firstNonEmptySlot.TakeOutWhole() : firstNonEmptySlot.TakeOut(1));
			int num = ((!ctrlKey) ? 1 : itemStack.StackSize);
			if (!player.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true))
			{
				Api.World.SpawnItemEntity(itemStack, xYZ.Add(0.5f + uP.Normalf.X, 0.5f + uP.Normalf.Y, 0.5f + uP.Normalf.Z));
			}
			else
			{
				didMoveItems(itemStack, player);
			}
			Api.World.Logger.Audit("{0} Took {1}x{2} from Boat crate at {3}.", player.PlayerName, num, itemStack.Collectible.Code, xYZ);
			orCreateContainerWorkspace.BagInventory.SaveSlotIntoBag((ItemSlotBagContent)firstNonEmptySlot);
		}
		if (!shiftKey || activeHotbarSlot.Empty)
		{
			return;
		}
		int num2 = ((!ctrlKey) ? 1 : activeHotbarSlot.StackSize);
		if (firstNonEmptySlot == null)
		{
			if (activeHotbarSlot.TryPutInto(Api.World, orCreateContainerWorkspace.WrapperInv[0], num2) > 0)
			{
				didMoveItems(orCreateContainerWorkspace.WrapperInv[0].Itemstack, player);
				Api.World.Logger.Audit("{0} Put {1}x{2} into Boat crate at {3}.", player.PlayerName, num2, orCreateContainerWorkspace.WrapperInv[0].Itemstack.Collectible.Code, xYZ);
			}
			orCreateContainerWorkspace.BagInventory.SaveSlotIntoBag((ItemSlotBagContent)orCreateContainerWorkspace.WrapperInv[0]);
		}
		else if (activeHotbarSlot.Itemstack.Equals(Api.World, firstNonEmptySlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
		{
			List<ItemSlot> list = new List<ItemSlot>();
			while (activeHotbarSlot.StackSize > 0 && list.Count < orCreateContainerWorkspace.WrapperInv.Count)
			{
				WeightedSlot bestSuitedSlot = orCreateContainerWorkspace.WrapperInv.GetBestSuitedSlot(activeHotbarSlot, null, list);
				if (bestSuitedSlot.slot == null)
				{
					break;
				}
				if (activeHotbarSlot.TryPutInto(Api.World, bestSuitedSlot.slot, num2) > 0)
				{
					didMoveItems(bestSuitedSlot.slot.Itemstack, player);
					orCreateContainerWorkspace.BagInventory.SaveSlotIntoBag((ItemSlotBagContent)bestSuitedSlot.slot);
					Api.World.Logger.Audit("{0} Put {1}x{2} into Boat crate at {3}.", player.PlayerName, num2, bestSuitedSlot.slot.Itemstack.Collectible.Code, xYZ);
					if (!ctrlKey)
					{
						break;
					}
				}
				list.Add(bestSuitedSlot.slot);
			}
		}
		activeHotbarSlot.MarkDirty();
	}

	protected void didMoveItems(ItemStack stack, IPlayer byPlayer)
	{
		(Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		AssetLocation assetLocation = stack?.Block?.Sounds?.Place;
		Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
	}
}
