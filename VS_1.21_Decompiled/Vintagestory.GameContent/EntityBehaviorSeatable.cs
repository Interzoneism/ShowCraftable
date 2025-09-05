using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorSeatable : EntityBehavior, IVariableSeatsMountable, IMountable
{
	public SeatConfig[] SeatConfigs;

	private bool interactMountAnySeat;

	public IMountableSeat[] Seats { get; set; }

	public EntityPos Position => entity.SidedPos;

	public double StepPitch => (entity.Properties.Client?.Renderer as EntityShapeRenderer)?.stepPitch ?? 0.0;

	private ICoreAPI Api => entity.Api;

	public Entity Controller { get; set; }

	public Entity OnEntity => entity;

	public EntityControls ControllingControls
	{
		get
		{
			IMountableSeat[] seats = Seats;
			foreach (IMountableSeat mountableSeat in seats)
			{
				if (mountableSeat.CanControl)
				{
					return mountableSeat.Controls;
				}
			}
			return null;
		}
	}

	public event CanSitDelegate CanSit;

	public EntityBehaviorSeatable(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		SeatConfigs = attributes["seats"].AsObject<SeatConfig[]>();
		interactMountAnySeat = attributes["interactMountAnySeat"].AsBool();
		int num = 0;
		SeatConfig[] seatConfigs = SeatConfigs;
		foreach (SeatConfig seatConfig in seatConfigs)
		{
			if (seatConfig.SeatId == null)
			{
				seatConfig.SeatId = "baseseat-" + num++;
			}
			RegisterSeat(seatConfig);
		}
		if (Api is ICoreClientAPI)
		{
			entity.WatchedAttributes.RegisterModifiedListener("seatdata", UpdatePassenger);
		}
		base.Initialize(properties, attributes);
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		for (int i = 0; i < Seats.Length; i++)
		{
			IMountableSeat mountableSeat = Seats[i];
			if (mountableSeat.Config == null)
			{
				Seats = Seats.RemoveAt(i);
				Api.Logger.Warning("Entity {0}, Seat #{1}, id {2} was loaded but not registered, will remove.", entity.Code, i, mountableSeat.SeatId);
				i--;
			}
			else if (mountableSeat.PassengerEntityIdForInit != 0L && mountableSeat.Passenger == null && Api.World.GetEntityById(mountableSeat.PassengerEntityIdForInit) is EntityAgent entityAgent)
			{
				entityAgent.TryMount(mountableSeat);
			}
		}
	}

	public bool TryMount(EntityAgent carriedCreature)
	{
		if (carriedCreature != null)
		{
			IMountableSeat[] seats = Seats;
			foreach (IMountableSeat mountableSeat in seats)
			{
				if (mountableSeat.Passenger == null && carriedCreature.TryMount(mountableSeat))
				{
					carriedCreature.Controls.StopAllMovement();
					return true;
				}
			}
		}
		return false;
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		if (mode != EnumInteractMode.Interact || !entity.Alive || !byEntity.Alive || !allowSit(byEntity) || itemslot.Itemstack?.Collectible is ItemRope)
		{
			return;
		}
		int num = (byEntity as EntityPlayer).EntitySelection?.SelectionBoxIndex ?? (-1);
		EntityBehaviorSelectionBoxes behavior = entity.GetBehavior<EntityBehaviorSelectionBoxes>();
		if (behavior != null && byEntity.MountedOn == null && num > 0)
		{
			AttachmentPointAndPose attachmentPointAndPose = behavior.selectionBoxes[num - 1];
			string apname = attachmentPointAndPose.AttachPoint.Code;
			IMountableSeat mountableSeat = Seats.FirstOrDefault((IMountableSeat seat) => seat.Config.APName == apname || seat.Config.SelectionBox == apname);
			if (mountableSeat != null && mountableSeat.Passenger != null && mountableSeat.Passenger.HasBehavior<EntityBehaviorRopeTieable>())
			{
				(mountableSeat.Passenger as EntityAgent).TryUnmount();
				handled = EnumHandling.PreventSubsequent;
				return;
			}
		}
		if (byEntity.Controls.CtrlKey)
		{
			return;
		}
		if (num > 0 && behavior != null)
		{
			AttachmentPointAndPose attachmentPointAndPose2 = behavior.selectionBoxes[num - 1];
			string apname2 = attachmentPointAndPose2.AttachPoint.Code;
			IMountableSeat mountableSeat2 = Seats.FirstOrDefault((IMountableSeat seat) => seat.Config.APName == apname2 || seat.Config.SelectionBox == apname2);
			if (mountableSeat2 != null && CanSitOn(mountableSeat2, num - 1) && byEntity.TryMount(mountableSeat2))
			{
				handled = EnumHandling.PreventSubsequent;
				if (Api.Side == EnumAppSide.Server)
				{
					Api.World.Logger.Audit("{0} mounts/embarks a {1} at {2}.", byEntity?.GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos);
				}
				return;
			}
			if (!interactMountAnySeat || !itemslot.Empty)
			{
				return;
			}
		}
		mountAnySeat(byEntity, out handled);
	}

	private bool allowSit(EntityAgent byEntity)
	{
		if (this.CanSit == null)
		{
			return true;
		}
		ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
		Delegate[] invocationList = this.CanSit.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			if (!((CanSitDelegate)invocationList[i])(byEntity, out var errorMessage))
			{
				if (errorMessage != null)
				{
					coreClientAPI?.TriggerIngameError(this, "cantride", Lang.Get("cantride-" + errorMessage));
				}
				return false;
			}
		}
		return true;
	}

	private void mountAnySeat(EntityAgent byEntity, out EnumHandling handled)
	{
		handled = EnumHandling.PreventSubsequent;
		IMountableSeat[] seats = Seats;
		foreach (IMountableSeat mountableSeat in seats)
		{
			if (CanSitOn(mountableSeat) && mountableSeat.CanControl && byEntity.TryMount(mountableSeat))
			{
				if (Api.Side == EnumAppSide.Server)
				{
					Api.World.Logger.Audit("{0} mounts/embarks a {1} at {2}.", byEntity?.GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos);
				}
				return;
			}
		}
		seats = Seats;
		foreach (IMountableSeat mountableSeat2 in seats)
		{
			if (CanSitOn(mountableSeat2) && byEntity.TryMount(mountableSeat2))
			{
				if (Api.Side == EnumAppSide.Server)
				{
					Api.World.Logger.Audit("{0} mounts/embarks a {1} at {2}.", byEntity?.GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos);
				}
				break;
			}
		}
	}

	public bool CanSitOn(IMountableSeat seat, int selectionBoxIndex = -1)
	{
		if (seat.Passenger != null)
		{
			return false;
		}
		EntityBehaviorAttachable behavior = entity.GetBehavior<EntityBehaviorAttachable>();
		if (behavior != null)
		{
			if (selectionBoxIndex == -1)
			{
				selectionBoxIndex = entity.GetBehavior<EntityBehaviorSelectionBoxes>().selectionBoxes.IndexOf((AttachmentPointAndPose x) => x.AttachPoint.Code == seat.Config.SelectionBox);
			}
			ItemSlot slotFromSelectionBoxIndex = behavior.GetSlotFromSelectionBoxIndex(selectionBoxIndex);
			string text = slotFromSelectionBoxIndex?.Itemstack?.Item?.Attributes?["attachableToEntity"]["categoryCode"].AsString();
			if (slotFromSelectionBoxIndex != null && !slotFromSelectionBoxIndex.Empty && text != "seat" && text != "saddle" && text != "pillion")
			{
				return false;
			}
			ItemSlot slotConfigFromAPName = behavior.GetSlotConfigFromAPName(seat.Config.APName);
			if (slotConfigFromAPName != null && !slotConfigFromAPName.Empty)
			{
				JsonObject itemAttributes = slotConfigFromAPName.Itemstack.ItemAttributes;
				if (itemAttributes == null || !itemAttributes["isSaddle"].AsBool())
				{
					JsonObject itemAttributes2 = slotConfigFromAPName.Itemstack.ItemAttributes;
					if (itemAttributes2 == null || !itemAttributes2["attachableToEntity"]["seatConfig"].Exists)
					{
						return slotConfigFromAPName.Itemstack.ItemAttributes?["attachableToEntity"]["seatConfigBySlotCode"].Exists ?? false;
					}
				}
				return true;
			}
		}
		return true;
	}

	public void RegisterSeat(SeatConfig seatconfig)
	{
		if (seatconfig?.SeatId == null)
		{
			throw new ArgumentNullException("seatConfig.SeatId must be set");
		}
		if (Seats == null)
		{
			Seats = Array.Empty<IMountableSeat>();
		}
		int num = Seats.IndexOf((IMountableSeat s) => s.SeatId == seatconfig.SeatId);
		if (num < 0)
		{
			Seats = Seats.Append(CreateSeat(seatconfig.SeatId, seatconfig));
		}
		else
		{
			Seats[num].Config = seatconfig;
		}
		entity.WatchedAttributes.MarkAllDirty();
	}

	public void RemoveSeat(string seatId)
	{
		int num = Seats.IndexOf((IMountableSeat s) => s.SeatId == seatId);
		if (num >= 0)
		{
			Seats = Seats.RemoveAt(num);
			entity.WatchedAttributes.MarkAllDirty();
		}
	}

	private ITreeAttribute seatsToAttr()
	{
		TreeAttribute treeAttribute = new TreeAttribute();
		for (int i = 0; i < Seats.Length; i++)
		{
			IMountableSeat mountableSeat = Seats[i];
			treeAttribute["s" + i] = new TreeAttribute().Set("passenger", new LongAttribute(mountableSeat.Passenger?.EntityId ?? 0)).Set("seatid", new StringAttribute(mountableSeat.SeatId));
		}
		return treeAttribute;
	}

	private void seatsFromAttr()
	{
		if (entity.WatchedAttributes["seatdata"] is TreeAttribute treeAttribute)
		{
			if (Seats == null || Seats.Length != treeAttribute.Count)
			{
				Seats = new IMountableSeat[treeAttribute.Count];
			}
			for (int i = 0; i < treeAttribute.Count; i++)
			{
				TreeAttribute treeAttribute2 = treeAttribute["s" + i] as TreeAttribute;
				Seats[i] = CreateSeat((treeAttribute2["seatid"] as StringAttribute).value, null);
				Seats[i].PassengerEntityIdForInit = (treeAttribute2["passenger"] as LongAttribute).value;
			}
		}
	}

	private void UpdatePassenger()
	{
		if (!(entity.WatchedAttributes["seatdata"] is TreeAttribute treeAttribute))
		{
			return;
		}
		for (int i = 0; i < treeAttribute.Count; i++)
		{
			TreeAttribute treeAttribute2 = treeAttribute["s" + i] as TreeAttribute;
			if (Api.World.GetEntityById((treeAttribute2["passenger"] as LongAttribute).value) is EntityAgent passenger)
			{
				((EntitySeat)Seats[i]).Passenger = passenger;
			}
			else if (Seats[i].Passenger != null)
			{
				((EntitySeat)Seats[i]).Passenger = null;
			}
		}
	}

	protected virtual IMountableSeat CreateSeat(string seatId, SeatConfig config)
	{
		return (entity as ISeatInstSupplier).CreateSeat(this, seatId, config);
	}

	public override void FromBytes(bool isSync)
	{
		seatsFromAttr();
	}

	public override void ToBytes(bool forClient)
	{
		entity.WatchedAttributes["seatdata"] = seatsToAttr();
	}

	public virtual bool AnyMounted()
	{
		IMountableSeat[] seats = Seats;
		for (int i = 0; i < seats.Length; i++)
		{
			if (seats[i].Passenger != null)
			{
				return true;
			}
		}
		return false;
	}

	public override string PropertyName()
	{
		return "seatable";
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
	{
		if (es.SelectionBoxIndex > 0)
		{
			return SeatableInteractionHelp.GetOrCreateInteractionHelp(world.Api, this, Seats, es.SelectionBoxIndex - 1);
		}
		return base.GetInteractionHelp(world, es, player, ref handled);
	}

	public override void OnEntityDeath(DamageSource damageSourceForDeath)
	{
		base.OnEntityDeath(damageSourceForDeath);
		IMountableSeat[] seats = Seats;
		for (int i = 0; i < seats.Length; i++)
		{
			(seats[i]?.Passenger as EntityAgent)?.TryUnmount();
		}
	}
}
