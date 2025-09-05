using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockWaterLily : BlockPlant
{
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		IceCheckOffset = -1;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (CanPlantStay(world.BlockAccessor, blockSel.Position.UpCopy()))
		{
			blockSel = blockSel.Clone();
			blockSel.Position = blockSel.Position.Up();
			return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
		}
		failureCode = "requirefreshwater";
		return false;
	}

	public override bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos pos)
	{
		Block blockBelow = blockAccessor.GetBlockBelow(pos, 1, 2);
		Block block = blockAccessor.GetBlock(pos, 2);
		if (blockBelow.IsLiquid() && blockBelow.LiquidLevel == 7 && blockBelow.LiquidCode == "water")
		{
			return block.Id == 0;
		}
		return false;
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		int colorWithoutTint = GetColorWithoutTint(capi, pos);
		return capi.World.ApplyColorMapOnRgba("climatePlantTint", "seasonalFoliage", colorWithoutTint, pos.X, pos.Y, pos.Z);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		CompositeTexture value = Textures.First().Value;
		if (value?.Baked == null)
		{
			return 0;
		}
		int randomColor = capi.BlockTextureAtlas.GetRandomColor(value.Baked.TextureSubId, rndIndex);
		return capi.World.ApplyColorMapOnRgba("climatePlantTint", "seasonalFoliage", randomColor, pos.X, pos.Y, pos.Z);
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (blockAccessor.GetBlockBelow(pos, 4, 2).Id != 0)
		{
			return false;
		}
		if (blockAccessor.GetBlockBelow(pos, 1, 1) is BlockPlant)
		{
			return false;
		}
		return base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldGenRand, attributes);
	}
}
