using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorChimney : BlockBehavior
{
	public BlockBehaviorChimney(Block block)
		: base(block)
	{
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		BlockPos blockPos = new BlockPos();
		for (int i = 1; i <= 8; i++)
		{
			blockPos.Set(pos.X, pos.Y - i, pos.Z);
			Block block = world.BlockAccessor.GetBlock(blockPos);
			if (block.Id != 0)
			{
				if (block.SideIsSolid(blockPos, BlockFacing.UP.Index))
				{
					return false;
				}
				ISmokeEmitter smokeEmitter = block.GetInterface<ISmokeEmitter>(world, pos);
				if (smokeEmitter != null)
				{
					return smokeEmitter.EmitsSmoke(blockPos);
				}
			}
		}
		return false;
	}
}
