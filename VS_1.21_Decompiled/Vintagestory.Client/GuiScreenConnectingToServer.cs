using System;
using System.Collections.Generic;
using System.IO;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Server;

namespace Vintagestory.Client;

internal class GuiScreenConnectingToServer : GuiScreen
{
	protected ClientMain runningGame;

	protected long lastLogfileCheck;

	protected long lastTextUpdate;

	private long lastDotsUpdate;

	private int dotsCount;

	private bool singleplayer;

	private string prevText;

	private long ellapseMs;

	private List<string> _lines;

	protected bool loggerAdded;

	private readonly EnumLogType _logToWatch = EnumLogType.Event;

	private static ILogger Logger => ScreenManager.Platform.Logger;

	public GuiScreenConnectingToServer(bool singleplayer, ScreenManager ScreenManager, GuiScreen parent)
		: base(ScreenManager, parent)
	{
		this.singleplayer = singleplayer;
		_lines = new List<string>();
		if (parent != null)
		{
			runningGame = ((GuiScreenRunningGame)parent).runningGame;
			if (singleplayer)
			{
				if (ClientSettings.DeveloperMode)
				{
					ComposeDeveloperLogDialog("startingspserver", Lang.Get("Launching singleplayer server..."), Lang.Get("Starting server..."));
				}
				else
				{
					_logToWatch = EnumLogType.StoryEvent;
					ComposePlayerLogDialog("startingspserver", Lang.Get("It begins..."));
				}
			}
			else
			{
				ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 900.0, 200.0);
				ElementBounds elementBounds2 = elementBounds.ForkBoundingParent(10.0, 10.0, 10.0, 10.0);
				ElementBounds bounds = elementBounds2.ForkBoundingParent(0.0, 50.0, 0.0, 100.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 280.0);
				ElementComposer?.Dispose();
				ElementComposer = ScreenManager.GuiComposers.Create("connectingtoserver", bounds).BeginChildElements(elementBounds2).AddStaticCustomDraw(ElementBounds.Fill, delegate(Context ctx, ImageSurface surface, ElementBounds elementBounds3)
				{
					GuiElement.RoundRectangle(ctx, elementBounds3.bgDrawX, elementBounds3.bgDrawY, elementBounds3.OuterWidth, elementBounds3.OuterHeight, 1.0);
					ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor);
					ctx.Fill();
				})
					.AddDynamicText(Lang.Get("Connecting to multiplayer server..."), CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center), elementBounds, "centertext")
					.EndChildElements()
					.AddButton(Lang.Get("Cancel"), onCancel, ElementStdBounds.MenuButton(4f).WithFixedPadding(10.0, 4.0), EnumButtonStyle.Normal, "cancelButton")
					.Compose();
				ElementComposer.GetButton("cancelButton").Enabled = true;
			}
		}
		Logger.Debug("GuiScreenConnectingToServer constructed");
	}

	protected void LogAdded(EnumLogType type, string message, object[] args)
	{
		if (type == _logToWatch || type == EnumLogType.Error || type == EnumLogType.Fatal)
		{
			try
			{
				string arg = string.Format(message, args);
				string item = $"{DateTime.Now:d.M.yyyy HH:mm:ss} [{type}] {arg}";
				_lines.Add(item);
			}
			catch (FormatException)
			{
				_lines.Add("Couldn't write to log file, failed formatting " + message + " (FormatException)");
			}
		}
	}

	protected void ComposePlayerLogDialog(string dialogCode, string firstLine)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 900.0, 300.0);
		ElementBounds elementBounds2 = elementBounds.ForkBoundingParent(10.0, 7.0, 10.0, 10.0);
		ElementBounds elementBounds3 = elementBounds.FlatCopy().WithParent(elementBounds2);
		elementBounds3.fixedHeight -= 3.0;
		ElementBounds elementBounds4 = elementBounds2.ForkBoundingParent(0.0, 50.0, 26.0, 80.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 280.0);
		ElementBounds bounds = ElementBounds.Fixed(0.0, -30.0, elementBounds4.fixedWidth, 28.0);
		ElementBounds elementBounds5 = ElementBounds.FixedPos(EnumDialogArea.CenterBottom, 0.0, 0.0).WithFixedPadding(10.0, 2.0);
		ElementBounds bounds2 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 1.0, 10.0, elementBounds2.fixedHeight - 2.0);
		ElementComposer?.Dispose();
		CairoFont cairoFont = CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center);
		cairoFont.Color[3] = 0.65;
		ElementComposer = ScreenManager.GuiComposers.Create(dialogCode, elementBounds4).BeginChildElements(elementBounds2).AddStaticCustomDraw(ElementBounds.Fill, delegate(Context ctx, ImageSurface surface, ElementBounds elementBounds6)
		{
			GuiElement.RoundRectangle(ctx, elementBounds6.bgDrawX, elementBounds6.bgDrawY, elementBounds6.OuterWidth, elementBounds6.OuterHeight, 1.0);
			ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor);
			ctx.Fill();
		})
			.BeginClip(elementBounds3)
			.AddDynamicText(firstLine, CairoFont.WhiteSmallishText(), elementBounds, "centertext")
			.EndClip()
			.AddCompactVerticalScrollbar(OnNewScrollbarBalue, bounds2, "scrollbar")
			.AddDynamicText(Lang.Get("Loading..."), cairoFont, bounds)
			.EndChildElements()
			.AddSmallButton(Lang.Get("Open Logs folder"), onOpenLogs, elementBounds5.BelowCopy(0.0, 50.0), EnumButtonStyle.Normal, "logsButton")
			.AddButton((dialogCode == "startingspserver") ? Lang.Get("Cancel") : Lang.Get("Force quit"), onCancel, elementBounds5, EnumButtonStyle.Normal, "cancelButton")
			.Compose();
		ElementComposer.GetButton("cancelButton").Enabled = true;
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)elementBounds3.fixedHeight, (float)elementBounds.fixedHeight);
	}

	internal void ComposeDeveloperLogDialog(string dialogCode, string titleText, string firstLine)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 900.0, 300.0);
		ElementBounds elementBounds2 = elementBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
		ElementBounds elementBounds3 = elementBounds.FlatCopy().WithParent(elementBounds2);
		elementBounds3.fixedHeight -= 3.0;
		ElementBounds elementBounds4 = elementBounds2.ForkBoundingParent(0.0, 50.0, 26.0, 80.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 280.0);
		ElementBounds bounds = ElementBounds.Fixed(0.0, 0.0, elementBounds4.fixedWidth, 20.0);
		ElementBounds elementBounds5 = ElementBounds.FixedPos(EnumDialogArea.CenterBottom, 0.0, 0.0).WithFixedPadding(10.0, 2.0);
		ElementBounds bounds2 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 50.0, 20.0, elementBounds2.fixedHeight);
		ElementComposer?.Dispose();
		ElementComposer = ScreenManager.GuiComposers.Create(dialogCode, ElementBounds.Fill).AddShadedDialogBG(ElementBounds.Fill, withTitleBar: false).BeginChildElements(elementBounds4)
			.AddStaticText(titleText, CairoFont.WhiteSmallishText(), bounds)
			.AddInset(elementBounds2, 3, 0.8f)
			.BeginClip(elementBounds3)
			.AddDynamicText(firstLine, CairoFont.WhiteSmallishText(), elementBounds, "centertext")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarBalue, bounds2, "scrollbar")
			.AddSmallButton(Lang.Get("Open Logs folder"), onOpenLogs, elementBounds5.BelowCopy(0.0, 50.0), EnumButtonStyle.Normal, "logsButton")
			.AddButton((dialogCode == "startingspserver") ? Lang.Get("Cancel") : Lang.Get("Force quit"), onCancel, elementBounds5, EnumButtonStyle.Normal, "cancelButton")
			.EndChildElements()
			.Compose();
		ElementComposer.GetButton("cancelButton").Enabled = true;
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)elementBounds3.fixedHeight, (float)elementBounds.fixedHeight);
	}

	private void OnNewScrollbarBalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetDynamicText("centertext").Bounds;
		bounds.fixedY = 5f - value;
		bounds.CalcWorldBounds();
	}

	private bool onOpenLogs()
	{
		NetUtil.OpenUrlInBrowser(GamePaths.Logs);
		return true;
	}

	private bool onCancel()
	{
		if (runningGame != null)
		{
			runningGame.DestroyGameSession(gotDisconnected: false);
			runningGame = null;
			ScreenManager.GamePlatform.ExitSinglePlayerServer();
		}
		if (singleplayer)
		{
			ScreenManager.LoadAndCacheScreen(typeof(GuiScreenExitingServer));
		}
		else
		{
			ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
		}
		ElementComposer.GetButton("cancelButton").Enabled = false;
		return true;
	}

	protected void updateLogText()
	{
		string text = string.Join("\n", _lines);
		GuiElementDynamicText dynamicText = ElementComposer.GetDynamicText("centertext");
		GuiElementScrollbar scrollbar = ElementComposer.GetScrollbar("scrollbar");
		if (dynamicText == null || text.Length > 10000)
		{
			return;
		}
		dynamicText.SetNewText(text, autoHeight: true);
		if (scrollbar != null)
		{
			scrollbar.SetNewTotalHeight((float)(dynamicText.Bounds.OuterHeight / (double)ClientSettings.GUIScale));
			if (!scrollbar.mouseDownOnScrollbarHandle && prevText != text)
			{
				scrollbar.ScrollToBottom();
			}
		}
		prevText = text;
	}

	public override void RenderToDefaultFramebuffer(float dt)
	{
		if (!singleplayer || !ClientSettings.DeveloperMode)
		{
			if (!runningGame.BlocksReceivedAndLoaded)
			{
				ellapseMs = ScreenManager.GamePlatform.EllapsedMs;
			}
			ScreenManager.mainScreen.Render(dt, ellapseMs, onlyBackground: true);
		}
		if (ServerMain.Logger != null && !loggerAdded)
		{
			loggerAdded = true;
			ServerMain.Logger.EntryAdded += LogAdded;
		}
		if (singleplayer && ScreenManager.Platform.EllapsedMs - lastLogfileCheck > 400)
		{
			updateLogText();
			lastLogfileCheck = ScreenManager.Platform.EllapsedMs;
		}
		if (runningGame == null)
		{
			ScreenManager.StartMainMenu();
			return;
		}
		updateScreenUI();
		ElementComposer.Render(dt);
		ElementComposer.PostRender(dt);
		LoadedTexture versionNumberTexture = ScreenManager.versionNumberTexture;
		float num = ScreenManager.GamePlatform.WindowSize.Width;
		float num2 = ScreenManager.GamePlatform.WindowSize.Height;
		ScreenManager.api.renderapi.Render2DTexturePremultipliedAlpha(versionNumberTexture.TextureId, num - (float)versionNumberTexture.Width - 10f, num2 - (float)versionNumberTexture.Height - 10f, versionNumberTexture.Width, versionNumberTexture.Height);
		runningGame.ExecuteMainThreadTasks(dt);
		if ((!runningGame.IsSingleplayer || (runningGame.IsSingleplayer && runningGame.AssetsReceived && !runningGame.AssetLoadingOffThread && ScreenManager.Platform.IsLoadedSinglePlayerServer())) && !runningGame.StartedConnecting)
		{
			connectToGameServer();
		}
		if (runningGame.exitToDisconnectScreen)
		{
			exitToDisconnectScreen();
		}
		if (runningGame.exitToMainMenu)
		{
			exitToMainMenu();
		}
	}

	private void updateScreenUI()
	{
		long ellapsedMs = ScreenManager.Platform.EllapsedMs;
		if (runningGame.AssetsReceived && runningGame.ServerReady)
		{
			if (!singleplayer)
			{
				ElementComposer.GetDynamicText("centertext")?.SetNewText(Lang.Get("Data received, launching client instance..."));
			}
			else
			{
				if (ellapsedMs - lastDotsUpdate > 500)
				{
					lastDotsUpdate = ellapsedMs;
					dotsCount = dotsCount % 3 + 1;
				}
				GuiElementDynamicText dynamicText = ElementComposer.GetDynamicText("centertext");
				if (dynamicText != null)
				{
					string text = ((!ClientSettings.DeveloperMode) ? ("\n" + Lang.Get("...")) : ("\n" + Lang.Get("Data received, launching single player instance...")));
					int num = dotsCount;
					while (--num > 0)
					{
						text = text + " " + Lang.Get("...");
					}
					dynamicText.SetNewText(prevText + text);
				}
			}
			GuiElementDynamicText dynamicText2 = ElementComposer.GetDynamicText("centertext");
			GuiElementScrollbar scrollbar = ElementComposer.GetScrollbar("scrollbar");
			if (dynamicText2 != null && scrollbar != null)
			{
				scrollbar.SetNewTotalHeight((float)(dynamicText2.Bounds.OuterHeight / (double)ClientSettings.GUIScale));
				if (!scrollbar.mouseDownOnScrollbarHandle)
				{
					scrollbar.ScrollToBottom();
				}
			}
		}
		else if (runningGame.Connectdata.ErrorMessage == null)
		{
			if (ellapsedMs - lastTextUpdate <= 150)
			{
				return;
			}
			lastTextUpdate = ellapsedMs;
			if (runningGame.Connectdata.Connected)
			{
				int num2 = runningGame.networkProc.TotalBytesReceivedAndReceiving / 1024;
				string text2;
				if (runningGame.Connectdata.PositionInQueue > 0)
				{
					text2 = Lang.Get("connect-inqueue", runningGame.Connectdata.PositionInQueue);
				}
				else
				{
					text2 = Lang.Get("Connected to server, downloading data...");
					text2 = text2 + "\n" + Lang.Get("{0} kilobyte received", num2);
				}
				if (text2 != ElementComposer.GetDynamicText("centertext").GetText())
				{
					Logger.Notification(text2);
				}
				ElementComposer.GetDynamicText("centertext").SetNewText(text2);
			}
		}
		else
		{
			string text3 = Lang.Get("error-connecting", runningGame.Connectdata.ErrorMessage);
			if (text3 != ElementComposer.GetDynamicText("centertext").GetText())
			{
				Logger.Notification(Lang.Get("error-connecting-host", runningGame.Connectdata.Host, runningGame.Connectdata.ErrorMessage));
			}
			ElementComposer.GetDynamicText("centertext").SetNewText(text3);
		}
	}

	public void exitToMainMenu()
	{
		runningGame.Dispose();
		if (runningGame.IsSingleplayer && ScreenManager.Platform.IsServerRunning)
		{
			ScreenManager.LoadAndCacheScreen(typeof(GuiScreenExitingServer));
		}
		else
		{
			ScreenManager.StartMainMenu();
		}
	}

	private void exitToDisconnectScreen()
	{
		runningGame?.Dispose();
		Logger.Notification("Exiting current game");
		if (runningGame?.disconnectAction == "trydownloadmods")
		{
			ServerConnectData connectdata = runningGame.Connectdata;
			string installPath = ((connectdata.Host == null) ? GamePaths.DataPathMods : Path.Combine(GamePaths.DataPathServerMods, GamePaths.ReplaceInvalidChars(connectdata.Host + "-" + connectdata.Port)));
			GuiScreenDownloadMods guiScreenDownloadMods = new GuiScreenDownloadMods(connectdata, installPath, runningGame.disconnectMissingMods, ScreenManager, ScreenManager.mainScreen);
			guiScreenDownloadMods.serverargs = (ParentScreen as GuiScreenRunningGame).serverargs;
			ScreenManager.LoadScreen(guiScreenDownloadMods);
		}
		else
		{
			string reason = runningGame.disconnectReason ?? "unknown";
			GuiScreenDisconnected screen = ((!(runningGame?.disconnectAction == "disconnectSP")) ? new GuiScreenDisconnected(reason, ScreenManager, ScreenManager.mainScreen) : new GuiScreenDisconnected(reason, ScreenManager, ScreenManager.mainScreen, "singleplayer-disconnected"));
			ScreenManager.LoadScreen(screen);
		}
	}

	private void connectToGameServer()
	{
		Logger.Debug("Opening socket to server...");
		runningGame.StartedConnecting = true;
		try
		{
			runningGame.Connect();
		}
		catch (Exception ex)
		{
			Logger.Notification("Exiting current game");
			string text = Lang.Get("Could not initiate connection: {0}\n\n<font color=\"#bbb\">Full Trace:\n{1}</font>", ex.Message, LoggerBase.CleanStackTrace(ex.ToString()));
			Logger.Warning(text.Replace("\n\n", "\n"));
			runningGame.Dispose();
			ScreenManager.LoadScreen(new GuiScreenDisconnected(text, ScreenManager, ScreenManager.mainScreen, "server-unableconnect"));
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		if (ServerMain.Logger != null)
		{
			ServerMain.Logger.EntryAdded -= LogAdded;
		}
		_lines = null;
	}
}
