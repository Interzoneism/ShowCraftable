using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockSticksLayer : Block
{
	public BlockFacing Orientation { get; set; }

	static BlockSticksLayer()
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Orientation = BlockFacing.FromFirstLetter(Variant["facing"][0]);
	}

	protected AssetLocation OrientedAsset(string orientation)
	{
		return CodeWithVariants(new string[2] { "type", "facing" }, new string[2] { "wooden", orientation });
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			BlockFacing blockFacing = OrientForPlacement(world.BlockAccessor, byPlayer, blockSel);
			string orientation = ((blockFacing == BlockFacing.NORTH || blockFacing == BlockFacing.SOUTH) ? "ns" : "ew");
			AssetLocation code = OrientedAsset(orientation);
			world.BlockAccessor.SetBlock(world.BlockAccessor.GetBlock(code).BlockId, blockSel.Position);
			return true;
		}
		return false;
	}

	protected virtual BlockFacing OrientForPlacement(IBlockAccessor world, IPlayer player, BlockSelection bs)
	{
		BlockFacing[] array = Block.SuggestedHVOrientation(player, bs);
		BlockFacing blockFacing = ((array.Length != 0) ? array[0] : null);
		if (blockFacing != null && player.Entity.Controls.ShiftKey)
		{
			return blockFacing;
		}
		BlockPos position = bs.Position;
		Block block = world.GetBlock(position.WestCopy());
		Block block2 = world.GetBlock(position.EastCopy());
		Block block3 = world.GetBlock(position.NorthCopy());
		Block block4 = world.GetBlock(position.SouthCopy());
		int num = ((block is BlockSticksLayer blockSticksLayer && blockSticksLayer.Orientation == BlockFacing.EAST) ? 1 : 0);
		int num2 = ((block2 is BlockSticksLayer blockSticksLayer2 && blockSticksLayer2.Orientation == BlockFacing.EAST) ? 1 : 0);
		int num3 = ((block3 is BlockSticksLayer blockSticksLayer3 && blockSticksLayer3.Orientation == BlockFacing.NORTH) ? 1 : 0);
		int num4 = ((block4 is BlockSticksLayer blockSticksLayer4 && blockSticksLayer4.Orientation == BlockFacing.NORTH) ? 1 : 0);
		if (num + num2 - num3 - num4 > 0)
		{
			return BlockFacing.EAST;
		}
		if (num3 + num4 - num - num2 > 0)
		{
			return BlockFacing.NORTH;
		}
		BlockPos blockPos = position.DownCopy();
		if (!CanSupportThis(world, blockPos, null))
		{
			int num5 = (CanSupportThis(world, blockPos.WestCopy(), BlockFacing.EAST) ? 1 : 0);
			int num6 = (CanSupportThis(world, blockPos.EastCopy(), BlockFacing.WEST) ? 1 : 0);
			int num7 = (CanSupportThis(world, blockPos.NorthCopy(), BlockFacing.SOUTH) ? 1 : 0);
			int num8 = (CanSupportThis(world, blockPos.SouthCopy(), BlockFacing.NORTH) ? 1 : 0);
			if (num5 + num6 == 2 && num7 + num8 < 2)
			{
				return BlockFacing.EAST;
			}
			if (num5 + num6 < 2 && num7 + num8 == 2)
			{
				return BlockFacing.NORTH;
			}
		}
		return blockFacing;
	}

	public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection bs, ref string failureCode)
	{
		BlockPos position = bs.Position;
		BlockPos blockPos = position.DownCopy();
		IBlockAccessor blockAccessor = world.BlockAccessor;
		if (!CanSupportThis(blockAccessor, blockPos, null))
		{
			bool flag = blockAccessor.GetBlock(position.WestCopy()) is BlockSticksLayer;
			if (!flag)
			{
				flag = blockAccessor.GetBlock(position.EastCopy()) is BlockSticksLayer;
			}
			if (!flag)
			{
				flag = blockAccessor.GetBlock(position.NorthCopy()) is BlockSticksLayer;
			}
			if (!flag)
			{
				flag = blockAccessor.GetBlock(position.SouthCopy()) is BlockSticksLayer;
			}
			if (!flag)
			{
				flag = CanSupportThis(blockAccessor, blockPos.WestCopy(), BlockFacing.EAST);
			}
			if (!flag)
			{
				flag = CanSupportThis(blockAccessor, blockPos.EastCopy(), BlockFacing.WEST);
			}
			if (!flag)
			{
				flag = CanSupportThis(blockAccessor, blockPos.NorthCopy(), BlockFacing.SOUTH);
			}
			if (!flag)
			{
				flag = CanSupportThis(blockAccessor, blockPos.SouthCopy(), BlockFacing.NORTH);
			}
			if (!flag)
			{
				failureCode = "requiresolidground";
				return false;
			}
		}
		return base.CanPlaceBlock(world, byPlayer, bs, ref failureCode);
	}

	private bool CanSupportThis(IBlockAccessor blockAccess, BlockPos pos, BlockFacing sideToTest)
	{
		Block block = blockAccess.GetBlock(pos);
		if (block.SideSolid[BlockFacing.UP.Index])
		{
			return true;
		}
		if (sideToTest == null && block.FirstCodePart() == "roughhewnfence")
		{
			return true;
		}
		Cuboidf[] collisionBoxes = block.CollisionBoxes;
		if (collisionBoxes != null)
		{
			for (int i = 0; i < collisionBoxes.Length; i++)
			{
				if (collisionBoxes[i].Y2 == 1f)
				{
					if (sideToTest == null)
					{
						return true;
					}
					if ((sideToTest != BlockFacing.WEST || collisionBoxes[i].X1 == 0f) && (sideToTest != BlockFacing.EAST || collisionBoxes[i].X2 == 1f) && (sideToTest != BlockFacing.NORTH || collisionBoxes[i].Z1 == 0f) && (sideToTest != BlockFacing.SOUTH || collisionBoxes[i].Z2 == 1f))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(OrientedAsset("ew")));
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		return OrientedAsset((Orientation == BlockFacing.NORTH) ? "ew" : "ns");
	}
}
