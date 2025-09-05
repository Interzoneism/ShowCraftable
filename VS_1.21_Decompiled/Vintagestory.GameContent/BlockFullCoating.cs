using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockFullCoating : Block
{
	private BlockFacing[] ownFacings;

	private Cuboidf[] selectionBoxes;

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
	{
		return selectionBoxes;
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		string text = Variant["coating"];
		ownFacings = new BlockFacing[text.Length];
		selectionBoxes = new Cuboidf[ownFacings.Length];
		for (int i = 0; i < text.Length; i++)
		{
			ownFacings[i] = BlockFacing.FromFirstLetter(text[i]);
			switch (text[i])
			{
			case 'n':
				selectionBoxes[i] = new Cuboidf(0f, 0f, 0f, 1f, 1f, 0.0625f);
				break;
			case 'e':
				selectionBoxes[i] = new Cuboidf(0f, 0f, 0f, 1f, 1f, 0.0625f).RotatedCopy(0f, 270f, 0f, new Vec3d(0.5, 0.5, 0.5));
				break;
			case 's':
				selectionBoxes[i] = new Cuboidf(0f, 0f, 0f, 1f, 1f, 0.0625f).RotatedCopy(0f, 180f, 0f, new Vec3d(0.5, 0.5, 0.5));
				break;
			case 'w':
				selectionBoxes[i] = new Cuboidf(0f, 0f, 0f, 1f, 1f, 0.0625f).RotatedCopy(0f, 90f, 0f, new Vec3d(0.5, 0.5, 0.5));
				break;
			case 'u':
				selectionBoxes[i] = new Cuboidf(0f, 0f, 0f, 1f, 0.0625f, 1f).RotatedCopy(180f, 0f, 0f, new Vec3d(0.5, 0.5, 0.5));
				break;
			case 'd':
				selectionBoxes[i] = new Cuboidf(0f, 0f, 0f, 1f, 0.0625f, 1f);
				break;
			}
		}
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		return TryPlaceBlockForWorldGen(world.BlockAccessor, blockSel.Position, blockSel.Face);
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		int num = 0;
		for (int i = 0; i < ownFacings.Length; i++)
		{
			num += ((world.Rand.NextDouble() > (double)Drops[0].Quantity.nextFloat()) ? 1 : 0);
		}
		ItemStack itemStack = Drops[0].ResolvedItemstack.Clone();
		itemStack.StackSize = Math.Max(1, num);
		return new ItemStack[1] { itemStack };
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariant("coating", "d")));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		string text = "";
		BlockFacing[] array = ownFacings;
		foreach (BlockFacing blockFacing in array)
		{
			if (world.BlockAccessor.GetBlockOnSide(pos, blockFacing).SideSolid[blockFacing.Opposite.Index])
			{
				text += blockFacing.Code[0];
			}
		}
		if (ownFacings.Length <= text.Length)
		{
			return;
		}
		if (text.Length == 0)
		{
			world.BlockAccessor.BreakBlock(pos, null);
			return;
		}
		int num = text.Length - ownFacings.Length;
		for (int j = 0; j < num; j++)
		{
			world.SpawnItemEntity(Drops[0].GetNextItemStack(), pos);
		}
		Block block = world.GetBlock(CodeWithVariant("coating", text));
		world.BlockAccessor.SetBlock(block.BlockId, pos);
	}

	public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		return false;
	}

	public string getSolidFacesAtPos(IBlockAccessor blockAccessor, BlockPos pos)
	{
		string text = "";
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing in aLLFACES)
		{
			blockFacing.IterateThruFacingOffsets(pos);
			if (blockAccessor.GetBlock(pos).SideSolid[blockFacing.Opposite.Index])
			{
				text += blockFacing.Code.Substring(0, 1);
			}
		}
		BlockFacing.FinishIteratingAllFaces(pos);
		return text;
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		return TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace);
	}

	public bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace)
	{
		float num = 14f / 51f * (float)api.World.BlockAccessor.MapSizeY;
		float num2 = 0.0627451f * (float)api.World.BlockAccessor.MapSizeY;
		if ((float)pos.Y < num2 || (float)pos.Y > num || blockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight) > 15)
		{
			return false;
		}
		Block block = blockAccessor.GetBlock(pos);
		if (block.Replaceable < 6000 || block.IsLiquid())
		{
			return false;
		}
		string solidFacesAtPos = getSolidFacesAtPos(blockAccessor, pos);
		if (solidFacesAtPos.Length > 0)
		{
			Block block2 = blockAccessor.GetBlock(CodeWithVariant("coating", solidFacesAtPos));
			blockAccessor.SetBlock(block2.BlockId, pos);
		}
		return true;
	}
}
