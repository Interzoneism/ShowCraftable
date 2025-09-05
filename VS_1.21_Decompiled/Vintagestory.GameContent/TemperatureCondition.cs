using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class TemperatureCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private float belowTemperature;

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "temperature";

	public TemperatureCondition()
	{
	}

	public TemperatureCondition(EntityActivitySystem vas, float belowTemperature, bool invert)
	{
		this.vas = vas;
		this.belowTemperature = belowTemperature;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		ICoreAPI api = vas.Entity.Api;
		return api.World.BlockAccessor.GetClimateAt(e.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, api.World.Calendar.TotalDays).Temperature < belowTemperature;
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
		singleComposer.AddStaticText("When below temperature", CairoFont.WhiteDetailText(), elementBounds).AddNumberInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "temp");
		singleComposer.GetNumberInput("temp").SetValue(belowTemperature.ToString() ?? "");
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		belowTemperature = singleComposer.GetNumberInput("temp").GetValue();
	}

	public IActionCondition Clone()
	{
		return new TemperatureCondition(vas, belowTemperature, Invert);
	}

	public override string ToString()
	{
		if (!Invert)
		{
			return "When below temperature" + belowTemperature;
		}
		return "When above temperature " + belowTemperature;
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
