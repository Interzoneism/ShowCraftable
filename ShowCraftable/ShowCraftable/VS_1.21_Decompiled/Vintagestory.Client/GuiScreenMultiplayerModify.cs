using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public class GuiScreenMultiplayerModify : GuiScreen
{
	private MultiplayerServerEntry serverentry;

	public GuiScreenMultiplayerModify(MultiplayerServerEntry serverentry, ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		ShowMainMenu = true;
		this.serverentry = serverentry;
		InitGui();
	}

	private void InitGui()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		TextExtents textExtents = CairoFont.ButtonText().GetTextExtents(Lang.Get("general-save"));
		double num = ((TextExtents)(ref textExtents)).Width + 20.0;
		ElementComposer = dialogBase("mainmenu-multiplayernewserver", -1.0, 330.0).AddStaticText(Lang.Get("multiplayer-modifyserver"), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(0f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0)).AddStaticText(Lang.Get("multiplayer-servername"), CairoFont.WhiteSmallText(), ElementStdBounds.Rowed(1.05f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0)).AddTextInput(ElementStdBounds.Rowed(1f, 0.0, EnumDialogArea.RightFixed).WithFixedSize(300.0, 30.0).WithFixedAlignmentOffset(-35.0, 0.0), null, null, "servername")
			.AddStaticText(Lang.Get("multiplayer-address"), CairoFont.WhiteSmallText(), ElementStdBounds.Rowed(1.74f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0))
			.AddTextInput(ElementStdBounds.Rowed(1.7f, 0.0, EnumDialogArea.RightFixed).WithFixedSize(300.0, 30.0).WithFixedAlignmentOffset(-35.0, 0.0), null, null, "serverhost")
			.AddIconButton("copy", OnCopyServer, ElementStdBounds.Rowed(1.7f, 0.0, EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0))
			.AddHoverText(Lang.Get("Copies the server address to your clipboard"), CairoFont.WhiteDetailText(), 200, ElementStdBounds.Rowed(1.7f, 0.0, EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0))
			.AddStaticText(Lang.Get("multiplayer-serverpassword"), CairoFont.WhiteSmallText(), ElementStdBounds.Rowed(2.47f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0))
			.AddTextInput(ElementStdBounds.Rowed(2.4f, 0.0, EnumDialogArea.RightFixed).WithFixedSize(300.0, 30.0).WithFixedAlignmentOffset(-35.0, 0.0), null, null, "serverpassword")
			.AddDynamicText("", CairoFont.WhiteSmallText().WithColor(GuiStyle.ErrorTextColor), ElementStdBounds.Rowed(3.5f, 0.0).WithFixedSize(550.0, 30.0), "errorLine")
			.AddButton(Lang.Get("general-cancel"), OnCancel, ElementStdBounds.Rowed(4.2f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0))
			.AddButton(Lang.Get("general-delete"), OnDelete, ElementStdBounds.Rowed(4.2f, 0.0, EnumDialogArea.RightFixed).WithFixedAlignmentOffset(0.0 - num - 20.0, 0.0).WithFixedPadding(10.0, 2.0))
			.AddButton(Lang.Get("general-save"), OnSave, ElementStdBounds.Rowed(4.2f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
			.EndChildElements()
			.Compose();
		ElementComposer.GetTextInput("servername").SetValue(serverentry.name);
		ElementComposer.GetTextInput("serverhost").SetValue(serverentry.host);
		ElementComposer.GetTextInput("serverpassword").HideCharacters();
		ElementComposer.GetTextInput("serverpassword").SetValue(serverentry.password);
	}

	private void OnCopyServer(bool ok)
	{
		ScreenManager.Platform.XPlatInterface.SetClipboardText(serverentry.host);
	}

	private bool OnSave()
	{
		serverentry.name = ElementComposer.GetTextInput("servername").GetText().Replace(",", " ");
		serverentry.host = ElementComposer.GetTextInput("serverhost").GetText().Replace(",", " ");
		string text = ElementComposer.GetTextInput("serverpassword").GetText().Replace(",", "&comma;");
		NetUtil.getUriInfo(serverentry.host, out var error);
		if (error != null)
		{
			ElementComposer.GetDynamicText("errorLine").SetNewText(error, autoHeight: true);
			return true;
		}
		if (serverentry.host.Length == 0)
		{
			ElementComposer.GetDynamicText("errorLine").SetNewText(Lang.Get("No host / ip address supplied"), autoHeight: true);
			return true;
		}
		ClientSettings.Inst.GetStringListSetting("multiplayerservers", new List<string>())[serverentry.index] = serverentry.name + "," + serverentry.host + "," + text;
		ClientSettings.Inst.Save(force: true);
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
		return true;
	}

	private bool OnDelete()
	{
		ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("multiplayer-deleteserver-confirmation", serverentry.name), OnDidConfirmDelete, ScreenManager, this));
		return true;
	}

	private void OnDidConfirmDelete(bool confirm)
	{
		if (confirm)
		{
			List<string> stringListSetting = ClientSettings.Inst.GetStringListSetting("multiplayerservers", new List<string>());
			for (int i = 0; i < stringListSetting.Count; i++)
			{
				if (stringListSetting[i] == serverentry.name + "," + serverentry.host + "," + serverentry.password || stringListSetting[i] == serverentry.name + "," + serverentry.host)
				{
					stringListSetting.RemoveAt(i);
					break;
				}
			}
			ClientSettings.Inst.Strings["multiplayerservers"] = stringListSetting;
			ClientSettings.Inst.Save(force: true);
			ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
		}
		else
		{
			ScreenManager.LoadScreen(this);
		}
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
