using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

public class HudTutorial : HudElement
{
	public HudTutorial(ICoreClientAPI capi)
		: base(capi)
	{
	}

	public void loadHud(string pagecode)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 300.0, 200.0);
		ElementBounds elementBounds2 = new ElementBounds().WithSizing(ElementSizing.FitToChildren).WithFixedPadding(GuiStyle.ElementToDialogPadding / 2.0);
		elementBounds2.WithChildren(elementBounds);
		ElementBounds bounds = elementBounds2.ForkBoundingParent().WithAlignment(EnumDialogArea.None).WithAlignment(EnumDialogArea.RightMiddle)
			.WithFixedPosition(0.0, -225.0);
		RichTextComponentBase[] pageText = capi.ModLoader.GetModSystem<ModSystemTutorial>().GetPageText(pagecode, skipOld: true);
		base.SingleComposer?.Dispose();
		base.SingleComposer = capi.Gui.CreateCompo("tutorialhud", bounds).AddGameOverlay(elementBounds2, GuiStyle.DialogLightBgColor).AddRichtext(pageText, elementBounds, "richtext")
			.Compose();
	}

	public override void Dispose()
	{
		base.Dispose();
	}
}
