using System;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Vintagestory.API.Client;

public static class GlKeyNames
{
	public static string ToString(GlKeys key)
	{
		return key switch
		{
			GlKeys.Keypad0 => "Keypad 0", 
			GlKeys.Keypad1 => "Keypad 1", 
			GlKeys.Keypad2 => "Keypad 2", 
			GlKeys.Keypad3 => "Keypad 3", 
			GlKeys.Keypad4 => "Keypad 4", 
			GlKeys.Keypad5 => "Keypad 5", 
			GlKeys.Keypad6 => "Keypad 6", 
			GlKeys.Keypad7 => "Keypad 7", 
			GlKeys.Keypad8 => "Keypad 8", 
			GlKeys.Keypad9 => "Keypad 9", 
			GlKeys.KeypadDivide => "Keypad Divide", 
			GlKeys.KeypadMultiply => "Keypad Multiply", 
			GlKeys.KeypadMinus => "Keypad Subtract", 
			GlKeys.KeypadAdd => "Keypad Add", 
			GlKeys.KeypadDecimal => "Keypad Decimal", 
			GlKeys.KeypadEnter => "Keypad Enter", 
			GlKeys.Unknown => "Unknown", 
			GlKeys.LShift => "Shift", 
			GlKeys.LControl => "Ctrl", 
			GlKeys.AltLeft => "Alt", 
			_ => GetKeyName(key), 
		};
	}

	public static string GetKeyName(GlKeys key)
	{
		string printableChar = GetPrintableChar((int)key);
		if (string.IsNullOrWhiteSpace(printableChar))
		{
			return key.ToString();
		}
		return printableChar.ToUpperInvariant();
	}

	public static string GetPrintableChar(int key)
	{
		try
		{
			return GLFW.GetKeyName((Keys)KeyConverter.GlKeysToNew[key], 0);
		}
		catch (IndexOutOfRangeException)
		{
			return string.Empty;
		}
	}
}
