using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class FruitTreeGrowingBranchBH : BlockEntityBehavior
{
	private int callbackTimeMs = 20000;

	public float VDrive;

	public float HDrive;

	private Block stemBlock;

	private BlockFruitTreeBranch branchBlock;

	private BlockFruitTreeFoliage leavesBlock;

	private long listenerId;

	private BlockEntityFruitTreeBranch ownBe => Blockentity as BlockEntityFruitTreeBranch;

	public FruitTreeGrowingBranchBH(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		if (Api.Side == EnumAppSide.Server)
		{
			listenerId = Blockentity.RegisterGameTickListener(OnTick, callbackTimeMs + Api.World.Rand.Next(callbackTimeMs));
		}
		stemBlock = Api.World.GetBlock(ownBe.Block.CodeWithVariant("type", "stem"));
		branchBlock = Api.World.GetBlock(ownBe.Block.CodeWithVariant("type", "branch")) as BlockFruitTreeBranch;
		leavesBlock = Api.World.GetBlock(AssetLocation.Create(ownBe.Block.Attributes["foliageBlock"].AsString(), ownBe.Block.Code.Domain)) as BlockFruitTreeFoliage;
		if (ownBe.Block == leavesBlock)
		{
			ownBe.PartType = EnumTreePartType.Leaves;
		}
		if (ownBe.Block == branchBlock)
		{
			ownBe.PartType = EnumTreePartType.Branch;
		}
		if (ownBe.lastGrowthAttemptTotalDays == 0.0)
		{
			ownBe.lastGrowthAttemptTotalDays = api.World.Calendar.TotalDays;
		}
	}

	protected void OnTick(float dt)
	{
		if (ownBe.RootOff == null)
		{
			return;
		}
		if (!(Api.World.BlockAccessor.GetBlockEntity(ownBe.Pos.AddCopy(ownBe.RootOff)) is BlockEntityFruitTreeBranch blockEntityFruitTreeBranch))
		{
			if (Api.World.Rand.NextDouble() < 0.25)
			{
				Api.World.BlockAccessor.BreakBlock(ownBe.Pos, null);
			}
			return;
		}
		double totalDays = Api.World.Calendar.TotalDays;
		if (ownBe.GrowTries > 60 || ownBe.FoliageState == EnumFoliageState.Dead)
		{
			ownBe.lastGrowthAttemptTotalDays = totalDays;
			return;
		}
		ownBe.lastGrowthAttemptTotalDays = Math.Max(ownBe.lastGrowthAttemptTotalDays, totalDays - (double)(Api.World.Calendar.DaysPerYear * 4));
		if (totalDays - ownBe.lastGrowthAttemptTotalDays < 0.5)
		{
			return;
		}
		double num = Api.World.Calendar.HoursPerDay;
		if (ownBe.TreeType == null)
		{
			Api.World.BlockAccessor.SetBlock(0, ownBe.Pos);
			return;
		}
		FruitTreeRootBH behavior = blockEntityFruitTreeBranch.GetBehavior<FruitTreeRootBH>();
		if (behavior == null || !behavior.propsByType.TryGetValue(ownBe.TreeType, out var value))
		{
			return;
		}
		if (ownBe.FoliageState == EnumFoliageState.Dead)
		{
			ownBe.UnregisterGameTickListener(listenerId);
			listenerId = 0L;
			return;
		}
		double num2 = value.GrowthStepDays;
		ClimateCondition climateAt = Api.World.BlockAccessor.GetClimateAt(ownBe.Pos, EnumGetClimateMode.WorldGenValues);
		if (climateAt == null)
		{
			return;
		}
		while (totalDays - ownBe.lastGrowthAttemptTotalDays > num2)
		{
			if (Api.World.BlockAccessor.GetClimateAt(ownBe.Pos, climateAt, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, (double)(int)ownBe.lastGrowthAttemptTotalDays + num / 2.0).Temperature < 12f)
			{
				ownBe.lastGrowthAttemptTotalDays += num2;
				continue;
			}
			TryGrow();
			ownBe.lastGrowthAttemptTotalDays += num2;
			ownBe.GrowTries++;
		}
	}

	public void OnNeighbourBranchRemoved(BlockFacing facing)
	{
		if ((ownBe.SideGrowth & (1 << facing.Index)) > 0)
		{
			ownBe.GrowTries = Math.Min(55, ownBe.GrowTries - 5);
			HDrive += 1f;
		}
	}

	private void TryGrow()
	{
		Random rand = Api.World.Rand;
		if (ownBe.TreeType == "" || ownBe.TreeType == null)
		{
			return;
		}
		switch (ownBe.PartType)
		{
		case EnumTreePartType.Stem:
		{
			Block block3 = Api.World.BlockAccessor.GetBlock(ownBe.Pos.UpCopy());
			if (block3.Id == 0)
			{
				TryGrowTo(EnumTreePartType.Leaves, BlockFacing.UP);
				ownBe.GrowTries /= 2;
				break;
			}
			if (block3 == leavesBlock)
			{
				TryGrowTo(EnumTreePartType.Branch, BlockFacing.UP);
				break;
			}
			BlockFacing[] hORIZONTALS = ((BlockFacing[])BlockFacing.ALLFACES.Clone()).Shuffle(Api.World.Rand);
			foreach (BlockFacing blockFacing in hORIZONTALS)
			{
				if ((ownBe.SideGrowth & (1 << blockFacing.Index)) > 0)
				{
					Block block4 = Api.World.BlockAccessor.GetBlock(ownBe.Pos.AddCopy(blockFacing));
					if (block4 == leavesBlock)
					{
						TryGrowTo(EnumTreePartType.Branch, blockFacing, 1, 1f);
					}
					else if (block4.Id == 0)
					{
						TryGrowTo(EnumTreePartType.Leaves, blockFacing);
					}
				}
			}
			break;
		}
		case EnumTreePartType.Cutting:
		{
			if (ownBe.FoliageState == EnumFoliageState.Dead || ownBe.GrowTries < 1)
			{
				break;
			}
			FruitTreeRootBH behavior = (Api.World.BlockAccessor.GetBlockEntity(ownBe.Pos.AddCopy(ownBe.RootOff)) as BlockEntityFruitTreeBranch).GetBehavior<FruitTreeRootBH>();
			if (behavior != null)
			{
				double num2 = Api.World.Rand.NextDouble();
				if ((ownBe.GrowthDir.IsVertical && (double)branchBlock.TypeProps[ownBe.TreeType].CuttingRootingChance >= num2) || (ownBe.GrowthDir.IsHorizontal && (double)branchBlock.TypeProps[ownBe.TreeType].CuttingGraftChance >= num2))
				{
					Api.World.BlockAccessor.ExchangeBlock(branchBlock.Id, ownBe.Pos);
					ownBe.GrowTries += 4;
					ownBe.PartType = EnumTreePartType.Branch;
					behavior.propsByType[ownBe.TreeType].State = EnumFruitTreeState.Young;
					TryGrowTo(EnumTreePartType.Leaves, ownBe.GrowthDir);
					ownBe.MarkDirty(redrawOnClient: true);
				}
				else
				{
					behavior.propsByType[ownBe.TreeType].State = EnumFruitTreeState.Dead;
					ownBe.FoliageState = EnumFoliageState.Dead;
					ownBe.MarkDirty(redrawOnClient: true);
				}
			}
			break;
		}
		case EnumTreePartType.Branch:
		{
			Block block = Api.World.BlockAccessor.GetBlock(ownBe.Pos.UpCopy());
			if (ownBe.GrowthDir == BlockFacing.UP)
			{
				if (ownBe.GrowTries > 5 && block == leavesBlock && VDrive > 0f)
				{
					TryGrowTo(EnumTreePartType.Branch, BlockFacing.UP);
					TryGrowTo(EnumTreePartType.Leaves, BlockFacing.UP, 2);
					break;
				}
				bool flag = ownBe.GrowTries > 20 && block == branchBlock && ownBe.Height < 3;
				bool flag2 = ownBe.GrowTries > 20 && block == branchBlock && ownBe.Height >= 3 && rand.NextDouble() < 0.05;
				if (flag || flag2)
				{
					if (flag)
					{
						Api.World.BlockAccessor.ExchangeBlock(stemBlock.Id, ownBe.Pos);
						ownBe.PartType = EnumTreePartType.Stem;
						ownBe.MarkDirty(redrawOnClient: true);
					}
					for (int i = 0; i < 4; i++)
					{
						BlockFacing facing = BlockFacing.HORIZONTALS[i];
						BlockPos blockPos = ownBe.Pos.AddCopy(facing);
						if (Api.World.BlockAccessor.GetBlock(blockPos) != leavesBlock)
						{
							continue;
						}
						if (ownBe.Height >= 2 && rand.NextDouble() < 0.6 && HDrive > 0f)
						{
							if (TryGrowTo(EnumTreePartType.Branch, facing))
							{
								ownBe.SideGrowth |= 1 << i;
								ownBe.MarkDirty(redrawOnClient: true);
								TryGrowTo(EnumTreePartType.Leaves, facing, 2);
							}
							continue;
						}
						bool flag3 = false;
						BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
						foreach (BlockFacing facing2 in hORIZONTALS)
						{
							flag3 |= Api.World.BlockAccessor.GetBlock(blockPos.AddCopy(facing2)) == branchBlock;
						}
						if (!flag3)
						{
							Api.World.BlockAccessor.SetBlock(0, blockPos);
						}
					}
				}
				else if (block.IsReplacableBy(leavesBlock))
				{
					TryGrowTo(EnumTreePartType.Leaves, BlockFacing.UP);
				}
				else if (ownBe.Height > 0)
				{
					BlockFacing facing3 = BlockFacing.HORIZONTALS[rand.Next(4)];
					TryGrowTo(EnumTreePartType.Leaves, facing3);
				}
				break;
			}
			if (rand.NextDouble() > 0.5)
			{
				BlockFacing growthDir = ownBe.GrowthDir;
				Block block2 = Api.World.BlockAccessor.GetBlock(ownBe.Pos.AddCopy(growthDir));
				TryGrowTo((block2 == leavesBlock && HDrive > 0f) ? EnumTreePartType.Branch : EnumTreePartType.Leaves, growthDir);
				break;
			}
			int num = 0;
			for (int k = 0; k < 5; k++)
			{
				BlockFacing facing4 = BlockFacing.ALLFACES[k];
				if (rand.NextDouble() < 0.4 && num < 2)
				{
					if (TryGrowTo(EnumTreePartType.Leaves, facing4))
					{
						ownBe.MarkDirty(redrawOnClient: true);
					}
					num++;
				}
			}
			break;
		}
		}
	}

	private bool TryGrowTo(EnumTreePartType partType, BlockFacing facing, int len = 1, float? hdrive = null)
	{
		BlockPos blockPos = ownBe.Pos.AddCopy(facing, len);
		Block block = stemBlock;
		if (partType == EnumTreePartType.Branch)
		{
			block = branchBlock;
		}
		if (partType == EnumTreePartType.Leaves)
		{
			block = leavesBlock;
		}
		Block block2 = Api.World.BlockAccessor.GetBlock(blockPos);
		if ((partType != EnumTreePartType.Leaves || !block2.IsReplacableBy(leavesBlock)) && (partType != EnumTreePartType.Branch || block2 != leavesBlock) && (partType != EnumTreePartType.Stem || block2 != branchBlock))
		{
			return false;
		}
		BlockPos blockPos2 = ownBe.Pos.AddCopy(ownBe.RootOff);
		if (!(Api.World.BlockAccessor.GetBlockEntity(blockPos2) is BlockEntityFruitTreeBranch blockEntityFruitTreeBranch))
		{
			return false;
		}
		FruitTreeRootBH behavior = blockEntityFruitTreeBranch.GetBehavior<FruitTreeRootBH>();
		if (behavior != null)
		{
			behavior.BlocksGrown++;
		}
		Api.World.BlockAccessor.SetBlock(block.Id, blockPos);
		BlockEntityFruitTreeBranch blockEntityFruitTreeBranch2 = Api.World.BlockAccessor.GetBlockEntity(blockPos) as BlockEntityFruitTreeBranch;
		FruitTreeGrowingBranchBH fruitTreeGrowingBranchBH = blockEntityFruitTreeBranch2?.GetBehavior<FruitTreeGrowingBranchBH>();
		if (fruitTreeGrowingBranchBH != null)
		{
			fruitTreeGrowingBranchBH.VDrive = VDrive - (float)(facing.IsVertical ? 1 : 0);
			float hDrive = ((!hdrive.HasValue) ? (HDrive - (float)(facing.IsHorizontal ? 1 : 0)) : hdrive.Value);
			fruitTreeGrowingBranchBH.HDrive = hDrive;
			blockEntityFruitTreeBranch2.ParentOff = facing.Normali.Clone();
			blockEntityFruitTreeBranch2.lastGrowthAttemptTotalDays = ownBe.lastGrowthAttemptTotalDays;
		}
		if (Api.World.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityFruitTreePart blockEntityFruitTreePart)
		{
			if (partType != EnumTreePartType.Stem)
			{
				blockEntityFruitTreePart.FoliageState = EnumFoliageState.Plain;
			}
			blockEntityFruitTreePart.GrowthDir = facing;
			blockEntityFruitTreePart.TreeType = ownBe.TreeType;
			blockEntityFruitTreePart.PartType = partType;
			blockEntityFruitTreePart.RootOff = (blockPos2 - blockPos).ToVec3i();
			blockEntityFruitTreePart.Height = ownBe.Height + facing.Normali.Y;
			blockEntityFruitTreePart.OnGrown();
		}
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (ownBe.PartType == EnumTreePartType.Cutting)
		{
			dsc.AppendLine((ownBe.FoliageState == EnumFoliageState.Dead) ? ("<font color=\"#ff8080\">" + Lang.Get("Dead tree cutting") + "</font>") : Lang.Get("Establishing tree cutting"));
			if (ownBe.FoliageState != EnumFoliageState.Dead && branchBlock.TypeProps.TryGetValue(ownBe.TreeType, out var value))
			{
				dsc.AppendLine(Lang.Get("{0}% survival chance", 100f * (ownBe.GrowthDir.IsVertical ? value.CuttingRootingChance : value.CuttingGraftChance)));
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		VDrive = tree.GetFloat("vdrive");
		HDrive = tree.GetFloat("hdrive");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("vdrive", VDrive);
		tree.SetFloat("hdrive", HDrive);
	}
}
