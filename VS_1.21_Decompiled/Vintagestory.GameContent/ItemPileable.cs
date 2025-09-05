using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public abstract class ItemPileable : Item
{
	protected abstract AssetLocation PileBlockCode { get; }

	public virtual bool IsPileable => true;

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (!IsPileable)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
			return;
		}
		if (blockSel == null || byEntity?.World == null || !byEntity.Controls.ShiftKey)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
			return;
		}
		BlockPos position = blockSel.Position;
		IPlayer player = null;
		if (byEntity is EntityPlayer)
		{
			player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (player == null)
		{
			return;
		}
		if (!byEntity.World.Claims.TryAccess(player, position, EnumBlockAccessFlags.BuildOrBreak))
		{
			api.World.BlockAccessor.MarkBlockEntityDirty(position.AddCopy(blockSel.Face));
			api.World.BlockAccessor.MarkBlockDirty(position.AddCopy(blockSel.Face));
			return;
		}
		Block block = byEntity.World.BlockAccessor.GetBlock(position);
		BlockEntity blockEntity = byEntity.World.BlockAccessor.GetBlockEntity(position);
		if (blockEntity is BlockEntityLabeledChest || blockEntity is BlockEntitySignPost || blockEntity is BlockEntitySign || blockEntity is BlockEntityBloomery || blockEntity is BlockEntityFirepit || blockEntity is BlockEntityForge || blockEntity is BlockEntityCrate || block.HasBehavior<BlockBehaviorJonasGasifier>())
		{
			return;
		}
		IBlockEntityItemPile obj = blockEntity?.Block.GetInterface<IBlockEntityItemPile>(api.World, position);
		if (obj != null && obj.OnPlayerInteract(player))
		{
			handling = EnumHandHandling.PreventDefaultAction;
			((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		}
		else
		{
			if (!byEntity.World.Claims.TryAccess(player, position.AddCopy(blockSel.Face), EnumBlockAccessFlags.BuildOrBreak))
			{
				return;
			}
			IBlockEntityItemPile obj2 = blockEntity?.Block.GetInterface<IBlockEntityItemPile>(api.World, position.AddCopy(blockSel.Face));
			if (obj2 != null && obj2.OnPlayerInteract(player))
			{
				handling = EnumHandHandling.PreventDefaultAction;
				((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				return;
			}
			Block block2 = byEntity.World.GetBlock(PileBlockCode);
			if (block2 != null)
			{
				BlockPos blockPos = position.Copy();
				if (byEntity.World.BlockAccessor.GetBlock(blockPos).Replaceable < 6000)
				{
					blockPos.Add(blockSel.Face);
				}
				bool num = ((IBlockItemPile)block2).Construct(slot, byEntity.World, blockPos, player);
				Cuboidf[] collisionBoxes = byEntity.World.BlockAccessor.GetBlock(blockPos).GetCollisionBoxes(byEntity.World.BlockAccessor, blockPos);
				if (collisionBoxes != null && collisionBoxes.Length != 0 && CollisionTester.AabbIntersect(collisionBoxes[0], blockPos.X, blockPos.Y, blockPos.Z, player.Entity.SelectionBox, player.Entity.SidedPos.XYZ))
				{
					player.Entity.SidedPos.Y += (double)collisionBoxes[0].Y2 - (player.Entity.SidedPos.Y - (double)(int)player.Entity.SidedPos.Y);
				}
				if (num)
				{
					handling = EnumHandHandling.PreventDefaultAction;
					((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				}
				else
				{
					base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
				}
			}
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		if (!IsPileable)
		{
			return base.GetHeldInteractionHelp(inSlot);
		}
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				HotKeyCode = "shift",
				ActionLangCode = "heldhelp-place",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
