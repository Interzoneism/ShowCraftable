using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class GuiScreenPublicServerView : GuiScreen
{
	private ServerListEntry entry;

	public GuiScreenPublicServerView(ServerListEntry entry, ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		this.entry = entry;
		ShowMainMenu = true;
		InitGui();
		screenManager.GamePlatform.WindowResized += delegate
		{
			invalidate();
		};
		ClientSettings.Inst.AddWatcher<float>("guiScale", delegate
		{
			invalidate();
		});
	}

	private void invalidate()
	{
		if (base.IsOpened)
		{
			InitGui();
		}
		else
		{
			ScreenManager.GuiComposers.Dispose("mainmenu-browserpublicserverview");
		}
	}

	private void InitGui()
	{
		//IL_06e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e8: Unknown result type (might be due to invalid IL or missing references)
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 700.0, 30.0);
		ElementBounds elementBounds2 = elementBounds.BelowCopy().WithFixedSize(170.0, 35.0);
		List<string> list = new List<string>();
		if (entry.hasPassword)
		{
			list.Add(Lang.Get("Password protected"));
		}
		if (entry.whitelisted)
		{
			list.Add(Lang.Get("Whitelisted players only"));
		}
		string text = string.Join(", ", list);
		List<string> list2 = new List<string>();
		int num = 0;
		ModPacket[] mods = entry.mods;
		foreach (ModPacket modPacket in mods)
		{
			if (num++ > 20 && entry.mods.Length > 25)
			{
				break;
			}
			list2.Add(modPacket.id);
		}
		string text2 = string.Join(", ", list2);
		if (list2.Count < entry.mods.Length)
		{
			text2 += Lang.Get(" and {0} more", entry.mods.Length - list2.Count);
		}
		if (text2.Length == 0)
		{
			text2 = Lang.Get("server-nomods");
		}
		CairoFont cairoFont = CairoFont.WhiteSmallText();
		ElementComposer = dialogBase("mainmenu-browserpublicserverview").AddStaticText(entry.serverName, CairoFont.WhiteSmallishText(), elementBounds.FlatCopy()).AddStaticText(Lang.Get("Description"), cairoFont, elementBounds2 = elementBounds2.BelowCopy()).AddRichtext((entry.gameDescription.Length == 0) ? "<i>No description</i>" : entry.gameDescription, cairoFont, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 35.0), "desc");
		ElementBounds bounds = ElementComposer.GetRichtext("desc").Bounds;
		ElementComposer.GetRichtext("desc").BeforeCalcBounds();
		ElementComposer.AddStaticText(Lang.Get("Playstyle"), cairoFont, elementBounds2 = elementBounds2.BelowCopy().WithFixedOffset(0.0, Math.Max(0.0, bounds.fixedHeight - 30.0))).AddStaticText((entry.playstyle.langCode == null) ? Lang.Get("playstyle-" + entry.playstyle.id) : Lang.Get("playstyle-" + entry.playstyle.langCode), cairoFont, EnumTextOrientation.Left, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 70.0)).AddStaticText(Lang.Get("Currently online"), cairoFont, elementBounds2 = elementBounds2.BelowCopy())
			.AddStaticText(entry.players + " / " + entry.maxPlayers, cairoFont, EnumTextOrientation.Left, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 30.0));
		if (text.Length > 0)
		{
			ElementComposer.AddStaticText(Lang.Get("Configuration"), cairoFont, elementBounds2 = elementBounds2.BelowCopy()).AddStaticText(text, cairoFont, EnumTextOrientation.Left, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 30.0));
		}
		ElementComposer.AddStaticText(Lang.Get("Game version"), cairoFont, elementBounds2 = elementBounds2.BelowCopy()).AddStaticText(entry.gameVersion, cairoFont, EnumTextOrientation.Left, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 30.0)).AddStaticText(Lang.Get("Mods", entry.mods.Length), cairoFont, elementBounds2 = elementBounds2.BelowCopy())
			.AddStaticText(text2, cairoFont, EnumTextOrientation.Left, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 30.0), "mods");
		ElementComposer.GetStaticText("mods").Bounds.CalcWorldBounds();
		double num2 = ElementComposer.GetStaticText("mods").GetTextHeight() / (double)RuntimeEnv.GUIScale;
		if (entry.hasPassword)
		{
			ElementComposer.AddIf(entry.hasPassword).AddStaticText(Lang.Get("Password"), CairoFont.WhiteSmallishText(), elementBounds2 = elementBounds2.BelowCopy(0.0, num2 - 20.0)).AddTextInput(elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedOffset(0.0, -3.0)
				.WithFixedSize(540.0, 30.0), null, null, "password")
				.EndIf();
		}
		else
		{
			elementBounds2 = elementBounds2.FlatCopy();
			elementBounds2.fixedY += num2;
		}
		TextExtents textExtents = CairoFont.ButtonText().GetTextExtents(Lang.Get("Join Server"));
		double num3 = ((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale;
		ElementComposer.AddButton(Lang.Get("Back"), OnBack, ElementBounds.Fixed(0, 0).FixedUnder(elementBounds2, 20.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedAlignmentOffset(0.0, 0.0)
			.WithFixedPadding(10.0, 2.0)).AddButton(Lang.Get("Add to Favorites"), OnAddToFavorites, ElementBounds.Fixed(0, 0).FixedUnder(elementBounds2, 20.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedAlignmentOffset(-30.0 - num3, 0.0)
			.WithFixedPadding(10.0, 2.0)).AddButton(Lang.Get("Join Server"), OnJoin, ElementBounds.Fixed(0, 0).FixedUnder(elementBounds2, 20.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0))
			.EndChildElements()
			.Compose();
	}

	private bool OnAddToFavorites()
	{
		List<string> stringListSetting = ClientSettings.Inst.GetStringListSetting("multiplayerservers", new List<string>());
		string serverIp = entry.serverIp;
		string text = entry.serverName.Replace(",", "");
		string text2 = ElementComposer.GetTextInput("password")?.GetText().Replace(",", "&comma;");
		stringListSetting.Add(text + "," + serverIp + "," + text2);
		ClientSettings.Inst.Strings["multiplayerservers"] = stringListSetting;
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
		return true;
	}

	private bool OnBack()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenPublicServers));
		return true;
	}

	private bool OnJoin()
	{
		if (!entry.hasPassword)
		{
			ScreenManager.ConnectToMultiplayer(entry.serverIp, null);
		}
		else
		{
			ScreenManager.ConnectToMultiplayer(entry.serverIp, ElementComposer.GetTextInput("password").GetText());
		}
		return true;
	}
}
