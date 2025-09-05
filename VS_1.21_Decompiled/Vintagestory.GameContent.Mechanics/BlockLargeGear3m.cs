using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockLargeGear3m : BlockMPBase
{
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
	}

	public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		if (face == BlockFacing.UP || face == BlockFacing.DOWN)
		{
			return true;
		}
		if (world.BlockAccessor.GetBlockEntity(pos) is BELargeGear3m bELargeGear3m)
		{
			return bELargeGear3m.HasGearAt(world.Api, pos.AddCopy(face));
		}
		return false;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		List<BlockPos> list = new List<BlockPos>();
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode, list))
		{
			return false;
		}
		bool flag = base.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
		if (flag)
		{
			BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
			List<BlockFacing> list2 = new List<BlockFacing>();
			foreach (BlockPos item in list)
			{
				int num = item.X - blockSel.Position.X;
				int num2 = item.Z - blockSel.Position.Z;
				char c = 'n';
				switch (num)
				{
				case 1:
					c = 'e';
					break;
				case -1:
					c = 'w';
					break;
				default:
					if (num2 == 1)
					{
						c = 's';
					}
					break;
				}
				BlockMPBase obj = world.GetBlock(new AssetLocation("angledgears-" + c + c)) as BlockMPBase;
				BlockFacing blockFacing = BlockFacing.FromFirstLetter(c);
				obj.ExchangeBlockAt(world, item);
				obj.DidConnectAt(world, item, blockFacing.Opposite);
				list2.Add(blockFacing);
			}
			PlaceFakeBlocks(world, blockSel.Position, list);
			BEBehaviorMPBase bEBehaviorMPBase = blockEntity?.GetBehavior<BEBehaviorMPBase>();
			BlockPos pos = blockSel.Position.DownCopy();
			if (world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock mechanicalPowerBlock && mechanicalPowerBlock.HasMechPowerConnectorAt(world, pos, BlockFacing.UP))
			{
				mechanicalPowerBlock.DidConnectAt(world, pos, BlockFacing.UP);
				list2.Add(BlockFacing.DOWN);
			}
			else
			{
				pos = blockSel.Position.UpCopy();
				if (world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock mechanicalPowerBlock2 && mechanicalPowerBlock2.HasMechPowerConnectorAt(world, pos, BlockFacing.DOWN))
				{
					mechanicalPowerBlock2.DidConnectAt(world, pos, BlockFacing.DOWN);
					list2.Add(BlockFacing.UP);
				}
			}
			foreach (BlockFacing item2 in list2)
			{
				bEBehaviorMPBase?.WasPlaced(item2);
			}
		}
		return flag;
	}

	private void PlaceFakeBlocks(IWorldAccessor world, BlockPos pos, List<BlockPos> skips)
	{
		Block block = world.GetBlock(new AssetLocation("mpmultiblockwood"));
		BlockPos blockPos = new BlockPos();
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (i == 0 && j == 0)
				{
					continue;
				}
				bool flag = false;
				foreach (BlockPos skip in skips)
				{
					if (pos.X + i == skip.X && pos.Z + j == skip.Z)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					blockPos.Set(pos.X + i, pos.Y, pos.Z + j);
					world.BlockAccessor.SetBlock(block.BlockId, blockPos);
					if (world.BlockAccessor.GetBlockEntity(blockPos) is BEMPMultiblock bEMPMultiblock)
					{
						bEMPMultiblock.Principal = pos;
					}
				}
			}
		}
	}

	private bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode, List<BlockPos> smallGears)
	{
		if (!base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		BlockPos position = blockSel.Position;
		BlockPos blockPos = new BlockPos();
		BlockSelection blockSelection = blockSel.Clone();
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (i == 0 && j == 0)
				{
					continue;
				}
				blockPos.Set(position.X + i, position.Y, position.Z + j);
				if ((i == 0 || j == 0) && world.BlockAccessor.GetBlock(blockPos) is BlockAngledGears)
				{
					smallGears.Add(blockPos.Copy());
					continue;
				}
				blockSelection.Position = blockPos;
				if (!base.CanPlaceBlock(world, byPlayer, blockSelection, ref failureCode))
				{
					return false;
				}
			}
		}
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (blockSel.SelectionBoxIndex == 0)
		{
			blockSel.Face = BlockFacing.UP;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
	{
		base.OnBlockRemoved(world, pos);
		BlockPos blockPos = new BlockPos();
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (i != 0 || j != 0)
				{
					blockPos.Set(pos.X + i, pos.Y, pos.Z + j);
					if (world.BlockAccessor.GetBlockEntity(blockPos) is BEMPMultiblock bEMPMultiblock && pos.Equals(bEMPMultiblock.Principal))
					{
						bEMPMultiblock.Principal = null;
						world.BlockAccessor.SetBlock(0, blockPos);
					}
					else if (world.BlockAccessor.GetBlock(blockPos) is BlockAngledGears blockAngledGears)
					{
						blockAngledGears.ToPegGear(world, blockPos);
					}
				}
			}
		}
	}
}
