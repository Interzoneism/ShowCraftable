using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class DressAction : EntityActionBase
{
	[JsonProperty]
	private string Code;

	[JsonProperty]
	private string Slot;

	public override string Type => "dress";

	public DressAction()
	{
	}

	public DressAction(EntityActivitySystem vas, string code, string slot)
	{
		base.vas = vas;
		Code = code;
		Slot = slot;
	}

	public override bool IsFinished()
	{
		return true;
	}

	public override void Start(EntityActivity act)
	{
		if (vas.Entity is EntityDressedHumanoid entityDressedHumanoid)
		{
			int num = entityDressedHumanoid.OutfitSlots.IndexOf(Slot);
			if (num < 0)
			{
				entityDressedHumanoid.OutfitCodes = entityDressedHumanoid.OutfitCodes.Append(Code);
				entityDressedHumanoid.OutfitSlots = entityDressedHumanoid.OutfitSlots.Append(Slot);
			}
			else
			{
				entityDressedHumanoid.OutfitCodes[num] = Code;
				entityDressedHumanoid.WatchedAttributes.MarkPathDirty("outfitcodes");
			}
		}
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Slot", CairoFont.WhiteDetailText(), elementBounds).AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "slot").AddStaticText("Outfit code", CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, 25.0).WithFixedWidth(300.0))
			.AddTextInput(elementBounds = elementBounds.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "code");
		singleComposer.GetTextInput("slot").SetValue(Slot);
		singleComposer.GetTextInput("code").SetValue(Code);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		Slot = singleComposer.GetTextInput("slot").GetText();
		Code = singleComposer.GetTextInput("code").GetText();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new DressAction(vas, Code, Slot);
	}

	public override string ToString()
	{
		return "Dress outfit " + Code + " in slot" + Slot;
	}
}
