using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ItemRope : Item
{
	private ClothManager cm;

	private SkillItem[] toolModes;

	public override void TryMergeStacks(ItemStackMergeOperation op)
	{
		int num = op.SinkSlot.Itemstack.Attributes.GetInt("clothId");
		int num2 = op.SourceSlot.Itemstack.Attributes.GetInt("clothId");
		if (num != 0 || num2 != 0)
		{
			op.MovableQuantity = 0;
		}
		else
		{
			base.TryMergeStacks(op);
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		cm = api.ModLoader.GetModSystem<ClothManager>();
		toolModes = new SkillItem[2]
		{
			new SkillItem
			{
				Code = new AssetLocation("shorten"),
				Name = Lang.Get("Shorten by 1m")
			},
			new SkillItem
			{
				Code = new AssetLocation("length"),
				Name = Lang.Get("Lengthen by 1m")
			}
		};
		if (api is ICoreClientAPI coreClientAPI)
		{
			toolModes[0].WithIcon(coreClientAPI, coreClientAPI.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/shorten.svg"), 48, 48, 5, -1));
			toolModes[0].TexturePremultipliedAlpha = false;
			toolModes[1].WithIcon(coreClientAPI, coreClientAPI.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/lengthen.svg"), 48, 48, 5, -1));
			toolModes[1].TexturePremultipliedAlpha = false;
		}
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		return toolModes;
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
	{
		return -1;
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
	{
		int num = slot.Itemstack.Attributes.GetInt("clothId");
		ClothSystem clothSystem = null;
		if (num != 0)
		{
			clothSystem = cm.GetClothSystem(num);
		}
		if (clothSystem == null)
		{
			return;
		}
		if (toolMode == 0)
		{
			if (!clothSystem.ChangeRopeLength(-0.5))
			{
				(api as ICoreClientAPI)?.TriggerIngameError(this, "tooshort", Lang.Get("Already at minimum length!"));
			}
			else if (api is ICoreServerAPI coreServerAPI)
			{
				coreServerAPI.Network.GetChannel("clothphysics").BroadcastPacket(new ClothLengthPacket
				{
					ClothId = clothSystem.ClothId,
					LengthChange = -0.5
				}, byPlayer as IServerPlayer);
			}
		}
		if (toolMode == 1)
		{
			if (!clothSystem.ChangeRopeLength(0.5))
			{
				(api as ICoreClientAPI)?.TriggerIngameError(this, "tooshort", Lang.Get("Already at maximum length!"));
			}
			else if (api is ICoreServerAPI coreServerAPI2)
			{
				coreServerAPI2.Network.GetChannel("clothphysics").BroadcastPacket(new ClothLengthPacket
				{
					ClothId = clothSystem.ClothId,
					LengthChange = 0.5
				}, byPlayer as IServerPlayer);
			}
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		handling = EnumHandHandling.PreventDefault;
		int num = slot.Itemstack.Attributes.GetInt("clothId");
		ClothSystem clothSystem = null;
		if (num != 0)
		{
			clothSystem = cm.GetClothSystem(num);
			if (clothSystem == null)
			{
				num = 0;
			}
		}
		ClothPoint[] array = clothSystem?.Ends;
		if (clothSystem == null)
		{
			if (blockSel != null && api.World.BlockAccessor.GetBlock(blockSel.Position).HasBehavior<BlockBehaviorRopeTieable>())
			{
				clothSystem = attachToBlock(byEntity, blockSel.Position, slot, null);
			}
			else if (entitySel != null)
			{
				clothSystem = attachToEntity(byEntity, entitySel, slot, null, out var relayRopeInteractions);
				if (relayRopeInteractions)
				{
					handling = EnumHandHandling.NotHandled;
					if (clothSystem != null)
					{
						splitStack(slot, byEntity);
					}
					return;
				}
			}
			if (clothSystem != null)
			{
				splitStack(slot, byEntity);
			}
		}
		else
		{
			if (blockSel != null && (blockSel.Position.Equals(array[0].PinnedToBlockPos) || blockSel.Position.Equals(array[1].PinnedToBlockPos)))
			{
				detach(clothSystem, slot, byEntity, null, blockSel.Position);
				return;
			}
			if (entitySel != null && (entitySel.Entity.EntityId == array[0].PinnedToEntity?.EntityId || entitySel.Entity.EntityId == array[1].PinnedToEntity?.EntityId))
			{
				detach(clothSystem, slot, byEntity, entitySel.Entity, null);
				return;
			}
			if (blockSel != null && api.World.BlockAccessor.GetBlock(blockSel.Position).HasBehavior<BlockBehaviorRopeTieable>())
			{
				clothSystem = attachToBlock(byEntity, blockSel.Position, slot, clothSystem);
				array = clothSystem?.Ends;
			}
			else if (entitySel != null)
			{
				attachToEntity(byEntity, entitySel, slot, clothSystem, out var relayRopeInteractions2);
				if (relayRopeInteractions2)
				{
					handling = EnumHandHandling.NotHandled;
					return;
				}
			}
		}
		if (num == 0 && clothSystem != null)
		{
			clothSystem.WalkPoints(delegate(ClothPoint p)
			{
				p.update(0f, api.World);
			});
			clothSystem.setRenderCenterPos();
		}
		if (array != null && array[0].PinnedToEntity?.EntityId != byEntity.EntityId && array[1].PinnedToEntity?.EntityId != byEntity.EntityId)
		{
			slot.Itemstack.Attributes.RemoveAttribute("clothId");
			slot.TakeOut(1);
			slot.MarkDirty();
		}
	}

	private void splitStack(ItemSlot slot, EntityAgent byEntity)
	{
		if (slot.StackSize > 1)
		{
			ItemStack itemStack = slot.TakeOut(slot.StackSize - 1);
			itemStack.Attributes.RemoveAttribute("clothId");
			itemStack.Attributes.RemoveAttribute("ropeHeldByEntityId");
			if (!byEntity.TryGiveItemStack(itemStack))
			{
				api.World.SpawnItemEntity(itemStack, byEntity.ServerPos.XYZ);
			}
		}
	}

	private ClothSystem createRope(ItemSlot slot, EntityAgent byEntity, Vec3d targetPos)
	{
		ClothSystem clothSystem = ClothSystem.CreateRope(api, cm, byEntity.Pos.XYZ, targetPos, null);
		Vec3d vec3d = new Vec3d(0.0, byEntity.LocalEyePos.Y - 0.30000001192092896, 0.0).AheadCopy(0.10000000149011612, byEntity.SidedPos.Pitch, byEntity.SidedPos.Yaw).AheadCopy(0.4000000059604645, byEntity.SidedPos.Pitch, byEntity.SidedPos.Yaw - (float)Math.PI / 2f);
		_ = byEntity.SidedPos;
		clothSystem.FirstPoint.PinTo(byEntity, vec3d.ToVec3f());
		cm.RegisterCloth(clothSystem);
		slot.Itemstack.Attributes.SetLong("ropeHeldByEntityId", byEntity.EntityId);
		slot.Itemstack.Attributes.SetInt("clothId", clothSystem.ClothId);
		slot.MarkDirty();
		return clothSystem;
	}

	private void detach(ClothSystem sys, ItemSlot slot, EntityAgent byEntity, Entity toEntity, BlockPos pos)
	{
		toEntity?.GetBehavior<EntityBehaviorRopeTieable>()?.Detach(sys);
		sys.WalkPoints(delegate(ClothPoint point)
		{
			if (point.PinnedToBlockPos != null && point.PinnedToBlockPos.Equals(pos))
			{
				point.UnPin();
			}
			if (point.PinnedToEntity?.EntityId == byEntity.EntityId)
			{
				point.UnPin();
			}
		});
		if (!sys.PinnedAnywhere)
		{
			slot.Itemstack.Attributes.RemoveAttribute("clothId");
			slot.Itemstack.Attributes.RemoveAttribute("ropeHeldByEntityId");
			cm.UnregisterCloth(sys.ClothId);
		}
	}

	private ClothSystem attachToEntity(EntityAgent byEntity, EntitySelection toEntitySel, ItemSlot slot, ClothSystem sys, out bool relayRopeInteractions)
	{
		relayRopeInteractions = false;
		Entity entity = toEntitySel.Entity;
		EntityBehaviorOwnable behavior = entity.GetBehavior<EntityBehaviorOwnable>();
		if (behavior != null && !behavior.IsOwner(byEntity))
		{
			(entity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "requiersownership", Lang.Get("mount-interact-requiresownership"));
			return null;
		}
		IRopeTiedCreatureCarrier ropeTiedCreatureCarrier = entity.GetInterface<IRopeTiedCreatureCarrier>();
		if (sys != null && ropeTiedCreatureCarrier != null)
		{
			ClothPoint[] ends = sys.Ends;
			ClothPoint clothPoint = ((ends[0].PinnedToEntity?.EntityId == byEntity.EntityId && ends[1].Pinned) ? ends[1] : ends[0]);
			if (ropeTiedCreatureCarrier.TryMount(clothPoint.PinnedToEntity as EntityAgent))
			{
				cm.UnregisterCloth(sys.ClothId);
				return null;
			}
		}
		if (!entity.HasBehavior<EntityBehaviorRopeTieable>())
		{
			relayRopeInteractions = entity?.Properties.Attributes?["relayRopeInteractions"].AsBool(defaultValue: true) == true;
			if (!relayRopeInteractions && api.World.Side == EnumAppSide.Client)
			{
				(api as ICoreClientAPI).TriggerIngameError(this, "notattachable", Lang.Get("This creature is not tieable"));
			}
			return null;
		}
		EntityBehaviorRopeTieable behavior2 = entity.GetBehavior<EntityBehaviorRopeTieable>();
		if (!behavior2.CanAttach())
		{
			return null;
		}
		if (sys == null)
		{
			sys = createRope(slot, byEntity, entity.SidedPos.XYZ);
			behavior2.Attach(sys, sys.LastPoint);
		}
		else
		{
			ClothPoint[] ends2 = sys.Ends;
			ClothPoint point = ((ends2[0].PinnedToEntity?.EntityId == byEntity.EntityId && ends2[1].Pinned) ? ends2[0] : ends2[1]);
			behavior2.Attach(sys, point);
		}
		return sys;
	}

	private ClothSystem attachToBlock(EntityAgent byEntity, BlockPos toPosition, ItemSlot slot, ClothSystem sys)
	{
		if (sys == null)
		{
			sys = createRope(slot, byEntity, toPosition.ToVec3d().Add(0.5, 0.5, 0.5));
			sys.LastPoint.PinTo(toPosition, new Vec3f(0.5f, 0.5f, 0.5f));
		}
		else
		{
			ClothPoint[] ends = sys.Ends;
			ClothPoint clothPoint = ends[0];
			Entity pinnedToEntity = ends[0].PinnedToEntity;
			Entity pinnedToEntity2 = ends[1].PinnedToEntity;
			Entity entity = pinnedToEntity ?? pinnedToEntity2;
			if (pinnedToEntity?.EntityId != byEntity.EntityId)
			{
				clothPoint = ends[1];
			}
			if (entity == byEntity)
			{
				entity = pinnedToEntity2 ?? pinnedToEntity;
			}
			if (entity is EntityAgent byEntity2 && ((pinnedToEntity != null && pinnedToEntity != byEntity) || (pinnedToEntity2 != null && pinnedToEntity2 != byEntity)))
			{
				cm.UnregisterCloth(sys.ClothId);
				sys = createRope(slot, byEntity2, toPosition.ToVec3d().Add(0.5, 0.5, 0.5));
				sys.LastPoint.PinTo(toPosition, new Vec3f(0.5f, 0.5f, 0.5f));
			}
			else
			{
				clothPoint.PinTo(toPosition, new Vec3f(0.5f, 0.5f, 0.5f));
			}
		}
		return sys;
	}

	public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
	{
		if (!(slot.Inventory is InventoryBasePlayer))
		{
			if (slot.Itemstack.Attributes.GetLong("ropeHeldByEntityId", 0L) != 0L)
			{
				slot.Itemstack.Attributes.RemoveAttribute("ropeHeldByEntityId");
			}
			int num = slot.Itemstack.Attributes.GetInt("clothId");
			if (num != 0)
			{
				cm.GetClothSystem(num);
			}
		}
	}

	public override void OnGroundIdle(EntityItem entityItem)
	{
		long num = entityItem.Itemstack.Attributes.GetLong("ropeHeldByEntityId", 0L);
		if (num == 0L)
		{
			return;
		}
		entityItem.Itemstack.Attributes.RemoveAttribute("ropeHeldByEntityId");
		int num2 = entityItem.Itemstack.Attributes.GetInt("clothId");
		if (num2 == 0)
		{
			return;
		}
		ClothSystem clothSystem = cm.GetClothSystem(num2);
		if (clothSystem != null)
		{
			ClothPoint clothPoint = null;
			Entity pinnedToEntity = clothSystem.FirstPoint.PinnedToEntity;
			if (pinnedToEntity != null && pinnedToEntity.EntityId == num)
			{
				clothPoint = clothSystem.FirstPoint;
			}
			Entity pinnedToEntity2 = clothSystem.LastPoint.PinnedToEntity;
			if (pinnedToEntity2 != null && pinnedToEntity2.EntityId == num)
			{
				clothPoint = clothSystem.LastPoint;
			}
			clothPoint?.PinTo(entityItem, new Vec3f(entityItem.SelectionBox.X2 / 2f, entityItem.SelectionBox.Y2 / 2f, entityItem.SelectionBox.Z2 / 2f));
		}
	}

	public override void OnCollected(ItemStack stack, Entity entity)
	{
		int clothId = stack.Attributes.GetInt("clothId");
		if (clothId == 0)
		{
			return;
		}
		ClothSystem clothSystem = cm.GetClothSystem(clothId);
		if (clothSystem == null)
		{
			return;
		}
		ClothPoint clothPoint = null;
		if (clothSystem.FirstPoint.PinnedToEntity is EntityItem { Alive: false })
		{
			clothPoint = clothSystem.FirstPoint;
		}
		if (clothSystem.LastPoint.PinnedToEntity is EntityItem { Alive: false })
		{
			clothPoint = clothSystem.LastPoint;
		}
		if (clothPoint == null)
		{
			return;
		}
		Vec3d vec3d = new Vec3d(0.0, entity.LocalEyePos.Y - 0.30000001192092896, 0.0).AheadCopy(0.10000000149011612, entity.SidedPos.Pitch, entity.SidedPos.Yaw).AheadCopy(0.4000000059604645, entity.SidedPos.Pitch, entity.SidedPos.Yaw - (float)Math.PI / 2f);
		clothPoint.PinTo(entity, vec3d.ToVec3f());
		ItemSlot collectedSlot = null;
		(entity as EntityPlayer).WalkInventory(delegate(ItemSlot slot)
		{
			if (!slot.Empty && slot.Itemstack.Attributes.GetInt("clothId") == clothId)
			{
				collectedSlot = slot;
				return false;
			}
			return true;
		});
		if (clothSystem.FirstPoint.PinnedToEntity == entity && clothSystem.LastPoint.PinnedToEntity == entity)
		{
			clothSystem.FirstPoint.UnPin();
			clothSystem.LastPoint.UnPin();
			if (collectedSlot != null)
			{
				collectedSlot.Itemstack = null;
				collectedSlot.MarkDirty();
			}
			cm.UnregisterCloth(clothSystem.ClothId);
		}
		else
		{
			collectedSlot?.Itemstack?.Attributes.SetLong("ropeHeldByEntityId", entity.EntityId);
			collectedSlot?.MarkDirty();
		}
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
	}
}
