using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorPillar : BlockBehavior
{
	[DocumentAsJson("Optional", "False", false)]
	private bool invertedPlacement;

	public BlockBehaviorPillar(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		invertedPlacement = properties["invertedPlacement"].AsBool();
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		string component = null;
		switch (blockSel.Face.Axis)
		{
		case EnumAxis.X:
			component = "we";
			break;
		case EnumAxis.Y:
			component = "ud";
			break;
		case EnumAxis.Z:
			component = "ns";
			break;
		}
		if (invertedPlacement)
		{
			BlockFacing[] array = Block.SuggestedHVOrientation(byPlayer, blockSel);
			component = ((!blockSel.Face.IsVertical) ? "ud" : ((array[0].Axis == EnumAxis.X) ? "we" : "ns"));
		}
		Block block = world.BlockAccessor.GetBlock(base.block.CodeWithParts(component));
		if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			block.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			return true;
		}
		return false;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		return new ItemStack(world.GetBlock(block.CodeWithParts("ud")));
	}

	public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (block.LastCodePart() == "ud")
		{
			return block.Code;
		}
		string[] array = new string[2] { "ns", "we" };
		if (angle < 0)
		{
			angle += 360;
		}
		int num = angle / 90;
		if (block.LastCodePart() == "we")
		{
			num++;
		}
		return block.CodeWithParts(array[num % 2]);
	}
}
