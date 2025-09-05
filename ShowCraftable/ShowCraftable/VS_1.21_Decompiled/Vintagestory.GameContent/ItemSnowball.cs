using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemSnowball : Item
{
	private float damage;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		damage = Attributes["damage"].AsFloat(0.001f);
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
	{
		return null;
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel != null && BlockBehaviorSnowballable.canPickSnowballFrom(api.World.BlockAccessor.GetBlock(blockSel.Position), blockSel.Position, (byEntity as EntityPlayer).Player))
		{
			handling = EnumHandHandling.NotHandled;
			return;
		}
		byEntity.Attributes.SetInt("aiming", 1);
		byEntity.Attributes.SetInt("aimingCancel", 0);
		byEntity.StartAnimation("aim");
		handling = EnumHandHandling.PreventDefault;
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (byEntity.Attributes.GetInt("aimingCancel") == 1)
		{
			return false;
		}
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform modelTransform = new ModelTransform();
			modelTransform.EnsureDefaultValues();
			float num = GameMath.Clamp(secondsUsed * 3f, 0f, 1.5f);
			modelTransform.Translation.Set(num / 4f, num / 2f, 0f);
			modelTransform.Rotation.Set(0f, 0f, GameMath.Min(90f, secondsUsed * 360f / 1.5f));
		}
		return true;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		byEntity.Attributes.SetInt("aiming", 0);
		byEntity.StopAnimation("aim");
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
		byEntity.StopAnimation("aim");
		if (secondsUsed < 0.35f)
		{
			return;
		}
		ItemStack projectileStack = slot.TakeOut(1);
		slot.MarkDirty();
		IPlayer dualCallByPlayer = null;
		if (byEntity is EntityPlayer)
		{
			dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/throw"), byEntity, dualCallByPlayer, randomizePitch: false, 8f);
		EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation("thrownsnowball-" + Variant["rock"]));
		Entity entity = byEntity.World.ClassRegistry.CreateEntity(entityType);
		((EntityThrownSnowball)entity).FiredBy = byEntity;
		((EntityThrownSnowball)entity).Damage = damage;
		((EntityThrownSnowball)entity).ProjectileStack = projectileStack;
		EntityProjectile.SpawnThrownEntity(entity, byEntity, 0.75, 0.1, 0.2);
		byEntity.StartAnimation("throw");
		if (byEntity is EntityPlayer)
		{
			RefillSlotIfEmpty(slot, byEntity, (ItemStack itemstack) => itemstack.Collectible is ItemSnowball);
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine(Lang.Get("{0} blunt damage when thrown", damage));
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-throw",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
