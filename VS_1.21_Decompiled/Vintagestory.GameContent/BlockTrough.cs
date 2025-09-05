using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockTrough : BlockTroughBase
{
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		init();
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (blockSel != null)
		{
			BlockPos position = blockSel.Position;
			if (world.BlockAccessor.GetBlockEntity(position) is BlockEntityTrough blockEntityTrough)
			{
				bool num = blockEntityTrough.OnInteract(byPlayer, blockSel);
				if (num && world.Side == EnumAppSide.Client)
				{
					(byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				}
				return num;
			}
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		if (Math.Abs(angle) == 90 || Math.Abs(angle) == 270)
		{
			string text = Variant["side"];
			return CodeWithVariant("side", (text == "we") ? "ns" : "we");
		}
		return Code;
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

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		if (LastCodePart(1) == "feet")
		{
			BlockFacing opposite = BlockFacing.FromCode(LastCodePart()).Opposite;
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
		return capi.BlockTextureAtlas.GetRandomColor(Textures["wood"].Baked.TextureSubId, rndIndex);
	}

	public override int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
	{
		int textureSubId = Textures["wood"].Baked.TextureSubId;
		return capi.BlockTextureAtlas.GetAverageColor(textureSubId);
	}
}
