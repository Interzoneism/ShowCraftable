using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockSignPost : Block
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
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

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection bs, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, bs, ref failureCode))
		{
			return false;
		}
		BlockPos pos = bs.Position.DownCopy();
		Block block = world.BlockAccessor.GetBlock(pos);
		if (!block.CanAttachBlockAt(world.BlockAccessor, this, bs.Position, bs.Face))
		{
			JsonObject attributes = block.GetAttributes(world.BlockAccessor, bs.Position);
			if (attributes == null || !attributes.IsTrue("partialAttachable"))
			{
				return false;
			}
		}
		world.BlockAccessor.SetBlock(BlockId, bs.Position);
		return true;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
		if (blockEntity is BlockEntitySignPost)
		{
			((BlockEntitySignPost)blockEntity).OnRightClick(byPlayer);
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
}
