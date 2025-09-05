using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class MacroManager : IMacroManager
{
	private ClientMain game;

	public SortedDictionary<int, IMacroBase> MacrosByIndex { get; set; } = new SortedDictionary<int, IMacroBase>();

	public MacroManager(ClientMain game)
	{
		this.game = game;
		LoadMacros();
	}

	public void LoadMacros()
	{
		SortedDictionary<string, Macro> sortedDictionary = new SortedDictionary<string, Macro>();
		foreach (string item in Directory.EnumerateFiles(GamePaths.Macros, "*.json"))
		{
			string text = File.ReadAllText(item);
			try
			{
				Macro value = JsonConvert.DeserializeObject<Macro>(text);
				sortedDictionary.Add(item, value);
			}
			catch (Exception ex)
			{
				ScreenManager.Platform.Logger.Warning("Failed deserializing macro " + item + ": " + ex.Message);
			}
		}
		foreach (Macro value2 in sortedDictionary.Values)
		{
			MacrosByIndex[value2.Index] = value2;
			SetupHotKey(value2.Index, value2, game);
		}
	}

	private bool SetupHotKey(int macroIndex, IMacroBase macro, ClientMain game)
	{
		if (macro.KeyCombination == null || macro.KeyCombination.KeyCode < 0)
		{
			return false;
		}
		HotKey hotkeyByKeyCombination = ScreenManager.hotkeyManager.GetHotkeyByKeyCombination(macro.KeyCombination);
		string text = "macro-" + macro.Code;
		if (hotkeyByKeyCombination != null && hotkeyByKeyCombination.Code != text)
		{
			ScreenManager.Platform.Logger.Warning("Can't register hotkey {0} for macro {1} because it is aready in use by hotkey {2}", macro.KeyCombination, macro.Code, hotkeyByKeyCombination.Code);
			return false;
		}
		ScreenManager.hotkeyManager.RegisterHotKey(text, "Macro: " + macro.Name, macro.KeyCombination, HotkeyType.DevTool);
		ScreenManager.hotkeyManager.SetHotKeyHandler(text, delegate
		{
			RunMacro(macroIndex, game);
			return true;
		});
		return true;
	}

	public void DeleteMacro(int macroIndex)
	{
		MacrosByIndex.TryGetValue(macroIndex, out var value);
		if (value != null)
		{
			File.Delete(Path.Combine(GamePaths.Macros, macroIndex + "-" + value.Code + ".json"));
			MacrosByIndex.Remove(macroIndex);
			string code = "macro-" + value.Code;
			ScreenManager.hotkeyManager.RemoveHotKey(code);
		}
	}

	public void SetMacro(int macroIndex, IMacroBase macro)
	{
		MacrosByIndex[macroIndex] = macro;
		SaveMacro(macroIndex);
		SetupHotKey(macroIndex, macro, game);
	}

	public virtual bool SaveMacro(int macroIndex)
	{
		MacrosByIndex.TryGetValue(macroIndex, out var value);
		if (value == null)
		{
			return false;
		}
		string path = Path.Combine(GamePaths.Macros, macroIndex + "-" + value.Code + ".json");
		try
		{
			using TextWriter textWriter = new StreamWriter(path);
			textWriter.Write(JsonConvert.SerializeObject((object)value, (Formatting)1));
			textWriter.Close();
		}
		catch (IOException)
		{
			return false;
		}
		SetupHotKey(macroIndex, value, game);
		return true;
	}

	public bool RunMacro(int macroIndex, IClientWorldAccessor world)
	{
		if (!MacrosByIndex.ContainsKey(macroIndex))
		{
			return false;
		}
		string[] commands = MacrosByIndex[macroIndex].Commands;
		for (int i = 0; i < commands.Length; i++)
		{
			(world as ClientMain).eventManager?.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, commands[i], EnumChatType.Macro, null);
		}
		return true;
	}
}
