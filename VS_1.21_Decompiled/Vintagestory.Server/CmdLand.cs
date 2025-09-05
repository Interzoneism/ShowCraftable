using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class CmdLand
{
	public delegate TextCommandResult ClaimInProgressHandlerDelegate(TextCommandCallingArgs args, ClaimInProgress claimp);

	private ServerMain server;

	private Dictionary<IPlayer, ClaimInProgress> TempClaims = new Dictionary<IPlayer, ClaimInProgress>();

	private int claimedColor = ColorUtil.ToRgba(64, 100, 255, 100);

	private int claimingColor = ColorUtil.ToRgba(64, 148, 210, 246);

	public CmdLand(ServerMain server)
	{
		CmdLand cmdLand = this;
		this.server = server;
		IChatCommandApi chatCommands = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		chatCommands.GetOrCreate("land").RequiresPrivilege(Privilege.chat).RequiresPlayer()
			.WithDesc("Manage land rights")
			.WithPreCondition((TextCommandCallingArgs args) => (!server.SaveGameData.WorldConfiguration.GetBool("allowLandClaiming", defaultValue: true)) ? TextCommandResult.Error(Lang.Get("Land claiming has been disabled by world configuration")) : TextCommandResult.Success())
			.BeginSub("free")
			.WithArgs(parsers.Int("claim id"), parsers.OptionalBool("confirm", "confirm"))
			.HandleWith((TextCommandCallingArgs args) => cmdLand.freeLand(args.Caller.Player as IServerPlayer, (int)args[0], (bool)args[1]))
			.WithDesc("Remove a land claim of yours")
			.EndSub()
			.BeginSub("adminfree")
			.RequiresPrivilege(Privilege.commandplayer)
			.WithArgs(parsers.PlayerUids("player name"))
			.WithDesc("Delete all claims of selected player(s)")
			.HandleWith(freeLandAdmin)
			.EndSub()
			.BeginSub("adminfreehere")
			.RequiresPrivilege(Privilege.commandplayer)
			.WithDesc("Remove a land claim at the calling position")
			.HandleWith(freeLandAdminHere)
			.EndSub()
			.BeginSub("list")
			.WithDesc("List your claimed lands or retrieve information about a claim")
			.WithArgs(parsers.OptionalInt("land claim index"))
			.HandleWith((TextCommandCallingArgs args) => cmdLand.landList(args.Caller.Player as IServerPlayer, args.Parsers[0].IsMissing ? ((int?)null) : ((int?)args[0])))
			.EndSub()
			.BeginSub("info")
			.WithDesc("Land rights information at your location")
			.HandleWith((TextCommandCallingArgs args) => cmdLand.landInfo(args.Caller.Player as IServerPlayer))
			.EndSub()
			.BeginSub("claim")
			.RequiresPrivilege(Privilege.claimland)
			.WithDesc("Add, Remove or Modify your claims")
			.BeginSub("load")
			.WithDesc("Load an existing claim")
			.WithArgs(parsers.Int("claim id"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
				List<LandClaim> playerClaims = GetPlayerClaims(server, serverPlayer.PlayerUID);
				int num = (int)args[0];
				if (num < 0 || num >= playerClaims.Count)
				{
					return TextCommandResult.Error(Lang.Get("Incorrect claimid, you only have {0} claims", playerClaims.Count));
				}
				cmdLand.TempClaims[serverPlayer] = new ClaimInProgress
				{
					Claim = playerClaims[num].Clone(),
					IsNew = false,
					OriginalClaim = playerClaims[num]
				};
				cmdLand.ResendHighlights(serverPlayer, cmdLand.TempClaims[serverPlayer].Claim);
				return TextCommandResult.Success(Lang.Get("Ok, claim loaded, you can now modify it", serverPlayer.Role.LandClaimMaxAreas));
			})
			.EndSub()
			.BeginSub("new")
			.WithDesc("Create a new claim")
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
				if (GetPlayerClaims(server, serverPlayer.PlayerUID).Count >= serverPlayer.Role.LandClaimMaxAreas + serverPlayer.ServerData.ExtraLandClaimAreas)
				{
					return TextCommandResult.Error(Lang.Get("Sorry you can't have more than {0} separate claims", serverPlayer.Role.LandClaimMaxAreas));
				}
				ClaimInProgress claimInProgress = new ClaimInProgress
				{
					Claim = LandClaim.CreateClaim(serverPlayer, serverPlayer.Role.PrivilegeLevel),
					IsNew = true
				};
				cmdLand.TempClaims[serverPlayer] = claimInProgress;
				claimInProgress.Start = serverPlayer.Entity.Pos.XYZ.AsBlockPos;
				cmdLand.ResendHighlights(serverPlayer, claimInProgress.Claim);
				return TextCommandResult.Success(Lang.Get("Ok new claim initiated, use /land claim start, then /land claim end to mark an area, you can use /land claim grow [up|north|east|...] [size] to grow/shrink the selection, if you messed up use /land claim cancel, then finally /land claim add to add that area. You can add multiple areas as long as they are adjacent. Once all is ready, use /land claim save [text] to save the claim"));
			})
			.EndSub()
			.BeginSub("grant")
			.WithDesc("Grant a player access to your claim")
			.WithArgs(parsers.PlayerUids("for player"), parsers.WordRange("permission type", "traverse", "use", "all"))
			.HandleWith((TextCommandCallingArgs ccargs) => CmdPlayer.Each(ccargs, cmdLand.handleGrant))
			.EndSub()
			.BeginSub("revoke")
			.WithDesc("Revoke a player access on your claim")
			.WithArgs(parsers.PlayerUids("for player"))
			.HandleWith((TextCommandCallingArgs ccargs) => CmdPlayer.Each(ccargs, cmdLand.handleRevoke))
			.EndSub()
			.BeginSub("grantgroup")
			.WithDesc("Grant a group access to your claim")
			.WithArgs(parsers.Word("group name"), parsers.WordRange("permission type", "traverse", "use", "all"))
			.HandleWith(handleGrantGroup)
			.EndSub()
			.BeginSub("revokegroup")
			.WithDesc("Revoke a group access on your claim")
			.WithArgs(parsers.Word("group name"))
			.HandleWith(handleRevokeGroup)
			.EndSub()
			.BeginSub("grow")
			.WithDesc("Grow area in one of 6 directions (up/down/north/east/south/west)")
			.WithArgs(parsers.WordRange("direction", "up", "down", "north", "east", "south", "west"), parsers.OptionalInt("amount", 1))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, (TextCommandCallingArgs args, ClaimInProgress claimp) => cmdLand.GrowSelection(args.Caller.Player, claimp, BlockFacing.FromCode((string)args[0]), (int)args[1])))
			.EndSub()
			.BeginSubs("gu", "gd", "gn", "ge", "gs", "gw")
			.WithDesc("Grow area in one of 6 directions (gu/gd/gn/ge/gs/gw)")
			.WithArgs(parsers.OptionalInt("amount", 1))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, (TextCommandCallingArgs args, ClaimInProgress claimp) => cmdLand.GrowSelection(args.Caller.Player, claimp, BlockFacing.FromFirstLetter(cargs.SubCmdCode[1]), (int)args[0])))
			.EndSub()
			.BeginSub("shrink")
			.WithDesc("Shrink area in one of 6 directions (up/down/north/east/south/west)")
			.WithArgs(parsers.WordRange("direction", "up", "down", "north", "east", "south", "west"), parsers.OptionalInt("amount", 1))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, (TextCommandCallingArgs args, ClaimInProgress claimp) => cmdLand.GrowSelection(args.Caller.Player, claimp, BlockFacing.FromCode((string)args[0]), -(int)args[1])))
			.EndSub()
			.BeginSubs("su", "sd", "sn", "se", "ss", "sw")
			.WithDesc("Shrink area in one of 6 directions (su/sd/sn/se/ss/sw)")
			.WithArgs(parsers.OptionalInt("amount", 1))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, (TextCommandCallingArgs args, ClaimInProgress claimp) => cmdLand.GrowSelection(args.Caller.Player, claimp, BlockFacing.FromFirstLetter(cargs.SubCmdCode[1]), -(int)args[0])))
			.EndSub()
			.BeginSub("start")
			.WithDesc("Set a start position for an area")
			.WithArgs(parsers.OptionalWorldPosition("position"))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				claimp.Start = (args[0] as Vec3d).AsBlockPos;
				cmdLand.ResendHighlights(args.Caller.Player, claimp.Claim, claimp.Start, claimp.End);
				return TextCommandResult.Success(Lang.Get("Ok, Land claim start position {0} set", claimp.Start.ToLocalPosition(server.api)));
			}))
			.EndSub()
			.BeginSub("end")
			.WithDesc("Set a end position for an area")
			.WithArgs(parsers.OptionalWorldPosition("position"))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				claimp.End = (args[0] as Vec3d).AsBlockPos;
				cmdLand.ResendHighlights(args.Caller.Player, claimp.Claim, claimp.Start, claimp.End);
				return TextCommandResult.Success(Lang.Get("Ok, Land claim end position {0} set", claimp.End.ToLocalPosition(server.api)));
			}))
			.EndSub()
			.BeginSub("add")
			.WithDesc("Add current area to the claim")
			.HandleWith(addCurrentArea)
			.EndSub()
			.BeginSub("allowuseeveryone")
			.WithDesc("Grant use privilege to all players")
			.WithArgs(parsers.Bool("on/off"))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				claimp.Claim.AllowUseEveryone = (bool)args[0];
				return TextCommandResult.Success(Lang.Get("Ok, allow use everyone is now {0}", claimp.Claim.AllowUseEveryone ? "on" : "off"));
			}))
			.EndSub()
			.BeginSub("allowtraverseveryone")
			.WithDesc("Grant traverse privilege to all players")
			.WithArgs(parsers.Bool("on/off"))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				claimp.Claim.AllowTraverseEveryone = (bool)args[0];
				return TextCommandResult.Success(Lang.Get("Ok, allow traverse everyone is now {0}", claimp.Claim.AllowTraverseEveryone ? "on" : "off"));
			}))
			.EndSub()
			.BeginSub("plevel")
			.WithDesc("Set protection level on your current claim")
			.WithArgs(parsers.Int("protection level"))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				claimp.Claim.ProtectionLevel = (int)args[0];
				return TextCommandResult.Success(Lang.Get("Ok, protection level set to {0}", claimp.Claim.ProtectionLevel));
			}))
			.EndSub()
			.BeginSub("fullheight")
			.WithDesc("Expand claim to cover the entire map height")
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				if (claimp.Start == null || claimp.End == null)
				{
					return TextCommandResult.Error(Lang.Get("Define start and end position first"));
				}
				claimp.Start.Y = 0;
				claimp.End.Y = server.WorldMap.MapSizeY;
				cmdLand.ResendHighlights(args.Caller.Player, claimp.Claim, claimp.Start, claimp.End);
				return TextCommandResult.Success(Lang.Get("Ok, extended land claim to cover full world height"));
			}))
			.EndSub()
			.BeginSub("save")
			.WithDesc("Save your currently edited claim")
			.WithArgs(parsers.All("description"))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				if (claimp.Claim.Areas.Count == 0)
				{
					return TextCommandResult.Error(Lang.Get("Cannot save an empty claim. Did you forget to type /land claim add?"));
				}
				claimp.Claim.Description = (string)args[0];
				if (claimp.IsNew)
				{
					server.WorldMap.Add(claimp.Claim);
				}
				else
				{
					server.WorldMap.UpdateClaim(claimp.OriginalClaim, claimp.Claim);
				}
				IPlayer player = args.Caller.Player;
				cmdLand.TempClaims[player] = null;
				cmdLand.ResendHighlights(player, null);
				return TextCommandResult.Success("Ok, Land claim saved on your name");
			}))
			.EndSub()
			.BeginSub("cancel")
			.WithDesc("Discard changes on currently edited claim")
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				IPlayer player = args.Caller.Player;
				if (!cmdLand.TempClaims.ContainsKey(player))
				{
					return TextCommandResult.Error("No current land claim changes active");
				}
				cmdLand.TempClaims[player] = null;
				cmdLand.ResendHighlights(player, null);
				return TextCommandResult.Success("Ok, Land claim changes cancelled");
			}))
			.EndSub()
			.EndSub()
			.Validate();
	}

	private TextCommandResult handleRevokeGroup(TextCommandCallingArgs cargs)
	{
		return acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
		{
			string text = (string)args[0];
			PlayerGroup playerGroupByName = server.PlayerDataManager.GetPlayerGroupByName(text);
			if (playerGroupByName != null && claimp.Claim.PermittedPlayerGroupIds.ContainsKey(playerGroupByName.Uid))
			{
				claimp.Claim.PermittedPlayerGroupIds.Remove(playerGroupByName.Uid);
				return TextCommandResult.Success("Ok, revoked access to group " + text);
			}
			return TextCommandResult.Error("No such group has access to your claim");
		});
	}

	private TextCommandResult handleGrantGroup(TextCommandCallingArgs cargs)
	{
		return acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
		{
			string text = (string)args[0];
			EnumBlockAccessFlags value = EnumBlockAccessFlags.Use | EnumBlockAccessFlags.Traverse;
			string text2 = (string)args[1];
			if (text2 == "all")
			{
				value = EnumBlockAccessFlags.BuildOrBreak | EnumBlockAccessFlags.Use | EnumBlockAccessFlags.Traverse;
			}
			else if (text2 == "traverse")
			{
				value = EnumBlockAccessFlags.Traverse;
			}
			PlayerGroup playerGroupByName = server.PlayerDataManager.GetPlayerGroupByName(text);
			if (playerGroupByName != null)
			{
				claimp.Claim.PermittedPlayerGroupIds[playerGroupByName.Uid] = value;
				return TextCommandResult.Success("Ok, granted access to group " + text);
			}
			return TextCommandResult.Error("No such group found");
		});
	}

	private TextCommandResult handleGrant(PlayerUidName forPlayer, TextCommandCallingArgs cargs)
	{
		return acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
		{
			EnumBlockAccessFlags value = EnumBlockAccessFlags.Use | EnumBlockAccessFlags.Traverse;
			string text = (string)args[1];
			if (text == "all")
			{
				value = EnumBlockAccessFlags.BuildOrBreak | EnumBlockAccessFlags.Use | EnumBlockAccessFlags.Traverse;
			}
			else if (text == "traverse")
			{
				value = EnumBlockAccessFlags.Traverse;
			}
			claimp.Claim.PermittedPlayerUids[forPlayer.Uid] = value;
			claimp.Claim.PermittedPlayerLastKnownPlayerName[forPlayer.Uid] = forPlayer.Name;
			return TextCommandResult.Success(Lang.Get("Ok, player {0} granted {1} access to your claim.", forPlayer.Name, args[1]));
		});
	}

	private TextCommandResult handleRevoke(PlayerUidName forPlayer, TextCommandCallingArgs cargs)
	{
		return acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
		{
			if (claimp.Claim.PermittedPlayerUids.ContainsKey(forPlayer.Uid))
			{
				claimp.Claim.PermittedPlayerUids.Remove(forPlayer.Uid);
				return TextCommandResult.Success(Lang.Get("Ok, revoked access to player {0}.", forPlayer.Name));
			}
			return TextCommandResult.Success(Lang.Get("Player {0} had no access to your claim.", forPlayer.Name));
		});
	}

	private TextCommandResult acquireClaimInProgress(TextCommandCallingArgs cargs, ClaimInProgressHandlerDelegate handler)
	{
		if (TempClaims.TryGetValue(cargs.Caller.Player, out var value) && value != null)
		{
			return handler(cargs, value);
		}
		return TextCommandResult.Success(Lang.Get("No current or incomplete claim, type '/land claim new' to prepare a new one or '/land claim load [id]' to modify an existing one. The id can be retrieved from /land list"));
	}

	private TextCommandResult landInfo(IServerPlayer player)
	{
		List<LandClaim> all = server.WorldMap.All;
		List<string> list = new List<string>();
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		foreach (LandClaim item in all)
		{
			if (item.PositionInside(player.Entity.ServerPos.XYZ))
			{
				list.Add(item.LastKnownOwnerName);
				flag4 = true;
				if (item.TestPlayerAccess(player, EnumBlockAccessFlags.BuildOrBreak) != EnumPlayerAccessResult.Denied)
				{
					flag = true;
				}
				if (item.TestPlayerAccess(player, EnumBlockAccessFlags.Use) != EnumPlayerAccessResult.Denied)
				{
					flag2 = true;
				}
				break;
			}
		}
		long key = server.WorldMap.MapRegionIndex2D(player.Entity.ServerPos.XInt / server.WorldMap.RegionSize, player.Entity.ServerPos.ZInt / server.WorldMap.RegionSize);
		if (server.WorldMap.LandClaimByRegion.ContainsKey(key))
		{
			foreach (LandClaim item2 in server.WorldMap.LandClaimByRegion[key])
			{
				if (item2.PositionInside(player.Entity.ServerPos.XYZ))
				{
					flag3 = true;
					break;
				}
			}
		}
		if (flag3 != flag4)
		{
			return TextCommandResult.Error($"Incorrect state. Spatially partitioned claim list not consistent with full claim list. Please contact the game developer. A server restart may temporarily fix the issue. (in partition: {flag3}, in listclaim: {flag4})");
		}
		string text = "";
		if (player.HasPrivilege(Privilege.readlists))
		{
			foreach (LandClaim item3 in all)
			{
				if (!item3.PositionInside(player.Entity.ServerPos.XYZ))
				{
					continue;
				}
				int protectionLevel = item3.ProtectionLevel;
				StringBuilder stringBuilder = new StringBuilder();
				foreach (KeyValuePair<string, EnumBlockAccessFlags> permittedPlayerUid in item3.PermittedPlayerUids)
				{
					if (stringBuilder.Length > 0)
					{
						stringBuilder.Append(", ");
					}
					ServerPlayerData serverPlayerData = server.GetServerPlayerData(permittedPlayerUid.Key);
					if (serverPlayerData != null)
					{
						stringBuilder.Append($"{serverPlayerData.LastKnownPlayername} can {permittedPlayerUid.Value}");
					}
					else
					{
						stringBuilder.Append($"{permittedPlayerUid.Key} can {permittedPlayerUid.Value}");
					}
				}
				if (stringBuilder.Length == 0)
				{
					stringBuilder.Append("None.");
				}
				StringBuilder stringBuilder2 = new StringBuilder();
				foreach (KeyValuePair<int, EnumBlockAccessFlags> permittedPlayerGroupId in item3.PermittedPlayerGroupIds)
				{
					if (stringBuilder2.Length > 0)
					{
						stringBuilder2.Append(", ");
					}
					server.PlayerDataManager.PlayerGroupsById.TryGetValue(permittedPlayerGroupId.Key, out var value);
					if (value != null)
					{
						stringBuilder2.Append($"{value.Name} can {permittedPlayerGroupId.Value}");
					}
					else
					{
						stringBuilder2.Append($"{permittedPlayerGroupId.Key} can {permittedPlayerGroupId.Value}");
					}
				}
				if (stringBuilder2.Length == 0)
				{
					stringBuilder2.Append("None.");
				}
				text = "\n" + $"AllowUseEveryone: {item3.AllowUseEveryone}, AllowTraverseEveryone: {item3.AllowTraverseEveryone}" + "\n" + $"Protection level: {protectionLevel}, Granted Players: {stringBuilder.ToString()}, Granted Groups: {stringBuilder2.ToString()}";
			}
		}
		if (list.Count > 0)
		{
			string text2 = Lang.Get("You don't have access to it.");
			if (flag && flag2)
			{
				text2 = Lang.Get("You have build and use access.");
			}
			else
			{
				if (flag)
				{
					text2 = Lang.Get("You have build access.");
				}
				if (flag2)
				{
					text2 = Lang.Get("You have use access.");
				}
			}
			return TextCommandResult.Success(Lang.Get("These lands are claimed by {0}. {1}", string.Join(", ", list), text2) + text);
		}
		return TextCommandResult.Success(Lang.Get("These lands are not claimed by anybody") + text);
	}

	private TextCommandResult freeLand(IServerPlayer player, int claimid, bool confirm)
	{
		List<LandClaim> all = server.WorldMap.All;
		List<LandClaim> list = new List<LandClaim>();
		foreach (LandClaim item in all)
		{
			if (item.OwnedByPlayerUid == player.PlayerUID)
			{
				list.Add(item);
			}
		}
		if (claimid < 0 || claimid >= list.Count)
		{
			return TextCommandResult.Error(Lang.Get("Claim number too wrong, you only have {0} claims", list.Count));
		}
		LandClaim landClaim = list[claimid];
		if (!confirm)
		{
			return TextCommandResult.Success(Lang.Get("command-deleteclaim-confirmation", landClaim.Description, landClaim.SizeXYZ, claimid));
		}
		server.WorldMap.Remove(landClaim);
		return TextCommandResult.Success(Lang.Get("Ok, claim removed"));
	}

	private TextCommandResult freeLandAdmin(TextCommandCallingArgs args)
	{
		PlayerUidName[] obj = (PlayerUidName[])args[0];
		List<LandClaim> all = server.WorldMap.All;
		int num = 0;
		List<string> list = new List<string>();
		PlayerUidName[] array = obj;
		foreach (PlayerUidName playerUidName in array)
		{
			list.Add(playerUidName.Name);
			foreach (LandClaim item in new List<LandClaim>(all))
			{
				if (item.OwnedByPlayerUid == playerUidName.Uid && server.WorldMap.Remove(item))
				{
					num++;
				}
			}
		}
		return TextCommandResult.Success(Lang.Get("Ok, {0} claims removed from {1}", num, string.Join(", ", list)));
	}

	private TextCommandResult freeLandAdminHere(TextCommandCallingArgs args)
	{
		Vec3d pos = args.Caller.Pos;
		long key = server.WorldMap.MapRegionIndex2D(pos.XInt / server.WorldMap.RegionSize, pos.ZInt / server.WorldMap.RegionSize);
		if (server.WorldMap.LandClaimByRegion.ContainsKey(key))
		{
			foreach (LandClaim item in server.WorldMap.LandClaimByRegion[key])
			{
				if (item.PositionInside(pos))
				{
					server.WorldMap.Remove(item);
					return TextCommandResult.Success(Lang.Get("Ok, Removed claim from {0}", item.LastKnownOwnerName));
				}
			}
		}
		return TextCommandResult.Error(Lang.Get("No claim found at this position"), "nonefound");
	}

	private TextCommandResult landList(IServerPlayer player, int? index)
	{
		List<LandClaim> all = server.WorldMap.All;
		if (index.HasValue)
		{
			LandClaim landClaim = null;
			int num = 0;
			int value = index.Value;
			foreach (LandClaim item in all)
			{
				if (!(item.OwnedByPlayerUid != player.PlayerUID))
				{
					if (value == num)
					{
						landClaim = item;
						break;
					}
					num++;
				}
			}
			if (landClaim == null)
			{
				return TextCommandResult.Error("No such claim");
			}
			BlockPos center = landClaim.Center;
			center = center.Copy().Sub(server.DefaultSpawnPosition.XYZ.AsBlockPos);
			string text = Lang.Get("{0} ({1}m³ at {2})", landClaim.Description, landClaim.SizeXYZ, center);
			StringBuilder stringBuilder = new StringBuilder();
			if (landClaim.PermittedPlayerUids.Count > 0)
			{
				foreach (KeyValuePair<string, EnumBlockAccessFlags> permittedPlayerUid in landClaim.PermittedPlayerUids)
				{
					string key = permittedPlayerUid.Key;
					if (!landClaim.PermittedPlayerLastKnownPlayerName.TryGetValue(key, out var value2))
					{
						value2 = key;
					}
					bool flag = (permittedPlayerUid.Value & EnumBlockAccessFlags.BuildOrBreak) > EnumBlockAccessFlags.None;
					bool flag2 = (permittedPlayerUid.Value & EnumBlockAccessFlags.Use) > EnumBlockAccessFlags.None;
					string value3 = ((flag && flag2) ? Lang.Get("Player {0} can build/break and use blocks", value2) : (flag ? Lang.Get("Player {0} can build/break but not use blocks", value2) : Lang.Get("Player {0} can use but not build/break blocks", value2)));
					stringBuilder.AppendLine(value3);
				}
			}
			if (landClaim.PermittedPlayerGroupIds.Count > 0)
			{
				Dictionary<int, PlayerGroup> playerGroupsById = server.PlayerDataManager.PlayerGroupsById;
				foreach (KeyValuePair<int, EnumBlockAccessFlags> permittedPlayerGroupId in landClaim.PermittedPlayerGroupIds)
				{
					int key2 = permittedPlayerGroupId.Key;
					if (playerGroupsById.TryGetValue(key2, out var value4))
					{
						bool flag3 = (permittedPlayerGroupId.Value & EnumBlockAccessFlags.BuildOrBreak) > EnumBlockAccessFlags.None;
						bool flag4 = (permittedPlayerGroupId.Value & EnumBlockAccessFlags.Use) > EnumBlockAccessFlags.None;
						string value5 = ((flag3 && flag4) ? Lang.Get("Group {0} can build/break and use blocks", value4.Name) : (flag3 ? Lang.Get("Group {0} can build/break but not use blocks", value4.Name) : Lang.Get("Group {0} can use but not build/break blocks", value4.Name)));
						stringBuilder.AppendLine(value5);
					}
				}
			}
			return TextCommandResult.Success(text + "\r\n" + ((stringBuilder.Length == 0) ? Lang.Get("No other players/groups have access to this claim") : stringBuilder.ToString()));
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		int num2 = 0;
		PlayerGroupMembership[] groups = player.Groups;
		bool flag5 = server.api.World.Config.GetBool("allowCoordinateHud", defaultValue: true);
		foreach (LandClaim claim in all)
		{
			BlockPos center2 = claim.Center;
			center2 = center2.Copy().Sub(server.DefaultSpawnPosition.XYZ.AsBlockPos);
			if (claim.OwnedByPlayerUid == player.PlayerUID)
			{
				if (flag5)
				{
					list.Add(Lang.Get("{0}: {1} ({2}m³ at {3})", num2, claim.Description, claim.SizeXYZ, center2));
				}
				else
				{
					list.Add(Lang.Get("{0}: {1} ({2}m³)", num2, claim.Description, claim.SizeXYZ));
				}
				num2++;
			}
			if (groups.Any((PlayerGroupMembership g) => g.GroupName.Equals(claim.OwnedByPlayerGroupUid)))
			{
				if (flag5)
				{
					list2.Add(Lang.Get("{0}: {1} ({2}m³ at {3}) (group owned)", num2, claim.Description, claim.SizeXYZ, center2));
				}
				else
				{
					list2.Add(Lang.Get("{0}: {1} ({2}m³) (group owned)", num2, claim.Description, claim.SizeXYZ));
				}
			}
		}
		return TextCommandResult.Success(Lang.Get("land-claim-list", string.Join("\n", list)));
	}

	private TextCommandResult addCurrentArea(TextCommandCallingArgs cargs)
	{
		List<LandClaim> allclaims = server.WorldMap.All;
		IServerPlayer fromPlayer = cargs.Caller.Player as IServerPlayer;
		List<LandClaim> ownclaims = GetPlayerClaims(server, cargs.Caller.Player.PlayerUID);
		return acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
		{
			if (claimp.Start == null || claimp.End == null)
			{
				return TextCommandResult.Error(Lang.Get("Start or End not marked"));
			}
			Cuboidi cuboidi = new Cuboidi(claimp.Start, claimp.End);
			if (cuboidi.SizeX < fromPlayer.Role.LandClaimMinSize.X || cuboidi.SizeY < fromPlayer.Role.LandClaimMinSize.Y || cuboidi.SizeZ < fromPlayer.Role.LandClaimMinSize.Z)
			{
				return TextCommandResult.Error(Lang.Get("Cannot add area. Your marked area has a size of {0}x{1}x{2} which is to small, needs to be at least {3}x{4}x{5}", cuboidi.SizeX, cuboidi.SizeY, cuboidi.SizeZ, fromPlayer.Role.LandClaimMinSize.X, fromPlayer.Role.LandClaimMinSize.Y, fromPlayer.Role.LandClaimMinSize.Z));
			}
			int num = cuboidi.SizeXYZ;
			foreach (LandClaim item in ownclaims)
			{
				num += item.SizeXYZ;
			}
			if (num > (long)fromPlayer.Role.LandClaimAllowance + (long)fromPlayer.ServerData.ExtraLandClaimAllowance)
			{
				return TextCommandResult.Error(Lang.Get("Cannot add area. Adding this area of size {0}m³ would bring your total claim size up to {1}m³, but your max allowance is {2}m³", cuboidi.SizeXYZ, num, fromPlayer.Role.LandClaimAllowance));
			}
			for (int i = 0; i < allclaims.Count; i++)
			{
				if (allclaims[i].Intersects(cuboidi))
				{
					return TextCommandResult.Error(Lang.Get("Cannot add area. This area overlaps with with another claim by {0}. Please correct your start/end position", allclaims[i].LastKnownOwnerName));
				}
			}
			EnumClaimError enumClaimError = claimp.Claim.AddArea(cuboidi);
			if (enumClaimError != EnumClaimError.NoError)
			{
				return TextCommandResult.Error((enumClaimError == EnumClaimError.Overlapping) ? Lang.Get("Cannot add area. This area overlaps with your other claims. Please correct your start/end position") : Lang.Get("Cannot add area. This area is not adjacent to other claims. Please correct your start/end position"));
			}
			claimp.Start = null;
			claimp.End = null;
			ResendHighlights(fromPlayer, claimp.Claim, claimp.Start, claimp.End);
			return TextCommandResult.Success(Lang.Get("Ok, Land claim area added"));
		});
	}

	private TextCommandResult GrowSelection(IPlayer plr, ClaimInProgress claimp, BlockFacing facing, int size)
	{
		if (claimp.Start == null || claimp.End == null)
		{
			return TextCommandResult.Error(Lang.Get("Define start and end position first"));
		}
		if (facing == BlockFacing.UP)
		{
			if (claimp.Start.Y < claimp.End.Y)
			{
				claimp.End.Y += size;
			}
			else
			{
				claimp.Start.Y += size;
			}
		}
		if (facing == BlockFacing.DOWN)
		{
			if (claimp.Start.Y < claimp.End.Y)
			{
				claimp.Start.Y -= size;
			}
			else
			{
				claimp.End.Y -= size;
			}
		}
		if (facing == BlockFacing.NORTH)
		{
			if (claimp.Start.Z < claimp.End.Z)
			{
				claimp.Start.Z -= size;
			}
			else
			{
				claimp.End.Z -= size;
			}
		}
		if (facing == BlockFacing.EAST)
		{
			if (claimp.Start.X > claimp.End.X)
			{
				claimp.Start.X += size;
			}
			else
			{
				claimp.End.X += size;
			}
		}
		if (facing == BlockFacing.WEST)
		{
			if (claimp.Start.X < claimp.End.X)
			{
				claimp.Start.X -= size;
			}
			else
			{
				claimp.End.X -= size;
			}
		}
		if (facing == BlockFacing.SOUTH)
		{
			if (claimp.Start.Z > claimp.End.Z)
			{
				claimp.Start.Z += size;
			}
			else
			{
				claimp.End.Z += size;
			}
		}
		ResendHighlights(plr, claimp.Claim, claimp.Start, claimp.End);
		return TextCommandResult.Success(Lang.Get("Ok, area extended {0} by {1} blocks", facing, size));
	}

	private void ResendHighlights(IPlayer toPlayer, LandClaim claim)
	{
		ResendHighlights(toPlayer, claim, null, null);
	}

	private void ResendHighlights(IPlayer toPlayer, LandClaim claim, BlockPos claimingStartPos, BlockPos claimingEndPos)
	{
		List<BlockPos> list = new List<BlockPos>();
		List<int> list2 = new List<int>();
		if (claim != null)
		{
			foreach (Cuboidi area in claim.Areas)
			{
				list.Add(area.Start.ToBlockPos());
				list.Add(area.End.ToBlockPos());
				list2.Add(claimedColor);
			}
		}
		if (claimingStartPos != null && claimingEndPos != null)
		{
			list.Add(claimingStartPos);
			list.Add(claimingEndPos);
			list2.Add(claimingColor);
		}
		server.api.World.HighlightBlocks(toPlayer, 3, list, list2, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);
	}

	public static List<LandClaim> GetPlayerClaims(ServerMain server, string playerUid)
	{
		List<LandClaim> all = server.WorldMap.All;
		List<LandClaim> list = new List<LandClaim>();
		foreach (LandClaim item in all)
		{
			if (item.OwnedByPlayerUid == playerUid)
			{
				list.Add(item);
			}
		}
		return list;
	}
}
