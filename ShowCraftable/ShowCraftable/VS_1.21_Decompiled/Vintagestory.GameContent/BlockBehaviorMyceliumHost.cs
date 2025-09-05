using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorMyceliumHost : BlockBehavior
{
	public BlockBehaviorMyceliumHost(Block block)
		: base(block)
	{
	}

	public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		world.BlockAccessor.RemoveBlockEntity(pos);
	}
}
