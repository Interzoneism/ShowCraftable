using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class GuiScreenSingleplayer : GuiScreen
{
	private SaveGameEntry[] entries;

	private int lastClickedCellIndex;

	private ElementBounds listBounds;

	private ElementBounds clippingBounds;

	public GuiScreenSingleplayer(ScreenManager screenManager, GuiScreen parent)
		: base(screenManager, parent)
	{
		ShowMainMenu = true;
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
			ScreenManager.GuiComposers.Dispose("mainmenu-singleplayer");
		}
	}

	private void InitGui()
	{
		List<SavegameCellEntry> list = LoadSaveGameCells();
		int width = ScreenManager.GamePlatform.WindowSize.Width;
		int height = ScreenManager.GamePlatform.WindowSize.Height;
		ElementBounds elementBounds = ElementBounds.FixedSize(60.0, 30.0).WithFixedPadding(10.0, 2.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedAlignmentOffset(-13.0, 0.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 35.0);
		_ = Math.Max(400.0, (double)width * 0.5) / (double)ClientSettings.GUIScale;
		ElementBounds elementBounds3;
		ElementComposer = dialogBase("mainmenu-singleplayer").AddStaticText(Lang.Get("singleplayer-worlds"), CairoFont.WhiteSmallishText(), elementBounds2).AddInset(elementBounds3 = elementBounds2.BelowCopy(0.0, 3.0).WithFixedSize(Math.Max(400.0, (double)width * 0.5) / (double)ClientSettings.GUIScale, (float)Math.Max(300, height) / ClientSettings.GUIScale - 205f)).AddVerticalScrollbar(OnNewScrollbarvalue, ElementStdBounds.VerticalScrollbar(elementBounds3), "scrollbar")
			.BeginClip(clippingBounds = elementBounds3.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
			.AddCellList(listBounds = clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0), createCellElem, list, "worldstable")
			.EndClip();
		GuiElementCellList<SavegameCellEntry> cellList = ElementComposer.GetCellList<SavegameCellEntry>("worldstable");
		cellList.BeforeCalcBounds();
		for (int i = 0; i < list.Count; i++)
		{
			ElementBounds elementBounds4 = cellList.elementCells[i].Bounds.ForkChild();
			cellList.elementCells[i].Bounds.ChildBounds.Add(elementBounds4);
			elementBounds4.fixedWidth -= 56.0;
			elementBounds4.fixedY = -3.0;
			elementBounds4.fixedX -= 6.0;
			elementBounds4.fixedHeight -= 2.0;
			ElementComposer.AddHoverText(list[i].Title + "\r\n" + list[i].HoverText, CairoFont.WhiteDetailText(), 320, elementBounds4, "hover-" + i);
			ElementComposer.GetHoverText("hover-" + i).InsideClipBounds = clippingBounds;
		}
		ElementComposer.AddIf(list.Count == 0).AddStaticText(Lang.Get("singleplayer-noworldsfound"), CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center), elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(elementBounds.fixedOffsetX, -30.0)).AddButton(Lang.Get("singleplayer-newworld"), OnNewWorld, elementBounds.FlatCopy().WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(elementBounds.fixedOffsetX, 30.0))
			.EndIf()
			.AddButton(Lang.Get("Open Saves Folder"), OnOpenSavesFolder, elementBounds.FlatCopy().FixedUnder(elementBounds3, 10.0).WithAlignment(EnumDialogArea.LeftFixed)
				.WithFixedAlignmentOffset(0.0, 0.0))
			.AddButton(Lang.Get("singleplayer-newworld"), OnNewWorld, elementBounds.FixedUnder(elementBounds3, 10.0))
			.EndChildElements()
			.Compose();
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
	}

	private IGuiElementCell createCellElem(SavegameCellEntry cell, ElementBounds bounds)
	{
		return new GuiElementMainMenuCell(ScreenManager.api, cell, bounds)
		{
			cellEntry = 
			{
				DetailTextOffY = 0.0,
				LeftOffY = -2f
			},
			OnMouseDownOnCellLeft = OnClickCellLeft,
			OnMouseDownOnCellRight = OnClickCellRight
		};
	}

	public override void OnScreenLoaded()
	{
		InitGui();
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
	}

	private bool OnOpenSavesFolder()
	{
		NetUtil.OpenUrlInBrowser(GamePaths.Saves);
		return true;
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetCellList<SavegameCellEntry>("worldstable").Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
	}

	private void OnClickCellRight(int cellIndex)
	{
		lastClickedCellIndex = cellIndex;
		if (entries[cellIndex].IsReadOnly)
		{
			ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Have no write access to this file, it seems in use. Make sure no other client or server is currently using this savegame."), delegate
			{
				ScreenManager.LoadScreen(this);
			}, ScreenManager, this, onlyCancel: true));
		}
		else
		{
			ScreenManager.LoadScreen(new GuiScreenSingleplayerModify(entries[cellIndex].Filename, ScreenManager, this));
		}
	}

	private void OnClickCellLeft(int cellIndex)
	{
		lastClickedCellIndex = cellIndex;
		if (entries[cellIndex].Savegame == null)
		{
			ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("savegame-corrupt-confirmrepair"), OnConfirmRepairMode, ScreenManager, this));
		}
		else
		{
			if (entries[cellIndex].Savegame.HighestChunkdataVersion > 2)
			{
				return;
			}
			if (entries[cellIndex].DatabaseVersion != GameVersion.DatabaseVersion)
			{
				ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("This world uses an old file format that needs upgrading. This might take a while. It is also suggested to first back up your savegame in case the upgrade fails. Proceed?"), OnDidConfirmUpgrade, ScreenManager, this));
				return;
			}
			if (entries[cellIndex].IsReadOnly)
			{
				ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Have no write access to this file, it seems in use. Make sure no other client or server is currently using this savegame."), delegate
				{
					ScreenManager.LoadScreen(this);
				}, ScreenManager, this, onlyCancel: true));
				return;
			}
			ScreenManager.ConnectToSingleplayer(new StartServerArgs
			{
				SaveFileLocation = entries[cellIndex].Filename,
				DisabledMods = ClientSettings.DisabledMods,
				Language = ClientSettings.Language
			});
		}
	}

	private void OnConfirmRepairMode(bool confirm)
	{
		if (confirm)
		{
			ScreenManager.ConnectToSingleplayer(new StartServerArgs
			{
				SaveFileLocation = entries[lastClickedCellIndex].Filename,
				DisabledMods = ClientSettings.DisabledMods,
				Language = ClientSettings.Language,
				RepairMode = true
			});
		}
		else
		{
			ScreenManager.LoadScreen(this);
		}
	}

	private void OnDidConfirmUpgrade(bool confirm)
	{
		if (confirm)
		{
			ScreenManager.ConnectToSingleplayer(new StartServerArgs
			{
				SaveFileLocation = entries[lastClickedCellIndex].Filename,
				DisabledMods = ClientSettings.DisabledMods,
				Language = ClientSettings.Language
			});
		}
		else
		{
			ScreenManager.LoadScreen(this);
		}
	}

	private bool OnNewWorld()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayerNewWorld));
		return true;
	}

	public override bool OnBackPressed()
	{
		ScreenManager.StartMainMenu();
		return true;
	}

	public bool OnCancel()
	{
		OnBackPressed();
		return true;
	}

	private List<SavegameCellEntry> LoadSaveGameCells()
	{
		List<SavegameCellEntry> list = new List<SavegameCellEntry>();
		LoadSaveGames();
		for (int i = 0; i < entries.Length; i++)
		{
			SaveGameEntry saveGameEntry = entries[i];
			SavegameCellEntry item;
			if (saveGameEntry.Savegame == null)
			{
				item = new SavegameCellEntry
				{
					Title = new FileInfo(saveGameEntry.Filename).Name,
					DetailText = (saveGameEntry.IsReadOnly ? Lang.Get("Unable to load savegame and no write access, likely already opened elsewhere.") : Lang.Get("Invalid or corrupted savegame")),
					TitleFont = CairoFont.WhiteSmallishText().WithColor(GuiStyle.ErrorTextColor),
					DetailTextFont = CairoFont.WhiteSmallText().WithColor(GuiStyle.ErrorTextColor)
				};
			}
			else if (saveGameEntry.Savegame.HighestChunkdataVersion > 2)
			{
				item = new SavegameCellEntry
				{
					Title = new FileInfo(saveGameEntry.Filename).Name,
					DetailText = Lang.Get("versionmismatch-chunk"),
					TitleFont = CairoFont.WhiteSmallishText().WithColor(GuiStyle.ErrorTextColor),
					DetailTextFont = CairoFont.WhiteSmallText().WithColor(GuiStyle.ErrorTextColor)
				};
			}
			else
			{
				bool flag = GameVersion.IsNewerVersionThan(saveGameEntry.Savegame.LastSavedGameVersion, "1.21.0");
				SavegameCellEntry savegameCellEntry = new SavegameCellEntry();
				savegameCellEntry.Title = saveGameEntry.Savegame.WorldName;
				savegameCellEntry.DetailText = string.Format("{0}, {1}{2}{3}", (saveGameEntry.Savegame.PlayStyleLangCode == null) ? Lang.Get("playstyle-" + saveGameEntry.Savegame.PlayStyle) : Lang.Get("playstyle-" + saveGameEntry.Savegame.PlayStyleLangCode), Lang.Get("Time played: {0}", PrettyTime(saveGameEntry.Savegame.TotalSecondsPlayed)), (saveGameEntry.DatabaseVersion != GameVersion.DatabaseVersion) ? ("\nRequires file format upgrade (DB v" + saveGameEntry.DatabaseVersion + ")") : "", flag ? ("\n" + Lang.Get("versionmismatch-savegame")) : "");
				savegameCellEntry.HoverText = getHoverText(saveGameEntry.Savegame);
				item = savegameCellEntry;
			}
			list.Add(item);
		}
		return list;
	}

	private string getHoverText(SaveGame savegame)
	{
		ITreeAttribute worldConfiguration = savegame.WorldConfiguration;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ModContainer verifiedMod in ScreenManager.verifiedMods)
		{
			ModWorldConfiguration worldConfig = verifiedMod.WorldConfig;
			if (worldConfig == null)
			{
				continue;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = worldConfig.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute worldConfigurationAttribute in worldConfigAttributes)
			{
				WorldConfigurationValue worldConfigurationValue = new WorldConfigurationValue();
				worldConfigurationValue.Attribute = worldConfigurationAttribute;
				worldConfigurationValue.Code = worldConfigurationAttribute.Code;
				PlayStyle playStyle = null;
				PlayStyle[] playStyles = verifiedMod.WorldConfig.PlayStyles;
				foreach (PlayStyle playStyle2 in playStyles)
				{
					if (playStyle2.Code == savegame.PlayStyle)
					{
						playStyle = playStyle2;
						break;
					}
				}
				string text = worldConfigurationAttribute.Default.ToLowerInvariant();
				if (playStyle != null && playStyle.WorldConfig[worldConfigurationValue.Code].Exists)
				{
					text = playStyle.WorldConfig[worldConfigurationValue.Code].ToString();
				}
				IAttribute attribute = worldConfiguration[worldConfigurationValue.Code];
				if (attribute != null && attribute.ToString().ToLowerInvariant() != text)
				{
					stringBuilder.AppendLine("<font opacity=\"0.6\">" + Lang.Get("worldattribute-" + worldConfigurationAttribute.Code) + ":</font> " + worldConfigurationAttribute.valueToHumanReadable(attribute.ToString()));
				}
			}
		}
		if (savegame.MapSizeY != 256)
		{
			stringBuilder.Append(Lang.Get("worldconfig-worldheight", savegame.MapSizeY));
		}
		if (stringBuilder.Length == 0)
		{
			stringBuilder.Append("<font opacity=\"0.6\"><i>" + Lang.Get("No custom configurations") + "</i></font>");
		}
		else
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("<font opacity=\"0.6\"><i>" + Lang.Get("All other configurations are default values") + "</i></font>");
		}
		return stringBuilder.ToString();
	}

	public static string PrettyTime(int seconds)
	{
		if (seconds < 60)
		{
			return Lang.Get("{0} seconds", seconds);
		}
		if (seconds < 3600)
		{
			return Lang.Get("{0} minutes, {1} seconds", seconds / 60, seconds - seconds / 60 * 60);
		}
		int num = seconds / 3600;
		int num2 = seconds / 60 - num * 60;
		return Lang.Get("{0} hours, {1} minutes", num, num2);
	}

	internal string[] GetFilenames()
	{
		string[] files = Directory.GetFiles(GamePaths.Saves);
		List<string> list = new List<string>();
		for (int i = 0; i < files.Length; i++)
		{
			if (files[i].EndsWithOrdinal(".vcdbs"))
			{
				list.Add(files[i]);
			}
		}
		return list.ToArray();
	}

	private void LoadSaveGames()
	{
		string[] filenames = GetFilenames();
		List<SaveGameEntry> list = new List<SaveGameEntry>();
		GameDatabase gameDatabase = new GameDatabase(ScreenManager.GamePlatform.Logger);
		for (int i = 0; i < filenames.Length; i++)
		{
			int foundVersion = 0;
			bool isReadonly = true;
			SaveGame saveGame = null;
			try
			{
				saveGame = gameDatabase.ProbeOpenConnection(filenames[i], corruptionProtection: false, out foundVersion, out isReadonly);
				saveGame?.LoadWorldConfig();
				gameDatabase.CloseConnection();
			}
			catch (Exception)
			{
			}
			SaveGameEntry item = new SaveGameEntry
			{
				DatabaseVersion = foundVersion,
				Savegame = saveGame,
				Filename = filenames[i],
				IsReadOnly = isReadonly,
				Modificationdate = File.GetLastWriteTime(filenames[i])
			};
			list.Add(item);
		}
		list.Sort((SaveGameEntry entry1, SaveGameEntry entry2) => entry2.Modificationdate.CompareTo(entry1.Modificationdate));
		entries = list.ToArray();
	}
}
