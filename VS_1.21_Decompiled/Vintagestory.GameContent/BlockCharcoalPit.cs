using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockCharcoalPit : Block, IIgnitable
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		interactions = ObjectCacheUtil.GetOrCreate(api, "charcoalpitInteractions", delegate
		{
			List<ItemStack> list = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: true);
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-firepit-ignite",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "shift",
					Itemstacks = list.ToArray(),
					GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
					{
						BlockEntityCharcoalPit obj = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityCharcoalPit;
						return (obj != null && !obj.Lit) ? wi.Itemstacks : null;
					}
				}
			};
		});
	}

	EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
	{
		BlockEntityCharcoalPit obj = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityCharcoalPit;
		if (obj != null && obj.Lit)
		{
			if (!(secondsIgniting > 2f))
			{
				return EnumIgniteState.Ignitable;
			}
			return EnumIgniteState.IgniteNow;
		}
		return EnumIgniteState.NotIgnitable;
	}

	public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
	{
		if (!(api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityCharcoalPit { Lit: false }))
		{
			return EnumIgniteState.NotIgnitablePreventDefault;
		}
		if (!(secondsIgniting > 3f))
		{
			return EnumIgniteState.Ignitable;
		}
		return EnumIgniteState.IgniteNow;
	}

	public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
	{
		if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityCharcoalPit { Lit: false } blockEntityCharcoalPit)
		{
			blockEntityCharcoalPit.IgniteNow();
		}
		handling = EnumHandling.PreventDefault;
	}

	public int GetFirewoodQuantity(IWorldAccessor world, BlockPos pos, ref NatFloat efficiency)
	{
		BlockEntityGroundStorage blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(pos);
		efficiency = blockEntity?.Inventory[0]?.Itemstack?.ItemAttributes?["efficiency"]?.AsObject<NatFloat>() ?? NatFloat.createUniform(0.75f, 0.25f);
		return (blockEntity?.Inventory[0]?.StackSize).GetValueOrDefault();
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
	{
		bool isWindAffected2;
		bool result = base.ShouldReceiveClientParticleTicks(world, player, pos, out isWindAffected2);
		isWindAffected = true;
		return result;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append<WorldInteraction>(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
