using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockStoneCoffinSection : Block
{
	public BlockFacing Orientation => BlockFacing.FromCode(Variant["side"]);

	public bool ControllerBlock => EntityClass != null;

	public bool IsCompleteCoffin(BlockPos pos)
	{
		if (api.World.BlockAccessor.GetBlock(pos.AddCopy(Orientation.Opposite)) is BlockStoneCoffinSection blockStoneCoffinSection)
		{
			return blockStoneCoffinSection.Orientation == Orientation.Opposite;
		}
		return false;
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		int temperature = GetTemperature(api.World, pos);
		int num = GameMath.Clamp((temperature - 550) / 2, 0, 255);
		for (int i = 0; i < sourceMesh.FlagsCount; i++)
		{
			sourceMesh.Flags[i] &= -256;
			sourceMesh.Flags[i] |= num;
		}
		int[] incandescenceColor = ColorUtil.getIncandescenceColor(temperature);
		float num2 = GameMath.Clamp((float)incandescenceColor[3] / 255f, 0f, 1f);
		for (int j = 0; j < lightRgbsByCorner.Length; j++)
		{
			int num3 = lightRgbsByCorner[j];
			int v = num3 & 0xFF;
			int v2 = (num3 >> 8) & 0xFF;
			int v3 = (num3 >> 16) & 0xFF;
			int v4 = (num3 >> 24) & 0xFF;
			lightRgbsByCorner[j] = (GameMath.Mix(v4, 0, Math.Min(1f, 1.5f * num2)) << 24) | (GameMath.Mix(v3, incandescenceColor[2], num2) << 16) | (GameMath.Mix(v2, incandescenceColor[1], num2) << 8) | GameMath.Mix(v, incandescenceColor[0], num2);
		}
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		if (blockFace == BlockFacing.UP && block.FirstCodePart() == "stonecoffinlid")
		{
			return true;
		}
		return base.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea);
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		BlockPos position = blockSel.Position;
		Block block = this;
		if (byPlayer != null && !byPlayer.Entity.Controls.ShiftKey)
		{
			BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
			foreach (BlockFacing blockFacing in hORIZONTALS)
			{
				if (world.BlockAccessor.GetBlock(position.AddCopy(blockFacing)) is BlockStoneCoffinSection blockStoneCoffinSection && blockStoneCoffinSection.Orientation == blockFacing)
				{
					block = api.World.GetBlock(CodeWithVariant("side", blockFacing.Opposite.Code));
					break;
				}
			}
		}
		world.BlockAccessor.SetBlock(block.BlockId, position);
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockPos blockPos = blockSel.Position;
		if (!ControllerBlock)
		{
			blockPos = GetControllerBlockPositionOrNull(blockSel.Position);
		}
		if (blockPos == null)
		{
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		if (world.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityStoneCoffin blockEntityStoneCoffin)
		{
			blockEntityStoneCoffin.Interact(byPlayer, !ControllerBlock);
			(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public int GetTemperature(IWorldAccessor world, BlockPos pos)
	{
		if (!ControllerBlock)
		{
			pos = GetControllerBlockPositionOrNull(pos);
		}
		if (pos == null)
		{
			return 0;
		}
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityStoneCoffin blockEntityStoneCoffin)
		{
			return blockEntityStoneCoffin.CoffinTemperature;
		}
		return 0;
	}

	private BlockPos GetControllerBlockPositionOrNull(BlockPos pos)
	{
		if (!ControllerBlock && IsCompleteCoffin(pos))
		{
			return pos.AddCopy(Orientation.Opposite);
		}
		return null;
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		BlockPos controllerBlockPositionOrNull;
		if ((controllerBlockPositionOrNull = GetControllerBlockPositionOrNull(pos)) != null)
		{
			return api.World.BlockAccessor.GetBlock(controllerBlockPositionOrNull).GetPlacedBlockInfo(world, controllerBlockPositionOrNull, forPlayer);
		}
		return base.GetPlacedBlockInfo(world, pos, forPlayer);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		BlockPos controllerBlockPositionOrNull;
		if ((controllerBlockPositionOrNull = GetControllerBlockPositionOrNull(selection.Position)) != null)
		{
			BlockSelection blockSelection = selection.Clone();
			blockSelection.Position = controllerBlockPositionOrNull;
			return api.World.BlockAccessor.GetBlock(controllerBlockPositionOrNull).GetPlacedBlockInteractionHelp(world, blockSelection, forPlayer);
		}
		BlockEntityStoneCoffin blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityStoneCoffin>(selection.Position);
		if (blockEntity != null && !blockEntity.StructureComplete)
		{
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-mulblock-struc-show",
					HotKeyCodes = new string[1] { "shift" },
					MouseButton = EnumMouseButton.Right
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-mulblock-struc-hide",
					HotKeyCodes = new string[1] { "ctrl" },
					MouseButton = EnumMouseButton.Right
				}
			};
		}
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		BlockPos controllerBlockPositionOrNull;
		if ((controllerBlockPositionOrNull = GetControllerBlockPositionOrNull(pos)) != null)
		{
			world.BlockAccessor.BreakBlock(controllerBlockPositionOrNull, byPlayer, dropQuantityMultiplier);
		}
		if (ControllerBlock && IsCompleteCoffin(pos))
		{
			world.BlockAccessor.BreakBlock(pos.AddCopy(Orientation.Opposite), byPlayer, dropQuantityMultiplier);
		}
	}
}
