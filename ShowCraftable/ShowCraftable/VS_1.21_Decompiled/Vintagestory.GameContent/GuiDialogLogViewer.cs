using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

public class GuiDialogLogViewer : GuiDialogGeneric
{
	public GuiDialogLogViewer(string text, ICoreClientAPI capi)
		: base("Log Viewer", capi)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding, 40.0, 900.0, 30.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 0.0, 900.0, 300.0).FixedUnder(elementBounds, 5.0);
		ElementBounds elementBounds3 = elementBounds2.ForkBoundingParent();
		ElementBounds elementBounds4 = elementBounds2.FlatCopy().FixedGrow(6.0).WithFixedOffset(-3.0, -3.0);
		ElementBounds elementBounds5 = elementBounds4.CopyOffsetedSibling(elementBounds2.fixedWidth + 7.0).WithFixedWidth(20.0);
		ElementBounds elementBounds6 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds3, 10.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(20.0, 4.0);
		ElementBounds elementBounds7 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds7.BothSizing = ElementSizing.FitToChildren;
		elementBounds7.WithChildren(elementBounds4, elementBounds3, elementBounds5, elementBounds6);
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
		base.SingleComposer = capi.Gui.CreateCompo("dialogviewer", bounds).AddShadedDialogBG(elementBounds7).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
			.AddStaticText("The following warnings and errors were reported during startup:", CairoFont.WhiteDetailText(), elementBounds)
			.BeginChildElements(elementBounds7)
			.BeginClip(elementBounds3)
			.AddInset(elementBounds4, 3)
			.AddDynamicText("", CairoFont.WhiteDetailText(), elementBounds2, "text")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarvalue, elementBounds5, "scrollbar")
			.AddSmallButton("Close", OnButtonClose, elementBounds6)
			.EndChildElements()
			.Compose();
		GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("text");
		dynamicText.AutoHeight();
		dynamicText.SetNewText(text);
		base.SingleComposer.GetScrollbar("scrollbar").SetHeights(300f, (float)elementBounds2.fixedHeight);
	}

	private void OnNewScrollbarvalue(float value)
	{
		GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("text");
		dynamicText.Bounds.fixedY = 3f - value;
		dynamicText.Bounds.CalcWorldBounds();
	}

	private void OnTitleBarClose()
	{
		OnButtonClose();
	}

	private bool OnButtonClose()
	{
		TryClose();
		return true;
	}
}
