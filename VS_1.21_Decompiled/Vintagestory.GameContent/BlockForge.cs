using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockForge : Block, IIgnitable
{
	private WorldInteraction[] interactions;

	public List<ItemStack> coalStacklist = new List<ItemStack>();

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		ICoreClientAPI capi = api as ICoreClientAPI;
		interactions = ObjectCacheUtil.GetOrCreate(api, "forgeBlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			List<ItemStack> list2 = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: false);
			foreach (CollectibleObject collectible in api.World.Collectibles)
			{
				switch (collectible.FirstCodePart())
				{
				case "ingot":
				case "metalplate":
				case "workitem":
				{
					List<ItemStack> handBookStacks2 = collectible.GetHandBookStacks(capi);
					if (handBookStacks2 != null)
					{
						list.AddRange(handBookStacks2);
					}
					break;
				}
				default:
				{
					CombustibleProperties combustibleProps = collectible.CombustibleProps;
					if (combustibleProps != null && combustibleProps.BurnTemperature > 1000)
					{
						List<ItemStack> handBookStacks = collectible.GetHandBookStacks(capi);
						if (handBookStacks != null)
						{
							coalStacklist.AddRange(handBookStacks);
						}
					}
					break;
				}
				}
			}
			return new WorldInteraction[4]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-forge-addworkitem",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
					{
						BlockEntityForge bef = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityForge;
						return (bef != null && bef.Contents != null) ? wi.Itemstacks.Where((ItemStack stack) => stack.Equals(api.World, bef.Contents, GlobalConstants.IgnoredStackAttributes)).ToArray() : wi.Itemstacks;
					}
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-forge-takeworkitem",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityForge { Contents: not null } blockEntityForge) ? new ItemStack[1] { blockEntityForge.Contents } : null
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-forge-fuel",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = coalStacklist.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityForge { FuelLevel: <0.625f }) ? wi.Itemstacks : null
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-forge-ignite",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list2.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityForge { CanIgnite: not false, IsBurning: false }) ? wi.Itemstacks : null
				}
			};
		});
	}

	EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
	{
		if ((byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityForge).Lit)
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
		if (!(byEntity.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityForge { CanIgnite: not false }))
		{
			return EnumIgniteState.NotIgnitablePreventDefault;
		}
		if (secondsIgniting > 0.25f && (int)(30f * secondsIgniting) % 9 == 1)
		{
			Random rand = byEntity.World.Rand;
			Vec3d basePos = new Vec3d((double)((float)pos.X + 0.25f) + 0.5 * rand.NextDouble(), (float)pos.InternalY + 0.875f, (double)((float)pos.Z + 0.25f) + 0.5 * rand.NextDouble());
			Block block = byEntity.World.GetBlock(new AssetLocation("fire"));
			AdvancedParticleProperties advancedParticleProperties = block.ParticleProperties[block.ParticleProperties.Length - 1];
			advancedParticleProperties.basePos = basePos;
			advancedParticleProperties.Quantity.avg = 1f;
			IPlayer dualCallByPlayer = null;
			if (byEntity is EntityPlayer)
			{
				dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			byEntity.World.SpawnParticles(advancedParticleProperties, dualCallByPlayer);
			advancedParticleProperties.Quantity.avg = 0f;
		}
		if (secondsIgniting >= 1.5f)
		{
			return EnumIgniteState.IgniteNow;
		}
		return EnumIgniteState.Ignitable;
	}

	public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
	{
		if (!(secondsIgniting < 1.45f))
		{
			handling = EnumHandling.PreventDefault;
			IPlayer player = null;
			if (byEntity is EntityPlayer)
			{
				player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			if (player != null)
			{
				(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityForge)?.TryIgnite();
			}
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityForge blockEntityForge)
		{
			return blockEntityForge.OnPlayerInteract(world, byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
