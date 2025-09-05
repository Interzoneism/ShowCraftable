using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockMPMultiblockGear : Block
{
	public override bool IsReplacableBy(Block block)
	{
		if (block is BlockAngledGears)
		{
			return true;
		}
		return base.IsReplacableBy(block);
	}

	public bool IsReplacableByGear(IWorldAccessor world, BlockPos pos)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BEMPMultiblock bEMPMultiblock) || bEMPMultiblock.Principal == null)
		{
			return true;
		}
		if (world.BlockAccessor.GetBlockEntity(bEMPMultiblock.Principal) is IGearAcceptor gearAcceptor)
		{
			return gearAcceptor.CanAcceptGear(pos);
		}
		return true;
	}

	public BlockEntity GearPlaced(IWorldAccessor world, BlockPos pos)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BEMPMultiblock bEMPMultiblock) || bEMPMultiblock.Principal == null)
		{
			return null;
		}
		IGearAcceptor obj = world.BlockAccessor.GetBlockEntity(bEMPMultiblock.Principal) as IGearAcceptor;
		if (obj == null)
		{
			world.Logger.Notification("no gear acceptor");
		}
		obj?.AddGear(pos);
		return obj as BlockEntity;
	}

	public static void OnGearDestroyed(IWorldAccessor world, BlockPos pos, char orient)
	{
		BlockPos blockPos = orient switch
		{
			's' => pos.NorthCopy(), 
			'w' => pos.EastCopy(), 
			'e' => pos.WestCopy(), 
			_ => pos.SouthCopy(), 
		};
		if (world.BlockAccessor.GetBlockEntity(blockPos) is IGearAcceptor gearAcceptor)
		{
			gearAcceptor.RemoveGearAt(pos);
			Block block = world.GetBlock(new AssetLocation("mpmultiblockwood"));
			world.BlockAccessor.SetBlock(block.BlockId, pos);
			if (world.BlockAccessor.GetBlockEntity(pos) is BEMPMultiblock bEMPMultiblock)
			{
				bEMPMultiblock.Principal = blockPos;
			}
		}
		else
		{
			world.Logger.Notification("no LG found at " + blockPos?.ToString() + " from " + pos);
		}
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
		Block block = world.BlockAccessor.GetBlock(principal);
		if (block.Id != 0)
		{
			block.OnBlockBroken(world, principal, byPlayer, dropQuantityMultiplier);
		}
		else
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		}
		if (api.Side == EnumAppSide.Client)
		{
			BlockFacing[] vERTICALS = BlockFacing.VERTICALS;
			foreach (BlockFacing facing in vERTICALS)
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

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		IBlockAccessor blockAccessor = world.BlockAccessor;
		if (!(blockAccessor.GetBlockEntity(pos) is BEMPMultiblock bEMPMultiblock) || bEMPMultiblock.Principal == null)
		{
			return new ItemStack(world.GetBlock(new AssetLocation("largegear3")));
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
