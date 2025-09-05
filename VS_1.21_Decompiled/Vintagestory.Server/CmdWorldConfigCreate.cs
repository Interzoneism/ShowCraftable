using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

internal class CmdWorldConfigCreate
{
	private ServerMain server;

	public CmdWorldConfigCreate(ServerMain server)
	{
		this.server = server;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.ChatCommands.Create("worldconfigcreate").RequiresPrivilege(Privilege.controlserver).WithDescription("Add a new world config value")
			.WithArgs(parsers.WordRange("type", "bool", "double", "float", "int", "string"), parsers.Word("key"), parsers.All("value"))
			.HandleWith(handle);
	}

	private TextCommandResult handle(TextCommandCallingArgs args)
	{
		string text = (string)args[0];
		string key = (string)args[1];
		string text2 = (string)args[2];
		string text3 = null;
		switch (text)
		{
		case "bool":
		{
			bool flag = text2.ToBool();
			server.SaveGameData.WorldConfiguration.SetBool(key, flag);
			text3 = $"Ok, value {flag} set";
			break;
		}
		case "double":
		{
			double num3 = text2.ToDouble();
			server.SaveGameData.WorldConfiguration.SetDouble(key, num3);
			text3 = $"Ok, value {num3} set";
			break;
		}
		case "float":
		{
			float num2 = text2.ToFloat();
			server.SaveGameData.WorldConfiguration.SetFloat(key, num2);
			text3 = $"Ok, value {num2} set";
			break;
		}
		case "string":
			server.SaveGameData.WorldConfiguration.SetString(key, text2);
			text3 = $"Ok, value {text2} set";
			break;
		case "int":
		{
			int num = text2.ToInt();
			server.SaveGameData.WorldConfiguration.SetInt(key, num);
			text3 = $"Ok, value {num} set";
			break;
		}
		default:
			return TextCommandResult.Error("Invalid or missing datatype");
		}
		return TextCommandResult.Success(text3);
	}
}
