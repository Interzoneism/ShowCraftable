using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemFlint : Item
{
	public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
	{
		if (slot.Itemstack?.Collectible == this)
		{
			EntityAgent obj = byEntity as EntityAgent;
			if (obj != null && obj.Controls.FloorSitting)
			{
				return "knapsitting";
			}
			return "knap";
		}
		return base.GetHeldTpHitAnimation(slot, byEntity);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (byEntity.Controls.ShiftKey && blockSel != null)
		{
			IWorldAccessor world = byEntity.World;
			Block block = world.GetBlock(new AssetLocation("knappingsurface"));
			if (block == null)
			{
				return;
			}
			if (!world.BlockAccessor.GetBlock(blockSel.Position).CanAttachBlockAt(byEntity.World.BlockAccessor, block, blockSel.Position, BlockFacing.UP))
			{
				if (api.Side == EnumAppSide.Client)
				{
					(api as ICoreClientAPI).TriggerIngameError(this, "cantplace", Lang.Get("Cannot place a knapping surface here"));
				}
				return;
			}
			BlockPos blockPos = blockSel.Position.AddCopy(blockSel.Face);
			if (!world.BlockAccessor.GetBlock(blockPos).IsReplacableBy(block))
			{
				return;
			}
			BlockSelection blockSelection = blockSel.Clone();
			blockSelection.Position = blockPos;
			blockSelection.DidOffset = true;
			string failureCode = "";
			if (!block.TryPlaceBlock(world, byPlayer, slot.Itemstack, blockSelection, ref failureCode))
			{
				(api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("placefailure-" + failureCode));
				return;
			}
			world.BlockAccessor.TriggerNeighbourBlockUpdate(blockPos);
			if (block.Sounds != null)
			{
				world.PlaySoundAt(block.Sounds.Place, blockPos, -0.5);
			}
			if (world.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityKnappingSurface blockEntityKnappingSurface)
			{
				blockEntityKnappingSurface.BaseMaterial = slot.Itemstack.Clone();
				blockEntityKnappingSurface.BaseMaterial.StackSize = 1;
				if (byEntity.World is IClientWorldAccessor)
				{
					blockEntityKnappingSurface.OpenDialog(world as IClientWorldAccessor, blockPos, slot.Itemstack);
				}
			}
			slot.TakeOut(1);
			handling = EnumHandHandling.PreventDefaultAction;
		}
		else
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		}
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockKnappingSurface && byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityKnappingSurface blockEntityKnappingSurface)
		{
			IPlayer player = null;
			if (byEntity is EntityPlayer)
			{
				player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			if (player != null && byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use))
			{
				blockEntityKnappingSurface.OnBeginUse(player, blockSel);
				handling = EnumHandHandling.PreventDefaultAction;
			}
		}
	}

	public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		return false;
	}

	public override bool OnHeldAttackStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
		return false;
	}

	public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null || !(byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockKnappingSurface) || !(byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityKnappingSurface blockEntityKnappingSurface))
		{
			return;
		}
		IPlayer player = null;
		if (byEntity is EntityPlayer)
		{
			player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (player != null)
		{
			GetToolMode(slot, player, blockSel);
			if (byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use) && byEntity.World is IClientWorldAccessor)
			{
				blockEntityKnappingSurface.OnUseOver(player, blockSel.SelectionBoxIndex, blockSel.Face, mouseMode: true);
			}
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-placetoknap",
				HotKeyCode = "shift",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
