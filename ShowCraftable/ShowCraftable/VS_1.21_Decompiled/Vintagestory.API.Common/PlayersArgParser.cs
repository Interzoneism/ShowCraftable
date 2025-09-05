using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class PlayersArgParser : ArgumentParserBase
{
	protected ICoreServerAPI api;

	private PlayerUidName[] players;

	public PlayersArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.api = api as ICoreServerAPI;
		if (api.Side != EnumAppSide.Server)
		{
			throw new InvalidOperationException("Players arg parser is only available server side");
		}
	}

	public override string GetSyntaxExplanation(string indent)
	{
		return indent + GetSyntax() + " is the name or uid of one player, or a selector in this format: s[] for self, o[] for online players, a[] for all players.Some filters can be specified inside the brackets, though that doesn't make much sense for s[].Filters include name, namematches, group, role, range.";
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return base.GetValidRange(args).Append("or any other valid player name");
	}

	public override object GetValue()
	{
		return players;
	}

	public override void SetValue(object data)
	{
		players = (PlayerUidName[])data;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		base.PreProcess(args);
		players = null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		char? c = args.RawArgs.PeekChar();
		if (!c.HasValue)
		{
			lastErrorMessage = "Wrong selector, needs to be either character o (online players), a (all players - online or offline)";
			return EnumParseResult.Bad;
		}
		string text = args.RawArgs.PopWord();
		Dictionary<string, string> dictionary;
		if (text.Contains('['))
		{
			dictionary = parseSubArgs(text.Substring(1));
		}
		else
		{
			dictionary = new Dictionary<string, string>();
			dictionary["name"] = text;
			c = 'a';
		}
		List<PlayerUidName> list = new List<PlayerUidName>();
		string role = dictionary.Get("role");
		string name = dictionary.Get("name");
		string text2 = dictionary.Get("group");
		string namematches = dictionary.Get("namematches");
		float? range = dictionary.Get("range")?.ToFloat();
		switch (c)
		{
		case 's':
			if (args.Caller.Player == null)
			{
				throw new InvalidOperationException("s selector can only be used when the caller is a player.");
			}
			list.Add(new PlayerUidName(args.Caller.Player.PlayerUID, args.Caller.Player.PlayerName));
			break;
		case 'o':
		{
			IPlayer[] allOnlinePlayers = api.World.AllOnlinePlayers;
			for (int i = 0; i < allOnlinePlayers.Length; i++)
			{
				IServerPlayer serverPlayer = (IServerPlayer)allOnlinePlayers[i];
				if (matches(serverPlayer.Entity.Pos.XYZ, args.Caller.Pos, serverPlayer.PlayerName, serverPlayer.GetGroups(), serverPlayer.Role.Code, role, name, text2, namematches, range))
				{
					list.Add(new PlayerUidName(serverPlayer.PlayerUID, serverPlayer.PlayerName));
				}
			}
			break;
		}
		case 'a':
			if (range.HasValue)
			{
				throw new InvalidOperationException("Range arg can only be used on online players");
			}
			foreach (IServerPlayerData value in api.PlayerData.PlayerDataByUid.Values)
			{
				if (matches(null, null, value.LastKnownPlayername, value.PlayerGroupMemberships.Values.ToArray(), value.RoleCode, role, name, text2, namematches, null))
				{
					list.Add(new PlayerUidName(value.PlayerUID, value.LastKnownPlayername));
				}
				else if (value.PlayerUID.Equals(name))
				{
					list.Add(new PlayerUidName(value.PlayerUID, value.LastKnownPlayername));
				}
			}
			break;
		default:
			lastErrorMessage = "Wrong selector, needs to be either character o (online players), a (all players - online or offline)";
			return EnumParseResult.Bad;
		}
		if (list.Count == 0 && name != null)
		{
			api.PlayerData.ResolvePlayerName(name, delegate(EnumServerResponse resp, string uid)
			{
				if (resp == EnumServerResponse.Good && !string.IsNullOrEmpty(uid))
				{
					onReady(new AsyncParseResults
					{
						Status = EnumParseResultStatus.Ready,
						Data = new PlayerUidName[1]
						{
							new PlayerUidName(uid, name)
						}
					});
				}
				else
				{
					api.PlayerData.ResolvePlayerUid(name, delegate(EnumServerResponse enumServerResponse, string playername)
					{
						if (enumServerResponse == EnumServerResponse.Good)
						{
							onReady(new AsyncParseResults
							{
								Status = EnumParseResultStatus.Ready,
								Data = new PlayerUidName[1]
								{
									new PlayerUidName(name, playername)
								}
							});
						}
						else
						{
							lastErrorMessage = Lang.Get("No player with name or uid '{0}' exists", name);
							onReady(new AsyncParseResults
							{
								Status = EnumParseResultStatus.Error
							});
						}
					});
				}
			});
			return EnumParseResult.Deferred;
		}
		players = list.ToArray();
		return EnumParseResult.Good;
	}

	private bool matches(Vec3d pos, Vec3d callerPos, string playerName, PlayerGroupMembership[] plrGroups, string plrRoleCode, string role, string name, string group, string namematches, float? range)
	{
		if (name != null)
		{
			return playerName == name;
		}
		if (namematches != null && !WildcardUtil.Match(namematches, playerName))
		{
			return false;
		}
		if (role != null && plrRoleCode != role)
		{
			return false;
		}
		if (range.HasValue && pos.DistanceTo(callerPos) > range.Value)
		{
			return false;
		}
		if (group != null && plrGroups.Where((PlayerGroupMembership pgm) => pgm.GroupName == group).Count() == 0)
		{
			return false;
		}
		return true;
	}
}
