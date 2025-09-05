using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.ModDb;

namespace Vintagestory.Client;

public class GuiScreenMods : GuiScreen
{
	private bool ingoreLoadOnce = true;

	private ElementBounds listBounds;

	private ElementBounds clippingBounds;

	private IAsset warningIcon;

	private ScreenManager screenManager;

	public GuiScreenMods(ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		this.screenManager = screenManager;
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
		warningIcon = ScreenManager.api.Assets.Get(new AssetLocation("textures/icons/warning.svg"));
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
		ElementComposer.GetCellList<ModCellEntry>("modstable").ReloadCells(LoadModCells());
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
	}

	private List<ModCellEntry> LoadModCells()
	{
		List<string> disabledMods = ClientSettings.DisabledMods;
		List<ModCellEntry> list = new List<ModCellEntry>();
		List<ModContainer> allMods = ScreenManager.allMods;
		if (!ClientSettings.DisableModSafetyCheck)
		{
			while (ModDbUtil.ModBlockList == null)
			{
				Thread.Sleep(20);
			}
		}
		foreach (ModContainer item in allMods)
		{
			ModInfo info = item.Info;
			CairoFont cairoFont = CairoFont.WhiteDetailText();
			cairoFont.WithFontSize((float)GuiStyle.SmallFontSize);
			string reason = string.Empty;
			if (!item.Error.HasValue && ModDbUtil.IsModBlocked(item.Info.ModID, item.Info.Version, out reason))
			{
				item.SetError(ModError.Blocked);
			}
			if (item.Error.HasValue)
			{
				string detailText = item.Error switch
				{
					ModError.Loading => Lang.Get("Unable to load mod. Check log files."), 
					ModError.Dependency => (item.MissingDependencies != null) ? ((item.MissingDependencies.Count != 1) ? Lang.Get("Unable to load mod. Requires dependencies {0}", string.Join(", ", item.MissingDependencies.Select((string str) => str.Replace("@", " v")))) : Lang.Get("Unable to load mod. Requires dependency {0}", string.Join(", ", item.MissingDependencies.Select((string str) => str.Replace("@", " v"))))) : Lang.Get("Unable to load mod. A dependency has an error. Make sure they all load correctly."), 
					ModError.Blocked => Lang.Get("modloader-blockedmod", reason), 
					_ => throw new InvalidOperationException(), 
				};
				list.Add(new ModCellEntry
				{
					Title = item.FileName,
					DetailText = detailText,
					Enabled = (!disabledMods.Contains(item.Info?.ModID + "@" + item.Info?.Version) && item.Error != ModError.Blocked),
					Mod = item,
					DetailTextFont = cairoFont
				});
				continue;
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (info.Authors.Count > 0)
			{
				stringBuilder.AppendLine(string.Join(", ", info.Authors));
			}
			if (!string.IsNullOrEmpty(info.Description))
			{
				stringBuilder.AppendLine(info.Description);
			}
			list.Add(new ModCellEntry
			{
				Title = info.Name + " (" + info.Type.ToString() + ")",
				RightTopText = ((!string.IsNullOrEmpty(info.Version)) ? info.Version : "--"),
				RightTopOffY = 3f,
				DetailText = stringBuilder.ToString().Trim(),
				Enabled = !disabledMods.Contains(item.Info.ModID + "@" + item.Info.Version),
				Mod = item,
				DetailTextFont = cairoFont
			});
		}
		return list;
	}

	private void InitGui()
	{
		int height = ScreenManager.GamePlatform.WindowSize.Height;
		int width = ScreenManager.GamePlatform.WindowSize.Width;
		ElementBounds elementBounds = ElementBounds.FixedSize(60.0, 30.0).WithFixedPadding(10.0, 2.0).WithAlignment(EnumDialogArea.RightFixed);
		ElementBounds elementBounds2 = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 35.0);
		_ = Math.Max(400.0, (double)width * 0.5) / (double)ClientSettings.GUIScale;
		float num = (float)Math.Max(300, height) / ClientSettings.GUIScale;
		ElementComposer?.Dispose();
		ElementBounds elementBounds3;
		ElementComposer = dialogBase("mainmenu-mods").AddStaticText(Lang.Get("Installed mods"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 40.0)).AddInset(elementBounds3 = elementBounds2.BelowCopy(0.0, 3.0).WithFixedSize(Math.Max(400.0, (double)width * 0.5) / (double)ClientSettings.GUIScale, num - 190f)).AddVerticalScrollbar(OnNewScrollbarvalue, ElementStdBounds.VerticalScrollbar(elementBounds3), "scrollbar")
			.BeginClip(clippingBounds = elementBounds3.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
			.AddCellList(listBounds = clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0), createCellElem, LoadModCells(), "modstable")
			.EndClip()
			.AddSmallButton(Lang.Get("Reload Mods"), OnReloadMods, elementBounds.FlatCopy().FixedUnder(elementBounds3, 10.0).WithFixedAlignmentOffset(-13.0, 0.0))
			.AddSmallButton(Lang.Get("Open Mods Folder"), OnOpenModsFolder, elementBounds.FlatCopy().FixedUnder(elementBounds3, 10.0).WithFixedAlignmentOffset(-150.0, 0.0))
			.EndChildElements()
			.Compose();
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
	}

	private bool OnBrowseOnlineMods()
	{
		ScreenManager.LoadScreen(new GuiScreenOnlineMods(ScreenManager, this));
		return true;
	}

	private IGuiElementCell createCellElem(ModCellEntry cell, ElementBounds bounds)
	{
		GuiElementModCell guiElementModCell = new GuiElementModCell(ScreenManager.api, cell, bounds, warningIcon)
		{
			On = cell.Enabled
		};
		if (cell.Mod.Error != ModError.Blocked)
		{
			guiElementModCell.OnMouseDownOnCellRight = OnClickCellRight;
		}
		guiElementModCell.OnMouseDownOnCellLeft = OnClickCellLeft;
		return guiElementModCell;
	}

	private bool OnReloadMods()
	{
		ScreenManager.loadMods();
		OnScreenLoaded();
		return true;
	}

	private bool OnOpenModsFolder()
	{
		NetUtil.OpenUrlInBrowser(GamePaths.DataPathMods);
		return true;
	}

	private void OnClickCellRight(int cellIndex)
	{
		GuiElementModCell guiElementModCell = (GuiElementModCell)ElementComposer.GetCellList<ModCellEntry>("modstable").elementCells[cellIndex];
		ModContainer mod = guiElementModCell.cell.Mod;
		if (mod.Info != null && mod.Info.CoreMod && mod.Status == ModStatus.Enabled)
		{
			ShowConfirmationDialog(guiElementModCell, mod);
		}
		else
		{
			SwitchModStatus(guiElementModCell, mod);
		}
	}

	private void SwitchModStatus(GuiElementModCell guicell, ModContainer mod)
	{
		guicell.On = !guicell.On;
		if (mod.Status == ModStatus.Enabled || mod.Status == ModStatus.Disabled)
		{
			mod.Status = (guicell.On ? ModStatus.Enabled : ModStatus.Disabled);
		}
		List<string> disabledMods = ClientSettings.DisabledMods;
		if (mod.Info != null)
		{
			disabledMods.Remove(mod.Info.ModID + "@" + mod.Info.Version);
			if (!guicell.On)
			{
				disabledMods.Add(mod.Info.ModID + "@" + mod.Info.Version);
			}
			ClientSettings.DisabledMods = disabledMods;
			ClientSettings.Inst.Save(force: true);
		}
	}

	private void ShowConfirmationDialog(GuiElementModCell guicell, ModContainer mod)
	{
		screenManager.LoadScreen(new GuiScreenConfirmAction("coremod-warningtitle", Lang.Get("coremod-warning", mod.Info.Name), "general-back", "Confirm", delegate(bool val)
		{
			if (val)
			{
				SwitchModStatus(guicell, mod);
			}
			screenManager.LoadScreen(this);
		}, screenManager, this, "coremod-confirmation"));
	}

	private void OnClickCellLeft(int cellIndex)
	{
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetCellList<ModCellEntry>("modstable").Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
	}
}
