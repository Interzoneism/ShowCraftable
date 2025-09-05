using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public abstract class PositionArgumentParserBase : ArgumentParserBase
{
	protected PositionArgumentParserBase(string argName, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
	}

	protected EnumParseResult tryGetPositionBySelector(char v, TextCommandCallingArgs args, ref Vec3d pos, ICoreAPI api)
	{
		string strargs = args.RawArgs.PopWord();
		Dictionary<string, string> dictionary = parseSubArgs(strargs);
		Vec3d pos2 = args.Caller.Pos;
		Entity entity = args.Caller.Entity;
		float? num = null;
		if (dictionary.TryGetValue("range", out var value))
		{
			num = value.ToFloat();
		}
		AssetLocation assetLocation = null;
		if (dictionary.TryGetValue("type", out var value2))
		{
			assetLocation = new AssetLocation(value2);
		}
		dictionary.TryGetValue("name", out var value3);
		bool? flag = null;
		if (dictionary.TryGetValue("alive", out var value4))
		{
			flag = value4.ToBool();
		}
		if (num.HasValue && pos2 == null)
		{
			lastErrorMessage = "Can't use range argument without source position";
			return EnumParseResult.Bad;
		}
		switch (v)
		{
		case 'p':
		{
			IPlayer player = null;
			IPlayer[] allOnlinePlayers = api.World.AllOnlinePlayers;
			foreach (IPlayer player2 in allOnlinePlayers)
			{
				if ((num.HasValue && player2.Entity.Pos.DistanceTo(pos2) > (double?)num) || (value3 != null && !WildcardUtil.Match(value3, player2.PlayerName)) || (flag.HasValue && player2.Entity.Alive != flag))
				{
					continue;
				}
				if (player == null)
				{
					player = player2;
					continue;
				}
				if (pos2 == null)
				{
					lastErrorMessage = "Two matching players found. Can't get nearest player without source position";
					return EnumParseResult.Bad;
				}
				if (player.Entity.Pos.DistanceTo(pos2) > player2.Entity.Pos.DistanceTo(pos2))
				{
					player = player2;
				}
			}
			pos = player?.Entity.Pos.XYZ;
			return EnumParseResult.Good;
		}
		case 'e':
		{
			ICollection<Entity> collection = ((api.Side != EnumAppSide.Server) ? (api as ICoreClientAPI).World.LoadedEntities.Values : (api as ICoreServerAPI).World.LoadedEntities.Values);
			Entity entity2 = null;
			foreach (Entity item in collection)
			{
				if ((num.HasValue && item.Pos.DistanceTo(pos2) > (double?)num) || (assetLocation != null && !WildcardUtil.Match(assetLocation, item.Code)) || (flag.HasValue && item.Alive != flag) || (value3 != null && !WildcardUtil.Match(value3, item.GetName())))
				{
					continue;
				}
				if (entity2 == null)
				{
					entity2 = item;
					continue;
				}
				if (pos2 == null)
				{
					lastErrorMessage = "Two matching entities found. Can't get nearest entity without source position";
					return EnumParseResult.Bad;
				}
				if (entity2.Pos.DistanceTo(pos2) > item.Pos.DistanceTo(pos2))
				{
					entity2 = item;
				}
			}
			pos = entity2?.Pos.XYZ;
			return EnumParseResult.Good;
		}
		case 'l':
			if (!(entity is EntityPlayer entityPlayer))
			{
				lastErrorMessage = "Can't use 'l' without source player";
				return EnumParseResult.Bad;
			}
			if (entityPlayer.Player.CurrentEntitySelection == null && entityPlayer.Player.CurrentBlockSelection == null)
			{
				lastErrorMessage = "Not looking at an entity or block";
				return EnumParseResult.Bad;
			}
			pos = entityPlayer.Player.CurrentEntitySelection?.Entity.Pos.XYZ ?? entityPlayer.Player.CurrentBlockSelection.Position.ToVec3d();
			return EnumParseResult.Good;
		case 's':
			pos = entity.Pos.XYZ;
			return EnumParseResult.Good;
		default:
			lastErrorMessage = "Wrong selector, needs to be p,e,l or s";
			return EnumParseResult.Bad;
		}
	}
}
