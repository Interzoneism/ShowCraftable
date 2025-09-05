using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

internal class CmdTime
{
	private ServerMain server;

	public CmdTime(ServerMain server)
	{
		this.server = server;
		IChatCommandApi chatCommands = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		chatCommands.Create("time").RequiresPrivilege(Privilege.time).HandleWith(cmdGetTime)
			.WithDescription("Get or set world time or time speed")
			.BeginSub("stop")
			.WithDesc("Stop passage of time and time affected processes")
			.HandleWith(cmdStopTime)
			.EndSub()
			.BeginSub("resume")
			.WithDesc("Resume passage of time and time affected processes")
			.HandleWith(cmdResumeTime)
			.EndSub()
			.BeginSub("speed")
			.WithDesc("Get/Set speed of time passage. Not recommended for normal gameplay! If you want longer days, use /time calendarspeedmul")
			.WithArgs(parsers.OptionalFloat("speed", 60f))
			.HandleWith(cmdTimeSpeed)
			.EndSub()
			.BeginSub("set")
			.WithDesc("Fast forward to a time of day")
			.WithArgs(parsers.Word("24 hour format or word", new string[11]
			{
				"lunch", "day", "night", "latenight", "morning", "latemorning", "sunrise", "sunset", "afternoon", "midnight",
				"witchinghour"
			}))
			.HandleWith(cmdTimeSet)
			.EndSub()
			.BeginSub("setmonth")
			.WithDesc("Fast forward to a given month")
			.WithArgs(parsers.WordRange("month", "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"))
			.HandleWith(cmdTimeSetMonth)
			.EndSub()
			.BeginSub("add")
			.WithDesc("Fast forward by given time span")
			.WithArgs(parsers.Float("amount"), parsers.OptionalWordRange("span", "minute", "minutes", "hour", "hours", "day", "days", "month", "months", "year", "years"))
			.HandleWith(cmdTimeAdd)
			.EndSub()
			.BeginSub("calendarspeedmul")
			.WithAlias("csm")
			.WithDesc("Determines the relationship between in-game time and real-world time. A value of 1 means one in-game minute is 1 real world second. A value of 0.5 means one in-game minute is 2 real world second")
			.WithArgs(parsers.OptionalFloat("value", 0.5f))
			.HandleWith(cmdCalendarSpeedMul)
			.EndSub()
			.BeginSub("hoursperday")
			.WithDesc("Determines how many hours a day has.")
			.WithArgs(parsers.OptionalFloat("value", 24f))
			.HandleWith(cmdHoursPerDay)
			.EndSub();
		chatCommands.GetOrCreate("debug").BeginSub("time").BeginSub("nexteclipse")
			.HandleWith(handleCmdNextEclipse)
			.EndSub()
			.EndSub();
	}

	private TextCommandResult handleCmdNextEclipse(TextCommandCallingArgs args)
	{
		Vec3d pos = args.Caller.Pos;
		double num = -1.0;
		double num2 = -99.0;
		StringBuilder stringBuilder = new StringBuilder();
		double totalDays = server.GameWorldCalendar.TotalDays;
		for (double num3 = 0.0; num3 <= 500.0; num3 += 1.0 / 120.0)
		{
			double num4 = totalDays + num3;
			Vec3f vec3f = server.GameWorldCalendar.GetSunPosition(pos, num4).Normalize();
			Vec3f moonPosition = server.GameWorldCalendar.GetMoonPosition(pos, num4);
			float num5 = vec3f.Dot(moonPosition);
			if ((double)num5 < num)
			{
				if (num > 0.9996 && num4 - num2 > 1.0)
				{
					double num6 = (num4 - 1.0 / 120.0) % 1.0 * (double)server.GameWorldCalendar.HoursPerDay;
					if (num6 > 6.0 && num6 < 17.0)
					{
						stringBuilder.AppendLine($"Eclipse will happen in {num3 - 1.0 / 12.0:0} days, will get within {Math.Acos(num) * 57.2957763671875:0.#} degrees of the sun, at {(int)num6:00}:{(num6 - (double)(int)num6) * 60.0:00}.");
						num2 = num4;
						num = 0.0;
					}
				}
			}
			else
			{
				num = num5;
			}
		}
		if (stringBuilder.Length > 0)
		{
			return TextCommandResult.Success(stringBuilder.ToString());
		}
		return TextCommandResult.Success("No eclipse found in next 500 days");
	}

	private TextCommandResult cmdGetTime(TextCommandCallingArgs args)
	{
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-gettime", server.GameWorldCalendar.PrettyDate(), Math.Round(server.GameWorldCalendar.DayLengthInRealLifeSeconds / 60f, 1)));
	}

	private TextCommandResult cmdHoursPerDay(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-hoursperday", server.GameWorldCalendar.HoursPerDay));
		}
		float num = (float)args[0];
		if ((double)num < 0.1)
		{
			return TextCommandResult.Error("Cannot be less than 0.1");
		}
		server.GameWorldCalendar.HoursPerDay = num;
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-hoursperdayset", num));
	}

	private TextCommandResult cmdCalendarSpeedMul(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-calendarspeedmul", server.GameWorldCalendar.CalendarSpeedMul));
		}
		server.GameWorldCalendar.CalendarSpeedMul = (float)args[0];
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-calendarspeedmulset", server.GameWorldCalendar.CalendarSpeedMul, Math.Round(server.GameWorldCalendar.DayLengthInRealLifeSeconds / 60f, 1)));
	}

	private TextCommandResult cmdTimeAdd(TextCommandCallingArgs args)
	{
		float num = (float)args[0];
		string text = (string)args[1];
		if (num < 0f)
		{
			return TextCommandResult.Error("Only positive values are allowed");
		}
		if (args.Parsers[1].IsMissing)
		{
			text = "hour";
		}
		if (text.Last().Equals('s'))
		{
			text = text.Substring(0, text.Length - 1);
		}
		switch (text)
		{
		case "minute":
			server.GameWorldCalendar.Add(num / 60f);
			break;
		case "hour":
			server.GameWorldCalendar.Add(num);
			break;
		case "day":
			server.GameWorldCalendar.Add(num * server.GameWorldCalendar.HoursPerDay);
			break;
		case "month":
			server.GameWorldCalendar.Add(num * server.GameWorldCalendar.HoursPerDay * (float)server.GameWorldCalendar.DaysPerMonth);
			break;
		case "year":
			server.GameWorldCalendar.Add(num * server.GameWorldCalendar.HoursPerDay * (float)server.GameWorldCalendar.DaysPerYear);
			break;
		default:
			return TextCommandResult.Error("Invalid time span type");
		}
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-timeadd-" + text, num, server.GameWorldCalendar.PrettyDate()));
	}

	private TextCommandResult cmdTimeSetMonth(TextCommandCallingArgs args)
	{
		int num = args.Parsers[0].GetValidRange(args.RawArgs).IndexOf((string)args[0]);
		server.GameWorldCalendar.SetMonth(num);
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-timeset", server.GameWorldCalendar.PrettyDate()));
	}

	private TextCommandResult cmdTimeSet(TextCommandCallingArgs args)
	{
		float? num = null;
		string text = (string)args[0];
		switch (text)
		{
		case "lunch":
			num = 12f;
			break;
		case "day":
			num = 12f;
			break;
		case "night":
			num = 20f;
			break;
		case "latenight":
			num = 22f;
			break;
		case "morning":
			num = 8f;
			break;
		case "latemorning":
			num = 10f;
			break;
		case "sunrise":
			num = 6.5f;
			break;
		case "sunset":
			num = 17.5f;
			break;
		case "afternoon":
			num = 14f;
			break;
		case "midnight":
			num = 0f;
			break;
		case "witchinghour":
			num = 3f;
			break;
		}
		if (num.HasValue)
		{
			server.GameWorldCalendar.SetDayTime(num.Value / 24f * server.GameWorldCalendar.HoursPerDay);
			resendTimePacket();
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-timeset", server.GameWorldCalendar.PrettyDate()));
		}
		if (ParseTimeSpan(text, out var hours))
		{
			if (hours < 0f)
			{
				return TextCommandResult.Error(Lang.GetL(args.LanguageCode, "command-time-negativeerror"));
			}
			if (hours > server.GameWorldCalendar.HoursPerDay)
			{
				return TextCommandResult.Error(Lang.GetL(args.LanguageCode, "command-invalidtimeset", server.GameWorldCalendar.HoursPerDay));
			}
			server.GameWorldCalendar.SetDayTime(hours);
			resendTimePacket();
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-timeset", server.GameWorldCalendar.PrettyDate()));
		}
		return TextCommandResult.Error(Lang.GetL(args.LanguageCode, "command-time-invalidtimespan", text));
	}

	private TextCommandResult cmdTimeSpeed(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-time-speed", server.GameWorldCalendar.TimeSpeedModifiers["baseline"]));
		}
		float num = (float)args[0];
		server.GameWorldCalendar.SetTimeSpeedModifier("baseline", num);
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, (num == 0f) ? "command-time-speed0set" : "command-time-speedset", num, Math.Round(server.GameWorldCalendar.DayLengthInRealLifeSeconds / 60f, 1)));
	}

	private TextCommandResult cmdResumeTime(TextCommandCallingArgs args)
	{
		server.GameWorldCalendar.SetTimeSpeedModifier("baseline", 60f);
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-time-resumed"));
	}

	private TextCommandResult cmdStopTime(TextCommandCallingArgs args)
	{
		server.GameWorldCalendar.SetTimeSpeedModifier("baseline", 0f);
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-time-stopped"));
	}

	private bool ParseTimeSpan(string timespan, out float hours)
	{
		int result = 0;
		int result2;
		bool flag;
		if (timespan.Contains(":"))
		{
			string[] array = timespan.Split(':');
			flag = int.TryParse(array[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out result2);
			flag &= int.TryParse(array[1], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out result);
		}
		else
		{
			flag = int.TryParse(timespan, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out result2);
		}
		hours = (float)result2 + (float)result / 60f;
		return flag;
	}

	private void resendTimePacket()
	{
		server.lastUpdateSentToClient = -1000 * MagicNum.CalendarPacketSecondInterval;
	}
}
