using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

internal class CmdGlobalList
{
	private ServerMain server;

	public CmdGlobalList(ServerMain server)
	{
		this.server = server;
		server.api.commandapi.Create("list").RequiresPrivilege(Privilege.readlists).WithDesc("Show global lists (clients, banned, roles or privileges)")
			.BeginSub("clients")
			.WithAlias("c")
			.WithDesc("Players who are currently online")
			.HandleWith(listClients)
			.EndSub()
			.BeginSub("banned")
			.WithAlias("b")
			.WithDesc("Users who are banned from this server")
			.HandleWith(listBanned)
			.EndSub()
			.BeginSub("roles")
			.WithAlias("r")
			.WithDesc("Available roles")
			.HandleWith(listRoles)
			.EndSub()
			.BeginSub("privileges")
			.WithAlias("p")
			.WithDesc("Available privileges")
			.HandleWith(listPrivileges)
			.EndSub()
			.HandleWith(handleList);
	}

	private TextCommandResult listClients(TextCommandCallingArgs args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Lang.Get("List of online Players"));
		foreach (ConnectedClient client in server.Clients.Values)
		{
			if (client.State != EnumClientState.Connected && client.State != EnumClientState.Playing && client.State != EnumClientState.Queued)
			{
				continue;
			}
			if (client.State == EnumClientState.Queued)
			{
				int num = server.ConnectionQueue.FindIndex((QueuedClient c) => c.Client.Id == client.Id);
				if (num >= 0)
				{
					QueuedClient queuedClient = server.ConnectionQueue[num];
					stringBuilder.AppendLine($"[{client.Id}] {queuedClient.Identification.Playername} {client.Socket.RemoteEndPoint()} | Queue position: ({num + 1})");
				}
				else
				{
					ServerMain.Logger.Warning("Client {0} not found in connection queue", client.Id);
				}
			}
			else
			{
				stringBuilder.AppendLine($"[{client.Id}] {client.PlayerName} {client.Socket.RemoteEndPoint()}");
			}
		}
		return TextCommandResult.Success(stringBuilder.ToString());
	}

	private TextCommandResult listBanned(TextCommandCallingArgs args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Lang.Get("List of Banned Users:"));
		foreach (PlayerEntry bannedPlayer in server.PlayerDataManager.BannedPlayers)
		{
			string text = bannedPlayer.Reason;
			if (string.IsNullOrEmpty(text))
			{
				text = "";
			}
			if (bannedPlayer.UntilDate >= DateTime.Now)
			{
				stringBuilder.AppendLine($"{bannedPlayer.PlayerName} until {bannedPlayer.UntilDate}. Reason: {text}");
			}
		}
		return TextCommandResult.Success(stringBuilder.ToString());
	}

	private TextCommandResult listRoles(TextCommandCallingArgs args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Lang.Get("List of roles:"));
		foreach (PlayerRole role in server.Config.Roles)
		{
			stringBuilder.AppendLine(role.ToString());
		}
		return TextCommandResult.Success(stringBuilder.ToString());
	}

	private TextCommandResult listPrivileges(TextCommandCallingArgs args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Lang.Get("Available privileges:"));
		foreach (string allPrivilege in server.AllPrivileges)
		{
			stringBuilder.AppendLine(allPrivilege.ToString());
		}
		return TextCommandResult.Success(stringBuilder.ToString());
	}

	private TextCommandResult handleList(TextCommandCallingArgs args)
	{
		return TextCommandResult.Error("Syntax error, requires argument clients|banned|roles|privileges or c|b|r|p");
	}
}
