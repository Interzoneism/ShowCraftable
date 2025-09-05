using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockAngledGears : BlockMPBase
{
	public string Orientation;

	public BlockFacing[] Facings
	{
		get
		{
			string orientation = Orientation;
			BlockFacing[] array = new BlockFacing[orientation.Length];
			for (int i = 0; i < orientation.Length; i++)
			{
				array[i] = BlockFacing.FromFirstLetter(orientation[i]);
			}
			if (array.Length == 2 && array[1] == array[0])
			{
				array[1] = array[1].Opposite;
			}
			return array;
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		Orientation = Variant["orientation"];
		base.OnLoaded(api);
	}

	public bool IsDeadEnd()
	{
		return Orientation.Length == 1;
	}

	public bool IsOrientedTo(BlockFacing facing)
	{
		string orientation = Orientation;
		if (orientation[0] == facing.Code[0])
		{
			return true;
		}
		if (orientation.Length == 1)
		{
			return false;
		}
		if (orientation[0] == orientation[1])
		{
			return orientation[0] == facing.Opposite.Code[0];
		}
		return orientation[1] == facing.Code[0];
	}

	public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		if (IsDeadEnd() && BlockFacing.FromFirstLetter(Orientation[0]).IsAdjacent(face))
		{
			return true;
		}
		return IsOrientedTo(face);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.GetBlock(new AssetLocation("angledgears-s")));
	}

	public Block getGearBlock(IWorldAccessor world, bool cageGear, BlockFacing facing, BlockFacing adjFacing = null)
	{
		char reference;
		char reference2;
		if (adjFacing == null)
		{
			char c = facing.Code[0];
			string text = FirstCodePart();
			string text2;
			if (!cageGear)
			{
				ReadOnlySpan<char> readOnlySpan = "-";
				reference = c;
				text2 = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference));
			}
			else
			{
				ReadOnlySpan<char> readOnlySpan2 = "-";
				reference = c;
				ReadOnlySpan<char> readOnlySpan3 = new ReadOnlySpan<char>(in reference);
				reference2 = c;
				text2 = string.Concat(readOnlySpan2, readOnlySpan3, new ReadOnlySpan<char>(in reference2));
			}
			return world.GetBlock(new AssetLocation(text + text2));
		}
		ReadOnlySpan<char> readOnlySpan4 = FirstCodePart();
		ReadOnlySpan<char> readOnlySpan5 = "-";
		reference2 = adjFacing.Code[0];
		ReadOnlySpan<char> readOnlySpan6 = new ReadOnlySpan<char>(in reference2);
		reference = facing.Code[0];
		AssetLocation blockCode = new AssetLocation(string.Concat(readOnlySpan4, readOnlySpan5, readOnlySpan6, new ReadOnlySpan<char>(in reference)));
		Block block = world.GetBlock(blockCode);
		if (block == null)
		{
			ReadOnlySpan<char> readOnlySpan7 = FirstCodePart();
			ReadOnlySpan<char> readOnlySpan8 = "-";
			reference = facing.Code[0];
			ReadOnlySpan<char> readOnlySpan9 = new ReadOnlySpan<char>(in reference);
			reference2 = adjFacing.Code[0];
			blockCode = new AssetLocation(string.Concat(readOnlySpan7, readOnlySpan8, readOnlySpan9, new ReadOnlySpan<char>(in reference2)));
			block = world.GetBlock(blockCode);
		}
		return block;
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		if (IsDeadEnd() && BlockFacing.FromFirstLetter(Orientation[0]).IsAdjacent(face))
		{
			(getGearBlock(world, cageGear: false, Facings[0], face) as BlockMPBase).ExchangeBlockAt(world, pos);
		}
	}

	public bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode, Block blockExisting)
	{
		if (blockExisting is BlockMPMultiblockGear blockMPMultiblockGear && !blockMPMultiblockGear.IsReplacableByGear(world, blockSel.Position))
		{
			failureCode = "notreplaceable";
			return false;
		}
		return base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		Block block = world.BlockAccessor.GetBlock(blockSel.Position);
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode, block))
		{
			return false;
		}
		BlockFacing blockFacing = null;
		BlockFacing blockFacing2 = null;
		BlockMPMultiblockGear blockMPMultiblockGear = block as BlockMPMultiblockGear;
		bool flag = false;
		if (blockMPMultiblockGear != null && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEMPMultiblock bEMPMultiblock)
		{
			flag = bEMPMultiblock.Principal != null;
		}
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing3 in aLLFACES)
		{
			if (flag && (blockFacing3 == BlockFacing.UP || blockFacing3 == BlockFacing.DOWN))
			{
				continue;
			}
			BlockPos pos = blockSel.Position.AddCopy(blockFacing3);
			if (world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock mechanicalPowerBlock && mechanicalPowerBlock.HasMechPowerConnectorAt(world, pos, blockFacing3.Opposite))
			{
				if (blockFacing == null)
				{
					blockFacing = blockFacing3;
				}
				else if (blockFacing3.IsAdjacent(blockFacing))
				{
					blockFacing2 = blockFacing3;
					break;
				}
			}
		}
		if (blockFacing != null)
		{
			BlockPos blockPos = blockSel.Position.AddCopy(blockFacing);
			BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(blockPos);
			IMechanicalPowerBlock mechanicalPowerBlock2 = blockEntity?.Block as IMechanicalPowerBlock;
			if (blockEntity?.GetBehavior<BEBehaviorMPAxle>() != null && !BEBehaviorMPAxle.IsAttachedToBlock(world.BlockAccessor, mechanicalPowerBlock2 as Block, blockPos))
			{
				failureCode = "axlemusthavesupport";
				return false;
			}
			BlockEntity blockEntity2 = (flag ? blockMPMultiblockGear.GearPlaced(world, blockSel.Position) : null);
			Block gearBlock = getGearBlock(world, flag, blockFacing, blockFacing2);
			world.BlockAccessor.SetBlock(gearBlock.BlockId, blockSel.Position);
			if (blockFacing2 != null)
			{
				BlockPos pos2 = blockSel.Position.AddCopy(blockFacing2);
				(world.BlockAccessor.GetBlock(pos2) as IMechanicalPowerBlock)?.DidConnectAt(world, pos2, blockFacing2.Opposite);
			}
			BEBehaviorMPAngledGears bEBehaviorMPAngledGears = world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorMPAngledGears>();
			if (bEBehaviorMPAngledGears == null)
			{
				return true;
			}
			if (blockEntity2?.GetBehavior<BEBehaviorMPBase>() is BEBehaviorMPLargeGear3m largeGear)
			{
				bEBehaviorMPAngledGears.AddToLargeGearNetwork(largeGear, blockFacing);
			}
			mechanicalPowerBlock2?.DidConnectAt(world, blockPos, blockFacing.Opposite);
			bEBehaviorMPAngledGears.newlyPlaced = true;
			if (!bEBehaviorMPAngledGears.tryConnect(blockFacing) && blockFacing2 != null)
			{
				bEBehaviorMPAngledGears.tryConnect(blockFacing2);
			}
			bEBehaviorMPAngledGears.newlyPlaced = false;
			return true;
		}
		failureCode = "requiresaxle";
		return false;
	}

	public override void WasPlaced(IWorldAccessor world, BlockPos ownPos, BlockFacing connectedOnFacing)
	{
		if (connectedOnFacing != null)
		{
			(world.BlockAccessor.GetBlockEntity(ownPos)?.GetBehavior<BEBehaviorMPAngledGears>())?.tryConnect(connectedOnFacing);
		}
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		string text = Orientation;
		if (text.Length == 2 && text[0] == text[1])
		{
			text = text[0].ToString() ?? "";
		}
		BlockFacing[] obj = ((text.Length != 1) ? new BlockFacing[2]
		{
			BlockFacing.FromFirstLetter(text[0]),
			BlockFacing.FromFirstLetter(text[1])
		} : new BlockFacing[1] { BlockFacing.FromFirstLetter(text[0]) });
		List<BlockFacing> list = new List<BlockFacing>();
		BlockFacing[] array = obj;
		foreach (BlockFacing blockFacing in array)
		{
			BlockPos pos2 = pos.AddCopy(blockFacing);
			if (world.BlockAccessor.GetBlock(pos2) is IMechanicalPowerBlock mechanicalPowerBlock && mechanicalPowerBlock.HasMechPowerConnectorAt(world, pos2, blockFacing.Opposite))
			{
				BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
				if (blockEntity == null || blockEntity.GetBehavior<BEBehaviorMPBase>()?.disconnected != true)
				{
					continue;
				}
			}
			list.Add(blockFacing);
		}
		if (list.Count == text.Length)
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
		else if (list.Count > 0)
		{
			text = text.Replace(list[0].Code[0].ToString() ?? "", "");
			(world.GetBlock(new AssetLocation(FirstCodePart() + "-" + text)) as BlockMPBase).ExchangeBlockAt(world, pos);
			world.BlockAccessor.GetBlockEntity(pos).GetBehavior<BEBehaviorMPBase>().LeaveNetwork();
			BlockFacing blockFacing2 = BlockFacing.FromFirstLetter(text[0]);
			BlockPos blockPos = pos.AddCopy(blockFacing2);
			BlockEntity blockEntity2 = world.BlockAccessor.GetBlockEntity(blockPos);
			IMechanicalPowerBlock mechanicalPowerBlock2 = blockEntity2?.Block as IMechanicalPowerBlock;
			if (blockEntity2?.GetBehavior<BEBehaviorMPAxle>() == null || BEBehaviorMPAxle.IsAttachedToBlock(world.BlockAccessor, mechanicalPowerBlock2 as Block, blockPos))
			{
				mechanicalPowerBlock2?.DidConnectAt(world, blockPos, blockFacing2.Opposite);
				WasPlaced(world, pos, blockFacing2);
			}
		}
	}

	public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
	{
		bool flag = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			obj.OnBlockRemoved(world, pos, ref handling);
			switch (handling)
			{
			case EnumHandling.PreventSubsequent:
				return;
			case EnumHandling.PreventDefault:
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			world.BlockAccessor.RemoveBlockEntity(pos);
			string text = Variant["orientation"];
			if (text.Length == 2 && text[1] == text[0])
			{
				BlockMPMultiblockGear.OnGearDestroyed(world, pos, text[0]);
			}
		}
	}

	internal void ToPegGear(IWorldAccessor world, BlockPos pos)
	{
		string text = Variant["orientation"];
		if (text.Length == 2 && text[1] == text[0])
		{
			((BlockMPBase)world.GetBlock(new AssetLocation(FirstCodePart() + "-" + text[0]))).ExchangeBlockAt(world, pos);
			(world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPAngledGears>())?.ClearLargeGear();
		}
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		string text = LastCodePart();
		string text2 = string.Empty;
		string text3;
		if (text.Length > 1)
		{
			text3 = text.Substring(0, 1);
			text2 = text.Substring(1);
		}
		else
		{
			text3 = text;
		}
		if (text3 != "u" && text3 != "d")
		{
			int num = GameMath.Mod(BlockFacing.FromFirstLetter(text3).HorizontalAngleIndex - angle / 90, 4);
			text3 = BlockFacing.HORIZONTALS_ANGLEORDER[num].Code.Substring(0, 1);
		}
		if (text2 != "u" && text2 != "d")
		{
			int num2 = GameMath.Mod(BlockFacing.FromFirstLetter(text2).HorizontalAngleIndex - angle / 90, 4);
			text2 = BlockFacing.HORIZONTALS_ANGLEORDER[num2].Code.Substring(0, 1);
		}
		AssetLocation assetLocation = CodeWithParts(text3 + text2);
		if (api.World.GetBlock(assetLocation) == null)
		{
			return CodeWithParts(text2 + text3);
		}
		return assetLocation;
	}
}
