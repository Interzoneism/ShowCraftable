using System;
using System.Drawing;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class ClientGameCalendar : GameCalendar, IClientGameCalendar, IGameCalendar
{
	public const long ClientCalendarStartingSeconds = 28000L;

	public Vec3f SunPosition = new Vec3f();

	public Vec3f MoonPosition = new Vec3f();

	public Vec3f SunPositionNormalized = new Vec3f();

	internal float dayLight;

	private Vec3f sunColor = new Vec3f();

	private Vec3f reflectColor = new Vec3f();

	private IClientWorldAccessor cworld;

	public static Vec3f nightColor = new Vec3f(0f, 0.0627451f, 2f / 15f);

	private double transitionDaysLast;

	public float DayLightStrength
	{
		get
		{
			return dayLight;
		}
		set
		{
			dayLight = value;
		}
	}

	public float MoonLightStrength { get; set; }

	public float SunLightStrength { get; set; }

	public Vec3f SunColor => sunColor;

	public Vec3f ReflectColor => reflectColor;

	public Color SunLightColor
	{
		get
		{
			float relx = (GameMath.Clamp(SunPositionNormalized.Y * 1.5f, -1f, 1f) + 1f) / 2f;
			return getSunlightPixelRel(relx, 0.01f);
		}
	}

	Vec3f IClientGameCalendar.SunPositionNormalized => SunPositionNormalized;

	Vec3f IClientGameCalendar.SunPosition => SunPosition;

	Vec3f IClientGameCalendar.MoonPosition => MoonPosition;

	internal ClientGameCalendar(IClientWorldAccessor cworld, IAsset sunlightTexture, int worldSeed, long totalSecondsStart = 28000L)
		: base(sunlightTexture, worldSeed, totalSecondsStart)
	{
		this.cworld = cworld;
	}

	public override void Update()
	{
		base.Update();
		Vec3d xYZ = cworld.Player.Entity.Pos.XYZ;
		SunPositionNormalized.Set(GetSunPosition(xYZ, base.TotalDays));
		SunPosition.Set(SunPositionNormalized).Mul(50f);
		Vec3f moonPosition = GetMoonPosition(xYZ.Z);
		MoonPosition.Set(moonPosition).Mul(50f);
		float num = (GameMath.Clamp(SunPositionNormalized.Y * 1.4f + 0.2f, -1f, 1f) + 1f) / 2f;
		float y = moonPosition.Y;
		float moonPhaseBrightness = base.MoonPhaseBrightness;
		float v = GameMath.Clamp(moonPhaseBrightness * (0.66f + 0.66f * y), -0.2f, moonPhaseBrightness);
		float num2 = Math.Max(0f, (SunPositionNormalized.Y - 0.4f) / 7.5f);
		MoonLightStrength = GameMath.Lerp(0f, v, GameMath.Clamp(y * 20f, 0f, 1f));
		SunLightStrength = (sunColor.R + sunColor.G + sunColor.B) / 3f + num2;
		DayLightStrength = Math.Max(MoonLightStrength, SunLightStrength);
		float num3 = GameMath.Clamp((SunPositionNormalized.Dot(moonPosition) - 0.99955f) * 2500f, 0f, DayLightStrength * 0.6f);
		DayLightStrength = Math.Max(0f, DayLightStrength - num3);
		DayLightStrength = Math.Min(1.5f, DayLightStrength + (float)Math.Max(0.0, (xYZ.Y - (double)cworld.SeaLevel - 1000.0) / 30000.0));
		double num4 = base.TotalDays + 1.0 / 48.0;
		float num5 = GameMath.Clamp(((float)sunsetModNoise.Noise(0.0, (int)num4) - 0.65f) / 1.8f, -0.1f, 0.3f);
		float num6 = GameMath.Clamp((float)((num4 - transitionDaysLast) * 6.0), 4.1666666E-05f, 1f);
		transitionDaysLast = num4;
		base.SunsetMod += (num5 - base.SunsetMod) * num6;
		Color sunlightPixelRel = getSunlightPixelRel(GameMath.Clamp(num + base.SunsetMod, 0f, 1f), 0.01f);
		sunColor.Set((float)(int)sunlightPixelRel.R / 255f, (float)(int)sunlightPixelRel.G / 255f, (float)(int)sunlightPixelRel.B / 255f);
		Color sunlightPixelRel2 = getSunlightPixelRel(GameMath.Clamp(num - base.SunsetMod, 0f, 1f), 0.01f);
		reflectColor.Set((float)(int)sunlightPixelRel2.R / 255f, (float)(int)sunlightPixelRel2.G / 255f, (float)(int)sunlightPixelRel2.B / 255f);
		if (SunPosition.Y < -0.1f)
		{
			float num7 = (0f - SunPosition.Y) / 10f - 0.3f;
			reflectColor.R = Math.Max(reflectColor.R - num7, nightColor[0]);
			reflectColor.G = Math.Max(reflectColor.G - num7, nightColor[1]);
			reflectColor.B = Math.Max(reflectColor.B - num7, nightColor[2]);
		}
	}
}
