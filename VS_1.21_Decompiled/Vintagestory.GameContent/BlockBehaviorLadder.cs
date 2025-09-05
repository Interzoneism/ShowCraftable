using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorLadder : BlockBehavior
{
	[DocumentAsJson("Optional", "north", false)]
	private string dropBlockFace = "north";

	private string ownFirstCodePart;

	[DocumentAsJson("Optional", "False", false)]
	public bool isFlexible;

	public string LadderType => block.Variant["material"];

	public BlockBehaviorLadder(Block block)
		: base(block)
	{
		ownFirstCodePart = block.FirstCodePart();
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		if (properties["dropBlockFace"].Exists)
		{
			dropBlockFace = properties["dropBlockFace"].AsString();
		}
		isFlexible = properties["isFlexible"].AsBool();
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		if (isFlexible && !byPlayer.Entity.Controls.ShiftKey)
		{
			TryCollectLowest(byPlayer, world, blockSel.Position);
			handling = EnumHandling.PreventDefault;
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
	{
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handled, ref string failureCode)
	{
		handled = EnumHandling.PreventDefault;
		if (isFlexible && !byPlayer.Entity.Controls.ShiftKey)
		{
			failureCode = "sneaktoplace";
			return false;
		}
		BlockPos position = blockSel.Position;
		BlockPos pos = (blockSel.DidOffset ? position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
		Block block = world.BlockAccessor.GetBlock(position.UpCopy());
		string text = block.GetBehavior<BlockBehaviorLadder>()?.LadderType;
		if (!isFlexible && text == LadderType && HasSupport(block, world.BlockAccessor, position) && block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			block.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			return true;
		}
		Block block2 = world.BlockAccessor.GetBlock(position.DownCopy());
		if (block2.GetBehavior<BlockBehaviorLadder>()?.LadderType == LadderType && HasSupport(block2, world.BlockAccessor, position) && block2.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			block2.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			return true;
		}
		if (blockSel.HitPosition.Y < 0.5 && TryStackDown(byPlayer, world, pos, blockSel.Face, itemstack))
		{
			return true;
		}
		if (TryStackUp(byPlayer, world, pos, blockSel.Face, itemstack))
		{
			return true;
		}
		if (TryStackDown(byPlayer, world, pos, blockSel.Face, itemstack))
		{
			return true;
		}
		if (isFlexible && blockSel.Face.IsVertical)
		{
			failureCode = "cantattachladder";
			return false;
		}
		AssetLocation code;
		if (blockSel.Face.IsVertical)
		{
			BlockFacing[] array = Block.SuggestedHVOrientation(byPlayer, blockSel);
			code = base.block.CodeWithParts(array[0].Code);
		}
		else
		{
			code = base.block.CodeWithParts(blockSel.Face.Opposite.Code);
		}
		Block block3 = world.BlockAccessor.GetBlock(code);
		if (HasSupport(block3, world.BlockAccessor, position) && block3.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			block3.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			return true;
		}
		code = base.block.CodeWithParts(blockSel.Face.Opposite.Code);
		block3 = world.BlockAccessor.GetBlock(code);
		if (block3 != null && HasSupport(block3, world.BlockAccessor, position) && block3.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			block3.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			return true;
		}
		failureCode = "cantattachladder";
		return false;
	}

	protected bool TryStackUp(IPlayer byPlayer, IWorldAccessor world, BlockPos pos, BlockFacing face, ItemStack itemstack)
	{
		if (isFlexible)
		{
			return false;
		}
		Block block = world.BlockAccessor.GetBlock(pos);
		if (block.GetBehavior<BlockBehaviorLadder>()?.LadderType != LadderType)
		{
			return false;
		}
		BlockPos blockPos = pos.UpCopy();
		Block block2 = null;
		while (blockPos.Y < world.BlockAccessor.MapSizeY)
		{
			block2 = world.BlockAccessor.GetBlock(blockPos);
			if (block2.FirstCodePart() != ownFirstCodePart)
			{
				break;
			}
			blockPos.Up();
		}
		string failureCode = "";
		if (block2 == null || block2.FirstCodePart() == ownFirstCodePart)
		{
			return false;
		}
		if (!block.CanPlaceBlock(world, byPlayer, new BlockSelection
		{
			Position = blockPos,
			Face = face
		}, ref failureCode))
		{
			return false;
		}
		block.DoPlaceBlock(world, byPlayer, new BlockSelection
		{
			Position = blockPos,
			Face = face
		}, itemstack);
		BlockPos blockPos2 = new BlockPos(pos.dimension);
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			blockPos2.Set(blockPos).Offset(facing);
			world.BlockAccessor.GetBlock(blockPos2).OnNeighbourBlockChange(world, blockPos2, blockPos);
		}
		return true;
	}

	protected bool TryStackDown(IPlayer byPlayer, IWorldAccessor world, BlockPos pos, BlockFacing face, ItemStack itemstack)
	{
		Block block = world.BlockAccessor.GetBlock(pos);
		if (block.GetBehavior<BlockBehaviorLadder>()?.LadderType != LadderType)
		{
			return false;
		}
		BlockPos blockPos = pos.DownCopy();
		Block block2 = null;
		while (blockPos.Y > 0)
		{
			block2 = world.BlockAccessor.GetBlock(blockPos);
			if (block2.FirstCodePart() != ownFirstCodePart)
			{
				break;
			}
			blockPos.Down();
		}
		string failureCode = "";
		if (block2 == null || block2.FirstCodePart() == ownFirstCodePart)
		{
			return false;
		}
		if (!block2.IsReplacableBy(base.block))
		{
			return false;
		}
		if (!block.CanPlaceBlock(world, byPlayer, new BlockSelection
		{
			Position = blockPos,
			Face = face
		}, ref failureCode))
		{
			return false;
		}
		block.DoPlaceBlock(world, byPlayer, new BlockSelection
		{
			Position = blockPos,
			Face = face
		}, itemstack);
		BlockPos blockPos2 = new BlockPos(pos.dimension);
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			blockPos2.Set(blockPos).Offset(facing);
			world.BlockAccessor.GetBlock(blockPos2).OnNeighbourBlockChange(world, blockPos2, blockPos);
		}
		return true;
	}

	protected bool TryCollectLowest(IPlayer byPlayer, IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlock(pos).FirstCodePart() != ownFirstCodePart)
		{
			return false;
		}
		BlockPos blockPos = pos.DownCopy();
		while (blockPos.Y > 0 && !(world.BlockAccessor.GetBlock(blockPos).FirstCodePart() != ownFirstCodePart))
		{
			blockPos.Down();
		}
		blockPos.Up();
		Block block = world.BlockAccessor.GetBlock(blockPos);
		BlockBehaviorLadder behavior = block.GetBehavior<BlockBehaviorLadder>();
		if (behavior == null || !behavior.isFlexible)
		{
			return false;
		}
		if (!world.Claims.TryAccess(byPlayer, blockPos, EnumBlockAccessFlags.BuildOrBreak))
		{
			return false;
		}
		ItemStack[] drops = block.GetDrops(world, pos, byPlayer);
		world.BlockAccessor.SetBlock(0, blockPos);
		world.PlaySoundAt(block.Sounds.Break, pos, 0.0, byPlayer);
		if (drops.Length != 0 && !byPlayer.InventoryManager.TryGiveItemstack(drops[0], slotNotifyEffect: true))
		{
			world.SpawnItemEntity(drops[0], byPlayer.Entity.Pos.XYZ);
		}
		return true;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
	{
		if (!HasSupport(block, world.BlockAccessor, pos))
		{
			handling = EnumHandling.PreventSubsequent;
			world.BlockAccessor.BreakBlock(pos, null);
			world.BlockAccessor.TriggerNeighbourBlockUpdate(pos.Copy());
		}
		else
		{
			base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
		}
	}

	public bool HasSupportUp(Block forBlock, IBlockAccessor blockAccess, BlockPos pos)
	{
		BlockFacing facing = BlockFacing.FromCode(forBlock.LastCodePart());
		BlockPos pos2 = pos.UpCopy();
		if (!SideSolid(blockAccess, pos, facing) && !SideSolid(blockAccess, pos, BlockFacing.UP))
		{
			if (pos.Y < blockAccess.MapSizeY - 1 && blockAccess.GetBlock(pos2) == forBlock)
			{
				return HasSupportUp(forBlock, blockAccess, pos2);
			}
			return false;
		}
		return true;
	}

	public bool HasSupportDown(Block forBlock, IBlockAccessor blockAccess, BlockPos pos)
	{
		BlockFacing facing = BlockFacing.FromCode(forBlock.LastCodePart());
		BlockPos pos2 = pos.DownCopy();
		if (!SideSolid(blockAccess, pos, facing) && !SideSolid(blockAccess, pos, BlockFacing.DOWN))
		{
			if (pos.Y > 0 && blockAccess.GetBlock(pos2) == forBlock)
			{
				return HasSupportDown(forBlock, blockAccess, pos2);
			}
			return false;
		}
		return true;
	}

	public bool HasSupport(Block forBlock, IBlockAccessor blockAccess, BlockPos pos)
	{
		BlockFacing facing = BlockFacing.FromCode(forBlock.LastCodePart());
		BlockPos pos2 = pos.DownCopy();
		BlockPos pos3 = pos.UpCopy();
		if (!SideSolid(blockAccess, pos, facing) && (isFlexible || !SideSolid(blockAccess, pos, BlockFacing.DOWN)) && !SideSolid(blockAccess, pos, BlockFacing.UP) && (pos.Y >= blockAccess.MapSizeY - 1 || blockAccess.GetBlock(pos3) != forBlock || !HasSupportUp(forBlock, blockAccess, pos3)))
		{
			if (!isFlexible && pos.Y > 0 && blockAccess.GetBlock(pos2) == forBlock)
			{
				return HasSupportDown(forBlock, blockAccess, pos2);
			}
			return false;
		}
		return true;
	}

	public bool SideSolid(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing)
	{
		BlockPos pos2 = pos.AddCopy(facing);
		Block block = blockAccess.GetBlock(pos2);
		if (block.Id == 0)
		{
			return false;
		}
		Cuboidi attachmentArea = new Cuboidi(14, 0, 0, 15, 7, 15).RotatedCopy(0, 90 * facing.HorizontalAngleIndex, 0, new Vec3d(7.5, 0.0, 7.5));
		if (block.CanAttachBlockAt(blockAccess, block, pos2, facing.Opposite, attachmentArea))
		{
			return true;
		}
		Cuboidi attachmentArea2 = new Cuboidi(14, 8, 0, 15, 15, 15).RotatedCopy(0, 90 * facing.HorizontalAngleIndex, 0, new Vec3d(7.5, 0.0, 7.5));
		if (block.CanAttachBlockAt(blockAccess, block, pos2, facing.Opposite, attachmentArea2))
		{
			return true;
		}
		return false;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		return new ItemStack[1]
		{
			new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts(dropBlockFace)))
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		return new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts(dropBlockFace)));
	}

	public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		int num = GameMath.Mod(BlockFacing.FromCode(block.LastCodePart()).HorizontalAngleIndex - angle / 90, 4);
		BlockFacing blockFacing = BlockFacing.HORIZONTALS_ANGLEORDER[num];
		return block.CodeWithParts(blockFacing.Code);
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing blockFacing = BlockFacing.FromCode(block.LastCodePart());
		if (blockFacing.Axis == axis)
		{
			return block.CodeWithParts(blockFacing.Opposite.Code);
		}
		return block.Code;
	}
}
