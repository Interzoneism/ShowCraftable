using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorBlockEntityInteract : BlockBehavior
{
	public BlockBehaviorBlockEntityInteract(Block block)
		: base(block)
	{
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return getInteractable(world, blockSel.Position)?.OnBlockInteractStart(world, byPlayer, blockSel, ref handling) ?? false;
	}

	public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		if (getInteractable(world, blockSel.Position) is ILongInteractable longInteractable)
		{
			return longInteractable.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, ref handling);
		}
		return false;
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		if (getInteractable(world, blockSel.Position) is ILongInteractable longInteractable)
		{
			return longInteractable.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel, ref handling);
		}
		return false;
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		if (getInteractable(world, blockSel.Position) is ILongInteractable longInteractable)
		{
			longInteractable.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel, ref handling);
		}
	}

	private IInteractable getInteractable(IWorldAccessor world, BlockPos pos)
	{
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
		if (blockEntity is IInteractable result)
		{
			return result;
		}
		if (blockEntity == null)
		{
			return null;
		}
		foreach (BlockEntityBehavior behavior in blockEntity.Behaviors)
		{
			if (behavior is IInteractable)
			{
				return behavior as IInteractable;
			}
		}
		return null;
	}
}
