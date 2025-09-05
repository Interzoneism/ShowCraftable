using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorAttachable : EntityBehaviorContainer, ICustomInteractionHelpPositioning
{
	protected WearableSlotConfig[] wearableSlots;

	protected InventoryGeneric inv;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "wearablesInv";

	public bool TransparentCenter => false;

	public EntityBehaviorAttachable(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		Api = entity.World.Api;
		wearableSlots = attributes["wearableSlots"].AsObject<WearableSlotConfig[]>();
		inv = new InventoryGeneric(wearableSlots.Length, InventoryClassName + "-" + entity.EntityId, entity.Api, (int id, InventoryGeneric inv) => new ItemSlotWearable(inv, wearableSlots[id].ForCategoryCodes));
		loadInv();
		entity.WatchedAttributes.RegisterModifiedListener("wearablesInv", wearablesModified);
		base.Initialize(properties, attributes);
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		updateSeats();
	}

	private void wearablesModified()
	{
		loadInv();
		updateSeats();
		entity.MarkShapeModified();
	}

	private void updateSeats()
	{
		IVariableSeatsMountable variableSeatsMountable = entity.GetInterface<IVariableSeatsMountable>();
		if (variableSeatsMountable == null)
		{
			return;
		}
		for (int i = 0; i < wearableSlots.Length; i++)
		{
			WearableSlotConfig wearableSlotConfig = wearableSlots[i];
			wearableSlotConfig.SeatConfig = null;
			ItemSlot itemSlot = inv[i];
			if (itemSlot.Empty)
			{
				if (wearableSlotConfig.ProvidesSeatId != null)
				{
					variableSeatsMountable.RemoveSeat(wearableSlotConfig.ProvidesSeatId);
					wearableSlotConfig.ProvidesSeatId = null;
				}
				continue;
			}
			SeatConfig seatConfig = itemSlot.Itemstack?.ItemAttributes?["attachableToEntity"]?["seatConfigBySlotCode"][wearableSlotConfig.Code]?.AsObject<SeatConfig>();
			if (seatConfig == null)
			{
				seatConfig = itemSlot.Itemstack?.ItemAttributes?["attachableToEntity"]?["seatConfig"]?.AsObject<SeatConfig>();
			}
			if (seatConfig != null)
			{
				seatConfig.SeatId = "attachableseat-" + i;
				seatConfig.APName = wearableSlotConfig.AttachmentPointCode;
				wearableSlotConfig.SeatConfig = seatConfig;
				variableSeatsMountable.RegisterSeat(wearableSlotConfig.SeatConfig);
				wearableSlotConfig.ProvidesSeatId = wearableSlotConfig.SeatConfig.SeatId;
			}
			else if (wearableSlotConfig.ProvidesSeatId != null)
			{
				variableSeatsMountable.RemoveSeat(wearableSlotConfig.ProvidesSeatId);
			}
		}
	}

	public override bool TryGiveItemStack(ItemStack itemstack, ref EnumHandling handling)
	{
		int num = 0;
		DummySlot itemslot = new DummySlot(itemstack);
		foreach (ItemSlot item in inv)
		{
			_ = item;
			if (GetSlotFromSelectionBoxIndex(num) != null && TryAttach(itemslot, num, null))
			{
				handling = EnumHandling.PreventDefault;
				return true;
			}
			num++;
		}
		return base.TryGiveItemStack(itemstack, ref handling);
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		int num = (byEntity as EntityPlayer).EntitySelection?.SelectionBoxIndex ?? (-1);
		if (num <= 0)
		{
			return;
		}
		int slotIndexFromSelectionBoxIndex = GetSlotIndexFromSelectionBoxIndex(num - 1);
		ItemSlot itemSlot = ((slotIndexFromSelectionBoxIndex >= 0) ? inv[slotIndexFromSelectionBoxIndex] : null);
		if (itemSlot == null)
		{
			return;
		}
		handled = EnumHandling.PreventSubsequent;
		EntityControls entityControls = byEntity.MountedOn?.Controls ?? byEntity.Controls;
		if (mode == EnumInteractMode.Interact && !entityControls.CtrlKey)
		{
			ItemStack itemstack = itemSlot.Itemstack;
			if (itemstack != null && itemstack.Collectible.Attributes?.IsTrue("interactPassthrough") == true)
			{
				handled = EnumHandling.PassThrough;
				return;
			}
			if (itemSlot.Empty && wearableSlots[slotIndexFromSelectionBoxIndex].EmptyInteractPassThrough)
			{
				handled = EnumHandling.PassThrough;
				return;
			}
			if (wearableSlots[slotIndexFromSelectionBoxIndex].SeatConfig != null)
			{
				handled = EnumHandling.PassThrough;
				return;
			}
		}
		IAttachedInteractions attachedInteractions = itemSlot.Itemstack?.Collectible.GetCollectibleInterface<IAttachedInteractions>();
		if (attachedInteractions != null)
		{
			EnumHandling handled2 = EnumHandling.PassThrough;
			attachedInteractions.OnInteract(itemSlot, num - 1, entity, byEntity, hitPosition, mode, ref handled2, storeInv);
			if (handled2 == EnumHandling.PreventDefault || handled2 == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (mode != EnumInteractMode.Interact || !entityControls.CtrlKey)
		{
			handled = EnumHandling.PassThrough;
			return;
		}
		if (!itemslot.Empty)
		{
			if (TryAttach(itemslot, num - 1, byEntity))
			{
				onAttachmentToggled(byEntity, itemslot);
				return;
			}
		}
		else if (TryRemoveAttachment(byEntity, num - 1))
		{
			onAttachmentToggled(byEntity, itemslot);
			return;
		}
		base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
	}

	private void onAttachmentToggled(EntityAgent byEntity, ItemSlot itemslot)
	{
		AssetLocation location = itemslot.Itemstack?.Block?.Sounds.Place ?? new AssetLocation("sounds/player/build");
		Api.World.PlaySoundAt(location, entity, (byEntity as EntityPlayer).Player, randomizePitch: true, 16f);
		entity.MarkShapeModified();
		entity.World.BlockAccessor.GetChunkAtBlockPos(entity.ServerPos.AsBlockPos).MarkModified();
	}

	private bool TryRemoveAttachment(EntityAgent byEntity, int slotIndex)
	{
		ItemSlot slotFromSelectionBoxIndex = GetSlotFromSelectionBoxIndex(slotIndex);
		if (slotFromSelectionBoxIndex == null || slotFromSelectionBoxIndex.Empty)
		{
			return false;
		}
		EntityBehaviorSeatable behavior = entity.GetBehavior<EntityBehaviorSeatable>();
		if (behavior != null)
		{
			AttachmentPointAndPose attachmentPointAndPose = entity.GetBehavior<EntityBehaviorSelectionBoxes>().selectionBoxes[slotIndex];
			string apname = attachmentPointAndPose.AttachPoint.Code;
			if (behavior.Seats.FirstOrDefault((IMountableSeat seat) => seat.Config.APName == apname || seat.Config.SelectionBox == apname)?.Passenger != null)
			{
				(entity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "requiredisembark", Lang.Get("Passenger must disembark first before being able to remove this seat"));
				return false;
			}
		}
		IAttachedInteractions obj = slotFromSelectionBoxIndex.Itemstack?.Collectible.GetCollectibleInterface<IAttachedInteractions>();
		if (obj != null && !obj.OnTryDetach(slotFromSelectionBoxIndex, slotIndex, entity))
		{
			return false;
		}
		EntityBehaviorOwnable behavior2 = entity.GetBehavior<EntityBehaviorOwnable>();
		if (behavior2 != null && !behavior2.IsOwner(byEntity))
		{
			(entity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "requiersownership", Lang.Get("mount-interact-requiresownership"));
			return false;
		}
		bool flag = slotFromSelectionBoxIndex.StackSize == 0;
		if (flag || byEntity.TryGiveItemStack(slotFromSelectionBoxIndex.Itemstack))
		{
			(slotFromSelectionBoxIndex.Itemstack?.Collectible.GetCollectibleInterface<IAttachedListener>())?.OnDetached(slotFromSelectionBoxIndex, slotIndex, entity, byEntity);
			if (Api.Side == EnumAppSide.Server && !flag)
			{
				slotFromSelectionBoxIndex.Itemstack.StackSize = 1;
				Api.World.Logger.Audit("{0} removed from a {1} at {2}, slot {4}: {3}", byEntity?.GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos, slotFromSelectionBoxIndex.Itemstack?.ToString(), slotIndex);
			}
			slotFromSelectionBoxIndex.Itemstack = null;
			storeInv();
			return true;
		}
		return false;
	}

	private bool TryAttach(ItemSlot itemslot, int slotIndex, EntityAgent byEntity)
	{
		IAttachableToEntity attachableToEntity = IAttachableToEntity.FromCollectible(itemslot.Itemstack.Collectible);
		if (attachableToEntity == null || !attachableToEntity.IsAttachable(entity, itemslot.Itemstack))
		{
			return false;
		}
		ItemSlot slotFromSelectionBoxIndex = GetSlotFromSelectionBoxIndex(slotIndex);
		string categoryCode = attachableToEntity.GetCategoryCode(itemslot.Itemstack);
		WearableSlotConfig slotConfig = wearableSlots[slotIndex];
		EntityBehaviorSeatable behavior = entity.GetBehavior<EntityBehaviorSeatable>();
		if (behavior != null)
		{
			int num = behavior.SeatConfigs.IndexOf((SeatConfig x) => x.SelectionBox == slotConfig.AttachmentPointCode);
			if (num > -1 && behavior.Seats[num].Passenger != null)
			{
				if (Api is ICoreClientAPI coreClientAPI)
				{
					coreClientAPI.TriggerIngameError(this, "alreadyoccupied", Lang.Get("mount-interact-alreadyoccupied"));
				}
				return false;
			}
		}
		if (!slotConfig.CanHold(categoryCode))
		{
			return false;
		}
		if (!slotFromSelectionBoxIndex.Empty)
		{
			return false;
		}
		if (attachableToEntity.RequiresBehindSlots > 0)
		{
			if (slotConfig.BehindSlots.Length < attachableToEntity.RequiresBehindSlots)
			{
				(entity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "notenoughspace", Lang.Get("mount-interact-requiresbehindslots", attachableToEntity.RequiresBehindSlots));
				return false;
			}
			int num2 = wearableSlots.IndexOf((WearableSlotConfig sc) => sc.Code == slotConfig.BehindSlots[0]);
			if (num2 >= 0 && !inv[num2].Empty)
			{
				(entity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "alreadyoccupied", Lang.Get("mount-interact-alreadyoccupiedbehind", attachableToEntity.RequiresBehindSlots + 1));
				return false;
			}
		}
		int num3 = wearableSlots.IndexOf((WearableSlotConfig sc) => sc.BehindSlots?.Contains(slotConfig.Code) ?? false);
		if (num3 >= 0)
		{
			ItemSlot itemSlot = inv[num3];
			if (!itemSlot.Empty && IAttachableToEntity.FromCollectible(itemSlot.Itemstack.Collectible).RequiresBehindSlots > 0)
			{
				(entity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "alreadyoccupied", Lang.Get("mount-interact-alreadyoccupiedinfront", attachableToEntity.RequiresBehindSlots));
				return false;
			}
		}
		EntityBehaviorOwnable behavior2 = entity.GetBehavior<EntityBehaviorOwnable>();
		if (behavior2 != null && !behavior2.IsOwner(byEntity))
		{
			(entity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "requiersownership", Lang.Get("mount-interact-requiresownership"));
			return false;
		}
		if (entity.GetBehavior<EntityBehaviorSeatable>()?.Seats.FirstOrDefault((IMountableSeat s) => s.Config.APName == slotConfig.AttachmentPointCode)?.Passenger != null)
		{
			return false;
		}
		IAttachedInteractions collectibleInterface = itemslot.Itemstack.Collectible.GetCollectibleInterface<IAttachedInteractions>();
		if (collectibleInterface != null && !collectibleInterface.OnTryAttach(itemslot, slotIndex, entity))
		{
			return false;
		}
		IAttachedListener attachedListener = itemslot.Itemstack?.Collectible.GetCollectibleInterface<IAttachedListener>();
		if (entity.World.Side == EnumAppSide.Server)
		{
			string message = string.Format("{0} attached to a {1} at {2}, slot {4}: {3}", byEntity?.GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos, itemslot.Itemstack.ToString(), slotIndex);
			bool num4 = itemslot.TryPutInto(entity.World, slotFromSelectionBoxIndex) > 0;
			if (num4)
			{
				Api.World.Logger.Audit(message);
				attachedListener?.OnAttached(slotFromSelectionBoxIndex, slotIndex, entity, byEntity);
				storeInv();
			}
			return num4;
		}
		return true;
	}

	public ItemSlot GetSlotFromSelectionBoxIndex(int seleBoxIndex)
	{
		int slotIndexFromSelectionBoxIndex = GetSlotIndexFromSelectionBoxIndex(seleBoxIndex);
		if (slotIndexFromSelectionBoxIndex == -1)
		{
			return null;
		}
		return inv[slotIndexFromSelectionBoxIndex];
	}

	public int GetSlotIndexFromSelectionBoxIndex(int seleBoxIndex)
	{
		AttachmentPointAndPose[] selectionBoxes = entity.GetBehavior<EntityBehaviorSelectionBoxes>().selectionBoxes;
		if (selectionBoxes.Length <= seleBoxIndex || seleBoxIndex < 0)
		{
			return -1;
		}
		string apCode = selectionBoxes[seleBoxIndex].AttachPoint.Code;
		return wearableSlots.IndexOf((WearableSlotConfig elem) => elem.AttachmentPointCode == apCode);
	}

	public ItemSlot GetSlotConfigFromAPName(string apCode)
	{
		_ = entity.GetBehavior<EntityBehaviorSelectionBoxes>().selectionBoxes;
		int num = wearableSlots.IndexOf((WearableSlotConfig elem) => elem.AttachmentPointCode == apCode);
		if (num < 0)
		{
			return null;
		}
		return inv[num];
	}

	protected override Shape addGearToShape(Shape entityShape, ItemSlot gearslot, string slotCode, string shapePathForLogging, ref bool shapeIsCloned, ref string[] willDeleteElements, Dictionary<string, StepParentElementTo> overrideStepParent = null)
	{
		int num = gearslot.Inventory.IndexOf((ItemSlot slot) => slot == gearslot);
		overrideStepParent = wearableSlots[num].StepParentTo;
		slotCode = wearableSlots[num].Code;
		return base.addGearToShape(entityShape, gearslot, slotCode, shapePathForLogging, ref shapeIsCloned, ref willDeleteElements, overrideStepParent);
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		int num = 0;
		foreach (ItemSlot item in inv)
		{
			(item.Itemstack?.Collectible.GetCollectibleInterface<IAttachedInteractions>())?.OnEntityDespawn(item, num++, entity, despawn);
		}
		base.OnEntityDespawn(despawn);
	}

	public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
	{
		int num = 0;
		foreach (ItemSlot item in inv)
		{
			(item.Itemstack?.Collectible.GetCollectibleInterface<IAttachedInteractions>())?.OnReceivedClientPacket(item, num, entity, player, packetid, data, ref handled, storeInv);
			num++;
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
	{
		if (es.SelectionBoxIndex > 0)
		{
			return AttachableInteractionHelp.GetOrCreateInteractionHelp(world.Api, this, wearableSlots, es.SelectionBoxIndex - 1, GetSlotFromSelectionBoxIndex(es.SelectionBoxIndex - 1));
		}
		return base.GetInteractionHelp(world, es, player, ref handled);
	}

	public override string PropertyName()
	{
		return "dressable";
	}

	public void Dispose()
	{
	}

	public Vec3d GetInteractionHelpPosition()
	{
		ICoreClientAPI coreClientAPI = entity.Api as ICoreClientAPI;
		if (coreClientAPI.World.Player.CurrentEntitySelection == null)
		{
			return null;
		}
		int num = coreClientAPI.World.Player.CurrentEntitySelection.SelectionBoxIndex - 1;
		if (num < 0)
		{
			return null;
		}
		return entity.GetBehavior<EntityBehaviorSelectionBoxes>().GetCenterPosOfBox(num)?.Add(0.0, 0.5, 0.0);
	}

	public override void OnEntityDeath(DamageSource damageSourceForDeath)
	{
		int num = 0;
		foreach (ItemSlot item in inv)
		{
			(item.Itemstack?.Collectible.GetCollectibleInterface<IAttachedInteractions>())?.OnEntityDeath(item, num++, entity, damageSourceForDeath);
		}
		base.OnEntityDeath(damageSourceForDeath);
	}
}
