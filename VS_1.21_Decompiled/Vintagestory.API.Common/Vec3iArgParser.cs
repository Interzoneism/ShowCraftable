using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class Vec3iArgParser : ArgumentParserBase
{
	private Vec3i _vector;

	public Vec3iArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		argCount = 3;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		base.PreProcess(args);
		_vector = null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		int? num = args.RawArgs.PopInt();
		int? num2 = args.RawArgs.PopInt();
		int? num3 = args.RawArgs.PopInt();
		if (num.HasValue && num2.HasValue && num3.HasValue)
		{
			_vector = new Vec3i(num.Value, num2.Value, num3.Value);
		}
		if (!(_vector == null))
		{
			return EnumParseResult.Good;
		}
		return EnumParseResult.Bad;
	}

	public override object GetValue()
	{
		return _vector;
	}

	public override void SetValue(object data)
	{
		_vector = (Vec3i)data;
	}
}
