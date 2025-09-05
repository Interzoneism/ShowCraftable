using System;
using System.IO;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class GuiScreenSingleplayerModify : GuiScreen
{
	private string worldfilename;

	private int worldSeed;

	private string playstylelangcode;

	private GameDatabase gamedb;

	private bool valuesChanged;

	private WorldConfig wcu;

	private GuiScreenWorldCustomize customizeScreen;

	public GuiScreenSingleplayerModify(string worldfilename, ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		ShowMainMenu = true;
		this.worldfilename = worldfilename;
	}

	private SaveGame getSaveGame(out int version, out bool isreadonly, bool keepOpen = false)
	{
		if (gamedb != null)
		{
			gamedb.Dispose();
		}
		gamedb = new GameDatabase(ScreenManager.GamePlatform.Logger);
		string errorMessage;
		SaveGame saveGame = gamedb.ProbeOpenConnection(worldfilename, corruptionProtection: false, out version, out errorMessage, out isreadonly);
		saveGame?.LoadWorldConfig();
		if (!keepOpen)
		{
			gamedb.CloseConnection();
		}
		return saveGame;
	}

	public void initGui(SaveGame savegame)
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		if (!valuesChanged)
		{
			wcu = new WorldConfig(ScreenManager.verifiedMods);
			wcu.loadFromSavegame(savegame);
		}
		wcu.updateJWorldConfig();
		if (savegame != null)
		{
			worldSeed = savegame.Seed;
			playstylelangcode = savegame.PlayStyleLangCode;
			ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 330.0, 80.0);
			ElementBounds elementBounds2 = elementBounds.BelowCopy().WithFixedHeight(35.0);
			TextExtents textExtents = CairoFont.ButtonText().GetTextExtents(Lang.Get("Save"));
			double num = ((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 40.0;
			textExtents = CairoFont.ButtonText().GetTextExtents(Lang.Get("Customize"));
			double num2 = ((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 40.0;
			string text = Lang.Get("Create a new world with this world seed");
			textExtents = CairoFont.WhiteSmallText().GetTextExtents(text);
			double num3 = ((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 40.0;
			string text2 = ((savegame.PlayStyleLangCode == null) ? Lang.Get("playstyle-" + savegame.PlayStyle) : Lang.Get("playstyle-" + savegame.PlayStyleLangCode));
			GuiComposer composer = dialogBase("mainmenu-singleplayermodifyworld", -1.0, 550.0).AddStaticText(Lang.Get("Modify World"), CairoFont.WhiteSmallishText(), elementBounds).AddStaticText(Lang.Get("World name"), CairoFont.WhiteSmallishText(), elementBounds2).AddTextInput(elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedOffset(0.0, -3.0)
				.WithFixedSize(470.0, 30.0), null, null, "worldname")
				.AddStaticText(Lang.Get("Filename on disk"), CairoFont.WhiteSmallishText(), elementBounds2 = elementBounds2.BelowCopy())
				.AddTextInput(elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedOffset(0.0, -3.0)
					.WithFixedSize(470.0, 30.0), null, null, "filename")
				.AddStaticText(Lang.Get("Seed"), CairoFont.WhiteSmallishText(), elementBounds2 = elementBounds2.BelowCopy(0.0, 4.0))
				.AddStaticText(worldSeed.ToString() ?? "", CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0))
				.AddIconButton("copy", OnCopySeed, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0))
				.AddHoverText(Lang.Get("Copies the seed to your clipboard"), CairoFont.WhiteDetailText(), 200, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0)
					.WithFixedPadding(5.0))
				.AddStaticText(Lang.Get("Total Time Played"), CairoFont.WhiteSmallishText(), elementBounds2 = elementBounds2.BelowCopy())
				.AddStaticText(GuiScreenSingleplayer.PrettyTime(savegame.TotalSecondsPlayed) ?? "", CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0))
				.AddStaticText(Lang.Get("Created with"), CairoFont.WhiteSmallishText(), elementBounds2 = elementBounds2.BelowCopy())
				.AddStaticText(Lang.Get("versionnumber", savegame.CreatedGameVersion), CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0))
				.AddStaticText(Lang.GetWithFallback("singleplayer-world-creator", "Created by"), CairoFont.WhiteSmallishText(), elementBounds2 = elementBounds2.BelowCopy())
				.AddStaticText(savegame.CreatedByPlayerName, CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0))
				.AddStaticText(Lang.Get("Playstyle"), CairoFont.WhiteSmallishText(), elementBounds2 = elementBounds2.BelowCopy())
				.AddStaticText(text2, CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0))
				.AddIconButton("copy", OnCopyPlaystyle, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0))
				.AddHoverText(Lang.Get("Copies the playstyle to your clipboard"), CairoFont.WhiteDetailText(), 200, elementBounds2.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0)
					.WithFixedPadding(5.0));
			string text3 = Lang.Get("World Size");
			CairoFont font = CairoFont.WhiteSmallishText();
			ElementBounds elementBounds3 = elementBounds2.BelowCopy();
			ElementComposer = GuiComposerHelpers.AddStaticText(bounds: elementBounds3.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0), composer: composer.AddStaticText(text3, font, elementBounds3), text: savegame.MapSizeX + "x" + savegame.MapSizeY + "x" + savegame.MapSizeZ, font: CairoFont.WhiteSmallishText(), orientation: EnumTextOrientation.Left).AddButton(Lang.Get("Back"), OnCancel, ElementStdBounds.Rowed(5.8f, 0.0, EnumDialogArea.LeftFixed).WithFixedAlignmentOffset(0.0, 0.0).WithFixedPadding(10.0, 2.0)).AddButton(Lang.Get("Delete"), OnDelete, ElementStdBounds.Rowed(5.8f, 0.0, EnumDialogArea.RightFixed).WithFixedAlignmentOffset(0.0 - num - num2, 0.0).WithFixedPadding(10.0, 2.0))
				.AddButton(Lang.Get("Customize"), OnCustomize, ElementStdBounds.Rowed(5.8f, 0.0, EnumDialogArea.RightFixed).WithFixedAlignmentOffset(0.0 - num, 0.0).WithFixedPadding(10.0, 2.0))
				.AddButton(Lang.Get("Save"), OnSave, ElementStdBounds.Rowed(5.8f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
				.AddSmallButton(Lang.Get("Create a backup"), OnCreateBackup, ElementStdBounds.Rowed(6.8f, 0.0, EnumDialogArea.RightFixed).WithFixedAlignmentOffset(0.0 - num3 - 20.0, 0.0).WithFixedPadding(10.0, 3.0))
				.AddSmallButton(text, OnNewWorldWithSeed, ElementStdBounds.Rowed(6.8f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 3.0))
				.AddSmallButton(Lang.Get("Run in Repair mode"), OnRunInRepairMode, ElementStdBounds.Rowed(7.5f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 3.0))
				.AddDynamicText("", CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(8.5f, 0.0).WithFixedSize(400.0, 30.0), "dyntextbottom")
				.EndChildElements()
				.Compose();
			ElementComposer.GetTextInput("worldname").SetValue(savegame.WorldName);
			FileInfo fileInfo = new FileInfo(worldfilename);
			ElementComposer.GetTextInput("filename").SetValue(fileInfo.Name);
		}
		else
		{
			ElementComposer = dialogBase("mainmenu-singleplayermodifyworld", -1.0, 550.0).AddStaticText(Lang.Get("singleplayer-modify"), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(0f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0)).AddStaticText(Lang.Get("singleplayer-corrupt", worldfilename), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(1f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0)).AddButton(Lang.Get("general-cancel"), OnCancel, ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0).WithFixedAlignmentOffset(-10.0, 0.0))
				.AddButton(Lang.Get("general-delete"), OnDelete, ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
				.Compose()
				.EndChildElements();
		}
	}

	private void OnCopyPlaystyle(bool ok)
	{
		ScreenManager.Platform.XPlatInterface.SetClipboardText(wcu.ToJson());
	}

	public bool OnCreateBackup()
	{
		ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Create back up now?"), OnDidBackup, ScreenManager, this));
		return true;
	}

	private void OnDidBackup(bool ok)
	{
		if (ok)
		{
			FileInfo fileInfo = new FileInfo(worldfilename);
			string path = string.Concat(str2: $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}", str0: fileInfo.Name.Replace(".vcdbs", ""), str1: "-bkp-", str3: ".vcdbs");
			File.Copy(worldfilename, Path.Combine(GamePaths.BackupSaves, path));
			ScreenManager.LoadScreen(this);
			ElementComposer.GetDynamicText("dyntextbottom").SetNewText(Lang.Get("Ok, backup created"));
		}
		else
		{
			ScreenManager.LoadScreen(this);
		}
	}

	public override void OnScreenLoaded()
	{
		base.OnScreenLoaded();
		initGui(getSaveGame(out var _, out var _));
	}

	private bool OnCustomize()
	{
		ScreenManager.LoadScreen(customizeScreen = new GuiScreenWorldCustomize(OnReturnFromCustomizer, ScreenManager, this, wcu.Clone(), null));
		return true;
	}

	private void OnReturnFromCustomizer(bool didApply)
	{
		if (didApply)
		{
			wcu = customizeScreen.wcu;
			valuesChanged = true;
		}
		ScreenManager.LoadScreen(this);
	}

	private void OnCopySeed(bool copy)
	{
		ScreenManager.Platform.XPlatInterface.SetClipboardText(worldSeed.ToString() ?? "");
	}

	private bool OnNewWorldWithSeed()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayerNewWorld));
		GuiScreenSingleplayerNewWorld guiScreenSingleplayerNewWorld = ScreenManager.CurrentScreen as GuiScreenSingleplayerNewWorld;
		int num = 0;
		foreach (PlaystyleListEntry cell in guiScreenSingleplayerNewWorld.cells)
		{
			if (cell.PlayStyle.LangCode == playstylelangcode)
			{
				guiScreenSingleplayerNewWorld.OnClickCellLeft(num);
				break;
			}
			num++;
		}
		guiScreenSingleplayerNewWorld.OnCustomize();
		if (!(ScreenManager.CurrentScreen is GuiScreenWorldCustomize guiScreenWorldCustomize))
		{
			return false;
		}
		guiScreenWorldCustomize.ElementComposer.GetTextInput("worldseed").SetValue(worldSeed.ToString() ?? "");
		return true;
	}

	private bool OnRunInRepairMode()
	{
		ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("confirm-repairmode"), delegate(bool val)
		{
			if (val)
			{
				repairGame();
			}
			else
			{
				ScreenManager.LoadScreen(this);
			}
		}, ScreenManager, this));
		return true;
	}

	private void repairGame()
	{
		getSaveGame(out var version, out var isreadonly);
		if (version != GameVersion.DatabaseVersion)
		{
			ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("This world uses an old file format that needs upgrading. This might take a while. It is also suggested to first back up your savegame in case the upgrade fails. Proceed?"), OnDidConfirmUpgrade, ScreenManager, this));
			return;
		}
		if (isreadonly)
		{
			ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Have no write access to this file, it seems in use. Make sure no other client or server is currently using this savegame."), delegate
			{
				ScreenManager.LoadScreen(this);
			}, ScreenManager, this, onlyCancel: true));
			return;
		}
		ScreenManager.ConnectToSingleplayer(new StartServerArgs
		{
			SaveFileLocation = worldfilename,
			DisabledMods = ClientSettings.DisabledMods,
			Language = ClientSettings.Language,
			RepairMode = true
		});
	}

	private void OnDidConfirmUpgrade(bool confirm)
	{
		if (confirm)
		{
			ScreenManager.ConnectToSingleplayer(new StartServerArgs
			{
				SaveFileLocation = worldfilename,
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

	private bool OnSave()
	{
		FileInfo fileInfo = new FileInfo(worldfilename);
		string text = ElementComposer.GetTextInput("filename").GetText();
		if (fileInfo.Name != text)
		{
			try
			{
				fileInfo.MoveTo(worldfilename = Path.Combine(fileInfo.DirectoryName, text));
			}
			catch (Exception)
			{
			}
		}
		if (!gamedb.OpenConnection(fileInfo.FullName, out var errorMessage, corruptionProtection: false, doIntegrityCheck: false))
		{
			ScreenManager.LoadScreen(new GuiScreenMessage(Lang.Get("singleplayer-failedchanges"), Lang.Get("singleplayer-maybecorrupt", errorMessage), OnMessageConfirmed, ScreenManager, this));
			return true;
		}
		int version;
		bool isreadonly;
		SaveGame saveGame = getSaveGame(out version, out isreadonly, keepOpen: true);
		saveGame.WorldName = ElementComposer.GetTextInput("worldname").GetText();
		if (valuesChanged)
		{
			foreach (string item in wcu.WorldConfigsPlaystyle.Keys.ToList().Concat(wcu.WorldConfigsCustom.Keys))
			{
				WorldConfigurationValue worldConfigurationValue = wcu[item];
				if (worldConfigurationValue != null)
				{
					switch (worldConfigurationValue.Attribute.DataType)
					{
					case EnumDataType.Bool:
						saveGame.WorldConfiguration.SetBool(item, (bool)worldConfigurationValue.Value);
						break;
					case EnumDataType.DoubleInput:
					case EnumDataType.DoubleRange:
						saveGame.WorldConfiguration.SetDouble(item, (double)worldConfigurationValue.Value);
						break;
					case EnumDataType.String:
					case EnumDataType.DropDown:
					case EnumDataType.StringRange:
						saveGame.WorldConfiguration.SetString(item, (string)worldConfigurationValue.Value);
						break;
					case EnumDataType.IntInput:
					case EnumDataType.IntRange:
						saveGame.WorldConfiguration.SetInt(item, (int)worldConfigurationValue.Value);
						break;
					}
				}
			}
			saveGame.WillSave(new FastMemoryStream());
			valuesChanged = false;
		}
		gamedb.StoreSaveGame(saveGame);
		gamedb.CloseConnection();
		gamedb.Dispose();
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
		return true;
	}

	private void OnMessageConfirmed()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
	}

	private bool OnDelete()
	{
		ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Really delete world '{0}'?", worldfilename), OnDidConfirmDelete, ScreenManager, this));
		return true;
	}

	private void OnDidConfirmDelete(bool confirm)
	{
		if (confirm)
		{
			ScreenManager.GamePlatform.XPlatInterface.MoveFileToRecyclebin(worldfilename);
			ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
		}
		else
		{
			ScreenManager.LoadScreen(this);
		}
	}

	private bool OnCancel()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
		return true;
	}
}
