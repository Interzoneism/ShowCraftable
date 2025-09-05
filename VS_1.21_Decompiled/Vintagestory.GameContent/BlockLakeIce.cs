using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockLakeIce : BlockForFluidsLayer
{
	private int waterBlock;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		waterBlock = api.World.GetBlock(new AssetLocation("water-still-7")).BlockId;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		bool flag = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			obj.OnBlockBroken(world, pos, byPlayer, ref handling);
			if (handling == EnumHandling.PreventDefault)
			{
				flag = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (flag)
		{
			return;
		}
		if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			ItemStack[] drops = GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
			if (drops != null)
			{
				for (int j = 0; j < drops.Length; j++)
				{
					if (SplitDropStacks)
					{
						for (int k = 0; k < drops[j].StackSize; k++)
						{
							ItemStack itemStack = drops[j].Clone();
							itemStack.StackSize = 1;
							world.SpawnItemEntity(itemStack, pos);
						}
					}
				}
			}
			world.PlaySoundAt(Sounds?.GetBreakSound(byPlayer), pos, -0.5, byPlayer);
		}
		SpawnBlockBrokenParticles(pos);
		world.BlockAccessor.SetBlock(waterBlock, pos, 2);
	}

	public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
	{
		extra = null;
		float num = GameMath.Clamp((world.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, api.World.Calendar.TotalDays).Temperature - 2f) / 20f, 0f, 1f);
		return offThreadRandom.NextDouble() < (double)num;
	}

	public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		world.BlockAccessor.SetBlock(waterBlock, pos, 2);
	}

	public override bool ShouldMergeFace(int facingIndex, Block neighbourIce, int intraChunkIndex3d)
	{
		return BlockMaterial == neighbourIce.BlockMaterial;
	}
}
