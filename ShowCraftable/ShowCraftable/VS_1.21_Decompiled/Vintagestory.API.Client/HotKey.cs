using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public class HotKey
{
	public bool IsGlobalHotkey;

	public bool IsIngameHotkey;

	public KeyCombination CurrentMapping;

	public KeyCombination DefaultMapping;

	public string Code;

	public string Name;

	public HotkeyType KeyCombinationType = HotkeyType.CharacterControls;

	public ActionConsumable<KeyCombination> Handler;

	public bool TriggerOnUpAlso;

	public virtual bool DidPress(KeyEvent keyEventargs, IWorldAccessor world, IPlayer player, bool allowCharacterControls)
	{
		if (keyEventargs.KeyCode == CurrentMapping.KeyCode && (MouseControlsIgnoreModifiers() || (keyEventargs.AltPressed == CurrentMapping.Alt && keyEventargs.CtrlPressed == CurrentMapping.Ctrl && keyEventargs.ShiftPressed == CurrentMapping.Shift)) && ((KeyCombinationType != HotkeyType.CharacterControls && KeyCombinationType != HotkeyType.MovementControls) || allowCharacterControls))
		{
			if (keyEventargs.KeyCode2 != CurrentMapping.SecondKeyCode && CurrentMapping.SecondKeyCode.HasValue)
			{
				return CurrentMapping.SecondKeyCode == 0;
			}
			return true;
		}
		return false;
	}

	private bool MouseControlsIgnoreModifiers()
	{
		if (CurrentMapping.IsMouseButton(CurrentMapping.KeyCode))
		{
			return !CurrentMapping.Alt && !CurrentMapping.Ctrl && !CurrentMapping.Shift;
		}
		return false;
	}

	public virtual bool FallbackDidPress(KeyEvent keyEventargs, IWorldAccessor world, IPlayer player, bool allowCharacterControls)
	{
		if (!CurrentMapping.Alt && !CurrentMapping.Ctrl && !CurrentMapping.Shift && keyEventargs.KeyCode == CurrentMapping.KeyCode && (keyEventargs.KeyCode2 == CurrentMapping.SecondKeyCode || !CurrentMapping.SecondKeyCode.HasValue || CurrentMapping.SecondKeyCode == 0))
		{
			return (KeyCombinationType != HotkeyType.CharacterControls && KeyCombinationType != HotkeyType.MovementControls) || allowCharacterControls;
		}
		return false;
	}

	public HotKey Clone()
	{
		HotKey obj = (HotKey)MemberwiseClone();
		obj.CurrentMapping = CurrentMapping.Clone();
		obj.DefaultMapping = DefaultMapping.Clone();
		return obj;
	}

	public void SetDefaultMapping()
	{
		DefaultMapping = new KeyCombination
		{
			KeyCode = CurrentMapping.KeyCode,
			SecondKeyCode = CurrentMapping.SecondKeyCode,
			Alt = CurrentMapping.Alt,
			Ctrl = CurrentMapping.Ctrl,
			Shift = CurrentMapping.Shift
		};
	}
}
