using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
[AddDocumentationProperty("SnowballableDecrementedBlockCode", "When the block is harvested for snowballs, what block should it be replaced with?", "System.String", "Optional", "None", true)]
public class BlockBehaviorSnowballable : BlockBehavior
{
	public BlockBehaviorSnowballable(Block block)
		: base(block)
	{
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		if (canPickSnowballFrom(block, blockSel.Position, byPlayer))
		{
			ItemStack itemstack = new ItemStack(world.GetItem(new AssetLocation("snowball-snow")), 2);
			if (!byPlayer.InventoryManager.TryGiveItemstack(itemstack))
			{
				world.SpawnItemEntity(itemstack, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
			}
			Block decrementedSnowLayerBlock = getDecrementedSnowLayerBlock(world, block);
			world.BlockAccessor.SetBlock(decrementedSnowLayerBlock.Id, blockSel.Position);
			world.PlaySoundAt(new AssetLocation("sounds/block/snow"), byPlayer, byPlayer);
			handling = EnumHandling.PreventDefault;
			return true;
		}
		handling = EnumHandling.PassThrough;
		return false;
	}

	private Block getDecrementedSnowLayerBlock(IWorldAccessor world, Block block)
	{
		if (block.Attributes != null && block.Attributes.KeyExists("snowballableDecrementedBlockCode"))
		{
			return world.GetBlock(AssetLocation.Create(block.Attributes["snowballableDecrementedBlockCode"].AsString(), block.Code.Domain));
		}
		if (block.snowLevel > 3f)
		{
			return world.GetBlock(block.CodeWithVariant("height", (block.snowLevel - 1f).ToString() ?? ""));
		}
		if (block != block.snowCovered3)
		{
			if (block != block.snowCovered2)
			{
				return world.Blocks[0];
			}
			return block.snowCovered1;
		}
		return block.snowCovered2;
	}

	public static bool canPickSnowballFrom(Block block, BlockPos pos, IPlayer byPlayer)
	{
		ItemSlot rightHandItemSlot = byPlayer.Entity.RightHandItemSlot;
		if ((block.snowCovered2 != null || (block.Attributes != null && block.Attributes.KeyExists("snowballableDecrementedBlockCode")) || byPlayer.Entity.World.BlockAccessor.GetBlock(pos.DownCopy()).BlockMaterial == EnumBlockMaterial.Snow) && (block.snowLevel != 0f || byPlayer.Entity.World.BlockAccessor.GetBlock(pos.UpCopy()).BlockMaterial != EnumBlockMaterial.Snow) && byPlayer.Entity.Controls.ShiftKey)
		{
			if (!rightHandItemSlot.Empty)
			{
				return rightHandItemSlot.Itemstack.Collectible is ItemSnowball;
			}
			return true;
		}
		return false;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
	{
		handling = EnumHandling.Handled;
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-snow-takesnowball",
				HotKeyCode = "shift",
				MouseButton = EnumMouseButton.Right,
				RequireFreeHand = true,
				ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => block.snowLevel != 0f || world.BlockAccessor.GetBlock(bs.Position.UpCopy()).BlockMaterial != EnumBlockMaterial.Snow
			}
		};
	}
}
