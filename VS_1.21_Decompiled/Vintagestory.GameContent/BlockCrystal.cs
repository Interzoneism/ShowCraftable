using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCrystal : Block
{
	private Block[] _facingBlocks;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		_facingBlocks = new Block[6];
		for (int i = 0; i < 6; i++)
		{
			_facingBlocks[i] = api.World.GetBlock(CodeWithPart(BlockFacing.ALLFACES[i].Code, 2));
		}
	}

	public Block FacingCrystal(IBlockAccessor blockAccessor, BlockFacing facing)
	{
		return blockAccessor.GetBlock(CodeWithPart(facing.Code));
	}

	public override double ExplosionDropChance(IWorldAccessor world, BlockPos pos, EnumBlastType blastType)
	{
		return 0.2;
	}

	public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, string ignitedByPlayerUid)
	{
		if (world.Rand.NextDouble() < 0.25)
		{
			ItemStack itemStack = new ItemStack(api.World.GetBlock(CodeWithVariant("position", "up")));
			itemStack.StackSize = 1;
			world.SpawnItemEntity(itemStack, pos);
		}
		else
		{
			int num = 3;
			if (Variant["variant"] == "cluster1" || Variant["variant"] == "cluster2")
			{
				num = 5;
			}
			if (Variant["variant"] == "large1" || Variant["variant"] == "large2")
			{
				num = 7;
			}
			int num2 = (int)((double)num * Math.Min(1.0, world.Rand.NextDouble() * 0.3100000023841858 + 0.699999988079071));
			string text = Variant["type"];
			string text2 = ((text == "milkyquartz") ? "clearquartz" : ((!(text == "olivine")) ? Variant["type"] : "ore-olivine"));
			string domainAndPath = text2;
			ItemStack itemStack2 = new ItemStack(api.World.GetItem(new AssetLocation(domainAndPath)));
			for (int i = 0; i < num2; i++)
			{
				ItemStack itemStack3 = itemStack2.Clone();
				itemStack3.StackSize = 1;
				world.SpawnItemEntity(itemStack3, pos);
			}
		}
		world.BulkBlockAccessor.SetBlock(0, pos);
	}

	public override double GetBlastResistance(IWorldAccessor world, BlockPos pos, Vec3f blastDirectionVector, EnumBlastType blastType)
	{
		return 0.5;
	}
}
