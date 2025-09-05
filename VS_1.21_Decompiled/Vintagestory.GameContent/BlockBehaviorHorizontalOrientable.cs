using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorHorizontalOrientable : BlockBehavior
{
	[DocumentAsJson("Optional", "north", false)]
	private string dropBlockFace = "north";

	private string variantCode = "horizontalorientation";

	[DocumentAsJson("Optional", "None", false)]
	private JsonItemStack drop;

	public BlockBehaviorHorizontalOrientable(Block block)
		: base(block)
	{
		if (!block.Variant.ContainsKey("horizontalorientation"))
		{
			variantCode = "side";
		}
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		if (properties["dropBlockFace"].Exists)
		{
			dropBlockFace = properties["dropBlockFace"].AsString();
		}
		if (properties["drop"].Exists)
		{
			drop = properties["drop"].AsObject<JsonItemStack>(null, block.Code.Domain);
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		drop?.Resolve(api.World, "HorizontalOrientable drop for " + block.Code);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing[] array = Block.SuggestedHVOrientation(byPlayer, blockSel);
		AssetLocation assetLocation = base.block.CodeWithVariant(variantCode, array[0].Code);
		Block block = world.BlockAccessor.GetBlock(assetLocation);
		if (block == null)
		{
			throw new NullReferenceException(string.Concat("Unable to to find a rotated block with code ", assetLocation, ", you're maybe missing the side variant group of have a dash in your block code"));
		}
		if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			block.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			return true;
		}
		return false;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (drop?.ResolvedItemstack == null)
		{
			return new ItemStack[1]
			{
				new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithVariant(variantCode, dropBlockFace)))
			};
		}
		return new ItemStack[1] { drop?.ResolvedItemstack.Clone() };
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (drop != null)
		{
			return drop?.ResolvedItemstack.Clone();
		}
		return new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithVariant(variantCode, dropBlockFace)));
	}

	public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		int num = GameMath.Mod(BlockFacing.FromCode(block.Variant[variantCode]).HorizontalAngleIndex - angle / 90, 4);
		BlockFacing blockFacing = BlockFacing.HORIZONTALS_ANGLEORDER[num];
		return block.CodeWithVariant(variantCode, blockFacing.Code);
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing blockFacing = BlockFacing.FromCode(block.Variant[variantCode]);
		if (blockFacing.Axis == axis)
		{
			return block.CodeWithVariant(variantCode, blockFacing.Opposite.Code);
		}
		return block.Code;
	}
}
