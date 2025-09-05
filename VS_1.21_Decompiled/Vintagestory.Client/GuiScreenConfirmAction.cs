using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client;

internal class GuiScreenConfirmAction : GuiScreen
{
	private Action<bool> DidPressButton;

	public override bool ShouldDisposePreviousScreen => false;

	public GuiScreenConfirmAction(string text, Action<bool> DidPressButton, ScreenManager screenManager, GuiScreen parentScreen, bool onlyCancel = false)
		: this("Please Confirm", text, "Cancel", "Confirm", DidPressButton, screenManager, parentScreen, null, onlyCancel)
	{
	}

	public GuiScreenConfirmAction(string title, string text, string cancelText, string confirmText, Action<bool> DidPressButton, ScreenManager screenManager, GuiScreen parentScreen, string composersubcode, bool onlyCancel = false)
		: base(screenManager, parentScreen)
	{
		this.DidPressButton = DidPressButton;
		ShowMainMenu = true;
		CairoFont cairoFont = CairoFont.WhiteSmallText().WithFontSize(17f).WithLineHeightMultiplier(1.25);
		double num = screenManager.api.Gui.Text.GetMultilineTextHeight(cairoFont, text, GuiElement.scaled(650.0)) / (double)RuntimeEnv.GUIScale;
		ElementBounds elementBounds = ElementStdBounds.Rowed(0f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(400.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, num + 90.0, 0.0, 0.0).WithFixedPadding(10.0, 2.0);
		ElementComposer = dialogBase("mainmenu-confirmaction" + composersubcode, -1.0, num + 130.0).AddStaticText(Lang.Get(title), CairoFont.WhiteSmallishText().WithWeight((FontWeight)1), elementBounds).AddRichtext(text, cairoFont, ElementBounds.FixedSize(650.0, 650.0).FixedUnder(elementBounds, 30.0)).AddButton(Lang.Get(cancelText), OnCancel, elementBounds2)
			.AddIf(!onlyCancel)
			.AddButton(Lang.Get(confirmText), OnConfirm, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed), EnumButtonStyle.Normal, "confirmButton")
			.EndIf()
			.EndChildElements()
			.Compose();
	}

	private bool OnConfirm()
	{
		ElementComposer.GetButton("confirmButton").Enabled = false;
		DidPressButton(obj: true);
		return true;
	}

	private bool OnCancel()
	{
		DidPressButton(obj: false);
		return true;
	}
}
