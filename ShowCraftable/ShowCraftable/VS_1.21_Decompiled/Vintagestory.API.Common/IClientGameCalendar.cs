using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IClientGameCalendar : IGameCalendar
{
	Vec3f SunPositionNormalized { get; }

	Vec3f SunPosition { get; }

	Vec3f MoonPosition { get; }

	Vec3f SunColor { get; }

	Vec3f ReflectColor { get; }

	float SunsetMod { get; }

	float DayLightStrength { get; }

	float MoonLightStrength { get; }

	float SunLightStrength { get; }

	bool Dusk { get; }
}
