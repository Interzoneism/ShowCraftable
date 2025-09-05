using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class GuiScreenSingleplayerNewWorld : GuiScreen
{
	private int selectedPlaystyleIndex;

	protected static bool allowCheats = true;

	protected ElementBounds listBounds;

	protected ElementBounds clippingBounds;

	internal List<PlaystyleListEntry> cells = new List<PlaystyleListEntry>();

	private bool isCustomWorldName;

	private WorldConfig wcu;

	private GuiScreenWorldCustomize customizeScreen;

	private Random rand = new Random();

	public GuiScreenSingleplayerNewWorld(ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		ShowMainMenu = true;
		wcu = new WorldConfig(screenManager.verifiedMods);
		wcu.IsNewWorld = true;
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

	public override void OnScreenLoaded()
	{
		base.OnScreenLoaded();
		InitGui();
	}

	private void invalidate()
	{
		if (base.IsOpened)
		{
			InitGui();
		}
		else
		{
			ScreenManager.GuiComposers.Dispose("mainmenu-singleplayernewworld");
		}
	}

	private void InitGui()
	{
		wcu.mods = ScreenManager.verifiedMods;
		wcu.LoadPlayStyles();
		cells.Clear();
		cells = loadPlaystyleCells();
		if (wcu.PlayStyles.Count > 0)
		{
			cells[0].Selected = true;
			wcu.selectPlayStyle(selectedPlaystyleIndex);
		}
		int width = ScreenManager.GamePlatform.WindowSize.Width;
		int height = ScreenManager.GamePlatform.WindowSize.Height;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 300.0, 30.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 0.0, 300.0, 30.0).FixedRightOf(elementBounds);
		ElementBounds elementBounds3 = null;
		double num = Math.Max(400.0, (double)width * 0.5) / (double)ClientSettings.GUIScale + 40.0;
		ElementComposer = dialogBase("mainmenu-singleplayernewworld").AddStaticText(Lang.Get("singleplayer-newworld"), CairoFont.WhiteSmallishText(), elementBounds.FlatCopy()).AddStaticText(Lang.Get("singleplayer-newworldname"), CairoFont.WhiteSmallishText(), elementBounds = elementBounds.BelowCopy(0.0, 13.0)).AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, 10.0).WithFixedWidth(270.0), null, null, "worldname")
			.AddIconButton("dice", OnPressDice, elementBounds2 = elementBounds2.FlatCopy().FixedRightOf(elementBounds2).WithFixedSize(30.0, 30.0))
			.AddStaticText(Lang.Get("singleplayer-selectplaystyle"), CairoFont.WhiteSmallishText(), elementBounds = elementBounds.BelowCopy(0.0, 13.0))
			.AddInset(elementBounds3 = elementBounds.BelowCopy(0.0, 3.0).WithFixedSize(num - (double)GuiElementScrollbar.DefaultScrollbarWidth - (double)GuiElementScrollbar.DeafultScrollbarPadding - 3.0, (double)((float)Math.Max(300, height) / ClientSettings.GUIScale - 170f) - elementBounds.fixedY - elementBounds.fixedHeight))
			.AddVerticalScrollbar(OnNewScrollbarvalue, ElementStdBounds.VerticalScrollbar(elementBounds3), "scrollbar")
			.BeginClip(clippingBounds = elementBounds3.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
			.AddCellList(listBounds = clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(10.0), createCellElem, cells, "playstylelist")
			.EndClip();
		GuiElementCellList<PlaystyleListEntry> cellList = ElementComposer.GetCellList<PlaystyleListEntry>("playstylelist");
		cellList.BeforeCalcBounds();
		for (int i = 0; i < cells.Count; i++)
		{
			ElementBounds bounds = cellList.elementCells[i].Bounds;
			ElementComposer.AddHoverText(cells[i].HoverText, CairoFont.WhiteDetailText(), 320, bounds, "hovertext-" + i);
			ElementComposer.GetHoverText("hovertext-" + i).InsideClipBounds = clippingBounds;
		}
		ElementComposer.AddButton(Lang.Get("general-back"), OnBack, elementBounds = elementBounds3.BelowCopy(0.0, 10.0).WithFixedSize(100.0, 30.0).WithFixedPadding(5.0, 0.0)).AddButton(Lang.Get("general-customize"), OnCustomize, elementBounds = elementBounds.FlatCopy().WithFixedWidth(200.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedAlignmentOffset(-220.0, 0.0)).AddButton(Lang.Get("general-createworld"), OnCreate, elementBounds = elementBounds.FlatCopy().WithFixedWidth(200.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedAlignmentOffset(0.0, 0.0))
			.EndChildElements()
			.Compose();
		ElementComposer.GetTextInput("worldname").OnKeyPressed = delegate
		{
			isCustomWorldName = true;
		};
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
		updatePlaysStyleSpecificFields();
		if (selectedPlaystyleIndex >= 0)
		{
			for (int num2 = 0; num2 < cells.Count; num2++)
			{
				cells[num2].Selected = false;
			}
			cells[selectedPlaystyleIndex].Selected = true;
		}
		for (int num3 = 0; num3 < cells.Count; num3++)
		{
			string newText = ((selectedPlaystyleIndex != num3) ? wcu.ToRichText(wcu.PlayStyles[num3], withCustomConfigs: false) : wcu.ToRichText(withCustomConfigs: true));
			ElementComposer.GetHoverText("hovertext-" + num3).SetNewText(newText);
		}
	}

	private IGuiElementCell createCellElem(SavegameCellEntry cell, ElementBounds bounds)
	{
		return new GuiElementMainMenuCell(ScreenManager.api, cell, bounds)
		{
			ShowModifyIcons = false,
			cellEntry = 
			{
				DetailTextOffY = 4.0
			},
			OnMouseDownOnCellLeft = OnClickCellLeft
		};
	}

	private void updatePlaysStyleSpecificFields()
	{
		if (!isCustomWorldName)
		{
			ElementComposer.GetTextInput("worldname").SetValue((wcu.CurrentPlayStyle?.Code == "creativebuilding") ? GenRandomCreativeName() : GenRandomSurvivalName());
		}
		if (!isCustomWorldName)
		{
			ElementComposer.GetTextInput("worldname").SetValue((wcu.CurrentPlayStyle?.Code == "creativebuilding") ? GenRandomCreativeName() : GenRandomSurvivalName());
		}
	}

	private List<PlaystyleListEntry> loadPlaystyleCells()
	{
		CairoFont cairoFont = CairoFont.WhiteDetailText();
		cairoFont.WithFontSize((float)GuiStyle.SmallFontSize);
		foreach (PlayStyle playStyle2 in wcu.PlayStyles)
		{
			cells.Add(new PlaystyleListEntry
			{
				Title = Lang.Get("playstyle-" + playStyle2.LangCode),
				DetailText = Lang.Get("playstyle-desc-" + playStyle2.LangCode),
				PlayStyle = playStyle2,
				DetailTextFont = cairoFont,
				HoverText = ""
			});
		}
		if (cells.Count == 0)
		{
			PlayStyle playStyle = new PlayStyle
			{
				Code = "default",
				LangCode = "default",
				WorldConfig = new JsonObject(JToken.Parse("{}")),
				WorldType = "none"
			};
			wcu.PlayStyles.Add(playStyle);
			wcu.selectPlayStyle(0);
			cells.Add(new PlaystyleListEntry
			{
				Title = Lang.Get("noplaystyles-title"),
				DetailText = Lang.Get("noplaystyles-desc"),
				PlayStyle = playStyle,
				DetailTextFont = cairoFont,
				Enabled = true
			});
		}
		return cells;
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetCellList<PlaystyleListEntry>("playstylelist").Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
	}

	private void OnPressDice(bool on)
	{
		ElementComposer.GetTextInput("worldname").SetValue((wcu.CurrentPlayStyle.Code == "creative") ? GenRandomCreativeName() : GenRandomSurvivalName());
		isCustomWorldName = false;
	}

	internal void OnClickCellLeft(int cellIndex)
	{
		wcu.selectPlayStyle(cellIndex);
		foreach (PlaystyleListEntry cell in cells)
		{
			cell.Selected = false;
		}
		cells[cellIndex].Selected = !cells[cellIndex].Selected;
		updatePlaysStyleSpecificFields();
		selectedPlaystyleIndex = cellIndex;
		for (int i = 0; i < cells.Count; i++)
		{
			string newText = ((selectedPlaystyleIndex != i) ? wcu.ToRichText(wcu.PlayStyles[i], withCustomConfigs: false) : wcu.ToRichText(withCustomConfigs: true));
			ElementComposer.GetHoverText("hovertext-" + i).SetNewText(newText);
		}
	}

	public bool OnCustomize()
	{
		if (wcu.CurrentPlayStyle == null)
		{
			return false;
		}
		customizeScreen = new GuiScreenWorldCustomize(OnReturnFromCustomizer, ScreenManager, this, wcu.Clone(), cells);
		ScreenManager.LoadScreen(customizeScreen);
		return true;
	}

	private void OnReturnFromCustomizer(bool didApply)
	{
		if (didApply)
		{
			wcu = customizeScreen.wcu;
		}
		string text = ElementComposer.GetTextInput("worldname").GetText();
		ScreenManager.LoadScreen(this);
		ElementComposer.GetTextInput("worldname").SetValue(text);
	}

	private bool OnCreate()
	{
		if (wcu.CurrentPlayStyle.Code == "creativebuilding")
		{
			if (wcu.MapsizeY > 1024)
			{
				string text = Lang.Get("createworld-creativebuilding-warning-largeworldheight", wcu.MapsizeY);
				ScreenManager.LoadScreen(new GuiScreenConfirmAction(text, OnDidConfirmCreate, ScreenManager, this));
				return true;
			}
		}
		else
		{
			if (wcu.MapsizeY > 384)
			{
				string text2 = Lang.Get("createworld-surviveandbuild-warning-largeworldheight", wcu.MapsizeY);
				ScreenManager.LoadScreen(new GuiScreenConfirmAction(text2, OnDidConfirmCreate, ScreenManager, this));
				return true;
			}
			if (wcu.MapsizeY < 256)
			{
				string text2 = Lang.Get("createworld-surviveandbuild-warning-smallworldheight", wcu.MapsizeY);
				ScreenManager.LoadScreen(new GuiScreenConfirmAction(text2, OnDidConfirmCreate, ScreenManager, this));
				return true;
			}
		}
		CreateWorld();
		return true;
	}

	private void OnDidConfirmCreate(bool confirm)
	{
		if (confirm)
		{
			CreateWorld();
		}
		else
		{
			ScreenManager.LoadScreen(this);
		}
	}

	private void CreateWorld()
	{
		string text = ElementComposer.GetTextInput("worldname").GetText();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = ((wcu.CurrentPlayStyle?.Code == "creativebuilding") ? GenRandomCreativeName() : GenRandomSurvivalName());
		}
		string text2 = Regex.Replace(text.ToLowerInvariant(), "[^\\w\\d0-9_\\- ]+", "");
		string path = text2;
		int num = 2;
		while (File.Exists(Path.Combine(GamePaths.Saves, path) + ".vcdbs"))
		{
			path = text2 + "-" + num;
			num++;
		}
		PlayStyle currentPlayStyle = wcu.CurrentPlayStyle;
		StartServerArgs serverargs = new StartServerArgs
		{
			AllowCreativeMode = allowCheats,
			PlayStyle = currentPlayStyle.Code,
			PlayStyleLangCode = currentPlayStyle.LangCode,
			WorldType = currentPlayStyle.WorldType,
			WorldName = text,
			WorldConfiguration = wcu.Jworldconfig,
			SaveFileLocation = Path.Combine(GamePaths.Saves, path) + ".vcdbs",
			Seed = wcu.Seed,
			MapSizeY = wcu.MapsizeY,
			CreatedByPlayerName = ClientSettings.PlayerName,
			DisabledMods = ClientSettings.DisabledMods,
			Language = ClientSettings.Language
		};
		ScreenManager.ConnectToSingleplayer(serverargs);
	}

	private bool OnBack()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
		return true;
	}

	public string GenRandomSurvivalName()
	{
		string text = ClientSettings.PlayerName;
		if (text == null)
		{
			text = "Tyron";
		}
		text = (text.EndsWith('s') ? (text + "'") : (text + "s"));
		string[] array = new string[11]
		{
			text, text, "Vintage", "Awesome", "Dark", "Serene", "Creepy", "Gloomy", "Peaceful", "Foggy",
			"Sunny"
		};
		string[] array2 = new string[5] { "Adventure", "Cave", "Kingdom", "Village", "Hermit" };
		string[] array3 = new string[5] { "Tales", "Valley", "Lands", "Story", "World" };
		return array[rand.Next(array.Length)] + " " + array2[rand.Next(array2.Length)] + " " + array3[rand.Next(array3.Length)];
	}

	public string GenRandomCreativeName()
	{
		string text = ClientSettings.PlayerName;
		if (text == null)
		{
			text = "Tyron";
		}
		text = (text.EndsWith('s') ? (text + "'") : (text + "s"));
		string[] array = new string[11]
		{
			text, text, "Vintage", "Massive", "Dark", "Serene", "Epic", "Gloomy", "Peaceful", "Foggy",
			"Sunny"
		};
		string[] array2 = new string[5] { "Test", "Superflat", "Creative", "Freestyle", "Doodle" };
		string[] array3 = new string[4] { "Place", "Lands", "Story", "World" };
		return array[rand.Next(array.Length)] + " " + array2[rand.Next(array2.Length)] + " " + array3[rand.Next(array3.Length)];
	}
}
