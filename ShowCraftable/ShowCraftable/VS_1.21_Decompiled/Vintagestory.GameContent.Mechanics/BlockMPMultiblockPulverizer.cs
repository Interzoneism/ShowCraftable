using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockMPMultiblockPulverizer : Block
{
	public override bool IsReplacableBy(Block block)
	{
		return base.IsReplacableBy(block);
	}

	public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		IWorldAccessor worldAccessor = player?.Entity?.World;
		if (worldAccessor == null)
		{
			worldAccessor = api.World;
		}
		if (!(worldAccessor.BlockAccessor.GetBlockEntity(blockSel.Position) is BEMPMultiblock bEMPMultiblock) || bEMPMultiblock.Principal == null)
		{
			return 1f;
		}
		Block block = worldAccessor.BlockAccessor.GetBlock(bEMPMultiblock.Principal);
		_ = api.Side;
		_ = 2;
		BlockSelection blockSelection = blockSel.Clone();
		blockSelection.Position = bEMPMultiblock.Principal;
		return block.OnGettingBroken(player, blockSelection, itemslot, remainingResistance, dt, counter);
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(world, blockPos, byItemStack);
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BEMPMultiblock bEMPMultiblock) || bEMPMultiblock.Principal == null)
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
			return;
		}
		BlockPos principal = bEMPMultiblock.Principal;
		world.BlockAccessor.GetBlock(principal).OnBlockBroken(world, principal, byPlayer, dropQuantityMultiplier);
		if (api.Side == EnumAppSide.Client)
		{
			BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
			foreach (BlockFacing facing in hORIZONTALS)
			{
				BlockPos pos2 = principal.AddCopy(facing);
				world.BlockAccessor.GetBlock(pos2).OnNeighbourBlockChange(world, pos2, principal);
			}
		}
	}

	public override Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing)
	{
		if (!(blockAccess.GetBlockEntity(pos) is BEMPMultiblock bEMPMultiblock) || bEMPMultiblock.Principal == null)
		{
			return base.GetParticleBreakBox(blockAccess, pos, facing);
		}
		return blockAccess.GetBlock(bEMPMultiblock.Principal).GetParticleBreakBox(blockAccess, bEMPMultiblock.Principal, facing);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		IBlockAccessor blockAccessor = capi.World.BlockAccessor;
		if (!(blockAccessor.GetBlockEntity(pos) is BEMPMultiblock bEMPMultiblock) || bEMPMultiblock.Principal == null)
		{
			return 0;
		}
		return blockAccessor.GetBlock(bEMPMultiblock.Principal).GetRandomColor(capi, bEMPMultiblock.Principal, facing, rndIndex);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEMPMultiblock bEMPMultiblock && world.BlockAccessor.GetBlockEntity(bEMPMultiblock.Principal) is BEPulverizer bEPulverizer)
		{
			return bEPulverizer.OnInteract(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		IBlockAccessor blockAccessor = world.BlockAccessor;
		if (!(blockAccessor.GetBlockEntity(pos) is BEMPMultiblock bEMPMultiblock) || bEMPMultiblock.Principal == null)
		{
			return new ItemStack(world.GetBlock(new AssetLocation("pulverizerframe")));
		}
		return blockAccessor.GetBlock(bEMPMultiblock.Principal).OnPickBlock(world, bEMPMultiblock.Principal);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(base.OnPickBlock(world, pos)?.GetName());
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			blockBehaviors[i].GetPlacedBlockName(stringBuilder, world, pos);
		}
		return stringBuilder.ToString().TrimEnd();
	}
}
