using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cairo;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.Gui;
using Vintagestory.Client.MaxObf;
using Vintagestory.Client.NoObf;
using Vintagestory.ClientNative;
using Vintagestory.Common;
using Vintagestory.ModDb;

namespace Vintagestory.Client;

public class ScreenManager : KeyEventHandler, MouseEventHandler, NewFrameHandler
{
	public static int MainThreadId;

	public static GuiComposerManager GuiComposers;

	public static ClientPlatformAbstract Platform;

	public static FrameProfilerUtil FrameProfiler;

	public static KeyModifiers KeyboardModifiers = new KeyModifiers();

	private static int keysMax = 512;

	public static bool[] KeyboardKeyState = new bool[keysMax];

	public static bool[] MouseButtonState = new bool[(int)Enum.GetValues(typeof(EnumMouseButton)).Cast<EnumMouseButton>().Max()];

	public static Dictionary<AssetLocation, AudioData> soundAudioData = new Dictionary<AssetLocation, AudioData>();

	public static Dictionary<AssetLocation, AudioData> soundAudioDataAsyncLoadTemp = new Dictionary<AssetLocation, AudioData>();

	public static Queue<Action> MainThreadTasks = new Queue<Action>();

	public static bool debugDrawCallNextFrame = false;

	public SessionManager sessionManager;

	public static HotkeyManager hotkeyManager;

	private Dictionary<GuiScreen, Type> CachedScreens = new Dictionary<GuiScreen, Type>();

	internal GuiScreen CurrentScreen;

	internal GuiScreenMainRight mainScreen;

	private long lastSaveCheck;

	public string loadingText;

	public bool ClientIsOffline = true;

	private bool cursorsLoaded;

	protected EnumAuthServerResponse? validationResponse;

	private bool awaitValidation = true;

	private int mouseX;

	private int mouseY;

	internal static ILoadedSound IntroMusic;

	internal static bool introMusicShouldStop = false;

	internal GuiComposer mainMenuComposer;

	internal GuiCompositeMainMenuLeft guiMainmenuLeft;

	public MainMenuAPI api;

	internal ModLoader modloader;

	internal List<ModContainer> allMods = new List<ModContainer>();

	internal List<ModContainer> verifiedMods = new List<ModContainer>();

	public static ClientProgramArgs ParsedArgs;

	public static string[] RawCmdLineArgs;

	internal LoadedTexture versionNumberTexture;

	internal string newestVersion;

	private bool withMainMenu = true;

	internal Dictionary<string, int> textures;

	public ClientPlatformAbstract GamePlatform => Platform;

	public List<IInventory> OpenedInventories => null;

	public IWorldAccessor World => null;

	public static bool AsyncSoundLoadComplete { get; set; }

	public IPlayerInventoryManager PlayerInventoryManager => null;

	public IPlayer Player
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public ScreenManager(ClientPlatformAbstract platform)
	{
		Platform = platform;
		textures = new Dictionary<string, int>();
		sessionManager = new SessionManager();
		hotkeyManager = new HotkeyManager();
		FrameProfiler = new FrameProfilerUtil(delegate(string text)
		{
			platform.Logger.Notification(text);
		});
	}

	public void Start(ClientProgramArgs args, string[] rawArgs)
	{
		api = new MainMenuAPI(this);
		GuiComposers = new GuiComposerManager(api);
		ParsedArgs = args;
		RawCmdLineArgs = rawArgs;
		MainThreadId = Environment.CurrentManagedThreadId;
		RuntimeEnv.MainThreadId = MainThreadId;
		Platform.SetTitle("Vintage Story");
		mainScreen = new GuiScreenMainRight(this, null);
		Platform.SetWindowClosedHandler(onWindowClosed);
		Platform.SetFrameHandler(this);
		Platform.RegisterKeyboardEvent(this);
		Platform.RegisterMouseEvent(this);
		Platform.RegisterOnFocusChange(onFocusChanged);
		Platform.SetFileDropHandler(onFileDrop);
		LoadAndCacheScreen(typeof(GuiScreenLoadingGame));
		versionNumberTexture = api.Gui.TextTexture.GenUnscaledTextTexture(GameVersion.LongGameVersion, CairoFont.WhiteDetailText());
		Thread thread = new Thread(DoGameInitStage1);
		thread.Start();
		thread.IsBackground = true;
		registerSettingsWatchers();
		Platform.GlDebugMode = ClientSettings.GlDebugMode;
		Platform.GlErrorChecking = ClientSettings.GlErrorChecking;
		Platform.GlToggleBlend(on: true);
		TyronThreadPool.QueueLongDurationTask(delegate
		{
			while (true)
			{
				string environmentVariable = Environment.GetEnvironmentVariable("TEXTURE_DEBUG_DISPOSE");
				string environmentVariable2 = Environment.GetEnvironmentVariable("CAIRO_DEBUG_DISPOSE");
				string environmentVariable3 = Environment.GetEnvironmentVariable("VAO_DEBUG_DISPOSE");
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					RuntimeEnv.DebugTextureDispose = environmentVariable == "1";
				}
				if (!string.IsNullOrEmpty(environmentVariable2))
				{
					CairoDebug.Enabled = environmentVariable2 == "1";
				}
				if (!string.IsNullOrEmpty(environmentVariable3))
				{
					RuntimeEnv.DebugVAODispose = environmentVariable3 == "1";
				}
				Thread.Sleep(1000);
			}
		});
	}

	public void registerSettingsWatchers()
	{
		ClientSettings.Inst.AddWatcher<int>("masterSoundLevel", OnMasterSoundLevelChanged);
		ClientSettings.Inst.AddWatcher<int>("musicLevel", OnMusicLevelChanged);
		ClientSettings.Inst.AddWatcher<int>("windowBorder", OnWindowBorderChanged);
		ClientSettings.Inst.AddWatcher<int>("gameWindowMode", OnWindowModeChanged);
		ClientSettings.Inst.AddWatcher("glDebugMode", delegate(bool val)
		{
			Platform.GlDebugMode = val;
		});
		ClientSettings.Inst.AddWatcher("glErrorChecking", delegate(bool val)
		{
			Platform.GlErrorChecking = val;
		});
		ClientSettings.Inst.AddWatcher("guiScale", delegate(float val)
		{
			RuntimeEnv.GUIScale = val;
			GuiComposers.MarkAllDialogsForRecompose();
			loadCursors();
		});
		Platform.AddAudioSettingsWatchers();
		Platform.MasterSoundLevel = (float)ClientSettings.MasterSoundLevel / 100f;
	}

	private void OnWindowModeChanged(int newvalue)
	{
		CurrentScreen.ElementComposer?.GetDropDown("windowModeSwitch")?.SetSelectedIndex(GuiCompositeSettings.GetWindowModeIndex());
	}

	private void OnWindowBorderChanged(int newValue)
	{
		Platform.WindowBorder = (EnumWindowBorder)newValue;
		CurrentScreen.ElementComposer?.GetDropDown("windowBorder")?.SetSelectedIndex(ClientSettings.WindowBorder);
	}

	public void DoGameInitStage1()
	{
		sessionManager.GetNewestVersion(OnReceivedNewestVersion);
		loadingText = Lang.Get("Loading assets");
		Platform.LoadAssets();
		loadingText = Lang.Get("Loading sounds");
		Platform.Logger.Notification("Loading sounds");
		LoadSoundsInitial();
		Platform.Logger.Notification("Sounds loaded");
		loadingText = null;
	}

	public void DoGameInitStage2()
	{
		ShaderRegistry.Load();
		if (!cursorsLoaded)
		{
			loadCursors();
			cursorsLoaded = true;
		}
		TyronThreadPool.QueueTask(delegate
		{
			AssetLocation location = new AssetLocation("music/roots.ogg");
			AudioData audioData = LoadMusicTrack(Platform.AssetManager.TryGet_BaseAssets(location));
			if (audioData != null)
			{
				Random random = new Random();
				IntroMusic = Platform.CreateAudio(new SoundParams
				{
					SoundType = EnumSoundType.Music,
					Location = location
				}, audioData);
				while (!Platform.IsShuttingDown && !introMusicShouldStop)
				{
					IntroMusic.Start();
					while (!IntroMusic.HasStopped)
					{
						Thread.Sleep(100);
						if (Platform.IsShuttingDown)
						{
							break;
						}
					}
					if (!Platform.IsShuttingDown)
					{
						Thread.Sleep((300 + random.Next(600)) * 1000);
					}
				}
			}
		});
		hotkeyManager.RegisterDefaultHotKeys();
		setupHotkeyHandlers();
		if (!ClientSettings.DisableModSafetyCheck)
		{
			ModDbUtil.GetBlockedModsAsync(Platform.Logger);
		}
		if (!sessionManager.IsCachedSessionKeyValid())
		{
			Platform.Logger.Notification("Cached session key is invalid, require login");
			Platform.ToggleOffscreenBuffer(enable: true);
			LoadAndCacheScreen(typeof(GuiScreenLogin));
			return;
		}
		Platform.Logger.Notification("Cached session key is valid, validating with server");
		loadingText = "Validating session with server";
		sessionManager.ValidateSessionKeyWithServer(OnValidationDone);
		TyronThreadPool.QueueTask(delegate
		{
			int num = 1;
			Thread.Sleep(1000);
			while (awaitValidation)
			{
				loadingText = "Validating session with server\n" + num;
				num++;
				Thread.Sleep(1000);
			}
		});
	}

	private void setupHotkeyHandlers()
	{
		hotkeyManager.SetHotKeyHandler("recomposeallguis", delegate
		{
			GuiComposers.RecomposeAllDialogs();
			return true;
		}, isIngameHotkey: false);
		hotkeyManager.SetHotKeyHandler("reloadworld", delegate
		{
			CurrentScreen.ReloadWorld("Reload world hotkey triggered");
			return true;
		}, isIngameHotkey: false);
		hotkeyManager.SetHotKeyHandler("cycledialogoutlines", delegate
		{
			GuiComposer.Outlines = (GuiComposer.Outlines + 1) % 3;
			return true;
		}, isIngameHotkey: false);
		hotkeyManager.SetHotKeyHandler("togglefullscreen", delegate
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Invalid comparison between Unknown and I4
			GuiCompositeSettings.SetWindowMode(((int)Platform.GetWindowState() != 3) ? 1 : 0);
			return true;
		}, isIngameHotkey: false);
	}

	private void loadCursors()
	{
		LoadCursor("textselect");
		LoadCursor("linkselect");
		LoadCursor("move");
		LoadCursor("busy");
		LoadCursor("normal");
		Platform.UseMouseCursor("normal", forceUpdate: true);
	}

	private void LoadCursor(string code)
	{
		if (RuntimeEnv.OS != OS.Mac)
		{
			Dictionary<string, Vec2i> dictionary = Platform.AssetManager.Get("textures/gui/cursors/coords.json").ToObject<Dictionary<string, Vec2i>>();
			BitmapRef bitmapRef = Platform.AssetManager.TryGet_BaseAssets("textures/gui/cursors/" + code + ".png")?.ToBitmap(api);
			if (bitmapRef != null)
			{
				Platform.LoadMouseCursor(code, dictionary[code].X, dictionary[code].Y, bitmapRef);
			}
		}
	}

	private void OnReceivedNewestVersion(string newestversion)
	{
		newestVersion = newestversion;
	}

	private void OnValidationDone(EnumAuthServerResponse response)
	{
		validationResponse = response;
		awaitValidation = false;
	}

	private void DoGameInitStage3()
	{
		Platform.ToggleOffscreenBuffer(enable: true);
		ILogger logger = Platform.Logger;
		EnumAuthServerResponse? enumAuthServerResponse = validationResponse;
		logger.Notification("Server validation response: " + enumAuthServerResponse.ToString());
		ClientIsOffline = false;
		if (validationResponse == EnumAuthServerResponse.Good)
		{
			DoGameInitStage4();
		}
		else if (validationResponse == EnumAuthServerResponse.Bad)
		{
			LoadAndCacheScreen(typeof(GuiScreenLogin));
			validationResponse = null;
		}
		else
		{
			ClientIsOffline = true;
			DoGameInitStage4();
		}
	}

	internal void DoGameInitStage4()
	{
		validationResponse = null;
		loadingText = "Loading mod meta infos";
		TyronThreadPool.QueueTask(delegate
		{
			loadMods();
			EnqueueMainThreadTask(DoGameInitStage5);
		});
	}

	private void DoGameInitStage5()
	{
		StartMainMenu();
		HandleArgs();
	}

	public static void CatalogSounds(Action onCompleted)
	{
		AsyncSoundLoadComplete = false;
		soundAudioData.Clear();
		new LoadSoundsThread(Platform.Logger, null, onCompleted).Process();
	}

	public static void LoadSoundsSlow_Async(ClientMain game)
	{
		Thread thread = new Thread(new LoadSoundsThread(Platform.Logger, game, null).ProcessSlow);
		thread.IsBackground = true;
		thread.Priority = ThreadPriority.BelowNormal;
		thread.Name = "LoadSounds async";
		thread.Start();
	}

	public static void LoadSoundsInitial()
	{
		List<IAsset> many = Platform.AssetManager.GetMany(AssetCategory.sounds, loadAsset: false);
		Platform.Logger.VerboseDebug("Loadsounds, found " + many.Count + " sounds");
		List<AudioData> list = new List<AudioData>();
		foreach (IAsset item in many)
		{
			LoadSound(item);
			if (item.Location.PathStartsWith("sounds/menubutton"))
			{
				list.Add(new AudioMetaData(item)
				{
					Loaded = 0
				});
			}
		}
		foreach (AudioData item2 in list)
		{
			item2.Load();
			Thread.Sleep(1);
		}
	}

	public static void LoadSoundsSlow(ClientMain game)
	{
		bool extendedDebugInfo = ClientSettings.ExtendedDebugInfo;
		foreach (KeyValuePair<AssetLocation, AudioData> item in soundAudioDataAsyncLoadTemp)
		{
			if (game.disposed)
			{
				return;
			}
			if (item.Key.BeginsWith("game", "sounds/weather/") || item.Key.BeginsWith("game", "sounds/environment") || item.Key.BeginsWith("game", "sounds/effect/tempstab") || item.Key.BeginsWith("game", "sounds/effect/rift"))
			{
				if (extendedDebugInfo)
				{
					Platform.Logger.VerboseDebug("Load sound asset " + item.Key.ToShortString());
				}
				item.Value.Load();
				Thread.Sleep(15);
				if (game.disposed)
				{
					return;
				}
			}
		}
		AsyncSoundLoadComplete = true;
		Platform.Logger.VerboseDebug("Loaded highest priority sound assets");
		if (ClientSettings.OptimizeRamMode < 2)
		{
			if (game.disposed)
			{
				return;
			}
			foreach (KeyValuePair<AssetLocation, AudioData> item2 in soundAudioDataAsyncLoadTemp)
			{
				if (extendedDebugInfo && item2.Value.Loaded == 0)
				{
					Platform.Logger.VerboseDebug("Load sound asset " + item2.Key);
				}
				item2.Value.Load();
				Thread.Sleep(20);
				if (game.disposed)
				{
					return;
				}
			}
		}
		soundAudioDataAsyncLoadTemp.Clear();
	}

	public static AudioData LoadMusicTrack(IAsset asset)
	{
		if (asset != null && !asset.IsLoaded())
		{
			asset.Origin.LoadAsset(asset);
		}
		if (asset == null || asset.Data == null)
		{
			return null;
		}
		if (!soundAudioData.TryGetValue(asset.Location, out var value))
		{
			soundAudioData[asset.Location] = Platform.CreateAudioData(asset);
			asset.Data = null;
		}
		else
		{
			value.Load();
		}
		return soundAudioData[asset.Location];
	}

	public static AudioData LoadSound(IAsset asset)
	{
		if (asset == null)
		{
			return null;
		}
		return soundAudioData[asset.Location] = new AudioMetaData(asset)
		{
			Loaded = 0
		};
	}

	public void loadMods()
	{
		List<string> list = new List<string>(ClientSettings.ModPaths);
		if (ParsedArgs.AddModPath != null)
		{
			list.AddRange(ParsedArgs.AddModPath);
		}
		modloader = new ModLoader(GamePlatform.Logger, EnumAppSide.Client, list, ParsedArgs.TraceLog);
		allMods.Clear();
		allMods.AddRange(modloader.LoadModInfos());
		verifiedMods = modloader.DisableAndVerify(new List<ModContainer>(allMods), ClientSettings.DisabledMods);
		CrashReporter.LoadedMods = verifiedMods.Where((ModContainer mod) => mod.Enabled).ToList();
	}

	private void HandleArgs()
	{
		if (ParsedArgs.AddOrigin != null)
		{
			foreach (string item in ParsedArgs.AddOrigin)
			{
				string[] directories = Directory.GetDirectories(item);
				for (int i = 0; i < directories.Length; i++)
				{
					string name = new DirectoryInfo(directories[i]).Name;
					Platform.AssetManager.CustomAppOrigins.Add(new PathOrigin(name, directories[i], "textures"));
				}
			}
		}
		if (ParsedArgs.ConnectServerAddress != null)
		{
			ConnectToMultiplayer(ParsedArgs.ConnectServerAddress, ParsedArgs.Password);
		}
		else if (ParsedArgs.InstallModId != null)
		{
			EnqueueMainThreadTask(delegate
			{
				GamePlatform.XPlatInterface.FocusWindow();
				InstallMod(ParsedArgs.InstallModId);
			});
		}
		else if (ParsedArgs.OpenWorldName != null || ParsedArgs.CreateRndWorld)
		{
			EnqueueMainThreadTask(openWorldFromArgs);
		}
	}

	private void openWorldFromArgs()
	{
		string playStyle = ParsedArgs.PlayStyle;
		string text = ParsedArgs.OpenWorldName;
		if (text == null)
		{
			int num = 0;
			while (File.Exists(GamePaths.Saves + Path.DirectorySeparatorChar + "world" + num + ".vcdbs"))
			{
				num++;
			}
			text = "world" + num + ".vcdbs";
		}
		foreach (ModContainer verifiedMod in verifiedMods)
		{
			if (verifiedMod.WorldConfig?.PlayStyles == null)
			{
				continue;
			}
			PlayStyle[] playStyles = verifiedMod.WorldConfig.PlayStyles;
			foreach (PlayStyle playStyle2 in playStyles)
			{
				if (playStyle2.LangCode == playStyle)
				{
					ConnectToSingleplayer(new StartServerArgs
					{
						SaveFileLocation = GamePaths.Saves + Path.DirectorySeparatorChar + text + ".vcdbs",
						WorldName = text,
						PlayStyle = playStyle2.Code,
						PlayStyleLangCode = playStyle2.LangCode,
						WorldType = playStyle2.WorldType,
						WorldConfiguration = playStyle2.WorldConfig,
						AllowCreativeMode = true,
						DisabledMods = ClientSettings.DisabledMods,
						Language = ClientSettings.Language
					});
					break;
				}
			}
		}
	}

	public void OnNewFrame(float dt)
	{
		if (debugDrawCallNextFrame)
		{
			Platform.DebugDrawCalls = true;
		}
		if (validationResponse.HasValue)
		{
			DoGameInitStage3();
		}
		if (CurrentScreen is GuiScreenRunningGame)
		{
			Platform.MaxFps = ClientSettings.MaxFPS;
		}
		else
		{
			Platform.MaxFps = 60f;
		}
		if (Environment.TickCount - lastSaveCheck > 2000)
		{
			ClientSettings.Inst.Save();
			lastSaveCheck = Environment.TickCount;
			if (guiMainmenuLeft != null && newestVersion != null)
			{
				if (GameVersion.IsNewerVersionThan(newestVersion, "1.21.0"))
				{
					guiMainmenuLeft.SetHasNewVersion(newestVersion);
				}
				newestVersion = null;
			}
		}
		int count = MainThreadTasks.Count;
		while (count-- > 0)
		{
			lock (MainThreadTasks)
			{
				MainThreadTasks.Dequeue()();
			}
		}
		Render(dt);
		FrameProfiler.Mark("rendered");
		if (debugDrawCallNextFrame)
		{
			Platform.DebugDrawCalls = false;
			debugDrawCallNextFrame = false;
		}
	}

	public static void EnqueueMainThreadTask(Action a)
	{
		lock (MainThreadTasks)
		{
			MainThreadTasks.Enqueue(a);
		}
	}

	public static void EnqueueCallBack(Action a, int msdelay)
	{
		TyronThreadPool.QueueTask(delegate
		{
			Thread.Sleep(msdelay);
			EnqueueMainThreadTask(a);
		});
	}

	internal void Render(float dt)
	{
		int width = Platform.WindowSize.Width;
		int height = Platform.WindowSize.Height;
		Platform.GlViewport(0, 0, width, height);
		Platform.ClearFrameBuffer(EnumFrameBuffer.Default);
		Platform.ClearFrameBuffer(EnumFrameBuffer.Primary);
		Platform.GlDisableDepthTest();
		Platform.GlDisableCullFace();
		Platform.CheckGlError();
		Platform.DoPostProcessingEffects = false;
		Platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
		CurrentScreen.RenderToPrimary(dt);
		FrameProfiler.Mark("doneRend");
		float[] projectMatrix = null;
		if (CurrentScreen is GuiScreenRunningGame guiScreenRunningGame)
		{
			projectMatrix = guiScreenRunningGame.runningGame.CurrentProjectionMatrix;
		}
		Platform.RenderPostprocessingEffects(projectMatrix);
		CurrentScreen.RenderAfterPostProcessing(dt);
		Platform.RenderFinalComposition();
		CurrentScreen.RenderAfterFinalComposition(dt);
		Platform.BlitPrimaryToDefault();
		Platform.CheckGlError();
		FrameProfiler.Mark("doneRender2Default");
		Mat4f.Identity(api.renderapi.pMatrix);
		Mat4f.Ortho(api.renderapi.pMatrix, 0f, Platform.WindowSize.Width, Platform.WindowSize.Height, 0f, 0f, 20001f);
		Platform.GlDepthFunc(EnumDepthFunction.Lequal);
		float num = 20000f;
		GL.ClearBuffer((ClearBuffer)6145, 0, ref num);
		GL.DepthRange(0f, 20000f);
		Platform.GlEnableDepthTest();
		Platform.GlDisableCullFace();
		Platform.GlToggleBlend(on: true);
		CurrentScreen.RenderAfterBlit(dt);
		if (CurrentScreen.RenderBg && !Platform.IsShuttingDown)
		{
			guiMainmenuLeft?.RenderBg(dt, withMainMenu);
			withMainMenu = false;
		}
		CurrentScreen.RenderToDefaultFramebuffer(dt);
		Platform.GlDepthFunc(EnumDepthFunction.Less);
		FrameProfiler.Mark("doneAfterRender");
		Platform.CheckGlError();
	}

	internal void RenderMainMenuParts(float dt, ElementBounds bounds, bool withMainMenu, bool darkenEdges = true)
	{
		this.withMainMenu = withMainMenu;
		if (withMainMenu)
		{
			Platform.GlToggleBlend(on: true);
			mainMenuComposer.Render(dt);
			mainMenuComposer.PostRender(dt);
		}
		float num = Platform.WindowSize.Width;
		float num2 = Platform.WindowSize.Height;
		api.Render.Render2DTexturePremultipliedAlpha(versionNumberTexture.TextureId, num - (float)versionNumberTexture.Width - 10f, (double)(num2 - (float)versionNumberTexture.Height) - GuiElement.scaled(5.0), versionNumberTexture.Width, versionNumberTexture.Height);
	}

	public IInventory GetOwnInventory(string className)
	{
		throw new NotImplementedException();
	}

	public void SendPacketClient(Packet_Client packetClient)
	{
		throw new NotImplementedException();
	}

	public void TriggerOnMouseEnterSlot(ItemSlot slot)
	{
		throw new NotImplementedException();
	}

	public void TriggerOnMouseLeaveSlot(ItemSlot itemSlot)
	{
		throw new NotImplementedException();
	}

	public void TriggerOnMouseClickSlot(ItemSlot itemSlot)
	{
		throw new NotImplementedException();
	}

	public void RenderItemstack(ItemStack itemstack, double posX, double posY, double posZ, float size, int color, bool rotate = false, bool showStackSize = true)
	{
		throw new NotImplementedException();
	}

	public void BeginClipArea(ElementBounds bounds)
	{
		Platform.GlScissor((int)bounds.renderX, (int)((double)Platform.WindowSize.Height - bounds.renderY - bounds.InnerHeight), (int)bounds.InnerWidth, (int)bounds.InnerHeight);
		Platform.GlScissorFlag(enable: true);
	}

	public void EndClipArea()
	{
		Platform.GlScissorFlag(enable: false);
	}

	private void OnMasterSoundLevelChanged(int newValue)
	{
		Platform.MasterSoundLevel = (float)newValue / 100f;
	}

	private void OnMusicLevelChanged(int newValue)
	{
		if (IntroMusic != null && !IntroMusic.HasStopped)
		{
			IntroMusic.SetVolume();
		}
	}

	private void onWindowClosed()
	{
		CurrentScreen.OnWindowClosed();
	}

	public void OnKeyDown(KeyEvent e)
	{
		KeyboardKeyState[e.KeyCode] = true;
		KeyboardModifiers.AltPressed = e.AltPressed;
		KeyboardModifiers.CtrlPressed = e.CtrlPressed;
		KeyboardModifiers.ShiftPressed = e.ShiftPressed;
		if (CurrentScreen.GetType() != typeof(GuiScreenRunningGame))
		{
			bool handled = hotkeyManager.TriggerGlobalHotKey(e, null, null, keyUp: false);
			e.Handled = handled;
		}
		CurrentScreen.OnKeyDown(e);
	}

	public void OnKeyUp(KeyEvent e)
	{
		KeyboardKeyState[e.KeyCode] = false;
		KeyboardModifiers.AltPressed = e.AltPressed;
		KeyboardModifiers.CtrlPressed = e.CtrlPressed;
		KeyboardModifiers.ShiftPressed = e.ShiftPressed;
		CurrentScreen.OnKeyUp(e);
	}

	public void OnKeyPress(KeyEvent e)
	{
		if (e.KeyCode == 50)
		{
			CurrentScreen.OnBackPressed();
		}
		CurrentScreen.OnKeyPress(e);
	}

	public void OnMouseDown(MouseEvent e)
	{
		MouseButtonState[(int)e.Button] = true;
		mouseX = e.X;
		mouseY = e.Y;
		CurrentScreen.OnMouseDown(e);
	}

	public void OnMouseUp(MouseEvent e)
	{
		MouseButtonState[(int)e.Button] = false;
		CurrentScreen.OnMouseUp(e);
	}

	public void OnMouseMove(MouseEvent e)
	{
		mouseX = e.X;
		mouseY = e.Y;
		CurrentScreen.OnMouseMove(e);
	}

	public void OnMouseWheel(MouseWheelEventArgs e)
	{
		CurrentScreen.OnMouseWheel(e);
	}

	public void LoadAndCacheScreen(Type screenType)
	{
		if (CachedScreens.ContainsValue(screenType))
		{
			if (CurrentScreen != null && !CachedScreens.ContainsKey(CurrentScreen) && CurrentScreen != mainScreen)
			{
				CurrentScreen.Dispose();
			}
			CurrentScreen = CachedScreens.FirstOrDefault((KeyValuePair<GuiScreen, Type> x) => x.Value == screenType).Key;
			if (CurrentScreen != null)
			{
				CurrentScreen.OnScreenLoaded();
			}
		}
		else
		{
			CurrentScreen = (GuiScreen)Activator.CreateInstance(screenType, this, mainScreen);
			CachedScreens[CurrentScreen] = screenType;
			if (CurrentScreen != null)
			{
				CurrentScreen.OnScreenLoaded();
			}
		}
	}

	public void LoadScreen(GuiScreen screen)
	{
		if (CurrentScreen != null && !CachedScreens.ContainsKey(CurrentScreen) && screen != CurrentScreen && screen != null && screen.ShouldDisposePreviousScreen)
		{
			CurrentScreen.Dispose();
		}
		CurrentScreen = screen;
		if (CurrentScreen != null)
		{
			CurrentScreen.OnScreenLoaded();
		}
	}

	public void LoadScreenNoLoadCall(GuiScreen screen)
	{
		if (CurrentScreen != null && !CachedScreens.ContainsKey(CurrentScreen) && screen != CurrentScreen && screen != null && screen.ShouldDisposePreviousScreen)
		{
			CurrentScreen.Dispose();
		}
		CurrentScreen = screen;
	}

	internal void StartMainMenu()
	{
		initMainMenu();
		CurrentScreen = mainScreen;
		CurrentScreen.OnScreenLoaded();
		Platform.MouseGrabbed = false;
	}

	private void initMainMenu()
	{
		GuiComposers.ClearCache();
		foreach (KeyValuePair<GuiScreen, Type> cachedScreen in CachedScreens)
		{
			cachedScreen.Key.Dispose();
		}
		CachedScreens.Clear();
		CurrentScreen?.Dispose();
		mainScreen?.Dispose();
		guiMainmenuLeft?.Dispose();
		guiMainmenuLeft = new GuiCompositeMainMenuLeft(this);
		mainScreen.Compose();
		mainScreen.Refresh();
	}

	public void StartGame(bool singleplayer, StartServerArgs serverargs, ServerConnectData connectData)
	{
		GuiScreenRunningGame guiScreenRunningGame = new GuiScreenRunningGame(this, mainScreen);
		guiScreenRunningGame.Start(singleplayer, serverargs, connectData);
		CurrentScreen = new GuiScreenConnectingToServer(singleplayer, this, guiScreenRunningGame);
	}

	public void InstallMod(string modid)
	{
		if (CurrentScreen is GuiScreenRunningGame guiScreenRunningGame)
		{
			guiScreenRunningGame.ExitOrRedirect(isDisconnect: false, "modinstall request");
			TyronThreadPool.QueueTask(delegate
			{
				int num = 0;
				while (num++ < 1000 && !(CurrentScreen is GuiScreenMainRight))
				{
					Thread.Sleep(100);
				}
				EnqueueMainThreadTask(delegate
				{
					InstallMod(modid);
				});
			}, "mod install");
		}
		else
		{
			modid = modid.Replace("vintagestorymodinstall://", "").TrimEnd('/');
			LoadScreen(new GuiScreenDownloadMods(null, GamePaths.DataPathMods, new List<string> { modid }, this, mainScreen));
		}
	}

	public void ConnectToMultiplayer(string host, string password)
	{
		if (host.Contains("vintagestoryjoin://"))
		{
			GuiScreen curScreen = CurrentScreen;
			LoadScreen(new GuiScreenConfirmAction(Lang.Get("confirm-joinserver", host.Replace("vintagestoryjoin://", "").TrimEnd('/')), delegate(bool ok)
			{
				if (ok)
				{
					ConnectToMultiplayer(host.Replace("vintagestoryjoin://", ""), null);
				}
				else
				{
					LoadScreen(curScreen);
				}
			}, this, CurrentScreen));
			return;
		}
		try
		{
			ServerConnectData serverConnectData = ServerConnectData.FromHost(host);
			serverConnectData.ServerPassword = password;
			StartGame(singleplayer: false, null, serverConnectData);
		}
		catch (Exception ex)
		{
			LoadScreen(new GuiScreenDisconnected(Lang.Get("multiplayer-disconnected", ex.Message), this, mainScreen));
			Platform.Logger.Warning("Could not initiate connection:");
			Platform.Logger.Warning(ex);
		}
	}

	public void ConnectToSingleplayer(StartServerArgs serverargs)
	{
		StartGame(singleplayer: true, serverargs, null);
	}

	internal int GetGuiTexture(string name)
	{
		if (!textures.ContainsKey(name))
		{
			BitmapRef bitmapRef = Platform.AssetManager.Get("textures/gui/" + name).ToBitmap(api);
			int value = Platform.LoadTexture((IBitmap)bitmapRef, linearMag: false, 0, generateMipmaps: false);
			textures[name] = value;
			bitmapRef.Dispose();
		}
		return textures[name];
	}

	public int GetMouseCurrentX()
	{
		return mouseX;
	}

	public int GetMouseCurrentY()
	{
		return mouseY;
	}

	public static void PlaySound(string name)
	{
		PlaySound(new AssetLocation("sounds/" + name).WithPathAppendixOnce(".ogg"));
	}

	public static void PlaySound(AssetLocation location)
	{
		location = location.Clone().WithPathAppendixOnce(".ogg");
		soundAudioData.TryGetValue(location, out var value);
		if (value != null)
		{
			if (!value.Load())
			{
				return;
			}
			ILoadedSound sound = Platform.CreateAudio(new SoundParams(location), value);
			sound.Start();
			TyronThreadPool.QueueLongDurationTask(delegate
			{
				while (!sound.HasStopped)
				{
					Thread.Sleep(100);
				}
				sound.Dispose();
			});
		}
		else
		{
			Platform.Logger.Error("Could not play {0}, sound file not found", location);
		}
	}

	public ClientPlatformAbstract getGamePlatform()
	{
		return Platform;
	}

	private void onFocusChanged(bool focus)
	{
		CurrentScreen.OnFocusChanged(focus);
	}

	private void onFileDrop(string filename)
	{
		CurrentScreen.OnFileDrop(filename);
	}

	internal void TryRedirect(MultiplayerServerEntry entry)
	{
		Platform.Logger.Notification("Redirecting to new server");
		ConnectToMultiplayer(entry.host, entry.password);
	}

	internal void OfferRestart(string message)
	{
		GuiScreen currentScreen = CurrentScreen;
		EnqueueMainThreadTask(delegate
		{
			LoadScreen(new GuiScreenConfirmAction("restart-title", message, "general-cancel", "game-exit", delegate(bool val)
			{
				if (val)
				{
					Platform.WindowExit("requires restart");
				}
				else if (currentScreen is GuiScreenConnectingToServer guiScreenConnectingToServer)
				{
					guiScreenConnectingToServer.exitToMainMenu();
				}
				else
				{
					StartMainMenu();
				}
			}, this, currentScreen, "restart-confirmation"));
		});
	}
}
