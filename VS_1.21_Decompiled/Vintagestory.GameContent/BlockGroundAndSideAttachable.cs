using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockGroundAndSideAttachable : Block
{
	private Dictionary<string, Cuboidi> attachmentAreas;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Dictionary<string, RotatableCube> dictionary = Attributes?["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>();
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

	public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
	{
		IPlayer player = (forEntity as EntityPlayer)?.Player;
		if (forEntity.AnimManager.IsAnimationActive("sleep", "wave", "cheer", "shrug", "cry", "nod", "facepalm", "bow", "laugh", "rage", "scythe", "bowaim", "bowhit"))
		{
			return null;
		}
		if (player?.InventoryManager?.ActiveHotbarSlot != null && !player.InventoryManager.ActiveHotbarSlot.Empty && hand == EnumHand.Left)
		{
			ItemStack itemstack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
			if (itemstack != null && itemstack.Collectible?.GetHeldTpIdleAnimation(player.InventoryManager.ActiveHotbarSlot, forEntity, EnumHand.Right) != null)
			{
				return null;
			}
			if (player != null && player.Entity?.Controls.LeftMouseDown == true)
			{
				string text = itemstack?.Collectible?.GetHeldTpHitAnimation(player.InventoryManager.ActiveHotbarSlot, forEntity);
				if (text != null && text != "knap")
				{
					return null;
				}
			}
		}
		if (hand != EnumHand.Left)
		{
			return "holdinglanternrighthand";
		}
		return "holdinglanternlefthand";
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (byPlayer.Entity.Controls.ShiftKey)
		{
			failureCode = "__ignore__";
			return false;
		}
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		if ((blockSel.Face.IsHorizontal || blockSel.Face == BlockFacing.UP) && TryAttachTo(world, blockSel.Position, blockSel.Face, itemstack))
		{
			return true;
		}
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		for (int i = 0; i < aLLFACES.Length; i++)
		{
			if (aLLFACES[i] != BlockFacing.DOWN && TryAttachTo(world, blockSel.Position, aLLFACES[i], itemstack))
			{
				return true;
			}
		}
		failureCode = "requireattachable";
		return false;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block block = world.BlockAccessor.GetBlock(CodeWithVariant("orientation", "up"));
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariant("orientation", "up")));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (HasBehavior<BlockBehaviorUnstableFalling>())
		{
			base.OnNeighbourBlockChange(world, pos, neibpos);
		}
		else if (!CanStay(world.BlockAccessor, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	private bool TryAttachTo(IWorldAccessor world, BlockPos blockpos, BlockFacing onBlockFace, ItemStack byItemstack)
	{
		BlockPos pos = blockpos.AddCopy(onBlockFace.Opposite);
		Block block = world.BlockAccessor.GetBlock(pos);
		Cuboidi value = null;
		attachmentAreas?.TryGetValue(onBlockFace.Opposite.Code, out value);
		if (block.CanAttachBlockAt(world.BlockAccessor, this, pos, onBlockFace, value))
		{
			int blockId = world.BlockAccessor.GetBlock(CodeWithVariant("orientation", onBlockFace.Code)).BlockId;
			world.BlockAccessor.SetBlock(blockId, blockpos, byItemstack);
			return true;
		}
		return false;
	}

	private bool CanStay(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockFacing blockFacing = BlockFacing.FromCode(Variant["orientation"]);
		BlockPos pos2 = pos.AddCopy(blockFacing.Opposite);
		Block block = blockAccessor.GetBlock(pos2);
		Cuboidi value = null;
		attachmentAreas?.TryGetValue(blockFacing.Opposite.Code, out value);
		return block.CanAttachBlockAt(blockAccessor, this, pos2, blockFacing, value);
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		return false;
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		if (Variant["orientation"] == "up")
		{
			return Code;
		}
		BlockFacing blockFacing = BlockFacing.FromCode(Variant["orientation"]);
		BlockFacing blockFacing2 = BlockFacing.HORIZONTALS_ANGLEORDER[((360 - angle) / 90 + blockFacing.HorizontalAngleIndex) % 4];
		return CodeWithParts(blockFacing2.Code);
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
	{
		BlockFacing blockFacing = BlockFacing.FromCode(Variant["orientation"]);
		if (blockFacing.Axis == axis)
		{
			return CodeWithVariant("orientation", blockFacing.Opposite.Code);
		}
		return Code;
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (CanStay(blockAccessor, pos))
		{
			return base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldGenRand, attributes);
		}
		return false;
	}
}
