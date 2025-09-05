using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.ModDb;

namespace Vintagestory.Client;

public class GuiScreenOnlineMods : GuiScreen
{
	private bool ingoreLoadOnce = true;

	private ElementBounds listBounds;

	private ElementBounds clippingBounds;

	private ModDbUtil modDbUtil;

	private List<ModCellEntry> modCells;

	private string searchText = "";

	public GuiScreenOnlineMods(ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
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
		modDbUtil = new ModDbUtil(screenManager.api, ClientSettings.ModDbUrl, GamePaths.DataPathMods);
	}

	private void invalidate()
	{
		if (base.IsOpened)
		{
			InitGui();
		}
		else
		{
			ScreenManager.GuiComposers.Dispose("mainmenu-mods");
		}
	}

	public override void OnScreenLoaded()
	{
		if (ingoreLoadOnce)
		{
			ingoreLoadOnce = false;
			return;
		}
		InitGui();
		Search();
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
	}

	private void Search()
	{
		modDbUtil.Search(searchText, onDone, Array.Empty<int>(), null, "trendingpoints", 25);
	}

	private List<ModCellEntry> LoadModCells(ModDbEntrySearchResponse[] mods)
	{
		List<ModCellEntry> list = new List<ModCellEntry>();
		foreach (ModDbEntrySearchResponse modDbEntrySearchResponse in mods)
		{
			CairoFont cairoFont = CairoFont.WhiteDetailText();
			cairoFont.WithFontSize((float)GuiStyle.SmallFontSize);
			if (!(modDbEntrySearchResponse.Type != "mod"))
			{
				list.Add(new ModCellEntry
				{
					Title = modDbEntrySearchResponse.Name,
					RightTopText = modDbEntrySearchResponse.Downloads + " downloads",
					RightTopOffY = 3f,
					DetailText = modDbEntrySearchResponse.Author,
					Enabled = false,
					DetailTextFont = cairoFont
				});
			}
		}
		return list;
	}

	private void onDone(ModSearchResult searchResult)
	{
		if (searchResult.Mods == null)
		{
			modCells = new List<ModCellEntry>
			{
				new ModCellEntry
				{
					Title = searchResult.StatusMessage
				}
			};
		}
		else
		{
			modCells = LoadModCells(searchResult.Mods);
		}
		ElementComposer.GetCellList<ModCellEntry>("modstable").ReloadCells(modCells);
	}

	private void InitGui()
	{
		int height = ScreenManager.GamePlatform.WindowSize.Height;
		int width = ScreenManager.GamePlatform.WindowSize.Width;
		ElementBounds elementBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 35.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 0.0, 200.0, 30.0).FixedUnder(elementBounds, 10.0);
		ElementBounds elementBounds3 = ElementBounds.FixedSize(60.0, 30.0).WithFixedPadding(10.0, 2.0).WithAlignment(EnumDialogArea.RightFixed);
		ElementBounds elementBounds4 = elementBounds2.BelowCopy(0.0, 10.0).WithFixedSize(Math.Max(400.0, (double)width * 0.5) / (double)ClientSettings.GUIScale, (float)Math.Max(300, height) / ClientSettings.GUIScale - 145f);
		_ = Math.Max(400.0, (double)width * 0.5) / (double)ClientSettings.GUIScale;
		ElementComposer?.Dispose();
		ElementComposer = dialogBase("mainmenu-onlinemods").AddStaticText(Lang.Get("All mods from the VS Mod DB (work in progress)"), CairoFont.WhiteSmallishText(), elementBounds).AddTextInput(elementBounds2, null, null, "search").AddSmallButton(Lang.Get("Search"), OnSearch, elementBounds3.FlatCopy().FixedUnder(elementBounds, 10.0).FixedRightOf(elementBounds2, 10.0)
			.WithAlignment(EnumDialogArea.LeftFixed))
			.AddInset(elementBounds4)
			.AddVerticalScrollbar(OnNewScrollbarvalue, ElementStdBounds.VerticalScrollbar(elementBounds4), "scrollbar")
			.BeginClip(clippingBounds = elementBounds4.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
			.AddCellList(listBounds = clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(10.0), createCellElem, modCells, "modstable")
			.EndClip()
			.AddSmallButton(Lang.Get("Back"), OnBack, elementBounds3.FlatCopy().FixedUnder(elementBounds4, 10.0).WithAlignment(EnumDialogArea.LeftFixed))
			.AddSmallButton(Lang.Get("Install"), OnInstall, elementBounds3.FlatCopy().FixedUnder(elementBounds4, 10.0).WithFixedAlignmentOffset(-13.0, 0.0))
			.EndChildElements()
			.Compose();
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
	}

	private bool OnInstall()
	{
		return true;
	}

	private bool OnBack()
	{
		ScreenManager.LoadScreen(ParentScreen);
		return true;
	}

	private bool OnSearch()
	{
		searchText = ElementComposer.GetTextInput("search").GetText();
		Search();
		return true;
	}

	private IGuiElementCell createCellElem(ModCellEntry cell, ElementBounds bounds)
	{
		return new GuiElementModCell(ScreenManager.api, cell, bounds, null)
		{
			On = cell.Enabled
		};
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetCellList<ModCellEntry>("modstable").Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
	}
}
