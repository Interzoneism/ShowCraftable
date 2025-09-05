using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockFirewoodPile : Block, IBlockItemPile
{
	private Cuboidf[][] CollisionBoxesByFillLevel;

	public BlockFirewoodPile()
	{
		CollisionBoxesByFillLevel = new Cuboidf[5][];
		for (int i = 0; i <= 4; i++)
		{
			CollisionBoxesByFillLevel[i] = new Cuboidf[1]
			{
				new Cuboidf(0f, 0f, 0f, 1f, (float)i * 0.25f, 1f)
			};
		}
	}

	public int FillLevel(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockEntity blockEntity = blockAccessor.GetBlockEntity(pos);
		if (blockEntity is BlockEntityFirewoodPile)
		{
			return (int)Math.Ceiling((double)((BlockEntityFirewoodPile)blockEntity).OwnStackSize / 8.0);
		}
		return 1;
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return CollisionBoxesByFillLevel[FillLevel(blockAccessor, pos)];
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return CollisionBoxesByFillLevel[FillLevel(blockAccessor, pos)];
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return Array.Empty<BlockDropItemStack>();
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return Array.Empty<ItemStack>();
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
		if (blockEntity is BlockEntityFirewoodPile)
		{
			return ((BlockEntityFirewoodPile)blockEntity).OnPlayerInteract(byPlayer);
		}
		return false;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityFirewoodPile blockEntityFirewoodPile)
		{
			return blockEntityFirewoodPile.inventory.FirstNonEmptySlot?.Itemstack.Clone();
		}
		return null;
	}

	public bool Construct(ItemSlot slot, IWorldAccessor world, BlockPos pos, IPlayer player)
	{
		if (!world.BlockAccessor.GetBlock(pos).IsReplacableBy(this))
		{
			return false;
		}
		Block block = world.BlockAccessor.GetBlock(pos.DownCopy());
		if (!block.CanAttachBlockAt(world.BlockAccessor, this, pos.DownCopy(), BlockFacing.UP) && (block != this || FillLevel(world.BlockAccessor, pos.DownCopy()) != 4))
		{
			return false;
		}
		world.BlockAccessor.SetBlock(BlockId, pos);
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
		if (blockEntity is BlockEntityFirewoodPile)
		{
			BlockEntityFirewoodPile blockEntityFirewoodPile = (BlockEntityFirewoodPile)blockEntity;
			if (player == null || player.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				blockEntityFirewoodPile.inventory[0].Itemstack = slot.TakeOut(player.Entity.Controls.CtrlKey ? blockEntityFirewoodPile.BulkTakeQuantity : blockEntityFirewoodPile.DefaultTakeQuantity);
				slot.MarkDirty();
			}
			else
			{
				blockEntityFirewoodPile.inventory[0].Itemstack = slot.Itemstack.Clone();
				blockEntityFirewoodPile.inventory[0].Itemstack.StackSize = Math.Min(blockEntityFirewoodPile.inventory[0].Itemstack.StackSize, blockEntityFirewoodPile.MaxStackSize);
			}
			blockEntityFirewoodPile.MarkDirty();
			world.BlockAccessor.MarkBlockDirty(pos);
			world.PlaySoundAt(blockEntityFirewoodPile.soundLocation, pos, (double)(blockEntityFirewoodPile.inventory[0].Itemstack.StackSize / blockEntityFirewoodPile.MaxStackSize) - 0.5, player);
		}
		return true;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		Block block = world.BlockAccessor.GetBlock(pos.DownCopy());
		if (!block.CanAttachBlockAt(world.BlockAccessor, this, pos.DownCopy(), BlockFacing.UP) && (block != this || FillLevel(world.BlockAccessor, pos.DownCopy()) < 4))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityFirewoodPile blockEntityFirewoodPile)
		{
			return blockEntityFirewoodPile.OwnStackSize == blockEntityFirewoodPile.MaxStackSize;
		}
		return base.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[4]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-firewoodpile-addlog",
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = "shift",
				Itemstacks = new ItemStack[1]
				{
					new ItemStack(world.GetItem(new AssetLocation("firewood")), 2)
				}
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-firewoodpile-removelog",
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = null
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-firewoodpile-8addlog",
				MouseButton = EnumMouseButton.Right,
				HotKeyCodes = new string[2] { "ctrl", "shift" },
				Itemstacks = new ItemStack[1]
				{
					new ItemStack(world.GetItem(new AssetLocation("firewood")), 8)
				}
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-firewoodpile-8removelog",
				HotKeyCode = "ctrl",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
