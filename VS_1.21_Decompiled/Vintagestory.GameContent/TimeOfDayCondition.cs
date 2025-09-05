using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class TimeOfDayCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private float minHour;

	[JsonProperty]
	private float maxHour;

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "timeofday";

	public TimeOfDayCondition()
	{
	}

	public TimeOfDayCondition(EntityActivitySystem vas, float minHour, float maxHour, bool invert = false)
	{
		this.vas = vas;
		this.minHour = minHour;
		this.maxHour = maxHour;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		ICoreAPI api = vas.Entity.Api;
		float num = api.World.Calendar.HourOfDay / api.World.Calendar.HoursPerDay;
		float num2 = minHour / 24f;
		float num3 = maxHour / 24f;
		if (maxHour < minHour)
		{
			if (!(num >= num2))
			{
				return num <= num3;
			}
			return true;
		}
		if (num >= num2)
		{
			return num <= num3;
		}
		return false;
	}

	public void LoadState(ITreeAttribute tree)
	{
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Min Hour", CairoFont.WhiteDetailText(), elementBounds).AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "minHour").AddStaticText("Max Hour", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 10.0))
			.AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "maxHour");
		singleComposer.GetTextInput("minHour").SetValue(minHour.ToString() ?? "");
		singleComposer.GetTextInput("maxHour").SetValue(maxHour.ToString() ?? "");
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		minHour = singleComposer.GetTextInput("minHour").GetText().ToFloat();
		maxHour = singleComposer.GetTextInput("maxHour").GetText().ToFloat();
	}

	public IActionCondition Clone()
	{
		return new TimeOfDayCondition(vas, minHour, maxHour, Invert);
	}

	public override string ToString()
	{
		if (!Invert)
		{
			return "Time of day, from hour " + minHour + " until " + maxHour;
		}
		return "Time of day, outside of hours " + minHour + " to " + maxHour;
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
