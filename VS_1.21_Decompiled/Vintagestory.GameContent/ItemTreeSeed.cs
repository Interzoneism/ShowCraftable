using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemTreeSeed : Item
{
	private WorldInteraction[] interactions;

	private bool isMapleSeed;

	public override void OnLoaded(ICoreAPI api)
	{
		isMapleSeed = Variant["type"] == "maple" || Variant["type"] == "crimsonkingmaple";
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "treeSeedInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Block block in api.World.Blocks)
			{
				if (!(block.Code == null) && block.EntityClass != null && block.Fertility > 0)
				{
					list.Add(new ItemStack(block));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-plant",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "shift",
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		if (isMapleSeed && target == EnumItemRenderTarget.Ground)
		{
			EntityItem ei = (renderinfo.InSlot as EntityItemSlot).Ei;
			if (!ei.Collided && !ei.Swimming)
			{
				renderinfo.Transform = renderinfo.Transform.Clone();
				renderinfo.Transform.Rotation.X = -90f;
				renderinfo.Transform.Rotation.Y = (float)((double)capi.World.ElapsedMilliseconds % 360.0) * 2f;
				renderinfo.Transform.Rotation.Z = 0f;
			}
		}
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null || !byEntity.Controls.ShiftKey)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		string text = Variant["type"];
		Block block = byEntity.World.GetBlock(AssetLocation.Create("sapling-" + text + "-free", Code.Domain));
		if (block == null)
		{
			return;
		}
		IPlayer player = null;
		if (byEntity is EntityPlayer)
		{
			player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		blockSel = blockSel.Clone();
		blockSel.Position.Up();
		string failureCode = "";
		if (!block.TryPlaceBlock(api.World, player, itemslot.Itemstack, blockSel, ref failureCode))
		{
			if (api is ICoreClientAPI coreClientAPI && failureCode != null && failureCode != "__ignore__")
			{
				coreClientAPI.TriggerIngameError(this, failureCode, Lang.Get("placefailure-" + failureCode));
			}
		}
		else
		{
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/dirt1"), (float)blockSel.Position.X + 0.5f, blockSel.Position.InternalY, (float)blockSel.Position.Z + 0.5f, player);
			((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			if (player == null || player.WorldData?.CurrentGameMode != EnumGameMode.Creative)
			{
				itemslot.TakeOut(1);
				itemslot.MarkDirty();
			}
		}
		handHandling = EnumHandHandling.PreventDefault;
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
