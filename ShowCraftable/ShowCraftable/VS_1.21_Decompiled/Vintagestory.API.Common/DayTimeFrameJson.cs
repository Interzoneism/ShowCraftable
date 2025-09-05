namespace Vintagestory.API.Common;

public class DayTimeFrameJson
{
	public double FromHour;

	public double ToHour;

	public DayTimeFrame ToStruct()
	{
		return new DayTimeFrame(FromHour, ToHour);
	}
}
