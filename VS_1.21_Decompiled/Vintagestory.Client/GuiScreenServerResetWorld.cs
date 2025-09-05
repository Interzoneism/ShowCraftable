using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.Client.Util;

namespace Vintagestory.Client;

public class GuiScreenServerResetWorld : GuiScreen
{
	private ServerCtrlBackendInterface backend;

	public GuiScreenServerResetWorld(ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		backend = new ServerCtrlBackendInterface();
		ShowMainMenu = true;
		InitGui();
	}

	private void InitGui()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		TextExtents textExtents = CairoFont.ButtonText().GetTextExtents(Lang.Get("general-save"));
		_ = ((TextExtents)(ref textExtents)).Width;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 300.0, 35.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(330.0, 0.0, 300.0, 25.0);
		string[] values = new string[1] { "test" };
		string[] names = new string[1] { "test" };
		ElementComposer = ScreenManager.GuiComposers.Create("mainmenu-servercontrol-dashboard", ElementStdBounds.MainScreenRightPart()).AddImageBG(ElementBounds.Fill, GuiElement.dirtTextureName, 1f, 1f, 0.125f).BeginChildElements(ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 110.0, 550.0, 600.0))
			.AddStaticText(Lang.Get("serverctrl-dashboard"), CairoFont.WhiteSmallText(), elementBounds)
			.AddStaticText(Lang.Get("serverctrl-serverstatus"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.BelowCopy(0.0, 30.0))
			.AddRichtext(Lang.Get("Loading..."), CairoFont.WhiteSmallText(), elementBounds = elementBounds.BelowCopy(0.0, 30.0))
			.AddStaticText(Lang.Get("serverctrl-servername"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.BelowCopy(0.0, 30.0))
			.AddStaticText(Lang.Get("serverctrl-serverdescription"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.BelowCopy())
			.AddStaticText(Lang.Get("serverctrl-whitelisted"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.BelowCopy())
			.AddStaticText(Lang.Get("serverctrl-serverpassword"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.BelowCopy())
			.AddStaticText(Lang.Get("serverctrl-motd"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.BelowCopy())
			.AddStaticText(Lang.Get("serverctrl-advertise"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.BelowCopy())
			.AddStaticText(Lang.Get("serverctrl-seed"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.BelowCopy())
			.AddStaticText(Lang.Get("serverctrl-playstyle"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.BelowCopy())
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, 40.0), null, null, "servername")
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0), null, null, "serverdescription")
			.AddSwitch(onToggleWhiteListed, elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0), "whiteListedSwitch", 25.0)
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0).WithFixedWidth(300.0), null, null, "serverpassword")
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0), null, null, "motd")
			.AddSwitch(onToggleAdvertise, elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0), "advertiseSwith", 25.0)
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0).WithFixedWidth(300.0), null, null, "seed")
			.AddDropDown(values, names, 0, onPlayStyleChanged, elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0))
			.AddButton(Lang.Get("general-cancel"), OnCancel, ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0))
			.AddButton(Lang.Get("general-save"), OnSave, ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
			.EndChildElements()
			.Compose();
	}

	private void onPlayStyleChanged(string code, bool selected)
	{
		throw new NotImplementedException();
	}

	private void onToggleAdvertise(bool t1)
	{
		throw new NotImplementedException();
	}

	private void onToggleWhiteListed(bool t1)
	{
		throw new NotImplementedException();
	}

	private bool OnSave()
	{
		return true;
	}

	private bool OnCancel()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
		return true;
	}

	public override void OnScreenLoaded()
	{
		InitGui();
	}
}
