using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class MusicTrack : IMusicTrack
{
	public AssetLocation Location;

	public bool loading;

	public bool ManualDispose;

	public ILoadedSound Sound;

	private IMusicEngine musicEngine;

	private bool docontinue = true;

	public bool ForceActive;

	public bool IsActive
	{
		get
		{
			if (!ForceActive && !loading)
			{
				if (Sound != null)
				{
					return Sound.IsPlaying;
				}
				return false;
			}
			return true;
		}
	}

	public float Priority { get; set; } = 1f;

	public string Name => Location.ToShortString();

	public string PositionString => $"{Sound.PlaybackPosition}/{Sound.SoundLengthSeconds}";

	public virtual float StartPriority => Priority;

	public void Stop()
	{
		docontinue = false;
		Sound?.Stop();
		musicEngine.StopTrack(this);
	}

	public virtual void Initialize(IAssetManager assetManager, ICoreClientAPI capi, IMusicEngine musicEngine)
	{
		this.musicEngine = musicEngine;
		Location.Path = Location.Path.ToLowerInvariant();
		if (!Location.PathStartsWith("sounds"))
		{
			Location.WithPathPrefixOnce("music/");
		}
		Location.WithPathAppendixOnce(".ogg");
	}

	public virtual bool ShouldPlay(TrackedPlayerProperties props, ClimateCondition conds, BlockPos pos)
	{
		if (IsActive)
		{
			return false;
		}
		return true;
	}

	public virtual void BeginPlay(TrackedPlayerProperties props)
	{
		loading = true;
		musicEngine.LoadTrack(Location, delegate(ILoadedSound sound)
		{
			if (sound != null)
			{
				sound.Start();
				if (!loading)
				{
					sound.Stop();
					sound.Dispose();
				}
				else
				{
					Sound = sound;
				}
			}
			loading = false;
		});
	}

	public virtual bool ContinuePlay(float dt, TrackedPlayerProperties props)
	{
		if (ForceActive)
		{
			return true;
		}
		if (!IsActive && !ManualDispose)
		{
			Sound?.Dispose();
			Sound = null;
			return false;
		}
		return docontinue;
	}

	public virtual void FadeOut(float seconds, Action onFadedOut = null)
	{
		loading = false;
		if (Sound != null && IsActive)
		{
			Sound.FadeOut(seconds, delegate(ILoadedSound sound)
			{
				sound.Dispose();
				Sound = null;
				onFadedOut?.Invoke();
			});
		}
	}

	public virtual void UpdateVolume()
	{
		if (Sound != null)
		{
			Sound.SetVolume();
		}
	}

	public void FastForward(float seconds)
	{
		Sound.PlaybackPosition += seconds;
	}

	public virtual void BeginSort()
	{
	}
}
