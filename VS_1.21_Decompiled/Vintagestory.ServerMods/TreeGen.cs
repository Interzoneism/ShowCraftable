using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class TreeGen : ITreeGenerator
{
	private IBlockAccessor blockAccessor;

	private TreeGenParams treeGenParams;

	private float size;

	private List<TreeGenBranch> branchesByDepth = new List<TreeGenBranch>();

	private TreeGenConfig config;

	private readonly ForestFloorSystem forestFloor;

	public TreeGen(TreeGenConfig config, int seed, ForestFloorSystem ffs)
	{
		this.config = config;
		forestFloor = ffs;
	}

	public void GrowTree(IBlockAccessor ba, BlockPos pos, TreeGenParams treeGenParams, IRandom random)
	{
		int treeSubType = random.NextInt(8);
		blockAccessor = ba;
		this.treeGenParams = treeGenParams;
		size = treeGenParams.size * config.sizeMultiplier + config.sizeVar.nextFloat(1f, random);
		pos.Up(config.yOffset);
		TreeGenTrunk[] trunks = config.trunks;
		branchesByDepth.Clear();
		branchesByDepth.Add(null);
		branchesByDepth.AddRange(config.branches);
		forestFloor.ClearOutline();
		TreeGenTrunk treeGenTrunk = config.trunks[0];
		float dieAt = Math.Max(0f, treeGenTrunk.dieAt.nextFloat(1f, random));
		float trunkWidthLoss = treeGenTrunk.WidthLoss(random);
		for (int i = 0; i < trunks.Length; i++)
		{
			treeGenTrunk = config.trunks[i];
			if (random.NextDouble() <= (double)treeGenTrunk.probability)
			{
				branchesByDepth[0] = treeGenTrunk;
				growBranch(random, 0, pos, treeSubType, treeGenTrunk.dx, 0f, treeGenTrunk.dz, treeGenTrunk.angleVert.nextFloat(1f, random), treeGenTrunk.angleHori.nextFloat(1f, random), size * treeGenTrunk.widthMultiplier, dieAt, trunkWidthLoss, trunks.Length > 1);
			}
		}
		if (!treeGenParams.skipForestFloor)
		{
			forestFloor.CreateForestFloor(ba, config, pos, random, treeGenParams.treesInChunkGenerated);
		}
	}

	private void growBranch(IRandom rand, int depth, BlockPos basePos, int treeSubType, float dx, float dy, float dz, float angleVerStart, float angleHorStart, float curWidth, float dieAt, float trunkWidthLoss, bool wideTrunk)
	{
		if (depth > 30)
		{
			Console.WriteLine("TreeGen.growBranch() aborted, too many branches!");
			return;
		}
		TreeGenBranch treeGenBranch = branchesByDepth[Math.Min(depth, branchesByDepth.Count - 1)];
		float num = ((depth == 0) ? trunkWidthLoss : treeGenBranch.WidthLoss(rand));
		float widthlossCurve = treeGenBranch.widthlossCurve;
		float num2 = treeGenBranch.branchSpacing.nextFloat(1f, rand);
		float num3 = treeGenBranch.branchStart.nextFloat(1f, rand);
		float firstvalue = treeGenBranch.branchQuantity.nextFloat(1f, rand);
		float branchWidthMulitplierStart = treeGenBranch.branchWidthMultiplier.nextFloat(1f, rand);
		float num4 = 0f;
		float num5 = curWidth / num;
		int num6 = 0;
		float num7 = 1f / (curWidth / num);
		BlockPos blockPos = new BlockPos(basePos.dimension);
		while (curWidth > 0f && num6++ < 5000)
		{
			curWidth -= num;
			if (widthlossCurve + curWidth / 20f < 1f)
			{
				num *= widthlossCurve + curWidth / 20f;
			}
			float num8 = num7 * (float)(num6 - 1);
			if (curWidth < dieAt)
			{
				break;
			}
			float rad = treeGenBranch.angleVertEvolve.nextFloat(angleVerStart, num8);
			float num9 = treeGenBranch.angleHoriEvolve.nextFloat(angleHorStart, num8);
			float num10 = GameMath.FastSin(rad);
			float num11 = GameMath.FastCos(num9);
			float num12 = GameMath.FastSin(num9);
			float num13 = Math.Max(-0.5f, Math.Min(0.5f, 0.7f * num10 * num11));
			float num14 = Math.Max(-0.5f, Math.Min(0.5f, 0.7f * num10 * num12));
			float num15 = treeGenBranch.gravityDrag * (float)Math.Sqrt(dx * dx + dz * dz);
			dx += num10 * num11 / Math.Max(1f, Math.Abs(num15));
			dy += Math.Min(1f, Math.Max(-1f, GameMath.FastCos(rad) - num15));
			dz += num10 * num12 / Math.Max(1f, Math.Abs(num15));
			int blockId = treeGenBranch.getBlockId(rand, curWidth, config.treeBlocks, this, treeSubType);
			if (blockId == 0)
			{
				break;
			}
			blockPos.Set((float)basePos.X + dx, (float)basePos.Y + dy, (float)basePos.Z + dz);
			switch (getPlaceResumeState(blockPos, blockId, wideTrunk))
			{
			case PlaceResumeState.CanPlace:
				PlaceBlockEtc(blockId, blockPos, rand, dx, dz);
				break;
			case PlaceResumeState.Stop:
				return;
			}
			float num16 = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz) / num5;
			if (!(num16 < num3) && num16 > num4 + num2 * (1f - num16))
			{
				num2 = treeGenBranch.branchSpacing.nextFloat(1f, rand);
				num4 = num16;
				float num17 = ((treeGenBranch.branchQuantityEvolve != null) ? treeGenBranch.branchQuantityEvolve.nextFloat(firstvalue, num8) : treeGenBranch.branchQuantity.nextFloat(1f, rand));
				if (rand.NextDouble() < (double)num17 % 1.0)
				{
					num17 += 1f;
				}
				curWidth = GrowBranchesHere((int)num17, treeGenBranch, depth + 1, rand, curWidth, branchWidthMulitplierStart, num8, num9, dx + num13, dy, dz + num14, basePos, treeSubType, trunkWidthLoss);
			}
		}
	}

	private float GrowBranchesHere(int branchQuantity, TreeGenBranch branch, int newDepth, IRandom rand, float curWidth, float branchWidthMulitplierStart, float currentSequence, float angleHor, float dx, float dy, float dz, BlockPos basePos, int treeSubType, float trunkWidthLoss)
	{
		float num = 0f;
		float num2 = Math.Min((float)Math.PI / 5f, branch.branchHorizontalAngle.var / 5f);
		bool flag = true;
		while (branchQuantity-- > 0)
		{
			curWidth *= branch.branchWidthLossMul;
			float num3 = angleHor + branch.branchHorizontalAngle.nextFloat(1f, rand);
			int num4 = 10;
			while (!flag && Math.Abs(num3 - num) < num2 && num4-- > 0)
			{
				float num5 = angleHor + branch.branchHorizontalAngle.nextFloat(1f, rand);
				if (Math.Abs(num3 - num) < Math.Abs(num5 - num))
				{
					num3 = num5;
				}
			}
			growBranch(curWidth: (branch.branchWidthMultiplierEvolve == null) ? branch.branchWidthMultiplier.nextFloat(curWidth, rand) : (curWidth * branch.branchWidthMultiplierEvolve.nextFloat(branchWidthMulitplierStart, currentSequence)), rand: rand, depth: newDepth, basePos: basePos, treeSubType: treeSubType, dx: dx, dy: dy, dz: dz, angleVerStart: branch.branchVerticalAngle.nextFloat(1f, rand), angleHorStart: num3, dieAt: Math.Max(0f, branch.dieAt.nextFloat(1f, rand)), trunkWidthLoss: trunkWidthLoss, wideTrunk: false);
			flag = false;
			num = angleHor + num3;
		}
		return curWidth;
	}

	private void PlaceBlockEtc(int blockId, BlockPos currentPos, IRandom rand, float dx, float dz)
	{
		blockAccessor.SetBlock(blockId, currentPos);
		if (blockAccessor.GetBlock(blockId).BlockMaterial == EnumBlockMaterial.Wood && treeGenParams.mossGrowthChance > 0f && config.treeBlocks.mossDecorBlock != null)
		{
			double num = rand.NextDouble();
			int num2 = ((treeGenParams.hemisphere != EnumHemisphere.North) ? 2 : 0);
			int num3 = 2;
			while (num3 >= 0 && !(num > (double)(treeGenParams.mossGrowthChance * (float)num3)))
			{
				BlockFacing blockFacing = BlockFacing.HORIZONTALS[num2 % 4];
				if (!blockAccessor.GetBlockOnSide(currentPos, blockFacing).SideSolid[blockFacing.Opposite.Index])
				{
					blockAccessor.SetDecor(config.treeBlocks.mossDecorBlock, currentPos, blockFacing);
				}
				num2 += rand.NextInt(4);
				num3--;
			}
		}
		int num4 = (int)(dz + 16f);
		int num5 = (int)(dx + 16f);
		if (num4 > 1 && num4 < 31 && num5 > 1 && num5 < 31)
		{
			short[] outline = forestFloor.GetOutline();
			int num6 = num4 * 33 + num5;
			outline[num6 - 66 - 2]++;
			outline[num6 - 66 - 1]++;
			outline[num6 - 66]++;
			outline[num6 - 66 + 1]++;
			outline[num6 - 66 + 2]++;
			outline[num6 - 33 - 2]++;
			outline[num6 - 33 - 1] += 2;
			outline[num6 - 33] += 2;
			outline[num6 - 33 + 1] += 2;
			outline[num6 - 33 + 2]++;
			outline[num6 - 2]++;
			outline[num6 - 1] += 2;
			outline[num6] += 3;
			outline[num6 + 1] += 2;
			outline[num6 + 2]++;
			outline[num6 + 33]++;
		}
		if (!(treeGenParams.vinesGrowthChance > 0f) || !(rand.NextDouble() < (double)treeGenParams.vinesGrowthChance) || config.treeBlocks.vinesBlock == null)
		{
			return;
		}
		BlockFacing blockFacing2 = BlockFacing.HORIZONTALS[rand.NextInt(4)];
		BlockPos blockPos = currentPos.AddCopy(blockFacing2);
		float num7 = 1f + (float)rand.NextInt(11) * (treeGenParams.vinesGrowthChance + 0.2f);
		while (blockAccessor.GetBlockId(blockPos) == 0 && num7-- > 0f)
		{
			Block block = config.treeBlocks.vinesBlock;
			if (num7 <= 0f && config.treeBlocks.vinesEndBlock != null)
			{
				block = config.treeBlocks.vinesEndBlock;
			}
			block.TryPlaceBlockForWorldGen(blockAccessor, blockPos, blockFacing2, rand);
			blockPos.Down();
		}
	}

	internal bool TriggerRandomOtherBlock(IRandom lcgRandom)
	{
		return lcgRandom.NextDouble() < (double)treeGenParams.otherBlockChance * config.treeBlocks.otherLogChance;
	}

	private PlaceResumeState getPlaceResumeState(BlockPos targetPos, int desiredblockId, bool wideTrunk)
	{
		if (targetPos.X < 0 || targetPos.Y < 0 || targetPos.Z < 0 || targetPos.X >= blockAccessor.MapSizeX || targetPos.Y >= blockAccessor.MapSizeY || targetPos.Z >= blockAccessor.MapSizeZ)
		{
			return PlaceResumeState.Stop;
		}
		int blockId = blockAccessor.GetBlockId(targetPos);
		switch (blockId)
		{
		case -1:
			return PlaceResumeState.CannotPlace;
		case 0:
			return PlaceResumeState.CanPlace;
		default:
		{
			Block block = blockAccessor.GetBlock(blockId);
			Block block2 = blockAccessor.GetBlock(desiredblockId);
			if ((block.Fertility == 0 || block2.BlockMaterial != EnumBlockMaterial.Wood) && block.BlockMaterial != EnumBlockMaterial.Leaves && block.Replaceable < 6000 && !wideTrunk && !config.treeBlocks.blockIds.Contains(block.BlockId))
			{
				return PlaceResumeState.Stop;
			}
			if (block2.Replaceable <= block.Replaceable)
			{
				return PlaceResumeState.CanPlace;
			}
			return PlaceResumeState.CannotPlace;
		}
		}
	}
}
