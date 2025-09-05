using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface ILoadedSound : IDisposable
{
	float SoundLengthSeconds { get; }

	float PlaybackPosition { get; set; }

	bool IsDisposed { get; }

	bool IsPlaying { get; }

	bool IsFadingIn { get; }

	bool IsFadingOut { get; }

	bool HasStopped { get; }

	int Channels { get; }

	SoundParams Params { get; }

	bool IsPaused { get; }

	bool IsReady { get; }

	void Start();

	void Stop();

	void Pause();

	void Toggle(bool on);

	void SetPitch(float val);

	void SetPitchOffset(float val);

	void SetVolume(float val);

	void SetVolume();

	void SetPosition(Vec3f position);

	void SetPosition(float x, float y, float z);

	void SetLooping(bool on);

	void FadeTo(double newVolume, float duration, Action<ILoadedSound> onFaded);

	void FadeOut(float seconds, Action<ILoadedSound> onFadedOut);

	void FadeIn(float seconds, Action<ILoadedSound> onFadedIn);

	void FadeOutAndStop(float seconds);

	void SetLowPassfiltering(float value);

	void SetReverb(float reverbDecayTime);

	bool HasReverbStopped(long elapsedMilliseconds);
}
