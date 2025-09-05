using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public abstract class CollectibleBehavior
{
	public CollectibleObject collObj;

	public string propertiesAtString;

	public virtual bool ClientSideOptional => false;

	public CollectibleBehavior(CollectibleObject collObj)
	{
		this.collObj = collObj;
	}

	public virtual void Initialize(JsonObject properties)
	{
		propertiesAtString = properties.ToString();
	}

	public virtual void OnLoaded(ICoreAPI api)
	{
	}

	public virtual void OnUnloaded(ICoreAPI api)
	{
	}

	public virtual EnumItemStorageFlags GetStorageFlags(ItemStack itemstack, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return (EnumItemStorageFlags)0;
	}

	public virtual void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handHandling, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
	}

	public virtual bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
	}

	public virtual void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
	}

	public virtual bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
	}

	public virtual bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handled)
	{
		return true;
	}

	public virtual void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
	}

	public virtual WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return Array.Empty<WorldInteraction>();
	}

	public virtual SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		return null;
	}

	public virtual int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
	{
		return 0;
	}

	public virtual void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
	{
	}

	public virtual void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
	}

	public virtual void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
	{
	}

	public virtual bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier, ref EnumHandling bhHandling)
	{
		bhHandling = EnumHandling.PassThrough;
		return true;
	}

	public virtual float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter, ref EnumHandling handled)
	{
		handled = EnumHandling.PassThrough;
		return remainingResistance;
	}

	public virtual string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity, ref EnumHandling bhHandling)
	{
		return null;
	}

	public virtual string GetHeldReadyAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand, ref EnumHandling bhHandling)
	{
		return null;
	}

	public virtual string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand, ref EnumHandling bhHandling)
	{
		return null;
	}

	public virtual string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity, ref EnumHandling bhHandling)
	{
		return null;
	}

	[Obsolete("Use OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe, ref EnumHandling bhHandling) instead")]
	public virtual void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, ref EnumHandling bhHandling)
	{
	}

	public virtual void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe, ref EnumHandling bhHandling)
	{
		OnCreatedByCrafting(allInputslots, outputSlot, ref bhHandling);
	}

	public virtual float OnGetMiningSpeed(IItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer, ref EnumHandling bhHandling)
	{
		return 1f;
	}

	public virtual int OnGetMaxDurability(ItemStack itemstack, ref EnumHandling bhHandling)
	{
		return 0;
	}

	public virtual int OnGetRemainingDurability(ItemStack itemstack, ref EnumHandling bhHandling)
	{
		return 0;
	}

	public virtual void OnDamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, ref int amount, ref EnumHandling bhHandling)
	{
	}

	public virtual void OnSetDurability(ItemStack itemstack, ref int amount, ref EnumHandling bhHandling)
	{
	}

	public virtual ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties props, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return null;
	}

	public virtual void OnHandbookRecipeRender(ICoreClientAPI capi, GridRecipe recipe, ItemSlot slot, double x, double y, double z, double size)
	{
	}
}
