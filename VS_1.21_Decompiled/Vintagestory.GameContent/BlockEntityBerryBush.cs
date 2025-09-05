using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityBerryBush : BlockEntity, IAnimalFoodSource, IPointOfInterest
{
	private static Random rand = new Random();

	private const float intervalHours = 2f;

	private double lastCheckAtTotalDays;

	private double transitionHoursLeft = -1.0;

	private double? totalDaysForNextStageOld;

	private RoomRegistry roomreg;

	public int roomness;

	public bool Pruned;

	public double LastPrunedTotalDays;

	private float resetBelowTemperature;

	private float resetAboveTemperature;

	private float stopBelowTemperature;

	private float stopAboveTemperature;

	private float revertBlockBelowTemperature;

	private float revertBlockAboveTemperature;

	private float growthRateMul = 1f;

	private NatFloat nextStageMonths = NatFloat.create(EnumDistribution.UNIFORM, 0.98f, 0.09f);

	public string[] creatureDietFoodTags;

	public bool IsEmpty => base.Block.Variant["state"] == "empty";

	public bool IsFlowering => base.Block.Variant["state"] == "flowering";

	public bool IsRipe => base.Block.Variant["state"] == "ripe";

	public Vec3d Position => Pos.ToVec3d().Add(0.5, 0.5, 0.5);

	public string Type => "food";

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		growthRateMul = (float)Api.World.Config.GetDecimal("cropGrowthRateMul", growthRateMul);
		if (api is ICoreServerAPI)
		{
			creatureDietFoodTags = base.Block.Attributes["foodTags"].AsArray<string>();
			if (transitionHoursLeft <= 0.0)
			{
				transitionHoursLeft = GetHoursForNextStage();
				lastCheckAtTotalDays = api.World.Calendar.TotalDays;
			}
			if (Api.World.Config.GetBool("processCrops", defaultValue: true))
			{
				RegisterGameTickListener(CheckGrow, 8000);
			}
			api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
			roomreg = Api.ModLoader.GetModSystem<RoomRegistry>();
			if (totalDaysForNextStageOld.HasValue)
			{
				transitionHoursLeft = (totalDaysForNextStageOld.Value - Api.World.Calendar.TotalDays) * (double)Api.World.Calendar.HoursPerDay;
			}
		}
	}

	public void Prune()
	{
		Pruned = true;
		LastPrunedTotalDays = Api.World.Calendar.TotalDays;
		MarkDirty(redrawOnClient: true);
	}

	protected virtual void CheckGrow(float dt)
	{
		if (!(Api as ICoreServerAPI).World.IsFullyLoadedChunk(Pos))
		{
			return;
		}
		if (base.Block.Attributes == null)
		{
			UnregisterAllTickListeners();
			return;
		}
		lastCheckAtTotalDays = Math.Min(lastCheckAtTotalDays, Api.World.Calendar.TotalDays);
		LastPrunedTotalDays = Math.Min(LastPrunedTotalDays, Api.World.Calendar.TotalDays);
		double num = GameMath.Mod(Api.World.Calendar.TotalDays - lastCheckAtTotalDays, Api.World.Calendar.DaysPerYear);
		float num2 = 2f / Api.World.Calendar.HoursPerDay;
		if (num <= (double)num2)
		{
			return;
		}
		if (Api.World.BlockAccessor.GetRainMapHeightAt(Pos) > Pos.Y)
		{
			Room room = roomreg?.GetRoomForPosition(Pos);
			roomness = ((room != null && room.SkylightCount > room.NonSkylightCount && room.ExitCount == 0) ? 1 : 0);
		}
		else
		{
			roomness = 0;
		}
		ClimateCondition climateCondition = null;
		float temperature = 0f;
		while (num > (double)num2)
		{
			num -= (double)num2;
			lastCheckAtTotalDays += num2;
			transitionHoursLeft -= 2.0;
			if (climateCondition == null)
			{
				climateCondition = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, lastCheckAtTotalDays);
				if (climateCondition == null)
				{
					return;
				}
				temperature = climateCondition.WorldGenTemperature;
			}
			else
			{
				climateCondition.Temperature = temperature;
				Api.World.BlockAccessor.GetClimateAt(Pos, climateCondition, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, lastCheckAtTotalDays);
			}
			float num3 = climateCondition.Temperature;
			if (roomness > 0)
			{
				num3 += 5f;
			}
			bool flag = num3 < resetBelowTemperature || num3 > resetAboveTemperature;
			if (num3 < stopBelowTemperature || num3 > stopAboveTemperature || flag)
			{
				if (!IsRipe)
				{
					transitionHoursLeft += 2.0;
				}
				if (flag)
				{
					bool num4 = num3 < revertBlockBelowTemperature || num3 > revertBlockAboveTemperature;
					if (!IsRipe)
					{
						transitionHoursLeft = GetHoursForNextStage();
					}
					if (num4 && !IsEmpty)
					{
						Block block = Api.World.GetBlock(base.Block.CodeWithVariant("state", "empty"));
						Api.World.BlockAccessor.ExchangeBlock(block.BlockId, Pos);
					}
				}
			}
			else
			{
				if (Pruned && Api.World.Calendar.TotalDays - LastPrunedTotalDays > (double)Api.World.Calendar.DaysPerYear)
				{
					Pruned = false;
				}
				if (transitionHoursLeft <= 0.0 && !DoGrow())
				{
					return;
				}
			}
		}
		MarkDirty();
	}

	public override void OnExchanged(Block block)
	{
		base.OnExchanged(block);
		UpdateTransitionsFromBlock();
		transitionHoursLeft = GetHoursForNextStage();
	}

	public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
	{
		base.CreateBehaviors(block, worldForResolve);
		UpdateTransitionsFromBlock();
	}

	protected virtual void UpdateTransitionsFromBlock()
	{
		if (base.Block?.Attributes == null)
		{
			resetBelowTemperature = (stopBelowTemperature = (revertBlockBelowTemperature = -999f));
			resetAboveTemperature = (stopAboveTemperature = (revertBlockAboveTemperature = 999f));
			nextStageMonths = NatFloat.create(EnumDistribution.UNIFORM, 0.98f, 0.09f);
			return;
		}
		resetBelowTemperature = base.Block.Attributes["resetBelowTemperature"].AsFloat(-999f);
		resetAboveTemperature = base.Block.Attributes["resetAboveTemperature"].AsFloat(999f);
		stopBelowTemperature = base.Block.Attributes["stopBelowTemperature"].AsFloat(-999f);
		stopAboveTemperature = base.Block.Attributes["stopAboveTemperature"].AsFloat(999f);
		revertBlockBelowTemperature = base.Block.Attributes["revertBlockBelowTemperature"].AsFloat(-999f);
		revertBlockAboveTemperature = base.Block.Attributes["revertBlockAboveTemperature"].AsFloat(999f);
		nextStageMonths = base.Block.Attributes["nextStageMonths"].AsObject(nextStageMonths);
	}

	public virtual double GetHoursForNextStage()
	{
		if (IsRipe)
		{
			return 4f * nextStageMonths.nextFloat() * (float)Api.World.Calendar.DaysPerMonth * Api.World.Calendar.HoursPerDay;
		}
		return nextStageMonths.nextFloat() * (float)Api.World.Calendar.DaysPerMonth * Api.World.Calendar.HoursPerDay / growthRateMul;
	}

	protected virtual bool DoGrow()
	{
		AssetLocation assetLocation = base.Block.CodeWithVariant("state", IsEmpty ? "flowering" : (IsFlowering ? "ripe" : "empty"));
		if (!assetLocation.Valid)
		{
			Api.World.BlockAccessor.RemoveBlockEntity(Pos);
			return false;
		}
		Block block = Api.World.GetBlock(assetLocation);
		if (block?.Code == null)
		{
			return false;
		}
		Api.World.BlockAccessor.ExchangeBlock(block.BlockId, Pos);
		MarkDirty(redrawOnClient: true);
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		transitionHoursLeft = tree.GetDouble("transitionHoursLeft");
		if (tree.HasAttribute("totalDaysForNextStage"))
		{
			totalDaysForNextStageOld = tree.GetDouble("totalDaysForNextStage");
		}
		lastCheckAtTotalDays = tree.GetDouble("lastCheckAtTotalDays");
		roomness = tree.GetInt("roomness");
		Pruned = tree.GetBool("pruned");
		LastPrunedTotalDays = tree.GetDecimal("lastPrunedTotalDays");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetDouble("transitionHoursLeft", transitionHoursLeft);
		tree.SetDouble("lastCheckAtTotalDays", lastCheckAtTotalDays);
		tree.SetBool("pruned", Pruned);
		tree.SetInt("roomness", roomness);
		tree.SetDouble("lastPrunedTotalDays", LastPrunedTotalDays);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		double num = transitionHoursLeft / (double)Api.World.Calendar.HoursPerDay;
		if (!IsRipe)
		{
			string text = (IsEmpty ? "flowering" : "ripen");
			if (num < 1.0)
			{
				sb.AppendLine(Lang.Get("berrybush-" + text + "-1day"));
			}
			else
			{
				sb.AppendLine(Lang.Get("berrybush-" + text + "-xdays", (int)num));
			}
			if (roomness > 0)
			{
				sb.AppendLine(Lang.Get("greenhousetempbonus"));
			}
		}
	}

	public bool IsSuitableFor(Entity entity, CreatureDiet diet)
	{
		if (diet == null || !IsRipe)
		{
			return false;
		}
		return diet.Matches(EnumFoodCategory.NoNutrition, creatureDietFoodTags);
	}

	public float ConsumeOnePortion(Entity entity)
	{
		AssetLocation assetLocation = base.Block.CodeWithVariant("state", "empty");
		if (!assetLocation.Valid)
		{
			Api.World.BlockAccessor.RemoveBlockEntity(Pos);
			return 0f;
		}
		Block block = Api.World.GetBlock(assetLocation);
		if (block?.Code == null)
		{
			return 0f;
		}
		BlockBehaviorHarvestable behavior = base.Block.GetBehavior<BlockBehaviorHarvestable>();
		behavior?.harvestedStacks?.Foreach(delegate(BlockDropItemStack harvestedStack)
		{
			Api.World.SpawnItemEntity(harvestedStack?.GetNextItemStack(), Pos);
		});
		Api.World.PlaySoundAt(behavior?.harvestingSound, Pos, 0.0);
		Api.World.BlockAccessor.ExchangeBlock(block.BlockId, Pos);
		MarkDirty(redrawOnClient: true);
		return 0.1f;
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (Pruned)
		{
			mesher.AddMeshData((base.Block as BlockBerryBush).GetPrunedMesh(Pos));
			return true;
		}
		return base.OnTesselation(mesher, tessThreadTesselator);
	}
}
