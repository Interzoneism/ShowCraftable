using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class FruitTreeRootBH : BlockEntityBehavior
{
	public int BlocksGrown;

	public int BlocksRemoved;

	public double TreePlantedTotalDays;

	public double LastRootTickTotalDays;

	public Dictionary<string, FruitTreeProperties> propsByType = new Dictionary<string, FruitTreeProperties>();

	private RoomRegistry roomreg;

	private ItemStack parentPlantStack;

	private BlockFruitTreeBranch blockBranch;

	private double stateUpdateIntervalDays = 1.0 / 3.0;

	public double nonFloweringYoungDays = 30.0;

	private float greenhouseTempBonus;

	private BlockEntity be => Blockentity;

	private BlockEntityFruitTreeBranch bebr => be as BlockEntityFruitTreeBranch;

	public bool IsYoung => Api?.World.Calendar.TotalDays - TreePlantedTotalDays < nonFloweringYoungDays;

	public FruitTreeRootBH(BlockEntity blockentity, ItemStack parentPlantStack)
		: base(blockentity)
	{
		this.parentPlantStack = parentPlantStack;
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		if (api.Side == EnumAppSide.Server && Api.World.Config.GetBool("processCrops", defaultValue: true))
		{
			Blockentity.RegisterGameTickListener(onRootTick, 5000, api.World.Rand.Next(5000));
		}
		roomreg = api.ModLoader.GetModSystem<RoomRegistry>();
		blockBranch = be.Block as BlockFruitTreeBranch;
		RegisterTreeType(bebr.TreeType);
		double totalDays = api.World.Calendar.TotalDays;
		if (TreePlantedTotalDays == 0.0)
		{
			TreePlantedTotalDays = totalDays;
			LastRootTickTotalDays = totalDays;
		}
		else
		{
			TreePlantedTotalDays = Math.Min(TreePlantedTotalDays, totalDays);
			LastRootTickTotalDays = Math.Min(LastRootTickTotalDays, totalDays);
		}
	}

	public void RegisterTreeType(string treeType)
	{
		if (treeType != null && !propsByType.ContainsKey(treeType))
		{
			if (!blockBranch.TypeProps.TryGetValue(bebr.TreeType, out var value))
			{
				Api.Logger.Error("Missing fruitTreeProperties for dynamic tree of type '" + bebr.TreeType + "', will use default values.");
				value = new FruitTreeTypeProperties();
			}
			Random rand = Api.World.Rand;
			FruitTreeProperties fruitTreeProperties = (propsByType[treeType] = new FruitTreeProperties
			{
				EnterDormancyTemp = value.EnterDormancyTemp.nextFloat(1f, rand),
				LeaveDormancyTemp = value.LeaveDormancyTemp.nextFloat(1f, rand),
				FloweringDays = value.FloweringDays.nextFloat(1f, rand),
				FruitingDays = value.FruitingDays.nextFloat(1f, rand),
				RipeDays = value.RipeDays.nextFloat(1f, rand),
				GrowthStepDays = value.GrowthStepDays.nextFloat(1f, rand),
				DieBelowTemp = value.DieBelowTemp.nextFloat(1f, rand),
				FruitStacks = value.FruitStacks,
				CycleType = value.CycleType,
				VernalizationHours = (value.VernalizationHours?.nextFloat(1f, rand) ?? 0f),
				VernalizationTemp = (value.VernalizationTemp?.nextFloat(1f, rand) ?? 0f),
				BlossomAtYearRel = (value.BlossomAtYearRel?.nextFloat(1f, rand) ?? 0f),
				LooseLeavesBelowTemp = (value.LooseLeavesBelowTemp?.nextFloat(1f, rand) ?? 0f),
				RootSizeMul = (value.RootSizeMul?.nextFloat(1f, rand) ?? 0f)
			});
			FruitTreeProperties fruitTreeProperties3 = fruitTreeProperties;
			if (parentPlantStack != null)
			{
				fruitTreeProperties3.EnterDormancyTemp = value.EnterDormancyTemp.ClampToRange((fruitTreeProperties3.EnterDormancyTemp + parentPlantStack.Attributes?.GetFloat("enterDormancyTempDiff")).GetValueOrDefault());
				fruitTreeProperties3.LeaveDormancyTemp = value.LeaveDormancyTemp.ClampToRange((fruitTreeProperties3.LeaveDormancyTemp + parentPlantStack.Attributes?.GetFloat("leaveDormancyTempDiff")).GetValueOrDefault());
				fruitTreeProperties3.FloweringDays = value.FloweringDays.ClampToRange((fruitTreeProperties3.FloweringDays + parentPlantStack.Attributes?.GetFloat("floweringDaysDiff")).GetValueOrDefault());
				fruitTreeProperties3.FruitingDays = value.FruitingDays.ClampToRange((fruitTreeProperties3.FruitingDays + parentPlantStack.Attributes?.GetFloat("fruitingDaysDiff")).GetValueOrDefault());
				fruitTreeProperties3.RipeDays = value.RipeDays.ClampToRange((fruitTreeProperties3.RipeDays + parentPlantStack.Attributes?.GetFloat("ripeDaysDiff")).GetValueOrDefault());
				fruitTreeProperties3.GrowthStepDays = value.GrowthStepDays.ClampToRange((fruitTreeProperties3.GrowthStepDays + parentPlantStack.Attributes?.GetFloat("growthStepDaysDiff")).GetValueOrDefault());
				fruitTreeProperties3.DieBelowTemp = value.DieBelowTemp.ClampToRange((fruitTreeProperties3.DieBelowTemp + parentPlantStack.Attributes?.GetFloat("dieBelowTempDiff")).GetValueOrDefault());
				fruitTreeProperties3.VernalizationHours = value.VernalizationHours.ClampToRange((fruitTreeProperties3.VernalizationHours + parentPlantStack.Attributes?.GetFloat("vernalizationHoursDiff")).GetValueOrDefault());
				fruitTreeProperties3.VernalizationTemp = value.VernalizationTemp.ClampToRange((fruitTreeProperties3.VernalizationTemp + parentPlantStack.Attributes?.GetFloat("vernalizationTempDiff")).GetValueOrDefault());
				fruitTreeProperties3.BlossomAtYearRel = value.BlossomAtYearRel.ClampToRange((fruitTreeProperties3.BlossomAtYearRel + parentPlantStack.Attributes?.GetFloat("blossomAtYearRelDiff")).GetValueOrDefault());
				fruitTreeProperties3.LooseLeavesBelowTemp = value.LooseLeavesBelowTemp.ClampToRange((fruitTreeProperties3.LooseLeavesBelowTemp + parentPlantStack.Attributes?.GetFloat("looseLeavesBelowTempDiff")).GetValueOrDefault());
				fruitTreeProperties3.RootSizeMul = value.RootSizeMul.ClampToRange((fruitTreeProperties3.RootSizeMul + parentPlantStack.Attributes?.GetFloat("rootSizeMulDiff")).GetValueOrDefault());
			}
		}
	}

	private void onRootTick(float dt)
	{
		double totalDays = Api.World.Calendar.TotalDays;
		if (totalDays - LastRootTickTotalDays < stateUpdateIntervalDays)
		{
			return;
		}
		int num = -99;
		float num2 = 0f;
		bool flag = false;
		ClimateCondition climateAt = Api.World.BlockAccessor.GetClimateAt(be.Pos, EnumGetClimateMode.WorldGenValues);
		if (climateAt == null)
		{
			return;
		}
		greenhouseTempBonus = getGreenhouseTempBonus();
		foreach (FruitTreeProperties value2 in propsByType.Values)
		{
			value2.workingState = value2.State;
		}
		while (totalDays - LastRootTickTotalDays >= stateUpdateIntervalDays)
		{
			int num3 = (int)LastRootTickTotalDays;
			foreach (KeyValuePair<string, FruitTreeProperties> item in propsByType)
			{
				FruitTreeProperties value = item.Value;
				if (value.workingState == EnumFruitTreeState.Dead)
				{
					continue;
				}
				if (num != num3)
				{
					double totalDays2 = (double)num3 + 0.5;
					num2 = Api.World.BlockAccessor.GetClimateAt(be.Pos, climateAt, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, totalDays2).Temperature;
					num2 = applyGreenhouseTempBonus(num2);
					num = num3;
				}
				if (value.DieBelowTemp > num2 + (float)((value.workingState == EnumFruitTreeState.Dormant) ? 3 : 0))
				{
					value.workingState = EnumFruitTreeState.Dead;
					value.lastStateChangeTotalDays = Api.World.Calendar.TotalDays;
					flag = true;
					break;
				}
				switch (value.workingState)
				{
				case EnumFruitTreeState.Young:
					if (value.CycleType == EnumTreeCycleType.Evergreen)
					{
						if (LastRootTickTotalDays - TreePlantedTotalDays < nonFloweringYoungDays)
						{
							continue;
						}
						if (Math.Abs(LastRootTickTotalDays / (double)Api.World.Calendar.DaysPerYear % 1.0 - (double)value.BlossomAtYearRel) < 0.125)
						{
							value.workingState = EnumFruitTreeState.Flowering;
							value.lastStateChangeTotalDays = LastRootTickTotalDays;
							flag = true;
						}
					}
					else if (value.CycleType == EnumTreeCycleType.Deciduous && num2 < value.EnterDormancyTemp)
					{
						value.workingState = EnumFruitTreeState.EnterDormancy;
						value.lastStateChangeTotalDays = LastRootTickTotalDays;
						flag = true;
					}
					break;
				case EnumFruitTreeState.Flowering:
					if (value.lastStateChangeTotalDays + (double)value.FloweringDays < LastRootTickTotalDays)
					{
						value.workingState = ((num2 < value.EnterDormancyTemp) ? EnumFruitTreeState.Empty : EnumFruitTreeState.Fruiting);
						value.lastStateChangeTotalDays = LastRootTickTotalDays;
						flag = true;
					}
					break;
				case EnumFruitTreeState.Fruiting:
					if (value.lastStateChangeTotalDays + (double)value.FruitingDays < LastRootTickTotalDays)
					{
						value.workingState = EnumFruitTreeState.Ripe;
						value.lastStateChangeTotalDays = LastRootTickTotalDays;
						flag = true;
					}
					break;
				case EnumFruitTreeState.Ripe:
					if (value.lastStateChangeTotalDays + (double)value.RipeDays < LastRootTickTotalDays)
					{
						value.workingState = EnumFruitTreeState.Empty;
						value.lastStateChangeTotalDays = LastRootTickTotalDays;
						flag = true;
					}
					break;
				case EnumFruitTreeState.Empty:
					if (value.CycleType == EnumTreeCycleType.Evergreen)
					{
						if (Math.Abs(LastRootTickTotalDays / (double)Api.World.Calendar.DaysPerYear % 1.0 - (double)value.BlossomAtYearRel) < 0.125)
						{
							value.workingState = EnumFruitTreeState.Flowering;
							value.lastStateChangeTotalDays = LastRootTickTotalDays;
							flag = true;
						}
					}
					else if (value.CycleType == EnumTreeCycleType.Deciduous && num2 < value.EnterDormancyTemp)
					{
						value.workingState = EnumFruitTreeState.EnterDormancy;
						value.lastStateChangeTotalDays = LastRootTickTotalDays;
						flag = true;
					}
					break;
				case EnumFruitTreeState.EnterDormancy:
					if (value.CycleType == EnumTreeCycleType.Deciduous && value.lastStateChangeTotalDays + 3.0 < LastRootTickTotalDays)
					{
						value.workingState = EnumFruitTreeState.Dormant;
						value.lastStateChangeTotalDays = LastRootTickTotalDays;
						flag = true;
					}
					break;
				case EnumFruitTreeState.Dormant:
					if (value.CycleType == EnumTreeCycleType.Deciduous)
					{
						updateVernalizedHours(value, num2);
						if (num2 >= 20f || (num2 > 15f && LastRootTickTotalDays - value.lastCheckAtTotalDays > 3.0))
						{
							value.workingState = EnumFruitTreeState.Empty;
							value.lastStateChangeTotalDays = LastRootTickTotalDays;
							flag = true;
						}
						else if (value.vernalizedHours > (double)value.VernalizationHours)
						{
							value.workingState = EnumFruitTreeState.DormantVernalized;
							value.lastStateChangeTotalDays = LastRootTickTotalDays;
							flag = true;
						}
					}
					break;
				case EnumFruitTreeState.DormantVernalized:
					if (num2 >= 15f || (num2 > 10f && LastRootTickTotalDays - value.lastCheckAtTotalDays > 3.0))
					{
						value.workingState = EnumFruitTreeState.Flowering;
						value.lastStateChangeTotalDays = LastRootTickTotalDays;
						flag = true;
					}
					break;
				}
				value.lastCheckAtTotalDays = LastRootTickTotalDays;
			}
			LastRootTickTotalDays += stateUpdateIntervalDays;
		}
		if (!flag)
		{
			return;
		}
		foreach (FruitTreeProperties value3 in propsByType.Values)
		{
			value3.State = value3.workingState;
		}
		Blockentity.MarkDirty(redrawOnClient: true);
	}

	public double GetCurrentStateProgress(string treeType)
	{
		if (Api == null)
		{
			return 0.0;
		}
		if (propsByType.TryGetValue(treeType, out var value))
		{
			switch (value.State)
			{
			case EnumFruitTreeState.Dormant:
				return 0.0;
			case EnumFruitTreeState.Flowering:
				return (Api.World.Calendar.TotalDays - value.lastStateChangeTotalDays) / (double)value.FloweringDays;
			case EnumFruitTreeState.Fruiting:
				return (Api.World.Calendar.TotalDays - value.lastStateChangeTotalDays) / (double)value.FruitingDays;
			case EnumFruitTreeState.Ripe:
				return (Api.World.Calendar.TotalDays - value.lastStateChangeTotalDays) / (double)value.RipeDays;
			case EnumFruitTreeState.Empty:
				return 0.0;
			}
		}
		return 0.0;
	}

	private void updateVernalizedHours(FruitTreeProperties props, float temp)
	{
		if (temp <= props.VernalizationTemp)
		{
			props.vernalizedHours += stateUpdateIntervalDays * (double)Api.World.Calendar.HoursPerDay;
		}
	}

	protected float getGreenhouseTempBonus()
	{
		if (Api.World.BlockAccessor.GetRainMapHeightAt(be.Pos) > be.Pos.Y)
		{
			Room room = roomreg?.GetRoomForPosition(be.Pos);
			if (((room != null && room.SkylightCount > room.NonSkylightCount && room.ExitCount == 0) ? 1 : 0) > 0)
			{
				return 5f;
			}
		}
		return 0f;
	}

	public float applyGreenhouseTempBonus(float temp)
	{
		return temp + greenhouseTempBonus;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		ITreeAttribute treeAttribute = tree.GetTreeAttribute("dynproprs");
		if (treeAttribute == null)
		{
			return;
		}
		foreach (KeyValuePair<string, IAttribute> item in treeAttribute)
		{
			propsByType[item.Key] = new FruitTreeProperties();
			propsByType[item.Key].FromTreeAttributes(item.Value as ITreeAttribute);
		}
		LastRootTickTotalDays = tree.GetDouble("lastRootTickTotalDays");
		TreePlantedTotalDays = tree.GetDouble("treePlantedTotalDays");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		TreeAttribute treeAttribute = (TreeAttribute)(tree["dynproprs"] = new TreeAttribute());
		tree.SetDouble("lastRootTickTotalDays", LastRootTickTotalDays);
		tree.SetDouble("treePlantedTotalDays", TreePlantedTotalDays);
		foreach (KeyValuePair<string, FruitTreeProperties> item in propsByType)
		{
			TreeAttribute treeAttribute2 = new TreeAttribute();
			item.Value.ToTreeAttributes(treeAttribute2);
			treeAttribute[item.Key] = treeAttribute2;
		}
	}
}
