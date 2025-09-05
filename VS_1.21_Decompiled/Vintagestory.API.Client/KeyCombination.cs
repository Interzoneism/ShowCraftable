using System.Collections.Generic;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class KeyCombination
{
	public const int MouseStart = 240;

	public int KeyCode;

	public int? SecondKeyCode;

	public bool Ctrl;

	public bool Alt;

	public bool Shift;

	public bool OnKeyUp;

	public bool IsMouseButton(int KeyCode)
	{
		if (KeyCode >= 240)
		{
			return KeyCode < 248;
		}
		return false;
	}

	public override string ToString()
	{
		if (KeyCode < 0)
		{
			return "?";
		}
		if (IsMouseButton(KeyCode))
		{
			return MouseButtonAsString(KeyCode);
		}
		List<string> list = new List<string>();
		if (Ctrl)
		{
			list.Add("CTRL");
		}
		if (Alt)
		{
			list.Add("ALT");
		}
		if (Shift)
		{
			list.Add("SHIFT");
		}
		if (KeyCode == 50)
		{
			list.Add("Esc");
		}
		else
		{
			list.Add(GlKeyNames.ToString((GlKeys)KeyCode) ?? "");
		}
		if (SecondKeyCode.HasValue && SecondKeyCode > 0)
		{
			list.Add(SecondaryAsString());
		}
		return string.Join(" + ", list.ToArray());
	}

	public KeyCombination Clone()
	{
		return (KeyCombination)MemberwiseClone();
	}

	public string PrimaryAsString()
	{
		if (IsMouseButton(KeyCode))
		{
			return MouseButtonAsString(KeyCode);
		}
		if (KeyCode == 50)
		{
			return "Esc";
		}
		return GlKeyNames.ToString((GlKeys)KeyCode);
	}

	public string SecondaryAsString()
	{
		if (IsMouseButton(SecondKeyCode.Value))
		{
			return MouseButtonAsString(SecondKeyCode.Value);
		}
		return GlKeyNames.ToString((GlKeys)SecondKeyCode.Value);
	}

	private string MouseButtonAsString(int keyCode)
	{
		int num = keyCode - 240;
		return num switch
		{
			0 => Lang.Get("Left mouse button"), 
			1 => Lang.Get("Middle mouse button"), 
			2 => Lang.Get("Right mouse button"), 
			_ => Lang.Get("Mouse button {0}", num + 1), 
		};
	}
}
