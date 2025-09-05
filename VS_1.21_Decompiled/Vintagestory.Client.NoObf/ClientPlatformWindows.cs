using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Cairo;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;
using VSPlatform;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.ClientNative;
using Vintagestory.Common;
using Vintagestory.Common.Convert;

namespace Vintagestory.Client.NoObf;

public sealed class ClientPlatformWindows : ClientPlatformAbstract
{
	public class GLBuffer
	{
		public int BufferId;
	}

	private AudioOpenAl audio;

	public GameExit gameexit;

	public bool SupportsThickLines;

	private int cpuCoreCount = 2;

	private AssetManager assetManager;

	private Stopwatch frameStopWatch;

	internal Stopwatch uptimeStopWatch = new Stopwatch();

	private Logger logger;

	private int doResize;

	private static DebugProc _debugProcCallback;

	private static GCHandle _debugProcCallbackHandle;

	public GameWindowNative window;

	private Screenshot screenshot = new Screenshot();

	public Action<StartServerArgs> OnStartSinglePlayerServer;

	public GameExit ServerExit = new GameExit();

	public bool singlePlayerServerLoaded;

	public DummyNetwork[] singlePlayerServerDummyNetwork;

	private Size2i windowsize = new Size2i();

	private Size2i screensize = new Size2i();

	private List<OnFocusChanged> focusChangedDelegates = new List<OnFocusChanged>();

	private Action windowClosedHandler;

	private NewFrameHandler frameHandler;

	public CrashReporter crashreporter;

	private OnCrashHandler onCrashHandler;

	public List<KeyEventHandler> keyEventHandlers = new List<KeyEventHandler>();

	public List<MouseEventHandler> mouseEventHandlers = new List<MouseEventHandler>();

	public Action<string> fileDropEventHandler;

	private bool debugDrawCalls;

	private List<string> drawCallStacks = new List<string>();

	private List<FrameBufferRef> frameBuffers;

	private MeshRef screenQuad;

	private bool serverRunning;

	private bool gamepause;

	private bool OffscreenBuffer = true;

	private bool RenderBloom;

	private bool RenderGodRays;

	private bool RenderFXAA;

	private bool RenderSSAO;

	private bool SetupSSAO;

	private int ShadowMapQuality;

	private float ssaaLevel;

	public GLBuffer[] PixelPackBuffer;

	public int sampleCount = 32;

	public int CurrentPixelPackBufferNum;

	private Random rand = new Random();

	private float[] ssaoKernel = new float[192];

	private FrameBufferRef curFb;

	private float[] clearColor = new float[4] { 0f, 0f, 0f, 1f };

	private bool glDebugMode;

	private bool supportsGlDebugMode;

	private bool supportsPersistentMapping;

	public bool ENABLE_MIPMAPS = true;

	public bool ENABLE_ANISOTROPICFILTERING;

	public bool ENABLE_TRANSPARENCY = true;

	[ThreadStatic]
	private static FaceData[] facedataBuffer;

	[ThreadStatic]
	private static int[] customIntsPruned;

	private const float minUV = -1.5E-05f;

	private const float maxUV = 1.000015f;

	private Vector2 previousMousePosition;

	private CursorState previousCursorState;

	private float mouseX;

	private float mouseY;

	private Dictionary<string, MouseCursor> preLoadedCursors = new Dictionary<string, MouseCursor>();

	private bool ignoreMouseMoveEvent;

	private float prevWheelValue;

	private long lastKeyUpMs;

	private int lastKeyUpKey;

	private ShaderProgramMinimalGui minimalGuiShaderProgram;

	public override IList<string> AvailableAudioDevices => audio.Devices;

	public override string CurrentAudioDevice
	{
		get
		{
			return audio.CurrentDevice;
		}
		set
		{
			LoadedSoundNative.ChangeOutputDevice(delegate
			{
				audio.RecreateContext(logger);
			});
		}
	}

	public override float MasterSoundLevel
	{
		get
		{
			return audio.MasterSoundLevel;
		}
		set
		{
			audio.MasterSoundLevel = value;
		}
	}

	public override AssetManager AssetManager => assetManager;

	public override ILogger Logger => logger;

	public override long EllapsedMs => uptimeStopWatch.ElapsedMilliseconds;

	public override Size2i WindowSize => windowsize;

	public override Size2i ScreenSize => screensize;

	public override EnumWindowBorder WindowBorder
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Expected I4, but got Unknown
			return (EnumWindowBorder)((NativeWindow)window).WindowBorder;
		}
		set
		{
			((NativeWindow)window).WindowBorder = (WindowBorder)value;
		}
	}

	public override int CpuCoreCount => cpuCoreCount;

	public override bool IsFocused => ((NativeWindow)window).IsFocused;

	public override bool DebugDrawCalls
	{
		get
		{
			return debugDrawCalls;
		}
		set
		{
			debugDrawCalls = value;
			if (!value)
			{
				logger.Notification("Call stacks:");
				int num = 0;
				foreach (string drawCallStack in drawCallStacks)
				{
					logger.Notification("{0}: {1}", num++, drawCallStack.Substring(0, 600));
				}
			}
			drawCallStacks.Clear();
		}
	}

	public override List<FrameBufferRef> FrameBuffers => frameBuffers;

	public override bool IsServerRunning
	{
		get
		{
			return serverRunning;
		}
		set
		{
			serverRunning = value;
		}
	}

	public override bool IsGamePaused => gamepause;

	public FrameBufferRef CurrentFrameBuffer
	{
		get
		{
			return curFb;
		}
		set
		{
			curFb = value;
			if (value == null)
			{
				GL.BindFramebuffer((FramebufferTarget)36160, 0);
			}
			else
			{
				GL.BindFramebuffer((FramebufferTarget)36160, value.FboId);
			}
		}
	}

	public override bool GlErrorChecking { get; set; }

	public override bool GlDebugMode
	{
		get
		{
			return glDebugMode;
		}
		set
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Expected O, but got Unknown
			if (value)
			{
				if (!supportsGlDebugMode)
				{
					throw new NotSupportedException("Your graphics card does not seem to support gl debug mode (neither GL_ARB_debug_output nor GL_KHR_debug was found)");
				}
				_debugProcCallback = new DebugProc(DebugCallback);
				_debugProcCallbackHandle = GCHandle.Alloc(_debugProcCallback);
				GL.DebugMessageCallback(_debugProcCallback, (IntPtr)IntPtr.Zero);
				GL.Enable((EnableCap)37600);
				GL.Enable((EnableCap)33346);
			}
			else
			{
				GL.Disable((EnableCap)37600);
				GL.Disable((EnableCap)33346);
			}
			glDebugMode = value;
		}
	}

	public override bool GlScissorFlagEnabled => GL.IsEnabled((EnableCap)3089);

	public override string CurrentMouseCursor { get; protected set; }

	public override bool MouseGrabbed
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Invalid comparison between Unknown and I4
			return (int)((NativeWindow)window).CursorState == 2;
		}
		set
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			CursorState val = (CursorState)(value ? 2 : 0);
			if (val != ((NativeWindow)window).CursorState && !RuntimeEnv.IsWaylandSession)
			{
				Vector2 val2 = default(Vector2);
				((Vector2)(ref val2))._002Ector((float)((NativeWindow)window).ClientSize.X / 2f, (float)((NativeWindow)window).ClientSize.Y / 2f);
				SetMousePosition(val2.X, val2.Y);
				((NativeWindow)window).MousePosition = val2;
			}
			((NativeWindow)window).CursorState = val;
		}
	}

	public override DefaultShaderUniforms ShaderUniforms { get; set; } = new DefaultShaderUniforms();

	public override ShaderProgramMinimalGui MinimalGuiShader => minimalGuiShaderProgram;

	public void StartAudio()
	{
		if (audio == null)
		{
			audio = new AudioOpenAl(logger);
		}
	}

	public override void AddAudioSettingsWatchers()
	{
		ClientSettings.Inst.AddWatcher("audioDevice", delegate(string newDevice)
		{
			CurrentAudioDevice = newDevice;
		});
		ClientSettings.Inst.AddWatcher<bool>("useHRTFaudio", delegate
		{
			LoadedSoundNative.ChangeOutputDevice(delegate
			{
				audio.RecreateContext(logger);
			});
		});
	}

	public void StopAudio()
	{
		ScreenManager.IntroMusic?.Dispose();
		if (audio != null)
		{
			audio.Dispose();
			audio = null;
		}
	}

	public override AudioData CreateAudioData(IAsset asset)
	{
		StartAudio();
		AudioMetaData sampleFromArray = audio.GetSampleFromArray(asset);
		sampleFromArray.Loaded = 2;
		return sampleFromArray;
	}

	public override ILoadedSound CreateAudio(SoundParams sound, AudioData data)
	{
		if ((data as AudioMetaData).Asset == null)
		{
			return null;
		}
		if (data.Loaded < 2)
		{
			if (data.Loaded == 0)
			{
				logger.VerboseDebug("Loading sound file, game may stutter " + (data as AudioMetaData)?.Asset.Location);
				data.Load();
			}
			else
			{
				logger.VerboseDebug("Attempt to use still-loading sound file, sound may error or not play " + (data as AudioMetaData)?.Asset.Location);
			}
		}
		return new LoadedSoundNative(sound, (AudioMetaData)data);
	}

	public override ILoadedSound CreateAudio(SoundParams sound, AudioData data, ClientMain game)
	{
		if ((data as AudioMetaData)?.Asset == null)
		{
			return null;
		}
		if (data.Loaded == 0)
		{
			logger.VerboseDebug("Loading sound file, game may stutter " + (data as AudioMetaData)?.Asset.Location);
			data.Load();
		}
		return new LoadedSoundNative(sound, (AudioMetaData)data, game);
	}

	public override void UpdateAudioListener(float posX, float posY, float posZ, float orientX, float orientY, float orientZ)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		StartAudio();
		audio.UpdateListener(new Vector3(posX, posY, posZ), new Vector3(orientX, orientY, orientZ));
	}

	public ClientPlatformWindows(Logger logger)
	{
		if (logger == null)
		{
			this.logger = new NullLogger();
		}
		else
		{
			base.XPlatInterface = XPlatformInterfaces.GetInterface();
			screensize = base.XPlatInterface.GetScreenSize();
			this.logger = logger;
		}
		TyronThreadPool.Inst.Logger = this.logger;
		uptimeStopWatch.Start();
		frameStopWatch = new Stopwatch();
		frameStopWatch.Start();
	}

	private void window_RenderFrame(FrameEventArgs e)
	{
		if (doResize != 0 && Environment.TickCount >= doResize)
		{
			Window_Resize();
		}
		ScreenManager.FrameProfiler.Begin(null);
		if (ClientSettings.VsyncMode != 1 && base.MaxFps > 10f && base.MaxFps < 241f)
		{
			int num = (int)(1000f / base.MaxFps - 1000f * (float)frameStopWatch.ElapsedTicks / (float)Stopwatch.Frequency);
			if (num > 0)
			{
				Thread.Sleep(num);
			}
		}
		float dt = (float)frameStopWatch.ElapsedTicks / (float)Stopwatch.Frequency;
		frameStopWatch.Restart();
		ScreenManager.FrameProfiler.Mark("sleep");
		UpdateMousePosition();
		RenderBloom = ClientSettings.Bloom && base.DoPostProcessingEffects;
		RenderGodRays = ClientSettings.GodRayQuality > 0 && base.DoPostProcessingEffects;
		RenderFXAA = ClientSettings.FXAA && base.DoPostProcessingEffects;
		RenderSSAO = ClientSettings.SSAOQuality > 0 && base.DoPostProcessingEffects;
		SetupSSAO = ClientSettings.SSAOQuality > 0;
		ShadowMapQuality = ClientSettings.ShadowMapQuality;
		ShaderProgramBase.shadowmapQuality = ShadowMapQuality;
		frameHandler.OnNewFrame(dt);
		((GameWindow)window).SwapBuffers();
		ScreenManager.FrameProfiler.End();
	}

	public string GetGraphicsCardRenderer()
	{
		return GL.GetString((StringName)7937);
	}

	public void LogAndTestHardwareInfosStage1()
	{
		logger.Notification("Process path: {0}", Environment.ProcessPath);
		logger.Notification("Operating System: " + RuntimeEnv.GetOsString());
		logger.Notification("CPU Cores: {0}", Environment.ProcessorCount);
		logger.Notification("CPU: {0}", base.XPlatInterface.GetCpuInfo());
		logger.Notification("Available RAM: {0} MB", base.XPlatInterface.GetRamCapacity() / 1024);
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			LogFrameworkVersions();
		}
	}

	private void LogFrameworkVersions()
	{
		logger.Notification("C# Framework: " + GetFrameworkInfos());
		logger.Notification("Cairo Graphics Version: " + CairoAPI.VersionString);
	}

	public void LogAndTestHardwareInfosStage2()
	{
		logger.Notification("Graphics Card Vendor: " + GL.GetString((StringName)7936));
		logger.Notification("Graphics Card Version: " + GL.GetString((StringName)7938));
		logger.Notification("Graphics Card Renderer: " + GL.GetString((StringName)7937));
		logger.Notification("Graphics Card ShadingLanguageVersion: " + GL.GetString((StringName)35724));
		logger.Notification("GL.MaxVertexUniformComponents: " + GL.GetInteger((GetPName)35658));
		logger.Notification("GL.MaxUniformBlockSize: " + GL.GetInteger((GetPName)35376));
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			LogFrameworkVersions();
		}
		logger.Notification("OpenAL Version: " + AL.Get((ALGetString)45058));
		string text = Path.Combine(GamePaths.Binaries, "Lib/OpenTK.dll");
		if (File.Exists(text))
		{
			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(text);
			logger.Notification("OpenTK Version: " + versionInfo.FileVersion + " (" + versionInfo.Comments + ")");
		}
		logger.Notification("Zstd Version: " + ZstdNative.Version);
		CheckGlError("loghwinfo");
		if (RuntimeEnv.OS != OS.Mac && ClientSettings.TestGlExtensions)
		{
			HashSet<string> hashSet = new HashSet<string>(new string[4] { "GL_ARB_framebuffer_object", "GL_ARB_vertex_array_object", "GL_ARB_draw_instanced", "GL_ARB_explicit_attrib_location" });
			int integer = GL.GetInteger((GetPName)33309);
			for (int i = 0; i < integer; i++)
			{
				string text2 = GL.GetString((StringNameIndexed)7939, i);
				supportsGlDebugMode |= text2 == "GL_ARB_debug_output" || text2 == "GL_KHR_debug";
				supportsPersistentMapping |= text2 == "GL_ARB_buffer_storage";
				if (hashSet.Contains(text2))
				{
					hashSet.Remove(text2);
				}
			}
			if (hashSet.Count > 0)
			{
				throw new NotSupportedException("Your graphics card does not support the extensions " + string.Join(", ", hashSet) + " which is required to start the game");
			}
		}
		CheckGlError("testhwinfo");
	}

	public override string GetGraphicCardInfos()
	{
		return "GC Vendor: " + GL.GetString((StringName)7936) + "\nGC Version: " + GL.GetString((StringName)7938) + "\nGC Renderer: " + GL.GetString((StringName)7937) + "\nGC ShaderVersion: " + GL.GetString((StringName)35724);
	}

	public override string GetFrameworkInfos()
	{
		return ".net " + Environment.Version;
	}

	public override bool IsExitAvailable()
	{
		return true;
	}

	public override void SetWindowSize(int width, int height)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		((NativeWindow)window).ClientSize = new Vector2i(width, height);
		Window_Resize();
	}

	public override BitmapRef CreateBitmap(int width, int height)
	{
		return new BitmapExternal(width, height);
	}

	public override void SetBitmapPixelsArgb(BitmapRef bmp, int[] pixels)
	{
		BitmapExternal bitmapExternal = (BitmapExternal)bmp;
		int width = bitmapExternal.bmp.Width;
		int height = bitmapExternal.bmp.Height;
		FastBitmap fastBitmap = new FastBitmap();
		fastBitmap.bmp = bitmapExternal.bmp;
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				fastBitmap.SetPixel(i, j, pixels[i + j * width]);
			}
		}
	}

	public override BitmapRef CreateBitmapFromPng(IAsset asset)
	{
		using MemoryStream ms = new MemoryStream(asset.Data);
		return new BitmapExternal(ms, Logger, asset.Location);
	}

	public override BitmapRef CreateBitmapFromPng(byte[] data)
	{
		return CreateBitmapFromPng(data, data.Length);
	}

	public override BitmapRef CreateBitmapFromPng(byte[] data, int dataLength)
	{
		return new BitmapExternal(data, dataLength, Logger);
	}

	public override BitmapRef CreateBitmapFromPixels(int[] pixels, int width, int height)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		nint num = GCHandle.Alloc(pixels, GCHandleType.Pinned).AddrOfPinnedObject();
		return new BitmapExternal(SKBitmap.FromImage(SKImage.FromPixels(new SKImageInfo(width, height), (IntPtr)num)));
	}

	public override IAviWriter CreateAviWriter(float framerate, string codec)
	{
		return base.XPlatInterface.GetAviWriter(ClientSettings.RecordingBufferSize, framerate, codec);
	}

	public override AvailableCodec[] GetAvailableCodecs()
	{
		return base.XPlatInterface.AvailableCodecs();
	}

	public override string GetGameVersion()
	{
		return "1.21.0";
	}

	public void SetServerExitInterface(GameExit exit)
	{
		gameexit = exit;
	}

	public override void ThreadSpinWait(int iterations)
	{
		Thread.SpinWait(iterations);
	}

	public override void LoadAssets()
	{
		if (assetManager == null)
		{
			assetManager = new AssetManager(GamePaths.AssetsPath, EnumAppSide.Client);
		}
		logger.Notification("Start discovering assets");
		int num = assetManager.InitAndLoadBaseAssets(logger, "textures");
		logger.Notification("Found {0} base assets in total", num);
	}

	public void Start()
	{
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Invalid comparison between Unknown and I4
		((NativeWindow)window).FocusedChanged += window_FocusChanged;
		((NativeWindow)window).KeyDown += game_KeyDown;
		((NativeWindow)window).KeyUp += game_KeyUp;
		((NativeWindow)window).TextInput += game_KeyPress;
		((NativeWindow)window).MouseDown += Mouse_ButtonDown;
		((NativeWindow)window).MouseUp += Mouse_ButtonUp;
		((NativeWindow)window).MouseMove += Mouse_Move;
		((NativeWindow)window).MouseWheel += Mouse_WheelChanged;
		((GameWindow)window).RenderFrame += window_RenderFrame;
		((NativeWindow)window).Closing += window_Closing;
		((NativeWindow)window).Resize += OnWindowResize;
		((NativeWindow)window).Title = "Vintage Story";
		((NativeWindow)window).FileDrop += Window_FileDrop;
		frameBuffers = SetupDefaultFrameBuffers();
		minimalGuiShaderProgram = new ShaderProgramMinimalGui();
		minimalGuiShaderProgram.Compile();
		windowsize.Width = ((NativeWindow)window).ClientSize.X;
		windowsize.Height = ((NativeWindow)window).ClientSize.Y;
		GL.LineWidth(1.5f);
		ErrorCode error = GL.GetError();
		SupportsThickLines = (int)error != 1281;
		cpuCoreCount = Environment.ProcessorCount;
	}

	public override void RebuildFrameBuffers()
	{
		List<FrameBufferRef> buffers = frameBuffers;
		List<FrameBufferRef> list = SetupDefaultFrameBuffers();
		frameBuffers = list;
		DisposeFrameBuffers(buffers);
	}

	private void Window_FileDrop(FileDropEventArgs e)
	{
		fileDropEventHandler?.Invoke(((FileDropEventArgs)(ref e)).FileNames[0]);
	}

	private void OnWindowResize(ResizeEventArgs e)
	{
		doResize = Environment.TickCount + 40;
	}

	private void Window_Resize()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Invalid comparison between Unknown and I4
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Invalid comparison between Unknown and I4
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		doResize = 0;
		if ((int)((NativeWindow)window).WindowState != 1)
		{
			Vector2i clientSize = ((NativeWindow)window).ClientSize;
			if (((NativeWindow)window).ClientSize.X < 600)
			{
				clientSize.X = 600;
			}
			if (((NativeWindow)window).ClientSize.Y < 400)
			{
				clientSize.Y = 400;
			}
			if (((NativeWindow)window).ClientSize.Y < 400 || ((NativeWindow)window).ClientSize.X < 600)
			{
				((NativeWindow)window).ClientSize = clientSize;
			}
		}
		if (((NativeWindow)window).ClientSize.X == 0 || ((NativeWindow)window).ClientSize.Y == 0)
		{
			logger.Notification("Window was resized to {0} {1}? Window probably got minimized. Will not rebuild frame buffers", ((NativeWindow)window).ClientSize.X, ((NativeWindow)window).ClientSize.Y);
		}
		else if (((NativeWindow)window).ClientSize.X != windowsize.Width || ((NativeWindow)window).ClientSize.Y != windowsize.Height)
		{
			logger.Notification("Window was resized to {0} {1}, rebuilding framebuffers...", ((NativeWindow)window).ClientSize.X, ((NativeWindow)window).ClientSize.Y);
			RebuildFrameBuffers();
			windowsize.Width = ((NativeWindow)window).ClientSize.X;
			windowsize.Height = ((NativeWindow)window).ClientSize.Y;
			if ((int)((NativeWindow)window).WindowState == 0)
			{
				ClientSettings.ScreenWidth = ((NativeWindow)window).Size.X;
				ClientSettings.ScreenHeight = ((NativeWindow)window).Size.Y;
			}
			WindowState windowState = ((NativeWindow)window).WindowState;
			int num = (((int)windowState == 2) ? 2 : (((int)windowState == 3) ? ((ClientSettings.GameWindowMode != 3) ? 1 : 3) : 0));
			int num2 = num;
			if (ClientSettings.GameWindowMode != num2)
			{
				ClientSettings.GameWindowMode = num2;
			}
			TriggerWindowResized(((NativeWindow)window).ClientSize.X, ((NativeWindow)window).ClientSize.Y);
		}
	}

	private void window_Closing(CancelEventArgs e)
	{
		gameexit.exit = true;
		try
		{
			windowClosedHandler();
		}
		catch (Exception)
		{
		}
	}

	public override void SetVSync(bool enabled)
	{
		((NativeWindow)window).VSync = (VSyncMode)(enabled ? 1 : 0);
	}

	public unsafe override void SetDirectMouseMode(bool enabled)
	{
		GLFW.SetInputMode(((NativeWindow)window).WindowPtr, (RawMouseMotionAttribute)208901, enabled);
	}

	public override string SaveScreenshot(string path = null, string filename = null, bool withAlpha = false, bool flip = false, string metaDataStr = null)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		screenshot.d_GameWindow = (GameWindow)(object)window;
		FrameBufferRef currentFrameBuffer = CurrentFrameBuffer;
		Size2i size = ((currentFrameBuffer == null) ? new Size2i(((NativeWindow)window).ClientSize.X, ((NativeWindow)window).ClientSize.Y) : new Size2i(currentFrameBuffer.Width, currentFrameBuffer.Height));
		return screenshot.SaveScreenshot(this, size, path, filename, withAlpha, flip, metaDataStr);
	}

	public override BitmapRef GrabScreenshot(bool withAlpha = false, bool scale = false)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		screenshot.d_GameWindow = (GameWindow)(object)window;
		FrameBufferRef currentFrameBuffer = CurrentFrameBuffer;
		Size2i size = ((currentFrameBuffer == null) ? new Size2i(((NativeWindow)window).ClientSize.X, ((NativeWindow)window).ClientSize.Y) : new Size2i(currentFrameBuffer.Width, currentFrameBuffer.Height));
		return new BitmapExternal(screenshot.GrabScreenshot(size, scale, flip: false, withAlpha));
	}

	public override BitmapRef GrabScreenshot(int width, int height, bool scaleScreenshot, bool flip, bool withAlpha = false)
	{
		screenshot.d_GameWindow = (GameWindow)(object)window;
		return new BitmapExternal(screenshot.GrabScreenshot(new Size2i(width, height), scaleScreenshot, flip, withAlpha));
	}

	public override void WindowExit(string reason)
	{
		logger.Notification("Exiting game now. Server running=" + serverRunning + ". Exit reason: {0}", reason);
		base.IsShuttingDown = true;
		if (gameexit != null)
		{
			gameexit.exit = true;
		}
		try
		{
			UriHandler.Instance.Dispose();
			GameWindowNative gameWindowNative = window;
			if (gameWindowNative != null)
			{
				((NativeWindow)gameWindowNative).Close();
			}
		}
		catch (Exception)
		{
			Environment.Exit(0);
		}
	}

	public override void SetTitle(string applicationname)
	{
		((NativeWindow)window).Title = applicationname;
	}

	public override WindowState GetWindowState()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return ((NativeWindow)window).WindowState;
	}

	public override void SetWindowState(WindowState value)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		((NativeWindow)window).WindowState = value;
		if (((NativeWindow)window).Location.Y < 0)
		{
			((NativeWindow)window).Location = new Vector2i(((NativeWindow)window).Location.X, 0);
		}
	}

	public unsafe override void SetWindowAttribute(WindowAttribute attribute, bool value)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		GLFW.SetWindowAttrib(((NativeWindow)window).WindowPtr, attribute, value);
	}

	public override void StartSinglePlayerServer(StartServerArgs serverargs)
	{
		ServerExit = new GameExit();
		OnStartSinglePlayerServer(serverargs);
	}

	public override void ExitSinglePlayerServer()
	{
		ServerExit.SetExit(p: true);
	}

	public override bool IsLoadedSinglePlayerServer()
	{
		return singlePlayerServerLoaded;
	}

	public override DummyNetwork[] GetSinglePlayerServerNetwork()
	{
		return singlePlayerServerDummyNetwork;
	}

	public override void SetFileDropHandler(Action<string> handler)
	{
		fileDropEventHandler = handler;
	}

	public override void RegisterOnFocusChange(OnFocusChanged handler)
	{
		focusChangedDelegates.Add(handler);
	}

	private void window_FocusChanged(FocusedChangedEventArgs e)
	{
		foreach (OnFocusChanged focusChangedDelegate in focusChangedDelegates)
		{
			focusChangedDelegate(((NativeWindow)window).IsFocused);
		}
	}

	public override void SetWindowClosedHandler(Action handler)
	{
		windowClosedHandler = handler;
	}

	public override void SetFrameHandler(NewFrameHandler handler)
	{
		frameHandler = handler;
	}

	public override void RegisterKeyboardEvent(KeyEventHandler handler)
	{
		keyEventHandlers.Add(handler);
	}

	public override void RegisterMouseEvent(MouseEventHandler handler)
	{
		mouseEventHandlers.Add(handler);
	}

	public override void AddOnCrash(OnCrashHandler handler)
	{
		crashreporter.OnCrash = OnCrash;
		onCrashHandler = handler;
	}

	public override void ClearOnCrash()
	{
		onCrashHandler = null;
		crashreporter.OnCrash = null;
	}

	private void OnCrash()
	{
		if (onCrashHandler != null)
		{
			onCrashHandler.OnCrash();
		}
	}

	public override void RenderMesh(MeshRef modelRef)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		RuntimeStats.drawCallsCount++;
		if (debugDrawCalls)
		{
			drawCallStacks.Add(Environment.StackTrace);
		}
		VAO vAO = (VAO)modelRef;
		if (vAO.VaoId == 0 || vAO.Disposed)
		{
			if (vAO.VaoId == 0)
			{
				throw new ArgumentException("Fatal: Trying to render an uninitialized mesh");
			}
			throw new ArgumentException("Fatal: Trying to render a disposed mesh");
		}
		GL.BindVertexArray(vAO.VaoId);
		for (int i = 0; i < vAO.vaoSlotNumber; i++)
		{
			GL.EnableVertexAttribArray(i);
		}
		GL.BindBuffer((BufferTarget)34963, vAO.vboIdIndex);
		GL.DrawElements(vAO.drawMode, vAO.IndicesCount, (DrawElementsType)5125, 0);
		GL.BindBuffer((BufferTarget)34963, 0);
		for (int j = 0; j < vAO.vaoSlotNumber; j++)
		{
			GL.DisableVertexAttribArray(j);
		}
		GL.BindVertexArray(0);
	}

	public void RenderFullscreenTriangle(MeshRef modelRef)
	{
		RuntimeStats.drawCallsCount++;
		GL.BindVertexArray(((VAO)modelRef).VaoId);
		GL.DrawArrays((PrimitiveType)4, 0, 3);
		GL.BindVertexArray(0);
	}

	public override void RenderMesh(MeshRef modelRef, int[] indices, int[] indicesSizes, int groupCount)
	{
		RenderMesh(modelRef, indices, indicesSizes, groupCount, useSSBOs: false);
	}

	public override void RenderMesh(MeshRef modelRef, int[] indices, int[] indicesSizes, int groupCount, bool useSSBOs)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		RuntimeStats.drawCallsCount++;
		VAO vAO = (VAO)modelRef;
		GL.BindVertexArray(vAO.VaoId);
		for (int i = 0; i < vAO.vaoSlotNumber; i++)
		{
			GL.EnableVertexAttribArray(i);
		}
		if (useSSBOs)
		{
			GL.BindBuffer((BufferTarget)34963, ClientPlatformAbstract.singleIndexBufferId);
			GL.BindBufferBase((BufferRangeTarget)37074, 3, vAO.xyzVboId);
			GL.MultiDrawElements<int>(vAO.drawMode, indicesSizes, (DrawElementsType)5125, indices, groupCount);
			GL.BindBufferBase((BufferRangeTarget)37074, 3, 0);
		}
		else
		{
			GL.BindBuffer((BufferTarget)34963, vAO.vboIdIndex);
			GL.MultiDrawElements<int>(vAO.drawMode, indicesSizes, (DrawElementsType)5125, indices, groupCount);
		}
		GL.BindBuffer((BufferTarget)34963, 0);
		for (int j = 0; j < vAO.vaoSlotNumber; j++)
		{
			GL.DisableVertexAttribArray(j);
		}
		GL.BindVertexArray(0);
	}

	public override void RenderMeshInstanced(MeshRef modelRef, int quantity = 1)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		RuntimeStats.drawCallsCount++;
		VAO vAO = (VAO)modelRef;
		GL.BindVertexArray(vAO.VaoId);
		for (int i = 0; i < vAO.vaoSlotNumber; i++)
		{
			GL.EnableVertexAttribArray(i);
		}
		GL.BindBuffer((BufferTarget)34963, vAO.vboIdIndex);
		GL.DrawElementsInstanced(vAO.drawMode, vAO.IndicesCount, (DrawElementsType)5125, (IntPtr)IntPtr.Zero, quantity);
		GL.BindBuffer((BufferTarget)34963, 0);
		for (int j = 0; j < vAO.vaoSlotNumber; j++)
		{
			GL.DisableVertexAttribArray(j);
		}
		GL.BindVertexArray(0);
	}

	public override void SetGamePausedState(bool paused)
	{
		gamepause = paused;
	}

	public override void ResetGamePauseAndUptimeState()
	{
		uptimeStopWatch.Start();
	}

	public override void ToggleOffscreenBuffer(bool enable)
	{
		OffscreenBuffer = enable;
	}

	public override void DisposeFrameBuffer(FrameBufferRef frameBuffer, bool disposeTextures = true)
	{
		if (frameBuffer == null)
		{
			return;
		}
		if (disposeTextures)
		{
			for (int i = 0; i < frameBuffer.ColorTextureIds.Length; i++)
			{
				GLDeleteTexture(frameBuffer.ColorTextureIds[i]);
			}
			if (frameBuffer.DepthTextureId > 0)
			{
				GLDeleteTexture(frameBuffer.DepthTextureId);
			}
		}
		GL.DeleteFramebuffer(frameBuffer.FboId);
	}

	public override FrameBufferRef CreateFramebuffer(FramebufferAttrs fbAttrs)
	{
		FrameBufferRef frameBufferRef = (CurrentFrameBuffer = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = fbAttrs.Width,
			Height = fbAttrs.Height
		});
		List<DrawBuffersEnum> list = new List<DrawBuffersEnum>();
		List<int> list2 = new List<int>();
		FramebufferAttrsAttachment[] attachments = fbAttrs.Attachments;
		foreach (FramebufferAttrsAttachment framebufferAttrsAttachment in attachments)
		{
			RawTexture texture = framebufferAttrsAttachment.Texture;
			int num = ((texture.TextureId == 0) ? GL.GenTexture() : texture.TextureId);
			if (texture.TextureId == 0)
			{
				GL.BindTexture((TextureTarget)3553, num);
				GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)texture.PixelInternalFormat, texture.Width, texture.Height, 0, (PixelFormat)texture.PixelFormat, (PixelType)5126, (IntPtr)IntPtr.Zero);
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, (int)texture.MinFilter);
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, (int)texture.MagFilter);
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, (int)texture.WrapS);
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, (int)texture.WrapT);
			}
			GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)framebufferAttrsAttachment.AttachmentType, (TextureTarget)3553, num, 0);
			if (framebufferAttrsAttachment.AttachmentType == EnumFramebufferAttachment.DepthAttachment)
			{
				GL.DepthFunc((DepthFunction)513);
				frameBufferRef.DepthTextureId = num;
			}
			else
			{
				list.Add((DrawBuffersEnum)framebufferAttrsAttachment.AttachmentType);
				list2.Add(texture.TextureId = num);
			}
		}
		frameBufferRef.ColorTextureIds = list2.ToArray();
		GL.DrawBuffers(list.Count, list.ToArray());
		CheckFboStatus((FramebufferTarget)36160, fbAttrs.Name);
		CurrentFrameBuffer = null;
		GL.BindTexture((TextureTarget)3553, 0);
		return frameBufferRef;
	}

	public List<FrameBufferRef> SetupDefaultFrameBuffers()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_0576: Unknown result type (might be due to invalid IL or missing references)
		//IL_0665: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b13: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b88: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bfe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c62: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ccf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d44: Unknown result type (might be due to invalid IL or missing references)
		//IL_0db2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a8c: Unknown result type (might be due to invalid IL or missing references)
		SetupSSAO = ClientSettings.SSAOQuality > 0;
		if (ClientSettings.IsNewSettingsFile && ((NativeWindow)window).ClientSize.X > 1920)
		{
			ClientSettings.SSAA = 0.5f;
		}
		List<FrameBufferRef> list = new List<FrameBufferRef>(31);
		for (int i = 0; i <= 24; i++)
		{
			list.Add(null);
		}
		ShadowMapQuality = ClientSettings.ShadowMapQuality;
		ssaaLevel = ClientSettings.SSAA;
		int num = (int)((float)((NativeWindow)window).ClientSize.X * ssaaLevel);
		int num2 = (int)((float)((NativeWindow)window).ClientSize.Y * ssaaLevel);
		if (num == 0 || num2 == 0)
		{
			return list;
		}
		PixelFormat val = (PixelFormat)6408;
		CheckGlError("sdfb-begin");
		FrameBufferRef frameBufferRef = (list[0] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = num,
			Height = num2
		});
		FrameBufferRef frameBufferRef3 = frameBufferRef;
		frameBufferRef3.DepthTextureId = GL.GenTexture();
		if (frameBufferRef3.FboId == 0)
		{
			base.XPlatInterface.ShowMessageBox("Fatal error", "Unable to generate a new framebuffer. This shouldn't happen, ever. Maybe a restart resolves the problem?");
		}
		CurrentFrameBuffer = frameBufferRef3;
		GL.BindTexture((TextureTarget)3553, frameBufferRef3.DepthTextureId);
		GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)33191, num, num2, 0, (PixelFormat)6402, (PixelType)5126, (IntPtr)IntPtr.Zero);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9728);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9728);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, 33071);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, 33071);
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36096, (TextureTarget)3553, frameBufferRef3.DepthTextureId, 0);
		GL.DepthFunc((DepthFunction)513);
		frameBufferRef3.ColorTextureIds = ArrayUtil.CreateFilled(SetupSSAO ? 4 : 2, (int n) => GL.GenTexture());
		GL.BindTexture((TextureTarget)3553, frameBufferRef3.ColorTextureIds[0]);
		GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)32856, num, num2, 0, val, (PixelType)5123, (IntPtr)IntPtr.Zero);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, (ssaaLevel <= 1f) ? 9728 : 9729);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, (ssaaLevel <= 1f) ? 9728 : 9729);
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36064, (TextureTarget)3553, frameBufferRef3.ColorTextureIds[0], 0);
		GL.BindTexture((TextureTarget)3553, frameBufferRef3.ColorTextureIds[1]);
		GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)32856, num, num2, 0, val, (PixelType)5121, (IntPtr)IntPtr.Zero);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, (ssaaLevel <= 1f) ? 9728 : 9729);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, (ssaaLevel <= 1f) ? 9728 : 9729);
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36065, (TextureTarget)3553, frameBufferRef3.ColorTextureIds[1], 0);
		if (SetupSSAO)
		{
			GL.BindTexture((TextureTarget)3553, frameBufferRef3.ColorTextureIds[2]);
			GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)34842, num, num2, 0, val, (PixelType)5126, (IntPtr)IntPtr.Zero);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9729);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)4100, new float[4] { 1f, 1f, 1f, 1f });
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, 33069);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, 33069);
			GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36066, (TextureTarget)3553, frameBufferRef3.ColorTextureIds[2], 0);
			GL.BindTexture((TextureTarget)3553, frameBufferRef3.ColorTextureIds[3]);
			GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)34842, num, num2, 0, (PixelFormat)6408, (PixelType)5126, (IntPtr)IntPtr.Zero);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9729);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)4100, new float[4] { 1f, 1f, 1f, 1f });
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, 33069);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, 33069);
			GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36067, (TextureTarget)3553, frameBufferRef3.ColorTextureIds[3], 0);
			DrawBuffersEnum[] array = new DrawBuffersEnum[4];
			RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
			DrawBuffersEnum[] array2 = (DrawBuffersEnum[])(object)array;
			GL.DrawBuffers(4, array2);
		}
		else
		{
			DrawBuffersEnum[] array3 = (DrawBuffersEnum[])(object)new DrawBuffersEnum[2]
			{
				(DrawBuffersEnum)36064,
				(DrawBuffersEnum)36065
			};
			GL.DrawBuffers(2, array3);
		}
		CheckFboStatus((FramebufferTarget)36160, EnumFrameBuffer.Primary);
		frameBufferRef = (list[1] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = num,
			Height = num2
		});
		frameBufferRef3 = frameBufferRef;
		frameBufferRef3.ColorTextureIds = new int[3]
		{
			GL.GenTexture(),
			GL.GenTexture(),
			GL.GenTexture()
		};
		CurrentFrameBuffer = frameBufferRef3;
		GL.BindTexture((TextureTarget)3553, frameBufferRef3.ColorTextureIds[0]);
		GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)34842, num, num2, 0, val, (PixelType)5123, (IntPtr)IntPtr.Zero);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9729);
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36064, (TextureTarget)3553, frameBufferRef3.ColorTextureIds[0], 0);
		GL.BindTexture((TextureTarget)3553, frameBufferRef3.ColorTextureIds[1]);
		GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)33325, num, num2, 0, (PixelFormat)6403, (PixelType)5123, (IntPtr)IntPtr.Zero);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9729);
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36065, (TextureTarget)3553, frameBufferRef3.ColorTextureIds[1], 0);
		GL.BindTexture((TextureTarget)3553, frameBufferRef3.ColorTextureIds[2]);
		GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)32856, num, num2, 0, val, (PixelType)5121, (IntPtr)IntPtr.Zero);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9729);
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36066, (TextureTarget)3553, frameBufferRef3.ColorTextureIds[2], 0);
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36096, (TextureTarget)3553, list[0].DepthTextureId, 0);
		DrawBuffersEnum[] array4 = new DrawBuffersEnum[3];
		RuntimeHelpers.InitializeArray(array4, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		DrawBuffersEnum[] array5 = (DrawBuffersEnum[])(object)array4;
		GL.DrawBuffers(3, array5);
		ClearFrameBuffer(EnumFrameBuffer.Transparent);
		CheckFboStatus((FramebufferTarget)36160, EnumFrameBuffer.Transparent);
		if (SetupSSAO)
		{
			_ = ClientSettings.SSAOQuality;
			float num3 = 0.5f;
			FrameBufferRef obj = new FrameBufferRef
			{
				FboId = GL.GenFramebuffer(),
				Width = (int)((float)num * num3),
				Height = (int)((float)num2 * num3)
			};
			frameBufferRef = obj;
			list[13] = obj;
			frameBufferRef3 = (CurrentFrameBuffer = frameBufferRef);
			frameBufferRef3.ColorTextureIds = new int[2]
			{
				GL.GenTexture(),
				GL.GenTexture()
			};
			GL.BindTexture((TextureTarget)3553, frameBufferRef3.ColorTextureIds[0]);
			GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)6407, frameBufferRef3.Width, frameBufferRef3.Height, 0, (PixelFormat)6407, (PixelType)5126, (IntPtr)IntPtr.Zero);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9728);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9728);
			GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36064, (TextureTarget)3553, frameBufferRef3.ColorTextureIds[0], 0);
			Random random = new Random(5);
			_ = frameBufferRef3.ColorTextureIds[1];
			int num4 = 16;
			float[] array6 = new float[num4 * num4 * 3];
			Vec3f vec3f = new Vec3f();
			for (int num5 = 0; num5 < num4 * num4; num5++)
			{
				vec3f.Set((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f, 0f).Normalize();
				array6[num5 * 3] = vec3f.X;
				array6[num5 * 3 + 1] = vec3f.Y;
				array6[num5 * 3 + 2] = vec3f.Z;
			}
			GL.BindTexture((TextureTarget)3553, frameBufferRef3.ColorTextureIds[1]);
			GL.TexImage2D<float>((TextureTarget)3553, 0, (PixelInternalFormat)34836, num4, num4, 0, (PixelFormat)6407, (PixelType)5126, array6);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9728);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9728);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, 10497);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, 10497);
			for (int num6 = 0; num6 < 64; num6++)
			{
				Vec3f vec3f2 = new Vec3f((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f, (float)random.NextDouble());
				vec3f2.Normalize();
				vec3f2 *= (float)random.NextDouble();
				float num7 = (float)num6 / 64f;
				num7 = GameMath.Lerp(0.1f, 1f, num7 * num7);
				vec3f2 *= num7;
				ssaoKernel[num6 * 3] = vec3f2.X;
				ssaoKernel[num6 * 3 + 1] = vec3f2.Y;
				ssaoKernel[num6 * 3 + 2] = vec3f2.Z;
			}
			GL.DrawBuffer((DrawBufferMode)36064);
			CheckFboStatus((FramebufferTarget)36160, EnumFrameBuffer.SSAO);
			EnumFrameBuffer[] array7 = new EnumFrameBuffer[2]
			{
				EnumFrameBuffer.SSAOBlurVertical,
				EnumFrameBuffer.SSAOBlurHorizontal
			};
			foreach (EnumFrameBuffer enumFrameBuffer in array7)
			{
				frameBufferRef = (list[(int)enumFrameBuffer] = new FrameBufferRef
				{
					FboId = GL.GenFramebuffer(),
					Width = (int)((float)num * num3),
					Height = (int)((float)num2 * num3)
				});
				frameBufferRef3 = (CurrentFrameBuffer = frameBufferRef);
				frameBufferRef3.ColorTextureIds = new int[1] { GL.GenTexture() };
				setupAttachment(frameBufferRef3, frameBufferRef3.Width, frameBufferRef3.Height, 0, val, (PixelInternalFormat)32856);
				GL.DrawBuffer((DrawBufferMode)36064);
				CheckFboStatus((FramebufferTarget)36160, enumFrameBuffer);
			}
		}
		frameBufferRef = (list[2] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = num / 2,
			Height = num2 / 2
		});
		frameBufferRef3 = (CurrentFrameBuffer = frameBufferRef);
		frameBufferRef3.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBufferRef3, num / 2, num2 / 2, 0, val, (PixelInternalFormat)32856);
		GL.DrawBuffer((DrawBufferMode)36064);
		CheckFboStatus((FramebufferTarget)36160, EnumFrameBuffer.BlurHorizontalMedRes);
		frameBufferRef = (list[3] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = num / 2,
			Height = num2 / 2
		});
		frameBufferRef3 = (CurrentFrameBuffer = frameBufferRef);
		frameBufferRef3.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBufferRef3, num / 2, num2 / 2, 0, val, (PixelInternalFormat)32856);
		GL.DrawBuffer((DrawBufferMode)36064);
		CheckFboStatus((FramebufferTarget)36160, EnumFrameBuffer.BlurVerticalMedRes);
		frameBufferRef = (list[9] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = num / 4,
			Height = num2 / 4
		});
		frameBufferRef3 = (CurrentFrameBuffer = frameBufferRef);
		frameBufferRef3.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBufferRef3, num / 4, num2 / 4, 0, val, (PixelInternalFormat)32856);
		GL.DrawBuffer((DrawBufferMode)36064);
		CheckFboStatus((FramebufferTarget)36160, EnumFrameBuffer.BlurHorizontalLowRes);
		frameBufferRef = (list[8] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer()
		});
		frameBufferRef3 = (CurrentFrameBuffer = frameBufferRef);
		frameBufferRef3.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBufferRef3, num / 4, num2 / 4, 0, val, (PixelInternalFormat)32856);
		GL.DrawBuffer((DrawBufferMode)36064);
		CheckFboStatus((FramebufferTarget)36160, EnumFrameBuffer.BlurVerticalLowRes);
		frameBufferRef = (list[4] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = num,
			Height = num2
		});
		frameBufferRef3 = (CurrentFrameBuffer = frameBufferRef);
		frameBufferRef3.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBufferRef3, num, num2, 0, val, (PixelInternalFormat)34842);
		GL.DrawBuffer((DrawBufferMode)36064);
		CheckFboStatus((FramebufferTarget)36160, EnumFrameBuffer.FindBright);
		frameBufferRef = (list[7] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = num / 2,
			Height = num2 / 2
		});
		frameBufferRef3 = (CurrentFrameBuffer = frameBufferRef);
		frameBufferRef3.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBufferRef3, num / 2, num2 / 2, 0, val, (PixelInternalFormat)34842);
		GL.DrawBuffer((DrawBufferMode)36064);
		CheckFboStatus((FramebufferTarget)36160, EnumFrameBuffer.GodRays);
		frameBufferRef = (list[10] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = num,
			Height = num2
		});
		frameBufferRef3 = (CurrentFrameBuffer = frameBufferRef);
		frameBufferRef3.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBufferRef3, num, num2, 0, val, (PixelInternalFormat)34842);
		GL.DrawBuffer((DrawBufferMode)36064);
		CheckFboStatus((FramebufferTarget)36160, EnumFrameBuffer.Luma);
		frameBufferRef = (list[5] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = num / 4,
			Height = num2 / 4
		});
		frameBufferRef3 = frameBufferRef;
		frameBufferRef3.ColorTextureIds = Array.Empty<int>();
		CheckGlError("sdfb-lide");
		CurrentFrameBuffer = frameBufferRef3;
		frameBufferRef3.DepthTextureId = GL.GenTexture();
		GL.BindTexture((TextureTarget)3553, frameBufferRef3.DepthTextureId);
		GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)33191, frameBufferRef3.Width, frameBufferRef3.Height, 0, (PixelFormat)6402, (PixelType)5126, (IntPtr)IntPtr.Zero);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9729);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)34892, 0);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, 33071);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, 33071);
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36096, (TextureTarget)3553, frameBufferRef3.DepthTextureId, 0);
		GL.DepthFunc((DepthFunction)513);
		GL.DrawBuffer((DrawBufferMode)0);
		GL.ReadBuffer((ReadBufferMode)0);
		CheckFboStatus((FramebufferTarget)36160, EnumFrameBuffer.LiquidDepth);
		int num9 = Math.Max(4, ShadowMapQuality + 2) * 1024;
		int num10 = Math.Max(4, ShadowMapQuality + 2) * 1024;
		frameBufferRef = (list[11] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = num9,
			Height = num10
		});
		frameBufferRef3 = frameBufferRef;
		frameBufferRef3.ColorTextureIds = Array.Empty<int>();
		CheckGlError("sdfb-fsm");
		if (ShadowMapQuality > 0)
		{
			CurrentFrameBuffer = frameBufferRef3;
			frameBufferRef3.DepthTextureId = GL.GenTexture();
			GL.BindTexture((TextureTarget)3553, frameBufferRef3.DepthTextureId);
			GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)33191, num9, num10, 0, (PixelFormat)6402, (PixelType)5126, (IntPtr)IntPtr.Zero);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9729);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)34892, 34894);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)34893, 515);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)4100, new float[4] { 1f, 1f, 1f, 1f });
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, 33069);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, 33069);
			GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36096, (TextureTarget)3553, frameBufferRef3.DepthTextureId, 0);
			GL.DepthFunc((DepthFunction)513);
			GL.DrawBuffer((DrawBufferMode)0);
			GL.ReadBuffer((ReadBufferMode)0);
			CheckFboStatus((FramebufferTarget)36160, EnumFrameBuffer.ShadowmapFar);
		}
		frameBufferRef = (list[12] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = num9,
			Height = num10
		});
		frameBufferRef3 = frameBufferRef;
		frameBufferRef3.ColorTextureIds = Array.Empty<int>();
		CheckGlError("sdfb-nsm-before");
		if (ShadowMapQuality > 1)
		{
			CurrentFrameBuffer = frameBufferRef3;
			frameBufferRef3.DepthTextureId = GL.GenTexture();
			GL.BindTexture((TextureTarget)3553, frameBufferRef3.DepthTextureId);
			GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)33191, num9, num10, 0, (PixelFormat)6402, (PixelType)5126, (IntPtr)IntPtr.Zero);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9729);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)34892, 34894);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)34893, 515);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)4100, new float[4] { 1f, 1f, 1f, 1f });
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, 33069);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, 33069);
			GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36096, (TextureTarget)3553, frameBufferRef3.DepthTextureId, 0);
			GL.DepthFunc((DepthFunction)513);
			GL.DrawBuffer((DrawBufferMode)0);
			GL.ReadBuffer((ReadBufferMode)0);
			CheckFboStatus((FramebufferTarget)36160, EnumFrameBuffer.ShadowmapNear);
		}
		CheckGlError("sdfb-nsm-after");
		PixelPackBuffer = new GLBuffer[3];
		for (int num11 = 0; num11 < PixelPackBuffer.Length; num11++)
		{
			PixelPackBuffer[num11] = new GLBuffer
			{
				BufferId = GL.GenBuffer()
			};
			GL.BindBuffer((BufferTarget)35051, PixelPackBuffer[num11].BufferId);
			int num12 = 4 * sampleCount;
			GL.BufferData((BufferTarget)35051, num12, (IntPtr)IntPtr.Zero, (BufferUsageHint)35041);
		}
		GL.BindBuffer((BufferTarget)35051, 0);
		MeshData customQuadModelData = QuadMeshUtil.GetCustomQuadModelData(-1f, -1f, 0f, 2f, 2f);
		customQuadModelData.Normals = null;
		customQuadModelData.Rgba = null;
		customQuadModelData.Uv = null;
		if (screenQuad != null)
		{
			screenQuad.Dispose();
		}
		screenQuad = UploadMesh(customQuadModelData);
		if (OffscreenBuffer)
		{
			CurrentFrameBuffer = list[0];
		}
		else
		{
			CurrentFrameBuffer = null;
			GL.DrawBuffer((DrawBufferMode)1029);
		}
		logger.Notification("(Re-)loaded frame buffers");
		return list;
	}

	private void setupAttachment(FrameBufferRef frameBuffer, int width, int height, int index, PixelFormat rgbaFormat, PixelInternalFormat dataFormat)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Invalid comparison between Unknown and I4
		GL.BindTexture((TextureTarget)3553, frameBuffer.ColorTextureIds[index]);
		GL.TexImage2D((TextureTarget)3553, 0, dataFormat, width, height, 0, rgbaFormat, (PixelType)(((int)dataFormat == 34842) ? 5123 : 5121), (IntPtr)IntPtr.Zero);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9729);
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36064, (TextureTarget)3553, frameBuffer.ColorTextureIds[index], 0);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, Convert.ToInt32((object)(TextureWrapMode)33071));
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, Convert.ToInt32((object)(TextureWrapMode)33071));
	}

	public void DisposeFrameBuffers(List<FrameBufferRef> buffers)
	{
		for (int i = 0; i < buffers.Count; i++)
		{
			if (buffers[i] != null)
			{
				GL.DeleteFramebuffer(buffers[i].FboId);
				GL.DeleteTexture(buffers[i].DepthTextureId);
				for (int j = 0; j < buffers[i].ColorTextureIds.Length; j++)
				{
					GL.DeleteTexture(buffers[i].ColorTextureIds[j]);
				}
			}
		}
	}

	public override void ClearFrameBuffer(FrameBufferRef framebuffer, bool clearDepth = true)
	{
		ClearFrameBuffer(framebuffer, clearColor, clearDepth);
	}

	public override void ClearFrameBuffer(FrameBufferRef framebuffer, float[] clearColor, bool clearDepthBuffer = true, bool clearColorBuffers = true)
	{
		CurrentFrameBuffer = framebuffer;
		if (clearColorBuffers)
		{
			for (int i = 0; i < framebuffer.ColorTextureIds.Length; i++)
			{
				GL.ClearBuffer((ClearBuffer)6144, i, clearColor);
			}
		}
		if (clearDepthBuffer)
		{
			float num = 1f;
			GL.ClearBuffer((ClearBuffer)6145, 0, ref num);
		}
	}

	public override void LoadFrameBuffer(FrameBufferRef frameBuffer, int textureId)
	{
		CurrentFrameBuffer = frameBuffer;
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36064, (TextureTarget)3553, textureId, 0);
		GL.Viewport(0, 0, frameBuffer.Width, frameBuffer.Height);
	}

	public override void LoadFrameBuffer(FrameBufferRef frameBuffer)
	{
		CurrentFrameBuffer = frameBuffer;
		GL.Viewport(0, 0, frameBuffer.Width, frameBuffer.Height);
	}

	public override void UnloadFrameBuffer(FrameBufferRef frameBuffer)
	{
		LoadFrameBuffer(EnumFrameBuffer.Primary);
	}

	public override void ClearFrameBuffer(EnumFrameBuffer framebuffer)
	{
		switch (framebuffer)
		{
		case EnumFrameBuffer.Default:
			CurrentFrameBuffer = null;
			GL.DrawBuffer((DrawBufferMode)1029);
			GL.Clear((ClearBufferMask)16640);
			CurrentFrameBuffer = frameBuffers[0];
			break;
		case EnumFrameBuffer.Primary:
		{
			GL.ClearBuffer((ClearBuffer)6144, 0, new float[4] { 0f, 0f, 0f, 1f });
			GL.ClearBuffer((ClearBuffer)6144, 1, new float[4] { 0f, 0f, 0f, 1f });
			if (RenderSSAO)
			{
				GL.ClearBuffer((ClearBuffer)6144, 2, new float[4] { 0f, 0f, 0f, 1f });
				GL.ClearBuffer((ClearBuffer)6144, 3, new float[4] { 0f, 0f, 0f, 1f });
			}
			float num2 = 1f;
			GL.ClearBuffer((ClearBuffer)6145, 0, ref num2);
			break;
		}
		case EnumFrameBuffer.LiquidDepth:
		case EnumFrameBuffer.ShadowmapFar:
		case EnumFrameBuffer.ShadowmapNear:
		{
			FrameBufferRef frameBufferRef = FrameBuffers[(int)framebuffer];
			float num = 1f;
			GL.Viewport(0, 0, frameBufferRef.Width, frameBufferRef.Height);
			GL.ClearBuffer((ClearBuffer)6145, 0, ref num);
			break;
		}
		case EnumFrameBuffer.Transparent:
			GL.ClearBuffer((ClearBuffer)6144, 0, new float[4]);
			GL.ClearBuffer((ClearBuffer)6144, 1, new float[4] { 1f, 0f, 0f, 0f });
			GL.ClearBuffer((ClearBuffer)6144, 2, new float[4]);
			break;
		}
	}

	public override void LoadFrameBuffer(EnumFrameBuffer framebuffer)
	{
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_036a: Unknown result type (might be due to invalid IL or missing references)
		switch (framebuffer)
		{
		case EnumFrameBuffer.Transparent:
		{
			CurrentFrameBuffer = frameBuffers[1];
			ScreenManager.FrameProfiler.Mark("rendTransp-fbbound");
			GlDisableCullFace();
			GlDepthMask(flag: false);
			GlEnableDepthTest();
			ScreenManager.FrameProfiler.Mark("rendTransp-dbset");
			DrawBuffersEnum[] array = new DrawBuffersEnum[3];
			RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
			DrawBuffersEnum[] array2 = (DrawBuffersEnum[])(object)array;
			GL.DrawBuffers(3, array2);
			GL.Enable((EnableCap)3042);
			GL.BlendEquation(0, (BlendEquationMode)32774);
			GL.BlendFunc(0, (BlendingFactorSrc)1, (BlendingFactorDest)1);
			GL.BlendEquation(1, (BlendEquationMode)32774);
			GL.BlendFunc(1, (BlendingFactorSrc)0, (BlendingFactorDest)769);
			GL.BlendEquation(2, (BlendEquationMode)32774);
			GL.BlendFunc(2, (BlendingFactorSrc)770, (BlendingFactorDest)771);
			break;
		}
		case EnumFrameBuffer.Default:
			CurrentFrameBuffer = null;
			GL.Viewport(0, 0, ((NativeWindow)window).ClientSize.X, ((NativeWindow)window).ClientSize.Y);
			GL.DrawBuffer((DrawBufferMode)1029);
			break;
		case EnumFrameBuffer.BlurHorizontalMedRes:
		case EnumFrameBuffer.BlurVerticalMedRes:
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.X / 2f), (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.Y / 2f));
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.SSAOBlurVertical:
		case EnumFrameBuffer.SSAOBlurHorizontal:
		case EnumFrameBuffer.SSAOBlurVerticalHalfRes:
		case EnumFrameBuffer.SSAOBlurHorizontalHalfRes:
		{
			FrameBufferRef frameBufferRef2 = frameBuffers[(int)framebuffer];
			GL.Viewport(0, 0, frameBufferRef2.Width, frameBufferRef2.Height);
			CurrentFrameBuffer = frameBufferRef2;
			break;
		}
		case EnumFrameBuffer.BlurVerticalLowRes:
		case EnumFrameBuffer.BlurHorizontalLowRes:
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.X / 4f), (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.Y / 4f));
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.FindBright:
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.GodRays:
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.X / 2f), (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.Y / 2f));
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.Luma:
			GL.Disable((EnableCap)3042);
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.SSAO:
		{
			FrameBufferRef frameBufferRef = frameBuffers[(int)framebuffer];
			GL.Viewport(0, 0, frameBufferRef.Width, frameBufferRef.Height);
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		}
		case EnumFrameBuffer.ShadowmapFar:
		case EnumFrameBuffer.ShadowmapNear:
			GlDepthMask(flag: true);
			GlEnableDepthTest();
			GlToggleBlend(on: true);
			GlEnableCullFace();
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.LiquidDepth:
			GlDepthMask(flag: true);
			GlEnableDepthTest();
			GlToggleBlend(on: true);
			GlEnableCullFace();
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.Primary:
			if (OffscreenBuffer)
			{
				CurrentFrameBuffer = frameBuffers[0];
				GL.Viewport(0, 0, (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.X), (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.Y));
			}
			else
			{
				CurrentFrameBuffer = null;
				GL.DrawBuffer((DrawBufferMode)1029);
			}
			break;
		case (EnumFrameBuffer)6:
			break;
		}
	}

	public override void UnloadFrameBuffer(EnumFrameBuffer framebuffer)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (framebuffer == EnumFrameBuffer.Transparent)
		{
			GlDepthMask(flag: true);
		}
		GL.Viewport(0, 0, (int)((float)((NativeWindow)window).ClientSize.X * ssaaLevel), (int)((float)((NativeWindow)window).ClientSize.Y * ssaaLevel));
		if (OffscreenBuffer)
		{
			CurrentFrameBuffer = frameBuffers[0];
			return;
		}
		CurrentFrameBuffer = null;
		GL.DrawBuffer((DrawBufferMode)1029);
	}

	public override void MergeTransparentRenderPass()
	{
		if (OffscreenBuffer)
		{
			CurrentFrameBuffer = frameBuffers[0];
		}
		else
		{
			CurrentFrameBuffer = null;
			GL.DrawBuffer((DrawBufferMode)1029);
		}
		GL.Disable((EnableCap)2929);
		GL.Enable((EnableCap)3042);
		GL.BlendFunc((BlendingFactor)770, (BlendingFactor)771);
		ShaderProgramTransparentcompose transparentcompose = ShaderPrograms.Transparentcompose;
		transparentcompose.Use();
		transparentcompose.Revealage2D = frameBuffers[1].ColorTextureIds[1];
		transparentcompose.Accumulation2D = frameBuffers[1].ColorTextureIds[0];
		transparentcompose.InGlow2D = frameBuffers[1].ColorTextureIds[2];
		RenderFullscreenTriangle(screenQuad);
		transparentcompose.Stop();
	}

	public override void RenderPostprocessingEffects(float[] projectMatrix)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c9: Unknown result type (might be due to invalid IL or missing references)
		if (!OffscreenBuffer)
		{
			return;
		}
		int x = ((NativeWindow)window).ClientSize.X;
		int y = ((NativeWindow)window).ClientSize.Y;
		if (RenderBloom)
		{
			GlToggleBlend(on: false);
			LoadFrameBuffer(EnumFrameBuffer.FindBright);
			ShaderProgramFindbright findbright = ShaderPrograms.Findbright;
			findbright.Use();
			findbright.ColorTex2D = frameBuffers[0].ColorTextureIds[0];
			findbright.GlowTex2D = frameBuffers[0].ColorTextureIds[1];
			findbright.AmbientBloomLevel = ClientSettings.AmbientBloomLevel / 100f + ShaderUniforms.AmbientBloomLevelAdd[0] + ShaderUniforms.AmbientBloomLevelAdd[1] + ShaderUniforms.AmbientBloomLevelAdd[2] + ShaderUniforms.AmbientBloomLevelAdd[3];
			findbright.ExtraBloom = ShaderUniforms.ExtraBloom;
			RenderFullscreenTriangle(screenQuad);
			findbright.Stop();
			ShaderProgramBlur blur = ShaderPrograms.Blur;
			blur.Use();
			blur.FrameSize = new Vec2f((float)x * ssaaLevel, (float)y * ssaaLevel);
			LoadFrameBuffer(EnumFrameBuffer.BlurHorizontalMedRes);
			blur.IsVertical = 0;
			blur.InputTexture2D = frameBuffers[4].ColorTextureIds[0];
			RenderFullscreenTriangle(screenQuad);
			LoadFrameBuffer(EnumFrameBuffer.BlurVerticalMedRes);
			blur.IsVertical = 1;
			blur.InputTexture2D = frameBuffers[2].ColorTextureIds[0];
			RenderFullscreenTriangle(screenQuad);
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.X / 4f), (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.Y / 4f));
			LoadFrameBuffer(EnumFrameBuffer.BlurHorizontalLowRes);
			blur.IsVertical = 0;
			blur.InputTexture2D = frameBuffers[3].ColorTextureIds[0];
			RenderFullscreenTriangle(screenQuad);
			LoadFrameBuffer(EnumFrameBuffer.BlurVerticalLowRes);
			blur.IsVertical = 1;
			blur.InputTexture2D = frameBuffers[9].ColorTextureIds[0];
			RenderFullscreenTriangle(screenQuad);
			blur.Stop();
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.X), (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.Y));
			GlToggleBlend(on: true);
		}
		if (RenderGodRays)
		{
			LoadFrameBuffer(EnumFrameBuffer.GodRays);
			ShaderProgramGodrays godrays = ShaderPrograms.Godrays;
			godrays.Use();
			godrays.InvFrameSizeIn = new Vec2f(1f / ((float)x * ssaaLevel), 1f / ((float)y * ssaaLevel));
			godrays.SunPosScreenIn = ShaderUniforms.SunPositionScreen;
			godrays.SunPos3dIn = ShaderUniforms.LightPosition3D;
			godrays.PlayerViewVector = ShaderUniforms.PlayerViewVector;
			godrays.Dusk = ShaderUniforms.Dusk;
			godrays.IGlobalTimeIn = (float)EllapsedMs / 1000f;
			godrays.InputTexture2D = frameBuffers[0].ColorTextureIds[0];
			godrays.GlowParts2D = frameBuffers[0].ColorTextureIds[1];
			RenderFullscreenTriangle(screenQuad);
			godrays.Stop();
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.X), (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.Y));
		}
		if (RenderSSAO && projectMatrix != null)
		{
			GlToggleBlend(on: false);
			LoadFrameBuffer(EnumFrameBuffer.SSAO);
			GL.ClearBuffer((ClearBuffer)6144, 0, new float[4] { 1f, 1f, 1f, 1f });
			ShaderProgramSsao ssao = ShaderPrograms.Ssao;
			ssao.Use();
			ssao.GNormal2D = frameBuffers[0].ColorTextureIds[2];
			ssao.GPosition2D = frameBuffers[0].ColorTextureIds[3];
			ssao.TexNoise2D = frameBuffers[13].ColorTextureIds[1];
			float num = ((ssaaLevel == 1f) ? 0.5f : 1f);
			ssao.ScreenSize = new Vec2f(ssaaLevel * (float)x * num, ssaaLevel * (float)y * num);
			ssao.Revealage2D = frameBuffers[1].ColorTextureIds[1];
			ssao.Projection = projectMatrix;
			ssao.SamplesArray(64, ssaoKernel);
			RenderFullscreenTriangle(screenQuad);
			ssao.Stop();
			ShaderProgramBilateralblur bilateralblur = ShaderPrograms.Bilateralblur;
			bilateralblur.Use();
			int num2 = ((ClientSettings.SSAOQuality == 1) ? 1 : 3);
			for (int i = 0; i < num2; i++)
			{
				FrameBufferRef frameBufferRef = frameBuffers[15];
				LoadFrameBuffer(EnumFrameBuffer.SSAOBlurHorizontal);
				bilateralblur.FrameSize = new Vec2f(frameBufferRef.Width, frameBufferRef.Height);
				bilateralblur.IsVertical = 0;
				bilateralblur.InputTexture2D = frameBuffers[(i == 0) ? 13 : 14].ColorTextureIds[0];
				bilateralblur.DepthTexture2D = frameBuffers[0].DepthTextureId;
				RenderFullscreenTriangle(screenQuad);
				LoadFrameBuffer(EnumFrameBuffer.SSAOBlurVertical);
				bilateralblur.IsVertical = 1;
				bilateralblur.FrameSize = new Vec2f(frameBufferRef.Width, frameBufferRef.Height);
				bilateralblur.InputTexture2D = frameBuffers[15].ColorTextureIds[0];
				RenderFullscreenTriangle(screenQuad);
			}
			bilateralblur.Stop();
			GlToggleBlend(on: true);
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.X), (int)(ssaaLevel * (float)((NativeWindow)window).ClientSize.Y));
		}
		if (RenderFXAA)
		{
			LoadFrameBuffer(EnumFrameBuffer.Luma);
			ShaderProgramLuma luma = ShaderPrograms.Luma;
			luma.Use();
			luma.Scene2D = frameBuffers[0].ColorTextureIds[0];
			RenderFullscreenTriangle(screenQuad);
			luma.Stop();
		}
		else
		{
			LoadFrameBuffer(EnumFrameBuffer.Luma);
			ShaderProgramBlit blit = ShaderPrograms.Blit;
			blit.Use();
			blit.Scene2D = frameBuffers[0].ColorTextureIds[0];
			RenderFullscreenTriangle(screenQuad);
			blit.Stop();
		}
		GL.Enable((EnableCap)3042);
		LoadFrameBuffer(EnumFrameBuffer.Primary);
		ScreenManager.Platform.CheckGlError();
	}

	public override void RenderFinalComposition()
	{
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		if (OffscreenBuffer)
		{
			int bloomParts2D = frameBuffers[8].ColorTextureIds[0];
			int godrayParts2D = frameBuffers[7].ColorTextureIds[0];
			int primaryScene2D = frameBuffers[10].ColorTextureIds[0];
			if (RenderBloom)
			{
				_ = frameBuffers[8].ColorTextureIds[0];
			}
			DrawBuffersEnum[] array = (DrawBuffersEnum[])(object)new DrawBuffersEnum[1] { (DrawBuffersEnum)36064 };
			GL.DrawBuffers(1, array);
			GL.Disable((EnableCap)2929);
			GlToggleBlend(on: true);
			ShaderProgramFinal final = ShaderPrograms.Final;
			final.Use();
			final.PrimaryScene2D = primaryScene2D;
			final.BloomParts2D = bloomParts2D;
			final.GlowParts2D = frameBuffers[0].ColorTextureIds[1];
			final.GodrayParts2D = godrayParts2D;
			final.AmbientBloomLevel = ClientSettings.AmbientBloomLevel / 100f + ShaderUniforms.AmbientBloomLevelAdd[0] + ShaderUniforms.AmbientBloomLevelAdd[1] + ShaderUniforms.AmbientBloomLevelAdd[2] + ShaderUniforms.AmbientBloomLevelAdd[3];
			if (RenderSSAO)
			{
				final.SsaoScene2D = frameBuffers[14].ColorTextureIds[0];
			}
			final.InvFrameSizeIn = new Vec2f(1f / ((float)((NativeWindow)window).ClientSize.X * ssaaLevel), 1f / ((float)((NativeWindow)window).ClientSize.Y * ssaaLevel));
			final.GammaLevel = ClientSettings.GammaLevel;
			final.ExtraGamma = ClientSettings.ExtraGammaLevel;
			final.ContrastLevel = ShaderUniforms.ExtraContrastLevel;
			final.BrightnessLevel = ClientSettings.BrightnessLevel + Math.Max(0f, ShaderUniforms.DropShadowIntensity * 2f - 1.66f) / 3f;
			final.SepiaLevel = ShaderUniforms.SepiaLevel + ShaderUniforms.ExtraSepia;
			final.WindWaveCounter = ShaderUniforms.WindWaveCounter;
			final.GlitchEffectStrength = ShaderUniforms.GlitchStrength;
			if (RenderGodRays)
			{
				final.SunPosScreenIn = ShaderUniforms.SunPositionScreen;
				final.SunPos3dIn = ShaderUniforms.SunPosition3D;
				final.PlayerViewVector = ShaderUniforms.PlayerViewVector;
			}
			final.DamageVignetting = ShaderUniforms.DamageVignetting;
			final.DamageVignettingSide = ShaderUniforms.DamageVignettingSide;
			final.FrostVignetting = ShaderUniforms.FrostVignetting;
			RenderFullscreenTriangle(screenQuad);
			final.Stop();
			if (RenderSSAO)
			{
				DrawBuffersEnum[] array2 = new DrawBuffersEnum[4];
				RuntimeHelpers.InitializeArray(array2, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
				array = (DrawBuffersEnum[])(object)array2;
				GL.DrawBuffers(4, array);
			}
			else
			{
				array = (DrawBuffersEnum[])(object)new DrawBuffersEnum[2]
				{
					(DrawBuffersEnum)36064,
					(DrawBuffersEnum)36065
				};
				GL.DrawBuffers(2, array);
			}
		}
	}

	private void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, nint message, nint userParam)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Invalid comparison between Unknown and I4
		if ((int)type != 33361)
		{
			string text = Marshal.PtrToStringAnsi(message, length);
			Logger.Notification("{0} {1} | {2}", severity, type, text);
			if ((int)type == 33356)
			{
				throw new Exception(text);
			}
		}
	}

	public override void BlitPrimaryToDefault()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (OffscreenBuffer)
		{
			int scene2D = frameBuffers[0].ColorTextureIds[0];
			LoadFrameBuffer(EnumFrameBuffer.Default);
			GL.Viewport(0, 0, ((NativeWindow)window).ClientSize.X, ((NativeWindow)window).ClientSize.Y);
			ShaderProgramBlit blit = ShaderPrograms.Blit;
			blit.Use();
			blit.Scene2D = scene2D;
			RenderFullscreenTriangle(screenQuad);
			blit.Stop();
		}
	}

	private void CheckFboStatus(FramebufferTarget target, EnumFrameBuffer fbtype)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		CheckFboStatus(target, fbtype.ToString() ?? "");
	}

	private unsafe void CheckFboStatus(FramebufferTarget target, string fbtype)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected I4, but got Unknown
		FramebufferErrorCode val = Ext.CheckFramebufferStatus(target);
		switch (val - 36053)
		{
		case 0:
			return;
		case 1:
			throw new Exception("FBO " + fbtype + ": One or more attachment points are not framebuffer attachment complete. This could mean there’s no texture attached or the format isn’t renderable. For color textures this means the base format must be RGB or RGBA and for depth textures it must be a DEPTH_COMPONENT format. Other causes of this error are that the width or height is zero or the z-offset is out of range in case of render to volume.");
		case 2:
			throw new Exception("FBO " + fbtype + ": There are no attachments.");
		case 4:
			throw new Exception("FBO " + fbtype + ": Attachments are of different size. All attachments must have the same width and height.");
		case 5:
			throw new Exception("FBO " + fbtype + ": The color attachments have different format. All color attachments must have the same format.");
		case 6:
			throw new Exception("FBO " + fbtype + ": An attachment point referenced by GL.DrawBuffers() doesn’t have an attachment.");
		case 7:
			throw new Exception("FBO " + fbtype + ": The attachment point referenced by GL.ReadBuffers() doesn’t have an attachment.");
		case 8:
			throw new Exception("FBO " + fbtype + ": This particular FBO configuration is not supported by the implementation.");
		}
		throw new Exception("FBO " + fbtype + ": Framebuffer unknown error (" + ((object)(*(FramebufferErrorCode*)(&val))/*cast due to .constrained prefix*/).ToString() + ")");
	}

	public override void CheckGlError(string errmsg = null)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (GlErrorChecking)
		{
			ErrorCode error = GL.GetError();
			if ((int)error != 0)
			{
				throw new Exception(string.Format("{0} - OpenGL threw an error: {1}", (errmsg == null) ? "" : (errmsg + " "), error));
			}
		}
	}

	public override void CheckGlErrorAlways(string errmsg = null)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Invalid comparison between Unknown and I4
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		ErrorCode error = GL.GetError();
		if ((int)error != 0)
		{
			string arg = (ClientSettings.GlDebugMode ? "" : ". Enable Gl Debug Mode in the settings or clientsettings.json to track this error");
			string message = string.Format("{0} - OpenGL threw an error: {1}{2}", (errmsg == null) ? "" : (errmsg + " "), error, arg);
			Logger.Error(message);
		}
		if ((int)error == 1285)
		{
			throw new OutOfMemoryException("Either the graphics card or the OS ran out of memory! Please close other programs and reduce your view distance to prevent the game from crashing.");
		}
	}

	public unsafe override string GlGetError()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		ErrorCode error = GL.GetError();
		if ((int)error != 0)
		{
			return ((object)(*(ErrorCode*)(&error))/*cast due to .constrained prefix*/).ToString();
		}
		return null;
	}

	public override string GetGLShaderVersionString()
	{
		return GL.GetString((StringName)35724);
	}

	public override int GenSampler(bool linear)
	{
		int num = GL.GenSampler();
		GL.SamplerParameter(num, (SamplerParameterName)10240, linear ? 9729 : 9728);
		GL.SamplerParameter(num, (SamplerParameterName)10241, 9986);
		return num;
	}

	public override void GLWireframes(bool toggle)
	{
		GL.PolygonMode((TriangleFace)1032, (PolygonMode)(toggle ? 6913 : 6914));
	}

	public override void GlViewport(int x, int y, int width, int height)
	{
		GL.Viewport(x, y, width, height);
	}

	public override void GlScissor(int x, int y, int width, int height)
	{
		GL.Scissor(x, y, width, height);
	}

	public override void GlScissorFlag(bool enable)
	{
		if (enable)
		{
			GL.Enable((EnableCap)3089);
		}
		else
		{
			GL.Disable((EnableCap)3089);
		}
	}

	public override void GlEnableDepthTest()
	{
		GL.Enable((EnableCap)2929);
	}

	public override void GlDisableDepthTest()
	{
		GL.Disable((EnableCap)2929);
	}

	public override void BindTexture2d(int texture)
	{
		GL.ActiveTexture((TextureUnit)33984);
		GL.BindTexture((TextureTarget)3553, texture);
	}

	public override void BindTextureCubeMap(int texture)
	{
		GL.BindTexture((TextureTarget)34067, texture);
	}

	public override void UnBindTextureCubeMap()
	{
		GL.BindTexture((TextureTarget)34067, 0);
	}

	public override void GlToggleBlend(bool on, EnumBlendMode blendMode = EnumBlendMode.Standard)
	{
		if (on)
		{
			GL.Enable((EnableCap)3042);
			switch (blendMode)
			{
			case EnumBlendMode.Brighten:
				GL.BlendFunc((BlendingFactor)774, (BlendingFactor)1);
				return;
			case EnumBlendMode.Multiply:
				GL.BlendFuncSeparate((BlendingFactorSrc)0, (BlendingFactorDest)771, (BlendingFactorSrc)1, (BlendingFactorDest)771);
				return;
			case EnumBlendMode.PremultipliedAlpha:
				GL.BlendFunc((BlendingFactor)1, (BlendingFactor)771);
				return;
			case EnumBlendMode.Glow:
				GL.BlendFuncSeparate((BlendingFactorSrc)770, (BlendingFactorDest)1, (BlendingFactorSrc)1, (BlendingFactorDest)0);
				return;
			case EnumBlendMode.Overlay:
				GL.BlendFuncSeparate((BlendingFactorSrc)770, (BlendingFactorDest)771, (BlendingFactorSrc)1, (BlendingFactorDest)1);
				return;
			}
			GL.BlendFunc((BlendingFactor)770, (BlendingFactor)771);
			if (RenderSSAO)
			{
				GL.BlendEquation(2, (BlendEquationMode)32774);
				GL.BlendFunc(2, (BlendingFactorSrc)1, (BlendingFactorDest)0);
				GL.BlendEquation(3, (BlendEquationMode)32774);
				GL.BlendFunc(3, (BlendingFactorSrc)1, (BlendingFactorDest)0);
			}
		}
		else
		{
			GL.Disable((EnableCap)3042);
		}
	}

	public override void GlDisableCullFace()
	{
		GL.Disable((EnableCap)2884);
	}

	public override void GlEnableCullFace()
	{
		GL.Enable((EnableCap)2884);
	}

	public override void GlClearColorRgbaf(float r, float g, float b, float a)
	{
		GL.ClearColor(r, g, b, a);
	}

	public override void GLLineWidth(float width)
	{
		if (RuntimeEnv.OS != OS.Mac)
		{
			GL.LineWidth(width);
		}
	}

	public override void SmoothLines(bool on)
	{
		if (RuntimeEnv.OS != OS.Mac)
		{
			if (on)
			{
				GL.Enable((EnableCap)2848);
				GL.Hint((HintTarget)3154, (HintMode)4354);
			}
			else
			{
				GL.Disable((EnableCap)2848);
				GL.Hint((HintTarget)3154, (HintMode)4352);
			}
		}
	}

	public override void GlDepthMask(bool flag)
	{
		GL.DepthMask(flag);
	}

	public override void GlDepthFunc(EnumDepthFunction depthFunc)
	{
		GL.DepthFunc((DepthFunction)depthFunc);
	}

	public override void GlCullFaceBack()
	{
		GL.CullFace((TriangleFace)1029);
	}

	public override void GlGenerateTex2DMipmaps()
	{
		GL.GenerateMipmap((GenerateMipmapTarget)3553);
	}

	public override int LoadCairoTexture(ImageSurface surface, bool linearMag)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Texture uploads must happen in the main thread. We only have one OpenGL context.");
		}
		int num = GL.GenTexture();
		GL.BindTexture((TextureTarget)3553, num);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, linearMag ? 9729 : 9728);
		GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)32856, surface.Width, surface.Height, 0, (PixelFormat)32993, (PixelType)5121, surface.DataPtr);
		return num;
	}

	public override void LoadOrUpdateCairoTexture(ImageSurface surface, bool linearMag, ref LoadedTexture intoTexture)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Texture uploads must happen in the main thread. We only have one OpenGL context.");
		}
		if (intoTexture.TextureId == 0 || intoTexture.Width != surface.Width || intoTexture.Height != surface.Height)
		{
			if (intoTexture.TextureId != 0)
			{
				GL.DeleteTexture(intoTexture.TextureId);
			}
			intoTexture.TextureId = GL.GenTexture();
			intoTexture.Width = surface.Width;
			intoTexture.Height = surface.Height;
			GL.BindTexture((TextureTarget)3553, intoTexture.TextureId);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, linearMag ? 9729 : 9728);
			GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)32856, surface.Width, surface.Height, 0, (PixelFormat)32993, (PixelType)5121, surface.DataPtr);
		}
		else
		{
			GL.BindTexture((TextureTarget)3553, intoTexture.TextureId);
			GL.TexSubImage2D((TextureTarget)3553, 0, 0, 0, surface.Width, surface.Height, (PixelFormat)32993, (PixelType)5121, surface.DataPtr);
		}
		CheckGlError("LoadOrUpdateCairoTexture");
	}

	public override int LoadTexture(SKBitmap bmp, bool linearMag = false, int clampMode = 0, bool generateMipmaps = false)
	{
		return LoadTexture((IBitmap)new BitmapExternal(bmp), linearMag, clampMode, generateMipmaps);
	}

	public override void LoadIntoTexture(IBitmap srcBmp, int targetTextureId, int destX, int destY, bool generateMipmaps = false)
	{
		GL.BindTexture((TextureTarget)3553, targetTextureId);
		if (srcBmp is BitmapExternal bitmapExternal)
		{
			GL.TexSubImage2D((TextureTarget)3553, 0, destX, destY, srcBmp.Width, srcBmp.Height, (PixelFormat)32993, (PixelType)5121, (IntPtr)bitmapExternal.PixelsPtrAndLock);
		}
		else
		{
			GL.TexSubImage2D<int>((TextureTarget)3553, 0, destX, destY, srcBmp.Width, srcBmp.Height, (PixelFormat)32993, (PixelType)5121, srcBmp.Pixels);
		}
		if (ENABLE_MIPMAPS && generateMipmaps)
		{
			BuildMipMaps(targetTextureId);
		}
	}

	public override int LoadTexture(IBitmap bmp, bool linearMag = false, int clampMode = 0, bool generateMipmaps = false)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Texture uploads must happen in the main thread. We only have one OpenGL context.");
		}
		int num = GL.GenTexture();
		GL.BindTexture((TextureTarget)3553, num);
		if (ENABLE_ANISOTROPICFILTERING)
		{
			float num2 = GL.GetFloat((GetPName)34047);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)34046, num2);
		}
		switch (clampMode)
		{
		case 1:
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, 33071);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, 33071);
			break;
		case 2:
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, 10497);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, 10497);
			break;
		}
		if (bmp is BitmapExternal bitmapExternal)
		{
			GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)6408, bmp.Width, bmp.Height, 0, (PixelFormat)32993, (PixelType)5121, (IntPtr)bitmapExternal.PixelsPtrAndLock);
		}
		else
		{
			GL.TexImage2D<int>((TextureTarget)3553, 0, (PixelInternalFormat)6408, bmp.Width, bmp.Height, 0, (PixelFormat)32993, (PixelType)5121, bmp.Pixels);
		}
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, linearMag ? 9729 : 9728);
		if (ENABLE_MIPMAPS && generateMipmaps)
		{
			BuildMipMaps(num);
		}
		return num;
	}

	public override void LoadOrUpdateTextureFromBgra_DeferMipMap(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		PixelFormat format = (PixelFormat)32993;
		LoadOrUpdateTextureFromPixels(rgbaPixels, linearMag, clampMode, ref intoTexture, format, makeMipMap: false);
	}

	public override void LoadOrUpdateTextureFromBgra(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		PixelFormat format = (PixelFormat)32993;
		LoadOrUpdateTextureFromPixels(rgbaPixels, linearMag, clampMode, ref intoTexture, format, makeMipMap: true);
	}

	public override void LoadOrUpdateTextureFromRgba(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		PixelFormat format = (PixelFormat)6408;
		LoadOrUpdateTextureFromPixels(rgbaPixels, linearMag, clampMode, ref intoTexture, format, makeMipMap: true);
	}

	private void LoadOrUpdateTextureFromPixels(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture, PixelFormat format, bool makeMipMap)
	{
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Texture uploads must happen in the main thread. We only have one OpenGL context.");
		}
		if (intoTexture.TextureId == 0 || intoTexture.Width * intoTexture.Height != rgbaPixels.Length)
		{
			if (intoTexture.TextureId != 0)
			{
				GL.DeleteTexture(intoTexture.TextureId);
			}
			intoTexture.TextureId = GL.GenTexture();
			GL.BindTexture((TextureTarget)3553, intoTexture.TextureId);
			if (ENABLE_ANISOTROPICFILTERING)
			{
				float num = GL.GetFloat((GetPName)34047);
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)34046, num);
			}
			if (clampMode == 1)
			{
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, 33071);
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, 33071);
			}
			GL.TexImage2D<int>((TextureTarget)3553, 0, (PixelInternalFormat)6408, intoTexture.Width, intoTexture.Height, 0, format, (PixelType)5121, rgbaPixels);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, linearMag ? 9729 : 9728);
			if (makeMipMap)
			{
				BuildMipMaps(intoTexture.TextureId);
			}
		}
		else
		{
			GL.BindTexture((TextureTarget)3553, intoTexture.TextureId);
			GL.TexSubImage2D<int>((TextureTarget)3553, 0, 0, 0, intoTexture.Width, intoTexture.Height, format, (PixelType)5121, rgbaPixels);
		}
	}

	public override void BuildMipMaps(int textureId)
	{
		if (ENABLE_MIPMAPS)
		{
			GL.BindTexture((TextureTarget)3553, textureId);
			int num = default(int);
			GL.GetTexParameter((TextureTarget)3553, (GetTextureParameter)33085, ref num);
			if (num > 0)
			{
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9986);
				GL.GenerateMipmap((GenerateMipmapTarget)3553);
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)34049, 0f);
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)33085, ClientSettings.MipMapLevel);
			}
		}
	}

	public override int Load3DTextureCube(BitmapRef[] bmps)
	{
		GL.ActiveTexture((TextureUnit)33984);
		int num = GL.GenTexture();
		GL.BindTexture((TextureTarget)34067, num);
		for (int i = 0; i < 6; i++)
		{
			Load3DTextureSide((BitmapExternal)bmps[i], (TextureTarget)(34069 + i));
		}
		GL.TexParameter((TextureTarget)34067, (TextureParameterName)10241, 9729);
		GL.TexParameter((TextureTarget)34067, (TextureParameterName)10240, 9729);
		GL.TexParameter((TextureTarget)34067, (TextureParameterName)32882, Convert.ToInt32((object)(TextureWrapMode)33071));
		GL.TexParameter((TextureTarget)34067, (TextureParameterName)10242, Convert.ToInt32((object)(TextureWrapMode)33071));
		GL.TexParameter((TextureTarget)34067, (TextureParameterName)10243, Convert.ToInt32((object)(TextureWrapMode)33071));
		return num;
	}

	private void Load3DTextureSide(BitmapExternal bmp, TextureTarget target)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		GL.TexImage2D(target, 0, (PixelInternalFormat)6408, bmp.Width, bmp.Height, 0, (PixelFormat)32993, (PixelType)5121, (IntPtr)bmp.PixelsPtrAndLock);
	}

	public override void GLDeleteTexture(int id)
	{
		GL.DeleteTexture(id);
	}

	public override int GlGetMaxTextureSize()
	{
		int result = 1024;
		try
		{
			GL.GetInteger((GetPName)3379, ref result);
		}
		catch
		{
		}
		return result;
	}

	public override UBORef CreateUBO(int shaderProgramId, int bindingPoint, string blockName, int size)
	{
		int num = GL.GenBuffer();
		GL.BindBuffer((BufferTarget)35345, num);
		GL.BufferData((BufferTarget)35345, size, (IntPtr)IntPtr.Zero, (BufferUsageHint)35048);
		ScreenManager.Platform.CheckGlError();
		int uniformBlockIndex = GL.GetUniformBlockIndex(shaderProgramId, blockName);
		ScreenManager.Platform.CheckGlError();
		GL.UniformBlockBinding(shaderProgramId, uniformBlockIndex, bindingPoint);
		GL.BindBufferBase((BufferRangeTarget)35345, bindingPoint, num);
		ScreenManager.Platform.CheckGlError();
		UBO uBO = new UBO();
		uBO.Handle = num;
		uBO.Size = size;
		uBO.Unbind();
		ScreenManager.Platform.CheckGlError();
		return uBO;
	}

	public override void UpdateMesh(MeshRef modelRef, MeshData data)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		VAO vAO = (VAO)modelRef;
		BufferAccessMask val = (BufferAccessMask)34;
		if (vAO.Persistent)
		{
			val = (BufferAccessMask)(val | 0x50);
		}
		bool persistent = vAO.Persistent;
		if (data.xyz != null)
		{
			updateVAO(data.xyz, data.XyzOffset, data.XyzCount, vAO.xyzVboId, vAO.xyzPtr, persistent);
		}
		if (data.Normals != null && data.VerticesCount > 0)
		{
			updateVAO(data.Normals, data.NormalsOffset, data.VerticesCount, vAO.normalsVboId, vAO.normalsPtr, persistent);
		}
		if (data.Uv != null && data.UvCount > 0)
		{
			updateVAO(data.Uv, data.UvOffset, data.UvCount, vAO.uvVboId, vAO.uvPtr, persistent);
		}
		if (data.Rgba != null && data.RgbaCount > 0)
		{
			updateVAO(data.Rgba, data.RgbaOffset, data.RgbaCount, vAO.rgbaVboId, vAO.rgbaPtr, persistent);
		}
		if (data.Flags != null && data.FlagsCount > 0)
		{
			updateVAO(data.Flags, data.FlagsOffset, data.FlagsCount, vAO.flagsVboId, vAO.flagsPtr, persistent);
		}
		if (data.CustomFloats != null && data.CustomFloats.Count > 0)
		{
			updateVAO(data.CustomFloats.Values, data.CustomFloats.BaseOffset, data.CustomFloats.Count, vAO.customDataFloatVboId, vAO.customDataFloatPtr, persistent);
		}
		if (data.CustomShorts != null && data.CustomShorts.Count > 0)
		{
			updateVAO(data.CustomShorts.Values, data.CustomShorts.BaseOffset, data.CustomShorts.Count, vAO.customDataShortVboId, vAO.customDataShortPtr, persistent);
		}
		if (data.CustomInts != null && data.CustomInts.Count > 0)
		{
			updateVAO(data.CustomInts.Values, data.CustomInts.BaseOffset, data.CustomInts.Count, vAO.customDataIntVboId, vAO.customDataIntPtr, persistent);
		}
		if (data.CustomBytes != null && data.CustomBytes.Count > 0)
		{
			updateVAO(data.CustomBytes.Values, data.CustomBytes.BaseOffset, data.CustomBytes.Count, vAO.customDataByteVboId, vAO.customDataBytePtr, persistent);
		}
		GL.BindBuffer((BufferTarget)34962, 0);
		updateIndices(data.Indices, data.IndicesOffset, data.IndicesCount, vAO, persistent);
		if (GlErrorChecking && GlDebugMode)
		{
			CheckGlError($"Error when trying to update vao indices, modeldata xyz/rgba/uv/indices sizes: {data.XyzCount}/{data.RgbaCount}/{data.UvCount}/{data.IndicesCount}");
		}
	}

	private unsafe void updateVAO(float[] data, int offset, int count, int vboId, nint vboPtr, bool pers)
	{
		GL.BindBuffer((BufferTarget)34962, vboId);
		if (pers)
		{
			float* ptr = (float*)vboPtr;
			ptr += offset / 4;
			for (int i = 0; i < count; i++)
			{
				*(ptr++) = data[i];
			}
		}
		else
		{
			GL.BufferSubData<float>((BufferTarget)34962, (IntPtr)offset, 4 * count, data);
		}
	}

	private unsafe void updateVAO(int[] data, int offset, int count, int vboId, nint vboPtr, bool pers)
	{
		GL.BindBuffer((BufferTarget)34962, vboId);
		if (pers)
		{
			int* ptr = (int*)vboPtr;
			ptr += offset / 4;
			for (int i = 0; i < count; i++)
			{
				*(ptr++) = data[i];
			}
		}
		else
		{
			GL.BufferSubData<int>((BufferTarget)34962, (IntPtr)offset, 4 * count, data);
		}
	}

	private unsafe void updateVAO(short[] data, int offset, int count, int vboId, nint vboPtr, bool pers)
	{
		GL.BindBuffer((BufferTarget)34962, vboId);
		if (pers)
		{
			short* ptr = (short*)vboPtr;
			ptr += offset / 2;
			for (int i = 0; i < count; i++)
			{
				*(ptr++) = data[i];
			}
		}
		else
		{
			GL.BufferSubData<short>((BufferTarget)34962, (IntPtr)offset, 2 * count, data);
		}
	}

	private unsafe void updateVAO(ushort[] data, int offset, int count, int vboId, nint vboPtr, bool pers)
	{
		GL.BindBuffer((BufferTarget)34962, vboId);
		if (pers)
		{
			ushort* ptr = (ushort*)vboPtr;
			ptr += offset / 2;
			for (int i = 0; i < count; i++)
			{
				*(ptr++) = data[i];
			}
		}
		else
		{
			GL.BufferSubData<ushort>((BufferTarget)34962, (IntPtr)offset, 2 * count, data);
		}
	}

	private unsafe void updateVAO(byte[] data, int offset, int count, int vboId, nint vboPtr, bool pers)
	{
		GL.BindBuffer((BufferTarget)34962, vboId);
		if (pers)
		{
			byte* ptr = (byte*)vboPtr;
			ptr += offset / 1;
			for (int i = 0; i < count; i++)
			{
				*(ptr++) = data[i];
			}
		}
		else
		{
			GL.BufferSubData<byte>((BufferTarget)34962, (IntPtr)offset, count, data);
		}
	}

	private unsafe void updateIndices(int[] Indices, int IndicesOffset, int IndicesCount, VAO vao, bool pers)
	{
		if (Indices == null)
		{
			return;
		}
		GL.BindBuffer((BufferTarget)34963, vao.vboIdIndex);
		if (pers)
		{
			int* indicesPtr = (int*)vao.indicesPtr;
			indicesPtr += IndicesOffset / 4;
			for (int i = 0; i < IndicesCount; i++)
			{
				*(indicesPtr++) = Indices[i];
			}
		}
		else
		{
			GL.BufferSubData<int>((BufferTarget)34963, (IntPtr)IndicesOffset, 4 * IndicesCount, Indices);
		}
		GL.BindBuffer((BufferTarget)34963, 0);
		vao.IndicesCount = IndicesCount;
	}

	public override MeshRef AllocateEmptyMesh(int xyzSize, int normalsSize, int uvSize, int rgbaSize, int flagsSize, int indicesSize, CustomMeshDataPartFloat customFloats, CustomMeshDataPartShort customShorts, CustomMeshDataPartByte customBytes, CustomMeshDataPartInt customInts, EnumDrawMode drawMode = EnumDrawMode.Triangles, bool staticDraw = true)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		VAO vAO = new VAO();
		int num = GL.GenVertexArray();
		int vaoSlotNumber = 0;
		GL.BindVertexArray(num);
		int xyzVboId = 0;
		int normalsVboId = 0;
		int uvVboId = 0;
		int rgbaVboId = 0;
		int flagsVboId = 0;
		BufferUsageHint usageHint = (BufferUsageHint)(staticDraw ? 35044 : 35048);
		bool flag = supportsPersistentMapping && !staticDraw;
		flag = false;
		BufferStorageFlags val = (BufferStorageFlags)450;
		MapBufferAccessMask val2 = (MapBufferAccessMask)194;
		if (xyzSize > 0)
		{
			xyzVboId = GenArrayBuffer(xyzSize, ref vAO.xyzPtr, flag, val, val2, usageHint);
			GL.VertexAttribPointer(vaoSlotNumber++, 3, (VertexAttribPointerType)5126, false, 0, 0);
		}
		CheckGlError("Failed loading model");
		if (normalsSize > 0)
		{
			normalsVboId = GenArrayBuffer(normalsSize, ref vAO.normalsPtr, flag, val, val2, usageHint);
			GL.VertexAttribPointer(vaoSlotNumber++, 4, (VertexAttribPointerType)36255, true, 0, 0);
		}
		if (uvSize > 0)
		{
			uvVboId = GenArrayBuffer(uvSize, ref vAO.uvPtr, flag, val, val2, usageHint);
			GL.VertexAttribPointer(vaoSlotNumber++, 2, (VertexAttribPointerType)5126, false, 0, 0);
		}
		if (rgbaSize > 0)
		{
			rgbaVboId = GenArrayBuffer(rgbaSize, ref vAO.rgbaPtr, flag, val, val2, usageHint);
			GL.VertexAttribPointer(vaoSlotNumber++, 4, (VertexAttribPointerType)5121, true, 0, 0);
		}
		if (flagsSize > 0)
		{
			flagsVboId = GenArrayBuffer(flagsSize, ref vAO.flagsPtr, flag, val, val2, usageHint);
			GL.VertexAttribIPointer(vaoSlotNumber++, 1, (VertexAttribIntegerType)5125, 0, (IntPtr)IntPtr.Zero);
		}
		vaoSlotNumber = AddCustoms(vAO, vaoSlotNumber, customFloats, customShorts, customInts, customBytes, flag, val, val2, 0);
		GL.BindBuffer((BufferTarget)34962, 0);
		int num2 = GL.GenBuffer();
		GL.BindBuffer((BufferTarget)34963, num2);
		if (flag)
		{
			GL.BufferStorage((BufferTarget)34963, indicesSize, (IntPtr)IntPtr.Zero, val);
			vAO.indicesPtr = GL.MapBufferRange((BufferTarget)34963, (IntPtr)IntPtr.Zero, indicesSize, val2);
		}
		else
		{
			GL.BufferData((BufferTarget)34963, indicesSize, (IntPtr)IntPtr.Zero, (BufferUsageHint)35044);
		}
		GL.BindBuffer((BufferTarget)34963, 0);
		GL.BindVertexArray(0);
		CheckGlError("Failed loading model");
		vAO.Persistent = flag;
		vAO.VaoId = num;
		vAO.IndicesCount = indicesSize;
		vAO.vaoSlotNumber = vaoSlotNumber;
		vAO.vboIdIndex = num2;
		vAO.normalsVboId = normalsVboId;
		vAO.xyzVboId = xyzVboId;
		vAO.uvVboId = uvVboId;
		vAO.rgbaVboId = rgbaVboId;
		vAO.flagsVboId = flagsVboId;
		vAO.drawMode = DrawModeToPrimiteType(drawMode);
		return vAO;
	}

	private int AddCustoms(VAO vao, int vaoSlotNumber, CustomMeshDataPartFloat customFloats, CustomMeshDataPartShort customShorts, CustomMeshDataPartInt customInts, CustomMeshDataPartByte customBytes, bool doPStorage, BufferStorageFlags flags, MapBufferAccessMask mapflags, int pruneCustomInts)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		int customDataFloatVboId = 0;
		int customDataByteVboId = 0;
		int customDataIntVboId = 0;
		int customDataShortVboId = 0;
		if (customFloats != null)
		{
			BufferUsageHint usageHint = (BufferUsageHint)(customFloats.StaticDraw ? 35044 : 35048);
			customDataFloatVboId = GenArrayBuffer(4 * customFloats.AllocationSize, ref vao.customDataFloatPtr, doPStorage, flags, mapflags, usageHint);
			int[] interleaveSizes = customFloats.InterleaveSizes;
			for (int i = 0; i < interleaveSizes.Length; i++)
			{
				GL.VertexAttribPointer(vaoSlotNumber, interleaveSizes[i], (VertexAttribPointerType)5126, false, customFloats.InterleaveStride, customFloats.InterleaveOffsets[i]);
				if (customFloats.Instanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
		}
		if (customShorts != null)
		{
			BufferUsageHint usageHint2 = (BufferUsageHint)(customShorts.StaticDraw ? 35044 : 35048);
			customDataShortVboId = GenArrayBuffer(2 * customShorts.AllocationSize, ref vao.customDataShortPtr, doPStorage, flags, mapflags, usageHint2);
			int[] interleaveSizes2 = customShorts.InterleaveSizes;
			for (int j = 0; j < interleaveSizes2.Length; j++)
			{
				if (customShorts.Conversion == DataConversion.Integer)
				{
					GL.VertexAttribIPointer(vaoSlotNumber, interleaveSizes2[j], (VertexAttribIntegerType)5122, customShorts.InterleaveStride, (IntPtr)customShorts.InterleaveOffsets[j]);
				}
				else
				{
					GL.VertexAttribPointer(vaoSlotNumber, interleaveSizes2[j], (VertexAttribPointerType)5123, customShorts.Conversion == DataConversion.NormalizedFloat, customShorts.InterleaveStride, customShorts.InterleaveOffsets[j]);
				}
				if (customShorts.Instanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
		}
		if (customInts != null && (pruneCustomInts == 0 || customInts.InterleaveStride > 4))
		{
			int num = 1 + pruneCustomInts;
			BufferUsageHint usageHint3 = (BufferUsageHint)(customInts.StaticDraw ? 35044 : 35048);
			customDataIntVboId = GenArrayBuffer(4 * customInts.AllocationSize / num, ref vao.customDataIntPtr, doPStorage, flags, mapflags, usageHint3);
			int[] interleaveSizes3 = customInts.InterleaveSizes;
			for (int k = 0; k < interleaveSizes3.Length; k++)
			{
				if (k - pruneCustomInts != -1)
				{
					if (customInts.Conversion == DataConversion.Integer)
					{
						GL.VertexAttribIPointer(vaoSlotNumber, interleaveSizes3[k], (VertexAttribIntegerType)5125, customInts.InterleaveStride / num, (IntPtr)customInts.InterleaveOffsets[k - pruneCustomInts]);
					}
					else
					{
						GL.VertexAttribPointer(vaoSlotNumber, interleaveSizes3[k], (VertexAttribPointerType)5125, customInts.Conversion == DataConversion.NormalizedFloat, customInts.InterleaveStride / num, customInts.InterleaveOffsets[k - pruneCustomInts]);
					}
					if (customInts.Instanced)
					{
						GL.VertexAttribDivisor(vaoSlotNumber, 1);
					}
					vaoSlotNumber++;
				}
			}
		}
		if (customBytes != null)
		{
			BufferUsageHint usageHint4 = (BufferUsageHint)(customBytes.StaticDraw ? 35044 : 35048);
			customDataByteVboId = GenArrayBuffer(customBytes.AllocationSize, ref vao.customDataBytePtr, doPStorage, flags, mapflags, usageHint4);
			int[] interleaveSizes4 = customBytes.InterleaveSizes;
			for (int l = 0; l < interleaveSizes4.Length; l++)
			{
				if (customBytes.Conversion == DataConversion.Integer)
				{
					GL.VertexAttribIPointer(vaoSlotNumber, interleaveSizes4[l], (VertexAttribIntegerType)5121, customBytes.InterleaveStride, (IntPtr)customBytes.InterleaveOffsets[l]);
				}
				else
				{
					GL.VertexAttribPointer(vaoSlotNumber, interleaveSizes4[l], (VertexAttribPointerType)5121, customBytes.Conversion == DataConversion.NormalizedFloat, customBytes.InterleaveStride, customBytes.InterleaveOffsets[l]);
				}
				if (customBytes.Instanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
		}
		vao.customDataFloatVboId = customDataFloatVboId;
		vao.customDataByteVboId = customDataByteVboId;
		vao.customDataIntVboId = customDataIntVboId;
		vao.customDataShortVboId = customDataShortVboId;
		return vaoSlotNumber;
	}

	private int GenArrayBuffer(int dataSize, ref nint ptr, bool doPStorage, BufferStorageFlags flags, MapBufferAccessMask mapflags, BufferUsageHint usageHint)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		int num = GL.GenBuffer();
		GL.BindBuffer((BufferTarget)34962, num);
		if (doPStorage)
		{
			GL.BufferStorage((BufferTarget)34962, dataSize, (IntPtr)IntPtr.Zero, flags);
			ptr = GL.MapBufferRange((BufferTarget)34962, (IntPtr)IntPtr.Zero, dataSize, mapflags);
		}
		else
		{
			GL.BufferData((BufferTarget)34962, dataSize, (IntPtr)IntPtr.Zero, usageHint);
		}
		return num;
	}

	public override MeshRef UploadMesh(MeshData data)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_043f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0471: Unknown result type (might be due to invalid IL or missing references)
		//IL_06be: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0529: Unknown result type (might be due to invalid IL or missing references)
		int num = GL.GenVertexArray();
		int num2 = 0;
		GL.BindVertexArray(num);
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		int num9 = 0;
		int num10 = 0;
		int num11 = 0;
		if (data.xyz != null)
		{
			BufferUsageHint val = (BufferUsageHint)(data.XyzStatic ? 35044 : 35048);
			num3 = GL.GenBuffer();
			GL.BindBuffer((BufferTarget)34962, num3);
			GL.BufferData<float>((BufferTarget)34962, 4 * data.XyzCount, data.xyz, val);
			GL.VertexAttribPointer(num2, 3, (VertexAttribPointerType)5126, false, 0, 0);
			if (data.XyzInstanced)
			{
				GL.VertexAttribDivisor(num2, 1);
			}
			num2++;
		}
		if (data.Normals != null)
		{
			BufferUsageHint val = (BufferUsageHint)(data.XyzStatic ? 35044 : 35048);
			num4 = GL.GenBuffer();
			GL.BindBuffer((BufferTarget)34962, num4);
			GL.BufferData<int>((BufferTarget)34962, 4 * data.VerticesCount, data.Normals, val);
			GL.VertexAttribPointer(num2, 4, (VertexAttribPointerType)36255, true, 0, 0);
			if (data.XyzInstanced)
			{
				GL.VertexAttribDivisor(num2, 1);
			}
			num2++;
		}
		if (data.Uv != null)
		{
			BufferUsageHint val = (BufferUsageHint)(data.UvStatic ? 35044 : 35048);
			num5 = GL.GenBuffer();
			GL.BindBuffer((BufferTarget)34962, num5);
			GL.BufferData<float>((BufferTarget)34962, 4 * data.UvCount, data.Uv, val);
			GL.VertexAttribPointer(num2, 2, (VertexAttribPointerType)5126, false, 0, 0);
			if (data.UvInstanced)
			{
				GL.VertexAttribDivisor(num2, 1);
			}
			num2++;
		}
		if (data.Rgba != null)
		{
			BufferUsageHint val = (BufferUsageHint)(data.RgbaStatic ? 35044 : 35048);
			num6 = GL.GenBuffer();
			GL.BindBuffer((BufferTarget)34962, num6);
			GL.BufferData<byte>((BufferTarget)34962, data.RgbaCount, data.Rgba, val);
			GL.VertexAttribPointer(num2, 4, (VertexAttribPointerType)5121, true, 0, 0);
			if (data.RgbaInstanced)
			{
				GL.VertexAttribDivisor(num2, 1);
			}
			num2++;
		}
		if (data.Flags != null)
		{
			BufferUsageHint val = (BufferUsageHint)(data.FlagsStatic ? 35044 : 35048);
			num11 = GL.GenBuffer();
			GL.BindBuffer((BufferTarget)34962, num11);
			GL.BufferData<int>((BufferTarget)34962, 4 * data.Flags.Length, data.Flags, val);
			GL.VertexAttribIPointer(num2, 1, (VertexAttribIntegerType)5125, 0, (IntPtr)IntPtr.Zero);
			if (data.FlagsInstanced)
			{
				GL.VertexAttribDivisor(num2, 1);
			}
			num2++;
		}
		if (data.CustomFloats != null)
		{
			BufferUsageHint val = (BufferUsageHint)(data.CustomFloats.StaticDraw ? 35044 : 35048);
			num7 = GL.GenBuffer();
			GL.BindBuffer((BufferTarget)34962, num7);
			GL.BufferData<float>((BufferTarget)34962, 4 * data.CustomFloats.AllocationSize, data.CustomFloats.Values, val);
			for (int i = 0; i < data.CustomFloats.InterleaveSizes.Length; i++)
			{
				GL.VertexAttribPointer(num2, data.CustomFloats.InterleaveSizes[i], (VertexAttribPointerType)5126, false, data.CustomFloats.InterleaveStride, data.CustomFloats.InterleaveOffsets[i]);
				if (data.CustomFloats.Instanced)
				{
					GL.VertexAttribDivisor(num2, 1);
				}
				num2++;
			}
		}
		if (data.CustomShorts != null)
		{
			BufferUsageHint val = (BufferUsageHint)(data.CustomShorts.StaticDraw ? 35044 : 35048);
			num8 = GL.GenBuffer();
			GL.BindBuffer((BufferTarget)34962, num8);
			GL.BufferData<short>((BufferTarget)34962, 2 * data.CustomShorts.AllocationSize, data.CustomShorts.Values, val);
			for (int j = 0; j < data.CustomShorts.InterleaveSizes.Length; j++)
			{
				if (data.CustomShorts.Conversion == DataConversion.Integer)
				{
					GL.VertexAttribIPointer(num2, data.CustomShorts.InterleaveSizes[j], (VertexAttribIntegerType)5122, data.CustomShorts.InterleaveStride, (IntPtr)IntPtr.Zero);
				}
				else
				{
					GL.VertexAttribPointer(num2, data.CustomShorts.InterleaveSizes[j], (VertexAttribPointerType)5122, data.CustomShorts.Conversion == DataConversion.NormalizedFloat, data.CustomShorts.InterleaveStride, data.CustomShorts.InterleaveOffsets[j]);
				}
				if (data.CustomShorts.Instanced)
				{
					GL.VertexAttribDivisor(num2, 1);
				}
				num2++;
			}
		}
		if (data.CustomInts != null)
		{
			BufferUsageHint val = (BufferUsageHint)(data.CustomInts.StaticDraw ? 35044 : 35048);
			num9 = GL.GenBuffer();
			GL.BindBuffer((BufferTarget)34962, num9);
			GL.BufferData<int>((BufferTarget)34962, 4 * data.CustomInts.AllocationSize, data.CustomInts.Values, val);
			for (int k = 0; k < data.CustomInts.InterleaveSizes.Length; k++)
			{
				GL.VertexAttribIPointer(num2, data.CustomInts.InterleaveSizes[k], (VertexAttribIntegerType)5124, data.CustomInts.InterleaveStride, (IntPtr)IntPtr.Zero);
				if (data.CustomInts.Instanced)
				{
					GL.VertexAttribDivisor(num2, 1);
				}
				num2++;
			}
		}
		if (data.CustomBytes != null)
		{
			BufferUsageHint val = (BufferUsageHint)(data.CustomBytes.StaticDraw ? 35044 : 35048);
			num10 = GL.GenBuffer();
			GL.BindBuffer((BufferTarget)34962, num10);
			GL.BufferData<byte>((BufferTarget)34962, data.CustomBytes.AllocationSize, data.CustomBytes.Values, val);
			for (int l = 0; l < data.CustomBytes.InterleaveSizes.Length; l++)
			{
				if (data.CustomBytes.Conversion == DataConversion.Integer)
				{
					GL.VertexAttribIPointer(num2, data.CustomBytes.InterleaveSizes[l], (VertexAttribIntegerType)5121, data.CustomBytes.InterleaveStride, (IntPtr)IntPtr.Zero);
				}
				else
				{
					GL.VertexAttribPointer(num2, data.CustomBytes.InterleaveSizes[l], (VertexAttribPointerType)5121, data.CustomBytes.Conversion == DataConversion.NormalizedFloat, data.CustomBytes.InterleaveStride, data.CustomBytes.InterleaveOffsets[l]);
				}
				if (data.CustomBytes.Instanced)
				{
					GL.VertexAttribDivisor(num2, 1);
				}
				num2++;
			}
		}
		GL.BindBuffer((BufferTarget)34962, 0);
		int num12 = GL.GenBuffer();
		GL.BindBuffer((BufferTarget)34963, num12);
		GL.BufferData<int>((BufferTarget)34963, 4 * data.IndicesCount, data.Indices, (BufferUsageHint)(data.IndicesStatic ? 35044 : 35048));
		GL.BindBuffer((BufferTarget)34963, 0);
		GL.BindVertexArray(0);
		CheckGlError("Something failed during mesh upload");
		return new VAO
		{
			VaoId = num,
			IndicesCount = data.IndicesCount,
			vaoSlotNumber = num2,
			vboIdIndex = num12,
			normalsVboId = num4,
			xyzVboId = num3,
			uvVboId = num5,
			rgbaVboId = num6,
			customDataFloatVboId = num7,
			customDataIntVboId = num9,
			customDataByteVboId = num10,
			customDataShortVboId = num8,
			flagsVboId = num11,
			drawMode = DrawModeToPrimiteType(data.mode)
		};
	}

	public override void DeleteMesh(MeshRef modelref)
	{
		if (modelref != null)
		{
			((VAO)modelref).Dispose();
		}
	}

	public override void UpdateSSBOMesh(MeshRef modelRef, MeshData data)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (data.xyz == null)
		{
			return;
		}
		VAO vAO = (VAO)modelRef;
		BufferAccessMask val = (BufferAccessMask)34;
		if (vAO.Persistent)
		{
			val = (BufferAccessMask)(val | 0x50);
		}
		bool persistent = vAO.Persistent;
		int verticesCount = data.VerticesCount;
		if (facedataBuffer == null || facedataBuffer.Length < verticesCount / 4)
		{
			facedataBuffer = new FaceData[verticesCount / 4];
		}
		float[] xyz = data.xyz;
		float[] uv = data.Uv;
		int[] flags = data.Flags;
		int[] array = ((data.CustomInts != null && data.CustomInts.Count > 0) ? data.CustomInts.Values : null);
		int num = ((data.CustomInts == null || data.CustomInts.Count <= 0) ? 1 : (data.CustomInts.InterleaveStride / 4));
		FaceData[] array2 = facedataBuffer;
		for (int i = 0; i < verticesCount; i += 4)
		{
			float num2 = uv[i * 2];
			float num3 = uv[i * 2 + 1];
			float num4 = uv[i * 2 + 3];
			float num5 = uv[i * 2 + 4];
			float num6 = uv[i * 2 + 5];
			if (num2 < -1.5E-05f || num2 > 1.000015f || num3 < -1.5E-05f || num3 > 1.000015f)
			{
				num2 = 0f;
				num3 = 0f;
			}
			if (num5 < -1.5E-05f || num5 > 1.000015f || num6 < -1.5E-05f || num6 > 1.000015f)
			{
				num5 = 0f;
				num6 = 0f;
			}
			bool rotateUV;
			if (rotateUV = num3 == num4)
			{
				float num7 = uv[i * 2 + 2];
				if (num5 != num7)
				{
					rotateUV = false;
				}
			}
			array2[i / 4] = new FaceData(xyz, i * 3, num2, num3, num5 - num2, num6 - num3, flags, i, (array != null) ? array[i * num] : 0, rotateUV);
		}
		int num8 = data.XyzOffset / 12 * 16;
		GL.BindBuffer((BufferTarget)37074, vAO.xyzVboId);
		GL.BufferSubData<FaceData>((BufferTarget)37074, (IntPtr)num8, 16 * verticesCount, facedataBuffer);
		GL.BindBuffer((BufferTarget)37074, 0);
		if (data.Rgba != null && data.RgbaCount > 0)
		{
			updateVAO(data.Rgba, data.RgbaOffset, data.RgbaCount, vAO.rgbaVboId, vAO.rgbaPtr, persistent);
		}
		if (data.CustomFloats != null && data.CustomFloats.Count > 0)
		{
			updateVAO(data.CustomFloats.Values, data.CustomFloats.BaseOffset, data.CustomFloats.Count, vAO.customDataFloatVboId, vAO.customDataFloatPtr, persistent);
		}
		if (data.CustomShorts != null && data.CustomShorts.Count > 0)
		{
			updateVAO(data.CustomShorts.Values, data.CustomShorts.BaseOffset, data.CustomShorts.Count, vAO.customDataShortVboId, vAO.customDataShortPtr, persistent);
		}
		if (data.CustomInts != null && data.CustomInts.Count > 0 && data.CustomInts.InterleaveStride > 4)
		{
			int[] values = data.CustomInts.Values;
			if (data.CustomInts.InterleaveStride / 4 != 2)
			{
				throw new Exception("We are assuming 2 customInts per vertex if it is not 1");
			}
			int num9 = data.CustomInts.Count / 2;
			if (customIntsPruned == null || customIntsPruned.Length < num9)
			{
				customIntsPruned = new int[num9];
			}
			int[] array3 = customIntsPruned;
			for (int j = 0; j < num9; j++)
			{
				array3[j] = values[j * 2 + 1];
			}
			updateVAO(customIntsPruned, data.CustomInts.BaseOffset / 2, num9, vAO.customDataIntVboId, vAO.customDataIntPtr, persistent);
		}
		if (data.CustomBytes != null && data.CustomBytes.Count > 0)
		{
			updateVAO(data.CustomBytes.Values, data.CustomBytes.BaseOffset, data.CustomBytes.Count, vAO.customDataByteVboId, vAO.customDataBytePtr, persistent);
		}
		GL.BindBuffer((BufferTarget)34962, 0);
		vAO.IndicesCount = data.IndicesCount;
		if (GlErrorChecking && GlDebugMode)
		{
			CheckGlError($"Error when trying to update vao indices, modeldata xyz/rgba/uv/indices sizes: {data.XyzCount}/{data.RgbaCount}/{data.UvCount}/{data.IndicesCount}");
		}
	}

	public override MeshRef AllocateEmptySSBOMesh(int xyzSize, int normalsSize, int uvSize, int rgbaSize, int flagsSize, int indicesSize, CustomMeshDataPartFloat customFloats, CustomMeshDataPartShort customShorts, CustomMeshDataPartByte customBytes, CustomMeshDataPartInt customInts, EnumDrawMode drawMode = EnumDrawMode.Triangles, bool staticDraw = true)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		VAO vAO = new VAO();
		int num = GL.GenVertexArray();
		int vaoSlotNumber = 0;
		GL.BindVertexArray(num);
		int num2 = 0;
		int rgbaVboId = 0;
		BufferUsageHint usageHint = (BufferUsageHint)(staticDraw ? 35044 : 35048);
		BufferStorageFlags flags = (BufferStorageFlags)450;
		MapBufferAccessMask mapflags = (MapBufferAccessMask)194;
		if (xyzSize > 0)
		{
			num2 = GL.GenBuffer();
			GL.BindBuffer((BufferTarget)37074, num2);
			GL.BufferStorage((BufferTarget)37074, xyzSize / 12 * 16, (IntPtr)IntPtr.Zero, (BufferStorageFlags)256);
		}
		CheckGlError("Failed loading model");
		if (rgbaSize > 0)
		{
			rgbaVboId = GenArrayBuffer(rgbaSize, ref vAO.rgbaPtr, doPStorage: false, flags, mapflags, usageHint);
			GL.VertexAttribPointer(vaoSlotNumber++, 4, (VertexAttribPointerType)5121, true, 0, 0);
		}
		vaoSlotNumber = AddCustoms(vAO, vaoSlotNumber, customFloats, customShorts, customInts, customBytes, doPStorage: false, flags, mapflags, 1);
		GL.BindBuffer((BufferTarget)34962, 0);
		if (ClientPlatformAbstract.singleIndexBufferSize < indicesSize)
		{
			ClientPlatformAbstract.singleIndexBufferSize = indicesSize;
			if (ClientPlatformAbstract.singleIndexBufferSize != 0)
			{
				ClientPlatformAbstract.DisposeIndexBuffer();
			}
			ClientPlatformAbstract.singleIndexBufferId = GL.GenBuffer();
			GL.BindBuffer((BufferTarget)34963, ClientPlatformAbstract.singleIndexBufferId);
			int[] array = new int[(indicesSize + 23) / 24 * 6];
			for (int i = 0; i < array.Length; i += 6)
			{
				int num3 = (array[i] = i / 6 * 4);
				array[i + 1] = num3 + 1;
				array[i + 2] = num3 + 2;
				array[i + 3] = num3;
				array[i + 4] = num3 + 2;
				array[i + 5] = num3 + 3;
			}
			GL.BufferData<int>((BufferTarget)34963, indicesSize, array, (BufferUsageHint)35044);
			GL.BindBuffer((BufferTarget)34963, 0);
		}
		GL.BindVertexArray(0);
		CheckGlError("Failed loading model");
		vAO.Persistent = false;
		vAO.VaoId = num;
		vAO.IndicesCount = indicesSize;
		vAO.vaoSlotNumber = vaoSlotNumber;
		vAO.vboIdIndex = ClientPlatformAbstract.singleIndexBufferId;
		vAO.normalsVboId = 0;
		vAO.xyzVboId = num2;
		vAO.uvVboId = 0;
		vAO.rgbaVboId = rgbaVboId;
		vAO.flagsVboId = 0;
		vAO.drawMode = DrawModeToPrimiteType(drawMode);
		return vAO;
	}

	private PrimitiveType DrawModeToPrimiteType(EnumDrawMode drawmode)
	{
		return (PrimitiveType)(drawmode switch
		{
			EnumDrawMode.Lines => 1, 
			EnumDrawMode.LineStrip => 3, 
			_ => 4, 
		});
	}

	public override bool LoadMouseCursor(string cursorCoode, int hotx, int hoty, BitmapRef bmpRef)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Expected O, but got Unknown
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			SKBitmap val = ((BitmapExternal)bmpRef).bmp;
			if (val.Width > 32 || val.Height > 32)
			{
				return false;
			}
			float gUIScale = ClientSettings.GUIScale;
			if (gUIScale != 1f)
			{
				val = val.Resize(new SKImageInfo((int)((float)val.Width * gUIScale), (int)((float)val.Height * gUIScale)), new SKSamplingOptions(SKCubicResampler.Mitchell));
			}
			int num = 0;
			byte[] array = new byte[val.BytesPerPixel * val.Width * val.Height];
			for (int i = 0; i < val.Height; i++)
			{
				for (int j = 0; j < val.Width; j++)
				{
					SKColor pixel = val.GetPixel(j, i);
					array[num] = ((SKColor)(ref pixel)).Red;
					array[num + 1] = ((SKColor)(ref pixel)).Green;
					array[num + 2] = ((SKColor)(ref pixel)).Blue;
					array[num + 3] = ((SKColor)(ref pixel)).Alpha;
					num += 4;
				}
			}
			preLoadedCursors[cursorCoode] = new MouseCursor(hotx, hoty, val.Width, val.Height, array);
			((SKNativeObject)val).Dispose();
		}
		catch (Exception e)
		{
			Logger.Error("Failed loading mouse cursor {0}:", cursorCoode);
			Logger.Error(e);
			RestoreWindowCursor();
			return false;
		}
		return true;
	}

	public override void UseMouseCursor(string cursorCode, bool forceUpdate = false)
	{
		if ((cursorCode == null || cursorCode == CurrentMouseCursor) && !forceUpdate)
		{
			return;
		}
		try
		{
			((NativeWindow)window).Cursor = preLoadedCursors[cursorCode];
			CurrentMouseCursor = cursorCode;
		}
		catch
		{
			RestoreWindowCursor();
		}
	}

	public override void RestoreWindowCursor()
	{
		((NativeWindow)window).Cursor = MouseCursor.Default;
	}

	private void UpdateMousePosition()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Invalid comparison between Unknown and I4
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		if (!((NativeWindow)window).IsFocused || ((NativeWindow)window).MouseState.Position == previousMousePosition)
		{
			return;
		}
		float num;
		float num2;
		if (previousCursorState != ((NativeWindow)window).CursorState)
		{
			num = (num2 = 0f);
		}
		else
		{
			num = ((NativeWindow)window).MouseState.Position.X - previousMousePosition.X;
			num2 = ((NativeWindow)window).MouseState.Position.Y - previousMousePosition.Y;
		}
		foreach (MouseEventHandler mouseEventHandler in mouseEventHandlers)
		{
			MouseEvent e = new MouseEvent((int)mouseX, (int)mouseY, (int)num, (int)num2);
			mouseEventHandler.OnMouseMove(e);
		}
		if ((int)((NativeWindow)window).CursorState == 2)
		{
			ignoreMouseMoveEvent = true;
			SetMousePosition((float)((NativeWindow)window).ClientSize.X / 2f, (float)((NativeWindow)window).ClientSize.Y / 2f);
		}
		else if (ignoreMouseMoveEvent)
		{
			ignoreMouseMoveEvent = false;
		}
		previousMousePosition = ((NativeWindow)window).MouseState.Position;
		previousCursorState = ((NativeWindow)window).CursorState;
	}

	private void Mouse_Move(MouseMoveEventArgs e)
	{
		if (!ignoreMouseMoveEvent)
		{
			SetMousePosition(((MouseMoveEventArgs)(ref e)).X, ((MouseMoveEventArgs)(ref e)).Y);
		}
	}

	private void SetMousePosition(float x, float y)
	{
		mouseX = x;
		mouseY = y;
		if (RuntimeEnv.OS == OS.Mac)
		{
			mouseY += ClientSettings.WeirdMacOSMouseYOffset;
		}
	}

	private void Mouse_WheelChanged(MouseWheelEventArgs e)
	{
		foreach (MouseEventHandler mouseEventHandler in mouseEventHandlers)
		{
			float num = ((MouseWheelEventArgs)(ref e)).OffsetY * ClientSettings.MouseWheelSensivity;
			if (RuntimeEnv.OS == OS.Mac)
			{
				num = GameMath.Clamp(num, -1f, 1f);
			}
			prevWheelValue += num;
			MouseWheelEventArgs e2 = new MouseWheelEventArgs
			{
				delta = (int)num,
				deltaPrecise = (int)num,
				value = (int)prevWheelValue,
				valuePrecise = prevWheelValue
			};
			mouseEventHandler.OnMouseWheel(e2);
		}
	}

	private void Mouse_ButtonDown(MouseButtonEventArgs e)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected I4, but got Unknown
		EnumMouseButton button = MouseButtonConverter.ToEnumMouseButton(((MouseButtonEventArgs)(ref e)).Button);
		foreach (MouseEventHandler mouseEventHandler in mouseEventHandlers)
		{
			MouseEvent e2 = new MouseEvent((int)mouseX, (int)mouseY, button, (int)((MouseButtonEventArgs)(ref e)).Modifiers);
			mouseEventHandler.OnMouseDown(e2);
		}
	}

	private void Mouse_ButtonUp(MouseButtonEventArgs e)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected I4, but got Unknown
		EnumMouseButton button = MouseButtonConverter.ToEnumMouseButton(((MouseButtonEventArgs)(ref e)).Button);
		foreach (MouseEventHandler mouseEventHandler in mouseEventHandlers)
		{
			MouseEvent e2 = new MouseEvent((int)mouseX, (int)mouseY, button, (int)((MouseButtonEventArgs)(ref e)).Modifiers);
			mouseEventHandler.OnMouseUp(e2);
		}
	}

	private void game_KeyPress(TextInputEventArgs e)
	{
		foreach (KeyEventHandler keyEventHandler in keyEventHandlers)
		{
			keyEventHandler.OnKeyPress(new KeyEvent
			{
				KeyCode = ((TextInputEventArgs)(ref e)).Unicode,
				KeyChar = (char)((TextInputEventArgs)(ref e)).Unicode
			});
		}
	}

	private void game_KeyDown(KeyboardKeyEventArgs e)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((KeyboardKeyEventArgs)(ref e)).Key == -1)
		{
			return;
		}
		int keyCode = KeyConverter.NewKeysToGlKeys[((KeyboardKeyEventArgs)(ref e)).Key];
		foreach (KeyEventHandler keyEventHandler in keyEventHandlers)
		{
			KeyEvent keyEvent = new KeyEvent
			{
				KeyCode = keyCode
			};
			if (EllapsedMs - lastKeyUpMs <= 200)
			{
				keyEvent.KeyCode2 = lastKeyUpKey;
			}
			keyEvent.CommandPressed = ((KeyboardKeyEventArgs)(ref e)).Command;
			keyEvent.CtrlPressed = ((KeyboardKeyEventArgs)(ref e)).Control;
			keyEvent.ShiftPressed = ((KeyboardKeyEventArgs)(ref e)).Shift;
			keyEvent.AltPressed = ((KeyboardKeyEventArgs)(ref e)).Alt;
			keyEventHandler.OnKeyDown(keyEvent);
		}
	}

	private void game_KeyUp(KeyboardKeyEventArgs e)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((KeyboardKeyEventArgs)(ref e)).Key == -1)
		{
			return;
		}
		int keyCode = KeyConverter.NewKeysToGlKeys[((KeyboardKeyEventArgs)(ref e)).Key];
		lastKeyUpMs = EllapsedMs;
		lastKeyUpKey = keyCode;
		foreach (KeyEventHandler keyEventHandler in keyEventHandlers)
		{
			KeyEvent keyEvent = new KeyEvent
			{
				KeyCode = keyCode
			};
			keyEvent.CommandPressed = ((KeyboardKeyEventArgs)(ref e)).Command;
			keyEvent.CtrlPressed = ((KeyboardKeyEventArgs)(ref e)).Control;
			keyEvent.ShiftPressed = ((KeyboardKeyEventArgs)(ref e)).Shift;
			keyEvent.AltPressed = ((KeyboardKeyEventArgs)(ref e)).Alt;
			keyEventHandler.OnKeyUp(keyEvent);
		}
	}

	public override MouseEvent CreateMouseEvent(EnumMouseButton button)
	{
		return new MouseEvent((int)mouseX, (int)mouseY, button, 0);
	}

	public override int GetUniformLocation(ShaderProgram program, string name)
	{
		return GL.GetUniformLocation(program.ProgramId, name);
	}

	public override bool CompileShader(Shader shader)
	{
		int num = (shader.ShaderId = GL.CreateShader((ShaderType)shader.shaderType));
		string text = shader.Code;
		if (text != null)
		{
			if (text.IndexOfOrdinal("#version") == -1)
			{
				logger.Warning("Shader {0}: Is not defining a shader version via #version", shader.Filename);
			}
			if (RuntimeEnv.OS == OS.Mac)
			{
				text = Regex.Replace(text, "#version \\d+", "#version 330");
			}
			else if (ScreenManager.Platform.UseSSBOs && shader.UsesSSBOs())
			{
				text = Regex.Replace(text, "#version \\d+", "#version 430");
			}
			int startIndex = text.IndexOf('\n', Math.Max(0, text.IndexOfOrdinal("#version"))) + 1;
			text = text.Insert(startIndex, shader.PrefixCode);
		}
		GL.ShaderSource(num, text);
		GL.CompileShader(num);
		int num2 = default(int);
		GL.GetShader(num, (ShaderParameter)35713, ref num2);
		if (num2 != 1)
		{
			string shaderInfoLog = GL.GetShaderInfoLog(num);
			logger.Error("Shader compile error in {0} {1}", shader.Filename, shaderInfoLog.TrimEnd());
			logger.VerboseDebug("{0}", text);
			return false;
		}
		return true;
	}

	public override bool CreateShaderProgram(ShaderProgram program)
	{
		bool result = true;
		int num = (program.ProgramId = GL.CreateProgram());
		GL.AttachShader(num, program.VertexShader.ShaderId);
		GL.AttachShader(num, program.FragmentShader.ShaderId);
		if (program.GeometryShader != null)
		{
			GL.AttachShader(num, program.GeometryShader.ShaderId);
		}
		foreach (KeyValuePair<int, string> attribute in program.attributes)
		{
			GL.BindAttribLocation(program.ProgramId, attribute.Key, attribute.Value);
		}
		GL.LinkProgram(num);
		int num2 = default(int);
		GL.GetProgram(num, (GetProgramParameterName)35714, ref num2);
		string programInfoLog = GL.GetProgramInfoLog(num);
		if (num2 != 1)
		{
			logger.Error("Link error in shader program for pass {0}: {1}", program.PassName, programInfoLog.TrimEnd());
			result = false;
		}
		else
		{
			logger.Notification("Loaded Shaderprogramm for render pass {0}.", program.PassName);
		}
		return result;
	}
}
