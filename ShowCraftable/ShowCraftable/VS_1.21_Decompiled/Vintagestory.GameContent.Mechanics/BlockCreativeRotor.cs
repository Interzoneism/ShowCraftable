using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockCreativeRotor : BlockMPBase, IMPPowered
{
	private BlockFacing powerOutFacing;

	public override void OnLoaded(ICoreAPI api)
	{
		powerOutFacing = BlockFacing.FromCode(Variant["side"]).Opposite;
		base.OnLoaded(api);
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
	}

	public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		return face == powerOutFacing;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing blockFacing in hORIZONTALS)
		{
			BlockPos pos = blockSel.Position.AddCopy(blockFacing);
			if (world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock mechanicalPowerBlock && mechanicalPowerBlock.HasMechPowerConnectorAt(world, pos, blockFacing.Opposite))
			{
				if (mechanicalPowerBlock is IMPPowered)
				{
					return false;
				}
				Block block = world.GetBlock(new AssetLocation(FirstCodePart() + "-" + blockFacing.Opposite.Code));
				world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
				mechanicalPowerBlock.DidConnectAt(world, pos, blockFacing.Opposite);
				WasPlaced(world, blockSel.Position, blockFacing);
				return true;
			}
		}
		bool num = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
		if (num)
		{
			WasPlaced(world, blockSel.Position, null);
		}
		return num;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		return (world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorMPCreativeRotor>())?.OnInteract(byPlayer) ?? base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-creativerotor-power",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = Array.Empty<ItemStack>()
			}
		};
	}
}
