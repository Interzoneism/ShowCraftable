using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockGlassPane : BlockRainAmbient
{
	public BlockFacing Orientation { get; set; }

	public string Frame { get; set; }

	public string GlassType { get; set; }

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Orientation = BlockFacing.FromFirstLetter(Variant["type"].Substring(0, 1));
		string text = Variant["wood"];
		Frame = ((text != null) ? string.Intern(text) : null);
		string text2 = Variant["glass"];
		GlassType = ((text2 != null) ? string.Intern(text2) : null);
	}

	protected AssetLocation OrientedAsset(string orientation)
	{
		return CodeWithVariants(new string[3] { "glass", "wood", "type" }, new string[3] { GlassType, Frame, orientation });
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
		BlockFacing result = ((array.Length != 0) ? array[0] : null);
		BlockPos position = bs.Position;
		Block block = world.GetBlock(position.UpCopy());
		Block block2 = world.GetBlock(position.DownCopy());
		int num = ((block is BlockGlassPane blockGlassPane) ? ((blockGlassPane.Orientation == BlockFacing.EAST) ? 1 : (-1)) : 0);
		int num2 = ((block2 is BlockGlassPane blockGlassPane2) ? ((blockGlassPane2.Orientation == BlockFacing.EAST) ? 1 : (-1)) : 0);
		int num3 = num + num2;
		if (num3 > 0)
		{
			return BlockFacing.EAST;
		}
		if (num3 < 0)
		{
			return BlockFacing.NORTH;
		}
		Block block3 = world.GetBlock(position.WestCopy());
		Block block4 = world.GetBlock(position.EastCopy());
		Block block5 = world.GetBlock(position.NorthCopy());
		Block block6 = world.GetBlock(position.SouthCopy());
		int num4 = ((block3 is BlockGlassPane blockGlassPane3 && blockGlassPane3.Orientation == BlockFacing.NORTH) ? 1 : 0);
		int num5 = ((block4 is BlockGlassPane blockGlassPane4 && blockGlassPane4.Orientation == BlockFacing.NORTH) ? 1 : 0);
		int num6 = ((block5 is BlockGlassPane blockGlassPane5 && blockGlassPane5.Orientation == BlockFacing.EAST) ? 1 : 0);
		int num7 = ((block6 is BlockGlassPane blockGlassPane6 && blockGlassPane6.Orientation == BlockFacing.EAST) ? 1 : 0);
		if (num4 + num5 - num6 - num7 > 0)
		{
			return BlockFacing.NORTH;
		}
		if (num6 + num7 - num4 - num5 > 0)
		{
			return BlockFacing.EAST;
		}
		int num8 = block3.GetLightAbsorption(world, position.WestCopy()) + block4.GetLightAbsorption(world, position.EastCopy());
		int num9 = block5.GetLightAbsorption(world, position.NorthCopy()) + block6.GetLightAbsorption(world, position.SouthCopy());
		if (num8 < num9)
		{
			return BlockFacing.EAST;
		}
		if (num8 > num9)
		{
			return BlockFacing.NORTH;
		}
		return result;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(OrientedAsset("ew")));
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		BlockFacing blockFacing = BlockFacing.FromFirstLetter(Variant["type"][0].ToString() ?? "");
		BlockFacing blockFacing2 = BlockFacing.HORIZONTALS_ANGLEORDER[(blockFacing.HorizontalAngleIndex + angle / 90) % 4];
		string text = Variant["type"];
		if (blockFacing.Axis != blockFacing2.Axis)
		{
			text = ((text == "ns") ? "ew" : "ns");
		}
		return CodeWithVariant("type", text);
	}

	public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type)
	{
		string text = Variant["type"];
		if (text == "ns" && (facing == BlockFacing.NORTH || facing == BlockFacing.SOUTH))
		{
			return 1;
		}
		if (text == "ew" && (facing == BlockFacing.EAST || facing == BlockFacing.WEST))
		{
			return 1;
		}
		return 0;
	}
}
