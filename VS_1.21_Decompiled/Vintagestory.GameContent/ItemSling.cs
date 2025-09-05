using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemSling : Item
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "slingInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject collectible in api.World.Collectibles)
			{
				if (collectible is ItemStone)
				{
					list.Add(new ItemStack(collectible));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-chargesling",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
	{
		return null;
	}

	private ItemSlot GetNextMunition(EntityAgent byEntity)
	{
		ItemSlot slot = null;
		byEntity.WalkInventory(delegate(ItemSlot invslot)
		{
			if (invslot is ItemSlotCreative)
			{
				return true;
			}
			if (invslot.Itemstack != null && invslot.Itemstack.Collectible is ItemStone)
			{
				slot = invslot;
				return false;
			}
			return true;
		});
		return slot;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (GetNextMunition(byEntity) != null)
		{
			if (byEntity.World is IClientWorldAccessor)
			{
				slot.Itemstack.TempAttributes.SetInt("renderVariant", 1);
			}
			slot.Itemstack.Attributes.SetInt("renderVariant", 1);
			byEntity.Attributes.SetInt("aiming", 1);
			byEntity.Attributes.SetInt("aimingCancel", 0);
			byEntity.AnimManager.StartAnimation("slingaimbalearic");
			IPlayer dualCallByPlayer = null;
			if (byEntity is EntityPlayer)
			{
				dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/bow-draw"), byEntity, dualCallByPlayer, randomizePitch: false, 8f);
			handling = EnumHandHandling.PreventDefault;
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		int num = GameMath.Clamp((int)Math.Ceiling(secondsUsed * 4f), 0, 3);
		int num2 = slot.Itemstack.Attributes.GetInt("renderVariant");
		slot.Itemstack.TempAttributes.SetInt("renderVariant", num);
		slot.Itemstack.Attributes.SetInt("renderVariant", num);
		if (num2 != num)
		{
			(byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
		}
		return true;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		byEntity.Attributes.SetInt("aiming", 0);
		byEntity.AnimManager.StopAnimation("slingaimbalearic");
		if (byEntity.World is IClientWorldAccessor)
		{
			slot.Itemstack.TempAttributes.RemoveAttribute("renderVariant");
		}
		slot.Itemstack.Attributes.SetInt("renderVariant", 0);
		if (cancelReason != EnumItemUseCancelReason.Destroyed)
		{
			(byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
		}
		if (cancelReason != EnumItemUseCancelReason.ReleasedMouse)
		{
			byEntity.Attributes.SetInt("aimingCancel", 1);
		}
		return true;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (byEntity.Attributes.GetInt("aimingCancel") == 1)
		{
			return;
		}
		byEntity.Attributes.SetInt("aiming", 0);
		byEntity.AnimManager.StopAnimation("slingaimbalearic");
		byEntity.World.RegisterCallback(delegate
		{
			slot.Itemstack?.Attributes.SetInt("renderVariant", 2);
		}, 250);
		byEntity.World.RegisterCallback(delegate
		{
			if (byEntity.World is IClientWorldAccessor)
			{
				slot.Itemstack?.TempAttributes.RemoveAttribute("renderVariant");
			}
			slot.Itemstack?.Attributes.SetInt("renderVariant", 0);
		}, 450);
		(byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
		if (secondsUsed < 0.75f)
		{
			return;
		}
		ItemSlot nextMunition = GetNextMunition(byEntity);
		if (nextMunition != null)
		{
			float num = 0f;
			if (slot.Itemstack.Collectible.Attributes != null)
			{
				num += slot.Itemstack.Collectible.Attributes["damage"].AsFloat();
			}
			if (nextMunition.Itemstack.Collectible.Attributes != null)
			{
				num += nextMunition.Itemstack.Collectible.Attributes["damage"].AsFloat();
			}
			if (byEntity != null)
			{
				num *= byEntity.Stats.GetBlended("rangedWeaponsDamage");
			}
			ItemStack itemStack = nextMunition.TakeOut(1);
			nextMunition.MarkDirty();
			if (byEntity is EntityPlayer)
			{
				byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			if (api.Side == EnumAppSide.Server)
			{
				byEntity.World.PlaySoundAt(new AssetLocation("sounds/tool/sling1"), byEntity, null, randomizePitch: false, 8f, 0.25f);
			}
			EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation("thrownstone-" + itemStack.Collectible.Variant["rock"]));
			Entity entity = byEntity.World.ClassRegistry.CreateEntity(entityType);
			IProjectile obj = entity as IProjectile;
			obj.FiredBy = byEntity;
			obj.Damage = num;
			obj.ProjectileStack = itemStack;
			obj.WeaponStack = slot.Itemstack;
			EntityProjectile.SpawnThrownEntity(entity, byEntity, 0.75, 0.0, 0.0, byEntity.Stats.GetBlended("bowDrawingStrength") * 0.8f);
			slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, slot);
			byEntity.AnimManager.StartAnimation("slingthrowbalearic");
			byEntity.World.RegisterCallback(delegate
			{
				byEntity.AnimManager.StopAnimation("slingthrowbalearic");
			}, 400);
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		if (inSlot.Itemstack.Collectible.Attributes != null)
		{
			float num = inSlot.Itemstack.Collectible.Attributes["damage"].AsFloat();
			if (num != 0f)
			{
				dsc.AppendLine(Lang.Get("sling-piercingdamage", num));
			}
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
