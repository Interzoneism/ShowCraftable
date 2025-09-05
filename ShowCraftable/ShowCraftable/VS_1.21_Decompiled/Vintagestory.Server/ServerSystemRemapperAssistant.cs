using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class ServerSystemRemapperAssistant : ServerSystem
{
	private Dictionary<string, string[]> remaps = new Dictionary<string, string[]>();

	public ServerSystemRemapperAssistant(ServerMain server)
		: base(server)
	{
		server.api.ChatCommands.Create("fixmapping").RequiresPrivilege(Privilege.controlserver).BeginSubCommand("doremap")
			.WithDescription("Do remap")
			.WithArgs(server.api.ChatCommands.Parsers.Word("code"))
			.HandleWith(OnCmdDoremap)
			.EndSubCommand()
			.BeginSubCommand("ignoreall")
			.WithDescription("Ignore all remappings")
			.HandleWith(OnCmdIgnoreall)
			.EndSubCommand()
			.BeginSubCommand("applyall")
			.WithDescription("Apply all remappings")
			.WithArgs(server.api.ChatCommands.Parsers.OptionalWord("force"))
			.HandleWith(OnCmdApplyall)
			.EndSubCommand();
	}

	private TextCommandResult OnCmdApplyall(TextCommandCallingArgs args)
	{
		int num = 0;
		int num2 = 0;
		bool flag = args[0] as string == "force";
		foreach (KeyValuePair<string, string[]> remap in remaps)
		{
			string key = remap.Key;
			if (!(!server.SaveGameData.RemappingsAppliedByCode.ContainsKey(key) || flag))
			{
				continue;
			}
			num2++;
			string[] value = remap.Value;
			for (int i = 0; i < value.Length; i++)
			{
				string text = value[i].Trim();
				if (text.Length != 0)
				{
					server.HandleChatMessage(args.Caller.Player as IServerPlayer, args.Caller.FromChatGroupId, text);
					num++;
				}
			}
			server.SaveGameData.RemappingsAppliedByCode[key] = true;
		}
		if (num == 0)
		{
			return TextCommandResult.Success("No applicable remappings found, seems all good for now!");
		}
		return TextCommandResult.Success($"Okay, {num2} remapping sets with a total of {num} remappings commands have been executed. You can now restart your game/server");
	}

	private TextCommandResult OnCmdIgnoreall(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<string, string[]> remap in remaps)
		{
			string key = remap.Key;
			if (!server.SaveGameData.RemappingsAppliedByCode.ContainsKey(key))
			{
				server.SaveGameData.RemappingsAppliedByCode[key] = false;
			}
		}
		return TextCommandResult.Success(Lang.Get("Okay, ignoring all new remappings. You can still manually remap them using /fixmapping doremap [code]"));
	}

	private TextCommandResult OnCmdDoremap(TextCommandCallingArgs args)
	{
		string key = args[0] as string;
		if (!remaps.ContainsKey(key))
		{
			return TextCommandResult.Success(Lang.Get("No remapping group found under this code"));
		}
		string[] array = remaps[key];
		int num = 0;
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string text = array2[i].Trim();
			if (text.Length != 0)
			{
				server.HandleChatMessage(args.Caller.Player as IServerPlayer, args.Caller.FromChatGroupId, text);
				num++;
			}
		}
		server.SaveGameData.RemappingsAppliedByCode[key] = true;
		return TextCommandResult.Success(Lang.Get("Ok, {0} commands executed.", num));
	}

	public override void Dispose()
	{
		BlockSchematic.BlockRemaps = null;
		BlockSchematic.ItemRemaps = null;
	}

	public override void OnFinalizeAssets()
	{
		remaps = server.AssetManager.Get("config/remaps.json").ToObject<Dictionary<string, string[]>>();
		extractRemapsForSchematicImports();
		HashSet<string> hashSet = new HashSet<string>(remaps.Keys);
		if (server.SaveGameData.IsNewWorld)
		{
			foreach (string item in hashSet)
			{
				server.SaveGameData.RemappingsAppliedByCode[item] = true;
			}
		}
		else
		{
			foreach (string key in server.SaveGameData.RemappingsAppliedByCode.Keys)
			{
				hashSet.Remove(key);
			}
			server.requiresRemaps = hashSet.Count > 0;
		}
		string[] array = server.AssetManager.Get("config/remapentities.json").ToObject<string[]>();
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(" ");
			if (array2[0].Equals("/eir"))
			{
				server.EntityCodeRemappings.TryAdd(array2[3], array2[2]);
			}
		}
	}

	private void extractRemapsForSchematicImports()
	{
		BlockSchematic.BlockRemaps = new Dictionary<string, Dictionary<string, string>>();
		BlockSchematic.ItemRemaps = new Dictionary<string, Dictionary<string, string>>();
		foreach (KeyValuePair<string, string[]> remap in remaps)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			string[] value = remap.Value;
			for (int i = 0; i < value.Length; i++)
			{
				string[] array = value[i].Split(" ");
				string text = array[0];
				if (text.Equals("/bir"))
				{
					dictionary.TryAdd(array[3], array[2]);
				}
				else if (text.Equals("/iir"))
				{
					dictionary2.TryAdd(array[3], array[2]);
				}
			}
			BlockSchematic.BlockRemaps.TryAdd(remap.Key, dictionary);
			BlockSchematic.ItemRemaps.TryAdd(remap.Key, dictionary2);
		}
	}
}
