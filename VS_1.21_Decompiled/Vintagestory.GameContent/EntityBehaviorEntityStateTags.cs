using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorEntityStateTags : EntityBehavior
{
	protected bool Swimming;

	protected bool FeetInLiquid;

	protected bool OnGround;

	protected bool Flying;

	protected bool Aiming;

	protected bool Moving;

	protected bool Alive;

	protected bool Sprinting;

	protected bool Sneaking;

	protected bool Armed;

	protected bool ArmedMelee;

	protected bool ArmedRanged;

	protected bool HoldingOpenFire;

	protected static EntityTagArray TagSwimming = EntityTagArray.Empty;

	protected static EntityTagArray TagFeetInLiquid = EntityTagArray.Empty;

	protected static EntityTagArray TagFlying = EntityTagArray.Empty;

	protected static EntityTagArray TagOnGround = EntityTagArray.Empty;

	protected static EntityTagArray TagMoving = EntityTagArray.Empty;

	protected static EntityTagArray TagAlive = EntityTagArray.Empty;

	protected static EntityTagArray TagAiming = EntityTagArray.Empty;

	protected static EntityTagArray TagSprinting = EntityTagArray.Empty;

	protected static EntityTagArray TagSneaking = EntityTagArray.Empty;

	protected static EntityTagArray TagArmed = EntityTagArray.Empty;

	protected static EntityTagArray TagArmedMelee = EntityTagArray.Empty;

	protected static EntityTagArray TagArmedRanged = EntityTagArray.Empty;

	protected static EntityTagArray TagHoldingOpenFire = EntityTagArray.Empty;

	protected static ItemTagArray ItemTagWeapon = ItemTagArray.Empty;

	protected static ItemTagArray ItemTagWeaponMelee = ItemTagArray.Empty;

	protected static ItemTagArray ItemTagWeaponRanged = ItemTagArray.Empty;

	protected static ItemTagArray ItemTagHasOpenFire = ItemTagArray.Empty;

	protected static BlockTagArray BlockTagHasOpenFire = BlockTagArray.Empty;

	protected float TimeSinceUpdateSec;

	protected float UpdatePeriodSec = 1f;

	public EntityBehaviorEntityStateTags(Entity entity)
		: base(entity)
	{
	}

	public override string PropertyName()
	{
		return "entityStateTags";
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		if (attributes.KeyExists("updatePeriodSec"))
		{
			UpdatePeriodSec = attributes["updatePeriodSec"].AsFloat(UpdatePeriodSec);
		}
		EntityTagArray tags = entity.Tags;
		TagsInitialUpdate(ref tags);
		if (entity.Tags != tags)
		{
			entity.Tags = tags;
			entity.MarkTagsDirty();
		}
		TimeSinceUpdateSec = (float)entity.World.Rand.NextDouble() * UpdatePeriodSec;
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.Api.Side != EnumAppSide.Server)
		{
			return;
		}
		TimeSinceUpdateSec += deltaTime;
		if (!(TimeSinceUpdateSec < UpdatePeriodSec))
		{
			TimeSinceUpdateSec = 0f;
			EntityTagArray tags = entity.Tags;
			TagsUpdate(ref tags);
			if (entity.Tags != tags)
			{
				entity.Tags = tags;
				entity.MarkTagsDirty();
			}
			entity.World.FrameProfiler.Mark("statetagsupdate");
		}
	}

	public static void GetTagsIds(ITagRegistry registry)
	{
		TagSwimming = new EntityTagArray(registry.EntityTagToTagId("state-swimming"));
		TagFeetInLiquid = new EntityTagArray(registry.EntityTagToTagId("state-feet-in-liquid"));
		TagFlying = new EntityTagArray(registry.EntityTagToTagId("state-flying"));
		TagOnGround = new EntityTagArray(registry.EntityTagToTagId("state-on-ground"));
		TagMoving = new EntityTagArray(registry.EntityTagToTagId("state-moving"));
		TagAlive = new EntityTagArray(registry.EntityTagToTagId("state-alive"));
		TagAiming = new EntityTagArray(registry.EntityTagToTagId("state-aiming"));
		TagSprinting = new EntityTagArray(registry.EntityTagToTagId("state-sprinting"));
		TagSneaking = new EntityTagArray(registry.EntityTagToTagId("state-sneaking"));
		TagArmed = new EntityTagArray(registry.EntityTagToTagId("state-armed"));
		TagArmedMelee = new EntityTagArray(registry.EntityTagToTagId("state-armed-melee"));
		TagArmedRanged = new EntityTagArray(registry.EntityTagToTagId("state-armed-ranged"));
		ItemTagWeapon = new ItemTagArray(registry.ItemTagToTagId("weapon"));
		ItemTagWeaponMelee = new ItemTagArray(registry.ItemTagToTagId("weapon-melee"));
		ItemTagWeaponRanged = new ItemTagArray(registry.ItemTagToTagId("weapon-ranged"));
		ItemTagHasOpenFire = new ItemTagArray(registry.ItemTagToTagId("has-open-fire"));
		BlockTagHasOpenFire = new BlockTagArray(registry.BlockTagToTagId("has-open-fire"));
	}

	protected virtual void TagsInitialUpdate(ref EntityTagArray tags)
	{
		EntityTagsInitialUpdate(ref tags);
		if (entity is EntityAgent entityAgent)
		{
			EntityAgentTagsInitialUpdate(entityAgent, ref tags);
			EntityAgentHandItemsTagsInitialUpdate(entityAgent, ref tags);
		}
	}

	protected virtual void TagsUpdate(ref EntityTagArray tags)
	{
		EntityTagsUpdate(ref tags);
		if (entity is EntityAgent entityAgent)
		{
			EntityAgentTagsUpdate(entityAgent, ref tags);
			EntityAgentHandItemsTagsUpdate(entityAgent, ref tags);
		}
	}

	protected virtual void EntityTagsInitialUpdate(ref EntityTagArray tags)
	{
		tags = tags.Remove(TagSwimming);
		tags = tags.Remove(TagFeetInLiquid);
		tags = tags.Remove(TagOnGround);
		tags = tags.Remove(TagAlive);
		EntityTagsUpdate(ref tags);
	}

	protected virtual void EntityAgentTagsInitialUpdate(EntityAgent entityAgent, ref EntityTagArray tags)
	{
		tags = tags.Remove(TagMoving);
		tags = tags.Remove(TagAiming);
		tags = tags.Remove(TagFlying);
		tags = tags.Remove(TagSneaking);
		tags = tags.Remove(TagSprinting);
		EntityAgentTagsUpdate(entityAgent, ref tags);
	}

	protected virtual void EntityAgentHandItemsTagsInitialUpdate(EntityAgent entityAgent, ref EntityTagArray tags)
	{
		ItemStack itemStack = entityAgent.RightHandItemSlot?.Itemstack;
		ItemTagArray itemTagArray = itemStack?.Item?.Tags ?? ItemTagArray.Empty;
		BlockTagArray blockTagArray = itemStack?.Block?.Tags ?? BlockTagArray.Empty;
		itemStack = entityAgent.LeftHandItemSlot?.Itemstack;
		if (itemStack?.Item != null)
		{
			itemTagArray |= itemStack.Item.Tags;
		}
		if (itemStack?.Block != null)
		{
			blockTagArray |= itemStack.Block.Tags;
		}
		InitializeTag(ref tags, Armed = itemTagArray.ContainsAll(ItemTagWeapon), TagArmed);
		InitializeTag(ref tags, ArmedMelee = itemTagArray.ContainsAll(ItemTagWeaponMelee), TagArmedMelee);
		InitializeTag(ref tags, ArmedRanged = itemTagArray.ContainsAll(ItemTagWeaponRanged), TagArmedRanged);
		InitializeTag(ref tags, HoldingOpenFire = itemTagArray.ContainsAll(ItemTagHasOpenFire) || blockTagArray.ContainsAll(BlockTagHasOpenFire), TagHoldingOpenFire);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected virtual void UpdateTag(ref EntityTagArray tags, ref bool storedValue, EntityTagArray mask, bool newValue)
	{
		if (storedValue != newValue)
		{
			storedValue = newValue;
			if (newValue)
			{
				tags |= mask;
			}
			else
			{
				tags &= ~mask;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected virtual void InitializeTag(ref EntityTagArray tags, bool newValue, EntityTagArray mask)
	{
		if (newValue)
		{
			tags |= mask;
		}
		else
		{
			tags &= ~mask;
		}
	}

	protected virtual void EntityTagsUpdate(ref EntityTagArray tags)
	{
		UpdateTag(ref tags, ref Swimming, TagSwimming, entity.Swimming);
		UpdateTag(ref tags, ref FeetInLiquid, TagFeetInLiquid, entity.FeetInLiquid && !Swimming);
		UpdateTag(ref tags, ref OnGround, TagOnGround, entity.OnGround);
		UpdateTag(ref tags, ref Alive, TagAlive, entity.Alive);
	}

	protected virtual void EntityAgentTagsUpdate(EntityAgent entityAgent, ref EntityTagArray tags)
	{
		EntityControls controls = entityAgent.Controls;
		UpdateTag(ref tags, ref Moving, TagMoving, controls.Forward || controls.Backward || controls.Right || controls.Left || controls.Jump || controls.Gliding);
		UpdateTag(ref tags, ref Aiming, TagAiming, controls.IsAiming);
		UpdateTag(ref tags, ref Flying, TagFlying, controls.IsFlying);
		UpdateTag(ref tags, ref Sneaking, TagSneaking, controls.Sneak);
		UpdateTag(ref tags, ref Sprinting, TagSprinting, controls.Sprint);
	}

	protected virtual void EntityAgentHandItemsTagsUpdate(EntityAgent entityAgent, ref EntityTagArray tags)
	{
		ItemStack obj = entityAgent.ActiveHandItemSlot?.Itemstack;
		Item item = obj?.Item;
		if (item == null)
		{
			UpdateTag(ref tags, ref Armed, TagArmed, newValue: false);
			UpdateTag(ref tags, ref ArmedMelee, TagArmedMelee, newValue: false);
			UpdateTag(ref tags, ref ArmedRanged, TagArmedRanged, newValue: false);
		}
		else
		{
			UpdateTag(ref tags, ref Armed, TagArmed, ItemTagWeapon.isPresentIn(ref item.Tags));
			UpdateTag(ref tags, ref ArmedMelee, TagArmedMelee, ItemTagWeaponMelee.isPresentIn(ref item.Tags));
			UpdateTag(ref tags, ref ArmedRanged, TagArmedRanged, ItemTagWeaponRanged.isPresentIn(ref item.Tags));
		}
		Block block = obj?.Block;
		UpdateTag(ref tags, ref HoldingOpenFire, TagHoldingOpenFire, (item != null && ItemTagHasOpenFire.isPresentIn(ref item.Tags)) || (block != null && BlockTagHasOpenFire.isPresentIn(ref block.Tags)));
	}
}
