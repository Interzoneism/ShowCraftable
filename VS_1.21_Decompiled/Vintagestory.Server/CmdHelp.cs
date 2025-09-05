using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

internal class CmdHelp
{
	private ServerMain server;

	public CmdHelp(ServerMain server)
	{
		this.server = server;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.commandapi.GetOrCreate("help").RequiresPrivilege(Privilege.chat).WithArgs(parsers.OptionalWord("commandname"), parsers.OptionalWord("subcommand"), parsers.OptionalWord("subsubcommand"))
			.WithDescription("Display list of available server commands")
			.HandleWith(handleHelp);
	}

	private TextCommandResult handleHelp(TextCommandCallingArgs args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		Dictionary<string, IChatCommand> ordered = IChatCommandApi.GetOrdered(server.api.commandapi.AllSubcommands());
		if (args.Parsers[0].IsMissing)
		{
			stringBuilder.AppendLine("Available commands:");
			WriteCommandsList(stringBuilder, ordered, args.Caller);
			stringBuilder.Append("\n" + Lang.Get("Type /help [commandname] to see more info about a command"));
			return TextCommandResult.Success(stringBuilder.ToString());
		}
		string text = (string)args[0];
		if (!args.Parsers[1].IsMissing)
		{
			bool flag = false;
			foreach (KeyValuePair<string, IChatCommand> item in ordered)
			{
				if (item.Key == text)
				{
					ordered = IChatCommandApi.GetOrdered(item.Value.AllSubcommands);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return TextCommandResult.Error(Lang.Get("No such sub-command found") + ": " + text + " " + (string)args[1]);
			}
			text = (string)args[1];
			if (!args.Parsers[2].IsMissing)
			{
				flag = false;
				foreach (KeyValuePair<string, IChatCommand> item2 in ordered)
				{
					if (item2.Key == text)
					{
						ordered = IChatCommandApi.GetOrdered(item2.Value.AllSubcommands);
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return TextCommandResult.Error(Lang.Get("No such sub-command found") + ": " + (string)args[0] + text + " " + (string)args[2]);
				}
				text = (string)args[2];
			}
		}
		foreach (KeyValuePair<string, IChatCommand> item3 in ordered)
		{
			if (!(item3.Key == text))
			{
				continue;
			}
			ChatCommandImpl chatCommandImpl = item3.Value as ChatCommandImpl;
			if (chatCommandImpl.IsAvailableTo(args.Caller))
			{
				Dictionary<string, IChatCommand> allSubcommands = chatCommandImpl.AllSubcommands;
				if (allSubcommands.Count > 0)
				{
					stringBuilder.AppendLine("Available subcommands:");
					WriteCommandsList(stringBuilder, allSubcommands, args.Caller, isSubCommand: true);
					stringBuilder.AppendLine();
					stringBuilder.AppendLine("Type <code>/help " + chatCommandImpl.CallSyntax.Substring(1) + " &lt;<i>subcommand_name</i>&gt;</code> for help on a specific subcommand");
				}
				else
				{
					stringBuilder.AppendLine();
					if (chatCommandImpl.Description != null)
					{
						stringBuilder.AppendLine(chatCommandImpl.Description);
					}
					if (chatCommandImpl.AdditionalInformation != null)
					{
						stringBuilder.AppendLine(chatCommandImpl.AdditionalInformation);
					}
					stringBuilder.AppendLine();
					stringBuilder.AppendLine("Usage: <code>");
					stringBuilder.Append(chatCommandImpl.GetCallSyntax(item3.Key));
					stringBuilder.Append("</code>");
					chatCommandImpl.AddSyntaxExplanation(stringBuilder, "");
					if (chatCommandImpl.Examples != null && chatCommandImpl.Examples.Length != 0)
					{
						stringBuilder.AppendLine((chatCommandImpl.Examples.Length > 1) ? "Examples:" : "Example:");
						string[] examples = chatCommandImpl.Examples;
						foreach (string value in examples)
						{
							stringBuilder.AppendLine(value);
						}
					}
				}
				return TextCommandResult.Success(stringBuilder.ToString());
			}
			return TextCommandResult.Error("Insufficient privilege to use this command");
		}
		return TextCommandResult.Error(Lang.Get("No such command found") + ": " + text);
	}

	private void WriteCommandsList(StringBuilder text, Dictionary<string, IChatCommand> commands, Caller caller, bool isSubCommand = false)
	{
		text.AppendLine();
		foreach (KeyValuePair<string, IChatCommand> command in commands)
		{
			IChatCommand value = command.Value;
			if (!value.IsAvailableTo(caller))
			{
				continue;
			}
			string text2 = value.Description;
			if (text2 == null)
			{
				text2 = " ";
			}
			else
			{
				int num = text2.IndexOf('\n');
				if (num >= 0)
				{
					text2 = text2.Substring(0, num);
				}
				text2 = Lang.Get(text2);
			}
			text.AppendLine("<code>" + value.GetCallSyntax(command.Key, !isSubCommand) + "</code> :  " + text2);
		}
	}
}
