namespace Vintagestory.API.Common;

public readonly struct DayTimeFrame
{
	public readonly double FromHour;

	public readonly double ToHour;

	public DayTimeFrame(double fromHour, double toHour)
	{
		FromHour = fromHour;
		ToHour = toHour;
	}

	public bool Matches(double hourOfDay)
	{
		if (FromHour <= hourOfDay)
		{
			return ToHour >= hourOfDay;
		}
		return false;
	}
}
