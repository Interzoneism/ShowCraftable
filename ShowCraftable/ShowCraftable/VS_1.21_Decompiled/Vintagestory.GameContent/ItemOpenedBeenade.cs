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

public class ItemOpenedBeenade : Item
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "openedBeenadeInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Block block in api.World.Blocks)
			{
				if (!(block.Code == null) && block is BlockSkep && block.Variant["type"].Equals("populated"))
				{
					list.Add(new ItemStack(block));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-fill",
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

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel != null && byEntity.World.Claims.TryAccess((byEntity as EntityPlayer)?.Player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
			if (block is BlockSkep && block.Variant["type"].Equals("populated"))
			{
				handling = EnumHandHandling.PreventDefaultAction;
			}
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null)
		{
			return false;
		}
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform modelTransform = new ModelTransform();
			modelTransform.EnsureDefaultValues();
			float num = GameMath.Clamp(secondsUsed * 3f, 0f, 2f);
			modelTransform.Translation.Set(0f - num, num / 4f, 0f);
		}
		SimpleParticleProperties bees = BlockEntityBeehive.Bees;
		BlockPos position = blockSel.Position;
		Random rand = byEntity.World.Rand;
		Vec3d vec3d = new Vec3d((double)position.X + rand.NextDouble(), (double)position.Y + rand.NextDouble() * 0.25, (double)position.Z + rand.NextDouble());
		Vec3d vec3d2 = new Vec3d(byEntity.SidedPos.X, byEntity.SidedPos.Y + byEntity.LocalEyePos.Y - 0.20000000298023224, byEntity.SidedPos.Z);
		Vec3f vec3f = new Vec3f((float)(vec3d2.X - vec3d.X), (float)(vec3d2.Y - vec3d.Y), (float)(vec3d2.Z - vec3d.Z));
		vec3f.Normalize();
		vec3f *= 2f;
		bees.MinPos = vec3d;
		bees.MinVelocity = vec3f;
		bees.WithTerrainCollision = true;
		IPlayer dualCallByPlayer = null;
		if (byEntity is EntityPlayer)
		{
			dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		byEntity.World.SpawnParticles(bees, dualCallByPlayer);
		return secondsUsed < 4f;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		return true;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null || !byEntity.World.Claims.TryAccess((byEntity as EntityPlayer)?.Player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			return;
		}
		Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
		if (block is BlockSkep && block.Variant["type"].Equals("populated") && !(secondsUsed < 3.9f))
		{
			slot.TakeOut(1);
			slot.MarkDirty();
			IPlayer player = null;
			if (byEntity is EntityPlayer)
			{
				player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			player?.InventoryManager.TryGiveItemstack(new ItemStack(byEntity.World.GetItem(CodeWithVariant("type", "closed"))));
			Block block2 = byEntity.World.GetBlock(block.CodeWithVariant("type", "empty"));
			byEntity.World.BlockAccessor.SetBlock(block2.BlockId, blockSel.Position);
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		if (inSlot.Itemstack.Collectible.Attributes != null)
		{
			dsc.AppendLine(Lang.Get("Fill it up with bees and throw it for a stingy surprise"));
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
