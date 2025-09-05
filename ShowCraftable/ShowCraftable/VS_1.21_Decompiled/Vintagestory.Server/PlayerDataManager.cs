using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class PlayerDataManager : IPermissionManager, IGroupManager, IPlayerDataManager
{
	private ServerMain server;

	public Dictionary<string, ServerWorldPlayerData> WorldDataByUID = new Dictionary<string, ServerWorldPlayerData>();

	public Dictionary<int, PlayerGroup> PlayerGroupsById;

	public Dictionary<string, ServerPlayerData> PlayerDataByUid;

	public List<PlayerEntry> BannedPlayers;

	public List<PlayerEntry> WhitelistedPlayers;

	public bool playerDataDirty;

	public bool playerGroupsDirty;

	public bool bannedListDirty;

	public bool whiteListDirty;

	Dictionary<int, PlayerGroup> IGroupManager.PlayerGroupsById => PlayerGroupsById;

	Dictionary<string, IServerPlayerData> IPlayerDataManager.PlayerDataByUid
	{
		get
		{
			Dictionary<string, IServerPlayerData> dictionary = new Dictionary<string, IServerPlayerData>();
			foreach (KeyValuePair<string, ServerPlayerData> item in PlayerDataByUid)
			{
				dictionary[item.Key] = item.Value;
			}
			return dictionary;
		}
	}

	public PlayerDataManager(ServerMain server)
	{
		this.server = server;
		server.RegisterGameTickListener(OnCheckRequireSave, 1000);
		server.EventManager.OnGameWorldBeingSaved += OnGameWorldBeingSaved;
		server.EventManager.OnPlayerJoin += EventManager_OnPlayerJoin;
	}

	private void EventManager_OnPlayerJoin(IServerPlayer byPlayer)
	{
		ServerPlayerData orCreateServerPlayerData = GetOrCreateServerPlayerData(byPlayer.PlayerUID, byPlayer.PlayerName);
		orCreateServerPlayerData.LastJoinDate = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
		if (orCreateServerPlayerData.FirstJoinDate == null)
		{
			orCreateServerPlayerData.FirstJoinDate = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
		}
	}

	private void OnGameWorldBeingSaved()
	{
		playerDataDirty = true;
		playerGroupsDirty = true;
		bannedListDirty = true;
		whiteListDirty = true;
		OnCheckRequireSave(0f);
	}

	private void OnCheckRequireSave(float dt)
	{
		if (playerDataDirty)
		{
			try
			{
				using (TextWriter textWriter = new StreamWriter(Path.Combine(GamePaths.PlayerData, "playerdata.json")))
				{
					textWriter.Write(JsonConvert.SerializeObject((object)PlayerDataByUid.Values.ToList(), (Formatting)1));
					textWriter.Close();
				}
				playerDataDirty = false;
			}
			catch (Exception ex)
			{
				ServerMain.Logger.Warning("Failed saving player data, will try again. {0}", ex.Message);
			}
		}
		if (playerGroupsDirty)
		{
			try
			{
				using (TextWriter textWriter2 = new StreamWriter(Path.Combine(GamePaths.PlayerData, "playergroups.json")))
				{
					textWriter2.Write(JsonConvert.SerializeObject((object)PlayerGroupsById.Values.ToList(), (Formatting)1));
					textWriter2.Close();
				}
				playerGroupsDirty = false;
			}
			catch (Exception ex2)
			{
				ServerMain.Logger.Warning("Failed saving player group data, will try again. {0}", ex2.Message);
			}
		}
		if (bannedListDirty)
		{
			try
			{
				using (TextWriter textWriter3 = new StreamWriter(Path.Combine(GamePaths.PlayerData, "playersbanned.json")))
				{
					textWriter3.Write(JsonConvert.SerializeObject((object)BannedPlayers, (Formatting)1));
					textWriter3.Close();
				}
				bannedListDirty = false;
			}
			catch (Exception ex3)
			{
				ServerMain.Logger.Warning("Failed saving player banned list, will try again. {0}", ex3.Message);
			}
		}
		if (!whiteListDirty)
		{
			return;
		}
		try
		{
			using (TextWriter textWriter4 = new StreamWriter(Path.Combine(GamePaths.PlayerData, "playerswhitelisted.json")))
			{
				textWriter4.Write(JsonConvert.SerializeObject((object)WhitelistedPlayers, (Formatting)1));
				textWriter4.Close();
			}
			whiteListDirty = false;
		}
		catch (Exception ex4)
		{
			ServerMain.Logger.Warning("Failed saving player whitelist, will try again. {0}", ex4.Message);
		}
	}

	private List<T> LoadList<T>(string name)
	{
		List<T> list = null;
		try
		{
			string path = Path.Combine(GamePaths.PlayerData, name);
			if (File.Exists(path))
			{
				using TextReader textReader = new StreamReader(path);
				list = JsonConvert.DeserializeObject<List<T>>(textReader.ReadToEnd());
				textReader.Close();
			}
			if (list == null)
			{
				list = new List<T>();
			}
		}
		catch (Exception e)
		{
			ServerMain.Logger.Error("Failed reading file " + name + ". Will stop server now.");
			ServerMain.Logger.Error(e);
			server.Stop("Failed reading playerdata");
		}
		return list;
	}

	public void Load()
	{
		PlayerGroupsById = new Dictionary<int, PlayerGroup>();
		PlayerDataByUid = new Dictionary<string, ServerPlayerData>();
		BannedPlayers = new List<PlayerEntry>();
		WhitelistedPlayers = new List<PlayerEntry>();
		List<ServerPlayerData> list = LoadList<ServerPlayerData>("playerdata.json");
		List<PlayerGroup> list2 = LoadList<PlayerGroup>("playergroups.json");
		List<PlayerEntry> list3 = LoadList<PlayerEntry>("playersbanned.json");
		List<PlayerEntry> list4 = LoadList<PlayerEntry>("playerswhitelisted.json");
		foreach (ServerPlayerData item in list)
		{
			PlayerDataByUid[item.PlayerUID] = item;
		}
		foreach (PlayerGroup item2 in list2)
		{
			PlayerGroupsById[item2.Uid] = item2;
		}
		foreach (PlayerEntry item3 in list3)
		{
			if (item3.UntilDate >= DateTime.Now)
			{
				BannedPlayers.Add(item3);
			}
			else
			{
				bannedListDirty = true;
			}
		}
		foreach (PlayerEntry item4 in list4)
		{
			WhitelistedPlayers.Add(item4);
		}
	}

	public PlayerGroup PlayerGroupForPrivateMessage(ConnectedClient sender, ConnectedClient receiver)
	{
		string text = GameMath.Md5Hash(sender.ServerData.PlayerUID + "-" + receiver.ServerData.PlayerUID);
		foreach (PlayerGroup value in PlayerGroupsById.Values)
		{
			if (value.Md5Identifier == text)
			{
				return value;
			}
		}
		PlayerGroup playerGroup = new PlayerGroup
		{
			OwnerUID = receiver.ServerData.PlayerUID,
			CreatedDate = DateTime.Today.ToLongDateString(),
			Md5Identifier = text,
			Name = "PM from " + sender.PlayerName + " to " + receiver.PlayerName,
			CreatedByPrivateMessage = true
		};
		AddPlayerGroup(playerGroup);
		return playerGroup;
	}

	public bool CanCreatePlayerGroup(string playerUid)
	{
		ServerPlayerData orCreateServerPlayerData = GetOrCreateServerPlayerData(playerUid);
		if (orCreateServerPlayerData == null)
		{
			return false;
		}
		if (!orCreateServerPlayerData.HasPrivilege(Privilege.manageplayergroups, server.Config.RolesByCode))
		{
			return false;
		}
		int num = 0;
		foreach (PlayerGroupMembership value in orCreateServerPlayerData.PlayerGroupMemberShips.Values)
		{
			if (value.Level == EnumPlayerGroupMemberShip.Owner)
			{
				num++;
			}
		}
		return num < server.Config.MaxOwnedGroupChannelsPerUser;
	}

	public PlayerGroup GetPlayerGroupByName(string name)
	{
		foreach (PlayerGroup value in PlayerGroupsById.Values)
		{
			if (value.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
			{
				return value;
			}
		}
		return null;
	}

	public void AddPlayerGroup(PlayerGroup group)
	{
		int num = 0;
		foreach (int key in PlayerGroupsById.Keys)
		{
			num = Math.Max(key, num);
		}
		if (num >= server.Config.NextPlayerGroupUid)
		{
			server.Config.NextPlayerGroupUid = num + 1;
			server.ConfigNeedsSaving = true;
		}
		group.Uid = server.Config.NextPlayerGroupUid++;
		server.ConfigNeedsSaving = true;
		PlayerGroupsById[group.Uid] = group;
	}

	public void RemovePlayerGroup(PlayerGroup group)
	{
		PlayerGroupsById.Remove(group.Uid);
	}

	public ServerPlayerData GetOrCreateServerPlayerData(string playerUID, string playerName = null)
	{
		PlayerDataByUid.TryGetValue(playerUID, out var value);
		string code = server.Config.DefaultRole.Code;
		if (value == null)
		{
			value = new ServerPlayerData
			{
				AllowInvite = true,
				PlayerUID = playerUID,
				RoleCode = code,
				LastKnownPlayername = playerName
			};
			PlayerDataByUid[playerUID] = value;
			playerDataDirty = true;
		}
		ConnectedClient clientByUID = server.GetClientByUID(playerUID);
		if (clientByUID != null && clientByUID.IsSinglePlayerClient)
		{
			value.RoleCode = server.Config.Roles.MaxBy((PlayerRole v) => v.PrivilegeLevel).Code;
		}
		return value;
	}

	public ServerPlayerData GetServerPlayerDataByLastKnownPlayername(string playername)
	{
		foreach (ServerPlayerData value in PlayerDataByUid.Values)
		{
			if (value.LastKnownPlayername != null && value.LastKnownPlayername.Equals(playername, StringComparison.InvariantCultureIgnoreCase))
			{
				return value;
			}
		}
		return null;
	}

	internal void BanPlayer(string playername, string playeruid, string byPlayerName, string reason = "", DateTime? untildate = null)
	{
		PlayerEntry playerBan = GetPlayerBan(playername, playeruid);
		if (playerBan == null)
		{
			BannedPlayers.Add(new PlayerEntry
			{
				PlayerName = playername,
				IssuedByPlayerName = byPlayerName,
				PlayerUID = playeruid,
				Reason = reason,
				UntilDate = untildate
			});
			ServerMain.Logger.Audit("{0} was banned by {1} until {2}. Reason: {3}", playername, byPlayerName, untildate, reason);
		}
		else
		{
			playerBan.Reason = reason;
			playerBan.UntilDate = untildate;
			ServerMain.Logger.Audit("Existing player ban of {0} updated by {1}. Now until {2}, Reason: {3}", playername, byPlayerName, untildate, reason);
		}
		bannedListDirty = true;
	}

	internal bool UnbanPlayer(string playername, string playeruid, string issuingPlayerName)
	{
		PlayerEntry playerBan = GetPlayerBan(playername, playeruid);
		if (playerBan != null)
		{
			BannedPlayers.Remove(playerBan);
			bannedListDirty = true;
			ServerMain.Logger.Audit("{0} was unbanned by {1}.", playername, issuingPlayerName);
			return true;
		}
		return false;
	}

	public bool UnWhitelistPlayer(string playername, string playeruid)
	{
		PlayerEntry playerWhitelist = GetPlayerWhitelist(playername, playeruid);
		if (playerWhitelist != null)
		{
			WhitelistedPlayers.Remove(playerWhitelist);
			whiteListDirty = true;
			return true;
		}
		return false;
	}

	public void WhitelistPlayer(string playername, string playeruid, string byPlayername, string reason = "", DateTime? untildate = null)
	{
		PlayerEntry playerWhitelist = GetPlayerWhitelist(playername, playeruid);
		if (playerWhitelist == null)
		{
			WhitelistedPlayers.Add(new PlayerEntry
			{
				PlayerName = playername,
				IssuedByPlayerName = byPlayername,
				Reason = reason,
				UntilDate = untildate,
				PlayerUID = playeruid
			});
		}
		else
		{
			playerWhitelist.Reason = reason;
			playerWhitelist.UntilDate = untildate;
		}
		whiteListDirty = true;
	}

	public PlayerEntry GetPlayerBan(string playername, string playeruid)
	{
		PlayerEntry playerEntry = GetPlayerEntry(BannedPlayers, playeruid, playername);
		if (playerEntry == null)
		{
			return null;
		}
		if (playeruid != null && playeruid != playerEntry.PlayerUID)
		{
			playerEntry.PlayerUID = playeruid;
			bannedListDirty = true;
		}
		return playerEntry;
	}

	public PlayerEntry GetPlayerWhitelist(string playername, string playeruid)
	{
		PlayerEntry playerEntry = GetPlayerEntry(WhitelistedPlayers, playeruid, playername);
		if (playerEntry == null)
		{
			return null;
		}
		if (playeruid != null && playeruid != playerEntry.PlayerUID)
		{
			playerEntry.PlayerUID = playeruid;
			whiteListDirty = true;
		}
		return playerEntry;
	}

	private PlayerEntry GetPlayerEntry(List<PlayerEntry> list, string playeruid, string playername)
	{
		foreach (PlayerEntry item in list)
		{
			if (item.PlayerUID == null || playeruid == null)
			{
				if (item.PlayerName?.ToLowerInvariant() == playername?.ToLowerInvariant())
				{
					return item;
				}
			}
			else if (item.PlayerUID == playeruid)
			{
				return item;
			}
		}
		return null;
	}

	public void SetRole(IServerPlayer player, IPlayerRole role)
	{
		if (!server.Config.RolesByCode.ContainsKey(role.Code))
		{
			throw new ArgumentException("No such role configured '" + role.Code + "'");
		}
		GetOrCreateServerPlayerData(player.PlayerUID).SetRole(role as PlayerRole);
	}

	public void SetRole(IServerPlayer player, string roleCode)
	{
		if (!server.Config.RolesByCode.ContainsKey(roleCode))
		{
			throw new ArgumentException("No such role configured '" + roleCode + "'");
		}
		GetOrCreateServerPlayerData(player.PlayerUID).SetRole(server.Config.RolesByCode[roleCode]);
	}

	public IPlayerRole GetRole(string code)
	{
		return server.Config.RolesByCode[code];
	}

	public void RegisterPrivilege(string code, string shortdescription, bool adminAutoGrant = true)
	{
		server.AllPrivileges.Add(code);
		server.PrivilegeDescriptions[code] = shortdescription;
		if (!adminAutoGrant)
		{
			return;
		}
		foreach (PlayerRole value in server.Config.RolesByCode.Values)
		{
			if (value.AutoGrant)
			{
				value.GrantPrivilege(code);
			}
		}
	}

	public void GrantTemporaryPrivilege(string code)
	{
		server.Config.RuntimePrivileveCodes.Add(code);
	}

	public void DropTemporaryPrivilege(string code)
	{
		server.Config.RuntimePrivileveCodes.Remove(code);
	}

	public bool GrantPrivilege(string playerUID, string code, bool permanent = false)
	{
		ServerPlayerData orCreateServerPlayerData = GetOrCreateServerPlayerData(playerUID);
		if (orCreateServerPlayerData == null)
		{
			return false;
		}
		if (permanent)
		{
			orCreateServerPlayerData.GrantPrivilege(code);
		}
		else
		{
			orCreateServerPlayerData.RuntimePrivileges.Add(code);
		}
		return true;
	}

	public bool DenyPrivilege(string playerUID, string code)
	{
		ServerPlayerData orCreateServerPlayerData = GetOrCreateServerPlayerData(playerUID);
		if (orCreateServerPlayerData == null)
		{
			return false;
		}
		orCreateServerPlayerData.DenyPrivilege(code);
		return true;
	}

	public bool RemovePrivilegeDenial(string playerUID, string code)
	{
		ServerPlayerData orCreateServerPlayerData = GetOrCreateServerPlayerData(playerUID);
		if (orCreateServerPlayerData == null)
		{
			return false;
		}
		orCreateServerPlayerData.RemovePrivilegeDenial(code);
		return true;
	}

	public bool RevokePrivilege(string playerUID, string code, bool permanent = false)
	{
		ServerPlayerData orCreateServerPlayerData = GetOrCreateServerPlayerData(playerUID);
		if (orCreateServerPlayerData == null)
		{
			return false;
		}
		if (permanent)
		{
			orCreateServerPlayerData.RevokePrivilege(code);
		}
		else
		{
			orCreateServerPlayerData.RuntimePrivileges.Remove(code);
		}
		return true;
	}

	public bool AddPrivilegeToGroup(string groupCode, string privilegeCode)
	{
		server.Config.RolesByCode.TryGetValue(groupCode, out var value);
		if (value == null)
		{
			return false;
		}
		value.RuntimePrivileges.Add(privilegeCode);
		return true;
	}

	public bool RemovePrivilegeFromGroup(string groupCode, string privilegeCode)
	{
		server.Config.RolesByCode.TryGetValue(groupCode, out var value);
		if (value == null)
		{
			return false;
		}
		value.RuntimePrivileges.Remove(privilegeCode);
		return true;
	}

	public int GetPlayerPermissionLevel(int player)
	{
		return server.Clients[player].ServerData.GetPlayerRole(server).PrivilegeLevel;
	}

	public IServerPlayerData GetPlayerDataByUid(string playerUid)
	{
		PlayerDataByUid.TryGetValue(playerUid, out var value);
		return value;
	}

	public IServerPlayerData GetPlayerDataByLastKnownName(string name)
	{
		return GetServerPlayerDataByLastKnownPlayername(name);
	}

	public void ResolvePlayerName(string playername, Action<EnumServerResponse, string> onPlayerReceived)
	{
		server.GetOnlineOrOfflinePlayer(playername, onPlayerReceived);
	}

	public void ResolvePlayerUid(string playeruid, Action<EnumServerResponse, string> onPlayerReceived)
	{
		server.GetOnlineOrOfflinePlayerByUid(playeruid, onPlayerReceived);
	}
}
