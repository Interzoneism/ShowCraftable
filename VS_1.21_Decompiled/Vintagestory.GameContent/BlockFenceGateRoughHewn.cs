using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockFenceGateRoughHewn : BlockBaseDoor
{
	public override string GetKnobOrientation()
	{
		return "left";
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		CanStep = false;
	}

	public override BlockFacing GetDirection()
	{
		return BlockFacing.FromFirstLetter(Variant["type"]);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			BlockFacing[] array = Block.SuggestedHVOrientation(byPlayer, blockSel);
			string text = ((array[0] == BlockFacing.NORTH || array[0] == BlockFacing.SOUTH) ? "n" : "w");
			AssetLocation code = CodeWithVariants(new string[2] { "type", "state" }, new string[2] { text, "closed" });
			world.BlockAccessor.SetBlock(world.BlockAccessor.GetBlock(code).BlockId, blockSel.Position);
			return true;
		}
		return false;
	}

	protected override void Open(IWorldAccessor world, IPlayer byPlayer, BlockPos pos)
	{
		AssetLocation code = CodeWithVariant("state", IsOpened() ? "closed" : "opened");
		world.BlockAccessor.ExchangeBlock(world.BlockAccessor.GetBlock(code).BlockId, pos);
	}

	protected override BlockPos TryGetConnectedDoorPos(BlockPos pos)
	{
		BlockFacing direction = GetDirection();
		return pos.AddCopy(direction.GetCW());
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[3] { "type", "state", "cover" }, new string[3] { "n", "closed", "free" }));
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariants(new string[3] { "type", "state", "cover" }, new string[3] { "n", "closed", "free" })));
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		BlockFacing blockFacing = BlockFacing.FromFirstLetter(Variant["type"]);
		BlockFacing blockFacing2 = BlockFacing.HORIZONTALS_ANGLEORDER[(blockFacing.HorizontalAngleIndex + angle / 90) % 4];
		string text = Variant["type"];
		if (blockFacing.Axis != blockFacing2.Axis)
		{
			text = ((text == "n") ? "w" : "n");
		}
		return CodeWithVariant("type", text);
	}

	public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs)
	{
		if (activationArgs == null || !activationArgs.HasAttribute("opened") || activationArgs.GetBool("opened") != IsOpened())
		{
			Open(world, caller.Player, blockSel.Position);
		}
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
	}
}
