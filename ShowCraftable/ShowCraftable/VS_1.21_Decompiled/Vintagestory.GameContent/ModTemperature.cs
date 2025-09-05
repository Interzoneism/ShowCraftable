using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModTemperature : ModSystem
{
	private ICoreAPI api;

	public SimplexNoise YearlyTemperatureNoise;

	public SimplexNoise DailyTemperatureNoise;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
		api.Event.OnGetClimate += Event_OnGetClimate;
		YearlyTemperatureNoise = SimplexNoise.FromDefaultOctaves(3, 0.001, 0.95, api.World.Seed + 12109);
		DailyTemperatureNoise = SimplexNoise.FromDefaultOctaves(3, 1.0, 0.95, api.World.Seed + 128109);
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		api.ChatCommands.GetOrCreate("debug").BeginSubCommand("exptempplot").WithDescription("Export a 1 year long temperatures at a 6 hour interval at this location")
			.RequiresPrivilege(Privilege.controlserver)
			.RequiresPlayer()
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				exportPlotHere(args.Caller.Entity.Pos.AsBlockPos);
				return TextCommandResult.Success("ok exported");
			})
			.EndSubCommand();
	}

	private void Event_OnGetClimate(ref ClimateCondition climate, BlockPos pos, EnumGetClimateMode mode, double totalDays)
	{
		if (mode != EnumGetClimateMode.WorldGenValues)
		{
			double yearRel = totalDays / (double)api.World.Calendar.DaysPerYear % 1.0;
			double hourOfDay = totalDays % 1.0 * (double)api.World.Calendar.HoursPerDay;
			updateTemperature(ref climate, pos, yearRel, hourOfDay, totalDays);
		}
	}

	private void updateTemperature(ref ClimateCondition climate, BlockPos pos, double yearRel, double hourOfDay, double totalDays)
	{
		double num = climate.WorldGenTemperature;
		double num2 = api.World.Calendar.OnGetLatitude(pos.Z);
		double num3 = Math.Abs(num2) * 65.0;
		num -= num3 / 2.0;
		float? seasonOverride = api.World.Calendar.SeasonOverride;
		if (seasonOverride.HasValue)
		{
			double num4 = GameMath.Smootherstep(Math.Abs(GameMath.CyclicValueDistance(0.5f, seasonOverride.Value * 12f, 12f) / 6f));
			num += num3 * num4;
		}
		else if (num2 > 0.0)
		{
			double num5 = GameMath.Smootherstep(Math.Abs(GameMath.CyclicValueDistance(0.5, yearRel * 12.0, 12.0) / 6.0));
			num += num3 * num5;
		}
		else
		{
			double num6 = GameMath.Smootherstep(Math.Abs(GameMath.CyclicValueDistance(6.5, yearRel * 12.0, 12.0) / 6.0));
			num += num3 * num6;
		}
		double num7 = 18f - climate.Rainfall * 13f;
		double num8 = GameMath.SmoothStep(Math.Abs(GameMath.CyclicValueDistance(4.0, hourOfDay, 24.0) / 12.0));
		num += (num8 - 0.5) * num7;
		num += YearlyTemperatureNoise.Noise(totalDays, 0.0) * 3.0;
		num += DailyTemperatureNoise.Noise(totalDays, 0.0);
		climate.Temperature = (float)num;
	}

	private void exportPlotHere(BlockPos pos)
	{
		ClimateCondition climate = api.World.BlockAccessor.GetClimateAt(pos);
		double num = 0.0;
		double num2 = climate.Temperature;
		double num3 = api.World.Calendar.HoursPerDay;
		double num4 = api.World.Calendar.DaysPerYear;
		double num5 = api.World.Calendar.DaysPerMonth;
		double num6 = num4 / num5;
		List<string> list = new List<string>();
		for (double num7 = 0.0; num7 < 3456.0; num7 += 1.0)
		{
			climate.Temperature = (float)num2;
			double num8 = num / num3;
			double num9 = num8 / num4 % 1.0;
			double num10 = num % num3;
			double num11 = num9 * num6;
			updateTemperature(ref climate, pos, num9, num10, num8);
			list.Add($"{(int)(num8 % num5) + 1}.{(int)num11 + 1}.{(int)(num8 / num4 + 1386.0)} {(int)num10}:00" + ";" + climate.Temperature);
			num += 1.0;
		}
		File.WriteAllText("temperatureplot.csv", string.Join("\r\n", list));
	}
}
