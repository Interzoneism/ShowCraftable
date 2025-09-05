using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public class GuiScreenMultiplayer : GuiScreen
{
	private List<MultiplayerServerEntry> serverentries;

	private ElementBounds tableBounds;

	private ElementBounds clippingBounds;

	public GuiScreenMultiplayer(ScreenManager screenManager, GuiScreen parent)
		: base(screenManager, parent)
	{
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
			ScreenManager.GuiComposers.Dispose("mainmenu-multiplayer");
		}
	}

	private void InitGui()
	{
		List<SavegameCellEntry> cells = LoadServerEntries();
		int height = ScreenManager.GamePlatform.WindowSize.Height;
		int width = ScreenManager.GamePlatform.WindowSize.Width;
		ElementBounds elementBounds = ElementBounds.FixedSize(60.0, 30.0).WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, ScreenManager.ClientIsOffline ? 30 : 0, 690.0, 35.0);
		float num = (float)Math.Max(300, height) / ClientSettings.GUIScale;
		ElementBounds elementBounds3;
		ElementComposer = dialogBase("mainmenu-multiplayer").AddStaticText(Lang.Get("multiplayer-yourservers"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 240.0)).AddIf(ScreenManager.ClientIsOffline).AddRichtext(Lang.Get("offlinemultiplayerwarning"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0.0, 25.0, 690.0, 30.0))
			.EndIf()
			.AddInset(elementBounds3 = elementBounds2.BelowCopy(0.0, 3.0).WithFixedSize(Math.Max(400.0, (double)width * 0.5) / (double)ClientSettings.GUIScale, num - 250f))
			.AddVerticalScrollbar(OnNewScrollbarvalue, ElementStdBounds.VerticalScrollbar(elementBounds3), "scrollbar")
			.BeginClip(clippingBounds = elementBounds3.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
			.AddCellList(tableBounds = clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0), createCellElem, cells, "serverstable")
			.EndClip()
			.AddButton(Lang.Get("multiplayer-addserver"), OnAddServer, elementBounds.FlatCopy().FixedUnder(elementBounds3, 10.0).WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedAlignmentOffset(-13.0, 0.0))
			.AddButton(Lang.Get("multiplayer-browsepublicservers"), OnPublicListing, elementBounds.FlatCopy().FixedUnder(elementBounds3, 10.0))
			.AddSmallButton(Lang.Get("multiplayer-selfhosting"), OnSelfHosting, elementBounds.FlatCopy().FixedUnder(elementBounds3, 60.0))
			.EndChildElements()
			.Compose();
		tableBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)tableBounds.fixedHeight);
	}

	private IGuiElementCell createCellElem(SavegameCellEntry cell, ElementBounds bounds)
	{
		GuiElementMainMenuCell guiElementMainMenuCell = new GuiElementMainMenuCell(ScreenManager.api, cell, bounds);
		cell.LeftOffY = -2f;
		guiElementMainMenuCell.OnMouseDownOnCellLeft = OnClickCellLeft;
		guiElementMainMenuCell.OnMouseDownOnCellRight = OnClickCellRight;
		return guiElementMainMenuCell;
	}

	private bool OnSelfHosting()
	{
		ScreenManager.api.Gui.OpenLink("https://www.vintagestory.at/multiplayer");
		return true;
	}

	private bool OnPublicListing()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenPublicServers));
		return true;
	}

	public override void OnScreenLoaded()
	{
		InitGui();
		ElementComposer.GetCellList<SavegameCellEntry>("serverstable").ReloadCells(LoadServerEntries());
		tableBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)tableBounds.fixedHeight);
	}

	private List<SavegameCellEntry> LoadServerEntries()
	{
		serverentries = new List<MultiplayerServerEntry>();
		List<string> stringListSetting = ClientSettings.Inst.GetStringListSetting("multiplayerservers", new List<string>());
		List<SavegameCellEntry> list = new List<SavegameCellEntry>();
		for (int i = 0; i < stringListSetting.Count; i++)
		{
			string[] array = stringListSetting[i].Split(',');
			MultiplayerServerEntry multiplayerServerEntry = new MultiplayerServerEntry
			{
				index = i,
				name = array[0],
				host = array[1],
				password = ((array.Length > 2) ? array[2] : "")
			};
			serverentries.Add(multiplayerServerEntry);
			SavegameCellEntry item = new SavegameCellEntry
			{
				Title = multiplayerServerEntry.name
			};
			list.Add(item);
		}
		return list;
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetCellList<SavegameCellEntry>("serverstable").Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
	}

	private void OnClickCellLeft(int index)
	{
		MultiplayerServerEntry multiplayerServerEntry = serverentries[index];
		ScreenManager.ConnectToMultiplayer(multiplayerServerEntry.host, multiplayerServerEntry.password);
	}

	private void OnClickCellRight(int cellIndex)
	{
		ScreenManager.LoadScreen(new GuiScreenMultiplayerModify(serverentries[cellIndex], ScreenManager, this));
	}

	private bool OnAddServer()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayerNewServer));
		return true;
	}
}
