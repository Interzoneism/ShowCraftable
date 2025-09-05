using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using AnimatedGif;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class WeatherSystemCommands : ModSystem
{
	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	private ICoreAPI api;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
	}

	public override void StartServerSide(ICoreServerAPI sapi)
	{
		this.sapi = sapi;
		sapi.ChatCommands.GetOrCreate("debug").BeginSubCommand("prectest").WithDescription("recipitation test export")
			.RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("pos")
			.RequiresPlayer()
			.HandleWith(CmdPrecTestServerPos)
			.EndSubCommand()
			.BeginSubCommand("here")
			.RequiresPlayer()
			.HandleWith(CmdPrecTestServerHere)
			.EndSubCommand()
			.BeginSubCommand("climate")
			.RequiresPlayer()
			.WithArgs(api.ChatCommands.Parsers.OptionalBool("climate"))
			.HandleWith(CmdPrecTestServerClimate)
			.EndSubCommand()
			.EndSubCommand();
		sapi.ChatCommands.GetOrCreate("debug").BeginSubCommand("snowaccum").WithDescription("Snow accum test")
			.RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("on")
			.HandleWith(CmdSnowAccumOn)
			.EndSubCommand()
			.BeginSubCommand("off")
			.HandleWith(CmdSnowAccumOff)
			.EndSubCommand()
			.BeginSubCommand("processhere")
			.RequiresPlayer()
			.HandleWith(CmdSnowAccumProcesshere)
			.EndSubCommand()
			.BeginSubCommand("info")
			.RequiresPlayer()
			.HandleWith(CmdSnowAccumInfo)
			.EndSubCommand()
			.BeginSubCommand("here")
			.RequiresPlayer()
			.WithArgs(api.ChatCommands.Parsers.OptionalFloat("amount"))
			.HandleWith(CmdSnowAccumHere)
			.EndSubCommand()
			.EndSubCommand();
		sapi.Event.ServerRunPhase(EnumServerRunPhase.GameReady, delegate
		{
			sapi.ChatCommands.Create("whenwillitstopraining").WithDescription("When does it finally stop to rain around here?!").RequiresPrivilege(Privilege.controlserver)
				.RequiresPlayer()
				.HandleWith(CmdWhenWillItStopRaining);
			WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
			sapi.ChatCommands.Create("weather").WithDescription("Show/Set current weather info").RequiresPrivilege(Privilege.controlserver)
				.HandleWith(CmdWeatherinfo)
				.BeginSubCommand("setprecip")
				.WithDescription("Running with no arguments returns the current precip. override, if one is set. Including an argument overrides the precipitation intensity and in turn also the rain cloud overlay. '-1' removes all rain clouds, '0' stops any rain but keeps some rain clouds, while '1' causes the heaviest rain and full rain clouds. The server will remain indefinitely in that rain state until reset with '/weather setprecipa'.")
				.RequiresPlayer()
				.WithArgs(api.ChatCommands.Parsers.OptionalFloat("level"))
				.HandleWith(CmdWeatherSetprecip)
				.EndSubCommand()
				.BeginSubCommand("setprecipa")
				.WithDescription("Resets the current precip override to auto mode.")
				.RequiresPlayer()
				.HandleWith(CmdWeatherSetprecipa)
				.EndSubCommand()
				.BeginSubCommand("cloudypos")
				.WithAlias("cyp")
				.RequiresPlayer()
				.WithArgs(api.ChatCommands.Parsers.OptionalFloat("level"))
				.HandleWith(CmdWeatherCloudypos)
				.EndSubCommand()
				.BeginSubCommand("stoprain")
				.WithDescription("Stops any current rain by forwarding to a time in the future where there is no rain.")
				.RequiresPlayer()
				.HandleWith(CmdWeatherStoprain)
				.EndSubCommand()
				.BeginSubCommand("acp")
				.WithDescription("Toggles auto-changing weather patterns.")
				.RequiresPlayer()
				.WithArgs(sapi.ChatCommands.Parsers.OptionalBool("mode"))
				.HandleWith(CmdWeatherAcp)
				.EndSubCommand()
				.BeginSubCommand("lp")
				.WithDescription("Lists all loaded weather patterns.")
				.RequiresPlayer()
				.HandleWith(CmdWeatherLp)
				.EndSubCommand()
				.BeginSubCommand("t")
				.WithDescription("Transitions to a random weather pattern.")
				.RequiresPlayer()
				.HandleWith(CmdWeatherT)
				.EndSubCommand()
				.BeginSubCommand("c")
				.WithDescription("Quickly transitions to a random weather pattern.")
				.RequiresPlayer()
				.HandleWith(CmdWeatherC)
				.EndSubCommand()
				.BeginSubCommand("setw")
				.WithDescription("Sets the current wind pattern to the given wind pattern.")
				.RequiresPlayer()
				.WithArgs(api.ChatCommands.Parsers.WordRange("windpattern", modSystem.WindConfigs.Select((WindPatternConfig w) => w.Code).ToArray()))
				.HandleWith(CmdWeatherSetw)
				.EndSubCommand()
				.BeginSubCommand("randomevent")
				.RequiresPlayer()
				.HandleWith(CmdWeatherRandomevent)
				.EndSubCommand()
				.BeginSubCommand("setev")
				.WithAlias("setevr")
				.WithDescription("setev - Sets a weather event globally.\n  setevr - Set a weather event only in the player's region.")
				.RequiresPlayer()
				.WithArgs(api.ChatCommands.Parsers.WordRange("weather_event", modSystem.WeatherEventConfigs.Select((WeatherEventConfig w) => w.Code).ToArray()), api.ChatCommands.Parsers.OptionalBool("allowStop"))
				.HandleWith(CmdWeatherSetev)
				.EndSubCommand()
				.BeginSubCommand("set")
				.WithAlias("seti")
				.RequiresPlayer()
				.WithArgs(api.ChatCommands.Parsers.WordRange("weatherpattern", modSystem.WeatherConfigs.Select((WeatherPatternConfig w) => w.Code).ToArray()))
				.HandleWith(CmdWeatherSet)
				.EndSubCommand()
				.BeginSubCommand("setirandom")
				.RequiresPlayer()
				.HandleWith(CmdWeatherSetirandom)
				.EndSubCommand()
				.BeginSubCommand("setir")
				.RequiresPlayer()
				.WithArgs(api.ChatCommands.Parsers.WordRange("weatherpattern", modSystem.WeatherConfigs.Select((WeatherPatternConfig w) => w.Code).ToArray()))
				.HandleWith(CmdWeatherSetir)
				.EndSubCommand();
		});
	}

	private TextCommandResult CmdWeatherinfo(TextCommandCallingArgs args)
	{
		return TextCommandResult.Success(GetWeatherInfo<WeatherSystemServer>(args.Caller.Player));
	}

	private TextCommandResult CmdWeatherSetir(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		modSystem.ReloadConfigs();
		string code = args.Parsers[0].GetValue() as string;
		BlockPos asBlockPos = (args.Caller.Player as IServerPlayer).Entity.SidedPos.XYZ.AsBlockPos;
		int regionX = asBlockPos.X / api.World.BlockAccessor.RegionSize;
		int regionZ = asBlockPos.Z / api.World.BlockAccessor.RegionSize;
		long key = modSystem.MapRegionIndex2D(regionX, regionZ);
		modSystem.weatherSimByMapRegion.TryGetValue(key, out var value);
		if (value == null)
		{
			return TextCommandResult.Success("Weather sim not loaded (yet) for this region");
		}
		if (value.SetWeatherPattern(code, updateInstant: true))
		{
			value.TickEvery25ms(0.025f);
			return TextCommandResult.Success("Ok weather pattern set for current region");
		}
		return TextCommandResult.Error("No such weather pattern found");
	}

	private TextCommandResult CmdWeatherSetirandom(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		modSystem.ReloadConfigs();
		bool flag = true;
		foreach (KeyValuePair<long, WeatherSimulationRegion> item in modSystem.weatherSimByMapRegion)
		{
			flag &= item.Value.SetWeatherPattern(item.Value.RandomWeatherPattern().config.Code, updateInstant: true);
			if (flag)
			{
				item.Value.TickEvery25ms(0.025f);
			}
		}
		if (flag)
		{
			return TextCommandResult.Success("Ok random weather pattern set");
		}
		return TextCommandResult.Error("No such weather pattern found");
	}

	private TextCommandResult CmdWeatherSet(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		string code = args.Parsers[0].GetValue() as string;
		modSystem.ReloadConfigs();
		bool flag = true;
		foreach (KeyValuePair<long, WeatherSimulationRegion> item in modSystem.weatherSimByMapRegion)
		{
			item.Value.ReloadPatterns(api.World.Seed);
			flag &= item.Value.SetWeatherPattern(code, updateInstant: true);
			if (flag)
			{
				item.Value.TickEvery25ms(0.025f);
			}
		}
		if (flag)
		{
			return TextCommandResult.Success("Ok weather pattern set for all loaded regions");
		}
		return TextCommandResult.Error("No such weather pattern found");
	}

	private TextCommandResult CmdWeatherSetev(TextCommandCallingArgs args)
	{
		string code = args.Parsers[0].GetValue() as string;
		bool allowStop = (bool)args.Parsers[1].GetValue();
		string subCmdCode = args.SubCmdCode;
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		modSystem.ReloadConfigs();
		BlockPos asBlockPos = (args.Caller.Player as IServerPlayer).Entity.SidedPos.XYZ.AsBlockPos;
		int regionX = asBlockPos.X / api.World.BlockAccessor.RegionSize;
		int regionZ = asBlockPos.Z / api.World.BlockAccessor.RegionSize;
		if (subCmdCode == "setevr")
		{
			long key = modSystem.MapRegionIndex2D(regionX, regionZ);
			modSystem.weatherSimByMapRegion.TryGetValue(key, out var value);
			if (value == null)
			{
				return TextCommandResult.Success("Weather sim not loaded (yet) for this region");
			}
			if (value.SetWeatherEvent(code, updateInstant: true))
			{
				value.CurWeatherEvent.AllowStop = allowStop;
				value.CurWeatherEvent.OnBeginUse();
				value.TickEvery25ms(0.025f);
				return TextCommandResult.Success("Ok weather event for this region set");
			}
			return TextCommandResult.Error("No such weather event found");
		}
		bool flag = true;
		foreach (KeyValuePair<long, WeatherSimulationRegion> item in modSystem.weatherSimByMapRegion)
		{
			flag &= item.Value.SetWeatherEvent(code, updateInstant: true);
			item.Value.CurWeatherEvent.AllowStop = allowStop;
			if (flag)
			{
				item.Value.CurWeatherEvent.OnBeginUse();
				item.Value.TickEvery25ms(0.025f);
			}
		}
		if (flag)
		{
			return TextCommandResult.Success("Ok weather event set for all loaded regions");
		}
		return TextCommandResult.Error("No such weather event found");
	}

	private TextCommandResult CmdWeatherRandomevent(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<long, WeatherSimulationRegion> item in sapi.ModLoader.GetModSystem<WeatherSystemServer>().weatherSimByMapRegion)
		{
			item.Value.selectRandomWeatherEvent();
			item.Value.sendWeatherUpdatePacket();
		}
		return TextCommandResult.Success("Random weather event selected for all regions");
	}

	private TextCommandResult CmdWeatherSetw(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		modSystem.ReloadConfigs();
		string code = args.Parsers[0].GetValue() as string;
		bool flag = true;
		foreach (KeyValuePair<long, WeatherSimulationRegion> item in modSystem.weatherSimByMapRegion)
		{
			item.Value.ReloadPatterns(api.World.Seed);
			flag &= item.Value.SetWindPattern(code, updateInstant: true);
			if (flag)
			{
				item.Value.TickEvery25ms(0.025f);
			}
		}
		if (flag)
		{
			return TextCommandResult.Success("Ok wind pattern set");
		}
		return TextCommandResult.Error("No such wind pattern found");
	}

	private TextCommandResult CmdWeatherC(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<long, WeatherSimulationRegion> item in sapi.ModLoader.GetModSystem<WeatherSystemServer>().weatherSimByMapRegion)
		{
			item.Value.TriggerTransition(1f);
		}
		return TextCommandResult.Success("Ok selected another weather pattern");
	}

	private TextCommandResult CmdWeatherT(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<long, WeatherSimulationRegion> item in sapi.ModLoader.GetModSystem<WeatherSystemServer>().weatherSimByMapRegion)
		{
			item.Value.TriggerTransition();
		}
		return TextCommandResult.Success("Ok transitioning to another weather pattern");
	}

	private TextCommandResult CmdWeatherLp(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		string text = string.Join(", ", modSystem.WeatherConfigs.Select((WeatherPatternConfig c) => c.Code));
		return TextCommandResult.Success("Patterns: " + text);
	}

	private TextCommandResult CmdWeatherAcp(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		if (args.Parsers[0].IsMissing)
		{
			modSystem.autoChangePatterns = !modSystem.autoChangePatterns;
		}
		else
		{
			modSystem.autoChangePatterns = (bool)args[0];
		}
		return TextCommandResult.Success("Ok autochange weather patterns now " + (modSystem.autoChangePatterns ? "on" : "off"));
	}

	private TextCommandResult CmdWeatherStoprain(TextCommandCallingArgs args)
	{
		TextCommandResult result = RainStopFunc(args.Caller.Player, skipForward: true);
		sapi.ModLoader.GetModSystem<WeatherSystemServer>().broadCastConfigUpdate();
		return result;
	}

	private TextCommandResult CmdWeatherCloudypos(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Cloud level rel = " + modSystem.CloudLevelRel);
		}
		modSystem.CloudLevelRel = (float)args.Parsers[0].GetValue();
		modSystem.serverChannel.BroadcastPacket(new WeatherCloudYposPacket
		{
			CloudYRel = modSystem.CloudLevelRel
		});
		return TextCommandResult.Success($"Cloud level rel {modSystem.CloudLevelRel:0.##} set. (y={(int)(modSystem.CloudLevelRel * (float)modSystem.api.World.BlockAccessor.MapSizeY)})");
	}

	private TextCommandResult CmdWeatherSetprecip(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		float num = (float)args.Parsers[0].GetValue();
		if (args.Parsers[0].IsMissing)
		{
			if (!modSystem.OverridePrecipitation.HasValue)
			{
				return TextCommandResult.Success("Currently no precipitation override active.");
			}
			return TextCommandResult.Success($"Override precipitation value is currently at {modSystem.OverridePrecipitation}.");
		}
		modSystem.OverridePrecipitation = num;
		modSystem.serverChannel.BroadcastPacket(new WeatherConfigPacket
		{
			OverridePrecipitation = modSystem.OverridePrecipitation,
			RainCloudDaysOffset = modSystem.RainCloudDaysOffset
		});
		return TextCommandResult.Success($"Ok precipitation set to {num}");
	}

	private TextCommandResult CmdWeatherSetprecipa(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		modSystem.OverridePrecipitation = null;
		modSystem.serverChannel.BroadcastPacket(new WeatherConfigPacket
		{
			OverridePrecipitation = modSystem.OverridePrecipitation,
			RainCloudDaysOffset = modSystem.RainCloudDaysOffset
		});
		return TextCommandResult.Success("Ok auto precipitation on");
	}

	private TextCommandResult CmdPrecTestServerClimate(TextCommandCallingArgs args)
	{
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		IPlayer player = args.Caller.Player;
		bool flag = (bool)args.Parsers[0].GetValue();
		WeatherSystemServer modSystem = api.ModLoader.GetModSystem<WeatherSystemServer>();
		EntityPos pos = player.Entity.Pos;
		int num = 400;
		float num2 = 4f;
		float num3 = 1f;
		float num4 = 2f;
		double num5 = api.World.Calendar.TotalDays;
		ClimateCondition climateAt = api.World.BlockAccessor.GetClimateAt(new BlockPos((int)pos.X, (int)pos.Y, (int)pos.Z), EnumGetClimateMode.WorldGenValues, num5);
		int num6 = num / 2;
		if (RuntimeEnv.OS != OS.Windows)
		{
			return TextCommandResult.Success("Command only supported on windows, try sub argument \"here\"");
		}
		Bitmap bitmap = new Bitmap(num, num);
		int[] array = new int[num * num];
		AnimatedGifCreator val = new AnimatedGifCreator("precip.gif", 100, -1);
		try
		{
			for (int i = 0; (float)i < num3 * 24f; i++)
			{
				if (flag)
				{
					for (int j = 0; j < num; j++)
					{
						for (int k = 0; k < num; k++)
						{
							climateAt.Rainfall = (float)i / (num3 * 24f);
							float rainCloudness = modSystem.GetRainCloudness(climateAt, pos.X + (double)((float)j * num4) - (double)num6, pos.Z + (double)((float)k * num4) - (double)num6, api.World.Calendar.TotalDays);
							int num7 = (int)GameMath.Clamp(255f * rainCloudness, 0f, 254f);
							array[k * num + j] = ColorUtil.ColorFromRgba(num7, num7, num7, 255);
						}
					}
				}
				else
				{
					for (int l = 0; l < num; l++)
					{
						for (int m = 0; m < num; m++)
						{
							float precipitation = modSystem.GetPrecipitation(pos.X + (double)((float)l * num4) - (double)num6, pos.Y, pos.Z + (double)((float)m * num4) - (double)num6, num5);
							int num8 = (int)GameMath.Clamp(255f * precipitation, 0f, 254f);
							array[m * num + l] = ColorUtil.ColorFromRgba(num8, num8, num8, 255);
						}
					}
				}
				num5 += (double)(num2 / 24f);
				bitmap.SetPixels(array);
				val.AddFrame((Image)bitmap, 100, (GifQuality)3);
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		return TextCommandResult.Success("Ok exported");
	}

	private TextCommandResult CmdPrecTestServerHere(TextCommandCallingArgs args)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		WeatherSystemServer modSystem = api.ModLoader.GetModSystem<WeatherSystemServer>();
		EntityPos pos = args.Caller.Player.Entity.Pos;
		double totalDays = api.World.Calendar.TotalDays;
		api.World.BlockAccessor.GetClimateAt(new BlockPos((int)pos.X, (int)pos.Y, (int)pos.Z), EnumGetClimateMode.WorldGenValues, totalDays);
		int num = 400;
		int num2 = num / 2;
		SKBitmap bmp = new SKBitmap(num, num, false);
		int[] array = new int[num * num];
		float num3 = 3f;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				float num4 = (float)i * num3 - (float)num2;
				float num5 = (float)j * num3 - (float)num2;
				if ((int)num4 == 0 && (int)num5 == 0)
				{
					array[j * num + i] = ColorUtil.ColorFromRgba(255, 0, 0, 255);
					continue;
				}
				float precipitation = modSystem.GetPrecipitation(pos.X + (double)num4, pos.Y, pos.Z + (double)num5, totalDays);
				int num6 = (int)GameMath.Clamp(255f * precipitation, 0f, 254f);
				array[j * num + i] = ColorUtil.ColorFromRgba(num6, num6, num6, 255);
			}
		}
		bmp.SetPixels(array);
		bmp.Save("preciphere.png");
		return TextCommandResult.Success("Ok exported");
	}

	private TextCommandResult CmdPrecTestServerPos(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = api.ModLoader.GetModSystem<WeatherSystemServer>();
		EntityPos pos = args.Caller.Player.Entity.Pos;
		return TextCommandResult.Success("Prec here: " + modSystem.GetPrecipitation(pos.X, pos.Y, pos.Z, api.World.Calendar.TotalDays));
	}

	private TextCommandResult CmdSnowAccumHere(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = api.ModLoader.GetModSystem<WeatherSystemServer>();
		float with = (float)args.Parsers[0].GetValue();
		BlockPos asBlockPos = args.Caller.Player.Entity.Pos.AsBlockPos;
		Vec2i vec2i = new Vec2i(asBlockPos.X / 32, asBlockPos.Z / 32);
		IServerMapChunk mapChunk = sapi.WorldManager.GetMapChunk(vec2i.X, vec2i.Y);
		int snowAccumResolution = WeatherSimulationRegion.snowAccumResolution;
		SnowAccumSnapshot snowAccumSnapshot = new SnowAccumSnapshot
		{
			SumTemperatureByRegionCorner = new FloatDataMap3D(snowAccumResolution, snowAccumResolution, snowAccumResolution),
			SnowAccumulationByRegionCorner = new FloatDataMap3D(snowAccumResolution, snowAccumResolution, snowAccumResolution)
		};
		snowAccumSnapshot.SnowAccumulationByRegionCorner.Data.Fill(with);
		UpdateSnowLayerChunk updateChunk = modSystem.snowSimSnowAccu.UpdateSnowLayer(snowAccumSnapshot, ignoreOldAccum: true, mapChunk, vec2i, null);
		modSystem.snowSimSnowAccu.accum = 1f;
		IBulkBlockAccessor blockAccessorBulkMinimalUpdate = sapi.World.GetBlockAccessorBulkMinimalUpdate(synchronize: true);
		blockAccessorBulkMinimalUpdate.UpdateSnowAccumMap = false;
		modSystem.snowSimSnowAccu.processBlockUpdates(mapChunk, updateChunk, blockAccessorBulkMinimalUpdate);
		blockAccessorBulkMinimalUpdate.Commit();
		return TextCommandResult.Success("Ok, test snow accum gen complete");
	}

	private TextCommandResult CmdSnowAccumInfo(TextCommandCallingArgs args)
	{
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		BlockPos asBlockPos = serverPlayer.Entity.Pos.AsBlockPos;
		Vec2i vec2i = new Vec2i(asBlockPos.X / 32, asBlockPos.Z / 32);
		double num = sapi.WorldManager.GetMapChunk(vec2i.X, vec2i.Y).GetModdata("lastSnowAccumUpdateTotalHours", 0.0);
		serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "lastSnowAccumUpdate: " + (api.World.Calendar.TotalHours - num) + " hours ago", EnumChatType.CommandSuccess);
		int regionX = (int)serverPlayer.Entity.Pos.X / sapi.World.BlockAccessor.RegionSize;
		int regionZ = (int)serverPlayer.Entity.Pos.Z / sapi.World.BlockAccessor.RegionSize;
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		long key = modSystem.MapRegionIndex2D(regionX, regionZ);
		modSystem.weatherSimByMapRegion.TryGetValue(key, out var value);
		int snowAccumResolution = WeatherSimulationRegion.snowAccumResolution;
		float[] data = new SnowAccumSnapshot
		{
			SnowAccumulationByRegionCorner = new FloatDataMap3D(snowAccumResolution, snowAccumResolution, snowAccumResolution)
		}.SnowAccumulationByRegionCorner.Data;
		float num2 = 3.5f;
		int length = value.SnowAccumSnapshots.Length;
		int num3 = value.SnowAccumSnapshots.EndPosition;
		while (length-- > 0)
		{
			SnowAccumSnapshot snowAccumSnapshot = value.SnowAccumSnapshots[num3];
			num3 = (num3 + 1) % value.SnowAccumSnapshots.Length;
			if (snowAccumSnapshot != null)
			{
				float[] data2 = snowAccumSnapshot.SnowAccumulationByRegionCorner.Data;
				for (int i = 0; i < data2.Length; i++)
				{
					data[i] = GameMath.Clamp(data[i] + data2[i], 0f - num2, num2);
				}
				num = Math.Max(num, snowAccumSnapshot.TotalHours);
			}
		}
		for (int j = 0; j < data.Length; j++)
		{
			serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, j + ": " + data[j], EnumChatType.CommandSuccess);
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdSnowAccumProcesshere(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = api.ModLoader.GetModSystem<WeatherSystemServer>();
		BlockPos asBlockPos = args.Caller.Player.Entity.Pos.AsBlockPos;
		Vec2i chunkCoord = new Vec2i(asBlockPos.X / 32, asBlockPos.Z / 32);
		modSystem.snowSimSnowAccu.AddToCheckQueue(chunkCoord);
		return TextCommandResult.Success("Ok, added to check queue");
	}

	private TextCommandResult CmdSnowAccumOff(TextCommandCallingArgs args)
	{
		api.ModLoader.GetModSystem<WeatherSystemServer>().snowSimSnowAccu.ProcessChunks = false;
		return TextCommandResult.Success("Snow accum process chunks off");
	}

	private TextCommandResult CmdSnowAccumOn(TextCommandCallingArgs args)
	{
		api.ModLoader.GetModSystem<WeatherSystemServer>().snowSimSnowAccu.ProcessChunks = true;
		return TextCommandResult.Success("Snow accum process chunks on");
	}

	private TextCommandResult CmdWhenWillItStopRaining(TextCommandCallingArgs args)
	{
		return RainStopFunc(args.Caller.Player);
	}

	private TextCommandResult RainStopFunc(IPlayer player, bool skipForward = false)
	{
		WeatherSystemServer modSystem = api.ModLoader.GetModSystem<WeatherSystemServer>();
		if (modSystem.OverridePrecipitation.HasValue)
		{
			return TextCommandResult.Success("Override precipitation set, rain pattern will not change. Fix by typing /weather setprecipa.");
		}
		Vec3d xYZ = player.Entity.Pos.XYZ;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		bool flag = false;
		for (; num < 21f; num += 1f / sapi.World.Calendar.HoursPerDay)
		{
			if (modSystem.GetPrecipitation(xYZ.X, xYZ.Y, xYZ.Z, sapi.World.Calendar.TotalDays + (double)num) < 0.04f)
			{
				if (!flag)
				{
					num3 = num;
				}
				flag = true;
				num2 += 1f / sapi.World.Calendar.HoursPerDay;
			}
			else if (flag)
			{
				break;
			}
		}
		if (num2 > 0f)
		{
			if (skipForward)
			{
				modSystem.RainCloudDaysOffset += num2;
				return TextCommandResult.Success($"Ok, forwarded rain simulation by {num3:0.##} days. The rain should stop for about {num2:0.##} days now", EnumChatType.CommandSuccess);
			}
			return TextCommandResult.Success($"In about {num3:0.##} days the rain should stop for about {num2:0.##} days");
		}
		return TextCommandResult.Success("No rain less days found for the next 3 in-game weeks :O");
	}

	public override void StartClientSide(ICoreClientAPI capi)
	{
		this.capi = capi;
		this.capi.ChatCommands.Create("weather").WithDescription("Show current weather info").HandleWith(CmdWeatherClient);
	}

	private TextCommandResult CmdWeatherClient(TextCommandCallingArgs textCommandCallingArgs)
	{
		return TextCommandResult.Success(GetWeatherInfo<WeatherSystemClient>(capi.World.Player));
	}

	private string GetWeatherInfo<T>(IPlayer player) where T : WeatherSystemBase
	{
		T modSystem = api.ModLoader.GetModSystem<T>();
		Vec3d xYZ = player.Entity.SidedPos.XYZ;
		BlockPos asBlockPos = xYZ.AsBlockPos;
		WeatherDataReaderPreLoad weatherDataReaderPreLoad = modSystem.getWeatherDataReaderPreLoad();
		weatherDataReaderPreLoad.LoadAdjacentSimsAndLerpValues(xYZ, 1f);
		int regionX = asBlockPos.X / api.World.BlockAccessor.RegionSize;
		int regionZ = asBlockPos.Z / api.World.BlockAccessor.RegionSize;
		long key = modSystem.MapRegionIndex2D(regionX, regionZ);
		modSystem.weatherSimByMapRegion.TryGetValue(key, out var value);
		if (value == null)
		{
			return "weatherSim is null. No idea what to do here";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Weather by region:");
		string[] array = new string[4] { "tl", "tr", "bl", "br" };
		double num = GameMath.BiLerp(1.0, 0.0, 0.0, 0.0, weatherDataReaderPreLoad.LerpLeftRight, weatherDataReaderPreLoad.LerpTopBot);
		double num2 = GameMath.BiLerp(0.0, 1.0, 0.0, 0.0, weatherDataReaderPreLoad.LerpLeftRight, weatherDataReaderPreLoad.LerpTopBot);
		double num3 = GameMath.BiLerp(0.0, 0.0, 1.0, 0.0, weatherDataReaderPreLoad.LerpLeftRight, weatherDataReaderPreLoad.LerpTopBot);
		double num4 = GameMath.BiLerp(0.0, 0.0, 0.0, 1.0, weatherDataReaderPreLoad.LerpLeftRight, weatherDataReaderPreLoad.LerpTopBot);
		int[] array2 = new int[4]
		{
			(int)(100.0 * num),
			(int)(100.0 * num2),
			(int)(100.0 * num3),
			(int)(100.0 * num4)
		};
		for (int i = 0; i < 4; i++)
		{
			WeatherSimulationRegion weatherSimulationRegion = weatherDataReaderPreLoad.AdjacentSims[i];
			if (weatherSimulationRegion == modSystem.dummySim)
			{
				stringBuilder.AppendLine($"{array[i]}: missing");
				continue;
			}
			string text = weatherSimulationRegion.OldWePattern.GetWeatherName();
			if (weatherSimulationRegion.Weight < 1f)
			{
				text = $"{weatherSimulationRegion.OldWePattern.GetWeatherName()} transitioning to {weatherSimulationRegion.NewWePattern.GetWeatherName()} ({(int)(100f * weatherSimulationRegion.Weight)}%)";
			}
			stringBuilder.AppendLine(string.Format("{0}: {1}% {2}. Wind: {3} (str={4}), Event: {5}", array[i], array2[i], text, weatherSimulationRegion.CurWindPattern.GetWindName(), weatherSimulationRegion.GetWindSpeed(asBlockPos.Y).ToString("0.###"), weatherSimulationRegion.CurWeatherEvent.config.Code));
		}
		ClimateCondition climateAt = api.World.BlockAccessor.GetClimateAt(player.Entity.Pos.AsBlockPos);
		stringBuilder.AppendLine($"Current precipitation: {(int)(climateAt.Rainfall * 100f)}%");
		stringBuilder.AppendLine($"Current wind: {GlobalConstants.CurrentWindSpeedClient}");
		return stringBuilder.ToString();
	}
}
