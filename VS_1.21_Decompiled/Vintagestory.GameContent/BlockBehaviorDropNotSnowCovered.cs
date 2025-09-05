using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorDropNotSnowCovered : BlockBehavior
{
	public BlockBehaviorDropNotSnowCovered(Block block)
		: base(block)
	{
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
	{
		if (block.Variant["cover"] == "snow")
		{
			handling = EnumHandling.PreventDefault;
			return new ItemStack[1]
			{
				new ItemStack(world.GetBlock(block.CodeWithVariant("cover", "free")))
			};
		}
		return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
	{
		if (base.block.Variant["cover"] == "snow")
		{
			Block block = world.GetBlock(new AssetLocation("snowblock"));
			if (block != null)
			{
				world.SpawnCubeParticles(pos.ToVec3d().Add(0.5, 0.5, 0.5), new ItemStack(block), 1f, 20, 1f, byPlayer);
			}
		}
	}
}
