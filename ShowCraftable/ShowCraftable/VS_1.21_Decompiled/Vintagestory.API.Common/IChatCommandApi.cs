using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Vintagestory.API.Common;

public interface IChatCommandApi : IEnumerable<KeyValuePair<string, IChatCommand>>, IEnumerable
{
	IChatCommand this[string name] { get; }

	CommandArgumentParsers Parsers { get; }

	IChatCommand Create();

	IChatCommand Create(string name);

	IChatCommand Get(string name);

	IChatCommand GetOrCreate(string name);

	void Execute(string name, TextCommandCallingArgs args, Action<TextCommandResult> onCommandComplete = null);

	void ExecuteUnparsed(string message, TextCommandCallingArgs args, Action<TextCommandResult> onCommandComplete = null);

	static Dictionary<string, IChatCommand> GetOrdered(Dictionary<string, IChatCommand> command)
	{
		return command.OrderBy((KeyValuePair<string, IChatCommand> s) => s.Key).ToDictionary((KeyValuePair<string, IChatCommand> i) => i.Key, (KeyValuePair<string, IChatCommand> i) => i.Value);
	}

	static Dictionary<string, IChatCommand> GetOrdered(IChatCommandApi chatCommandApi)
	{
		return chatCommandApi.OrderBy((KeyValuePair<string, IChatCommand> s) => s.Key).ToDictionary((KeyValuePair<string, IChatCommand> i) => i.Key, (KeyValuePair<string, IChatCommand> i) => i.Value);
	}
}
