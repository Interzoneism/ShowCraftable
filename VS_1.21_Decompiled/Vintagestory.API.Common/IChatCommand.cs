using System;
using System.Collections.Generic;
using System.Text;

namespace Vintagestory.API.Common;

public interface IChatCommand
{
	string FullName { get; }

	string Name { get; }

	string Description { get; }

	string AdditionalInformation { get; }

	string[] Examples { get; }

	bool Incomplete { get; }

	List<string> Aliases { get; }

	List<string> RootAliases { get; }

	string CommandPrefix { get; }

	IChatCommand this[string name] { get; }

	IEnumerable<IChatCommand> Subcommands { get; }

	Dictionary<string, IChatCommand> AllSubcommands { get; }

	string CallSyntax { get; }

	string CallSyntaxUnformatted { get; }

	IChatCommand WithPreCondition(CommandPreconditionDelegate p);

	IChatCommand WithName(string name);

	IChatCommand WithAlias(params string[] name);

	IChatCommand WithRootAlias(string name);

	IChatCommand WithDescription(string description);

	IChatCommand WithAdditionalInformation(string detail);

	IChatCommand WithExamples(params string[] examaples);

	IChatCommand WithArgs(params ICommandArgumentParser[] args);

	IChatCommand RequiresPrivilege(string privilege);

	IChatCommand RequiresPlayer();

	IChatCommand BeginSubCommand(string name);

	IChatCommand BeginSubCommands(params string[] name);

	IChatCommand EndSubCommand();

	IChatCommand HandleWith(OnCommandDelegate handler);

	void Execute(TextCommandCallingArgs callargs, Action<TextCommandResult> onCommandComplete = null);

	bool IsAvailableTo(Caller caller);

	void Validate();

	IChatCommand IgnoreAdditionalArgs();

	string GetFullSyntaxConsole(Caller caller);

	string GetFullSyntaxHandbook(Caller caller, string indent = "", bool isRootAlias = false);

	void AddParameterSyntax(StringBuilder sb, string indent);

	void AddSyntaxExplanation(StringBuilder sb, string indent);

	string GetFullName(string alias, bool isRootAlias = false);

	string GetCallSyntax(string alias, bool isRootAlias = false);

	string GetCallSyntaxUnformatted(string alias, bool isRootAlias = false);
}
