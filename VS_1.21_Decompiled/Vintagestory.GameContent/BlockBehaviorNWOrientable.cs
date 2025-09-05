using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorNWOrientable : BlockBehavior
{
	private string variantCode = "orientation";

	public BlockBehaviorNWOrientable(Block block)
		: base(block)
	{
		if (!block.Variant.ContainsKey("orientation"))
		{
			variantCode = "side";
		}
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing[] array = Block.SuggestedHVOrientation(byPlayer, blockSel);
		string value = "ns";
		if (array[0].Index == 1 || array[0].Index == 3)
		{
			value = "we";
		}
		Block block = world.BlockAccessor.GetBlock(base.block.CodeWithVariant(variantCode, value));
		if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			block.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			return true;
		}
		return false;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		return new ItemStack[1]
		{
			new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithVariant(variantCode, "ns")))
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		return new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithVariant(variantCode, "ns")));
	}

	public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		string[] array = new string[2] { "ns", "we" };
		int num = GameMath.Mod(angle / 90, 4);
		if (block.Variant[variantCode] == "we")
		{
			num++;
		}
		return block.CodeWithVariant(variantCode, array[num % 2]);
	}
}
