using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.Common;

public class ChatCommandApi : IChatCommandApi, IEnumerable<KeyValuePair<string, IChatCommand>>, IEnumerable
{
	public static string ClientCommandPrefix = ".";

	public static string ServerCommandPrefix = "/";

	internal Dictionary<string, IChatCommand> ichatCommands = new Dictionary<string, IChatCommand>(StringComparer.OrdinalIgnoreCase);

	private ICoreAPI api;

	private CommandArgumentParsers parsers;

	public string CommandPrefix
	{
		get
		{
			if (api.Side != EnumAppSide.Client)
			{
				return ServerCommandPrefix;
			}
			return ClientCommandPrefix;
		}
	}

	public CommandArgumentParsers Parsers => parsers;

	public int Count => ichatCommands.Count;

	public IChatCommand this[string name] => ichatCommands[name];

	public IEnumerator<IChatCommand> GetEnumerator()
	{
		foreach (IChatCommand value in ichatCommands.Values)
		{
			yield return value;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ichatCommands.GetEnumerator();
	}

	public ChatCommandApi(ICoreAPI api)
	{
		this.api = api;
		parsers = new CommandArgumentParsers(api);
	}

	public IChatCommand Get(string name)
	{
		ichatCommands.TryGetValue(name, out var value);
		return value;
	}

	public IChatCommand Create()
	{
		return new ChatCommandImpl(this);
	}

	public IChatCommand Create(string commandName)
	{
		return new ChatCommandImpl(this).WithName(commandName.ToLowerInvariant());
	}

	public IChatCommand GetOrCreate(string commandName)
	{
		commandName = commandName.ToLowerInvariant();
		return Get(commandName) ?? Create().WithName(commandName);
	}

	public IEnumerable<IChatCommand> ListAll()
	{
		return ichatCommands.Values;
	}

	public Dictionary<string, IChatCommand> AllSubcommands()
	{
		return ichatCommands;
	}

	public void Execute(string commandName, TextCommandCallingArgs args, Action<TextCommandResult> onCommandComplete = null)
	{
		commandName = commandName.ToLowerInvariant();
		if (ichatCommands.TryGetValue(commandName, out var value))
		{
			if (api.Side == EnumAppSide.Server && value.Incomplete)
			{
				throw new InvalidOperationException("Programming error: Incomplete command - no name or required privilege has been set");
			}
			if (api.Side == EnumAppSide.Client && (value as ChatCommandImpl).AnyPrivilegeSet)
			{
				args.Caller.CallerPrivileges = null;
			}
			args.LanguageCode = (args.Caller.Player as IServerPlayer)?.LanguageCode ?? Lang.CurrentLocale;
			value.Execute(args, onCommandComplete);
		}
		else
		{
			onCommandComplete(new TextCommandResult
			{
				Status = EnumCommandStatus.NoSuchCommand,
				ErrorCode = "nosuchcommand"
			});
		}
	}

	public void ExecuteUnparsed(string message, TextCommandCallingArgs args, Action<TextCommandResult> onCommandComplete = null)
	{
		message = message.Substring(1);
		int num = message.IndexOf(' ');
		string joinedargs;
		string commandName;
		if (num > 0)
		{
			joinedargs = message.Substring(num + 1);
			commandName = message.Substring(0, num);
		}
		else
		{
			joinedargs = "";
			commandName = message;
		}
		args.RawArgs = new CmdArgs(joinedargs);
		Execute(commandName, args, onCommandComplete);
	}

	public void Execute(string commandName, IServerPlayer player, int groupId, string args, Action<TextCommandResult> onCommandComplete = null)
	{
		api.Logger.Audit("Handling command for {0} /{1} {2}", player.PlayerName, commandName, args);
		string langCode = player.LanguageCode;
		try
		{
			Execute(commandName, new TextCommandCallingArgs
			{
				Caller = new Caller
				{
					Player = player,
					Pos = player.Entity.Pos.XYZ,
					FromChatGroupId = groupId
				},
				RawArgs = new CmdArgs(args)
			}, delegate(TextCommandResult results)
			{
				if (results.StatusMessage != null && results.StatusMessage.Length > 0)
				{
					string message = results.StatusMessage;
					if (results.StatusMessage.IndexOf('\n') == -1)
					{
						message = ((results.MessageParams == null) ? Lang.GetL(langCode, results.StatusMessage) : Lang.GetL(langCode, results.StatusMessage, results.MessageParams));
					}
					player.SendMessage(groupId, message, (results.Status != EnumCommandStatus.Success) ? EnumChatType.CommandError : EnumChatType.CommandSuccess);
				}
				if (results.Status == EnumCommandStatus.NoSuchCommand)
				{
					player.SendMessage(groupId, Lang.GetL(langCode, "No such command exists"), EnumChatType.CommandError);
					SuggestCommands(player, groupId, commandName);
				}
				if (results.Status == EnumCommandStatus.Error)
				{
					player.SendMessage(groupId, Lang.GetL(langCode, "For help, type <code>/help {0}</code>", commandName), EnumChatType.CommandError);
				}
				onCommandComplete?.Invoke(results);
			});
		}
		catch (Exception ex)
		{
			api.Logger.Error("Player {0}/{1} caused an exception through a command.", player.PlayerName, player.PlayerUID);
			api.Logger.Error("Command: /{0} {1}", commandName, args);
			api.Logger.Error(ex);
			string text = "An Exception was thrown while executing Command: {0}. Check error log for more detail.";
			player.SendMessage(groupId, Lang.GetL(langCode, text, ex.Message), EnumChatType.CommandError);
			onCommandComplete?.Invoke(TextCommandResult.Error(text, "exception"));
		}
	}

	public void Execute(string commandName, IClientPlayer player, int groupId, string args, Action<TextCommandResult> onCommandComplete = null)
	{
		Execute(commandName, new TextCommandCallingArgs
		{
			Caller = new Caller
			{
				Player = player,
				FromChatGroupId = groupId,
				CallerPrivileges = new string[1] { "*" }
			},
			RawArgs = new CmdArgs(args)
		}, delegate(TextCommandResult results)
		{
			if (results.StatusMessage != null)
			{
				player.ShowChatNotification(Lang.Get(results.StatusMessage));
			}
			if (results.Status == EnumCommandStatus.NoSuchCommand)
			{
				player.ShowChatNotification(Lang.Get("No such command exists"));
			}
			onCommandComplete?.Invoke(results);
		});
	}

	private void SuggestCommands(IServerPlayer player, int groupId, string commandName)
	{
		string text = null;
		int num = 99;
		foreach (KeyValuePair<string, IChatCommand> ichatCommand in ichatCommands)
		{
			int num2 = LevenshteinDistance(ichatCommand.Key, commandName);
			if (num2 < 4 && num2 < commandName.Length / 2 && num > num2)
			{
				text = ichatCommand.Key;
				num = num2;
			}
		}
		if (text != null)
		{
			player.SendMessage(groupId, Lang.Get("command-suggestion", text), EnumChatType.CommandError);
		}
	}

	public static int LevenshteinDistance(string source1, string source2)
	{
		int length = source1.Length;
		int length2 = source2.Length;
		int[,] array = new int[length + 1, length2 + 1];
		if (length == 0)
		{
			return length2;
		}
		if (length2 == 0)
		{
			return length;
		}
		int num = 0;
		while (num <= length)
		{
			array[num, 0] = num++;
		}
		int num2 = 0;
		while (num2 <= length2)
		{
			array[0, num2] = num2++;
		}
		for (int i = 1; i <= length; i++)
		{
			for (int j = 1; j <= length2; j++)
			{
				int num3 = ((source2[j - 1] != source1[i - 1]) ? 1 : 0);
				array[i, j] = Math.Min(Math.Min(array[i - 1, j] + 1, array[i, j - 1] + 1), array[i - 1, j - 1] + num3);
			}
		}
		return array[length, length2];
	}

	internal void UnregisterCommand(string name)
	{
		ichatCommands.Remove(name);
	}

	internal virtual bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ClientChatCommandDelegate handler, string requiredPrivilege = null)
	{
		try
		{
			Create(command).WithDesc(descriptionMsg + "\nSyntax:" + syntaxMsg).RequiresPrivilege(requiredPrivilege).WithArgs(parsers.Unparsed("legacy args"))
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					handler(args.Caller.FromChatGroupId, args.RawArgs);
					return new TextCommandResult
					{
						Status = EnumCommandStatus.UnknownLegacy
					};
				});
		}
		catch (InvalidOperationException e)
		{
			api.Logger.Warning("Command {0}{1} already registered:", ClientCommandPrefix, command);
			api.Logger.Warning(e);
			return false;
		}
		return true;
	}

	internal virtual bool RegisterCommand(ChatCommand chatCommand)
	{
		try
		{
			Create(chatCommand.Command).WithDesc(chatCommand.Description + "\nSyntax:" + chatCommand.Syntax).RequiresPrivilege(chatCommand.RequiredPrivilege).WithArgs(parsers.Unparsed("legacy args"))
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					chatCommand.CallHandler(args.Caller.Player, args.Caller.FromChatGroupId, args.RawArgs);
					return new TextCommandResult
					{
						Status = EnumCommandStatus.UnknownLegacy
					};
				});
		}
		catch (InvalidOperationException e)
		{
			api.Logger.Warning("Command {0}{1} already registered:", (chatCommand is ServerChatCommand) ? ServerCommandPrefix : ClientCommandPrefix, chatCommand.Command);
			api.Logger.Warning(e);
			return false;
		}
		return true;
	}

	[Obsolete("Better to directly use new ChatCommands api instead")]
	public bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ServerChatCommandDelegate handler, string requiredPrivilege = null)
	{
		try
		{
			Create(command).WithDesc(descriptionMsg + "\nSyntax:" + syntaxMsg).RequiresPrivilege(requiredPrivilege).WithArgs(parsers.Unparsed("legacy args"))
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					handler(args.Caller.Player as IServerPlayer, args.Caller.FromChatGroupId, args.RawArgs);
					return new TextCommandResult
					{
						Status = EnumCommandStatus.UnknownLegacy
					};
				});
		}
		catch (InvalidOperationException e)
		{
			api.Logger.Warning("Command {0}{1} already registered:", ClientCommandPrefix, command);
			api.Logger.Warning(e);
			return false;
		}
		return true;
	}

	public bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ClientChatCommandDelegate handler)
	{
		return RegisterCommand(command, descriptionMsg, syntaxMsg, handler, null);
	}

	IEnumerator<KeyValuePair<string, IChatCommand>> IEnumerable<KeyValuePair<string, IChatCommand>>.GetEnumerator()
	{
		return ichatCommands.GetEnumerator();
	}
}
