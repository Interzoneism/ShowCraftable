using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorOmniAttachable : BlockBehavior
{
	[DocumentAsJson("Optional", "orientation", false)]
	public string facingCode = "orientation";

	[DocumentAsJson("Optional", "None", false)]
	private Dictionary<string, Cuboidi> attachmentAreas;

	public BlockBehaviorOmniAttachable(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		facingCode = properties["facingCode"].AsString("orientation");
		Dictionary<string, RotatableCube> dictionary = properties["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>();
		if (dictionary == null)
		{
			return;
		}
		attachmentAreas = new Dictionary<string, Cuboidi>();
		foreach (KeyValuePair<string, RotatableCube> item in dictionary)
		{
			item.Value.Origin.Set(8.0, 8.0, 8.0);
			attachmentAreas[item.Key] = item.Value.RotatedCopy().ConvertToCuboidi();
		}
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		if (TryAttachTo(world, byPlayer, blockSel.Position, blockSel.HitPosition, blockSel.Face, itemstack))
		{
			return true;
		}
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		for (int i = 0; i < aLLFACES.Length; i++)
		{
			if (TryAttachTo(world, byPlayer, blockSel.Position, blockSel.HitPosition, aLLFACES[i], itemstack))
			{
				return true;
			}
		}
		failureCode = "requireattachable";
		return false;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		Block block = world.BlockAccessor.GetBlock(base.block.CodeWithVariant(facingCode, "up"));
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		return new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithVariant(facingCode, "up")));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (!CanStay(world, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	private bool TryAttachTo(IWorldAccessor world, IPlayer byPlayer, BlockPos blockpos, Vec3d hitPosition, BlockFacing onBlockFace, ItemStack itemstack)
	{
		BlockPos pos = blockpos.AddCopy(onBlockFace.Opposite);
		Block block = world.BlockAccessor.GetBlock(world.BlockAccessor.GetBlockId(pos));
		Block obj = world.BlockAccessor.GetBlock(blockpos);
		Cuboidi value = null;
		attachmentAreas?.TryGetValue(onBlockFace.Code, out value);
		if (obj.Replaceable >= 6000 && block.CanAttachBlockAt(world.BlockAccessor, base.block, pos, onBlockFace, value))
		{
			world.BlockAccessor.GetBlock(base.block.CodeWithVariant(facingCode, onBlockFace.Code)).DoPlaceBlock(world, byPlayer, new BlockSelection
			{
				Position = blockpos,
				HitPosition = hitPosition,
				Face = onBlockFace
			}, itemstack);
			return true;
		}
		return false;
	}

	private bool CanStay(IWorldAccessor world, BlockPos pos)
	{
		BlockFacing blockFacing = BlockFacing.FromCode(block.Variant[facingCode]);
		BlockPos pos2 = pos.AddCopy(blockFacing.Opposite);
		Block obj = world.BlockAccessor.GetBlock(world.BlockAccessor.GetBlockId(pos2));
		BlockFacing blockFace = blockFacing;
		Cuboidi value = null;
		attachmentAreas?.TryGetValue(blockFacing.Code, out value);
		return obj.CanAttachBlockAt(world.BlockAccessor, block, pos2, blockFace, value);
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handled, Cuboidi attachmentArea = null)
	{
		handled = EnumHandling.PreventDefault;
		return false;
	}

	public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (block.Variant[facingCode] == "up" || block.Variant[facingCode] == "down")
		{
			return block.Code;
		}
		BlockFacing blockFacing = BlockFacing.HORIZONTALS_ANGLEORDER[((360 - angle) / 90 + BlockFacing.FromCode(block.Variant[facingCode]).HorizontalAngleIndex) % 4];
		return block.CodeWithParts(blockFacing.Code);
	}

	public override AssetLocation GetVerticallyFlippedBlockCode(ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		if (!(block.Variant[facingCode] == "up"))
		{
			return block.CodeWithVariant(facingCode, "up");
		}
		return block.CodeWithVariant(facingCode, "down");
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing blockFacing = BlockFacing.FromCode(block.Variant[facingCode]);
		if (blockFacing.Axis == axis)
		{
			return block.CodeWithVariant(facingCode, blockFacing.Opposite.Code);
		}
		return block.Code;
	}
}
