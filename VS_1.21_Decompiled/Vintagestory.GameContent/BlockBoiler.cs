using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBoiler : BlockLiquidContainerBase, IIgnitable
{
	private Block firepitBlock;

	private WorldInteraction[] boilerinteractions;

	private Cuboidf[] partCollBoxes;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		firepitBlock = api.World.GetBlock(BlockEntityBoiler.firepitShapeBlockCodes[6]);
		partCollBoxes = (Cuboidf[])CollisionBoxes.Clone();
		partCollBoxes[0].Y1 = 0.4375f;
		boilerinteractions = ObjectCacheUtil.GetOrCreate(api, "boilerInteractions", delegate
		{
			List<ItemStack> list = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: true);
			List<ItemStack> list2 = new List<ItemStack>();
			List<ItemStack> list3 = new List<ItemStack>();
			foreach (Item item in api.World.Items)
			{
				if (item is ItemDryGrass)
				{
					list2.Add(new ItemStack((CollectibleObject)item, 1));
				}
				if (item.Attributes != null && item.Attributes.IsTrue("isFirewood"))
				{
					list3.Add(new ItemStack((CollectibleObject)item, 1));
				}
			}
			return new WorldInteraction[3]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-firepit-ignite",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "shift",
					Itemstacks = list.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityBoiler { IsBurning: false, fuelHours: >0f, firepitStage: >=5 }) ? wi.Itemstacks : null
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-boiler-addtinder",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list2.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityBoiler { firepitStage: 0 }) ? wi.Itemstacks : null
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-boiler-addfuel",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list3.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityBoiler { firepitStage: >0, fuelHours: <=6f }) ? wi.Itemstacks : null
				}
			};
		});
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(this);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBoiler blockEntityBoiler && blockEntityBoiler.OnInteract(byPlayer, blockSel))
		{
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		if (pos == null)
		{
			return base.GetLightHsv(blockAccessor, pos, stack);
		}
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityBoiler { firepitStage: 6 })
		{
			return firepitBlock.LightHsv;
		}
		return base.GetLightHsv(blockAccessor, pos, stack);
	}

	public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return partCollBoxes;
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
	{
		isWindAffected = true;
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoiler { firepitStage: 6 })
		{
			return true;
		}
		return base.ShouldReceiveClientParticleTicks(world, player, pos, out isWindAffected);
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoiler { firepitStage: 6 })
		{
			AdvancedParticleProperties[] particleProperties = firepitBlock.ParticleProperties;
			if (particleProperties != null && particleProperties.Length != 0)
			{
				foreach (AdvancedParticleProperties advancedParticleProperties in particleProperties)
				{
					advancedParticleProperties.WindAffectednesAtPos = windAffectednessAtPos;
					advancedParticleProperties.basePos.X = (float)pos.X + firepitBlock.TopMiddlePos.X;
					advancedParticleProperties.basePos.Y = (float)pos.InternalY + firepitBlock.TopMiddlePos.Y;
					advancedParticleProperties.basePos.Z = (float)pos.Z + firepitBlock.TopMiddlePos.Z;
					manager.Spawn(advancedParticleProperties);
				}
			}
		}
		else
		{
			base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
		}
	}

	EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
	{
		if ((byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityBoiler).IsBurning)
		{
			if (!(secondsIgniting > 3f))
			{
				return EnumIgniteState.Ignitable;
			}
			return EnumIgniteState.IgniteNow;
		}
		return EnumIgniteState.NotIgnitable;
	}

	public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
	{
		if (!(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityBoiler).CanIgnite())
		{
			return EnumIgniteState.NotIgnitablePreventDefault;
		}
		if (!(secondsIgniting > 4f))
		{
			return EnumIgniteState.Ignitable;
		}
		return EnumIgniteState.IgniteNow;
	}

	public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityBoiler)?.TryIgnite();
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		string placedBlockInfo = base.GetPlacedBlockInfo(world, pos, forPlayer);
		BlockEntityBoiler blockEntityBoiler = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityBoiler;
		float num = blockEntityBoiler?.InputStackTemp ?? 0f;
		placedBlockInfo = ((!(num <= 20f)) ? (placedBlockInfo + "\r\n" + Lang.Get("Temperature: {0}°C", (int)num)) : (placedBlockInfo + "\r\n" + Lang.Get("Cold.")));
		if (blockEntityBoiler != null && blockEntityBoiler.firepitStage >= 5)
		{
			placedBlockInfo = ((!(blockEntityBoiler.fuelHours <= 0f)) ? (placedBlockInfo + "\r\n" + Lang.Get("Fuel for {0:#.#} hours.", blockEntityBoiler.fuelHours)) : (placedBlockInfo + "\r\n" + Lang.Get("No more fuel.")));
		}
		return placedBlockInfo;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(boilerinteractions);
	}
}
