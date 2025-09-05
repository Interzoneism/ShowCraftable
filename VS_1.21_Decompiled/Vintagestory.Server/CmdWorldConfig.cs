using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

internal class CmdWorldConfig
{
	private ServerMain server;

	public CmdWorldConfig(ServerMain server)
	{
		this.server = server;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.ChatCommands.Create("worldconfig").WithAlias("wc").RequiresPrivilege(Privilege.controlserver)
			.WithDescription("Modify the world config")
			.WithArgs(parsers.OptionalWord("key"), parsers.OptionalAll("value"))
			.HandleWith(handle);
	}

	private TextCommandResult handle(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success($"Specify one of the following world configuration settings: {ListConfigs()}");
		}
		string text = (string)args[0];
		if (text == "worldWidth" || text == "worldLength")
		{
			return TextCommandResult.Error($"Changing world size is not supported");
		}
		string arg = "";
		bool flag = false;
		WorldConfigurationAttribute worldConfigurationAttribute = null;
		double result;
		foreach (Mod mod in server.api.modLoader.Mods)
		{
			ModWorldConfiguration worldConfig = mod.WorldConfig;
			if (worldConfig == null)
			{
				continue;
			}
			if (flag)
			{
				break;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = worldConfig.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute worldConfigurationAttribute2 in worldConfigAttributes)
			{
				if (!worldConfigurationAttribute2.Code.Equals(text, StringComparison.InvariantCultureIgnoreCase))
				{
					continue;
				}
				text = worldConfigurationAttribute2.Code;
				worldConfigurationAttribute = worldConfigurationAttribute2;
				arg = "(default:) " + worldConfigurationAttribute2.TypedDefault.ToString();
				if (server.SaveGameData.WorldConfiguration.HasAttribute(text))
				{
					switch (worldConfigurationAttribute.DataType)
					{
					case EnumDataType.Bool:
						arg = server.SaveGameData.WorldConfiguration.GetBool(text).ToString() ?? "";
						break;
					case EnumDataType.DoubleInput:
					case EnumDataType.DoubleRange:
						result = server.SaveGameData.WorldConfiguration.GetDecimal(text);
						arg = result.ToString() ?? "";
						break;
					case EnumDataType.String:
					case EnumDataType.DropDown:
					case EnumDataType.StringRange:
						arg = server.SaveGameData.WorldConfiguration.GetAsString(text) ?? "";
						break;
					case EnumDataType.IntInput:
					case EnumDataType.IntRange:
						arg = server.SaveGameData.WorldConfiguration.GetInt(text).ToString() ?? "";
						break;
					}
				}
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			if (args.Parsers[1].IsMissing && server.SaveGameData.WorldConfiguration.HasAttribute(text))
			{
				return TextCommandResult.Success($"{text} currently has value: {server.SaveGameData.WorldConfiguration[text]}");
			}
			return TextCommandResult.Error($"No such config found: {text}");
		}
		if (args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Success($"{text} currently has value: {arg}");
		}
		string text2 = (string)args[1];
		string text3 = null;
		switch (worldConfigurationAttribute.DataType)
		{
		case EnumDataType.Bool:
		{
			bool flag2 = text2.ToBool();
			server.SaveGameData.WorldConfiguration.SetBool(text, flag2);
			text3 = $"Ok, value {flag2} set. Restart game world or server to apply changes.";
			break;
		}
		case EnumDataType.DoubleInput:
		case EnumDataType.DoubleRange:
		{
			double num2 = text2.ToDouble();
			server.SaveGameData.WorldConfiguration.SetDouble(text, num2);
			text3 = $"Ok, value {num2} set. Restart game world or server to apply changes.";
			break;
		}
		case EnumDataType.String:
		case EnumDataType.DropDown:
		case EnumDataType.StringRange:
			server.SaveGameData.WorldConfiguration.SetString(text, text2);
			text3 = $"Ok, value {text2} set. Restart game world or server to apply changes.";
			break;
		case EnumDataType.IntInput:
		case EnumDataType.IntRange:
		{
			int num = text2.ToInt();
			server.SaveGameData.WorldConfiguration.SetInt(text, num);
			text3 = $"Ok, value {num} set. Restart game world or server to apply changes.";
			break;
		}
		default:
			return TextCommandResult.Error($"Unknown attr datatype.");
		}
		if (worldConfigurationAttribute.Values != null && !worldConfigurationAttribute.Values.Any((string value) => !double.TryParse(value, out var _)) && !double.TryParse(text2, out result))
		{
			text3 = text3 + "\n" + $"Values for this config are usually decimals, {text2} is not a decimal. Config might not apply correctly.";
		}
		return TextCommandResult.Success(text3);
	}

	private string ListConfigs()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Mod mod in server.api.modLoader.Mods)
		{
			ModWorldConfiguration worldConfig = mod.WorldConfig;
			if (worldConfig == null)
			{
				continue;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = worldConfig.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute worldConfigurationAttribute in worldConfigAttributes)
			{
				if (stringBuilder.Length != 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(worldConfigurationAttribute.Code);
			}
		}
		return stringBuilder.ToString();
	}
}
