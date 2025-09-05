using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockStaticTranslocator : Block
{
	public SimpleParticleProperties idleParticles;

	public SimpleParticleProperties insideParticles;

	public SimpleParticleProperties teleportParticles;

	public bool Repaired => Variant["state"] != "broken";

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		idleParticles = new SimpleParticleProperties(0.5f, 1f, ColorUtil.ToRgba(150, 34, 47, 44), new Vec3d(), new Vec3d(), new Vec3f(-0.1f, -0.1f, -0.1f), new Vec3f(0.1f, 0.1f, 0.1f), 1.5f, 0f, 0.5f, 0.75f, EnumParticleModel.Quad);
		idleParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f);
		idleParticles.AddPos.Set(1.0, 2.0, 1.0);
		idleParticles.addLifeLength = 0.5f;
		idleParticles.RedEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 80f);
		insideParticles = new SimpleParticleProperties(0.5f, 1f, ColorUtil.ToRgba(150, 92, 111, 107), new Vec3d(), new Vec3d(), new Vec3f(-0.2f, -0.2f, -0.2f), new Vec3f(0.2f, 0.2f, 0.2f), 1.5f, 0f, 0.5f, 0.75f, EnumParticleModel.Quad);
		insideParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f);
		insideParticles.AddPos.Set(1.0, 2.0, 1.0);
		insideParticles.addLifeLength = 0.5f;
		teleportParticles = new SimpleParticleProperties(0.5f, 1f, ColorUtil.ToRgba(150, 92, 111, 107), new Vec3d(), new Vec3d(), new Vec3f(-0.2f, -0.2f, -0.2f), new Vec3f(0.2f, 0.2f, 0.2f), 4.5f, 0f, 0.5f, 0.75f, EnumParticleModel.Quad);
		teleportParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -1f);
		teleportParticles.AddPos.Set(1.0, 2.0, 1.0);
		teleportParticles.addLifeLength = 0.5f;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Empty)
		{
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		if (!Repaired)
		{
			if (activeHotbarSlot.Itemstack.Collectible.Code.Path == "metal-parts" && activeHotbarSlot.StackSize >= 2)
			{
				activeHotbarSlot.TakeOut(2);
				world.PlaySoundAt(new AssetLocation("sounds/effect/latch"), blockSel.Position, -0.25, byPlayer, randomizePitch: true, 16f);
				Block block = world.GetBlock(CodeWithVariant("state", "normal"));
				world.BlockAccessor.SetBlock(block.Id, blockSel.Position);
				if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityStaticTranslocator blockEntityStaticTranslocator)
				{
					blockEntityStaticTranslocator.DoRepair(byPlayer);
				}
				return true;
			}
		}
		else
		{
			if (!(world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityStaticTranslocator blockEntityStaticTranslocator2))
			{
				return false;
			}
			if (!blockEntityStaticTranslocator2.FullyRepaired && activeHotbarSlot.Itemstack.Collectible is ItemTemporalGear)
			{
				blockEntityStaticTranslocator2.DoRepair(byPlayer);
				activeHotbarSlot.TakeOut(1);
				world.PlaySoundAt(new AssetLocation("sounds/effect/latch"), blockSel.Position, -0.25, byPlayer, randomizePitch: true, 16f);
				return true;
			}
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityStaticTranslocator blockEntityStaticTranslocator)
		{
			blockEntityStaticTranslocator.OnEntityCollide(entity);
		}
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		if (!Repaired)
		{
			return Lang.Get("Seems to be missing a couple of gears. I think I've seen such gears before.");
		}
		return base.GetPlacedBlockInfo(world, pos, forPlayer);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (Repaired)
		{
			if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityStaticTranslocator blockEntityStaticTranslocator))
			{
				return base.GetPlacedBlockName(world, pos);
			}
			if (!blockEntityStaticTranslocator.FullyRepaired)
			{
				return world.GetBlock(CodeWithVariant("state", "broken")).GetPlacedBlockName(world, pos);
			}
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		if (!Repaired)
		{
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-translocator-repair-1",
					Itemstacks = new ItemStack[1]
					{
						new ItemStack(world.GetBlock(new AssetLocation("metal-parts")), 2)
					},
					MouseButton = EnumMouseButton.Right
				}
			};
		}
		if (!(world.BlockAccessor.GetBlockEntity(selection.Position) is BlockEntityStaticTranslocator blockEntityStaticTranslocator))
		{
			return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
		}
		if (!blockEntityStaticTranslocator.FullyRepaired)
		{
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-translocator-repair-2",
					Itemstacks = new ItemStack[1]
					{
						new ItemStack(world.GetItem(new AssetLocation("gear-temporal")), 3)
					},
					MouseButton = EnumMouseButton.Right
				}
			};
		}
		if (!blockEntityStaticTranslocator.Activated)
		{
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-translocator-activate",
					Itemstacks = new ItemStack[1]
					{
						new ItemStack(world.GetItem(new AssetLocation("gear-rusty")))
					},
					MouseButton = EnumMouseButton.Right
				}
			};
		}
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		}
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		int num = GameMath.Mod(BlockFacing.FromCode(LastCodePart()).HorizontalAngleIndex - angle / 90, 4);
		BlockFacing blockFacing = BlockFacing.HORIZONTALS_ANGLEORDER[num];
		return CodeWithParts(blockFacing.Code);
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
}
