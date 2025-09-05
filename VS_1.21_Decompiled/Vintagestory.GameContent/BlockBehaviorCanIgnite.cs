using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorCanIgnite : BlockBehavior
{
	public static List<ItemStack> CanIgniteStacks(ICoreAPI api, bool withFirestarter)
	{
		List<ItemStack> orCreate = ObjectCacheUtil.GetOrCreate(api, "canIgniteStacks", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			List<ItemStack> canIgniteStacksWithFirestarter = new List<ItemStack>();
			foreach (CollectibleObject collectible in api.World.Collectibles)
			{
				if (collectible is Block block)
				{
					if (block.HasBehavior<BlockBehaviorCanIgnite>())
					{
						List<ItemStack> handBookStacks = collectible.GetHandBookStacks(api as ICoreClientAPI);
						if (handBookStacks != null)
						{
							list.AddRange(handBookStacks);
							canIgniteStacksWithFirestarter.AddRange(handBookStacks);
						}
					}
				}
				else if (collectible is ItemFirestarter)
				{
					List<ItemStack> handBookStacks2 = collectible.GetHandBookStacks(api as ICoreClientAPI);
					canIgniteStacksWithFirestarter.AddRange(handBookStacks2);
				}
			}
			ObjectCacheUtil.GetOrCreate(api, "canIgniteStacksWithFirestarter", () => canIgniteStacksWithFirestarter);
			return list;
		});
		List<ItemStack> orCreate2 = ObjectCacheUtil.GetOrCreate(api, "canIgniteStacksWithFirestarter", () => new List<ItemStack>());
		if (!withFirestarter)
		{
			return orCreate;
		}
		return orCreate2;
	}

	public BlockBehaviorCanIgnite(Block block)
		: base(block)
	{
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling blockHandling)
	{
		if (blockSel == null)
		{
			return;
		}
		Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			EnumIgniteState enumIgniteState = EnumIgniteState.NotIgnitable;
			IIgnitable ignitable = block.GetInterface<IIgnitable>(byEntity.World, blockSel.Position);
			if (ignitable != null)
			{
				enumIgniteState = ignitable.OnTryIgniteBlock(byEntity, blockSel.Position, 0f);
			}
			if (enumIgniteState == EnumIgniteState.NotIgnitablePreventDefault)
			{
				blockHandling = EnumHandling.PreventDefault;
				handHandling = EnumHandHandling.PreventDefault;
			}
			if (byEntity.Controls.ShiftKey || enumIgniteState == EnumIgniteState.Ignitable)
			{
				blockHandling = EnumHandling.PreventDefault;
				handHandling = EnumHandHandling.PreventDefault;
				byEntity.World.PlaySoundAt(new AssetLocation("sounds/torch-ignite"), byEntity, player, randomizePitch: false, 16f);
			}
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
	{
		if (blockSel == null)
		{
			return false;
		}
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		Block obj = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
		EnumIgniteState enumIgniteState = EnumIgniteState.NotIgnitable;
		IIgnitable ignitable = obj.GetInterface<IIgnitable>(byEntity.World, blockSel.Position);
		if (ignitable != null)
		{
			enumIgniteState = ignitable.OnTryIgniteBlock(byEntity, blockSel.Position, secondsUsed);
		}
		if (enumIgniteState == EnumIgniteState.NotIgnitablePreventDefault)
		{
			return false;
		}
		handling = EnumHandling.PreventDefault;
		if (byEntity.World is IClientWorldAccessor && secondsUsed > 0.25f && (int)(30f * secondsUsed) % 2 == 1)
		{
			Random rand = byEntity.World.Rand;
			Vec3d basePos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition).Add(rand.NextDouble() * 0.25 - 0.125, rand.NextDouble() * 0.25 - 0.125, rand.NextDouble() * 0.25 - 0.125);
			Block block = byEntity.World.GetBlock(new AssetLocation("fire"));
			AdvancedParticleProperties advancedParticleProperties = block.ParticleProperties[block.ParticleProperties.Length - 1].Clone();
			advancedParticleProperties.basePos = basePos;
			advancedParticleProperties.Quantity.avg = 0.5f;
			byEntity.World.SpawnParticles(advancedParticleProperties, player);
			advancedParticleProperties.Quantity.avg = 0f;
		}
		if (byEntity.World.Side == EnumAppSide.Server)
		{
			return true;
		}
		return (double)secondsUsed <= 3.2;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
	{
		if (blockSel == null || secondsUsed < 3f)
		{
			return;
		}
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return;
		}
		EnumHandling handling2 = EnumHandling.PassThrough;
		byEntity.World.BlockAccessor.GetBlock(blockSel.Position).GetInterface<IIgnitable>(byEntity.World, blockSel.Position)?.OnTryIgniteBlockOver(byEntity, blockSel.Position, secondsUsed, ref handling2);
		if (handling2 != EnumHandling.PassThrough)
		{
			return;
		}
		handling = EnumHandling.PreventDefault;
		if (byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak) && blockSel != null && byEntity.World.Side == EnumAppSide.Server)
		{
			BlockPos blockPos = blockSel.Position.AddCopy(blockSel.Face);
			if (byEntity.World.BlockAccessor.GetBlock(blockPos).BlockId == 0)
			{
				byEntity.World.BlockAccessor.SetBlock(byEntity.World.GetBlock(new AssetLocation("fire")).BlockId, blockPos);
				byEntity.World.BlockAccessor.GetBlockEntity(blockPos)?.GetBehavior<BEBehaviorBurning>()?.OnFirePlaced(blockSel.Face, (byEntity as EntityPlayer).PlayerUID);
			}
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				HotKeyCode = "shift",
				ActionLangCode = "heldhelp-igniteblock",
				MouseButton = EnumMouseButton.Right
			}
		};
	}
}
