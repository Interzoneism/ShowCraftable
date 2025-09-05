using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockMultiblock : Block, IMultiblockOffset
{
	public delegate T BlockCallDelegateInterface<T, K>(K block);

	public delegate T BlockCallDelegateBlock<T>(Block block);

	public delegate void BlockCallDelegateInterface<K>(K block);

	public delegate void BlockCallDelegateBlock(Block block);

	public Vec3i Offset;

	public Vec3i OffsetInv;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Offset = new Vec3i(Variant["dx"].Replace("n", "-").Replace("p", "").ToInt(), Variant["dy"].Replace("n", "-").Replace("p", "").ToInt(), Variant["dz"].Replace("n", "-").Replace("p", "").ToInt());
		OffsetInv = -Offset;
	}

	private T Handle<T, K>(IBlockAccessor ba, int x, int y, int z, BlockCallDelegateInterface<T, K> onImplementsInterface, BlockCallDelegateBlock<T> onIsMultiblock, BlockCallDelegateBlock<T> onOtherwise) where K : class
	{
		Block block = ba.GetBlock(x, y, z);
		K val = block as K;
		if (val == null)
		{
			val = block.GetBehavior(typeof(K), withInheritance: true) as K;
		}
		if (val != null)
		{
			return onImplementsInterface(val);
		}
		if (block is BlockMultiblock)
		{
			return onIsMultiblock(block);
		}
		return onOtherwise(block);
	}

	private void Handle<K>(IBlockAccessor ba, int x, int y, int z, BlockCallDelegateInterface<K> onImplementsInterface, BlockCallDelegateBlock onIsMultiblock, BlockCallDelegateBlock onOtherwise) where K : class
	{
		Block block = ba.GetBlock(x, y, z);
		K val = block as K;
		if (val == null)
		{
			val = block.GetBehavior(typeof(K), withInheritance: true) as K;
		}
		if (val != null)
		{
			onImplementsInterface(val);
		}
		else if (block is BlockMultiblock)
		{
			onIsMultiblock(block);
		}
		else
		{
			onOtherwise(block);
		}
	}

	public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs = null)
	{
		BlockSelection bsOffseted = blockSel.Clone();
		bsOffseted.Position.Add(OffsetInv);
		Handle(world.BlockAccessor, bsOffseted.Position.X, bsOffseted.Position.InternalY, bsOffseted.Position.Z, delegate(IMultiBlockActivate inf)
		{
			inf.MBActivate(world, caller, bsOffseted, activationArgs, OffsetInv);
		}, delegate
		{
			base.Activate(world, caller, bsOffseted, activationArgs);
		}, delegate(Block block)
		{
			block.Activate(world, caller, bsOffseted, activationArgs);
		});
	}

	public override BlockSounds GetSounds(IBlockAccessor ba, BlockSelection blockSel, ItemStack stack = null)
	{
		return Handle(ba, blockSel.Position.X + OffsetInv.X, blockSel.Position.InternalY + OffsetInv.Y, blockSel.Position.Z + OffsetInv.Z, (IMultiBlockInteract inf) => inf.MBGetSounds(ba, blockSel, stack, OffsetInv), (Block block) => base.GetSounds(ba, blockSel.AddPosCopy(OffsetInv), stack), (Block block) => block.GetSounds(ba, blockSel.AddPosCopy(OffsetInv), stack));
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor ba, BlockPos pos)
	{
		return Handle(ba, pos.X + OffsetInv.X, pos.InternalY + OffsetInv.Y, pos.Z + OffsetInv.Z, (IMultiBlockColSelBoxes inf) => inf.MBGetSelectionBoxes(ba, pos, OffsetInv), (Block block) => new Cuboidf[1] { Cuboidf.Default() }, (Block block) => (block.Id == 0) ? new Cuboidf[1] { Cuboidf.Default() } : block.GetSelectionBoxes(ba, pos.AddCopy(OffsetInv)));
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor ba, BlockPos pos)
	{
		return Handle(ba, pos.X + OffsetInv.X, pos.InternalY + OffsetInv.Y, pos.Z + OffsetInv.Z, (IMultiBlockColSelBoxes inf) => inf.MBGetCollisionBoxes(ba, pos, OffsetInv), (Block block) => new Cuboidf[1] { Cuboidf.Default() }, (Block block) => block.GetCollisionBoxes(ba, pos.AddCopy(OffsetInv)));
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return Handle(world.BlockAccessor, pos.X + OffsetInv.X, pos.InternalY + OffsetInv.Y, pos.Z + OffsetInv.Z, (IMultiBlockInteract inf) => inf.MBDoParticalSelection(world, pos, OffsetInv), (Block block) => base.DoParticalSelection(world, pos.AddCopy(OffsetInv)), (Block block) => block.DoParticalSelection(world, pos.AddCopy(OffsetInv)));
	}

	public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		BlockSelection bsOffseted = blockSel.Clone();
		bsOffseted.Position.Add(OffsetInv);
		return Handle(api.World.BlockAccessor, bsOffseted.Position.X, bsOffseted.Position.InternalY, bsOffseted.Position.Z, (IMultiBlockBlockBreaking inf) => inf.MBOnGettingBroken(player, blockSel, itemslot, remainingResistance, dt, counter, OffsetInv), (Block block) => base.OnGettingBroken(player, bsOffseted, itemslot, remainingResistance, dt, counter), delegate(Block block)
		{
			if (api is ICoreClientAPI coreClientAPI)
			{
				coreClientAPI.World.CloneBlockDamage(blockSel.Position, blockSel.Position.AddCopy(OffsetInv));
			}
			return block.OnGettingBroken(player, bsOffseted, itemslot, remainingResistance, dt, counter);
		});
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block block = world.BlockAccessor.GetBlock(pos.AddCopy(OffsetInv));
		if (block.Id == 0)
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
			return;
		}
		IMultiBlockBlockBreaking multiBlockBlockBreaking = block as IMultiBlockBlockBreaking;
		if (multiBlockBlockBreaking == null)
		{
			multiBlockBlockBreaking = block.GetBehavior(typeof(IMultiBlockBlockBreaking), withInheritance: true) as IMultiBlockBlockBreaking;
		}
		if (multiBlockBlockBreaking != null)
		{
			multiBlockBlockBreaking.MBOnBlockBroken(world, pos, OffsetInv, byPlayer);
		}
		else if (!(block is BlockMultiblock))
		{
			block.OnBlockBroken(world, pos.AddCopy(OffsetInv), byPlayer, dropQuantityMultiplier);
		}
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return Handle(world.BlockAccessor, pos.X + OffsetInv.X, pos.InternalY + OffsetInv.Y, pos.Z + OffsetInv.Z, (IMultiBlockInteract inf) => inf.MBOnPickBlock(world, pos, OffsetInv), (Block block) => base.OnPickBlock(world, pos.AddCopy(OffsetInv)), (Block block) => block.OnPickBlock(world, pos.AddCopy(OffsetInv)));
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockSelection bsOffseted = blockSel.Clone();
		bsOffseted.Position.Add(OffsetInv);
		return Handle(world.BlockAccessor, bsOffseted.Position.X, bsOffseted.Position.InternalY, bsOffseted.Position.Z, (IMultiBlockInteract inf) => inf.MBOnBlockInteractStart(world, byPlayer, blockSel, OffsetInv), (Block block) => base.OnBlockInteractStart(world, byPlayer, bsOffseted), (Block block) => block.OnBlockInteractStart(world, byPlayer, bsOffseted));
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockSelection bsOffseted = blockSel.Clone();
		bsOffseted.Position.Add(OffsetInv);
		return Handle(world.BlockAccessor, bsOffseted.Position.X, bsOffseted.Position.InternalY, bsOffseted.Position.Z, (IMultiBlockInteract inf) => inf.MBOnBlockInteractStep(secondsUsed, world, byPlayer, blockSel, OffsetInv), (Block block) => base.OnBlockInteractStep(secondsUsed, world, byPlayer, bsOffseted), (Block block) => block.OnBlockInteractStep(secondsUsed, world, byPlayer, bsOffseted));
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockSelection bsOffseted = blockSel.Clone();
		bsOffseted.Position.Add(OffsetInv);
		Handle(world.BlockAccessor, bsOffseted.Position.X, bsOffseted.Position.InternalY, bsOffseted.Position.Z, delegate(IMultiBlockInteract inf)
		{
			inf.MBOnBlockInteractStop(secondsUsed, world, byPlayer, blockSel, OffsetInv);
		}, delegate
		{
			base.OnBlockInteractStop(secondsUsed, world, byPlayer, bsOffseted);
		}, delegate(Block block)
		{
			block.OnBlockInteractStop(secondsUsed, world, byPlayer, bsOffseted);
		});
	}

	public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
	{
		BlockSelection bsOffseted = blockSel.Clone();
		bsOffseted.Position.Add(OffsetInv);
		return Handle(world.BlockAccessor, bsOffseted.Position.X, bsOffseted.Position.InternalY, bsOffseted.Position.Z, (IMultiBlockInteract inf) => inf.MBOnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason, OffsetInv), (Block block) => base.OnBlockInteractCancel(secondsUsed, world, byPlayer, bsOffseted, cancelReason), (Block block) => block.OnBlockInteractCancel(secondsUsed, world, byPlayer, bsOffseted, cancelReason));
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer)
	{
		BlockSelection bsOffseted = blockSel.Clone();
		bsOffseted.Position.Add(OffsetInv);
		return Handle(world.BlockAccessor, bsOffseted.Position.X, bsOffseted.Position.InternalY, bsOffseted.Position.Z, (IMultiBlockInteract inf) => inf.MBGetPlacedBlockInteractionHelp(world, blockSel, forPlayer, OffsetInv), (Block block) => base.GetPlacedBlockInteractionHelp(world, bsOffseted, forPlayer), (Block block) => block.GetPlacedBlockInteractionHelp(world, bsOffseted, forPlayer));
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		BlockPos pos2 = pos.AddCopy(OffsetInv);
		Block block = world.BlockAccessor.GetBlock(pos2);
		if (block is BlockMultiblock)
		{
			return "";
		}
		return block.GetPlacedBlockInfo(world, pos2, forPlayer);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		IClientWorldAccessor world = capi.World;
		return Handle(world.BlockAccessor, pos.X + OffsetInv.X, pos.InternalY + OffsetInv.Y, pos.Z + OffsetInv.Z, (IMultiBlockBlockBreaking inf) => inf.MBGetRandomColor(capi, pos, facing, rndIndex, OffsetInv), (Block block) => base.GetRandomColor(capi, pos, facing, rndIndex), (Block block) => block.GetRandomColor(capi, pos, facing, rndIndex));
	}

	public override int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
	{
		IClientWorldAccessor world = capi.World;
		return Handle(world.BlockAccessor, pos.X + OffsetInv.X, pos.InternalY + OffsetInv.Y, pos.Z + OffsetInv.Z, (IMultiBlockBlockBreaking inf) => inf.MBGetColorWithoutTint(capi, pos, OffsetInv), (Block block) => base.GetColorWithoutTint(capi, pos), (Block block) => block.GetColorWithoutTint(capi, pos));
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		return Handle(blockAccessor, pos.X + OffsetInv.X, pos.InternalY + OffsetInv.Y, pos.Z + OffsetInv.Z, (IMultiBlockBlockProperties inf) => inf.MBCanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea, OffsetInv), (Block nblock) => base.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea), (Block nblock) => nblock.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea));
	}

	public override JsonObject GetAttributes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return Handle(blockAccessor, pos.X + OffsetInv.X, pos.InternalY + OffsetInv.Y, pos.Z + OffsetInv.Z, (IMultiBlockBlockProperties inf) => inf.MBGetAttributes(blockAccessor, pos), (Block nblock) => base.GetAttributes(blockAccessor, pos), (Block nblock) => nblock.GetAttributes(blockAccessor, pos));
	}

	public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type)
	{
		IBlockAccessor blockAccessor = api.World.BlockAccessor;
		return Handle(blockAccessor, pos.X + OffsetInv.X, pos.InternalY + OffsetInv.Y, pos.Z + OffsetInv.Z, (IMultiBlockBlockProperties inf) => inf.MBGetRetention(pos, facing, type, OffsetInv), (Block nblock) => base.GetRetention(pos, facing, EnumRetentionType.Heat), (Block nblock) => nblock.GetRetention(pos, facing, EnumRetentionType.Heat));
	}

	public override float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos)
	{
		IBlockAccessor blockAccessor = api.World.BlockAccessor;
		return Handle(blockAccessor, pos.X + OffsetInv.X, pos.InternalY + OffsetInv.Y, pos.Z + OffsetInv.Z, (IMultiBlockBlockProperties inf) => inf.MBGetLiquidBarrierHeightOnSide(face, pos, OffsetInv), (Block nblock) => base.GetLiquidBarrierHeightOnSide(face, pos), (Block nblock) => nblock.GetLiquidBarrierHeightOnSide(face, pos));
	}

	public override T GetBlockEntity<T>(BlockPos position)
	{
		Block block = api.World.BlockAccessor.GetBlock(position.AddCopy(OffsetInv));
		if (block is BlockMultiblock)
		{
			return base.GetBlockEntity<T>(position);
		}
		return block.GetBlockEntity<T>(position.AddCopy(OffsetInv));
	}

	public override T GetBlockEntity<T>(BlockSelection blockSel)
	{
		Block block = api.World.BlockAccessor.GetBlock(blockSel.Position.AddCopy(OffsetInv));
		if (block is BlockMultiblock)
		{
			return base.GetBlockEntity<T>(blockSel);
		}
		BlockSelection blockSelection = blockSel.Clone();
		blockSelection.Position.Add(OffsetInv);
		return block.GetBlockEntity<T>(blockSelection);
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		Vec3i vec3i;
		switch ((angle / 90 % 4 + 4) % 4)
		{
		case 0:
			return Code;
		case 1:
			vec3i = new Vec3i(-Offset.Z, Offset.Y, Offset.X);
			break;
		case 2:
			vec3i = new Vec3i(-Offset.X, Offset.Y, -Offset.Z);
			break;
		case 3:
			vec3i = new Vec3i(Offset.Z, Offset.Y, -Offset.X);
			break;
		default:
			vec3i = null;
			break;
		}
		return new AssetLocation(Code.Domain, "multiblock-monolithic" + OffsetToString(vec3i.X) + OffsetToString(vec3i.Y) + OffsetToString(vec3i.Z));
	}

	private string OffsetToString(int x)
	{
		if (x == 0)
		{
			return "-0";
		}
		if (x < 0)
		{
			return "-n" + -x;
		}
		return "-p" + x;
	}

	public virtual BlockPos GetControlBlockPos(BlockPos pos)
	{
		return pos.AddCopy(OffsetInv);
	}
}
