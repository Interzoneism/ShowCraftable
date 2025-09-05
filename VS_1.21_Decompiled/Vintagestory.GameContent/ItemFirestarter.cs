using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemFirestarter : Item
{
	private string igniteAnimation;

	public override void OnLoaded(ICoreAPI api)
	{
		igniteAnimation = Attributes["igniteAnimation"].AsString();
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		if (blockSel == null)
		{
			return;
		}
		Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return;
		}
		EnumIgniteState enumIgniteState = EnumIgniteState.NotIgnitable;
		if (!(block is IIgnitable ignitable) || (enumIgniteState = ignitable.OnTryIgniteBlock(byEntity, blockSel.Position, 0f)) != EnumIgniteState.Ignitable)
		{
			if (enumIgniteState == EnumIgniteState.NotIgnitablePreventDefault)
			{
				handling = EnumHandHandling.PreventDefault;
			}
			return;
		}
		handling = EnumHandHandling.PreventDefault;
		byEntity.AnimManager.StartAnimation(igniteAnimation);
		if (api.Side == EnumAppSide.Client)
		{
			api.Event.UnregisterCallback(ObjectCacheUtil.TryGet<long>(api, "firestartersound"));
			api.ObjectCache["firestartersound"] = api.Event.RegisterCallback(delegate
			{
				byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/handdrill"), byEntity, byPlayer, randomizePitch: false, 16f);
			}, 500);
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null)
		{
			api.Event.UnregisterCallback(ObjectCacheUtil.TryGet<long>(api, "firestartersound"));
			return false;
		}
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			api.Event.UnregisterCallback(ObjectCacheUtil.TryGet<long>(api, "firestartersound"));
			return false;
		}
		Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
		EnumIgniteState enumIgniteState = EnumIgniteState.NotIgnitable;
		if (block is IIgnitable ignitable)
		{
			enumIgniteState = ignitable.OnTryIgniteBlock(byEntity, blockSel.Position, secondsUsed);
		}
		if (enumIgniteState == EnumIgniteState.NotIgnitable || enumIgniteState == EnumIgniteState.NotIgnitablePreventDefault)
		{
			api.Event.UnregisterCallback(ObjectCacheUtil.TryGet<long>(api, "firestartersound"));
			return false;
		}
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform modelTransform = new ModelTransform();
			modelTransform.EnsureDefaultValues();
			float num = GameMath.Clamp(1f - 2f * secondsUsed, 0f, 1f);
			Random rand = api.World.Rand;
			modelTransform.Translation.Set(num * num * num * 1.6f - 1.6f, 0f, 0f);
			modelTransform.Rotation.Y = 0f - Math.Min(secondsUsed * 120f, 30f);
			if (secondsUsed > 0.5f)
			{
				modelTransform.Translation.Add((float)rand.NextDouble() * 0.1f, (float)rand.NextDouble() * 0.1f, (float)rand.NextDouble() * 0.1f);
				(api as ICoreClientAPI).World.SetCameraShake(0.04f);
			}
			if (secondsUsed > 0.25f && (int)(30f * secondsUsed) % 2 == 1)
			{
				Vec3d basePos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition);
				Block block2 = byEntity.World.GetBlock(new AssetLocation("fire"));
				AdvancedParticleProperties advancedParticleProperties = block2.ParticleProperties[block2.ParticleProperties.Length - 1].Clone();
				advancedParticleProperties.basePos = basePos;
				advancedParticleProperties.Quantity.avg = 0.3f;
				advancedParticleProperties.Size.avg = 0.03f;
				byEntity.World.SpawnParticles(advancedParticleProperties, player);
				advancedParticleProperties.Quantity.avg = 0f;
			}
		}
		return enumIgniteState == EnumIgniteState.Ignitable;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		byEntity.AnimManager.StopAnimation(igniteAnimation);
		if (blockSel == null || api.World.Side == EnumAppSide.Client || api.World.Rand.NextDouble() > 0.25)
		{
			return;
		}
		Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
		EnumIgniteState enumIgniteState = EnumIgniteState.NotIgnitable;
		IIgnitable ignitable = block as IIgnitable;
		if (ignitable != null)
		{
			enumIgniteState = ignitable.OnTryIgniteBlock(byEntity, blockSel.Position, secondsUsed);
		}
		if (enumIgniteState != EnumIgniteState.IgniteNow)
		{
			api.Event.UnregisterCallback(ObjectCacheUtil.TryGet<long>(api, "firestartersound"));
			return;
		}
		DamageItem(api.World, byEntity, slot);
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			EnumHandling handling = EnumHandling.PassThrough;
			ignitable.OnTryIgniteBlockOver(byEntity, blockSel.Position, secondsUsed, ref handling);
		}
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		byEntity.AnimManager.StopAnimation(igniteAnimation);
		api.Event.UnregisterCallback(ObjectCacheUtil.TryGet<long>(api, "firestartersound"));
		return true;
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		WorldInteraction[] value = new WorldInteraction[1]
		{
			new WorldInteraction
			{
				HotKeyCode = "shift",
				ActionLangCode = "heldhelp-igniteblock",
				MouseButton = EnumMouseButton.Right
			}
		};
		return base.GetHeldInteractionHelp(inSlot).Append(value);
	}
}
