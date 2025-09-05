using System;
using System.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class WorldPosition2DArgParser : PositionArgumentParserBase
{
	private Vec2i pos;

	private PositionProviderDelegate mapmiddlePosProvider;

	private ICoreAPI api;

	public WorldPosition2DArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.api = api;
		mapmiddlePosProvider = () => api.World.DefaultSpawnPosition.XYZ;
		argCount = 3;
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return null;
	}

	public override object GetValue()
	{
		return pos;
	}

	public override void SetValue(object data)
	{
		pos = (Vec2i)data;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		pos = posTo2D(args.Caller.Pos?.Clone());
		base.PreProcess(args);
	}

	private Vec2i posTo2D(Vec3d callerPos)
	{
		if (!(callerPos == null))
		{
			return new Vec2i(callerPos);
		}
		return null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		if (args.RawArgs.Length == 1)
		{
			string maybeplayername = args.RawArgs.PeekWord();
			IPlayer player = api.World.AllOnlinePlayers.FirstOrDefault((IPlayer p) => p.PlayerName.Equals(maybeplayername, StringComparison.InvariantCultureIgnoreCase));
			if (player != null)
			{
				args.RawArgs.PopWord();
				pos = new Vec2i(player.Entity.Pos.XYZ);
				return EnumParseResult.Good;
			}
			char? c = args.RawArgs.PopChar();
			if (c == 'p' || c == 'e' || c == 'l' || c == 's')
			{
				Vec3d callerPos = new Vec3d();
				EnumParseResult result = tryGetPositionBySelector(c.Value, args, ref callerPos, api);
				pos = posTo2D(callerPos);
				return result;
			}
			lastErrorMessage = "World position 2D must be either 2 coordinates or a target selector beginning with p (nearest player), e (nearest entity), l (looked at entity) or s (executing entity)";
			return EnumParseResult.Bad;
		}
		if (args.RawArgs.Length < 2)
		{
			lastErrorMessage = "Need 2 values";
			return EnumParseResult.Good;
		}
		pos = args.RawArgs.PopFlexiblePos2D(args.Caller.Pos, mapmiddlePosProvider());
		if (pos == null)
		{
			lastErrorMessage = Lang.Get("Invalid position, must be 2 numbers");
		}
		if (!(pos == null))
		{
			return EnumParseResult.Good;
		}
		return EnumParseResult.Bad;
	}
}
