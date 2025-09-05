using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorFiniteSpreadingLiquid : BlockBehavior
{
	private const int MAXLEVEL = 7;

	private const float MAXLEVEL_float = 7f;

	public static Vec2i[] downPaths = ShapeUtil.GetSquarePointsSortedByMDist(3);

	public static SimpleParticleProperties steamParticles;

	public static int ReplacableThreshold = 5000;

	[DocumentAsJson]
	private AssetLocation collisionReplaceSound;

	[DocumentAsJson("Recommended", "150", false)]
	private int spreadDelay = 150;

	[DocumentAsJson("Optional", "None", false)]
	private string collidesWith;

	[DocumentAsJson("Optional", "None", false)]
	private AssetLocation liquidSourceCollisionReplacement;

	[DocumentAsJson("Optional", "None", false)]
	private AssetLocation liquidFlowingCollisionReplacement;

	public BlockBehaviorFiniteSpreadingLiquid(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		spreadDelay = properties["spreadDelay"].AsInt();
		collisionReplaceSound = CreateAssetLocation(properties, "sounds/", "liquidCollisionSound");
		liquidSourceCollisionReplacement = CreateAssetLocation(properties, "sourceReplacementCode");
		liquidFlowingCollisionReplacement = CreateAssetLocation(properties, "flowingReplacementCode");
		collidesWith = properties["collidesWith"]?.AsString();
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
	{
		if (world is IServerWorldAccessor)
		{
			world.RegisterCallbackUnique(OnDelayedWaterUpdateCheck, blockSel.Position, spreadDelay);
		}
		return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack, ref handling);
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (world is IServerWorldAccessor)
		{
			world.RegisterCallbackUnique(OnDelayedWaterUpdateCheck, pos, spreadDelay);
		}
	}

	private void OnDelayedWaterUpdateCheck(IWorldAccessor world, BlockPos pos, float dt)
	{
		SpreadAndUpdateLiquidLevels(world, pos);
		world.BulkBlockAccessor.Commit();
		Block block = world.BlockAccessor.GetBlock(pos, 2);
		if (block.HasBehavior<BlockBehaviorFiniteSpreadingLiquid>())
		{
			updateOwnFlowDir(block, world, pos);
		}
		BlockPos blockPos = pos.Copy();
		Cardinal[] aLL = Cardinal.ALL;
		foreach (Cardinal cardinal in aLL)
		{
			blockPos.Set(pos.X + cardinal.Normali.X, pos.Y, pos.Z + cardinal.Normali.Z);
			Block block2 = world.BlockAccessor.GetBlock(blockPos, 2);
			if (block2.HasBehavior<BlockBehaviorFiniteSpreadingLiquid>())
			{
				updateOwnFlowDir(block2, world, blockPos);
			}
		}
	}

	private void SpreadAndUpdateLiquidLevels(IWorldAccessor world, BlockPos pos)
	{
		Block block = world.BlockAccessor.GetBlock(pos, 2);
		int liquidLevel = block.LiquidLevel;
		if (liquidLevel <= 0 || TryLoweringLiquidLevel(block, world, pos))
		{
			return;
		}
		Block mostSolidBlock = world.BlockAccessor.GetMostSolidBlock(pos.DownCopy());
		Block block2 = world.BlockAccessor.GetBlock(pos, 1);
		if (((double)mostSolidBlock.GetLiquidBarrierHeightOnSide(BlockFacing.UP, pos.DownCopy()) != 1.0 && (double)block2.GetLiquidBarrierHeightOnSide(BlockFacing.DOWN, pos) != 1.0 && TrySpreadDownwards(world, block2, block, pos)) || liquidLevel <= 1)
		{
			return;
		}
		List<PosAndDist> list = FindDownwardPaths(world, pos, block);
		if (list.Count > 0)
		{
			FlowTowardDownwardPaths(list, block, block2, pos, world);
			return;
		}
		TrySpreadHorizontal(block, block2, world, pos);
		if (IsLiquidSourceBlock(block))
		{
			return;
		}
		int num = CountNearbySourceBlocks(world.BlockAccessor, pos, block);
		if (num < 3 && (num != 2 || CountNearbyDiagonalSources(world.BlockAccessor, pos, block) < 3))
		{
			return;
		}
		world.BlockAccessor.SetBlock(GetMoreLiquidBlockId(world, pos, block), pos, 2);
		BlockPos pos2 = pos.Copy();
		for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
		{
			BlockFacing.HORIZONTALS[i].IterateThruFacingOffsets(pos2);
			Block block3 = world.BlockAccessor.GetBlock(pos2, 2);
			if (block3.HasBehavior<BlockBehaviorFiniteSpreadingLiquid>())
			{
				updateOwnFlowDir(block3, world, pos2);
			}
		}
	}

	private int CountNearbySourceBlocks(IBlockAccessor blockAccessor, BlockPos pos, Block ourBlock)
	{
		BlockPos pos2 = pos.Copy();
		int num = 0;
		for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
		{
			BlockFacing.HORIZONTALS[i].IterateThruFacingOffsets(pos2);
			Block other = blockAccessor.GetBlock(pos2, 2);
			if (IsSameLiquid(ourBlock, other) && IsLiquidSourceBlock(other))
			{
				num++;
			}
		}
		return num;
	}

	private int CountNearbyDiagonalSources(IBlockAccessor blockAccessor, BlockPos pos, Block ourBlock)
	{
		BlockPos blockPos = pos.Copy();
		int num = 0;
		Cardinal[] aLL = Cardinal.ALL;
		foreach (Cardinal cardinal in aLL)
		{
			if (cardinal.IsDiagnoal)
			{
				blockPos.Set(pos.X + cardinal.Normali.X, pos.Y, pos.Z + cardinal.Normali.Z);
				Block other = blockAccessor.GetBlock(blockPos, 2);
				if (IsSameLiquid(ourBlock, other) && IsLiquidSourceBlock(other))
				{
					num++;
				}
			}
		}
		return num;
	}

	private void FlowTowardDownwardPaths(List<PosAndDist> downwardPaths, Block liquidBlock, Block solidBlock, BlockPos pos, IWorldAccessor world)
	{
		foreach (PosAndDist downwardPath in downwardPaths)
		{
			if (CanSpreadIntoBlock(liquidBlock, solidBlock, pos, downwardPath.pos, downwardPath.pos.FacingFrom(pos), world))
			{
				Block block = world.BlockAccessor.GetBlock(downwardPath.pos, 2);
				if (IsDifferentCollidableLiquid(liquidBlock, block))
				{
					ReplaceLiquidBlock(block, downwardPath.pos, world);
				}
				else
				{
					SpreadLiquid(GetLessLiquidBlockId(world, downwardPath.pos, liquidBlock), downwardPath.pos, world);
				}
			}
		}
	}

	private bool TrySpreadDownwards(IWorldAccessor world, Block ourSolid, Block ourBlock, BlockPos pos)
	{
		BlockPos blockPos = pos.DownCopy();
		Block block = world.BlockAccessor.GetBlock(blockPos, 2);
		if (CanSpreadIntoBlock(ourBlock, ourSolid, pos, blockPos, BlockFacing.DOWN, world))
		{
			if (IsDifferentCollidableLiquid(ourBlock, block))
			{
				ReplaceLiquidBlock(block, blockPos, world);
				TryFindSourceAndSpread(blockPos, world);
			}
			else
			{
				bool flag = false;
				if (IsLiquidSourceBlock(ourBlock))
				{
					if (CountNearbySourceBlocks(world.BlockAccessor, blockPos, ourBlock) > 1)
					{
						flag = true;
					}
					else
					{
						blockPos.Down();
						if ((double)world.BlockAccessor.GetBlock(blockPos, 4).GetLiquidBarrierHeightOnSide(BlockFacing.UP, blockPos) == 1.0 || (double)ourSolid.GetLiquidBarrierHeightOnSide(BlockFacing.DOWN, pos) == 1.0 || IsLiquidSourceBlock(world.BlockAccessor.GetBlock(blockPos, 2)))
						{
							flag = CountNearbySourceBlocks(world.BlockAccessor, pos, ourBlock) >= 2;
						}
						blockPos.Up();
					}
				}
				SpreadLiquid(flag ? ourBlock.BlockId : GetFallingLiquidBlockId(ourBlock, world), blockPos, world);
			}
			return true;
		}
		if (IsLiquidSourceBlock(ourBlock))
		{
			return !IsLiquidSourceBlock(block);
		}
		return true;
	}

	private void TrySpreadHorizontal(Block ourblock, Block ourSolid, IWorldAccessor world, BlockPos pos)
	{
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing facing in hORIZONTALS)
		{
			TrySpreadIntoBlock(ourblock, ourSolid, pos, pos.AddCopy(facing), facing, world);
		}
	}

	private void ReplaceLiquidBlock(Block liquidBlock, BlockPos pos, IWorldAccessor world)
	{
		Block replacementBlock = GetReplacementBlock(liquidBlock, world);
		if (replacementBlock != null)
		{
			world.BulkBlockAccessor.SetBlock(replacementBlock.BlockId, pos);
			BlockBehaviorBreakIfFloating behavior = replacementBlock.GetBehavior<BlockBehaviorBreakIfFloating>();
			if (behavior != null && behavior.IsSurroundedByNonSolid(world, pos))
			{
				world.BulkBlockAccessor.SetBlock(replacementBlock.BlockId, pos.DownCopy());
			}
			UpdateNeighbouringLiquids(pos, world);
			GenerateSteamParticles(pos, world);
			world.PlaySoundAt(collisionReplaceSound, pos, 0.0, null, randomizePitch: true, 16f);
		}
	}

	private void SpreadLiquid(int blockId, BlockPos pos, IWorldAccessor world)
	{
		world.BulkBlockAccessor.SetBlock(blockId, pos, 2);
		world.RegisterCallbackUnique(OnDelayedWaterUpdateCheck, pos, spreadDelay);
		Block ourBlock = world.GetBlock(blockId);
		TryReplaceNearbyLiquidBlocks(ourBlock, pos, world);
	}

	private void updateOwnFlowDir(Block block, IWorldAccessor world, BlockPos pos)
	{
		int liquidBlockId = GetLiquidBlockId(world, pos, block, block.LiquidLevel);
		if (block.BlockId != liquidBlockId)
		{
			world.BlockAccessor.SetBlock(liquidBlockId, pos, 2);
		}
	}

	private void TryReplaceNearbyLiquidBlocks(Block ourBlock, BlockPos pos, IWorldAccessor world)
	{
		BlockPos pos2 = pos.Copy();
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		for (int i = 0; i < hORIZONTALS.Length; i++)
		{
			hORIZONTALS[i].IterateThruFacingOffsets(pos2);
			Block other = world.BlockAccessor.GetBlock(pos2, 2);
			if (IsDifferentCollidableLiquid(ourBlock, other))
			{
				ReplaceLiquidBlock(ourBlock, pos2, world);
			}
		}
	}

	private bool TryFindSourceAndSpread(BlockPos startingPos, IWorldAccessor world)
	{
		BlockPos blockPos = startingPos.UpCopy();
		Block block = world.BlockAccessor.GetBlock(blockPos, 2);
		while (block.IsLiquid())
		{
			if (IsLiquidSourceBlock(block))
			{
				Block ourSolid = world.BlockAccessor.GetBlock(blockPos, 1);
				TrySpreadHorizontal(block, ourSolid, world, blockPos);
				return true;
			}
			blockPos.Add(0, 1, 0);
			block = world.BlockAccessor.GetBlock(blockPos, 2);
		}
		return false;
	}

	private void GenerateSteamParticles(BlockPos pos, IWorldAccessor world)
	{
		float maxQuantity = 100f;
		int color = ColorUtil.ToRgba(100, 225, 225, 225);
		Vec3d minPos = new Vec3d();
		Vec3d maxPos = new Vec3d();
		Vec3f minVelocity = new Vec3f(-0.25f, 0.1f, -0.25f);
		Vec3f maxVelocity = new Vec3f(0.25f, 0.1f, 0.25f);
		float lifeLength = 2f;
		float gravityEffect = -0.015f;
		float minSize = 0.1f;
		float maxSize = 0.1f;
		SimpleParticleProperties simpleParticleProperties = new SimpleParticleProperties(50f, maxQuantity, color, minPos, maxPos, minVelocity, maxVelocity, lifeLength, gravityEffect, minSize, maxSize, EnumParticleModel.Quad);
		simpleParticleProperties.Async = true;
		simpleParticleProperties.MinPos.Set(pos.ToVec3d().AddCopy(0.5, 1.1, 0.5));
		simpleParticleProperties.AddPos.Set(new Vec3d(0.5, 1.0, 0.5));
		simpleParticleProperties.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARINCREASE, 1f);
		world.SpawnParticles(simpleParticleProperties);
	}

	private void UpdateNeighbouringLiquids(BlockPos pos, IWorldAccessor world)
	{
		BlockPos blockPos = pos.DownCopy();
		if (world.BlockAccessor.GetBlock(blockPos, 2).HasBehavior<BlockBehaviorFiniteSpreadingLiquid>())
		{
			world.RegisterCallbackUnique(OnDelayedWaterUpdateCheck, blockPos.Copy(), spreadDelay);
		}
		blockPos.Up(2);
		if (world.BlockAccessor.GetBlock(blockPos, 2).HasBehavior<BlockBehaviorFiniteSpreadingLiquid>())
		{
			world.RegisterCallbackUnique(OnDelayedWaterUpdateCheck, blockPos.Copy(), spreadDelay);
		}
		blockPos.Down();
		Cardinal[] aLL = Cardinal.ALL;
		foreach (Cardinal cardinal in aLL)
		{
			blockPos.Set(pos.X + cardinal.Normali.X, pos.Y, pos.Z + cardinal.Normali.Z);
			if (world.BlockAccessor.GetBlock(blockPos, 2).HasBehavior<BlockBehaviorFiniteSpreadingLiquid>())
			{
				world.RegisterCallbackUnique(OnDelayedWaterUpdateCheck, blockPos.Copy(), spreadDelay);
			}
		}
	}

	private Block GetReplacementBlock(Block neighborBlock, IWorldAccessor world)
	{
		AssetLocation assetLocation = liquidFlowingCollisionReplacement;
		if (IsLiquidSourceBlock(neighborBlock))
		{
			assetLocation = liquidSourceCollisionReplacement;
		}
		if (!(assetLocation == null))
		{
			return world.GetBlock(assetLocation);
		}
		return null;
	}

	private bool IsDifferentCollidableLiquid(Block block, Block other)
	{
		if (other.IsLiquid() && block.IsLiquid())
		{
			return other.LiquidCode == collidesWith;
		}
		return false;
	}

	private bool IsSameLiquid(Block block, Block other)
	{
		return block.LiquidCode == other.LiquidCode;
	}

	private bool IsLiquidSourceBlock(Block block)
	{
		return block.LiquidLevel == 7;
	}

	private bool TryLoweringLiquidLevel(Block ourBlock, IWorldAccessor world, BlockPos pos)
	{
		if (!IsLiquidSourceBlock(ourBlock) && GetMaxNeighbourLiquidLevel(ourBlock, world, pos) <= ourBlock.LiquidLevel)
		{
			LowerLiquidLevelAndNotifyNeighbors(ourBlock, pos, world);
			return true;
		}
		return false;
	}

	private void LowerLiquidLevelAndNotifyNeighbors(Block block, BlockPos pos, IWorldAccessor world)
	{
		SpreadLiquid(GetLessLiquidBlockId(world, pos, block), pos, world);
		BlockPos pos2 = pos.Copy();
		for (int i = 0; i < 6; i++)
		{
			BlockFacing.ALLFACES[i].IterateThruFacingOffsets(pos2);
			Block block2 = world.BlockAccessor.GetBlock(pos2, 2);
			if (block2.BlockId != 0)
			{
				block2.OnNeighbourBlockChange(world, pos2, pos);
			}
		}
	}

	private void TrySpreadIntoBlock(Block ourblock, Block ourSolid, BlockPos pos, BlockPos npos, BlockFacing facing, IWorldAccessor world)
	{
		if (CanSpreadIntoBlock(ourblock, ourSolid, pos, npos, facing, world))
		{
			Block block = world.BlockAccessor.GetBlock(npos, 2);
			if (IsDifferentCollidableLiquid(ourblock, block))
			{
				ReplaceLiquidBlock(block, npos, world);
			}
			else
			{
				SpreadLiquid(GetLessLiquidBlockId(world, npos, ourblock), npos, world);
			}
		}
	}

	public int GetLessLiquidBlockId(IWorldAccessor world, BlockPos pos, Block block)
	{
		return GetLiquidBlockId(world, pos, block, block.LiquidLevel - 1);
	}

	public int GetMoreLiquidBlockId(IWorldAccessor world, BlockPos pos, Block block)
	{
		return GetLiquidBlockId(world, pos, block, Math.Min(7, block.LiquidLevel + 1));
	}

	public int GetLiquidBlockId(IWorldAccessor world, BlockPos pos, Block block, int liquidLevel)
	{
		if (liquidLevel < 1)
		{
			return 0;
		}
		Vec3i vec3i = new Vec3i();
		bool flag = false;
		BlockPos blockPos = pos.Copy();
		IBlockAccessor blockAccessor = world.BlockAccessor;
		Cardinal[] aLL = Cardinal.ALL;
		foreach (Cardinal cardinal in aLL)
		{
			blockPos.Set(pos.X + cardinal.Normali.X, pos.Y, pos.Z + cardinal.Normali.Z);
			Block block2 = blockAccessor.GetBlock(blockPos, 2);
			if (block2.LiquidLevel != liquidLevel && block2.Replaceable >= 6000 && block2.IsLiquid())
			{
				Vec3i vec3i2 = ((block2.LiquidLevel < liquidLevel) ? cardinal.Normali : cardinal.Opposite.Normali);
				if (!cardinal.IsDiagnoal)
				{
					block2 = blockAccessor.GetBlock(blockPos, 1);
					flag |= (double)block2.GetLiquidBarrierHeightOnSide(BlockFacing.ALLFACES[cardinal.Opposite.Index / 2], blockPos) != 1.0;
					block2 = blockAccessor.GetBlock(pos, 1);
					flag |= (double)block2.GetLiquidBarrierHeightOnSide(BlockFacing.ALLFACES[cardinal.Index / 2], pos) != 1.0;
				}
				vec3i.X += vec3i2.X;
				vec3i.Z += vec3i2.Z;
			}
		}
		if (Math.Abs(vec3i.X) > Math.Abs(vec3i.Z))
		{
			vec3i.Z = 0;
		}
		else if (Math.Abs(vec3i.Z) > Math.Abs(vec3i.X))
		{
			vec3i.X = 0;
		}
		vec3i.X = Math.Sign(vec3i.X);
		vec3i.Z = Math.Sign(vec3i.Z);
		Cardinal cardinal2 = Cardinal.FromNormali(vec3i);
		if (cardinal2 == null)
		{
			Block block3 = blockAccessor.GetBlock(pos.DownCopy(), 2);
			Block block4 = blockAccessor.GetBlock(pos.UpCopy(), 2);
			bool num = IsSameLiquid(block3, block);
			bool flag2 = IsSameLiquid(block4, block);
			if ((num && block3.Variant["flow"] == "d") || (flag2 && block4.Variant["flow"] == "d"))
			{
				return world.GetBlock(block.CodeWithParts("d", liquidLevel.ToString() ?? "")).BlockId;
			}
			if (flag)
			{
				return world.GetBlock(block.CodeWithParts("d", liquidLevel.ToString() ?? "")).BlockId;
			}
			return world.GetBlock(block.CodeWithParts("still", liquidLevel.ToString() ?? "")).BlockId;
		}
		return world.GetBlock(block.CodeWithParts(cardinal2.Initial, liquidLevel.ToString() ?? "")).BlockId;
	}

	private int GetFallingLiquidBlockId(Block ourBlock, IWorldAccessor world)
	{
		return world.GetBlock(ourBlock.CodeWithParts("d", "6")).BlockId;
	}

	public int GetMaxNeighbourLiquidLevel(Block ourblock, IWorldAccessor world, BlockPos pos)
	{
		Block block = world.BlockAccessor.GetBlock(pos, 1);
		BlockPos blockPos = pos.UpCopy();
		Block other = world.BlockAccessor.GetBlock(blockPos, 2);
		Block block2 = world.BlockAccessor.GetBlock(blockPos, 1);
		if (IsSameLiquid(ourblock, other) && (double)block.GetLiquidBarrierHeightOnSide(BlockFacing.UP, pos) == 0.0 && (double)block2.GetLiquidBarrierHeightOnSide(BlockFacing.DOWN, blockPos) == 0.0)
		{
			return 7;
		}
		int num = 0;
		blockPos.Down();
		for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
		{
			BlockFacing.HORIZONTALS[i].IterateThruFacingOffsets(blockPos);
			Block block3 = world.BlockAccessor.GetBlock(blockPos, 2);
			if (IsSameLiquid(ourblock, block3))
			{
				int liquidLevel = block3.LiquidLevel;
				if (!(block.GetLiquidBarrierHeightOnSide(BlockFacing.HORIZONTALS[i], pos) >= (float)liquidLevel / 7f) && !(world.BlockAccessor.GetBlock(blockPos, 1).GetLiquidBarrierHeightOnSide(BlockFacing.HORIZONTALS[i].Opposite, blockPos) >= (float)liquidLevel / 7f))
				{
					num = Math.Max(num, liquidLevel);
				}
			}
		}
		return num;
	}

	[Obsolete("Instead Use CanSpreadIntoBlock(Block, BlockPos, IWorldAccessor) to read from the liquid layer correctly, as well as the block layer")]
	public bool CanSpreadIntoBlock(Block ourblock, Block neighborBlock, IWorldAccessor world)
	{
		if (!IsSameLiquid(ourblock, neighborBlock) || neighborBlock.LiquidLevel >= ourblock.LiquidLevel)
		{
			if (!IsSameLiquid(ourblock, neighborBlock))
			{
				return neighborBlock.Replaceable >= ReplacableThreshold;
			}
			return false;
		}
		return true;
	}

	public bool CanSpreadIntoBlock(Block ourblock, Block ourSolid, BlockPos pos, BlockPos npos, BlockFacing facing, IWorldAccessor world)
	{
		if (ourSolid.GetLiquidBarrierHeightOnSide(facing, pos) >= (float)ourblock.LiquidLevel / 7f)
		{
			return false;
		}
		if (world.BlockAccessor.GetBlock(npos, 1).GetLiquidBarrierHeightOnSide(facing.Opposite, npos) >= (float)ourblock.LiquidLevel / 7f)
		{
			return false;
		}
		Block block = world.BlockAccessor.GetBlock(npos, 2);
		if (IsSameLiquid(ourblock, block))
		{
			return block.LiquidLevel < ourblock.LiquidLevel;
		}
		if (block.LiquidLevel == 7 && !IsDifferentCollidableLiquid(ourblock, block))
		{
			return false;
		}
		if (block.BlockId != 0)
		{
			return block.Replaceable >= ourblock.Replaceable;
		}
		if (ourblock.LiquidLevel <= 1)
		{
			return facing == BlockFacing.DOWN;
		}
		return true;
	}

	public override bool IsReplacableBy(Block byBlock, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (block.IsLiquid() || block.Replaceable >= ReplacableThreshold)
		{
			return byBlock.Replaceable <= block.Replaceable;
		}
		return false;
	}

	public List<PosAndDist> FindDownwardPaths(IWorldAccessor world, BlockPos pos, Block ourBlock)
	{
		List<PosAndDist> list = new List<PosAndDist>();
		Queue<BlockPos> queue = new Queue<BlockPos>();
		int num = 99;
		BlockPos blockPos = new BlockPos(pos.dimension);
		for (int i = 0; i < downPaths.Length; i++)
		{
			Vec2i vec2i = downPaths[i];
			blockPos.Set(pos.X + vec2i.X, pos.Y - 1, pos.Z + vec2i.Y);
			Block block = world.BlockAccessor.GetBlock(blockPos);
			blockPos.Y++;
			Block obj = world.BlockAccessor.GetBlock(blockPos, 2);
			Block block2 = world.BlockAccessor.GetBlock(blockPos, 1);
			if (obj.LiquidLevel >= ourBlock.LiquidLevel || block.Replaceable < ReplacableThreshold || block2.Replaceable < ReplacableThreshold)
			{
				continue;
			}
			queue.Enqueue(new BlockPos(pos.X + vec2i.X, pos.Y, pos.Z + vec2i.Y, pos.dimension));
			BlockPos blockPos2 = BfsSearchPath(world, queue, pos, ourBlock);
			if (blockPos2 != null)
			{
				PosAndDist posAndDist = new PosAndDist
				{
					pos = blockPos2,
					dist = pos.ManhattenDistance(pos.X + vec2i.X, pos.Y, pos.Z + vec2i.Y)
				};
				if (posAndDist.dist == 1 && ourBlock.LiquidLevel < 7)
				{
					list.Clear();
					list.Add(posAndDist);
					return list;
				}
				list.Add(posAndDist);
				num = Math.Min(num, posAndDist.dist);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			if (list[j].dist > num)
			{
				list.RemoveAt(j);
				j--;
			}
		}
		return list;
	}

	private BlockPos BfsSearchPath(IWorldAccessor world, Queue<BlockPos> uncheckedPositions, BlockPos target, Block ourBlock)
	{
		BlockPos blockPos = new BlockPos(target.dimension);
		BlockPos blockPos2 = null;
		while (uncheckedPositions.Count > 0)
		{
			BlockPos blockPos3 = uncheckedPositions.Dequeue();
			if (blockPos2 == null)
			{
				blockPos2 = blockPos3;
			}
			int num = blockPos3.ManhattenDistance(target);
			blockPos.Set(blockPos3);
			for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
			{
				BlockFacing.HORIZONTALS[i].IterateThruFacingOffsets(blockPos);
				if (blockPos.ManhattenDistance(target) <= num)
				{
					if (blockPos.Equals(target))
					{
						return blockPos3;
					}
					if (!(world.BlockAccessor.GetMostSolidBlock(blockPos).GetLiquidBarrierHeightOnSide(BlockFacing.HORIZONTALS[i].Opposite, blockPos) >= (float)(ourBlock.LiquidLevel - blockPos3.ManhattenDistance(blockPos2)) / 7f))
					{
						uncheckedPositions.Enqueue(blockPos.Copy());
					}
				}
			}
		}
		return null;
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (block.ParticleProperties == null || block.ParticleProperties.Length == 0)
		{
			return false;
		}
		if (block.LiquidCode == "lava")
		{
			return world.BlockAccessor.GetBlockAbove(pos).Replaceable > ReplacableThreshold;
		}
		handled = EnumHandling.PassThrough;
		return false;
	}

	private static AssetLocation CreateAssetLocation(JsonObject properties, string propertyName)
	{
		return CreateAssetLocation(properties, null, propertyName);
	}

	private static AssetLocation CreateAssetLocation(JsonObject properties, string prefix, string propertyName)
	{
		string text = properties[propertyName]?.AsString();
		if (text == null)
		{
			return null;
		}
		if (prefix != null)
		{
			return new AssetLocation(prefix + text);
		}
		return new AssetLocation(text);
	}
}
