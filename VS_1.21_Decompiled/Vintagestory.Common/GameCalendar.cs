using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class GameCalendar : IGameCalendar
{
	protected float currentSpeedOfTime = 60f;

	public float HoursPerDay = 24f;

	public int MoonOrbitDays = 8;

	public float DayLengthInRealLifeSeconds;

	internal Stopwatch watchIngameTime;

	protected TimeSpan timespan = TimeSpan.Zero;

	protected long totalSecondsStart;

	public Size2i sunLightTextureSize;

	public int[] sunLightTexture;

	protected Dictionary<string, float> timeSpeedModifiers = new Dictionary<string, float>();

	protected NormalizedSimplexNoise sunsetModNoise;

	protected NormalizedSimplexNoise superMoonNoise;

	protected float superMoonSize;

	protected double moonPhaseCached;

	private double moonTauCached;

	private double moonSinDeltaCached;

	protected float timelapse;

	private float calendarSpeedMul = 0.5f;

	public static float[] MoonBrightnesByPhase = new float[8] { -0.1f, 0.1f, 0.2f, 0.26f, 0.33f, 0.26f, 0.2f, 0.1f };

	public static float[] MoonSizeByPhase = new float[8] { 0.8f, 0.9f, 1f, 1.1f, 1.2f, 1.1f, 1f, 0.9f };

	public Dictionary<string, float> TimeSpeedModifiers
	{
		get
		{
			return timeSpeedModifiers;
		}
		set
		{
			timeSpeedModifiers = value;
			CalculateCurrentTimeSpeed();
		}
	}

	public float SpeedOfTime => currentSpeedOfTime;

	public float CalendarSpeedMul
	{
		get
		{
			return calendarSpeedMul;
		}
		set
		{
			calendarSpeedMul = value;
			CalculateCurrentTimeSpeed();
		}
	}

	public float Timelapse
	{
		get
		{
			return timelapse;
		}
		set
		{
			timelapse = value;
		}
	}

	public int DaysPerMonth { get; set; } = 9;

	public int DaysPerYear => DaysPerMonth * 12;

	public int DayOfMonth => (int)(TotalDays % (double)DaysPerMonth) + 1;

	public int MonthsPerYear => DaysPerYear / DaysPerMonth;

	public int FullHourOfDay => (int)(timespan.TotalHours % (double)HoursPerDay);

	public float HourOfDay => (float)(timespan.TotalHours % (double)HoursPerDay);

	public long ElapsedSeconds => (long)(timespan.TotalSeconds - (double)totalSecondsStart);

	public double ElapsedHours => (double)ElapsedSeconds / 60.0 / 60.0;

	public double ElapsedDays => ElapsedHours / (double)HoursPerDay;

	public long TotalSeconds => (long)timespan.TotalSeconds;

	public double TotalHours => timespan.TotalHours;

	public double TotalDays => timespan.TotalHours / (double)HoursPerDay + (double)timelapse;

	public int DayOfYear => (int)(TotalDays % (double)DaysPerYear);

	public float DayOfYearf => (float)(TotalDays % (double)DaysPerYear);

	public int Year => (int)(TotalDays / (double)DaysPerYear);

	public int Month => (int)Math.Ceiling(YearRel * (float)MonthsPerYear);

	public float Monthf => YearRel * (float)MonthsPerYear;

	public EnumMonth MonthName => (EnumMonth)Month;

	public float YearRel => (float)(GameMath.Mod(TotalDays, DaysPerYear) / (double)DaysPerYear);

	int IGameCalendar.DaysPerYear => DaysPerYear;

	float IGameCalendar.HoursPerDay => HoursPerDay;

	public EnumMoonPhase MoonPhase => (EnumMoonPhase)((int)MoonPhaseExact % MoonOrbitDays);

	public double MoonPhaseExact => moonPhaseCached;

	public bool Dusk => (double)(HourOfDay / HoursPerDay) > 0.5;

	public float MoonPhaseBrightness
	{
		get
		{
			double moonPhaseExact = MoonPhaseExact;
			float num = (float)moonPhaseExact - (float)(int)moonPhaseExact;
			float num2 = MoonBrightnesByPhase[(int)moonPhaseExact];
			float num3 = MoonBrightnesByPhase[(int)(moonPhaseExact + 1.0) % MoonOrbitDays];
			return num2 * (1f - num) + num3 * num;
		}
	}

	public float MoonSize
	{
		get
		{
			double moonPhaseExact = MoonPhaseExact;
			float num = (float)moonPhaseExact - (float)(int)moonPhaseExact;
			float num2 = MoonSizeByPhase[(int)moonPhaseExact];
			float num3 = MoonSizeByPhase[(int)(moonPhaseExact + 1.0) % MoonOrbitDays];
			return (num2 * (1f - num) + num3 * num) * superMoonSize;
		}
	}

	public float SunsetMod { get; protected set; }

	public bool IsRunning => watchIngameTime.IsRunning;

	public SolarSphericalCoordsDelegate OnGetSolarSphericalCoords { get; set; }

	public HemisphereDelegate OnGetHemisphere { get; set; }

	public GetLatitudeDelegate OnGetLatitude { get; set; }

	public float? SeasonOverride { get; set; }

	public GameCalendar(IAsset sunlightTexture, int worldSeed, long totalSecondsStart = 4176000L)
	{
		OnGetSolarSphericalCoords = (double posX, double posZ, float yearRel, float dayRel) => new SolarSphericalCoords((float)Math.PI * 2f * GameMath.Mod(HourOfDay / HoursPerDay, 1f) - (float)Math.PI, 0f);
		OnGetLatitude = (double posZ) => 0.5;
		watchIngameTime = new Stopwatch();
		timespan = TimeSpan.FromSeconds(totalSecondsStart);
		timeSpeedModifiers["baseline"] = 60f;
		BitmapRef bitmapRef = BitmapCreateFromPng(sunlightTexture);
		sunLightTexture = bitmapRef.Pixels;
		sunLightTextureSize = new Size2i(bitmapRef.Width, bitmapRef.Height);
		bitmapRef.Dispose();
		sunsetModNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 0.9, worldSeed);
		superMoonNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 0.9, worldSeed + 12098);
	}

	public double GetMoonPhase(double totaldays)
	{
		double sunAscension = GetSunAscension(totaldays);
		double sinDelta;
		return ((double)(GameMath.Mod((float)(GetMoonAscensionDeclination(totaldays, out sinDelta) - sunAscension) / ((float)Math.PI * 2f), 1f) * (float)MoonOrbitDays) + 0.5) % (double)MoonOrbitDays;
	}

	public double GetSunAscension(double totaldays)
	{
		double num = GameMath.Mod(totaldays, DaysPerYear) / (double)DaysPerYear;
		double num2 = GameMath.Mod(totaldays, 1.0);
		return (GameMath.Mod(totaldays - num, 1.0) - (num2 - 0.5)) * 6.2831854820251465;
	}

	public SolarSphericalCoords GetCelestialAngles(double x, double z, double totalDays)
	{
		float yearRel = (float)(GameMath.Mod(totalDays, DaysPerYear) / (double)DaysPerYear);
		float dayRel = (float)GameMath.Mod(totalDays, 1.0);
		return OnGetSolarSphericalCoords(x, z, yearRel, dayRel);
	}

	public Vec3f GetSunPosition(Vec3d pos, double totalDays)
	{
		SolarSphericalCoords celestialAngles = GetCelestialAngles(pos.X, pos.Z, totalDays);
		float azimuthAngle = celestialAngles.AzimuthAngle;
		float zenithAngle = celestialAngles.ZenithAngle;
		float num = GameMath.Sin(zenithAngle);
		return new Vec3f(num * GameMath.Sin(azimuthAngle), GameMath.Cos(zenithAngle), num * GameMath.Cos(azimuthAngle));
	}

	public Vec3f GetMoonPosition(Vec3d position, double totalDays)
	{
		return GetMoonPosition(position.Z, totalDays);
	}

	protected Vec3f GetMoonPosition(double posZ, double totalDays)
	{
		double sinh;
		double moonCelestialAngles = GetMoonCelestialAngles(posZ, totalDays, out sinh);
		double num = Math.Sqrt(1.0 - sinh * sinh);
		return new Vec3f((float)(num * Math.Sin(moonCelestialAngles)), (float)sinh, (float)(num * Math.Cos(moonCelestialAngles)));
	}

	protected Vec3f GetMoonPosition(double posZ)
	{
		double sinh;
		double moonCelestialAnglesFromCache = GetMoonCelestialAnglesFromCache(posZ, out sinh);
		double num = Math.Sqrt(1.0 - sinh * sinh);
		return new Vec3f((float)(num * Math.Sin(moonCelestialAnglesFromCache)), (float)sinh, (float)(num * Math.Cos(moonCelestialAnglesFromCache)));
	}

	private double GetMoonAscensionDeclination(double totalDays, out double sinDelta)
	{
		double num = 1386.0 + (totalDays - 3.0) / (double)DaysPerYear;
		double num2 = GameMath.Mod(218.3164477 + 4812.6788123421 * num, 360.0);
		double num3 = GameMath.Mod(134.9634114 + 4771.988675605 * num, 360.0) * (Math.PI / 180.0);
		double num4 = GameMath.Mod(93.2720906 + 4832.020175273 * num, 360.0) * (Math.PI / 180.0);
		double num5 = (297.8501921 + 4452.671114034 * num) * (Math.PI / 180.0);
		double num6 = num2 + 6.289 * Math.Sin(num3) - 1.274 * Math.Sin(num3 - 2.0 * num5) + 0.658 * Math.Sin(2.0 * num5);
		double num7 = 5.128 * Math.Sin(num4) + 0.28 * Math.Sin(num4 + num3) - 0.28 * Math.Sin(num4 - num3);
		num6 *= Math.PI / 180.0;
		double num8 = num7 * (Math.PI / 180.0);
		double num9 = Math.Cos(num8);
		double num10 = Math.Sin(num8);
		double num11 = num9 * Math.Sin(num6);
		double x = num9 * Math.Cos(num6);
		double y = 0.9174771 * num11 - 0.3977885 * num10;
		sinDelta = 0.3977885 * num11 + 0.9174771 * num10;
		return Math.Atan2(y, x);
	}

	public double GetMoonCelestialAngles(double z, double totalDays, out double sinh)
	{
		double sinDelta;
		double moonAscensionDeclination = GetMoonAscensionDeclination(totalDays, out sinDelta);
		double num = Math.Sqrt(1.0 - sinDelta * sinDelta);
		double num2 = GameMath.Mod(totalDays - GameMath.Mod(totalDays, DaysPerYear) / (double)DaysPerYear, 1.0) * 6.2831854820251465 - moonAscensionDeclination;
		double num3 = OnGetLatitude(z) * 1.5707963705062866;
		double num4 = Math.Sin(num3);
		double num5 = Math.Cos(num3);
		double num6 = Math.Cos(num2);
		sinh = num4 * sinDelta + num5 * num * num6;
		return 0.0 - Math.Atan2(Math.Sin(num2), num4 * num6 - num5 * sinDelta / num);
	}

	protected double GetMoonCelestialAnglesFromCache(double z, out double sinh)
	{
		double num = moonSinDeltaCached;
		double num2 = Math.Sqrt(1.0 - num * num);
		double num3 = moonTauCached;
		double num4 = OnGetLatitude(z) * 1.5707963705062866;
		double num5 = Math.Sin(num4);
		double num6 = Math.Cos(num4);
		double num7 = Math.Cos(num3);
		sinh = num5 * num + num6 * num2 * num7;
		return 0.0 - Math.Atan2(Math.Sin(num3), num5 * num7 - num6 * num / num2);
	}

	protected void CacheMoonCelestialAngles()
	{
		double totalDays = TotalDays;
		double sinDelta;
		double moonAscensionDeclination = GetMoonAscensionDeclination(totalDays, out sinDelta);
		moonTauCached = GameMath.Mod(totalDays - GameMath.Mod(totalDays, DaysPerYear) / (double)DaysPerYear, 1.0) * 6.2831854820251465 - moonAscensionDeclination;
		moonSinDeltaCached = sinDelta;
	}

	public float RealSecondsToGameSeconds(float seconds)
	{
		return seconds * currentSpeedOfTime * CalendarSpeedMul;
	}

	public void SetSeasonOverride(float? seasonRel)
	{
		SeasonOverride = seasonRel;
	}

	public void SetTimeSpeedModifier(string name, float speed)
	{
		timeSpeedModifiers[name] = speed;
		CalculateCurrentTimeSpeed();
	}

	public void RemoveTimeSpeedModifier(string name)
	{
		timeSpeedModifiers.Remove(name);
		CalculateCurrentTimeSpeed();
	}

	private void CalculateCurrentTimeSpeed()
	{
		float num = 0f;
		foreach (float value in timeSpeedModifiers.Values)
		{
			num += value;
		}
		currentSpeedOfTime = num;
		DayLengthInRealLifeSeconds = ((currentSpeedOfTime == 0f) ? float.MaxValue : (3600f * HoursPerDay / currentSpeedOfTime / CalendarSpeedMul));
	}

	public void SetTotalSeconds(long totalSecondsNow, long totalSecondsStart)
	{
		timespan = TimeSpan.FromSeconds(totalSecondsNow);
		this.totalSecondsStart = totalSecondsStart;
	}

	public void Start()
	{
		if (!watchIngameTime.IsRunning)
		{
			watchIngameTime.Start();
		}
	}

	public void Stop()
	{
		if (watchIngameTime.IsRunning)
		{
			watchIngameTime.Stop();
		}
	}

	public virtual void Tick()
	{
		if (watchIngameTime.IsRunning)
		{
			double value = (double)watchIngameTime.ElapsedMilliseconds / 1000.0 * (double)SpeedOfTime * (double)CalendarSpeedMul;
			timespan += TimeSpan.FromSeconds(value);
			watchIngameTime.Restart();
			Update();
		}
	}

	public virtual void Update()
	{
		float num = Math.Max(0f, 1.15f - MoonSize);
		double num2 = superMoonNoise.Noise(0.0, TotalDays / 8.0);
		superMoonSize = (float)GameMath.Clamp((num2 - 0.74 - (double)num) * 50.0, 1.0, 2.5);
		moonPhaseCached = GetMoonPhase(TotalDays);
		CacheMoonCelestialAngles();
	}

	public void SetDayTime(float wantHourOfDay)
	{
		float hours = ((!(HourOfDay > wantHourOfDay)) ? (wantHourOfDay - HourOfDay) : (wantHourOfDay + (HoursPerDay - HourOfDay)));
		Add(hours);
	}

	public void SetMonth(float month)
	{
		float hours = ((!(Monthf > month)) ? ((month - Monthf) * HoursPerDay * (float)DaysPerMonth + 12f) : ((month + ((float)MonthsPerYear - Monthf)) * HoursPerDay * (float)DaysPerMonth + 12f));
		Add(hours);
	}

	public void Add(float hours)
	{
		TimeSpan ts = TimeSpan.FromHours(hours);
		timespan = timespan.Add(ts);
	}

	public float GetDayLightStrength(double x, double z)
	{
		double totalDays = TotalDays;
		SolarSphericalCoords celestialAngles = GetCelestialAngles(x, z, totalDays);
		float azimuthAngle = celestialAngles.AzimuthAngle;
		float zenithAngle = celestialAngles.ZenithAngle;
		float num = GameMath.Sin(zenithAngle);
		Vec3f vec3f = new Vec3f(num * GameMath.Sin(azimuthAngle), GameMath.Cos(zenithAngle), num * GameMath.Cos(azimuthAngle));
		Vec3f moonPosition = GetMoonPosition(z);
		float moonPhaseBrightness = MoonPhaseBrightness;
		float num2 = (GameMath.Clamp(vec3f.Y * 1.4f + 0.2f, -1f, 1f) + 1f) / 2f;
		float y = moonPosition.Y;
		float num3 = GameMath.Clamp(moonPhaseBrightness * (0.66f + 0.33f * y), -0.2f, moonPhaseBrightness);
		float num4 = GameMath.Clamp((vec3f.Dot(moonPosition) - 0.99955f) * 2500f, 0f, num2 * 0.6f);
		num2 = Math.Max(0f, num2 - num4);
		num3 = Math.Max(0f, num3 - num4);
		float val = GameMath.Lerp(0f, num3, GameMath.Clamp(y * 20f, 0f, 1f));
		float num5 = Math.Max(0f, (vec3f.Y - 0.4f) / 7.5f);
		Color sunlightPixelRel = getSunlightPixelRel(GameMath.Clamp(num2 + SunsetMod, 0f, 1f), 0.01f);
		return Math.Max(val, (float)(sunlightPixelRel.R + sunlightPixelRel.G + sunlightPixelRel.B) / 3f / 255f + num5);
	}

	public float GetDayLightStrength(BlockPos pos)
	{
		return GetDayLightStrength(pos.X, pos.Z);
	}

	public EnumSeason GetSeason(BlockPos pos)
	{
		float num = GameMath.Mod(GetSeasonRel(pos) - 0.21916668f, 1f);
		return (EnumSeason)(4f * num);
	}

	public float GetSeasonRel(BlockPos pos)
	{
		if (SeasonOverride.HasValue)
		{
			return SeasonOverride.Value;
		}
		if (GetHemisphere(pos) != EnumHemisphere.North)
		{
			return (YearRel + 0.5f) % 1f;
		}
		return YearRel;
	}

	public EnumHemisphere GetHemisphere(BlockPos pos)
	{
		if (OnGetHemisphere != null)
		{
			return OnGetHemisphere(pos.X, pos.Z);
		}
		return EnumHemisphere.North;
	}

	public Color getSunlightPixelRel(float relx, float rely)
	{
		float num = Math.Min(sunLightTextureSize.Width - 1, relx * (float)sunLightTextureSize.Width);
		int num2 = (int)num;
		int num3 = (int)Math.Min(sunLightTextureSize.Height - 1, rely * (float)sunLightTextureSize.Height);
		float lx = num - (float)num2;
		int left = sunLightTexture[num3 * sunLightTextureSize.Width + num2];
		int right = sunLightTexture[num3 * sunLightTextureSize.Width + Math.Min(sunLightTextureSize.Width - 1, num2 + 1)];
		return Color.FromArgb(GameMath.LerpRgbaColor(lx, left, right));
	}

	public Packet_Server ToPacket()
	{
		string[] array = new string[timeSpeedModifiers.Count];
		int[] array2 = new int[timeSpeedModifiers.Count];
		int num = 0;
		foreach (KeyValuePair<string, float> timeSpeedModifier in timeSpeedModifiers)
		{
			array[num] = timeSpeedModifier.Key;
			array2[num] = CollectibleNet.SerializeFloatPrecise(timeSpeedModifier.Value);
			num++;
		}
		Packet_ServerCalendar packet_ServerCalendar = new Packet_ServerCalendar
		{
			TotalSeconds = (long)timespan.TotalSeconds,
			TotalSecondsStart = totalSecondsStart,
			MoonOrbitDays = MoonOrbitDays,
			DaysPerMonth = DaysPerMonth,
			HoursPerDay = CollectibleNet.SerializeFloatVeryPrecise(HoursPerDay),
			CalendarSpeedMul = CollectibleNet.SerializeFloatVeryPrecise(calendarSpeedMul)
		};
		packet_ServerCalendar.SetTimeSpeedModifierNames(array);
		packet_ServerCalendar.SetTimeSpeedModifierSpeeds(array2);
		packet_ServerCalendar.Running = (watchIngameTime.IsRunning ? 1 : 0);
		return new Packet_Server
		{
			Id = 13,
			Calendar = packet_ServerCalendar
		};
	}

	public string PrettyDate()
	{
		float hourOfDay = HourOfDay;
		int num = (int)hourOfDay;
		int num2 = (int)((hourOfDay - (float)num) * 60f);
		return Lang.Get("dateformat", DayOfMonth, Lang.Get("month-" + MonthName), Year.ToString("0"), num.ToString("00"), num2.ToString("00"));
	}

	public void UpdateFromPacket(Packet_Server packet)
	{
		Packet_ServerCalendar calendar = packet.Calendar;
		SetTotalSeconds(calendar.TotalSeconds, calendar.TotalSecondsStart);
		timeSpeedModifiers.Clear();
		for (int i = 0; i < calendar.TimeSpeedModifierNamesCount; i++)
		{
			timeSpeedModifiers[calendar.TimeSpeedModifierNames[i]] = CollectibleNet.DeserializeFloatPrecise(calendar.TimeSpeedModifierSpeeds[i]);
		}
		MoonOrbitDays = calendar.MoonOrbitDays;
		HoursPerDay = CollectibleNet.DeserializeFloatVeryPrecise(calendar.HoursPerDay);
		calendarSpeedMul = CollectibleNet.DeserializeFloatVeryPrecise(calendar.CalendarSpeedMul);
		DaysPerMonth = calendar.DaysPerMonth;
		if (HoursPerDay == 0f)
		{
			throw new ArgumentException("Trying to set 0 hours per day.");
		}
		if (calendar.Running > 0)
		{
			if (!watchIngameTime.IsRunning)
			{
				watchIngameTime.Start();
			}
		}
		else if (watchIngameTime.IsRunning)
		{
			watchIngameTime.Stop();
		}
		CalculateCurrentTimeSpeed();
	}

	public BitmapRef BitmapCreateFromPng(IAsset asset)
	{
		return new BitmapExternal((Stream)new MemoryStream(asset.Data), (ILogger?)null);
	}
}
