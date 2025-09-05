using System;

namespace Vintagestory.API.Common;

public class BoolArgParser : ArgumentParserBase
{
	private bool value;

	private string trueAlias;

	public BoolArgParser(string argName, string trueAlias, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.trueAlias = trueAlias;
	}

	public override string GetSyntaxExplanation(string indent)
	{
		return indent + GetSyntax() + " is a boolean, including 1 or 0, yes or no, true or false, or " + trueAlias;
	}

	public override object GetValue()
	{
		return value;
	}

	public override void SetValue(object data)
	{
		value = (bool)data;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		value = false;
		base.PreProcess(args);
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		bool? flag = args.RawArgs.PopBool(null, trueAlias);
		if (!flag.HasValue)
		{
			lastErrorMessage = "Missing";
			return EnumParseResult.Bad;
		}
		value = flag.Value;
		return EnumParseResult.Good;
	}
}
