using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityTransient : BlockEntity
{
	private double lastCheckAtTotalDays;

	private double transitionHoursLeft = -1.0;

	private TransientProperties props;

	private long listenerId;

	private double? transitionAtTotalDaysOld;

	public string ConvertToOverride;

	public virtual int CheckIntervalMs { get; set; } = 2000;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		JsonObject attributes = base.Block.Attributes;
		if (attributes == null || !attributes["transientProps"].Exists)
		{
			return;
		}
		if (api.Side == EnumAppSide.Server)
		{
			Block block = Api.World.BlockAccessor.GetBlock(Pos, 1);
			if (block.Id != base.Block.Id)
			{
				if (!(block.EntityClass == base.Block.EntityClass))
				{
					Api.World.Logger.Warning("BETransient @{0} for Block {1}, but there is {2} at this position? Will delete BE", Pos, base.Block.Code.ToShortString(), block.Code.ToShortString());
					api.Event.EnqueueMainThreadTask(delegate
					{
						api.World.BlockAccessor.RemoveBlockEntity(Pos);
					}, "delete betransient");
					return;
				}
				if (!(block.Code.FirstCodePart() == base.Block.Code.FirstCodePart()))
				{
					Api.World.Logger.Warning("BETransient @{0} for Block {1}, but there is {2} at this position? Will delete BE and attempt to recreate it", Pos, base.Block.Code.ToShortString(), block.Code.ToShortString());
					api.Event.EnqueueMainThreadTask(delegate
					{
						api.World.BlockAccessor.RemoveBlockEntity(Pos);
						Block block2 = api.World.BlockAccessor.GetBlock(Pos, 1);
						api.World.BlockAccessor.SetBlock(block2.Id, Pos, 1);
					}, "delete betransient");
					return;
				}
				base.Block = block;
			}
		}
		props = base.Block.Attributes["transientProps"].AsObject<TransientProperties>();
		if (props == null)
		{
			return;
		}
		if (transitionHoursLeft <= 0.0)
		{
			transitionHoursLeft = props.InGameHours;
		}
		if (api.Side == EnumAppSide.Server)
		{
			if (listenerId != 0L)
			{
				throw new InvalidOperationException("Initializing BETransient twice would create a memory and performance leak");
			}
			listenerId = RegisterGameTickListener(CheckTransition, CheckIntervalMs);
			if (transitionAtTotalDaysOld.HasValue)
			{
				lastCheckAtTotalDays = Api.World.Calendar.TotalDays;
				transitionHoursLeft = (transitionAtTotalDaysOld.Value - lastCheckAtTotalDays) * (double)Api.World.Calendar.HoursPerDay;
			}
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		lastCheckAtTotalDays = Api.World.Calendar.TotalDays;
	}

	public virtual void CheckTransition(float dt)
	{
		if (Api.World.BlockAccessor.GetBlock(Pos).Attributes == null)
		{
			Api.World.Logger.Error("BETransient @{0}: cannot find block attributes for {1}. Will stop transient timer", Pos, base.Block.Code.ToShortString());
			UnregisterGameTickListener(listenerId);
			return;
		}
		lastCheckAtTotalDays = Math.Min(lastCheckAtTotalDays, Api.World.Calendar.TotalDays);
		ClimateCondition climateAt = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.WorldGenValues);
		if (climateAt == null)
		{
			return;
		}
		float temperature = climateAt.Temperature;
		float num = 1f / Api.World.Calendar.HoursPerDay;
		double totalDays = Api.World.Calendar.TotalDays;
		while (totalDays - lastCheckAtTotalDays > (double)num)
		{
			lastCheckAtTotalDays += num;
			transitionHoursLeft -= 1.0;
			climateAt.Temperature = temperature;
			ClimateCondition climateAt2 = Api.World.BlockAccessor.GetClimateAt(Pos, climateAt, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, lastCheckAtTotalDays);
			if (props.Condition == EnumTransientCondition.Temperature)
			{
				if (climateAt2.Temperature < props.WhenBelowTemperature || climateAt2.Temperature > props.WhenAboveTemperature)
				{
					tryTransition(props.ConvertTo);
				}
				continue;
			}
			bool flag = climateAt2.Temperature < props.ResetBelowTemperature;
			if (climateAt2.Temperature < props.StopBelowTemperature || flag)
			{
				transitionHoursLeft += 1.0;
				if (flag)
				{
					transitionHoursLeft = props.InGameHours;
				}
			}
			else if (transitionHoursLeft <= 0.0)
			{
				tryTransition(ConvertToOverride ?? props.ConvertTo);
				break;
			}
		}
	}

	public void tryTransition(string toCode)
	{
		Block block = Api.World.BlockAccessor.GetBlock(Pos);
		if (block.Attributes == null)
		{
			return;
		}
		string text = props.ConvertFrom;
		if (text != null && toCode != null)
		{
			if (text.IndexOf(':') == -1)
			{
				text = block.Code.Domain + ":" + text;
			}
			if (toCode.IndexOf(':') == -1)
			{
				toCode = block.Code.Domain + ":" + toCode;
			}
			AssetLocation blockCode = ((text != null && toCode.Contains('*')) ? block.Code.WildCardReplace(new AssetLocation(text), new AssetLocation(toCode)) : new AssetLocation(toCode));
			Block block2 = Api.World.GetBlock(blockCode);
			if (block2 != null)
			{
				Api.World.BlockAccessor.SetBlock(block2.BlockId, Pos, 1);
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		transitionHoursLeft = tree.GetDouble("transitionHoursLeft");
		if (tree.HasAttribute("transitionAtTotalDays"))
		{
			transitionAtTotalDaysOld = tree.GetDouble("transitionAtTotalDays");
		}
		lastCheckAtTotalDays = tree.GetDouble("lastCheckAtTotalDays");
		ConvertToOverride = tree.GetString("convertToOverride");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetDouble("transitionHoursLeft", transitionHoursLeft);
		tree.SetDouble("lastCheckAtTotalDays", lastCheckAtTotalDays);
		if (ConvertToOverride != null)
		{
			tree.SetString("convertToOverride", ConvertToOverride);
		}
	}

	public void SetPlaceTime(double totalHours)
	{
		float inGameHours = props.InGameHours;
		transitionHoursLeft = (double)inGameHours + totalHours - Api.World.Calendar.TotalHours;
	}

	public bool IsDueTransition()
	{
		return transitionHoursLeft <= 0.0;
	}
}
