using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class EntitiesArgParser : ArgumentParserBase
{
	private Entity[] entities;

	private ICoreAPI api;

	public EntitiesArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.api = api;
	}

	public override string GetSyntaxExplanation(string indent)
	{
		return indent + GetSyntax() + " is either a player name, or else one of the following selection codes:\n" + indent + "  s[] for self\n" + indent + "  l[] for the entity currently looked at\n" + indent + "  p[] for all players\n" + indent + "  e[] for all entities.\n" + indent + "  Inside the square brackets, one or more filters can be added, to be more selective.  Filters include name, type, class, alive, range.  For example, <code>e[type=gazelle,range=3,alive=true]</code>.  The filters minx/miny/minz/maxx/maxy/maxz can also be used to specify a volume to search, coordinates are relative to the command caller's position.\n" + indent + "  This argument may be omitted if the remainder of the command makes sense, in which case it will be interpreted as self.";
	}

	public override object GetValue()
	{
		return entities;
	}

	public override void SetValue(object data)
	{
		entities = (Entity[])data;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		entities = null;
		base.PreProcess(args);
		if (base.IsMissing)
		{
			entities = new Entity[1] { args.Caller.Entity };
		}
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		string maybeplayername = args.RawArgs.PeekWord();
		IPlayer player = api.World.AllOnlinePlayers.FirstOrDefault((IPlayer p) => p.PlayerName.Equals(maybeplayername, StringComparison.InvariantCultureIgnoreCase));
		if (player != null)
		{
			args.RawArgs.PopWord();
			entities = new Entity[1] { player.Entity };
			return EnumParseResult.Good;
		}
		string text = maybeplayername;
		char c = ((text != null && text.Length > 0) ? maybeplayername[0] : ' ');
		if (c != 'p' && c != 'e' && c != 'l' && c != 's')
		{
			lastErrorMessage = Lang.Get("Not a player name and not a selector p, e, l or s: {0}'", maybeplayername);
			entities = new Entity[1] { args.Caller.Entity };
			return EnumParseResult.DependsOnSubsequent;
		}
		c = args.RawArgs.PopChar() ?? ' ';
		Dictionary<string, string> dictionary;
		if (args.RawArgs.PeekChar() == '[')
		{
			string parseErrorMsg;
			string strargs = args.RawArgs.PopCodeBlock('[', ']', out parseErrorMsg);
			if (parseErrorMsg != null)
			{
				lastErrorMessage = parseErrorMsg;
				return EnumParseResult.Bad;
			}
			dictionary = parseSubArgs(strargs);
		}
		else
		{
			if (args.RawArgs.PeekChar() != ' ')
			{
				lastErrorMessage = "Invalid selector, needs to be p,e,l,s followed by [";
				return EnumParseResult.Bad;
			}
			args.RawArgs.PopWord();
			dictionary = new Dictionary<string, string>();
		}
		Vec3d sourcePos = args.Caller.Pos;
		Entity entity = args.Caller.Entity;
		float? range = null;
		if (dictionary.TryGetValue("range", out var value))
		{
			range = value.ToFloat();
			dictionary.Remove("range");
		}
		AssetLocation type = null;
		if (dictionary.TryGetValue("type", out var value2))
		{
			type = new AssetLocation(value2);
			dictionary.Remove("type");
		}
		if (dictionary.TryGetValue("class", out var classstr))
		{
			classstr = classstr.ToLowerInvariant();
			dictionary.Remove("class");
		}
		if (dictionary.TryGetValue("name", out var name))
		{
			dictionary.Remove("name");
		}
		bool? alive = null;
		if (dictionary.TryGetValue("alive", out var value3))
		{
			alive = value3.ToBool();
			dictionary.Remove("alive");
		}
		long? id = null;
		if (dictionary.TryGetValue("id", out var value4))
		{
			id = value4.ToLong(0L);
			dictionary.Remove("id");
		}
		Cuboidi box = null;
		if (sourcePos != null)
		{
			bool flag = false;
			string[] array = new string[6] { "minx", "miny", "minz", "maxx", "maxy", "maxz" };
			int[] array2 = new int[6];
			for (int num = 0; num < array.Length; num++)
			{
				if (dictionary.TryGetValue(array[num], out var value5))
				{
					array2[num] = value5.ToInt() + num / 3;
					dictionary.Remove(array[num]);
					flag = true;
				}
			}
			if (flag)
			{
				BlockPos asBlockPos = sourcePos.AsBlockPos;
				box = new Cuboidi(array2).Translate(asBlockPos.X, asBlockPos.Y, asBlockPos.Z);
			}
		}
		if (dictionary.Count > 0)
		{
			lastErrorMessage = "Unknown selector '" + string.Join(", ", dictionary.Keys) + "'";
			return EnumParseResult.Bad;
		}
		List<Entity> list = new List<Entity>();
		if (range.HasValue && sourcePos == null)
		{
			lastErrorMessage = "Can't use range argument without source pos";
			return EnumParseResult.Bad;
		}
		switch (c)
		{
		case 'p':
		{
			IPlayer[] allOnlinePlayers = api.World.AllOnlinePlayers;
			foreach (IPlayer player2 in allOnlinePlayers)
			{
				if (entityMatches(player2.Entity, sourcePos, type, classstr, range, box, name, alive, id))
				{
					list.Add(player2.Entity);
				}
			}
			entities = list.ToArray();
			return EnumParseResult.Good;
		}
		case 'e':
			if (!range.HasValue)
			{
				ICollection<Entity> collection = ((api.Side != EnumAppSide.Server) ? (api as ICoreClientAPI).World.LoadedEntities.Values : (api as ICoreServerAPI).World.LoadedEntities.Values);
				foreach (Entity item in collection)
				{
					if (entityMatches(item, sourcePos, type, classstr, range, box, name, alive, id))
					{
						list.Add(item);
					}
				}
				entities = list.ToArray();
			}
			else
			{
				float num2 = range.Value;
				entities = api.World.GetEntitiesAround(sourcePos, num2, num2, (Entity e) => entityMatches(e, sourcePos, type, classstr, range, box, name, alive, id));
			}
			return EnumParseResult.Good;
		case 'l':
		{
			if (!(entity is EntityPlayer entityPlayer))
			{
				lastErrorMessage = "Can't use 'l' without source player";
				return EnumParseResult.Bad;
			}
			if (entityPlayer.Player.CurrentEntitySelection == null)
			{
				lastErrorMessage = "Not looking at an entity";
				return EnumParseResult.Bad;
			}
			Entity entity2 = entityPlayer.Player.CurrentEntitySelection.Entity;
			if (entityMatches(entity2, sourcePos, type, classstr, range, box, name, alive, id))
			{
				entities = new Entity[1] { entity2 };
			}
			else
			{
				entities = Array.Empty<Entity>();
			}
			return EnumParseResult.Good;
		}
		case 's':
			if (entityMatches(entity, sourcePos, type, classstr, range, box, name, alive, id))
			{
				entities = new Entity[1] { entity };
			}
			else
			{
				entities = Array.Empty<Entity>();
			}
			return EnumParseResult.Good;
		default:
			lastErrorMessage = "Wrong selector, needs to be a player name or p,e,l or s";
			return EnumParseResult.Bad;
		}
	}

	private bool entityMatches(Entity e, Vec3d sourcePos, AssetLocation type, string classstr, float? range, Cuboidi box, string name, bool? alive, long? id)
	{
		if (id.HasValue && e.EntityId != id)
		{
			return false;
		}
		if (range.HasValue && e.SidedPos.DistanceTo(sourcePos) > (double?)range)
		{
			return false;
		}
		if (box != null && !box.ContainsOrTouches(e.SidedPos))
		{
			return false;
		}
		if (classstr != null && classstr != e.Class.ToLowerInvariant())
		{
			return false;
		}
		if (type != null && !WildcardUtil.Match(type, e.Code))
		{
			return false;
		}
		if (alive.HasValue && e.Alive != alive)
		{
			return false;
		}
		if (name != null && !WildcardUtil.Match(name, e.GetName()))
		{
			return false;
		}
		return true;
	}
}
