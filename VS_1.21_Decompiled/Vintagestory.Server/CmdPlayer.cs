using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server.Network;

namespace Vintagestory.Server;

public class CmdPlayer : ServerSystem
{
	public delegate TextCommandResult PlayerEachDelegate(PlayerUidName targetPlayer, TextCommandCallingArgs args);

	private bool ConfigNeedsSaving
	{
		get
		{
			return server.ConfigNeedsSaving;
		}
		set
		{
			server.ConfigNeedsSaving = value;
		}
	}

	private ServerConfig Config => server.Config;

	public CmdPlayer(ServerMain server)
		: base(server)
	{
		CmdPlayer cmdPlayer = this;
		IChatCommandApi chatCommands = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		string[] array = new string[10] { "0", "1", "2", "3", "4", "creative", "survival", "spectator", "guest", "abbreviated game mode names are valid as well" };
		chatCommands.GetOrCreate("mystats").WithDescription("shows players stats").RequiresPrivilege(Privilege.chat)
			.RequiresPlayer()
			.HandleWith(OnCmdMyStats)
			.Validate();
		chatCommands.GetOrCreate("whitelist").WithDesc("Whitelist control").RequiresPrivilege(Privilege.whitelist)
			.BeginSub("add")
			.WithDesc("Add a player to the whitelist")
			.WithArgs(parsers.PlayerUids("player"), parsers.OptionalAll("optional reason"))
			.HandleWith((TextCommandCallingArgs args) => Each(args, delegate(PlayerUidName targetPlayer, TextCommandCallingArgs textCommandCallingArgs)
			{
				if (server.PlayerDataManager.WhitelistedPlayers.Any((PlayerEntry item) => item.PlayerUID == targetPlayer.Uid))
				{
					return TextCommandResult.Error("Player is already whitelisted");
				}
				string reason = (string)textCommandCallingArgs[1];
				server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
				string byPlayername = ((textCommandCallingArgs.Caller.Player != null) ? textCommandCallingArgs.Caller.Player.PlayerName : textCommandCallingArgs.Caller.Type.ToString());
				DateTime dateTime = DateTime.Now.AddYears(50);
				server.PlayerDataManager.WhitelistPlayer(targetPlayer.Name, targetPlayer.Uid, byPlayername, reason, dateTime);
				return TextCommandResult.Success(Lang.Get("Player is now whitelisted until {0}", dateTime));
			}))
			.EndSub()
			.BeginSub("remove")
			.WithDesc("Remove a player from the whitelist")
			.WithArgs(parsers.PlayerUids("player"))
			.HandleWith((TextCommandCallingArgs args) => Each(args, delegate(PlayerUidName targetPlayer, TextCommandCallingArgs textCommandCallingArgs)
			{
				if (!server.PlayerDataManager.WhitelistedPlayers.Any((PlayerEntry item) => item.PlayerUID == targetPlayer.Uid))
				{
					return TextCommandResult.Error("Player is not whitelisted");
				}
				server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
				server.PlayerDataManager.UnWhitelistPlayer(targetPlayer.Name, targetPlayer.Uid);
				return TextCommandResult.Success(Lang.Get("Player is now removed from the whitelist"));
			}))
			.EndSub()
			.BeginSub("on")
			.WithDesc("Enable whitelist system. Only whitelisted players can join")
			.HandleWith(delegate
			{
				if (server.Config.WhitelistMode == EnumWhitelistMode.On)
				{
					return TextCommandResult.Error(Lang.Get("Whitelist was already enabled"));
				}
				server.Config.WhitelistMode = EnumWhitelistMode.On;
				server.ConfigNeedsSaving = true;
				return TextCommandResult.Success(Lang.Get("Whitelist now enabled"));
			})
			.EndSub()
			.BeginSub("off")
			.HandleWith(delegate
			{
				if (server.Config.WhitelistMode == EnumWhitelistMode.Off)
				{
					return TextCommandResult.Error(Lang.Get("Whitelist was already disabled"));
				}
				server.Config.WhitelistMode = EnumWhitelistMode.Off;
				server.ConfigNeedsSaving = true;
				return TextCommandResult.Success(Lang.Get("Whitelist now disabled"));
			})
			.WithDesc("Disable whitelist system. All players can join")
			.EndSub()
			.Validate();
		chatCommands.GetOrCreate("player").WithDesc("Player control").WithArgs(parsers.PlayerUids("player"))
			.RequiresPrivilege(Privilege.chat)
			.BeginSub("movespeed")
			.RequiresPrivilege(Privilege.grantrevoke)
			.WithDesc("Set a player's move speed")
			.WithArgs(parsers.Float("movespeed"))
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.setMovespeed))
			.EndSub()
			.BeginSub("whitelist")
			.RequiresPrivilege(Privilege.whitelist)
			.WithDesc("Add/remove player to/from the whitelist")
			.WithArgs(parsers.OptionalBool("add/remove", "add"), parsers.OptionalAll("optional reason"))
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.addRemoveWhitelist))
			.EndSub()
			.BeginSub("privilege")
			.RequiresPrivilege(Privilege.grantrevoke)
			.WithDesc("Player privilege control")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.listPrivilege))
			.BeginSub("grant")
			.WithDesc("Grant a privilege to a player")
			.WithArgs(parsers.Word("privilege_name", Privilege.AllCodes().Append("or custom defined privileges")))
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.grantPrivilege))
			.EndSub()
			.BeginSub("revoke")
			.WithArgs(parsers.Word("privilege_name", Privilege.AllCodes().Append("or custom defined privileges")))
			.WithDesc("Revoke a privilege from a player")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.revokePrivilege))
			.EndSub()
			.BeginSub("deny")
			.WithArgs(parsers.Privilege("privilege_name"))
			.WithDesc("Deny a privilege to a player that was ordinarily granted from a role")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.denyPrivilege))
			.EndSub()
			.BeginSub("removedeny")
			.WithArgs(parsers.Privilege("privilege_name"))
			.WithDesc("Remove a previous privilege denial from a player")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.removeDenyPrivilege))
			.EndSub()
			.EndSub()
			.BeginSub("role")
			.RequiresPrivilege(Privilege.grantrevoke)
			.WithDesc("Set or get a player role")
			.WithArgs(parsers.OptionalPlayerRole("role"))
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.GetSetRole))
			.EndSub()
			.BeginSub("stats")
			.RequiresPrivilege(Privilege.grantrevoke)
			.WithDesc("Display player parameters")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.getStats))
			.EndSub()
			.BeginSub("entity")
			.RequiresPrivilege(Privilege.grantrevoke)
			.WithDesc("Get/Set an attribute value on the player entity")
			.WithArgs(parsers.OptionalWord("attribute_name"), parsers.OptionalFloat("attribute value"))
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.handleEntity))
			.EndSub()
			.BeginSub("wipedata")
			.RequiresPrivilege(Privilege.controlserver)
			.WithDesc("Wipe the player data, such as the entire inventory, skin/class, etc.")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.WipePlayerData))
			.EndSub()
			.BeginSub("clearinv")
			.RequiresPrivilege(Privilege.controlserver)
			.WithDesc("Clear the player's entire inventory")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.WipePlayerInventory))
			.EndSub()
			.BeginSub("gamemode")
			.WithAlias("gm")
			.WithArgs(parsers.OptionalWordRange("mode", array))
			.RequiresPrivilege(Privilege.gamemode)
			.WithDesc("Set (or discover) the player(s) game mode")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.getSetGameMode))
			.EndSub()
			.BeginSub("allowcharselonce")
			.WithAlias("acso")
			.RequiresPrivilege(Privilege.grantrevoke)
			.WithDesc("Allow changing character class and skin one more time")
			.WithAdditionalInformation("Allows the player to run the <code>.charsel</code> command client-side")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.handleCharSel))
			.EndSub()
			.BeginSub("landclaimallowance")
			.WithAlias("lca")
			.WithArgs(parsers.OptionalInt("amount"))
			.WithDesc("Get/Set land claim allowance")
			.WithAdditionalInformation("Specifies the amount of land a player can claim, in m³")
			.RequiresPrivilege(Privilege.grantrevoke)
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.handleLandClaimAllowance))
			.EndSub()
			.BeginSub("landclaimmaxareas")
			.WithAlias("lcma")
			.WithArgs(parsers.OptionalInt("number"))
			.WithDesc("Get/Set land claim max areas")
			.WithAdditionalInformation("Specifies the maximum number of separate land areas a player can claim")
			.RequiresPrivilege(Privilege.grantrevoke)
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.handleLandClaimMaxAreas))
			.EndSub()
			.Validate();
		chatCommands.Create("op").WithDesc("Give a player admin status. Shorthand for /player &lt;playername&gt; role admin").WithArgs(parsers.PlayerUids("playername"))
			.RequiresPrivilege(Privilege.grantrevoke)
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.opPlayer))
			.Validate();
		chatCommands.Create("self").WithDesc("Information about your player").RequiresPrivilege(Privilege.chat)
			.BeginSub("stats")
			.WithDesc("Full stats")
			.HandleWith((TextCommandCallingArgs args) => cmdPlayer.getStats(new PlayerUidName(args.Caller.Player?.PlayerUID, args.Caller.Player?.PlayerName), args))
			.EndSub()
			.BeginSub("privileges")
			.WithDesc("Your current privileges")
			.HandleWith((TextCommandCallingArgs args) => cmdPlayer.listPrivilege(new PlayerUidName(args.Caller.Player?.PlayerUID, args.Caller.Player?.PlayerName), args))
			.EndSub()
			.BeginSub("role")
			.WithDesc("Your current role")
			.HandleWith((TextCommandCallingArgs args) => cmdPlayer.GetSetRole(new PlayerUidName(args.Caller.Player?.PlayerUID, args.Caller.Player?.PlayerName), args))
			.EndSub()
			.BeginSub("gamemode")
			.WithDesc("Your current game mode")
			.HandleWith(handleGameMode)
			.EndSub()
			.BeginSub("clearinv")
			.RequiresPrivilege(Privilege.gamemode)
			.WithRootAlias("clearinv")
			.WithDesc("Empties your inventory")
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				args.Caller.Player?.InventoryManager.DiscardAll();
				return TextCommandResult.Success();
			})
			.EndSub()
			.BeginSub("kill")
			.RequiresPrivilege(Privilege.selfkill)
			.WithRootAlias("kill")
			.WithDesc("Kill yourself")
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				args.Caller.Entity.Die(EnumDespawnReason.Death, new DamageSource
				{
					Source = EnumDamageSource.Suicide
				});
				return TextCommandResult.Success();
			})
			.EndSub()
			.Validate();
		chatCommands.Create("gamemode").WithAlias("gm").WithDesc("Get/Set one players game mode. Omit playername arg to get/set your own game mode")
			.RequiresPrivilege(Privilege.chat)
			.WithArgs(parsers.Unparsed("playername"), parsers.Unparsed("mode", array))
			.HandleWith(handleGameMode)
			.Validate();
		chatCommands.Create("role").RequiresPrivilege(Privilege.controlserver).WithDescription("Modify/See player role related data")
			.WithArgs(parsers.PlayerRole("rolename"))
			.BeginSub("landclaimallowance")
			.WithAlias("lca")
			.WithDescription("Get/Set land claim allowance m³")
			.WithArgs(parsers.OptionalInt("landClaimAllowance", -1))
			.HandleWith(OnLandclaimallowanceCmd)
			.EndSub()
			.BeginSub("landclaimminsize")
			.WithAlias("lcms")
			.WithDescription("Get/Set land claim minimum size")
			.WithArgs(parsers.OptionalVec3i("minSize"))
			.HandleWith(OnLandclaimminsizeCmd)
			.EndSub()
			.BeginSub("landclaimmaxareas")
			.WithAlias("lcma")
			.WithDescription("Get/Set land claim maximum areas")
			.WithArgs(parsers.OptionalInt("area", -1))
			.HandleWith(OnLandclaimmaxareasCmd)
			.EndSub()
			.BeginSub("privilege")
			.WithDescription("Show privileges for role")
			.HandleWith(OnPrivilegeCmd)
			.BeginSub("grant")
			.WithDescription("Grant a privilege")
			.WithArgs(parsers.Word("privilege_name", Privilege.AllCodes().Append("or custom defined privileges")))
			.HandleWith(OnGrantCmd)
			.EndSub()
			.BeginSub("revoke")
			.WithDescription("Revoke  a privilege")
			.WithArgs(parsers.Privilege("privilege_name"))
			.HandleWith(OnRevokeCmd)
			.EndSub()
			.EndSub()
			.BeginSub("spawnpoint")
			.WithDescription("Get/Set/Unset the default spawnpoint")
			.HandleWith(OnSpawnpointCmd)
			.BeginSub("set")
			.WithDescription("Set the default spawnpoint")
			.WithArgs(parsers.WorldPosition("pos"))
			.HandleWith(OnSpawnpointSetCmd)
			.EndSub()
			.BeginSub("unset")
			.WithDesc("Unset the default spawnpoint")
			.HandleWith(OnSpawnpointUnsetCmd)
			.EndSub()
			.EndSub()
			.Validate();
	}

	private TextCommandResult OnSpawnpointUnsetCmd(TextCommandCallingArgs args)
	{
		PlayerRole playerRole = (PlayerRole)args.Parsers[0].GetValue();
		playerRole.DefaultSpawn = null;
		ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Spawnpoint for role {0} now unset.", playerRole.Name, playerRole.DefaultSpawn));
	}

	private TextCommandResult OnSpawnpointSetCmd(TextCommandCallingArgs args)
	{
		PlayerRole playerRole = (PlayerRole)args.Parsers[0].GetValue();
		Vec3d vec3d = (Vec3d)args.Parsers[1].GetValue();
		playerRole.DefaultSpawn = new PlayerSpawnPos
		{
			x = (int)vec3d.X,
			y = (int)vec3d.Y,
			z = (int)vec3d.Z
		};
		ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Spawnpoint for role {0} now set to {1}", playerRole.Name, playerRole.DefaultSpawn));
	}

	private TextCommandResult OnSpawnpointCmd(TextCommandCallingArgs args)
	{
		PlayerRole playerRole = (PlayerRole)args.Parsers[0].GetValue();
		if (playerRole.DefaultSpawn == null)
		{
			return TextCommandResult.Success(Lang.Get("Spawnpoint for role {0} is not set.", playerRole.Name));
		}
		return TextCommandResult.Success(Lang.Get("Spawnpoint for role {0} is at {1}", playerRole.Name, playerRole.DefaultSpawn));
	}

	private TextCommandResult OnRevokeCmd(TextCommandCallingArgs args)
	{
		IPlayerRole playerRole = (IPlayerRole)args.Parsers[0].GetValue();
		string text = (string)args.Parsers[1].GetValue();
		if (!playerRole.Privileges.Contains(text))
		{
			return TextCommandResult.Error(Lang.Get("Role does not have this privilege"));
		}
		playerRole.RevokePrivilege(text);
		server.ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Ok, privilege '{0}' now revoked", text));
	}

	private TextCommandResult OnGrantCmd(TextCommandCallingArgs args)
	{
		IPlayerRole playerRole = (IPlayerRole)args.Parsers[0].GetValue();
		string text = (string)args.Parsers[1].GetValue();
		if (playerRole.Privileges.Contains(text))
		{
			return TextCommandResult.Error(Lang.Get("Role already has this privilege"));
		}
		playerRole.GrantPrivilege(text);
		server.ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Ok, privilege '{0}' now granted", text));
	}

	private TextCommandResult OnPrivilegeCmd(TextCommandCallingArgs args)
	{
		IPlayerRole playerRole = (IPlayerRole)args.Parsers[0].GetValue();
		return TextCommandResult.Success(Lang.Get("This role has following privileges: {0}", string.Join(", ", playerRole.Privileges)));
	}

	private TextCommandResult OnLandclaimmaxareasCmd(TextCommandCallingArgs args)
	{
		IPlayerRole playerRole = (IPlayerRole)args.Parsers[0].GetValue();
		int? num = (int?)args.Parsers[1].GetValue();
		if (!num.HasValue || num < 0)
		{
			return TextCommandResult.Success(Lang.Get("This role has a land claim max areas {0}", playerRole.LandClaimMaxAreas));
		}
		playerRole.LandClaimMaxAreas = num.Value;
		server.ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Land claim max areas now set to {0}", playerRole.LandClaimMaxAreas));
	}

	private TextCommandResult OnLandclaimminsizeCmd(TextCommandCallingArgs args)
	{
		IPlayerRole playerRole = (IPlayerRole)args.Parsers[0].GetValue();
		Vec3i vec3i = (Vec3i)args.Parsers[1].GetValue();
		if (vec3i == null)
		{
			return TextCommandResult.Success(Lang.Get("This role has a land claim min size of {0} blocks", playerRole.LandClaimMinSize));
		}
		playerRole.LandClaimMinSize = vec3i;
		server.ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Land claim min size now set to {0} blocks", playerRole.LandClaimMinSize));
	}

	private TextCommandResult OnLandclaimallowanceCmd(TextCommandCallingArgs args)
	{
		IPlayerRole playerRole = (IPlayerRole)args.Parsers[0].GetValue();
		int? num = (int?)args.Parsers[1].GetValue();
		if (!num.HasValue || num < 0)
		{
			return TextCommandResult.Success(Lang.Get("This role has a land claim allowance of {0}m³", playerRole.LandClaimAllowance));
		}
		playerRole.LandClaimAllowance = num.Value;
		server.ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Land claim allowance now set to {0}m³", playerRole.LandClaimAllowance));
	}

	private TextCommandResult OnCmdMyStats(TextCommandCallingArgs args)
	{
		return getStats(new PlayerUidName(args.Caller.Player.PlayerUID, args.Caller.Player.PlayerName), args);
	}

	private TextCommandResult WipePlayerInventory(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ConnectedClient clientByUID = server.GetClientByUID(targetPlayer.Uid);
		if (clientByUID != null)
		{
			foreach (KeyValuePair<string, InventoryBase> inventory in clientByUID.WorldData.inventories)
			{
				inventory.Value.Clear();
			}
			clientByUID.Player.BroadcastPlayerData(sendInventory: true);
			return TextCommandResult.Success("Inventory cleared.");
		}
		server.ClearPlayerInvs.Add(targetPlayer.Uid);
		return TextCommandResult.Success("Clear command queued. Inventory will be cleared next time the player connects, which must happen before the server restarts");
	}

	private TextCommandResult WipePlayerData(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		if (server.chunkThread.gameDatabase.GetPlayerData(targetPlayer.Uid) == null)
		{
			return TextCommandResult.Error("No data for this player found in savegame");
		}
		server.chunkThread.gameDatabase.SetPlayerData(targetPlayer.Uid, null);
		server.PlayerDataManager.PlayerDataByUid.Remove(targetPlayer.Uid);
		server.PlayerDataManager.WorldDataByUID.Remove(targetPlayer.Uid);
		server.PlayerDataManager.playerDataDirty = true;
		return TextCommandResult.Success("Ok, player data deleted");
	}

	private TextCommandResult handleGameMode(TextCommandCallingArgs args)
	{
		string text = args.Caller.Player?.PlayerName;
		if (args.RawArgs.Length > 0 && (server.GetClientByPlayername(args.RawArgs.PeekWord()) != null || args.RawArgs.Length > 1))
		{
			text = args.RawArgs.PopWord();
		}
		string text2 = args.RawArgs.PopWord();
		ConnectedClient clientByPlayername = server.GetClientByPlayername(text);
		if (clientByPlayername == null)
		{
			return TextCommandResult.Error(Lang.Get("No player with name '{0}' online", text));
		}
		bool flag = args.Caller.Player?.PlayerUID == clientByPlayername.Player.PlayerUID;
		if (text2 == null)
		{
			if (flag)
			{
				return TextCommandResult.Success(Lang.Get("Your Current gamemode is {0}", clientByPlayername.WorldData.GameMode));
			}
			return TextCommandResult.Success(Lang.Get("Current gamemode for {0} is {1}", text, clientByPlayername.WorldData.GameMode));
		}
		if (!flag && !args.Caller.HasPrivilege(Privilege.commandplayer))
		{
			return TextCommandResult.Error(Lang.Get("Insufficient Privileges to set another players game mode"));
		}
		if (flag && !args.Caller.HasPrivilege(Privilege.gamemode))
		{
			return TextCommandResult.Error(Lang.Get("Insufficient Privileges to set your game mode"));
		}
		return SetGameMode(args.Caller, new PlayerUidName(clientByPlayername.SentPlayerUid, clientByPlayername.PlayerName), text2);
	}

	private TextCommandResult handleEntity(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		string text = (string)args[1];
		float num = (float)args[2];
		if (!(server.PlayerByUid(targetPlayer.Uid) is IServerPlayer { Entity: var entity }))
		{
			return TextCommandResult.Error(Lang.Get("Player must be online to set attributes"));
		}
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("hunger");
		ITreeAttribute treeAttribute2 = entity.WatchedAttributes.GetTreeAttribute("health");
		ITreeAttribute treeAttribute3 = entity.WatchedAttributes.GetTreeAttribute("oxygen");
		if (args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Error(Lang.Get("Position: {0}, Satiety: {1}/{2}, Health: {3}/{4}", entity.ServerPos.XYZ, treeAttribute.GetFloat("currentsaturation"), treeAttribute.TryGetFloat("maxsaturation"), treeAttribute2.GetFloat("currenthealth"), treeAttribute2.TryGetFloat("maxhealth")));
		}
		float? num2 = treeAttribute.TryGetFloat("maxsaturation");
		switch (text)
		{
		case "satiety":
			num = GameMath.Clamp(num, 0f, 1f);
			if (treeAttribute != null)
			{
				float value = num * num2.Value;
				treeAttribute.SetFloat("currentsaturation", value);
				entity.WatchedAttributes.MarkPathDirty("hunger");
				return TextCommandResult.Success("Satiety " + value + " set.");
			}
			return TextCommandResult.Error("hunger attribute tree not found.");
		case "protein":
		case "fruit":
		case "dairy":
		case "grain":
		case "vegetable":
			num = GameMath.Clamp(num, 0f, 1f);
			if (treeAttribute != null)
			{
				float value3 = num * num2.Value;
				treeAttribute.SetFloat(text + "Level", value3);
				return TextCommandResult.Success(text + " level " + value3 + " set.");
			}
			return TextCommandResult.Error("hunger attribute tree not found.");
		case "intox":
			entity.WatchedAttributes.SetFloat("intoxication", num);
			return TextCommandResult.Success("Intoxication value " + num + " set.");
		case "temp":
			entity.WatchedAttributes.GetTreeAttribute("bodyTemp").SetFloat("bodytemp", num);
			return TextCommandResult.Success("Body temp " + num + " set.");
		case "tempstab":
			num = GameMath.Clamp(num, 0f, 1f);
			entity.WatchedAttributes.SetDouble("temporalStability", num);
			return TextCommandResult.Success("Stability " + num + " set.");
		case "health":
			num = GameMath.Clamp(num, 0f, 1f);
			if (treeAttribute2 != null)
			{
				float value2 = num * treeAttribute2.TryGetFloat("maxhealth").Value;
				treeAttribute2.SetFloat("currenthealth", value2);
				entity.WatchedAttributes.MarkPathDirty("health");
				return TextCommandResult.Success("Health " + value2 + " set.");
			}
			return TextCommandResult.Error("health attribute tree not found.");
		case "maxhealth":
			num = GameMath.Clamp(num, 0f, 9999f);
			if (treeAttribute2 != null)
			{
				treeAttribute2.SetFloat("basemaxhealth", num);
				treeAttribute2.SetFloat("maxhealth", num);
				treeAttribute2.SetFloat("currenthealth", num);
				entity.WatchedAttributes.MarkPathDirty("health");
				return TextCommandResult.Success("Max Health " + num + " set.");
			}
			return TextCommandResult.Error("health attribute tree not found.");
		case "maxoxygen":
		case "maxoxy":
			num = GameMath.Clamp(num, 0f, 100000000f);
			if (treeAttribute3 != null)
			{
				treeAttribute3.SetFloat("maxoxygen", num);
				treeAttribute3.SetFloat("currentoxygen", num);
				entity.WatchedAttributes.MarkPathDirty("oxygen");
				return TextCommandResult.Success("Max Oxygen " + num + " set.");
			}
			return TextCommandResult.Error("Oxygen attribute tree not found.");
		default:
			return TextCommandResult.Success("Incorrect attribute name");
		}
	}

	private TextCommandResult handleLandClaimMaxAreas(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData serverPlayerData = server.GetServerPlayerData(targetPlayer.Uid);
		if (serverPlayerData == null)
		{
			return TextCommandResult.Error(Lang.Get("Only works for players that have connected to your server at least once"));
		}
		if (args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Success(Lang.Get("This player has a land claim extra max areas setting of {0}", serverPlayerData.ExtraLandClaimAreas));
		}
		serverPlayerData.ExtraLandClaimAreas = (int)args[1];
		return TextCommandResult.Success(Lang.Get("Land claim extra max areas now set to {0}", serverPlayerData.ExtraLandClaimAreas));
	}

	private TextCommandResult handleLandClaimAllowance(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData serverPlayerData = server.GetServerPlayerData(targetPlayer.Uid);
		if (serverPlayerData == null)
		{
			return TextCommandResult.Error(Lang.Get("Only works for players that have connected to your server at least once"));
		}
		if (args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Success(Lang.Get("This player has a land claim extra allowance of {0}m³", serverPlayerData.ExtraLandClaimAllowance));
		}
		serverPlayerData.ExtraLandClaimAllowance = (int)args[1];
		return TextCommandResult.Success(Lang.Get("Land claim extra allowance now set to {0}m³", serverPlayerData.ExtraLandClaimAllowance));
	}

	private TextCommandResult handleCharSel(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		IWorldPlayerData worldPlayerData = server.GetWorldPlayerData(targetPlayer.Uid);
		if (worldPlayerData == null)
		{
			return TextCommandResult.Error(Lang.Get("Only works for players that have connected to your server at least once"));
		}
		if (!worldPlayerData.EntityPlayer.WatchedAttributes.GetBool("allowcharselonce"))
		{
			worldPlayerData.EntityPlayer.WatchedAttributes.SetBool("allowcharselonce", value: true);
			return TextCommandResult.Success(Lang.Get("Ok, player can now run .charsel to change skin and character class once"));
		}
		return TextCommandResult.Error(Lang.Get("Player can already run .charsel to change skin and character class"));
	}

	private TextCommandResult getSetGameMode(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		if (args.Parsers[1].IsMissing)
		{
			if (!server.PlayerDataManager.WorldDataByUID.TryGetValue(targetPlayer.Uid, out var value))
			{
				return TextCommandResult.Error(Lang.Get("Player never connected to this server. Must at least connect once to set game mode"));
			}
			return TextCommandResult.Success(Lang.Get("Player has game mode {0}", value.GameMode));
		}
		return SetGameMode(args.Caller, targetPlayer, (string)args[1]);
	}

	private TextCommandResult SetGameMode(Caller caller, PlayerUidName parsedTargetPlayer, string modestring)
	{
		EnumGameMode? enumGameMode = null;
		if (int.TryParse(modestring, out var result))
		{
			if (Enum.IsDefined(typeof(EnumGameMode), result))
			{
				enumGameMode = (EnumGameMode)result;
			}
		}
		else if (modestring.ToLowerInvariant().StartsWith('c'))
		{
			enumGameMode = EnumGameMode.Creative;
		}
		else if (modestring.ToLowerInvariant().StartsWithOrdinal("sp"))
		{
			enumGameMode = EnumGameMode.Spectator;
		}
		else if (modestring.ToLowerInvariant().StartsWith('s'))
		{
			enumGameMode = EnumGameMode.Survival;
		}
		else if (modestring.ToLowerInvariant().StartsWith('g'))
		{
			enumGameMode = EnumGameMode.Guest;
		}
		if (!enumGameMode.HasValue)
		{
			return TextCommandResult.Error(Lang.Get("Invalid game mode '{0}'", modestring));
		}
		if (!server.PlayerDataManager.WorldDataByUID.TryGetValue(parsedTargetPlayer.Uid, out var value))
		{
			return TextCommandResult.Error(Lang.Get("Player never connected to this server. Must at least connect once to set game mode.", modestring));
		}
		EnumGameMode gameMode = value.GameMode;
		value.GameMode = enumGameMode.Value;
		bool flag = enumGameMode == EnumGameMode.Creative || enumGameMode == EnumGameMode.Spectator;
		value.FreeMove = (value.FreeMove && flag) || enumGameMode == EnumGameMode.Spectator;
		value.NoClip = (value.NoClip && flag) || enumGameMode == EnumGameMode.Spectator;
		if (enumGameMode == EnumGameMode.Survival || enumGameMode == EnumGameMode.Guest)
		{
			if (gameMode == EnumGameMode.Creative)
			{
				value.PreviousPickingRange = value.PickingRange;
			}
			value.PickingRange = GlobalConstants.DefaultPickingRange;
		}
		if (enumGameMode == EnumGameMode.Creative && (gameMode == EnumGameMode.Survival || gameMode == EnumGameMode.Guest))
		{
			value.PickingRange = value.PreviousPickingRange;
		}
		ServerPlayer serverPlayer = server.GetConnectedClient(parsedTargetPlayer.Uid)?.Player;
		if (enumGameMode != gameMode)
		{
			if (serverPlayer != null)
			{
				for (int i = 0; i < server.Systems.Length; i++)
				{
					server.Systems[i].OnPlayerSwitchGameMode(serverPlayer);
				}
			}
			if (enumGameMode == EnumGameMode.Guest || enumGameMode == EnumGameMode.Survival)
			{
				value.MoveSpeedMultiplier = 1f;
			}
			if (serverPlayer != null)
			{
				server.EventManager.TriggerPlayerChangeGamemode(serverPlayer);
			}
		}
		if (serverPlayer != null)
		{
			server.BroadcastPlayerData(serverPlayer, sendInventory: false);
			serverPlayer.Entity.UpdatePartitioning();
			if (serverPlayer.client.Socket is TcpNetConnection tcpNetConnection)
			{
				tcpNetConnection.SetLengthLimit(enumGameMode == EnumGameMode.Creative);
			}
		}
		object obj;
		if (enumGameMode.HasValue)
		{
			EnumGameMode? enumGameMode2 = enumGameMode;
			obj = Lang.Get("gamemode-" + enumGameMode2.ToString());
		}
		else
		{
			obj = "-";
		}
		string text = (string)obj;
		if (serverPlayer == caller.Player)
		{
			ServerMain.Logger.Audit("{0} put himself into game mode {1}", caller.GetName(), text);
			return TextCommandResult.Success(Lang.Get("Game mode {0} set.", text));
		}
		serverPlayer?.SendMessage(GlobalConstants.CurrentChatGroup, Lang.Get("{0} has set your gamemode to {1}", caller.GetName(), text), EnumChatType.Notification);
		ServerMain.Logger.Audit("{0} put {1} into game mode {2}", caller.GetName(), parsedTargetPlayer.Name, text);
		return TextCommandResult.Success(Lang.Get("Game mode {0} set for player {1}.", text, parsedTargetPlayer.Name));
	}

	private TextCommandResult getStats(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData serverPlayerData = server.GetServerPlayerData(targetPlayer.Uid);
		HashSet<string> allPrivilegeCodes = serverPlayerData.GetAllPrivilegeCodes(server.Config);
		StringBuilder stringBuilder = new StringBuilder();
		ConnectedClient clientByUID = server.GetClientByUID(targetPlayer.Uid);
		PlayerRole playerRole = serverPlayerData.GetPlayerRole(server);
		stringBuilder.AppendLine(Lang.Get("{0} is currently {1}", serverPlayerData.LastKnownPlayername, (clientByUID == null) ? "offline" : "online"));
		stringBuilder.AppendLine(Lang.Get("Role: {0}", serverPlayerData.RoleCode));
		stringBuilder.AppendLine(Lang.Get("All Privilege codes: {0}", string.Join(", ", allPrivilegeCodes.ToArray())));
		stringBuilder.AppendLine(Lang.Get("Land claim allowance: {0}m³ + {1}m³", playerRole.LandClaimAllowance, serverPlayerData.ExtraLandClaimAllowance));
		stringBuilder.AppendLine(Lang.Get("Land claim max areas: {0} + {1}", playerRole.LandClaimMaxAreas, serverPlayerData.ExtraLandClaimAreas));
		List<LandClaim> playerClaims = CmdLand.GetPlayerClaims(server, targetPlayer.Uid);
		int num = 0;
		foreach (LandClaim item in playerClaims)
		{
			num += item.SizeXYZ;
		}
		stringBuilder.AppendLine(Lang.Get("Land claimed: {0}m³", num));
		stringBuilder.AppendLine(Lang.Get("Amount of areas claimed: {0}", playerClaims.Count));
		if (args.Caller.HasPrivilege(Privilege.grantrevoke) && clientByUID != null)
		{
			stringBuilder.AppendLine($"Fly suspicion count: {clientByUID.AuditFlySuspicion}");
			stringBuilder.AppendLine($"Tele/Speed suspicion count: {clientByUID.TotalTeleSupicions}");
		}
		return TextCommandResult.Success(stringBuilder.ToString());
	}

	private TextCommandResult GetSetRole(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		if (args.Parsers.Count == 0 || args.Parsers[1].IsMissing)
		{
			ServerPlayerData orCreateServerPlayerData = server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
			return TextCommandResult.Success("Player has role " + orCreateServerPlayerData.RoleCode);
		}
		PlayerRole playerRole = (PlayerRole)args[1];
		return ChangeRole(args.Caller, targetPlayer, playerRole.Code);
	}

	private TextCommandResult opPlayer(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		return ChangeRole(args.Caller, targetPlayer, "admin");
	}

	public TextCommandResult ChangeRole(Caller caller, PlayerUidName targetPlayer, string newRoleCode)
	{
		ServerPlayerData orCreateServerPlayerData = server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
		if (orCreateServerPlayerData == null)
		{
			return TextCommandResult.Error(Lang.Get("No player with this playername found"));
		}
		if (caller.Player?.PlayerUID == orCreateServerPlayerData.PlayerUID)
		{
			return TextCommandResult.Error(Lang.Get("Can't change your own group"));
		}
		PlayerRole playerRole = null;
		foreach (KeyValuePair<string, PlayerRole> item in Config.RolesByCode)
		{
			if (item.Key.ToLowerInvariant() == newRoleCode.ToLowerInvariant())
			{
				playerRole = item.Value;
				break;
			}
		}
		if (playerRole == null)
		{
			return TextCommandResult.Error(Lang.Get("No group '{0}' found", newRoleCode));
		}
		string key = caller.CallerRole;
		if (caller.Player != null)
		{
			key = server.PlayerDataManager.GetPlayerDataByUid(caller.Player.PlayerUID).RoleCode;
		}
		Config.RolesByCode.TryGetValue(key, out var value);
		if (playerRole.IsSuperior(value) || (playerRole.EqualLevel(value) && !caller.HasPrivilege(Privilege.root)))
		{
			return TextCommandResult.Error(Lang.Get("Can only set lower role level than your own"));
		}
		PlayerRole playerRole2 = Config.RolesByCode[orCreateServerPlayerData.RoleCode];
		if (playerRole2.Code == playerRole.Code)
		{
			return TextCommandResult.Error(Lang.Get("Player is already in group {0}", playerRole2.Code));
		}
		if (playerRole2.IsSuperior(value) || (playerRole2.EqualLevel(value) && !caller.HasPrivilege(Privilege.root)))
		{
			return TextCommandResult.Error(Lang.Get("Can't modify a players role with a superior role. Players current role is {0}", playerRole2.Code));
		}
		orCreateServerPlayerData.SetRole(playerRole);
		server.PlayerDataManager.playerDataDirty = true;
		ServerMain.Logger.Audit($"{caller.GetName()} assigned {playerRole.Name} the role {targetPlayer.Name}.");
		ConnectedClient clientByPlayername = server.GetClientByPlayername(targetPlayer.Name);
		if (clientByPlayername != null)
		{
			server.SendOwnPlayerData(clientByPlayername.Player, sendInventory: false, sendPrivileges: true);
			string message = ((playerRole.PrivilegeLevel > playerRole2.PrivilegeLevel) ? Lang.Get("You've been promoted to role {0}", playerRole.Name) : Lang.Get("You've been demoted to role {0}", playerRole.Name));
			server.SendMessage(clientByPlayername.Player, GlobalConstants.CurrentChatGroup, message, EnumChatType.Notification);
			server.SendRoles(clientByPlayername.Player);
		}
		return TextCommandResult.Success(Lang.Get("Ok, role {0} assigned to {1}", playerRole.Name, targetPlayer.Name));
	}

	private TextCommandResult removeDenyPrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData orCreateServerPlayerData = server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
		string text = (string)args[1];
		string name = targetPlayer.Name;
		if (!orCreateServerPlayerData.DeniedPrivileges.Contains(text))
		{
			return TextCommandResult.Error(Lang.Get("Player {0} did not have this privilege denied.", name));
		}
		orCreateServerPlayerData.RemovePrivilegeDenial(text);
		string message = Lang.Get("{0} removed your Privilege denial for {1}", args.Caller.GetName(), text);
		ConnectedClient connectedClient = server.GetConnectedClient(targetPlayer.Uid);
		if (connectedClient != null)
		{
			server.SendMessage(connectedClient.Player, GlobalConstants.CurrentChatGroup, message, EnumChatType.Notification);
			server.SendOwnPlayerData(connectedClient.Player, sendInventory: false, sendPrivileges: true);
		}
		ServerMain.Logger.Audit($"{args.Caller.GetName()} no longer denied {targetPlayer.Name} the privilege {text}.");
		ServerMain.Logger.Event($"{args.Caller.GetName()} no longer denied {targetPlayer.Name} the privilege {text}.");
		return TextCommandResult.Success(Lang.Get("Privilege {0} is no longer denied from {1}", text, name));
	}

	private TextCommandResult denyPrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData orCreateServerPlayerData = server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
		string text = (string)args[1];
		string name = targetPlayer.Name;
		if (orCreateServerPlayerData.DeniedPrivileges.Contains(text))
		{
			return TextCommandResult.Error(Lang.Get("Player {0} already has this privilege denied.", name));
		}
		orCreateServerPlayerData.DenyPrivilege(text);
		string message = Lang.Get("{0} has denied Privilege {1}", args.Caller.GetName(), text);
		ConnectedClient connectedClient = server.GetConnectedClient(targetPlayer.Uid);
		if (connectedClient != null)
		{
			server.SendMessage(connectedClient.Player, GlobalConstants.CurrentChatGroup, message, EnumChatType.Notification);
			server.SendOwnPlayerData(connectedClient.Player, sendInventory: false, sendPrivileges: true);
		}
		ServerMain.Logger.Audit($"{args.Caller.GetName()} denied {targetPlayer.Name} the privilege {text}.");
		ServerMain.Logger.Event($"{args.Caller.GetName()} denied {targetPlayer.Name} the privilege {text}.");
		return TextCommandResult.Success(Lang.Get("Privilege {0} has been denied from {1}", text, name));
	}

	private TextCommandResult revokePrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData orCreateServerPlayerData = server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
		string text = (string)args[1];
		string name = targetPlayer.Name;
		if (!orCreateServerPlayerData.PermaPrivileges.Contains(text) && !orCreateServerPlayerData.HasPrivilege(text, server.Config.RolesByCode))
		{
			return TextCommandResult.Error(Lang.Get("Player {0} does not have this privilege neither directly or by role", name));
		}
		orCreateServerPlayerData.RevokePrivilege(text);
		string message = Lang.Get("{0} has revoked your Privilege {1}", args.Caller.GetName(), text);
		ConnectedClient connectedClient = server.GetConnectedClient(targetPlayer.Uid);
		if (connectedClient != null)
		{
			server.SendMessage(connectedClient.Player, GlobalConstants.CurrentChatGroup, message, EnumChatType.Notification);
			server.SendOwnPlayerData(connectedClient.Player, sendInventory: false, sendPrivileges: true);
		}
		ServerMain.Logger.Audit($"{args.Caller.GetName()} revoked {targetPlayer.Name} privilege {text}.");
		ServerMain.Logger.Event($"{args.Caller.GetName()} revoked {targetPlayer.Name} privilege {text}.");
		return TextCommandResult.Success(Lang.Get("Privilege {0} has been revoked from {1}", text, name));
	}

	private TextCommandResult grantPrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData orCreateServerPlayerData = server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
		string text = (string)args[1];
		string name = targetPlayer.Name;
		string text2 = "";
		if (orCreateServerPlayerData.DeniedPrivileges.Contains(text))
		{
			text2 = Lang.Get("Privilege deny for '{0}' removed from player {1}", text, name);
			ServerMain.Logger.Audit("{0} removed the privilege deny for '{1}' from player {2}", args.Caller.GetName(), text, name);
		}
		if (orCreateServerPlayerData.HasPrivilege(text, server.Config.RolesByCode))
		{
			if (text2.Length == 0)
			{
				return TextCommandResult.Error(Lang.Get("Player {0} has this privilege already", name));
			}
			return TextCommandResult.Success(text2);
		}
		orCreateServerPlayerData.GrantPrivilege(text);
		ConnectedClient connectedClient = server.GetConnectedClient(targetPlayer.Uid);
		if (connectedClient != null)
		{
			string message = Lang.Get("{0} granted you the privilege {1}", args.Caller.GetName(), text);
			server.SendMessage(connectedClient.Player, GlobalConstants.CurrentChatGroup, message, EnumChatType.Notification);
			server.SendOwnPlayerData(connectedClient.Player, sendInventory: false, sendPrivileges: true);
		}
		ServerMain.Logger.Audit("Player {0} granted {1} the privilege {2}", args.Caller.GetName(), name, text);
		ServerMain.Logger.Event($"{args.Caller.GetName()} grants {name} the privilege {text}.");
		return TextCommandResult.Success(Lang.Get("Privilege {0} granted to {1}", text, name));
	}

	private TextCommandResult listPrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		bool flag = targetPlayer.Uid == args.Caller.Player?.PlayerUID;
		if (!server.PlayerDataManager.PlayerDataByUid.ContainsKey(targetPlayer.Uid))
		{
			return TextCommandResult.Error(Lang.Get("This player is has never joined your server. He will have the privileges of the default role '{0}'.", server.Config.DefaultRoleCode));
		}
		ServerPlayerData serverPlayerData = server.PlayerDataManager.PlayerDataByUid[targetPlayer.Uid];
		HashSet<string> allPrivilegeCodes = serverPlayerData.GetAllPrivilegeCodes(server.Config);
		foreach (string deniedPrivilege in serverPlayerData.DeniedPrivileges)
		{
			allPrivilegeCodes.Remove(deniedPrivilege);
		}
		return TextCommandResult.Success(flag ? Lang.Get("You have {0} privileges: {1}", allPrivilegeCodes.Count, allPrivilegeCodes.Implode()) : Lang.Get("{0} has {1} privileges: {2}", targetPlayer.Name, allPrivilegeCodes.Count, allPrivilegeCodes.Implode()));
	}

	private TextCommandResult addRemoveWhitelist(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		if (args.Parsers[1].IsMissing)
		{
			bool flag = server.PlayerDataManager.WhitelistedPlayers.Any((PlayerEntry item) => item.PlayerUID == targetPlayer.Uid);
			return TextCommandResult.Success(Lang.Get("Player is currently {0}", flag ? "whitelisted" : "not whitelisted"));
		}
		bool num = (bool)args[1];
		string reason = (string)args[2];
		server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
		string byPlayername = ((args.Caller.Player != null) ? args.Caller.Player.PlayerName : args.Caller.Type.ToString());
		if (num)
		{
			DateTime dateTime = DateTime.Now.AddYears(50);
			server.PlayerDataManager.WhitelistPlayer(targetPlayer.Name, targetPlayer.Uid, byPlayername, reason, dateTime);
			return TextCommandResult.Success(Lang.Get("Player is now whitelisted until {0}", dateTime));
		}
		if (server.PlayerDataManager.UnWhitelistPlayer(targetPlayer.Name, targetPlayer.Uid))
		{
			return TextCommandResult.Success(Lang.Get("Player is now removed from the whitelist"));
		}
		return TextCommandResult.Error(Lang.Get("Player is not whitelisted"));
	}

	private TextCommandResult setMovespeed(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		IWorldPlayerData worldPlayerData = server.GetWorldPlayerData(targetPlayer.Uid);
		worldPlayerData.MoveSpeedMultiplier = (float)args[1];
		if (server.PlayerByUid(worldPlayerData.PlayerUID) is IServerPlayer serverPlayer)
		{
			serverPlayer.Entity.Controls.MovespeedMultiplier = worldPlayerData.MoveSpeedMultiplier;
			server.broadCastModeChange(serverPlayer);
		}
		return TextCommandResult.Success("Ok, movespeed set to " + worldPlayerData.MoveSpeedMultiplier);
	}

	public static TextCommandResult Each(TextCommandCallingArgs args, PlayerEachDelegate onPlayer)
	{
		PlayerUidName[] array = (PlayerUidName[])args.Parsers[0].GetValue();
		int num = 0;
		LimitedList<TextCommandResult> limitedList = new LimitedList<TextCommandResult>(10);
		if (array.Length == 0)
		{
			return TextCommandResult.Error(Lang.Get("No players found that match your selector"));
		}
		PlayerUidName[] array2 = array;
		foreach (PlayerUidName targetPlayer in array2)
		{
			TextCommandResult textCommandResult = onPlayer(targetPlayer, args);
			if (textCommandResult.Status == EnumCommandStatus.Success)
			{
				num++;
			}
			limitedList.Add(textCommandResult);
		}
		if (array.Length <= 10)
		{
			return TextCommandResult.Success(string.Join(", ", limitedList.Select((TextCommandResult el) => el.StatusMessage)));
		}
		return TextCommandResult.Success(Lang.Get("Successfully executed commands on {0}/{1} players", num, array.Length));
	}
}
