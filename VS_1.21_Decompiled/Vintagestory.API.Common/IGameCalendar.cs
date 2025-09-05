using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IGameCalendar
{
	const int StartYear = 1386;

	HemisphereDelegate OnGetHemisphere { get; set; }

	SolarSphericalCoordsDelegate OnGetSolarSphericalCoords { get; set; }

	GetLatitudeDelegate OnGetLatitude { get; set; }

	float SpeedOfTime { get; }

	long ElapsedSeconds { get; }

	double ElapsedHours { get; }

	double ElapsedDays { get; }

	float CalendarSpeedMul { get; set; }

	float HoursPerDay { get; }

	int DaysPerYear { get; }

	int DaysPerMonth { get; }

	int Month { get; }

	EnumMonth MonthName { get; }

	int FullHourOfDay { get; }

	float HourOfDay { get; }

	double TotalHours { get; }

	double TotalDays { get; }

	int DayOfYear { get; }

	float DayOfYearf { get; }

	int Year { get; }

	float YearRel { get; }

	EnumMoonPhase MoonPhase { get; }

	double MoonPhaseExact { get; }

	float MoonPhaseBrightness { get; }

	float MoonSize { get; }

	float? SeasonOverride { get; set; }

	float Timelapse { get; set; }

	float GetDayLightStrength(double x, double z);

	float GetDayLightStrength(BlockPos pos);

	Vec3f GetSunPosition(Vec3d pos, double totalDays);

	Vec3f GetMoonPosition(Vec3d position, double totaldays);

	string PrettyDate();

	void SetTimeSpeedModifier(string name, float speed);

	void RemoveTimeSpeedModifier(string name);

	EnumSeason GetSeason(BlockPos pos);

	float GetSeasonRel(BlockPos pos);

	EnumHemisphere GetHemisphere(BlockPos pos);

	void Add(float hours);

	void SetSeasonOverride(float? seasonRel);
}
