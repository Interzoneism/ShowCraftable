using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class DressedCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private string Code;

	[JsonProperty]
	private string Slot;

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "dressed";

	public DressedCondition()
	{
	}

	public DressedCondition(EntityActivitySystem vas, string code, string slot, bool invert = false)
	{
		this.vas = vas;
		Code = code;
		Slot = slot;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		if (!(vas.Entity is EntityDressedHumanoid entityDressedHumanoid))
		{
			return false;
		}
		int num = entityDressedHumanoid.OutfitSlots.IndexOf(Slot);
		if (num < 0)
		{
			return false;
		}
		return entityDressedHumanoid.OutfitCodes[num] == Code;
	}

	public void LoadState(ITreeAttribute tree)
	{
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 250.0, 25.0);
		singleComposer.AddStaticText("Slot", CairoFont.WhiteDetailText(), elementBounds).AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "slot").AddStaticText("Accessory Code", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 15.0))
			.AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "code");
		singleComposer.GetTextInput("code").SetValue(Code);
		singleComposer.GetTextInput("slot").SetValue(Slot);
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		Code = singleComposer.GetTextInput("code").GetText();
		Slot = singleComposer.GetTextInput("slot").GetText();
	}

	public IActionCondition Clone()
	{
		return new DressedCondition(vas, Code, Slot, Invert);
	}

	public override string ToString()
	{
		return string.Format(Invert ? "When not {0} dressed in slot {1}" : "When {0} dressed in slot {1}", Code, Slot);
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
