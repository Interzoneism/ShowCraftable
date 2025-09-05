using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemRustyGear : Item
{
	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (blockSel == null || !byEntity.Controls.ShiftKey)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		}
		else
		{
			if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
			{
				return;
			}
			handling = EnumHandHandling.PreventDefault;
			Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
			if (block is BlockLooseGears)
			{
				if (int.TryParse(block.LastCodePart(), out var result) && result < 5)
				{
					Block block2 = byEntity.World.GetBlock(block.CodeWithPart((result + 1).ToString() ?? "", 1));
					byEntity.World.BlockAccessor.SetBlock(block2.BlockId, blockSel.Position);
					byEntity.World.PlaySoundAt(block.Sounds.Place, blockSel.Position, -0.5, player);
					slot.TakeOut(1);
				}
				return;
			}
			BlockPos blockPos = blockSel.Position.AddCopy(blockSel.Face);
			if (byEntity.World.Claims.TryAccess((byEntity as EntityPlayer)?.Player, blockPos, EnumBlockAccessFlags.BuildOrBreak))
			{
				block = byEntity.World.BlockAccessor.GetBlock(blockPos);
				Block block3 = byEntity.World.GetBlock(new AssetLocation("loosegears-1"));
				blockPos.Y--;
				if (block.IsReplacableBy(block3) && byEntity.World.BlockAccessor.GetMostSolidBlock(blockPos).CanAttachBlockAt(byEntity.World.BlockAccessor, block3, blockPos, BlockFacing.UP))
				{
					blockPos.Y++;
					byEntity.World.BlockAccessor.SetBlock(block3.BlockId, blockPos);
					slot.TakeOut(1);
					byEntity.World.PlaySoundAt(block.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, player);
				}
			}
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
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
