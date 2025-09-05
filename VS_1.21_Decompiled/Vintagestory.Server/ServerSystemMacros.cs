using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

public class ServerSystemMacros : ServerSystem
{
	private Dictionary<string, ServerCommandMacro> wipMacroByPlayer = new Dictionary<string, ServerCommandMacro>();

	private Dictionary<string, ServerCommandMacro> ServerCommmandMacros = new Dictionary<string, ServerCommandMacro>();

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		wipMacroByPlayer.Remove(player.PlayerUID);
	}

	public override void OnBeginConfiguration()
	{
		LoadMacros();
	}

	public ServerSystemMacros(ServerMain server)
		: base(server)
	{
		IChatCommandApi chatCommands = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		chatCommands.Create("macro").WithDesc("Manage server side macros").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("addcmd")
			.WithDesc("Append a command")
			.WithArgs(parsers.All("Command to add. {{param0}}, {{param1}}, etc. can be used as placeholders for command arguments."))
			.HandleWith((TextCommandCallingArgs args) => addCmd(args, clear: false))
			.EndSubCommand()
			.BeginSubCommand("setcmd")
			.WithDesc("Set command (clears any previously set commands). {{param0}}, {{param1}}, etc. can be used as placeholders for command arguments.")
			.WithArgs(parsers.All("Command to set"))
			.HandleWith((TextCommandCallingArgs args) => addCmd(args, clear: true))
			.EndSubCommand()
			.BeginSubCommand("desc")
			.WithDesc("Set command description")
			.WithArgs(parsers.All("Description to set"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				getWipMacro(args.Caller, createIfNotExists: true).Description = (string)args[0];
				return TextCommandResult.Success(Lang.Get("Ok, description set"));
			})
			.EndSubCommand()
			.BeginSubCommand("priv")
			.WithDesc("Set command privilege")
			.WithArgs(parsers.Word("Required privilege to run command", Privilege.AllCodes().Append("or custom privelges")))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				getWipMacro(args.Caller, createIfNotExists: true).Privilege = (string)args[0];
				return TextCommandResult.Success(Lang.Get("Ok, privilege set"));
			})
			.EndSubCommand()
			.BeginSubCommand("discard")
			.WithDesc("Discard wip macro")
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				wipMacroByPlayer.Remove(args.Caller.Player?.PlayerUID ?? "_console");
				return TextCommandResult.Success("wip macro discarded");
			})
			.EndSubCommand()
			.BeginSubCommand("save")
			.WithDesc("Save wip macro")
			.WithArgs(parsers.Word("name of the macro"))
			.HandleWith(saveMacro)
			.EndSubCommand()
			.BeginSubCommand("delete")
			.WithDesc("Delete a macro")
			.WithArgs(parsers.Word("macro name"))
			.HandleWith(deleteMacro)
			.EndSubCommand()
			.BeginSubCommand("list")
			.WithDesc("List current macros")
			.HandleWith(listMacros)
			.EndSubCommand()
			.BeginSubCommand("show")
			.WithDesc("Show given info on macro")
			.WithArgs(parsers.Word("macro name"))
			.HandleWith(showMacro)
			.EndSubCommand()
			.BeginSubCommand("showwip")
			.WithDesc("Show info on current wip macro")
			.HandleWith(showWipMacro)
			.EndSubCommand();
	}

	private TextCommandResult saveMacro(TextCommandCallingArgs args)
	{
		string text = (string)args[0];
		if (server.api.commandapi.Get(text) != null)
		{
			return TextCommandResult.Error(Lang.Get("Command /{0} is already taken, please choose another name", text), "commandnameused");
		}
		ServerCommandMacro wipMacro = getWipMacro(args.Caller, createIfNotExists: false);
		if (wipMacro == null || wipMacro.Commands.Length == 0)
		{
			return TextCommandResult.Error(Lang.Get("No commands defined for this macro. Add at least 1 command first."), "nocommandsdefined");
		}
		if (wipMacro.Privilege == null)
		{
			return TextCommandResult.Error(Lang.Get("No privilege defined for this macro. Set privilege with /macro priv."), "noprivdefined");
		}
		wipMacro.CreatedByPlayerUid = args.Caller.Player?.PlayerUID ?? "console";
		ServerCommmandMacros[text] = wipMacro;
		RegisterMacro(wipMacro);
		SaveMacros();
		wipMacroByPlayer.Remove(args.Caller.Player?.PlayerUID ?? "_console");
		return TextCommandResult.Success(Lang.Get("Ok, command created. You can use it now."));
	}

	private TextCommandResult showWipMacro(TextCommandCallingArgs args)
	{
		ServerCommandMacro wipMacro = getWipMacro(args.Caller, createIfNotExists: false);
		if (wipMacro != null)
		{
			return TextCommandResult.Success(Lang.Get("Name: {0}\nDescription: {1}\nRequired privilege: {2}\nCommands: {3}", wipMacro.Name, wipMacro.Syntax, wipMacro.Description, wipMacro.Privilege, wipMacro.Commands));
		}
		return TextCommandResult.Error(Lang.Get("No macro in wip"), "nomacroinwip");
	}

	private TextCommandResult showMacro(TextCommandCallingArgs args)
	{
		string key = (string)args[0];
		if (ServerCommmandMacros.TryGetValue(key, out var value))
		{
			return TextCommandResult.Success(Lang.Get("Name: {0}\nDescription: {1}\nRequired privilege: {2}\nCommands: {3}", value.Name, value.Syntax, value.Description, value.Privilege, value.Commands));
		}
		return TextCommandResult.Error(Lang.Get("No such macro found"), "notfound");
	}

	private TextCommandResult listMacros(TextCommandCallingArgs args)
	{
		if (ServerCommmandMacros.Count > 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (ServerCommandMacro value in ServerCommmandMacros.Values)
			{
				stringBuilder.AppendLine("  /" + value.Name + " " + value.Syntax + " - " + value.Description);
			}
			return TextCommandResult.Success(Lang.Get("{0}Type /macro show [name] to see more info about a particular macro", stringBuilder.ToString()));
		}
		return TextCommandResult.Error("No macros defined on this server", "nomacros");
	}

	private TextCommandResult deleteMacro(TextCommandCallingArgs args)
	{
		string key = (string)args[0];
		if (ServerCommmandMacros.TryGetValue(key, out var value))
		{
			ServerCommmandMacros.Remove(value.Name);
			server.api.commandapi.UnregisterCommand(value.Name);
			SaveMacros();
			return TextCommandResult.Success("Ok, macro deleted");
		}
		return TextCommandResult.Error("No such macro found", "nosuchmacro");
	}

	private TextCommandResult addCmd(TextCommandCallingArgs args, bool clear)
	{
		ServerCommandMacro wipMacro = getWipMacro(args.Caller, createIfNotExists: true);
		if (clear)
		{
			wipMacro.Commands = "";
		}
		wipMacro.Commands += (string)args[0];
		wipMacro.Commands += "\n";
		return TextCommandResult.Success(Lang.Get("Ok, command added."));
	}

	private ServerCommandMacro getWipMacro(Caller caller, bool createIfNotExists)
	{
		string key = caller.Player?.PlayerUID ?? "_console";
		if (wipMacroByPlayer.TryGetValue(key, out var value))
		{
			return value;
		}
		if (createIfNotExists)
		{
			value = new ServerCommandMacro();
			return wipMacroByPlayer[key] = value;
		}
		return null;
	}

	private void OnMacro(string name, TextCommandCallingArgs args, Action<TextCommandResult> onCommandComplete = null)
	{
		if (!ServerCommmandMacros.ContainsKey(name))
		{
			onCommandComplete(TextCommandResult.Error("No such macro found", "nosuchmacro"));
		}
		ServerCommandMacro serverCommandMacro = ServerCommmandMacros[name];
		string[] commands = serverCommandMacro.Commands.Split('\n');
		int success = 0;
		for (int i = 0; i < commands.Length; i++)
		{
			int index = i;
			string text = commands[i];
			for (int j = 0; j < args.RawArgs.Length; j++)
			{
				text = text.Replace("{param" + (j + 1) + "}", args.RawArgs[j]);
			}
			text = Regex.Replace(text, "{param\\d+}", "");
			if (text.Length == 0)
			{
				continue;
			}
			string[] array = text.Split(new char[1] { ' ' });
			string command = array[0].Replace("/", "");
			string joinedargs = ((text.IndexOf(' ') < 0) ? "" : text.Substring(text.IndexOf(' ') + 1));
			server.api.ChatCommands.Execute(command, new TextCommandCallingArgs
			{
				Caller = args.Caller,
				RawArgs = new CmdArgs(joinedargs)
			}, delegate(TextCommandResult result)
			{
				if (result.Status == EnumCommandStatus.Success)
				{
					int num = success;
					success = num + 1;
				}
				if (index == command.Length - 1)
				{
					onCommandComplete(TextCommandResult.Success(Lang.Get("Macro executed. {0}/{1} commands successful.", success, commands.Length)));
				}
			});
		}
	}

	public void LoadMacros()
	{
		string text = "servermacros.json";
		if (!File.Exists(Path.Combine(GamePaths.Config, text)))
		{
			return;
		}
		try
		{
			List<ServerCommandMacro> list = null;
			using (TextReader textReader = new StreamReader(Path.Combine(GamePaths.Config, text)))
			{
				list = JsonConvert.DeserializeObject<List<ServerCommandMacro>>(textReader.ReadToEnd());
				textReader.Close();
			}
			foreach (ServerCommandMacro item in list)
			{
				ServerCommmandMacros[item.Name] = item;
				RegisterMacro(item);
			}
			ServerMain.Logger.Notification("{0} Macros loaded", list.Count);
		}
		catch (Exception e)
		{
			ServerMain.Logger.Error("Failed loading {0}:", text);
			ServerMain.Logger.Error(e);
		}
	}

	private void RegisterMacro(ServerCommandMacro macro)
	{
		server.api.ChatCommands.Create(macro.Name).WithDesc(macro.Description).HandleWith(delegate(TextCommandCallingArgs args)
		{
			OnMacro(macro.Name, args);
			return TextCommandResult.Deferred;
		})
			.RequiresPrivilege(macro.Privilege);
	}

	public void SaveMacros()
	{
		StreamWriter streamWriter = new StreamWriter(Path.Combine(GamePaths.Config, "servermacros.json"));
		streamWriter.Write(JsonConvert.SerializeObject((object)ServerCommmandMacros.Values, (Formatting)1));
		streamWriter.Close();
	}
}
