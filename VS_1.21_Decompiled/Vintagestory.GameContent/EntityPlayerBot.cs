using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityPlayerBot : EntityAnimalBot
{
	private EntityBehaviorSeraphInventory invbh;

	protected string lastRunningHeldUseAnimation;

	protected string lastRunningRightHeldIdleAnimation;

	protected string lastRunningLeftHeldIdleAnimation;

	protected string lastRunningHeldHitAnimation;

	public override bool StoreWithChunk => true;

	public override ItemSlot RightHandItemSlot => invbh.Inventory[15];

	public override ItemSlot LeftHandItemSlot => invbh.Inventory[16];

	public override void Initialize(EntityProperties properties, ICoreAPI api, long chunkindex3d)
	{
		base.Initialize(properties, api, chunkindex3d);
		Name = WatchedAttributes.GetTreeAttribute("nametag")?.GetString("name");
		invbh = GetBehavior<EntityBehaviorSeraphInventory>();
	}

	public override void OnEntitySpawn()
	{
		base.OnEntitySpawn();
		if (World.Side == EnumAppSide.Client)
		{
			(base.Properties.Client.Renderer as EntityShapeRenderer).DoRenderHeldItem = true;
		}
		JsonObject jsonObject = base.Properties.Attributes?["inventory"];
		if (jsonObject == null || !jsonObject.Exists)
		{
			return;
		}
		JsonItemStack[] array = jsonObject.AsArray<JsonItemStack>();
		foreach (JsonItemStack jsonItemStack in array)
		{
			if (jsonItemStack.Resolve(World, "player bot inventory"))
			{
				TryGiveItemStack(jsonItemStack.ResolvedItemstack);
			}
		}
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (WatchedAttributes.GetString("currentCommand", "") != "")
		{
			AnimManager.StopAnimation("idle");
			AnimManager.StopAnimation("idle1");
		}
		HandleHandAnimations(dt);
	}

	protected override void HandleHandAnimations(float dt)
	{
		ItemStack obj = RightHandItemSlot?.Itemstack;
		EnumHandInteract handUse = servercontrols.HandUse;
		bool flag = handUse == EnumHandInteract.BlockInteract || handUse == EnumHandInteract.HeldItemInteract || (servercontrols.RightMouseDown && !servercontrols.LeftMouseDown);
		bool flag2 = lastRunningHeldUseAnimation != null && AnimManager.ActiveAnimationsByAnimCode.ContainsKey(lastRunningHeldUseAnimation);
		bool flag3 = handUse == EnumHandInteract.HeldItemAttack || servercontrols.LeftMouseDown;
		bool flag4 = lastRunningHeldHitAnimation != null && AnimManager.ActiveAnimationsByAnimCode.ContainsKey(lastRunningHeldHitAnimation);
		string text = obj?.Collectible.GetHeldTpUseAnimation(RightHandItemSlot, this);
		string text2 = obj?.Collectible.GetHeldTpHitAnimation(RightHandItemSlot, this);
		string text3 = obj?.Collectible.GetHeldTpIdleAnimation(RightHandItemSlot, this, EnumHand.Right);
		string text4 = LeftHandItemSlot?.Itemstack?.Collectible.GetHeldTpIdleAnimation(LeftHandItemSlot, this, EnumHand.Left);
		bool flag5 = text3 != null && !flag && !flag3;
		bool flag6 = lastRunningRightHeldIdleAnimation != null && AnimManager.ActiveAnimationsByAnimCode.ContainsKey(lastRunningRightHeldIdleAnimation);
		bool flag7 = text4 != null;
		bool flag8 = lastRunningLeftHeldIdleAnimation != null && AnimManager.ActiveAnimationsByAnimCode.ContainsKey(lastRunningLeftHeldIdleAnimation);
		if (obj == null)
		{
			text2 = "breakhand";
			text = "interactstatic";
		}
		if (flag != flag2 || (lastRunningHeldUseAnimation != null && text != lastRunningHeldUseAnimation))
		{
			AnimManager.StopAnimation(lastRunningHeldUseAnimation);
			lastRunningHeldUseAnimation = null;
			if (flag)
			{
				AnimManager.StopAnimation(lastRunningRightHeldIdleAnimation);
				AnimManager.StartAnimation(lastRunningHeldUseAnimation = text);
			}
		}
		if (flag3 != flag4 || (lastRunningHeldHitAnimation != null && text2 != lastRunningHeldHitAnimation))
		{
			AnimManager.StopAnimation(lastRunningHeldHitAnimation);
			lastRunningHeldHitAnimation = null;
			if (flag3)
			{
				AnimManager.StopAnimation(lastRunningLeftHeldIdleAnimation);
				AnimManager.StopAnimation(lastRunningRightHeldIdleAnimation);
				AnimManager.StartAnimation(lastRunningHeldHitAnimation = text2);
			}
		}
		if (flag5 != flag6 || (lastRunningRightHeldIdleAnimation != null && text3 != lastRunningRightHeldIdleAnimation))
		{
			AnimManager.StopAnimation(lastRunningRightHeldIdleAnimation);
			lastRunningRightHeldIdleAnimation = null;
			if (flag5)
			{
				AnimManager.StartAnimation(lastRunningRightHeldIdleAnimation = text3);
			}
		}
		if (flag7 != flag8 || (lastRunningLeftHeldIdleAnimation != null && text4 != lastRunningLeftHeldIdleAnimation))
		{
			AnimManager.StopAnimation(lastRunningLeftHeldIdleAnimation);
			lastRunningLeftHeldIdleAnimation = null;
			if (flag7)
			{
				AnimManager.StartAnimation(lastRunningLeftHeldIdleAnimation = text4);
			}
		}
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
	{
		base.OnInteract(byEntity, slot, hitPosition, mode);
		if (byEntity is EntityPlayer entityPlayer && entityPlayer.Controls.Sneak && mode == EnumInteractMode.Interact && byEntity.World.Side == EnumAppSide.Server && entityPlayer.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			if (!LeftHandItemSlot.Empty || !RightHandItemSlot.Empty)
			{
				LeftHandItemSlot.Itemstack = null;
				RightHandItemSlot.Itemstack = null;
			}
			else
			{
				invbh.Inventory.DiscardAll();
			}
			WatchedAttributes.MarkAllDirty();
		}
	}
}
