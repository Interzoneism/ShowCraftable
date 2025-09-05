using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockDaubWattle : Block
{
	private int daubUpgradeAmount;

	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		daubUpgradeAmount = Attributes["daubUpgradeAmount"].AsInt(2);
		AssetLocation itemCode = new AssetLocation("daubraw-" + Variant["color"]);
		Item item = api.World.GetItem(itemCode);
		if (api.Side == EnumAppSide.Client)
		{
			interactions = new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-clayform-adddaub",
					Itemstacks = new ItemStack[1]
					{
						new ItemStack(item, daubUpgradeAmount)
					},
					MouseButton = EnumMouseButton.Right
				}
			};
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (base.OnBlockInteractStart(world, byPlayer, blockSel))
		{
			return true;
		}
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!activeHotbarSlot.Empty)
		{
			ItemStack itemstack = activeHotbarSlot.Itemstack;
			if (itemstack != null && itemstack.Collectible?.Code?.Path.StartsWithFast("daubraw") == true)
			{
				string text = Variant["type"];
				string text2 = activeHotbarSlot.Itemstack.Collectible.Variant["color"];
				if (!string.Equals(text, "normal") && string.Equals(text2, Variant["color"]) && activeHotbarSlot.StackSize >= daubUpgradeAmount && world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
				{
					string text3 = ((text == "wattle") ? "-cracked" : ((!(text == "cracked")) ? "-wattle" : "-normal"));
					string text4 = text3;
					Block block = world.GetBlock(new AssetLocation("daub-" + text2 + text4));
					if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
					{
						activeHotbarSlot.TakeOut(daubUpgradeAmount);
					}
					world.BlockAccessor.SetBlock(block.Id, blockSel.Position);
					return true;
				}
			}
		}
		return false;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions;
	}
}
