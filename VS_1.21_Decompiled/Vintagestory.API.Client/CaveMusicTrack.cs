using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class CaveMusicTrack : IMusicTrack
{
	private Random rand = new Random();

	[JsonProperty]
	private MusicTrackPart[] Parts;

	private MusicTrackPart[] PartsShuffled;

	private int maxSimultaenousTracks = 3;

	private float simultaenousTrackChance = 0.01f;

	private float priority = 2f;

	private long activeUntilMs;

	private long cooldownUntilMs;

	private ICoreClientAPI capi;

	private IMusicEngine musicEngine;

	public static bool ShouldPlayCaveMusic = true;

	public string Name
	{
		get
		{
			string text = "";
			for (int i = 0; i < Parts.Length; i++)
			{
				if (Parts[i].IsPlaying)
				{
					if (text.Length > 0)
					{
						text += ", ";
					}
					text += Parts[i].NowPlayingFile.GetName();
				}
			}
			return "Cave Mix (" + text + ")";
		}
	}

	private double SessionPlayTime => 240.0 + 360.0 * rand.NextDouble();

	public bool IsActive
	{
		get
		{
			MusicTrackPart[] parts = Parts;
			foreach (MusicTrackPart musicTrackPart in parts)
			{
				if (musicTrackPart.IsPlaying || musicTrackPart.Loading)
				{
					return true;
				}
			}
			return false;
		}
	}

	float IMusicTrack.Priority => priority;

	public string PositionString => "?";

	public float StartPriority => priority;

	public void Initialize(IAssetManager assetManager, ICoreClientAPI capi, IMusicEngine musicEngine)
	{
		this.capi = capi;
		this.musicEngine = musicEngine;
		PartsShuffled = new MusicTrackPart[Parts.Length];
		for (int i = 0; i < Parts.Length; i++)
		{
			AssetLocation[] array = (AssetLocation[])Parts[i].Files.Clone();
			Parts[i].ExpandFiles(assetManager);
			if (array.Length != 0 && Parts[i].Files.Length == 0)
			{
				capi.Logger.Warning("No files for cave music track part? Will not play anything (first file = {0}).", array[0]);
			}
			PartsShuffled[i] = Parts[i];
		}
	}

	public bool ShouldPlay(TrackedPlayerProperties props, ClimateCondition conds, BlockPos pos)
	{
		if (props.sunSlight > 3f || !ShouldPlayCaveMusic)
		{
			return false;
		}
		if (capi.World.ElapsedMilliseconds < cooldownUntilMs)
		{
			return false;
		}
		return true;
	}

	public void BeginPlay(TrackedPlayerProperties props)
	{
		activeUntilMs = capi.World.ElapsedMilliseconds + (int)(SessionPlayTime * 1000.0);
	}

	public bool ContinuePlay(float dt, TrackedPlayerProperties props)
	{
		if (props.sunSlight > 3f || !ShouldPlayCaveMusic)
		{
			FadeOut(3f);
			return false;
		}
		if (activeUntilMs > 0 && capi.World.ElapsedMilliseconds >= activeUntilMs)
		{
			bool isActive = IsActive;
			if (!isActive)
			{
				activeUntilMs = 0L;
				MusicTrackPart[] parts = Parts;
				for (int i = 0; i < parts.Length; i++)
				{
					parts[i].Sound?.Dispose();
				}
			}
			return isActive;
		}
		int num = 0;
		for (int j = 0; j < Parts.Length; j++)
		{
			num += ((Parts[j].IsPlaying || Parts[j].Loading) ? 1 : 0);
		}
		GameMath.Shuffle(rand, PartsShuffled);
		for (int k = 0; k < PartsShuffled.Length; k++)
		{
			MusicTrackPart part = PartsShuffled[k];
			if (part.Files.Length == 0)
			{
				continue;
			}
			bool isPlaying = part.IsPlaying;
			bool flag = part.Applicable(capi.World, props);
			if (!isPlaying && part.Sound != null)
			{
				part.Sound.Dispose();
				part.Sound = null;
			}
			else if (isPlaying && !flag)
			{
				if (!part.Sound.IsFadingOut)
				{
					part.Sound.FadeOut(3f, delegate
					{
						part.Sound.Dispose();
						part.Sound = null;
					});
				}
			}
			else
			{
				if (!(!isPlaying && flag) || part.Loading || num >= maxSimultaenousTracks || (num != 0 && !(rand.NextDouble() < (double)simultaenousTrackChance)))
				{
					continue;
				}
				AssetLocation assetLocation = part.Files[rand.Next(part.Files.Length)];
				part.NowPlayingFile = assetLocation;
				part.Loading = true;
				musicEngine.LoadTrack(assetLocation, delegate(ILoadedSound sound)
				{
					if (sound != null)
					{
						sound.Start();
						part.Sound = sound;
					}
					part.Loading = false;
				});
				part.StartedMs = capi.World.ElapsedMilliseconds;
				num++;
			}
		}
		return true;
	}

	public void FadeOut(float seconds, Action onFadedOut = null)
	{
		bool flag = false;
		MusicTrackPart[] parts = Parts;
		foreach (MusicTrackPart part in parts)
		{
			if (part.IsPlaying)
			{
				part.Sound.FadeOut(seconds, delegate(ILoadedSound sound)
				{
					sound.Dispose();
					part.Sound = null;
					onFadedOut?.Invoke();
				});
				flag = true;
			}
		}
		if (!flag)
		{
			cooldownUntilMs = capi.World.ElapsedMilliseconds + (long)(1000.0 * (120.0 + rand.NextDouble() * 5.0 * 60.0));
		}
	}

	public void UpdateVolume()
	{
		MusicTrackPart[] parts = Parts;
		foreach (MusicTrackPart musicTrackPart in parts)
		{
			if (musicTrackPart.IsPlaying)
			{
				musicTrackPart.Sound.SetVolume();
			}
		}
	}

	public void FastForward(float seconds)
	{
	}

	public void BeginSort()
	{
	}
}
