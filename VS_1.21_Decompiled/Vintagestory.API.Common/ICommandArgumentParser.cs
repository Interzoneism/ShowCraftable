using System;

namespace Vintagestory.API.Common;

public interface ICommandArgumentParser
{
	int ArgCount { get; }

	string LastErrorMessage { get; }

	string ArgumentName { get; }

	bool IsMandatoryArg { get; }

	bool IsMissing { get; set; }

	void PreProcess(TextCommandCallingArgs args);

	EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null);

	string[] GetValidRange(CmdArgs args);

	object GetValue();

	string GetSyntax();

	string GetSyntaxExplanation(string indent);

	void SetValue(object data);
}
