using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockSign : Block
{
	private WorldInteraction[] interactions;

	public TextAreaConfig signConfig;

	protected bool isWallSign;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		PlacedPriorityInteract = true;
		isWallSign = Variant["attachment"] == "wall";
		signConfig = new TextAreaConfig();
		if (Attributes != null)
		{
			signConfig = Attributes.AsObject(signConfig);
		}
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		interactions = ObjectCacheUtil.GetOrCreate(api, "signBlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject collectible in api.World.Collectibles)
			{
				JsonObject attributes = collectible.Attributes;
				if (attributes != null && attributes["pigment"].Exists)
				{
					list.Add(new ItemStack(collectible));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-sign-write",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (isWallSign)
		{
			return base.GetCollisionBoxes(blockAccessor, pos);
		}
		if (blockAccessor.GetBlockEntity(pos) is BlockEntitySign blockEntitySign)
		{
			return blockEntitySign.colSelBox;
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (isWallSign)
		{
			return base.GetCollisionBoxes(blockAccessor, pos);
		}
		if (blockAccessor.GetBlockEntity(pos) is BlockEntitySign blockEntitySign)
		{
			return blockEntitySign.colSelBox;
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection bs, ref string failureCode)
	{
		BlockPos pos = bs.Position.AddCopy(bs.Face.Opposite);
		Block block = world.BlockAccessor.GetBlock(pos);
		if (bs.Face.IsHorizontal)
		{
			if (!block.CanAttachBlockAt(world.BlockAccessor, this, pos, bs.Face))
			{
				JsonObject attributes = block.GetAttributes(world.BlockAccessor, pos);
				if (attributes == null || !attributes.IsTrue("partialAttachable"))
				{
					goto IL_00d0;
				}
			}
			Block block2 = world.BlockAccessor.GetBlock(CodeWithParts("wall", bs.Face.Opposite.Code));
			if (!block2.CanPlaceBlock(world, byPlayer, bs, ref failureCode))
			{
				return false;
			}
			world.BlockAccessor.SetBlock(block2.BlockId, bs.Position);
			return true;
		}
		goto IL_00d0;
		IL_00d0:
		if (!CanPlaceBlock(world, byPlayer, bs, ref failureCode))
		{
			return false;
		}
		BlockFacing[] array = Block.SuggestedHVOrientation(byPlayer, bs);
		AssetLocation code = CodeWithParts(array[0].Code);
		Block block3 = world.BlockAccessor.GetBlock(code);
		world.BlockAccessor.SetBlock(block3.BlockId, bs.Position);
		if (world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntitySign blockEntitySign)
		{
			BlockPos blockPos = (bs.DidOffset ? bs.Position.AddCopy(bs.Face.Opposite) : bs.Position);
			double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + bs.HitPosition.X);
			double x = (double)(float)byPlayer.Entity.Pos.Z - ((double)blockPos.Z + bs.HitPosition.Z);
			float num = (float)Math.Atan2(y, x);
			float num2 = (float)Math.PI / 4f;
			float meshAngleRad = (float)(int)Math.Round(num / num2) * num2;
			blockEntitySign.MeshAngleRad = meshAngleRad;
		}
		return true;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block block = world.BlockAccessor.GetBlock(CodeWithParts("ground", "north"));
		if (block == null)
		{
			block = world.BlockAccessor.GetBlock(CodeWithParts("wall", "north"));
		}
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		Block block = world.BlockAccessor.GetBlock(CodeWithParts("ground", "north"));
		if (block == null)
		{
			block = world.BlockAccessor.GetBlock(CodeWithParts("wall", "north"));
		}
		return new ItemStack(block);
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
		if (blockEntity is BlockEntitySign)
		{
			((BlockEntitySign)blockEntity).OnRightClick(byPlayer);
			return true;
		}
		return true;
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
	{
		BlockFacing blockFacing = BlockFacing.FromCode(LastCodePart());
		if (blockFacing.Axis == axis)
		{
			return CodeWithParts(blockFacing.Opposite.Code);
		}
		return Code;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		int num = GameMath.Mod(BlockFacing.FromCode(LastCodePart()).HorizontalAngleIndex - angle / 90, 4);
		BlockFacing blockFacing = BlockFacing.HORIZONTALS_ANGLEORDER[num];
		return CodeWithParts(blockFacing.Code);
	}
}
