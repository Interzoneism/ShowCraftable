using System;
using Vintagestory.API.Config;

namespace Vintagestory.API.Common;

public class FloatArgParser : ArgumentParserBase
{
	private float min;

	private float max;

	private float value;

	private float defaultvalue;

	public FloatArgParser(string argName, float min, float max, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.min = min;
		this.max = max;
	}

	public override string GetSyntaxExplanation(string indent)
	{
		return indent + GetSyntax() + " is a decimal number, for example 0.5";
	}

	public FloatArgParser(string argName, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		defaultvalue = 0f;
		min = float.MinValue;
		max = float.MaxValue;
	}

	public FloatArgParser(string argName, float defaultvalue, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.defaultvalue = defaultvalue;
		min = float.MinValue;
		max = float.MaxValue;
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return new string[2]
		{
			float.MinValue.ToString() ?? "",
			float.MaxValue.ToString() ?? ""
		};
	}

	public override object GetValue()
	{
		return value;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		value = defaultvalue;
		base.PreProcess(args);
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		if (args.RawArgs.Length == 0)
		{
			lastErrorMessage = Lang.Get("Missing");
			return EnumParseResult.Bad;
		}
		float? num = args.RawArgs.PopFloat();
		if (!num.HasValue)
		{
			lastErrorMessage = Lang.Get("Not a number");
			return EnumParseResult.Bad;
		}
		if (num < min || num > max)
		{
			lastErrorMessage = Lang.Get("Number out of range");
			return EnumParseResult.Bad;
		}
		value = num.Value;
		return EnumParseResult.Good;
	}

	public override void SetValue(object data)
	{
		value = (float)data;
	}
}
