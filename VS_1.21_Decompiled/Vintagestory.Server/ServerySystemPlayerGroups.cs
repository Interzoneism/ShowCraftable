using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

public class ServerySystemPlayerGroups : ServerSystem
{
	public Dictionary<int, string> DisbandRequests = new Dictionary<int, string>();

	public Dictionary<string, string> InviteRequests = new Dictionary<string, string>();

	public Dictionary<int, PlayerGroup> PlayerGroupsByUid => server.PlayerDataManager.PlayerGroupsById;

	public ServerySystemPlayerGroups(ServerMain server)
		: base(server)
	{
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.ChatCommands.Create("group").WithDescription("Manage a player group").RequiresPrivilege(Privilege.controlplayergroups)
			.BeginSubCommand("create")
			.WithDescription("Creates a new group.")
			.WithExamples("Syntax: /group create [groupname]")
			.RequiresPlayer()
			.WithArgs(parsers.Word("groupName"))
			.HandleWith(CmdCreategroup)
			.EndSubCommand()
			.BeginSubCommand("disband")
			.WithDescription("Disband a group. Only the owner has the privilege to disband.")
			.WithExamples("Syntax: /group disband [groupname]")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalWord("groupName"))
			.HandleWith(CmdDisbandgroup)
			.EndSubCommand()
			.BeginSubCommand("confirmdisband")
			.WithDescription("Confirm disband a group. Only the owner has the privilege to disband.")
			.WithExamples("Syntax: /group confirmdisband [groupname]")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalWord("groupName"))
			.HandleWith(CmdConfirmDisbandgroup)
			.EndSubCommand()
			.BeginSubCommand("joinpolicy")
			.WithDescription("Define how users can join your group")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalWordRange("policy", "inviteonly", "everyone"))
			.HandleWith(CmdJoinPolicy)
			.EndSubCommand()
			.BeginSubCommand("join")
			.WithDescription("Join a group thats open for everyone")
			.RequiresPlayer()
			.WithArgs(parsers.Word("group name"))
			.HandleWith(CmdJoin)
			.EndSubCommand()
			.BeginSubCommand("rename")
			.WithDescription("Rename a group.")
			.WithExamples("Syntax: /group rename [oldname] [newname]", " Syntax in group chat: /group rename [newname]")
			.RequiresPlayer()
			.WithArgs(parsers.Word("groupName"), parsers.OptionalWord("newName"))
			.HandleWith(CmdRenamegroup)
			.EndSubCommand()
			.BeginSubCommand("invite")
			.WithDescription("Invite a player.")
			.WithExamples("Syntax: /group invite [groupname] [playername]")
			.RequiresPlayer()
			.WithArgs(parsers.Word("groupName"), parsers.OptionalWord("playerName"))
			.HandleWith(CmdInvitePlayer)
			.EndSubCommand()
			.BeginSubCommand("acceptinvite")
			.WithAlias("ai")
			.WithDescription("Accept an invitation to a group.")
			.WithExamples("Syntax: /group acceptinvite [groupname/groupid]")
			.WithArgs(parsers.Word("groupName/groupId"))
			.RequiresPlayer()
			.HandleWith(CmdAcceptInvite)
			.EndSubCommand()
			.BeginSubCommand("leave")
			.WithDescription("Leave a group.")
			.WithExamples("Syntax: /group leave [groupname]", "/group leave while in the groups chat room")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalWord("groupName"))
			.HandleWith(CmdLeavegroup)
			.EndSubCommand()
			.BeginSubCommand("list")
			.WithDescription("Lists the group you are in")
			.RequiresPlayer()
			.HandleWith(CmdListgroups)
			.EndSubCommand()
			.BeginSubCommand("info")
			.WithDescription("Show some info on a group.")
			.WithExamples("Syntax: /group info [groupname]")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalWord("groupName"))
			.HandleWith(CmdgroupInfo)
			.EndSubCommand()
			.BeginSubCommand("kick")
			.WithDescription("Kick a player from a group.")
			.WithExamples("Syntax: /group kick [groupname] (playername)", "/group kick (playername) while in the groups chat room")
			.RequiresPlayer()
			.WithArgs(parsers.Word("groupName"), parsers.OptionalWord("playerName"))
			.HandleWith(CmdKickFromgroup)
			.EndSubCommand()
			.BeginSubCommand("op")
			.WithDescription("Grant operator status to a player. Gives that player the ability to kick and invite players.")
			.WithExamples("Syntax: /group op [groupname] (playername)", "/group op (playername) while in the groups chat room")
			.RequiresPlayer()
			.WithArgs(parsers.Word("groupName"), parsers.OptionalWord("playerName"))
			.HandleWith((TextCommandCallingArgs args) => CmdOpPlayer(args, deop: false))
			.EndSubCommand()
			.BeginSubCommand("deop")
			.WithDescription("Revoke operator status from a player.")
			.WithExamples("Syntax: /group deop [groupname] (playername)", "/group deop (playername) while in the groups chat room")
			.RequiresPlayer()
			.WithArgs(parsers.Word("groupName"), parsers.OptionalWord("playerName"))
			.HandleWith((TextCommandCallingArgs args) => CmdOpPlayer(args, deop: true))
			.EndSubCommand();
		server.api.ChatCommands.Create("groupinvite").WithDescription("Enables or disables group invites to be sent to you").RequiresPrivilege(Privilege.chat)
			.RequiresPlayer()
			.WithArgs(parsers.Bool("enable"))
			.HandleWith(CmdNoInvite);
	}

	public override void OnPlayerJoinPost(ServerPlayer player)
	{
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, PlayerGroupMembership> playerGroupMemberShip in player.serverdata.PlayerGroupMemberShips)
		{
			if (playerGroupMemberShip.Value.Level == EnumPlayerGroupMemberShip.None)
			{
				continue;
			}
			server.PlayerDataManager.PlayerGroupsById.TryGetValue(playerGroupMemberShip.Key, out var value);
			if (value == null)
			{
				list.Add(playerGroupMemberShip.Key);
				server.SendMessage(player, GlobalConstants.ServerInfoChatGroup, "The player group " + playerGroupMemberShip.Value.GroupName + " you were a member of no longer exists. It probably has been disbanded", EnumChatType.Notification);
				continue;
			}
			server.PlayerDataManager.PlayerGroupsById[playerGroupMemberShip.Key].OnlinePlayers.Add(player);
			if (value.Name != playerGroupMemberShip.Value.GroupName)
			{
				server.SendMessage(player, GlobalConstants.ServerInfoChatGroup, "The player group " + playerGroupMemberShip.Value.GroupName + " you were a member of has been renamed to " + value.Name, EnumChatType.Notification);
				playerGroupMemberShip.Value.GroupName = value.Name;
			}
		}
		foreach (int item in list)
		{
			player.serverdata.PlayerGroupMemberShips.Remove(item);
			server.PlayerDataManager.playerDataDirty = true;
		}
		SendPlayerGroups(player);
	}

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		foreach (KeyValuePair<int, PlayerGroupMembership> playerGroupMemberShip in player.serverdata.PlayerGroupMemberShips)
		{
			if (playerGroupMemberShip.Value.Level != EnumPlayerGroupMemberShip.None && server.PlayerDataManager.PlayerGroupsById.TryGetValue(playerGroupMemberShip.Key, out var value))
			{
				value.OnlinePlayers.Remove(player);
			}
		}
	}

	private TextCommandResult Success(TextCommandCallingArgs args, string message, params string[] msgargs)
	{
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, message, msgargs));
	}

	private TextCommandResult Error(TextCommandCallingArgs args, string message, params string[] msgargs)
	{
		return TextCommandResult.Error(Lang.GetL(args.LanguageCode, message, msgargs));
	}

	private TextCommandResult CmdCreategroup(TextCommandCallingArgs args)
	{
		string playerUID = args.Caller.Player.PlayerUID;
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		string text = args[0] as string;
		if (!server.PlayerDataManager.CanCreatePlayerGroup(playerUID))
		{
			return Error(args, "No privilege to create groups.");
		}
		if (server.PlayerDataManager.GetPlayerGroupByName(text) != null)
		{
			return Error(args, "This group name already exists, please choose another name");
		}
		if (Regex.IsMatch(text, "[^" + GlobalConstants.AllowedChatGroupChars + "]+"))
		{
			return Error(args, "Invalid group name, may only use letters and numbers");
		}
		PlayerGroup playerGroup = new PlayerGroup
		{
			Name = text,
			OwnerUID = playerUID
		};
		server.PlayerDataManager.AddPlayerGroup(playerGroup);
		playerGroup.Md5Identifier = GameMath.Md5Hash(playerGroup.Uid + playerUID);
		server.PlayerDataManager.PlayerDataByUid[playerUID].JoinGroup(playerGroup, EnumPlayerGroupMemberShip.Owner);
		playerGroup.OnlinePlayers.Add(serverPlayer);
		SendPlayerGroup(serverPlayer, playerGroup);
		GotoGroup(serverPlayer, playerGroup.Uid);
		server.PlayerDataManager.playerDataDirty = true;
		server.PlayerDataManager.playerGroupsDirty = true;
		serverPlayer.SendMessage(playerGroup.Uid, Lang.GetL(serverPlayer.LanguageCode, "Group {0} created by {1}", args[0], serverPlayer.PlayerName), EnumChatType.CommandSuccess);
		return Success(args, "Group {0} created.", args[0] as string);
	}

	private int GetgroupId(string groupName)
	{
		foreach (PlayerGroup value in server.PlayerDataManager.PlayerGroupsById.Values)
		{
			if (value.Name.Equals(groupName, StringComparison.CurrentCultureIgnoreCase))
			{
				return value.Uid;
			}
		}
		return 0;
	}

	private TextCommandResult CmdDisbandgroup(TextCommandCallingArgs args)
	{
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		string playerUID = serverPlayer.PlayerUID;
		int num = args.Caller.FromChatGroupId;
		if (!args.Parsers[0].IsMissing)
		{
			num = GetgroupId(args[0] as string);
		}
		if (num <= 0)
		{
			return Error(args, "Invalid group name");
		}
		if (!HasPlayerPrivilege(serverPlayer, num, EnumPlayerGroupPrivilege.Disband))
		{
			return Error(args, "You must be the owner of the group to disband it.");
		}
		if (DisbandRequests.ContainsKey(num))
		{
			return Error(args, "Disband already requested, type /group confirmdisband [groupname] to confirm.");
		}
		DisbandRequests.Add(num, playerUID);
		return Success(args, "Really disband group {0}? Type /group confirmdisband [groupname] to confirm.", PlayerGroupsByUid[num].Name);
	}

	private TextCommandResult CmdConfirmDisbandgroup(TextCommandCallingArgs args)
	{
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		string playerUID = serverPlayer.PlayerUID;
		int num = args.Caller.FromChatGroupId;
		if (!args.Parsers[0].IsMissing)
		{
			num = GetgroupId(args[0] as string);
		}
		if (num <= 0)
		{
			return Error(args, "Invalid group name");
		}
		if (!HasPlayerPrivilege(serverPlayer, num, EnumPlayerGroupPrivilege.Disband))
		{
			return Error(args, "You must be the owner of the group to disband it.");
		}
		if (DisbandRequests.ContainsKey(num) && DisbandRequests[num] == playerUID)
		{
			PlayerGroup playerGroup = PlayerGroupsByUid[num];
			server.PlayerDataManager.RemovePlayerGroup(PlayerGroupsByUid[num]);
			server.PlayerDataManager.playerGroupsDirty = true;
			server.PlayerDataManager.playerDataDirty = true;
			foreach (IServerPlayer onlinePlayer in playerGroup.OnlinePlayers)
			{
				((ServerPlayer)onlinePlayer).serverdata.LeaveGroup(playerGroup);
				SendPlayerGroups(onlinePlayer);
				string l = Lang.GetL(onlinePlayer.LanguageCode, "Player group {0} has been disbanded by {1}", playerGroup.Name, serverPlayer.PlayerName);
				onlinePlayer.SendMessage((onlinePlayer.ClientId == serverPlayer.ClientId && args.Caller.FromChatGroupId != num) ? args.Caller.FromChatGroupId : GlobalConstants.ServerInfoChatGroup, l, EnumChatType.Notification);
			}
			return TextCommandResult.Success();
		}
		return Error(args, "Found no disband request to confirm, please use /group disband [groupname] first.");
	}

	private TextCommandResult CmdRenamegroup(TextCommandCallingArgs args)
	{
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		int num = args.Caller.FromChatGroupId;
		string text;
		if (!args.Parsers[1].IsMissing)
		{
			num = GetgroupId(args[0] as string);
			text = args[1] as string;
		}
		else
		{
			text = args[0] as string;
		}
		if (num <= 0)
		{
			return Error(args, "Invalid group name");
		}
		if (!HasPlayerPrivilege(serverPlayer, num, EnumPlayerGroupPrivilege.Rename))
		{
			return Error(args, "You must be the owner of the group to rename it.");
		}
		if (server.PlayerDataManager.GetPlayerGroupByName(text) != null)
		{
			return Error(args, "This group name already exists, please choose another name");
		}
		if (Regex.IsMatch(text, "[^" + GlobalConstants.AllowedChatGroupChars + "]+"))
		{
			return Error(args, "Invalid group name, may only use letters and numbers");
		}
		PlayerGroup playerGroup = PlayerGroupsByUid[num];
		string name = playerGroup.Name;
		playerGroup.Name = text;
		server.PlayerDataManager.playerGroupsDirty = true;
		foreach (IServerPlayer onlinePlayer in playerGroup.OnlinePlayers)
		{
			SendPlayerGroup(onlinePlayer, playerGroup);
			server.Clients[serverPlayer.ClientId].ServerData.PlayerGroupMemberShips[playerGroup.Uid].GroupName = playerGroup.Name;
			onlinePlayer.SendMessage(num, Lang.GetL(onlinePlayer.LanguageCode, "Player group has been renamed from {0} to {1}", name, playerGroup.Name), EnumChatType.Notification);
		}
		return Success(args, "Player group renamed");
	}

	private TextCommandResult CmdJoin(TextCommandCallingArgs args)
	{
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		string groupname = args[0] as string;
		PlayerGroup value = PlayerGroupsByUid.FirstOrDefault((KeyValuePair<int, PlayerGroup> g) => g.Value.JoinPolicy == "everyone" && g.Value.Name == groupname).Value;
		if (value == null)
		{
			PlayerGroupsByUid.TryGetValue(groupname.ToInt(), out value);
		}
		if (value == null || value.JoinPolicy != "everyone")
		{
			return Error(args, "No such group found or the invite policy is invite only");
		}
		int uid = value.Uid;
		PlayerGroupMembership membership = ((ServerPlayer)serverPlayer).serverdata.JoinGroup(value, EnumPlayerGroupMemberShip.Member);
		server.PlayerDataManager.playerDataDirty = true;
		PlayerGroupsByUid[uid].OnlinePlayers.Add(serverPlayer);
		SendPlayerGroup(serverPlayer, PlayerGroupsByUid[uid], membership);
		GotoGroup(serverPlayer, uid);
		foreach (IServerPlayer onlinePlayer in PlayerGroupsByUid[uid].OnlinePlayers)
		{
			onlinePlayer.SendMessage(uid, Lang.Get("Player {0} has joined the group.", serverPlayer.PlayerName), EnumChatType.Notification);
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdJoinPolicy(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		int fromChatGroupId = args.Caller.FromChatGroupId;
		if (fromChatGroupId <= 0)
		{
			return Error(args, "Must write the command inside the chat group you wish to modify");
		}
		if (args.Parsers[0].IsMissing)
		{
			return Success(args, "Join policy of this group is: {0}.", Lang.Get("plrgroup-invitepolicy-" + (PlayerGroupsByUid[fromChatGroupId].JoinPolicy ?? "inviteonly")));
		}
		string text = args[0] as string;
		if (!HasPlayerPrivilege(player, fromChatGroupId, EnumPlayerGroupPrivilege.Rename))
		{
			return Error(args, "You must be the owner of the group to rename it.");
		}
		PlayerGroupsByUid[fromChatGroupId].JoinPolicy = text;
		return Success(args, "Join policy {0} set.", text);
	}

	private TextCommandResult CmdInvitePlayer(TextCommandCallingArgs args)
	{
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		int num = args.Caller.FromChatGroupId;
		string text;
		if (!args.Parsers[1].IsMissing)
		{
			num = GetgroupId(args[0] as string);
			text = args[1] as string;
		}
		else
		{
			text = args[0] as string;
		}
		if (num <= 0)
		{
			return Error(args, "Invalid group name");
		}
		if (!HasPlayerPrivilege(serverPlayer, num, EnumPlayerGroupPrivilege.Invite))
		{
			return Error(args, "You must be the op or owner of the group to invite players.");
		}
		ConnectedClient clientByPlayername = server.GetClientByPlayername(text);
		if (clientByPlayername == null || clientByPlayername.Player == null)
		{
			return Error(args, "Can't invite. Player name {0} does not exist or is not online", text);
		}
		if (!server.PlayerDataManager.PlayerDataByUid[clientByPlayername.ServerData.PlayerUID].AllowInvite)
		{
			return Error(args, "Can't invite. Player name {0} has disabled group invites", text);
		}
		if (clientByPlayername.ServerData.PlayerGroupMemberShips.ContainsKey(num))
		{
			return Error(args, "Can't invite. Player name {0} already in this player group!", text);
		}
		InviteRequests[num + "-" + clientByPlayername.ServerData.PlayerUID] = clientByPlayername.ServerData.PlayerUID;
		string text2 = "/group ai " + PlayerGroupsByUid[num].Uid;
		string l = Lang.GetL(clientByPlayername.Player.LanguageCode, "playergroup-invitemsg", serverPlayer.PlayerName, PlayerGroupsByUid[num].Name, text2, text2);
		clientByPlayername.Player.SendMessage(GlobalConstants.GeneralChatGroup, l, EnumChatType.GroupInvite);
		return Success(args, "Player name {0} invited.", text);
	}

	private TextCommandResult CmdAcceptInvite(TextCommandCallingArgs args)
	{
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		string playerUID = serverPlayer.PlayerUID;
		string text = args[0] as string;
		if (!int.TryParse(text, out var result))
		{
			PlayerGroup playerGroupByName = server.PlayerDataManager.GetPlayerGroupByName(text);
			if (playerGroupByName == null)
			{
				return Error(args, "Invalid param (not a number and no such group name exists), use /group help ai to see available params.");
			}
			result = playerGroupByName.Uid;
		}
		if (InviteRequests.ContainsKey(result + "-" + playerUID))
		{
			server.PlayerDataManager.PlayerGroupsById.TryGetValue(result, out var value);
			if (value == null)
			{
				return Error(args, "Player group no longer exists.");
			}
			ServerPlayerData serverdata = ((ServerPlayer)serverPlayer).serverdata;
			if (serverdata.PlayerGroupMemberShips.ContainsKey(result))
			{
				serverPlayer.SendMessage(args.Caller.FromChatGroupId, Lang.GetL(serverPlayer.LanguageCode, "Can't accept invite, you are already joined this player group"), EnumChatType.CommandError);
			}
			else
			{
				PlayerGroupMembership membership = serverdata.JoinGroup(value, EnumPlayerGroupMemberShip.Member);
				server.PlayerDataManager.playerDataDirty = true;
				PlayerGroupsByUid[result].OnlinePlayers.Add(serverPlayer);
				SendPlayerGroup(serverPlayer, PlayerGroupsByUid[result], membership);
				GotoGroup(serverPlayer, result);
				foreach (IServerPlayer onlinePlayer in PlayerGroupsByUid[result].OnlinePlayers)
				{
					onlinePlayer.SendMessage(result, Lang.GetL(onlinePlayer.LanguageCode, "Player {0} has joined the group.", serverPlayer.PlayerName), EnumChatType.Notification);
				}
			}
		}
		else
		{
			serverPlayer.SendMessage(args.Caller.FromChatGroupId, Lang.GetL(serverPlayer.LanguageCode, "No invite for this player group found."), EnumChatType.CommandError);
		}
		return TextCommandResult.Success();
	}

	private void GotoGroup(IServerPlayer player, int groupId)
	{
		server.SendPacket(player, new Packet_Server
		{
			Id = 57,
			GotoGroup = new Packet_GotoGroup
			{
				GroupId = groupId
			}
		});
	}

	private TextCommandResult CmdLeavegroup(TextCommandCallingArgs args)
	{
		int num = args.Caller.FromChatGroupId;
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		string groupName = args[0] as string;
		if (!args.Parsers[0].IsMissing)
		{
			num = GetgroupId(groupName);
		}
		if (num <= 0)
		{
			return Error(args, "Invalid group name");
		}
		ServerPlayerData serverdata = ((ServerPlayer)serverPlayer).serverdata;
		if (!serverdata.PlayerGroupMemberShips.ContainsKey(num))
		{
			return Error(args, "No such group membership found, perhaps you already left this group.");
		}
		if (PlayerGroupsByUid.ContainsKey(num))
		{
			PlayerGroup playerGroup = PlayerGroupsByUid[num];
			serverPlayer.SendMessage((args.Caller.FromChatGroupId == num) ? GlobalConstants.ServerInfoChatGroup : args.Caller.FromChatGroupId, Lang.GetL(serverPlayer.LanguageCode, "You have left the group {0}", playerGroup.Name), EnumChatType.CommandSuccess);
			serverdata.LeaveGroup(playerGroup);
			server.PlayerDataManager.playerDataDirty = true;
			SendPlayerGroups(serverPlayer);
			playerGroup.OnlinePlayers.Remove(serverPlayer);
			server.SendMessageToGroup(args.Caller.FromChatGroupId, Lang.Get("Player {0} has left the group.", serverPlayer.PlayerName), EnumChatType.Notification);
		}
		else
		{
			serverdata.LeaveGroup(num);
			serverPlayer.SendMessage((args.Caller.FromChatGroupId == num) ? GlobalConstants.ServerInfoChatGroup : args.Caller.FromChatGroupId, Lang.GetL(serverPlayer.LanguageCode, "You have left the group."), EnumChatType.CommandSuccess);
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdListgroups(TextCommandCallingArgs args)
	{
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		Dictionary<int, PlayerGroupMembership> playerGroupMemberShips = ((ServerPlayer)serverPlayer).serverdata.PlayerGroupMemberShips;
		serverPlayer.SendMessage(args.Caller.FromChatGroupId, Lang.GetL(serverPlayer.LanguageCode, "You are in the following groups: "), EnumChatType.Notification);
		foreach (KeyValuePair<int, PlayerGroupMembership> item in playerGroupMemberShips)
		{
			string message = Lang.GetL(serverPlayer.LanguageCode, "Disbanded group name {0}", item.Value.GroupName);
			if (PlayerGroupsByUid.ContainsKey(item.Key))
			{
				message = PlayerGroupsByUid[item.Key].Name;
			}
			serverPlayer.SendMessage(args.Caller.FromChatGroupId, message, EnumChatType.Notification);
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdgroupInfo(TextCommandCallingArgs args)
	{
		PlayerGroup value;
		if (!args.Parsers[0].IsMissing)
		{
			value = server.PlayerDataManager.GetPlayerGroupByName(args[0] as string);
			if (value == null)
			{
				return Error(args, "No such group exists.");
			}
		}
		else
		{
			if (GlobalConstants.DefaultChatGroups.Contains(args.Caller.FromChatGroupId))
			{
				return Error(args, "This is a default group.");
			}
			if (!server.PlayerDataManager.PlayerGroupsById.TryGetValue(args.Caller.FromChatGroupId, out value))
			{
				return Error(args, "No such group exists.");
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Lang.GetL(args.LanguageCode, "Created: {0}", value.CreatedDate));
		stringBuilder.AppendLine(Lang.GetL(args.LanguageCode, "Created by: {0}", server.PlayerDataManager.PlayerDataByUid[value.OwnerUID].LastKnownPlayername));
		stringBuilder.Append(Lang.GetL(args.LanguageCode, "Members: "));
		int num = 0;
		foreach (ServerPlayerData value2 in server.PlayerDataManager.PlayerDataByUid.Values)
		{
			if (value2.PlayerGroupMemberships.ContainsKey(value.Uid))
			{
				if (num > 0)
				{
					stringBuilder.Append(", ");
				}
				num++;
				stringBuilder.Append(value2.LastKnownPlayername);
			}
		}
		stringBuilder.AppendLine();
		return TextCommandResult.Success(stringBuilder.ToString());
	}

	private TextCommandResult CmdKickFromgroup(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		int num = args.Caller.FromChatGroupId;
		string a;
		if (!args.Parsers[1].IsMissing)
		{
			num = GetgroupId(args[0] as string);
			a = args[1] as string;
		}
		else
		{
			a = args[0] as string;
		}
		if (num <= 0)
		{
			return Error(args, "Invalid group name");
		}
		PlayerGroup playerGroup = PlayerGroupsByUid[num];
		foreach (ServerPlayerData value2 in server.PlayerDataManager.PlayerDataByUid.Values)
		{
			if (!string.Equals(a, value2.LastKnownPlayername, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			foreach (KeyValuePair<int, PlayerGroupMembership> playerGroupMemberShip in value2.PlayerGroupMemberShips)
			{
				if (playerGroupMemberShip.Key != num)
				{
					continue;
				}
				if (!HasPlayerPrivilege(player, num, EnumPlayerGroupPrivilege.Kick, value2.PlayerUID))
				{
					return Error(args, "You must be the op or owner to kick this player (and ops can only be kicked by owner).");
				}
				value2.LeaveGroup(playerGroup);
				server.PlayerDataManager.playerDataDirty = true;
				if (server.PlayersByUid.TryGetValue(value2.PlayerUID, out var value))
				{
					PlayerGroupsByUid[num].OnlinePlayers.Remove(value);
				}
				server.SendMessageToGroup(args.Caller.FromChatGroupId, Lang.GetL(args.LanguageCode, "Player {0} has been removed from the player group.", value2.LastKnownPlayername), EnumChatType.CommandSuccess);
				foreach (ConnectedClient value3 in server.Clients.Values)
				{
					if (value3.WorldData.PlayerUID == value2.PlayerUID && value3.Player != null)
					{
						SendPlayerGroups(value3.Player);
						value3.Player.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(value3.Player.LanguageCode, "You've been kicked from player group {0}.", playerGroup.Name), EnumChatType.Notification);
						break;
					}
				}
				return TextCommandResult.Success();
			}
			return Error(args, "This player is not in this group.");
		}
		return Success(args, "No such player name found");
	}

	private TextCommandResult CmdOpPlayer(TextCommandCallingArgs args, bool deop)
	{
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		int num = args.Caller.FromChatGroupId;
		string a;
		if (!args.Parsers[1].IsMissing)
		{
			num = GetgroupId(args[0] as string);
			a = args[1] as string;
		}
		else
		{
			a = args[0] as string;
		}
		if (num <= 0)
		{
			return Error(args, "Invalid group name");
		}
		if (!HasPlayerPrivilege(serverPlayer, num, EnumPlayerGroupPrivilege.Op))
		{
			return Error(args, "You must be the owner to op/deop players");
		}
		PlayerGroup playerGroup = PlayerGroupsByUid[num];
		foreach (ServerPlayerData value in server.PlayerDataManager.PlayerDataByUid.Values)
		{
			if (!string.Equals(a, value.LastKnownPlayername, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			EnumPlayerGroupMemberShip level = GetGroupMemberShip(value.PlayerUID, num).Level;
			if (level == EnumPlayerGroupMemberShip.None)
			{
				return Error(args, "This player is not in this group, invite him first.");
			}
			if (!deop && (level == EnumPlayerGroupMemberShip.Op || level == EnumPlayerGroupMemberShip.Owner))
			{
				return Error(args, "This player is already op in this channel.");
			}
			if (deop && level != EnumPlayerGroupMemberShip.Op)
			{
				return Error(args, "This player is no op in this channel.");
			}
			value.PlayerGroupMemberShips[num].Level = (deop ? EnumPlayerGroupMemberShip.Member : EnumPlayerGroupMemberShip.Op);
			server.PlayerDataManager.playerDataDirty = true;
			foreach (ServerPlayer onlinePlayer in playerGroup.OnlinePlayers)
			{
				if (onlinePlayer.WorldData.PlayerUID == value.PlayerUID)
				{
					string l = Lang.GetL(onlinePlayer.LanguageCode, "{0} has given you op status. You can now invite and kick group members.", serverPlayer.PlayerName);
					if (deop)
					{
						l = Lang.GetL(onlinePlayer.LanguageCode, "{0} has removed your op status. You can no longer invite or kick members", serverPlayer.PlayerName);
					}
					onlinePlayer.SendMessage(num, l, EnumChatType.Notification);
				}
				else
				{
					string l2 = Lang.GetL(onlinePlayer.LanguageCode, deop ? "Player {0} has been deopped." : "Player {0} has been opped.", value.LastKnownPlayername);
					onlinePlayer.SendMessage((onlinePlayer.ClientId == serverPlayer.ClientId && args.Caller.FromChatGroupId != num) ? args.Caller.FromChatGroupId : num, l2, EnumChatType.CommandSuccess);
				}
			}
			break;
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdNoInvite(TextCommandCallingArgs args)
	{
		string playerUID = args.Caller.Player.PlayerUID;
		server.PlayerDataManager.PlayerDataByUid[playerUID].AllowInvite = (bool)args[0];
		return Success(args, server.PlayerDataManager.PlayerDataByUid[playerUID].AllowInvite ? "Ok, Group invites are now enabled" : "Ok, Group invites are now disabled");
	}

	public void SendPlayerGroups(IServerPlayer player)
	{
		List<Packet_PlayerGroup> list = new List<Packet_PlayerGroup>();
		foreach (KeyValuePair<int, PlayerGroupMembership> playerGroupMemberShip in ((ServerPlayer)player).serverdata.PlayerGroupMemberShips)
		{
			if (playerGroupMemberShip.Value.Level != EnumPlayerGroupMemberShip.None)
			{
				server.PlayerDataManager.PlayerGroupsById.TryGetValue(playerGroupMemberShip.Key, out var value);
				if (value != null)
				{
					list.Add(GetPlayerGroupPacket(value, playerGroupMemberShip.Value));
				}
			}
		}
		Packet_PlayerGroups packet_PlayerGroups = new Packet_PlayerGroups();
		packet_PlayerGroups.SetGroups(list.ToArray());
		server.SendPacket(player, new Packet_Server
		{
			Id = 49,
			PlayerGroups = packet_PlayerGroups
		});
	}

	public void SendPlayerGroup(IServerPlayer player, PlayerGroup playergroup)
	{
		((ServerPlayer)player).serverdata.PlayerGroupMemberShips.TryGetValue(playergroup.Uid, out var value);
		if (value != null && value.Level != EnumPlayerGroupMemberShip.None)
		{
			server.SendPacket(player, new Packet_Server
			{
				Id = 50,
				PlayerGroup = GetPlayerGroupPacket(playergroup, value)
			});
		}
	}

	public void SendPlayerGroup(IServerPlayer player, PlayerGroup playergroup, PlayerGroupMembership membership)
	{
		if (membership.Level != EnumPlayerGroupMemberShip.None)
		{
			server.SendPacket(player, new Packet_Server
			{
				Id = 50,
				PlayerGroup = GetPlayerGroupPacket(playergroup, membership)
			});
		}
	}

	private Packet_PlayerGroup GetPlayerGroupPacket(PlayerGroup plrgroup, PlayerGroupMembership membership)
	{
		Packet_PlayerGroup packet_PlayerGroup = new Packet_PlayerGroup
		{
			Membership = (int)membership.Level,
			Name = plrgroup.Name,
			Owneruid = plrgroup.OwnerUID,
			Uid = plrgroup.Uid
		};
		List<Packet_ChatLine> list = new List<Packet_ChatLine>();
		foreach (ChatLine item in plrgroup.ChatHistory)
		{
			list.Add(new Packet_ChatLine
			{
				ChatType = (int)item.ChatType,
				Groupid = plrgroup.Uid,
				Message = item.Message
			});
		}
		packet_PlayerGroup.SetChathistory(list.ToArray());
		return packet_PlayerGroup;
	}

	public bool HasPlayerPrivilege(IServerPlayer player, int targetGroupid, EnumPlayerGroupPrivilege priv, string targetPlayerUid = null)
	{
		EnumPlayerGroupMemberShip level = GetGroupMemberShip(player, targetGroupid).Level;
		switch (priv)
		{
		case EnumPlayerGroupPrivilege.Invite:
			if (level != EnumPlayerGroupMemberShip.Op)
			{
				return level == EnumPlayerGroupMemberShip.Owner;
			}
			return true;
		case EnumPlayerGroupPrivilege.Kick:
			if (level != EnumPlayerGroupMemberShip.Op || GetGroupMemberShip(targetPlayerUid, targetGroupid).Level != EnumPlayerGroupMemberShip.Member)
			{
				return level == EnumPlayerGroupMemberShip.Owner;
			}
			return true;
		case EnumPlayerGroupPrivilege.Disband:
			return level == EnumPlayerGroupMemberShip.Owner;
		case EnumPlayerGroupPrivilege.Op:
			return level == EnumPlayerGroupMemberShip.Owner;
		case EnumPlayerGroupPrivilege.Rename:
			return level == EnumPlayerGroupMemberShip.Owner;
		default:
			return false;
		}
	}

	public PlayerGroupMembership GetGroupMemberShip(IServerPlayer player, int targetGroupid)
	{
		return GetGroupMemberShip(player.PlayerUID, targetGroupid);
	}

	public PlayerGroupMembership GetGroupMemberShip(string playerUID, int targetGroupid)
	{
		ServerPlayerData serverPlayerData = server.PlayerDataManager.PlayerDataByUid[playerUID];
		if (!serverPlayerData.PlayerGroupMemberShips.ContainsKey(targetGroupid))
		{
			return new PlayerGroupMembership
			{
				Level = EnumPlayerGroupMemberShip.None
			};
		}
		return serverPlayerData.PlayerGroupMemberShips[targetGroupid];
	}
}
