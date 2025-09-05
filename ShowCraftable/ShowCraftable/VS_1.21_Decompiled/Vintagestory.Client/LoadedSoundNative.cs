using System;
using System.Collections.Generic;
using System.Threading;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public class LoadedSoundNative : ILoadedSound, IDisposable
{
	private static object loadedSoundsLock = new object();

	private static readonly List<LoadedSoundNative> loadedSounds = new List<LoadedSoundNative>();

	private SoundParams soundParams;

	private AudioMetaData sample;

	private int sourceId;

	private int bufferId;

	private int fadeState;

	private bool disposed;

	private string trace;

	private float pitchOffset;

	private float playbackPosition;

	private ALSourceState sourceState = (ALSourceState)4116;

	private float soundLengthSeconds;

	private bool didStop;

	private long stoppedMsAgo;

	private int curFadingIter;

	public SoundParams Params => soundParams;

	public bool IsDisposed => disposed;

	public int Channels => sample.Channels;

	public bool IsPlaying
	{
		get
		{
			int num = default(int);
			AL.GetSource(sourceId, (ALGetSourcei)4112, ref num);
			if (!disposed)
			{
				return num == 4114;
			}
			return false;
		}
	}

	public bool IsPaused
	{
		get
		{
			int num = default(int);
			AL.GetSource(sourceId, (ALGetSourcei)4112, ref num);
			if (!disposed)
			{
				return num == 4115;
			}
			return false;
		}
	}

	public bool IsReady => sample.Loaded == 3;

	public bool HasStopped
	{
		get
		{
			if (disposed)
			{
				return true;
			}
			int num = default(int);
			AL.GetSource(sourceId, (ALGetSourcei)4112, ref num);
			if (num != 4114)
			{
				return num != 4115;
			}
			return false;
		}
	}

	public float PlaybackPosition
	{
		get
		{
			if (!disposed)
			{
				AL.GetSource(sourceId, (ALSourcef)4132, ref playbackPosition);
				testError("get playback pos");
			}
			return playbackPosition;
		}
		set
		{
			playbackPosition = value;
			if (!disposed)
			{
				AL.Source(sourceId, (ALSourcef)4132, value);
				testError("set playback pos");
			}
		}
	}

	public float SoundLengthSeconds => soundLengthSeconds;

	private float GlobalVolume
	{
		get
		{
			if (soundParams.SoundType == EnumSoundType.Music || soundParams.SoundType == EnumSoundType.MusicGlitchunaffected)
			{
				return (float)ClientSettings.MusicLevel / 100f;
			}
			if (soundParams.SoundType == EnumSoundType.Weather)
			{
				return (float)ClientSettings.WeatherSoundLevel / 100f;
			}
			if (soundParams.SoundType == EnumSoundType.Ambient || soundParams.SoundType == EnumSoundType.AmbientGlitchunaffected)
			{
				return (float)ClientSettings.AmbientSoundLevel / 100f;
			}
			if (soundParams.SoundType == EnumSoundType.Entity)
			{
				return (float)ClientSettings.EntitySoundLevel / 100f;
			}
			return (float)ClientSettings.SoundLevel / 100f;
		}
	}

	public bool IsFadingIn => fadeState == 1;

	public bool IsFadingOut => fadeState == 2;

	public bool HasReverbStopped(long elapsedMilliseconds)
	{
		if (Params.ReverbDecayTime <= 0f)
		{
			return true;
		}
		if (!didStop && HasStopped)
		{
			didStop = true;
			stoppedMsAgo = elapsedMilliseconds;
		}
		return (float)(elapsedMilliseconds - stoppedMsAgo) > Params.ReverbDecayTime * 1000f;
	}

	public static void ChangeOutputDevice(Action changeCallback)
	{
		lock (loadedSoundsLock)
		{
			foreach (LoadedSoundNative loadedSound in loadedSounds)
			{
				loadedSound.disposeSoundSource();
			}
		}
		try
		{
			changeCallback();
		}
		finally
		{
			lock (loadedSoundsLock)
			{
				foreach (LoadedSoundNative loadedSound2 in loadedSounds)
				{
					loadedSound2.createSoundSource();
				}
			}
		}
	}

	public LoadedSoundNative(SoundParams soundParams, AudioMetaData sample, ClientMain game)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		this.sample = sample;
		this.soundParams = soundParams;
		testError("construction before");
		if (RuntimeEnv.DebugSoundDispose)
		{
			trace = Environment.StackTrace;
		}
		lock (loadedSoundsLock)
		{
			loadedSounds.Add(this);
		}
		switch (sample.Loaded)
		{
		case 0:
			sample.Load();
			createSoundSource();
			break;
		case 1:
			sample.AddOnLoaded(new MainThreadAction(game, createSoundSource, "soundloading"));
			break;
		default:
			createSoundSource();
			break;
		}
	}

	public LoadedSoundNative(SoundParams soundParams, AudioMetaData sample)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		this.sample = sample;
		this.soundParams = soundParams;
		testError("construction before");
		if (RuntimeEnv.DebugSoundDispose)
		{
			trace = Environment.StackTrace;
		}
		lock (loadedSoundsLock)
		{
			loadedSounds.Add(this);
		}
		if (sample.Loaded == 0)
		{
			sample.Load();
		}
		int num = 64;
		while (sample.Loaded < 2 && num-- > 0)
		{
			Thread.Sleep(15);
		}
		createSoundSource();
	}

	private unsafe int createSoundSource()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Invalid comparison between Unknown and I4
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Invalid comparison between Unknown and I4
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Invalid comparison between Unknown and I4
		try
		{
			sourceId = AL.GenSource();
			if (sourceId == 0)
			{
				throw new Exception("Unable to get sourceId");
			}
			bufferId = AL.GenBuffer();
			ALFormat soundFormat = AudioOpenAl.GetSoundFormat(sample.Channels, sample.BitsPerSample);
			fixed (byte* pcm = sample.Pcm)
			{
				AL.BufferData(bufferId, soundFormat, (void*)pcm, sample.Pcm.Length, sample.Rate);
			}
			int num = default(int);
			AL.GetBuffer(bufferId, (ALGetBufferi)8196, ref num);
			int num2 = default(int);
			AL.GetBuffer(bufferId, (ALGetBufferi)8195, ref num2);
			soundLengthSeconds = 1f * (float)num / (float)(sample.Rate * sample.BitsPerSample / 8) / (float)num2;
			float num3 = 0f - (float)(Math.Log(0.009999999776482582) / Math.Log(soundParams.Range));
			float num4 = (float)Math.Max(3.0, Math.Pow(soundParams.Range, 0.5) - 2.0);
			AL.DistanceModel((ALDistanceModel)53254);
			AL.Source(sourceId, (ALSourcef)4129, num3);
			AL.Source(sourceId, (ALSourcef)4128, (soundParams.ReferenceDistance != 3f) ? soundParams.ReferenceDistance : num4);
			AL.Source(sourceId, (ALSourcef)4131, 9999f);
			AL.Source(sourceId, (ALSourcei)4105, bufferId);
			AL.Source(sourceId, (ALSourcef)4106, soundParams.Volume * GlobalVolume);
			AL.Source(sourceId, (ALSourcef)4099, GameMath.Clamp(soundParams.Pitch + pitchOffset, 0.1f, 3f));
			bool flag = soundFormat - 4354 <= 1;
			if (flag && AudioOpenAl.UseHrtf)
			{
				AL.Source(sourceId, (ALSourcei)4147, 2);
			}
			if (soundParams.Position != null)
			{
				Vector3 val = default(Vector3);
				((Vector3)(ref val))._002Ector(soundParams.Position.X, soundParams.Position.Y, soundParams.Position.Z);
				AL.Source(sourceId, (ALSource3f)4100, ref val);
			}
			AL.Source(sourceId, (ALSourceb)514, soundParams.RelativePosition);
			AL.Source(sourceId, (ALSourceb)4103, soundParams.ShouldLoop);
			if (playbackPosition > 0f)
			{
				AL.Source(sourceId, (ALSourcef)4132, playbackPosition);
			}
			testError("setup");
			SetReverb(Params.ReverbDecayTime);
			SetLowPassfiltering(Params.LowPassFilter);
			testError("filter");
			ALSourceState val2 = sourceState;
			if ((int)val2 != 4114)
			{
				if ((int)val2 == 4115)
				{
					AL.SourcePause(sourceId);
				}
			}
			else
			{
				AL.SourcePlay(sourceId);
			}
			sample.Loaded = 3;
			disposed = false;
		}
		catch (Exception e)
		{
			ScreenManager.Platform.Logger.Error("Could not load sound " + soundParams?.Location);
			ScreenManager.Platform.Logger.Error(e);
			disposed = true;
		}
		testError("construction");
		return 0;
	}

	public void SetReverb(float reverbDecayTime)
	{
		Params.ReverbDecayTime = reverbDecayTime;
		if (AudioOpenAl.HasEffectsExtension)
		{
			if (Params.ReverbDecayTime > 0f)
			{
				ReverbEffect orCreateReverbEffect = AudioOpenAl.GetOrCreateReverbEffect(reverbDecayTime);
				AL.Source(sourceId, (ALSource3i)131078, orCreateReverbEffect.ReverbEffectSlot, 0, 0);
			}
			else
			{
				AL.Source(sourceId, (ALSource3i)131078, 0, 0, 0);
			}
			testError("SetReverb");
		}
	}

	private void DisposeReverb()
	{
		if (AudioOpenAl.HasEffectsExtension)
		{
			AL.Source(sourceId, (ALSource3i)131078, 0, 0, 0);
			testError("disposereverb");
		}
	}

	public void SetLowPassfiltering(float value)
	{
		Params.LowPassFilter = value;
		if (!AudioOpenAl.HasEffectsExtension)
		{
			return;
		}
		if (Params.LowPassFilter < 1f)
		{
			if (AudioOpenAl.EchoFilterId == 0)
			{
				AudioOpenAl.EchoFilterId = EFX.GenFilter();
				EFX.Filter(AudioOpenAl.EchoFilterId, (FilterInteger)32769, 1);
				EFX.Filter(AudioOpenAl.EchoFilterId, (FilterFloat)1, 1f);
				EFX.Filter(AudioOpenAl.EchoFilterId, (FilterFloat)2, Params.LowPassFilter);
			}
			AL.Source(sourceId, (ALSourcei)131077, AudioOpenAl.EchoFilterId);
		}
		else
		{
			AL.Source(sourceId, (ALSourcei)131077, 0);
		}
		testError("SetLowPassfiltering");
	}

	private void DisposeLowPassfilter()
	{
		if (AudioOpenAl.HasEffectsExtension)
		{
			AL.Source(sourceId, (ALSourcei)131077, 0);
			testError("disposeLowPassFilter");
		}
	}

	private void disposeSoundSource()
	{
		if (sourceId == 0)
		{
			disposed = true;
			return;
		}
		AL.GetSource(sourceId, (ALSourcef)4132, ref playbackPosition);
		AL.SourceStop(sourceId);
		AL.Source(sourceId, (ALSourcei)4105, 0);
		testError("disposestop");
		DisposeLowPassfilter();
		DisposeReverb();
		AL.DeleteSource(sourceId);
		AL.DeleteBuffer(bufferId);
		testError("dispose");
		sourceId = 0;
		bufferId = 0;
		disposed = true;
	}

	public void SetPosition(float x, float y, float z)
	{
		if (soundParams.Position == null)
		{
			soundParams.Position = new Vec3f(x, y, z);
			soundParams.RelativePosition = false;
			if (!disposed)
			{
				AL.Source(sourceId, (ALSourceb)514, false);
			}
		}
		else
		{
			soundParams.Position.Set(x, y, z);
		}
		if (sourceId != 0)
		{
			if (!disposed)
			{
				Vector3 val = default(Vector3);
				((Vector3)(ref val))._002Ector(x, y, z);
				AL.Source(sourceId, (ALSource3f)4100, ref val);
			}
			testError("setposition x/y/z");
		}
	}

	public void SetPosition(Vec3f position)
	{
		testError("before setposition vec3");
		if (soundParams.Position == null)
		{
			soundParams.Position = position.Clone();
			soundParams.RelativePosition = false;
			if (!disposed)
			{
				AL.Source(sourceId, (ALSourceb)514, false);
			}
		}
		else
		{
			soundParams.Position.Set(position.X, position.Y, position.Z);
		}
		if (sourceId != 0)
		{
			if (!disposed)
			{
				Vector3 val = default(Vector3);
				((Vector3)(ref val))._002Ector(position.X, position.Y, position.Z);
				AL.Source(sourceId, (ALSource3f)4100, ref val);
			}
			testError("setposition vec3");
		}
	}

	public void SetPitch(float val)
	{
		soundParams.Pitch = val;
		if (sourceId != 0 && !disposed)
		{
			AL.Source(sourceId, (ALSourcef)4099, GameMath.Clamp(soundParams.Pitch + pitchOffset, 0.1f, 3f));
			testError("SetPitch");
		}
	}

	public void SetPitchOffset(float val)
	{
		pitchOffset = val;
		if (sourceId != 0 && !disposed)
		{
			AL.Source(sourceId, (ALSourcef)4099, GameMath.Clamp(soundParams.Pitch + pitchOffset, 0.1f, 3f));
			testError("SetPitchOffset");
		}
	}

	public void SetVolume()
	{
		if (sourceId != 0 && !disposed)
		{
			AL.Source(sourceId, (ALSourcef)4106, soundParams.Volume * GlobalVolume);
			testError("setvolume");
		}
	}

	public void SetVolume(float val)
	{
		soundParams.Volume = val;
		if (sourceId != 0 && !disposed)
		{
			AL.Source(sourceId, (ALSourcef)4106, val * GlobalVolume);
			testError("setvolume(val)");
		}
	}

	public void Toggle(bool on)
	{
		if (on)
		{
			Start();
		}
		else
		{
			Stop();
		}
	}

	public void Start()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		sourceState = (ALSourceState)4114;
		if (!disposed)
		{
			AL.SourcePlay(sourceId);
			testError("start");
		}
	}

	public void Stop()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		sourceState = (ALSourceState)4116;
		if (!disposed)
		{
			AL.SourceStop(sourceId);
			testError("stop");
		}
	}

	public void Pause()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		sourceState = (ALSourceState)4115;
		if (!disposed)
		{
			AL.SourcePause(sourceId);
			testError("pause");
		}
	}

	public void Dispose()
	{
		if (!disposed)
		{
			lock (loadedSoundsLock)
			{
				disposeSoundSource();
				loadedSounds.Remove(this);
			}
			if (ClientSettings.OptimizeRamMode == 2 && sample.AutoUnload)
			{
				sample?.Unload();
			}
		}
	}

	private void testError(string during)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Invalid comparison between Unknown and I4
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		ALError error = AL.GetError();
		if ((int)error != 0)
		{
			ScreenManager.Platform.Logger.Warning("OpenAL Error during {0} of sound {1}: {2}", during, Params?.Location, error);
		}
		if ((int)error != 40965 || !RuntimeEnv.DebugSoundDispose)
		{
			return;
		}
		ScreenManager.Platform.Logger.Warning("OutOfMemory error detected. Sound dispose debug enabled, printing all active sources ({0}):", loadedSounds.Count);
		lock (loadedSoundsLock)
		{
			foreach (LoadedSoundNative loadedSound in loadedSounds)
			{
				ScreenManager.Platform.Logger.Notification(loadedSound.sourceId + ": " + loadedSound.soundParams?.Location.ToShortString());
			}
		}
		throw new Exception("Sound dispose debug enabled, killing game. More debug info see client-main.log");
	}

	~LoadedSoundNative()
	{
		if (!disposed && !ScreenManager.Platform.IsShuttingDown)
		{
			lock (loadedSoundsLock)
			{
				loadedSounds.Remove(this);
			}
			if (trace == null)
			{
				ScreenManager.Platform.Logger.Debug("Loaded sound {0} is leaking memory, missing call to Dispose()", Params.Location);
			}
			else
			{
				ScreenManager.Platform.Logger.Debug("Loaded sound {0} is leaking memory, missing call to Dispose(). Allocated at {1}", Params.Location, trace);
			}
		}
	}

	public void FadeTo(double newVolume, float duration, Action<ILoadedSound> onFaded)
	{
		duration = Math.Max(0.02f, duration);
		newVolume = GameMath.Clamp(newVolume, 0.01, 1.0);
		double curVolume = Params.Volume;
		fadeState = 0;
		if (newVolume > curVolume)
		{
			fadeState = 1;
		}
		if (newVolume < curVolume)
		{
			fadeState = 2;
		}
		double num = 0.019999999552965164;
		double factor = 1.0 - num;
		if (newVolume > curVolume)
		{
			factor = 1.0 + num;
			if (curVolume <= 0.0)
			{
				curVolume = 0.01;
			}
		}
		double num2 = Math.Ceiling((Math.Log(newVolume) - Math.Log(curVolume)) / Math.Log(factor)) / (double)duration;
		double stepsPer10ms = num2 / 100.0;
		int sleepMs = 10;
		if (RuntimeEnv.OS == OS.Windows && duration <= 0.1f)
		{
			sleepMs = 0;
		}
		TyronThreadPool.QueueLongDurationTask(delegate
		{
			double num3 = 0.0;
			int num4 = ++curFadingIter;
			while ((factor > 1.0) ? (newVolume - curVolume > 0.01) : (curVolume - newVolume > 0.01))
			{
				Thread.Sleep(sleepMs);
				for (num3 += stepsPer10ms; num3 > 1.0; num3 -= 1.0)
				{
					SetVolume((float)(curVolume *= factor));
				}
				if (num4 != curFadingIter)
				{
					return;
				}
			}
			Params.Volume = (float)curVolume;
			fadeState = 0;
			if (onFaded != null && num4 == curFadingIter)
			{
				ScreenManager.EnqueueMainThreadTask(delegate
				{
					onFaded(this);
				});
			}
		});
	}

	public void FadeOut(float duration, Action<ILoadedSound> onFadedOut)
	{
		FadeTo(0.0, duration, onFadedOut);
	}

	public void FadeIn(float duration, Action<ILoadedSound> onFadedIn)
	{
		FadeTo(1.0, duration, onFadedIn);
	}

	public void FadeOutAndStop(float duration)
	{
		FadeTo(0.0, duration, delegate
		{
			Stop();
		});
	}

	public void SetLooping(bool on)
	{
		Params.ShouldLoop = on;
		AL.Source(sourceId, (ALSourceb)4103, on);
		testError("set looping");
	}

	public static void DisposeAllSounds()
	{
		lock (loadedSoundsLock)
		{
			foreach (LoadedSoundNative loadedSound in loadedSounds)
			{
				loadedSound.disposeSoundSource();
			}
		}
	}
}
