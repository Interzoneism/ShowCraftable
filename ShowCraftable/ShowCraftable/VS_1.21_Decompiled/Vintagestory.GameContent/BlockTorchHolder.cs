using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockTorchHolder : Block
{
	public bool Empty => Variant["state"] == "empty";

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (Empty)
		{
			ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			if (itemstack != null && itemstack.Collectible.Code.Path.Equals("torch-basic-lit-up"))
			{
				byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
				byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
				Block block = world.GetBlock(CodeWithVariant("state", "filled"));
				world.BlockAccessor.ExchangeBlock(block.BlockId, blockSel.Position);
				if (Sounds?.Place != null)
				{
					world.PlaySoundAt(Sounds.Place, blockSel.Position, 0.1, byPlayer);
				}
				return true;
			}
		}
		else
		{
			ItemStack itemstack2 = new ItemStack(world.GetBlock(new AssetLocation("torch-basic-lit-up")));
			if (byPlayer.InventoryManager.TryGiveItemstack(itemstack2, slotNotifyEffect: true))
			{
				Block block2 = world.GetBlock(CodeWithVariant("state", "empty"));
				world.BlockAccessor.ExchangeBlock(block2.BlockId, blockSel.Position);
				if (Sounds?.Place != null)
				{
					world.PlaySoundAt(Sounds.Place, blockSel.Position, 0.1, byPlayer);
				}
				return true;
			}
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		if (Empty)
		{
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-torchholder-addtorch",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = new ItemStack[1]
					{
						new ItemStack(world.GetBlock(new AssetLocation("torch-basic-lit-up")))
					}
				}
			}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
		}
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-torchholder-removetorch",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = null
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
