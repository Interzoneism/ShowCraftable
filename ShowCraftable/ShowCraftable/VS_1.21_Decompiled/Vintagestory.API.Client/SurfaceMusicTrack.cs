using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class SurfaceMusicTrack : IMusicTrack
{
	[JsonProperty("File")]
	public AssetLocation Location;

	[JsonProperty]
	public string OnPlayList = "*";

	public string[] OnPlayLists;

	[JsonProperty]
	public int MinSunlight = 5;

	[JsonProperty]
	public float MinHour;

	[JsonProperty]
	public float MaxHour = 24f;

	[JsonProperty]
	public float Chance = 1f;

	[JsonProperty]
	public float MaxTemperature = 99f;

	[JsonProperty]
	public float MinRainFall = -99f;

	[JsonProperty]
	public float MinSeason;

	[JsonProperty]
	public float MaxSeason = 1f;

	[JsonProperty]
	public float MinLatitude;

	[JsonProperty]
	public float MaxLatitude = 1f;

	[JsonProperty]
	public float DistanceToSpawnPoint = -1f;

	private bool loading;

	public ILoadedSound Sound;

	private static Random rand = new Random();

	private static readonly float[][] AnySongCoolDowns = new float[4][]
	{
		new float[2] { 960f, 480f },
		new float[2] { 420f, 240f },
		new float[2] { 180f, 120f },
		new float[2]
	};

	private static readonly float[][] SameSongCoolDowns = new float[4][]
	{
		new float[2] { 1500f, 1200f },
		new float[2] { 1200f, 1200f },
		new float[2] { 900f, 900f },
		new float[2] { 480f, 300f }
	};

	public static bool ShouldPlayMusic = true;

	private static bool initialized = false;

	public static long globalCooldownUntilMs;

	public static Dictionary<string, long> tracksCooldownUntilMs = new Dictionary<string, long>();

	protected ICoreClientAPI capi;

	protected IMusicEngine musicEngine;

	protected float nowMinHour;

	protected float nowMaxHour;

	protected static int prevFrequency;

	[JsonProperty]
	public float Priority { get; set; } = 1f;

	[JsonProperty("StartPriority")]
	public NatFloat StartPriorityRnd { get; set; } = NatFloat.One;

	public float StartPriority { get; set; }

	public bool IsActive
	{
		get
		{
			if (!loading)
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

	public string Name => Location.ToShortString();

	public int MusicFrequency => capi.Settings.Int["musicFrequency"];

	public string PositionString => $"{(int)Sound.PlaybackPosition}/{(int)Sound.SoundLengthSeconds}";

	public virtual void Initialize(IAssetManager assetManager, ICoreClientAPI capi, IMusicEngine musicEngine)
	{
		this.capi = capi;
		this.musicEngine = musicEngine;
		OnPlayLists = OnPlayList.Split('|');
		for (int i = 0; i < OnPlayLists.Length; i++)
		{
			OnPlayLists[i] = OnPlayLists[i].ToLowerInvariant();
		}
		selectMinMaxHour();
		Location.Path = "music/" + Location.Path.ToLowerInvariant() + ".ogg";
		if (!initialized)
		{
			globalCooldownUntilMs = (long)(1000.0 * ((double)(AnySongCoolDowns[MusicFrequency][0] / 4f) + rand.NextDouble() * (double)AnySongCoolDowns[MusicFrequency][1] / 2.0));
			capi.Settings.Int.AddWatcher("musicFrequency", delegate(int newval)
			{
				FrequencyChanged(newval, capi);
			});
			initialized = true;
			prevFrequency = MusicFrequency;
		}
	}

	public virtual void BeginSort()
	{
		StartPriority = Math.Max(1f, StartPriorityRnd.nextFloat(1f, rand));
	}

	protected virtual void selectMinMaxHour()
	{
		Random random = capi.World.Rand;
		float num = Math.Min(2 + Math.Max(0, prevFrequency), MaxHour - MinHour);
		nowMinHour = Math.Max(MinHour, Math.Min(MaxHour - 1f, MinHour - 1f + (float)(random.NextDouble() * (double)(num + 1f))));
		nowMaxHour = Math.Min(MaxHour, nowMinHour + num);
	}

	protected static void FrequencyChanged(int newFreq, ICoreClientAPI capi)
	{
		if (newFreq > prevFrequency)
		{
			globalCooldownUntilMs = 0L;
		}
		if (newFreq < prevFrequency)
		{
			globalCooldownUntilMs = (long)((double)capi.World.ElapsedMilliseconds + 1000.0 * ((double)(AnySongCoolDowns[newFreq][0] / 4f) + rand.NextDouble() * (double)AnySongCoolDowns[newFreq][1] / 2.0));
		}
		prevFrequency = newFreq;
	}

	public virtual bool ShouldPlay(TrackedPlayerProperties props, ClimateCondition conds, BlockPos pos)
	{
		if (IsActive || !ShouldPlayMusic)
		{
			return false;
		}
		if (capi.World.ElapsedMilliseconds < globalCooldownUntilMs)
		{
			return false;
		}
		if (OnPlayList != "*" && !OnPlayLists.Contains(props.PlayListCode))
		{
			return false;
		}
		if (props.sunSlight < (float)MinSunlight)
		{
			return false;
		}
		if (musicEngine.LastPlayedTrack == this)
		{
			return false;
		}
		if (conds.Temperature > MaxTemperature)
		{
			return false;
		}
		if (conds.Rainfall < MinRainFall)
		{
			return false;
		}
		if (props.DistanceToSpawnPoint < DistanceToSpawnPoint)
		{
			return false;
		}
		float seasonRel = capi.World.Calendar.GetSeasonRel(pos);
		if (seasonRel < MinSeason || seasonRel > MaxSeason)
		{
			return false;
		}
		float num = (float)Math.Abs(capi.World.Calendar.OnGetLatitude(pos.Z));
		if (num < MinLatitude || num > MaxLatitude)
		{
			return false;
		}
		tracksCooldownUntilMs.TryGetValue(Name, out var value);
		if (capi.World.ElapsedMilliseconds < value)
		{
			return false;
		}
		if (prevFrequency == 3)
		{
			float num2 = capi.World.Calendar.HourOfDay / 24f * capi.World.Calendar.HoursPerDay;
			if (num2 < MinHour || num2 > MaxHour)
			{
				return false;
			}
		}
		else
		{
			float num3 = capi.World.Calendar.HourOfDay / 24f * capi.World.Calendar.HoursPerDay;
			if (num3 < nowMinHour || num3 > nowMaxHour)
			{
				return false;
			}
		}
		return true;
	}

	public virtual void BeginPlay(TrackedPlayerProperties props)
	{
		loading = true;
		Sound?.Dispose();
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
		if (!IsActive)
		{
			Sound?.Dispose();
			Sound = null;
			SetCooldown(1f);
			return false;
		}
		if (!ShouldPlayMusic)
		{
			FadeOut(3f);
		}
		return true;
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
			SetCooldown(0.5f);
		}
		else
		{
			SetCooldown(1f);
		}
	}

	public virtual void SetCooldown(float multiplier)
	{
		globalCooldownUntilMs = (long)((float)capi.World.ElapsedMilliseconds + (float)(long)(1000.0 * ((double)AnySongCoolDowns[MusicFrequency][0] + rand.NextDouble() * (double)AnySongCoolDowns[MusicFrequency][1])) * multiplier);
		tracksCooldownUntilMs[Name] = (long)((float)capi.World.ElapsedMilliseconds + (float)(long)(1000.0 * ((double)SameSongCoolDowns[MusicFrequency][0] + rand.NextDouble() * (double)SameSongCoolDowns[MusicFrequency][1])) * multiplier);
		selectMinMaxHour();
	}

	public virtual void UpdateVolume()
	{
		if (Sound != null)
		{
			Sound.SetVolume();
		}
	}

	public virtual void FastForward(float seconds)
	{
		if (Sound.PlaybackPosition + seconds > Sound.SoundLengthSeconds)
		{
			Sound.Stop();
		}
		Sound.PlaybackPosition += seconds;
	}
}
