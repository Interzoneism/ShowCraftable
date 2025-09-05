using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCokeOvenDoor : Block
{
	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockPos position = blockSel.Position;
		if (Variant["state"] == "closed")
		{
			world.BlockAccessor.ExchangeBlock(world.GetBlock(CodeWithVariant("state", "opened")).Id, position);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(position);
			}
			world.PlaySoundAt(new AssetLocation("sounds/block/cokeovendoor-open"), position, 0.0, byPlayer);
		}
		else
		{
			world.BlockAccessor.ExchangeBlock(world.GetBlock(CodeWithVariant("state", "closed")).Id, position);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(position);
			}
			world.PlaySoundAt(new AssetLocation("sounds/block/cokeovendoor-close"), position, 0.0, byPlayer);
		}
		return true;
	}
}
