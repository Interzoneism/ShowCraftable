using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

internal class CmdStats
{
	private ServerMain server;

	public CmdStats(ServerMain server)
	{
		this.server = server;
		server.api.commandapi.Create("stats").RequiresPrivilege(Privilege.controlserver).WithArgs(server.api.commandapi.Parsers.OptionalWord("compact"))
			.HandleWith(handleStats);
	}

	private TextCommandResult handleStats(TextCommandCallingArgs args)
	{
		string ending = (((string)args[0] == "compact") ? ";" : "\n");
		return TextCommandResult.Success(genStats(server, ending));
	}

	public static string genStats(ServerMain server, string ending)
	{
		StringBuilder stringBuilder = new StringBuilder();
		long num = server.totalUpTime.ElapsedMilliseconds / 1000;
		long num2 = server.totalUpTime.ElapsedMilliseconds / 1000;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		if (num2 > 60)
		{
			num3 = (int)(num2 / 60);
			num2 -= 60 * num3;
		}
		if (num3 > 60)
		{
			num4 = num3 / 60;
			num3 -= 60 * num4;
		}
		if (num4 > 24)
		{
			num5 = num4 / 24;
			num4 -= 24 * num5;
		}
		ICollection<ConnectedClient> values = server.Clients.Values;
		int num6 = values.Count((ConnectedClient x) => x.State != EnumClientState.Queued);
		if (num6 > 0)
		{
			server.lastDisconnectTotalMs = server.totalUpTime.ElapsedMilliseconds;
		}
		int num7 = Math.Max(0, (int)(num - server.lastDisconnectTotalMs / 1000));
		stringBuilder.Append("Version: 1.21.0");
		stringBuilder.Append(ending);
		stringBuilder.Append($"Uptime: {num5} days, {num4} hours, {num3} minutes, {num2} seconds");
		stringBuilder.Append(ending);
		stringBuilder.Append($"Players last online: {num7} seconds ago");
		stringBuilder.Append(ending);
		stringBuilder.Append("Players online: " + num6 + " / " + server.Config.MaxClients);
		if (num6 > 0 && num6 < 20)
		{
			stringBuilder.Append(" (");
			int num8 = 0;
			foreach (ConnectedClient item in values)
			{
				if (item.State != EnumClientState.Connecting && item.State != EnumClientState.Queued)
				{
					if (num8++ > 0)
					{
						stringBuilder.Append(", ");
					}
					stringBuilder.Append(item.PlayerName);
				}
			}
			stringBuilder.Append(")");
		}
		stringBuilder.Append(ending);
		if (server.Config.MaxClientsInQueue > 0)
		{
			stringBuilder.Append("Players in queue: " + server.ConnectionQueue.Count + " / " + server.Config.MaxClientsInQueue);
		}
		int num9 = 0;
		foreach (Entity value in server.LoadedEntities.Values)
		{
			if (value.State != EnumEntityState.Inactive)
			{
				num9++;
			}
		}
		stringBuilder.Append(ending);
		string text = decimal.Round((decimal)((float)GC.GetTotalMemory(forceFullCollection: false) / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);
		string text2 = decimal.Round((decimal)((float)Process.GetCurrentProcess().WorkingSet64 / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);
		stringBuilder.Append("Memory usage Managed/Total: " + text + "Mb / " + text2 + " Mb");
		stringBuilder.Append(ending);
		StatsCollection statsCollection = server.StatsCollector[GameMath.Mod(server.StatsCollectorIndex - 1, server.StatsCollector.Length)];
		double num10 = 2.0;
		if (statsCollection.ticksTotal > 0)
		{
			stringBuilder.Append("Last 2s Average Tick Time: " + decimal.Round((decimal)statsCollection.tickTimeTotal / (decimal)statsCollection.ticksTotal, 2) + " ms");
			stringBuilder.Append(ending);
			stringBuilder.Append("Last 2s Ticks/s: " + decimal.Round((decimal)((double)statsCollection.ticksTotal / num10), 2));
			stringBuilder.Append(ending);
			stringBuilder.Append("Last 10 ticks (ms): " + string.Join(", ", statsCollection.tickTimes));
		}
		stringBuilder.Append(ending);
		stringBuilder.Append("Loaded chunks: " + server.loadedChunks.Count);
		stringBuilder.Append(ending);
		stringBuilder.Append("Loaded entities: " + server.LoadedEntities.Count + " (" + num9 + " active)");
		stringBuilder.Append(ending);
		stringBuilder.Append("Network TCP: " + decimal.Round((decimal)((double)statsCollection.statTotalPackets / num10), 2) + " Packets/s or " + decimal.Round((decimal)((double)statsCollection.statTotalPacketsLength / num10 / 1024.0), 2, MidpointRounding.AwayFromZero) + " Kb/s");
		stringBuilder.Append(ending);
		stringBuilder.Append("Network UDP: " + decimal.Round((decimal)((double)statsCollection.statTotalUdpPackets / num10), 2) + " Packets/s or " + decimal.Round((decimal)((double)statsCollection.statTotalUdpPacketsLength / num10 / 1024.0), 2, MidpointRounding.AwayFromZero) + " Kb/s");
		return stringBuilder.ToString();
	}
}
