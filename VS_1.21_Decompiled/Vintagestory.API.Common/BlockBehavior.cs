using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public abstract class BlockBehavior : CollectibleBehavior
{
	public Block block;

	public BlockBehavior(Block block)
		: base(block)
	{
		this.block = block;
	}

	public virtual void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
	}

	public virtual ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return null;
	}

	public virtual ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return null;
	}

	public virtual void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
	}

	public virtual bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handling, Cuboidi attachmentArea = null)
	{
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual bool CanCreatureSpawnOn(IBlockAccessor blockAccessor, BlockPos pos, EntityProperties type, BaseSpawnConditions sc, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return null;
	}

	public virtual AssetLocation GetVerticallyFlippedBlockCode(ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return null;
	}

	public virtual AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return null;
	}

	public virtual bool IsReplacableBy(Block block, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
	}

	public virtual void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
	}

	public virtual void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs, ref EnumHandling handled)
	{
	}

	public virtual bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		return "";
	}

	public virtual void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
	}

	public virtual WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return Array.Empty<WorldInteraction>();
	}

	public virtual void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
	}

	public virtual bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
	}

	public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe, ref EnumHandling handled)
	{
		handled = EnumHandling.PassThrough;
	}

	public virtual string GetHeldBlockInfo(IWorldAccessor world, ItemSlot inSlot)
	{
		return "";
	}

	public virtual AssetLocation GetSnowCoveredBlockCode(float snowLevel)
	{
		return null;
	}

	public virtual float GetMiningSpeedModifier(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		return 1f;
	}

	public virtual void GetPlacedBlockName(StringBuilder sb, IWorldAccessor world, BlockPos pos)
	{
	}

	[Obsolete("Use GetRetention() instead")]
	public virtual int GetHeatRetention(BlockPos pos, BlockFacing facing, ref EnumHandling handled)
	{
		return 0;
	}

	public virtual int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type, ref EnumHandling handled)
	{
		return 0;
	}

	public virtual float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos, ref EnumHandling handled)
	{
		return 0f;
	}
}
