using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemDryGrass : Item
{
	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null || byEntity?.World == null || !byEntity.Controls.ShiftKey)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		IWorldAccessor world = byEntity.World;
		Block block = world.GetBlock(new AssetLocation("firepit-construct1"));
		if (block == null)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position : blockSel.Position.AddCopy(blockSel.Face));
		IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer)?.PlayerUID);
		if (!byEntity.World.Claims.TryAccess(player, blockPos, EnumBlockAccessFlags.BuildOrBreak))
		{
			return;
		}
		Block block2 = world.BlockAccessor.GetBlock(blockPos.DownCopy());
		Block block3 = world.BlockAccessor.GetBlock(blockSel.Position);
		if (block3 is BlockGroundStorage)
		{
			BlockEntityGroundStorage blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(blockSel.Position);
			if (blockEntity.Inventory[3].Empty && blockEntity.Inventory[2].Empty && blockEntity.Inventory[1].Empty && blockEntity.Inventory[0].Itemstack.Collectible is ItemFirewood)
			{
				if (blockEntity.Inventory[0].StackSize == blockEntity.Capacity)
				{
					string failureCode = "";
					if (!block.CanPlaceBlock(world, player, new BlockSelection
					{
						Position = blockPos,
						Face = BlockFacing.UP
					}, ref failureCode))
					{
						return;
					}
					world.BlockAccessor.SetBlock(block.BlockId, blockPos);
					if (block.Sounds != null)
					{
						world.PlaySoundAt(block.Sounds.Place, blockSel.Position.X, blockSel.Position.InternalY, blockSel.Position.Z, player);
					}
					itemslot.Itemstack.StackSize--;
				}
				handHandling = EnumHandHandling.PreventDefault;
			}
			else if (!(block3 is BlockPitkiln) && (world.GetBlock(new AssetLocation("pitkiln")) as BlockPitkiln).TryCreateKiln(world, player, blockSel.Position))
			{
				handHandling = EnumHandHandling.PreventDefault;
			}
			return;
		}
		string failureCode2 = "";
		if (block2.CanAttachBlockAt(byEntity.World.BlockAccessor, block, blockPos.DownCopy(), BlockFacing.UP) && block.CanPlaceBlock(world, player, new BlockSelection
		{
			Position = blockPos,
			Face = BlockFacing.UP
		}, ref failureCode2))
		{
			world.BlockAccessor.SetBlock(block.BlockId, blockPos);
			if (block.Sounds != null)
			{
				world.PlaySoundAt(block.Sounds.Place, blockSel.Position.X, blockSel.Position.InternalY, blockSel.Position.Z, player);
			}
			itemslot.Itemstack.StackSize--;
			handHandling = EnumHandHandling.PreventDefaultAction;
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				HotKeyCode = "shift",
				ActionLangCode = "heldhelp-createfirepit",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
