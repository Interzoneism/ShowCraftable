using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemSpear : Item
{
	private bool isHackingSpear;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		CollectibleBehaviorAnimationAuthoritative collectibleBehaviorAnimationAuthoritative = GetCollectibleBehavior<CollectibleBehaviorAnimationAuthoritative>(withInheritance: true);
		if (collectibleBehaviorAnimationAuthoritative == null)
		{
			api.World.Logger.Warning("Spear {0} uses ItemSpear class, but lacks required AnimationAuthoritative behavior. I'll take the freedom to add this behavior, but please fix json item type.", Code);
			collectibleBehaviorAnimationAuthoritative = new CollectibleBehaviorAnimationAuthoritative(this);
			collectibleBehaviorAnimationAuthoritative.OnLoaded(api);
			CollectibleBehaviors = CollectibleBehaviors.Append(collectibleBehaviorAnimationAuthoritative);
		}
		collectibleBehaviorAnimationAuthoritative.OnBeginHitEntity += ItemSpear_OnBeginHitEntity;
		isHackingSpear = Attributes.IsTrue("hacking");
	}

	private void ItemSpear_OnBeginHitEntity(EntityAgent byEntity, ref EnumHandling handling)
	{
		if (byEntity.World.Side == EnumAppSide.Client)
		{
			return;
		}
		EntitySelection entitySelection = (byEntity as EntityPlayer)?.EntitySelection;
		if (byEntity.Attributes.GetInt("didattack") == 0 && entitySelection != null)
		{
			byEntity.Attributes.SetInt("didattack", 1);
			_ = byEntity.ActiveHandItemSlot;
			JsonObject attributes = entitySelection.Entity.Properties.Attributes;
			bool flag = attributes != null && attributes["hackedEntity"].Exists && isHackingSpear && api.ModLoader.GetModSystem<CharacterSystem>().HasTrait((byEntity as EntityPlayer).Player, "technical");
			ICoreServerAPI coreServerAPI = api as ICoreServerAPI;
			if (flag)
			{
				coreServerAPI.World.PlaySoundAt(new AssetLocation("sounds/player/hackingspearhit.ogg"), entitySelection.Entity);
			}
			if (api.World.Rand.NextDouble() < 0.15 && flag)
			{
				SpawnEntityInPlaceOf(entitySelection.Entity, entitySelection.Entity.Properties.Attributes["hackedEntity"].AsString(), byEntity);
				coreServerAPI.World.DespawnEntity(entitySelection.Entity, new EntityDespawnData
				{
					Reason = EnumDespawnReason.Removed
				});
			}
		}
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
	{
		return null;
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		if (handling != EnumHandHandling.PreventDefault)
		{
			handling = EnumHandHandling.PreventDefault;
			byEntity.Attributes.SetInt("aiming", 1);
			byEntity.Attributes.SetInt("aimingCancel", 0);
			byEntity.StartAnimation("aim");
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
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
		float damage = 1.5f;
		if (slot.Itemstack.Collectible.Attributes != null)
		{
			damage = slot.Itemstack.Collectible.Attributes["damage"].AsFloat();
		}
		(api as ICoreClientAPI)?.World.AddCameraShake(0.17f);
		ItemStack projectileStack = slot.TakeOut(1);
		slot.MarkDirty();
		IPlayer player = null;
		if (byEntity is EntityPlayer)
		{
			player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/throw"), byEntity, player, randomizePitch: false, 8f);
		EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation(Attributes["spearEntityCode"].AsString()));
		Entity entity = byEntity.World.ClassRegistry.CreateEntity(entityType);
		IProjectile obj = entity as IProjectile;
		obj.FiredBy = byEntity;
		obj.Damage = damage;
		obj.DamageTier = Attributes["damageTier"].AsInt();
		obj.ProjectileStack = projectileStack;
		obj.DropOnImpactChance = 1.1f;
		obj.DamageStackOnImpact = true;
		obj.Weight = 0.3f;
		obj.IgnoreInvFrames = Attributes["ignoreInvFrames"].AsBool();
		EntityProjectile.SpawnThrownEntity(entity, byEntity, 0.75, -0.2, 0.0, 0.65 * (double)byEntity.Stats.GetBlended("bowDrawingStrength"), 0.15);
		byEntity.StartAnimation("throw");
		if (byEntity is EntityPlayer)
		{
			RefillSlotIfEmpty(slot, byEntity, (ItemStack itemstack) => itemstack.Collectible is ItemSpear);
		}
		float pitchModifier = (byEntity as EntityPlayer).talkUtil.pitchModifier;
		player.Entity.World.PlaySoundAt(new AssetLocation("sounds/player/strike"), player.Entity, player, pitchModifier * 0.9f + (float)api.World.Rand.NextDouble() * 0.2f, 16f, 0.35f);
	}

	private void SpawnEntityInPlaceOf(Entity byEntity, string code, EntityAgent causingEntity)
	{
		AssetLocation assetLocation = AssetLocation.Create(code, byEntity.Code.Domain);
		EntityProperties entityType = byEntity.World.GetEntityType(assetLocation);
		if (entityType == null)
		{
			byEntity.World.Logger.Error("ItemCreature: No such entity - {0}", assetLocation);
			if (api.World.Side == EnumAppSide.Client)
			{
				(api as ICoreClientAPI).TriggerIngameError(this, "nosuchentity", $"No such entity loaded - '{assetLocation}'.");
			}
			return;
		}
		Entity entity = byEntity.World.ClassRegistry.CreateEntity(entityType);
		if (entity != null)
		{
			entity.ServerPos.X = byEntity.ServerPos.X;
			entity.ServerPos.Y = byEntity.ServerPos.Y;
			entity.ServerPos.Z = byEntity.ServerPos.Z;
			entity.ServerPos.Motion.X = byEntity.ServerPos.Motion.X;
			entity.ServerPos.Motion.Y = byEntity.ServerPos.Motion.Y;
			entity.ServerPos.Motion.Z = byEntity.ServerPos.Motion.Z;
			entity.ServerPos.Yaw = byEntity.ServerPos.Yaw;
			entity.Pos.SetFrom(entity.ServerPos);
			entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
			entity.Attributes.SetString("origin", "playerplaced");
			entity.WatchedAttributes.SetLong("guardedEntityId", byEntity.EntityId);
			if (causingEntity is EntityPlayer entityPlayer)
			{
				entity.WatchedAttributes.SetString("guardedPlayerUid", entityPlayer.PlayerUID);
			}
			byEntity.World.SpawnEntity(entity);
		}
	}

	public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		if (inSlot.Itemstack.Collectible.Attributes != null)
		{
			float num = 1.5f;
			if (inSlot.Itemstack.Collectible.Attributes != null)
			{
				num = inSlot.Itemstack.Collectible.Attributes["damage"].AsFloat();
			}
			dsc.AppendLine(num + Lang.Get("piercing-damage-thrown"));
		}
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
