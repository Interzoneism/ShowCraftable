using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemHoe : Item
{
	private WorldInteraction[]? interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		ICoreClientAPI capi = api as ICoreClientAPI;
		if (capi == null)
		{
			return;
		}
		interactions = ObjectCacheUtil.GetOrCreate(capi, "hoeInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Block block in capi.World.Blocks)
			{
				if (!(block.Code == null) && block.Code.PathStartsWith("soil"))
				{
					list.Add(new ItemStack(block));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-till",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null)
		{
			return;
		}
		if (byEntity.Controls.ShiftKey && byEntity.Controls.CtrlKey)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		BlockPos position = blockSel.Position;
		Block block = byEntity.World.BlockAccessor.GetBlock(position);
		if (byEntity.World.BlockAccessor.GetBlock(position.UpCopy()).Id != 0)
		{
			(api as ICoreClientAPI)?.TriggerIngameError(this, "covered", Lang.Get("Requires no block above"));
			handHandling = EnumHandHandling.PreventDefault;
			return;
		}
		byEntity.Attributes.SetInt("didtill", 0);
		if (block.Code.PathStartsWith("soil"))
		{
			handHandling = EnumHandHandling.PreventDefault;
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null)
		{
			return false;
		}
		if (byEntity.Controls.ShiftKey && byEntity.Controls.CtrlKey)
		{
			return false;
		}
		if (byEntity.World.BlockAccessor.GetBlock(blockSel.Position.UpCopy()).BlockId != 0)
		{
			return false;
		}
		IPlayer dualCallByPlayer = (byEntity as EntityPlayer)?.Player;
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform modelTransform = new ModelTransform();
			modelTransform.EnsureDefaultValues();
			float num = GameMath.Clamp(secondsUsed * 18f, 0f, 2f);
			float num2 = GameMath.SmoothStep(2.5f * GameMath.Clamp(secondsUsed - 0.35f, 0f, 1f));
			float x = ((secondsUsed > 0.35f && secondsUsed < 0.75f) ? (GameMath.Sin(secondsUsed * 50f) / 60f) : 0f);
			float num3 = Math.Max(0f, num - GameMath.Clamp(24f * (secondsUsed - 0.75f), 0f, 2f));
			float num4 = Math.Max(0f, num2 - Math.Max(0f, 20f * (secondsUsed - 0.75f)));
			modelTransform.Origin.Set(0f, 0f, 0.5f);
			modelTransform.Rotation.Set(0f, num3 * 45f, 0f);
			modelTransform.Translation.Set(x, 0f, num4 / 2f);
		}
		if (secondsUsed > 0.35f && secondsUsed < 0.87f)
		{
			Vec3d vec3d = new Vec3d().AheadCopy(1.0, 0f, byEntity.SidedPos.Yaw - (float)Math.PI);
			Vec3d vec3d2 = blockSel.Position.ToVec3d().Add(0.5 + vec3d.X, 1.03, 0.5 + vec3d.Z);
			vec3d2.X -= vec3d.X * (double)secondsUsed * 1.0 / 0.75 * 1.2000000476837158;
			vec3d2.Z -= vec3d.Z * (double)secondsUsed * 1.0 / 0.75 * 1.2000000476837158;
			byEntity.World.SpawnCubeParticles(blockSel.Position, vec3d2, 0.25f, 3, 0.5f, dualCallByPlayer);
		}
		if (secondsUsed > 0.6f && byEntity.Attributes.GetInt("didtill") == 0 && byEntity.World.Side == EnumAppSide.Server)
		{
			byEntity.Attributes.SetInt("didtill", 1);
			DoTill(secondsUsed, slot, byEntity, blockSel, entitySel);
		}
		return secondsUsed < 1f;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		return false;
	}

	public virtual void DoTill(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null)
		{
			return;
		}
		BlockPos position = blockSel.Position;
		Block block = byEntity.World.BlockAccessor.GetBlock(position);
		if (!block.Code.PathStartsWith("soil"))
		{
			return;
		}
		string text = block.LastCodePart(1);
		Block block2 = byEntity.World.GetBlock(new AssetLocation("farmland-dry-" + text));
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (block2 != null && player != null)
		{
			if (block.Sounds != null)
			{
				byEntity.World.PlaySoundAt(block.Sounds.Place, position, 0.4);
			}
			byEntity.World.BlockAccessor.SetBlock(block2.BlockId, position);
			slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, player.InventoryManager.ActiveHotbarSlot);
			if (slot.Empty)
			{
				byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.Pos.X, byEntity.Pos.InternalY, byEntity.Pos.Z);
			}
			BlockEntity blockEntity = byEntity.World.BlockAccessor.GetBlockEntity(position);
			if (blockEntity is BlockEntityFarmland)
			{
				((BlockEntityFarmland)blockEntity).OnCreatedFromSoil(block);
			}
			byEntity.World.BlockAccessor.MarkBlockDirty(position);
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append<WorldInteraction>(base.GetHeldInteractionHelp(inSlot));
	}
}
