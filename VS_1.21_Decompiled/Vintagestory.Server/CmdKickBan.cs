using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

internal class CmdKickBan
{
	private ServerMain server;

	public CmdKickBan(ServerMain server)
	{
		this.server = server;
		IChatCommandApi chatCommands = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		chatCommands.Create("kick").RequiresPrivilege(Privilege.kick).WithDescription("Kicks a player from the server")
			.WithArgs(parsers.PlayerUids("player name"), parsers.OptionalAll("kick reason"))
			.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, (PlayerUidName plr, TextCommandCallingArgs textCommandCallingArgs) => Kick(textCommandCallingArgs.Caller, plr, (string)textCommandCallingArgs[1])))
			.Validate();
		chatCommands.Create("ban").RequiresPrivilege(Privilege.ban).WithDescription("Ban a player from the server")
			.WithArgs(parsers.PlayerUids("player name"), parsers.DateTime("duration"), parsers.All("reason"))
			.HandleWith((TextCommandCallingArgs cargs) => CmdPlayer.Each(cargs, (PlayerUidName plr, TextCommandCallingArgs args) => Ban(args.Caller, plr, (DateTime)args[1], (string)args[2])))
			.Validate();
		chatCommands.Create("unban").RequiresPrivilege(Privilege.ban).WithDescription("Remove a player ban")
			.WithArgs(parsers.PlayerUids("player name"))
			.HandleWith((TextCommandCallingArgs cargs) => CmdPlayer.Each(cargs, (PlayerUidName plr, TextCommandCallingArgs args) => UnBan(args.Caller, plr)))
			.Validate();
		chatCommands.Create("hardban").RequiresPrivilege(Privilege.ban).WithDescription("Ban a player forever without reason")
			.WithArgs(parsers.PlayerUids("player name"))
			.HandleWith((TextCommandCallingArgs cargs) => CmdPlayer.Each(cargs, (PlayerUidName plr, TextCommandCallingArgs args) => Ban(args.Caller, plr, DateTime.Now.AddYears(1000), "hard ban")))
			.Validate();
	}

	private TextCommandResult UnBan(Caller caller, PlayerUidName plr)
	{
		if (server.PlayerDataManager.UnbanPlayer(plr.Name, plr.Uid, caller.GetName()))
		{
			return TextCommandResult.Success(Lang.Get("Player is now unbanned"));
		}
		return TextCommandResult.Error(Lang.Get("Player was not banned"));
	}

	private TextCommandResult Ban(Caller caller, PlayerUidName targetPlayer, DateTime untilDate, string reason)
	{
		TextCommandResult textCommandResult = CanKickOrBanTarget(caller, targetPlayer.Name);
		if (textCommandResult.Status == EnumCommandStatus.Error)
		{
			return textCommandResult;
		}
		server.PlayerDataManager.BanPlayer(targetPlayer.Name, targetPlayer.Uid, caller.GetName(), reason, untilDate);
		ConnectedClient clientByUID = server.GetClientByUID(targetPlayer.Uid);
		if (clientByUID != null)
		{
			server.DisconnectPlayer(clientByUID, Lang.Get("cmdban-playerwasbanned", targetPlayer.Name, caller.GetName(), (reason.Length > 0) ? (", reason: " + reason) : ""), Lang.Get("cmdban-youvebeenbanned", caller.GetName(), (reason.Length > 0) ? (", reason: " + reason) : ""));
		}
		return TextCommandResult.Success(Lang.Get("cmdban-playerisnowbanned", untilDate));
	}

	private TextCommandResult Kick(Caller caller, PlayerUidName puidn, string reason = "")
	{
		IPlayer player = server.AllOnlinePlayers.FirstOrDefault((IPlayer plr) => plr.PlayerUID == puidn.Uid);
		if (player == null)
		{
			return TextCommandResult.Error("No such user online");
		}
		if (!server.Clients.TryGetValue(player.ClientId, out var value))
		{
			return TextCommandResult.Error(Lang.Get("No player with connectionid '{0}' exists", player.ClientId));
		}
		TextCommandResult textCommandResult = CanKickOrBanTarget(caller, player.PlayerName);
		if (textCommandResult.Status == EnumCommandStatus.Error)
		{
			return textCommandResult;
		}
		string playerName = value.PlayerName;
		string name = caller.GetName();
		if (reason == null)
		{
			reason = "";
		}
		string hisKickMessage = ((reason.Length == 0) ? Lang.Get("You've been kicked by {0}", name) : Lang.Get("You've been kicked by {0}, reason: {1}", name, reason));
		string text = ((reason.Length == 0) ? Lang.Get("{0} has been kicked by {1}", playerName, name) : Lang.Get("{0} has been kicked by {1}, reason: {2}", playerName, name, reason));
		server.DisconnectPlayer(value, text, hisKickMessage);
		ServerMain.Logger.Audit(string.Format("{0} kicks {1}. Reason: {2}", name, playerName, (reason.Length == 0) ? "none given" : reason));
		return TextCommandResult.Success(text);
	}

	protected TextCommandResult CanKickOrBanTarget(Caller caller, string targetPlayerName)
	{
		ServerPlayerData serverPlayerDataByLastKnownPlayername = server.PlayerDataManager.GetServerPlayerDataByLastKnownPlayername(targetPlayerName);
		if (serverPlayerDataByLastKnownPlayername == null)
		{
			return TextCommandResult.Success();
		}
		PlayerRole playerRole = serverPlayerDataByLastKnownPlayername.GetPlayerRole(server);
		if (playerRole == null)
		{
			return TextCommandResult.Success();
		}
		IPlayerRole role = caller.GetRole(server.api);
		if (playerRole.IsSuperior(role) || (playerRole.EqualLevel(role) && !caller.HasPrivilege(Privilege.root)))
		{
			return TextCommandResult.Error(Lang.Get("Can't kick or ban a player with a superior or equal group level"));
		}
		return TextCommandResult.Success();
	}
}
