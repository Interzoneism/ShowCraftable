using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using CommandLine;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.ClientNative;
using Vintagestory.Common;
using Vintagestory.Server;
using Vintagestory.Server.Network;

namespace Vintagestory.Client;

public class ClientProgram
{
	private CrashReporter crashreporter;

	private DummyNetwork dummyNetwork;

	private DummyNetwork dummyNetworkUdp;

	private StartServerArgs startServerargs;

	private static Logger logger;

	public ClientPlatformWindows platform;

	private static string[] rawArgs;

	private static ClientProgramArgs progArgs;

	private static readonly PosixSignalRegistration[] Signals = new PosixSignalRegistration[2];

	public static ScreenManager screenManager;

	public static void Main(string[] rawArgs)
	{
		AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.AssemblyResolve;
		ClientProgram.rawArgs = rawArgs;
		new ClientProgram(rawArgs);
	}

	public ClientProgram(string[] rawArgs)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		ClientProgram clientProgram = this;
		AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
		progArgs = new ClientProgramArgs();
		ParserResult<ClientProgramArgs> val = new Parser((Action<ParserSettings>)delegate(ParserSettings config)
		{
			config.HelpWriter = null;
			config.IgnoreUnknownArguments = true;
			config.AutoHelp = false;
			config.AutoVersion = false;
		}).ParseArguments<ClientProgramArgs>((IEnumerable<string>)rawArgs);
		progArgs = val.Value;
		if (progArgs.DataPath != null && progArgs.DataPath.Length > 0)
		{
			GamePaths.DataPath = progArgs.DataPath;
		}
		if (progArgs.LogPath != null && progArgs.LogPath.Length > 0)
		{
			GamePaths.CustomLogPath = progArgs.LogPath;
		}
		GamePaths.EnsurePathsExist();
		if (RuntimeEnv.OS == OS.Windows && (progArgs.PrintVersion || progArgs.PrintHelp))
		{
			WindowsConsole.Attach();
		}
		if (progArgs.PrintVersion)
		{
			Console.WriteLine("1.21.0");
			return;
		}
		if (progArgs.PrintHelp)
		{
			Console.WriteLine(progArgs.GetUsage(val));
			return;
		}
		if (progArgs.InstallModId != null)
		{
			progArgs.InstallModId = progArgs.InstallModId.Replace("vintagestorymodinstall://", "");
		}
		UriHandler instance = UriHandler.Instance;
		if (instance.TryConnectClientPipe())
		{
			if (progArgs.ConnectServerAddress != null)
			{
				instance.SendConnect(progArgs.ConnectServerAddress);
				instance.Dispose();
				return;
			}
			if (progArgs.InstallModId != null)
			{
				instance.SendModInstall(progArgs.InstallModId);
				instance.Dispose();
				return;
			}
		}
		else
		{
			instance.StartPipeServer();
		}
		dummyNetwork = new DummyNetwork();
		dummyNetworkUdp = new DummyNetwork();
		dummyNetwork.Start();
		dummyNetworkUdp.Start();
		crashreporter = new CrashReporter(EnumAppSide.Client);
		try
		{
			crashreporter.Start(delegate
			{
				clientProgram.Start(progArgs, rawArgs);
			});
		}
		finally
		{
			instance.Dispose();
		}
	}

	private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		Exception ex = (Exception)e.ExceptionObject;
		if (crashreporter == null)
		{
			platform.XPlatInterface.ShowMessageBox("Fatal Error", ex.Message);
		}
		else if (!crashreporter.isCrashing)
		{
			crashreporter.Crash(ex);
		}
	}

	private unsafe void Start(ClientProgramArgs args, string[] rawArgs)
	{
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		//IL_038f: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Expected O, but got Unknown
		//IL_03f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Expected O, but got Unknown
		//IL_0410: Unknown result type (might be due to invalid IL or missing references)
		//IL_0455: Unknown result type (might be due to invalid IL or missing references)
		//IL_046c: Unknown result type (might be due to invalid IL or missing references)
		string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		if (!Debugger.IsAttached)
		{
			Environment.CurrentDirectory = directoryName;
		}
		logger = new ClientLogger();
		logger.TraceLog = args.TraceLog;
		ClientPlatformWindows clientPlatformWindows = new ClientPlatformWindows(logger);
		clientPlatformWindows.ShaderUniforms.SepiaLevel = ClientSettings.SepiaLevel;
		clientPlatformWindows.ShaderUniforms.ExtraContrastLevel = ClientSettings.ExtraContrastLevel;
		CrashReporter.SetLogger((Logger)clientPlatformWindows.Logger);
		clientPlatformWindows.LogAndTestHardwareInfosStage1();
		screenManager = new ScreenManager(clientPlatformWindows);
		GuiStyle.DecorativeFontName = ClientSettings.DecorativeFontName;
		GuiStyle.StandardFontName = ClientSettings.DefaultFontName;
		Lang.PreLoad(ScreenManager.Platform.Logger, GamePaths.AssetsPath, ClientSettings.Language);
		if (RuntimeEnv.OS == OS.Windows && !ClientSettings.SkipNvidiaProfileCheck && NvidiaGPUFix64.SOP_SetProfile("Vintagestory", GetExecutableName()) == 1)
		{
			clientPlatformWindows.XPlatInterface.ShowMessageBox("Vintagestory Nvidia Profile", Lang.Get("Your game is now configured to use your dedicated NVIDIA Graphics card. This requires a restart so please start the game again."));
			return;
		}
		if (!CleanInstallCheck.IsCleanInstall())
		{
			clientPlatformWindows.XPlatInterface.ShowMessageBox("Vintagestory Warning", Lang.Get("launchfailure-notcleaninstall"));
			return;
		}
		if (RuntimeEnv.OS == OS.Windows && !ClientSettings.MultipleInstances)
		{
			new Mutex(initiallyOwned: true, "Vintagestory", out var createdNew);
			if (!createdNew)
			{
				clientPlatformWindows.XPlatInterface.ShowMessageBox(Lang.Get("Multiple Instances"), Lang.Get("game-alreadyrunning"));
				return;
			}
		}
		Signals[0] = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnExit);
		Signals[1] = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnExit);
		clientPlatformWindows.SetServerExitInterface(clientPlatformWindows.ServerExit);
		clientPlatformWindows.crashreporter = crashreporter;
		clientPlatformWindows.singlePlayerServerDummyNetwork = new DummyNetwork[2];
		clientPlatformWindows.singlePlayerServerDummyNetwork[0] = dummyNetwork;
		clientPlatformWindows.singlePlayerServerDummyNetwork[1] = dummyNetworkUdp;
		platform = clientPlatformWindows;
		clientPlatformWindows.OnStartSinglePlayerServer = delegate(StartServerArgs serverargs)
		{
			startServerargs = serverargs;
			Thread thread = new Thread(ServerThreadStart);
			thread.Name = "SingleplayerServer";
			thread.Priority = ThreadPriority.BelowNormal;
			thread.IsBackground = true;
			thread.Start();
		};
		WindowState val = (WindowState)(ClientSettings.GameWindowMode switch
		{
			3 => 3, 
			2 => 2, 
			1 => 3, 
			_ => 0, 
		});
		ScreenManager.Platform.Logger.Debug("Creating game window with window mode " + ((object)(*(WindowState*)(&val))/*cast due to .constrained prefix*/).ToString());
		Size2i screenSize = ScreenManager.Platform.ScreenSize;
		if (ClientSettings.IsNewSettingsFile)
		{
			int num = 1280;
			int num2 = 850;
			float gUIScale = 1f;
			if (screenSize.Width - 20 < num || screenSize.Height - 20 < num2)
			{
				gUIScale = 0.875f;
				num = Math.Min(screenSize.Width - 20, num);
				num2 = Math.Min(screenSize.Height - 20, num2);
			}
			if (num2 < 680)
			{
				gUIScale = 0.75f;
			}
			if (screenSize.Width > 2500)
			{
				gUIScale = 1.25f;
			}
			if (screenSize.Width > 3000)
			{
				gUIScale = 1.5f;
				num = 2000;
			}
			if (screenSize.Width > 5000)
			{
				gUIScale = 2f;
			}
			if (screenSize.Height > 1300)
			{
				screenSize.Height = 1200;
			}
			ClientSettings.ScreenWidth = num;
			ClientSettings.ScreenHeight = num2;
			ClientSettings.GUIScale = gUIScale;
		}
		if (ClientSettings.ScreenWidth < 10)
		{
			ClientSettings.ScreenWidth = 10;
		}
		if (ClientSettings.ScreenHeight < 10)
		{
			ClientSettings.ScreenHeight = 10;
		}
		string[] array = ClientSettings.GlContextVersion.Split('.');
		int num3 = array[0].ToInt(4);
		int num4 = array[1].ToInt(3);
		GameWindowSettings gameWindowSettings = GameWindowSettings.Default;
		NativeWindowSettings val2 = new NativeWindowSettings
		{
			Title = "Vintage Story",
			APIVersion = new Version(num3, num4),
			ClientSize = new Vector2i(ClientSettings.ScreenWidth, ClientSettings.ScreenHeight),
			Flags = (ContextFlags)0,
			Vsync = (VSyncMode)(ClientSettings.VsyncMode != 0),
			WindowState = val,
			WindowBorder = (WindowBorder)ClientSettings.WindowBorder
		};
		if (RuntimeEnv.OS == OS.Mac)
		{
			val2.Flags = (ContextFlags)2;
		}
		GLFW.SetErrorCallback(new ErrorCallback(GlfwErrorCallback));
		GameWindowNative gameWindowNative = AttemptToOpenWindow(gameWindowSettings, val2, num3, num4, 12);
		if ((int)val == 0 && !RuntimeEnv.IsWaylandSession)
		{
			((NativeWindow)gameWindowNative).CenterWindow();
		}
		clientPlatformWindows.StartAudio();
		clientPlatformWindows.LogAndTestHardwareInfosStage2();
		clientPlatformWindows.window = gameWindowNative;
		clientPlatformWindows.XPlatInterface.Window = (GameWindow)(object)gameWindowNative;
		clientPlatformWindows.SetDirectMouseMode(ClientSettings.DirectMouseMode);
		clientPlatformWindows.WindowSize.Width = ((NativeWindow)gameWindowNative).ClientSize.X;
		clientPlatformWindows.WindowSize.Height = ((NativeWindow)gameWindowNative).ClientSize.Y;
		if (ClientSettings.GameWindowMode == 3)
		{
			clientPlatformWindows.SetWindowAttribute((WindowAttribute)131078, value: false);
		}
		screenManager.Start(args, rawArgs);
		clientPlatformWindows.Start();
		Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
		try
		{
			((GameWindow)gameWindowNative).Run();
		}
		finally
		{
			if (RuntimeEnv.OS == OS.Windows)
			{
				GLFW.IconifyWindow(((NativeWindow)gameWindowNative).WindowPtr);
			}
			Thread.CurrentThread.Priority = ThreadPriority.Normal;
			ScreenManager.Platform.Logger.Debug("After gamewindow.Run()");
			clientPlatformWindows.DisposeFrameBuffers(clientPlatformWindows.FrameBuffers);
			clientPlatformWindows.StopAudio();
			((NativeWindow)gameWindowNative).Dispose();
		}
	}

	private GameWindowNative AttemptToOpenWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, int openGlMajor, int openGlMinor, int tries)
	{
		nativeWindowSettings.APIVersion = new Version(openGlMajor, openGlMinor);
		GameWindowNative result;
		try
		{
			result = new GameWindowNative(gameWindowSettings, nativeWindowSettings);
		}
		catch (Exception ex)
		{
			if (tries <= 1)
			{
				throw;
			}
			GLFWException ex2 = (GLFWException)(object)((ex is GLFWException) ? ex : null);
			if (ex2 != null)
			{
				int num = ((Exception)(object)ex2).Message.IndexOf(openGlMinor + ", got version ");
				if (num > 0)
				{
					openGlMajor = ex.Message[num + 15] - 48;
					openGlMinor = ex.Message[num + 17] - 48;
					return AttemptToOpenWindow(gameWindowSettings, nativeWindowSettings, openGlMajor, openGlMinor, tries - 1);
				}
				num = ((Exception)(object)ex2).Message.IndexOf("OpenGL version " + openGlMajor + "." + openGlMinor);
				if (num < 0)
				{
					throw;
				}
			}
			if (--openGlMinor < 0)
			{
				openGlMajor--;
				openGlMinor = openGlMajor switch
				{
					1 => 5, 
					2 => 1, 
					3 => 3, 
					_ => 6, 
				};
			}
			return AttemptToOpenWindow(gameWindowSettings, nativeWindowSettings, openGlMajor, openGlMinor, tries - 1);
		}
		if (openGlMajor < 4 || (openGlMajor == 4 && openGlMinor < 3))
		{
			ClientSettings.AllowSSBOs = false;
		}
		return result;
	}

	private void GlfwErrorCallback(ErrorCode error, string description)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if ((int)error == 65545)
		{
			platform.Logger.Debug("GLFW FormatUnavailable: " + description);
			return;
		}
		platform.Logger.Error($"GLFW Exception: ErrorCode:{error} {description}");
	}

	private void OnExit(PosixSignalContext ctx)
	{
		ctx.Cancel = true;
		UriHandler.Instance.Dispose();
		screenManager.GamePlatform.WindowExit("SIGTERM or SIGINT received");
	}

	public void ServerThreadStart()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		ServerMain serverMain = null;
		ServerProgramArgs value = new Parser((Action<ParserSettings>)delegate(ParserSettings config)
		{
			config.IgnoreUnknownArguments = true;
			config.AutoHelp = false;
			config.AutoVersion = false;
		}).ParseArguments<ServerProgramArgs>((IEnumerable<string>)rawArgs).Value;
		dummyNetwork.Clear();
		platform.Logger.Notification("Server args parsed");
		try
		{
			serverMain = new ServerMain(startServerargs, rawArgs, value, isDedicatedServer: false);
			platform.Logger.Notification("Server main instantiated");
			serverMain.exit = platform.ServerExit;
			DummyTcpNetServer dummyTcpNetServer = new DummyTcpNetServer();
			dummyTcpNetServer.SetNetwork(dummyNetwork);
			serverMain.MainSockets[0] = dummyTcpNetServer;
			DummyUdpNetServer dummyUdpNetServer = new DummyUdpNetServer();
			dummyUdpNetServer.SetNetwork(dummyNetworkUdp);
			serverMain.UdpSockets[0] = dummyUdpNetServer;
			platform.IsServerRunning = true;
			platform.SetGamePausedState(paused: false);
			serverMain.PreLaunch();
			serverMain.Launch();
			platform.Logger.Notification("Server launched");
			bool flag = false;
			do
			{
				if (!flag && platform.IsGamePaused)
				{
					serverMain.Suspend(newSuspendState: true);
					flag = true;
				}
				if (flag && !platform.IsGamePaused)
				{
					serverMain.Suspend(newSuspendState: false);
					flag = false;
				}
				serverMain.Process();
				if (!platform.singlePlayerServerLoaded)
				{
					platform.Logger.VerboseDebug("--- Server started ---");
				}
				platform.singlePlayerServerLoaded = true;
			}
			while (platform.ServerExit == null || !platform.ServerExit.GetExit());
			serverMain.Stop("Exit request by client");
			platform.IsServerRunning = false;
			platform.singlePlayerServerLoaded = false;
			serverMain.Dispose();
		}
		catch (Exception ex)
		{
			platform.Logger.Fatal(ex);
			if (serverMain != null)
			{
				serverMain.Stop("Exception thrown by server during startup or process");
				platform.IsServerRunning = false;
				platform.singlePlayerServerLoaded = false;
				try
				{
					serverMain.Dispose();
				}
				catch (Exception)
				{
				}
			}
			if (ex is RestartGameException)
			{
				screenManager.OfferRestart(ex.Message);
			}
		}
		dummyNetwork.Clear();
	}

	private static string GetExecutableName()
	{
		string fileName = Process.GetCurrentProcess().MainModule.FileName;
		int num = fileName.LastIndexOf('\\');
		return fileName.Substring(num + 1, fileName.Length - num - 1);
	}
}
