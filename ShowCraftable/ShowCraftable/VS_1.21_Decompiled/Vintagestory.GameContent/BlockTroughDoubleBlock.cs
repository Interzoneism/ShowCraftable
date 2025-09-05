using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockTroughDoubleBlock : BlockTroughBase
{
	public override void OnLoaded(ICoreAPI api)
	{
		if (Variant["part"] == "large-feet")
		{
			RootOffset.Set(BlockFacing.FromCode(Variant["side"]).Opposite.Normali);
		}
		base.OnLoaded(api);
		init();
	}

	public BlockFacing OtherPartFacing()
	{
		BlockFacing blockFacing = BlockFacing.FromCode(Variant["side"]);
		if (Variant["part"] == "large-feet")
		{
			blockFacing = blockFacing.Opposite;
		}
		return blockFacing;
	}

	public BlockPos OtherPartPos(BlockPos pos)
	{
		BlockFacing blockFacing = BlockFacing.FromCode(Variant["side"]);
		if (Variant["part"] == "large-feet")
		{
			blockFacing = blockFacing.Opposite;
		}
		return pos.AddCopy(blockFacing);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			BlockFacing[] array = Block.SuggestedHVOrientation(byPlayer, blockSel);
			BlockPos position = blockSel.Position.AddCopy(array[0]);
			if (!CanPlaceBlock(world, byPlayer, new BlockSelection
			{
				Position = position,
				Face = blockSel.Face
			}, ref failureCode))
			{
				return false;
			}
			string code = array[0].Opposite.Code;
			world.BlockAccessor.GetBlock(CodeWithVariants(new string[2] { "part", "side" }, new string[2] { "large-head", code })).DoPlaceBlock(world, byPlayer, new BlockSelection
			{
				Position = position,
				Face = blockSel.Face
			}, itemstack);
			AssetLocation code2 = CodeWithVariants(new string[2] { "part", "side" }, new string[2] { "large-feet", code });
			world.BlockAccessor.GetBlock(code2).DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			return true;
		}
		return false;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (blockSel != null)
		{
			bool flag = (world.BlockAccessor.GetBlockEntity(blockSel.Position.AddCopy(RootOffset)) as BlockEntityTrough)?.OnInteract(byPlayer, blockSel) ?? false;
			if (flag && world.Side == EnumAppSide.Client)
			{
				(byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				return true;
			}
			return flag;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
	{
		BlockFacing blockFacing = BlockFacing.FromCode(Variant["side"]);
		if (Variant["part"] == "large-feet")
		{
			blockFacing = blockFacing.Opposite;
		}
		Block block = world.BlockAccessor.GetBlock(pos.AddCopy(blockFacing));
		if (block is BlockTroughDoubleBlock && block.Variant["part"] != Variant["part"])
		{
			if (Variant["part"] == "large-feet")
			{
				(world.BlockAccessor.GetBlockEntity(pos.AddCopy(blockFacing)) as BlockEntityTrough)?.OnBlockBroken();
			}
			world.BlockAccessor.SetBlock(0, pos.AddCopy(blockFacing));
		}
		base.OnBlockRemoved(world, pos);
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1]
		{
			new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariants(new string[2] { "part", "side" }, new string[2] { "large-head", "north" })))
		};
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		int num = GameMath.Mod(BlockFacing.FromCode(Variant["side"]).HorizontalAngleIndex - angle / 90, 4);
		BlockFacing blockFacing = BlockFacing.HORIZONTALS_ANGLEORDER[num];
		return CodeWithParts(blockFacing.Code);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariants(new string[2] { "part", "side" }, new string[2] { "large-head", "north" })));
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
	{
		BlockFacing blockFacing = BlockFacing.FromCode(Variant["side"]);
		if (blockFacing.Axis == axis)
		{
			return CodeWithParts(blockFacing.Opposite.Code);
		}
		return Code;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		if (Variant["part"] == "large-feet")
		{
			BlockFacing opposite = BlockFacing.FromCode(Variant["side"]).Opposite;
			pos = pos.AddCopy(opposite);
		}
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityTrough blockEntityTrough)
		{
			StringBuilder stringBuilder = new StringBuilder();
			blockEntityTrough.GetBlockInfo(forPlayer, stringBuilder);
			return stringBuilder.ToString();
		}
		return base.GetPlacedBlockInfo(world, pos, forPlayer);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (Textures.TryGetValue("aged", out var value))
		{
			capi.BlockTextureAtlas.GetRandomColor(value.Baked.TextureSubId, rndIndex);
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public override int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
	{
		if (Textures.TryGetValue("aged", out var value))
		{
			return capi.BlockTextureAtlas.GetAverageColor(value.Baked.TextureSubId);
		}
		return base.GetColorWithoutTint(capi, pos);
	}
}
