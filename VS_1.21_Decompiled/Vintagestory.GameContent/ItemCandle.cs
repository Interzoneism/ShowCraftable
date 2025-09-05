using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemCandle : Item
{
	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null || byEntity?.World == null || !byEntity.Controls.ShiftKey)
		{
			return;
		}
		IWorldAccessor world = byEntity.World;
		BlockPos blockPos = blockSel.Position.AddCopy(blockSel.Face);
		BlockPos pos = blockPos.DownCopy();
		Block block = world.BlockAccessor.GetBlock(blockSel.Position);
		AssetLocation assetLocation = new AssetLocation(Attributes["blockfirstcodepart"].AsString());
		string path = assetLocation.Path;
		IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer)?.PlayerUID);
		if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			slot.MarkDirty();
			return;
		}
		Block block2;
		if (block.FirstCodePart() == path)
		{
			int.TryParse(block.LastCodePart(), out var result);
			if (result == 9)
			{
				return;
			}
			block2 = world.GetBlock(block.CodeWithPart((result + 1).ToString() ?? "", 1));
			world.BlockAccessor.SetBlock(block2.BlockId, blockSel.Position);
		}
		else
		{
			if (world.BlockAccessor.GetBlock(pos) is BlockFence)
			{
				block2 = world.GetBlock(new AssetLocation("candle"));
			}
			else
			{
				block2 = byEntity.World.GetBlock(assetLocation.WithPathAppendix("-1"));
				if (block2 == null)
				{
					return;
				}
			}
			if (!world.BlockAccessor.GetBlock(blockPos).IsReplacableBy(block2) || !world.BlockAccessor.GetBlock(pos).CanAttachBlockAt(world.BlockAccessor, block2, pos, BlockFacing.UP, new Cuboidi(1, 14, 1, 14, 15, 14)))
			{
				return;
			}
			world.BlockAccessor.SetBlock(block2.BlockId, blockPos);
		}
		if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			slot.TakeOut(1);
		}
		slot.MarkDirty();
		if (block2.Sounds != null)
		{
			IPlayer dualCallByPlayer = null;
			if (byEntity is EntityPlayer)
			{
				dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			world.PlaySoundAt(block2.Sounds.Place, blockSel.Position, -0.4, dualCallByPlayer);
		}
		handHandling = EnumHandHandling.PreventDefault;
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
