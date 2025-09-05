using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Client;

public interface IInputAPI
{
	string ClipboardText { get; set; }

	bool[] KeyboardKeyStateRaw { get; }

	bool[] KeyboardKeyState { get; }

	MouseButtonState MouseButton { get; }

	MouseButtonState InWorldMouseButton { get; }

	int MouseX { get; }

	int MouseY { get; }

	float MouseYaw { get; set; }

	float MousePitch { get; set; }

	bool MouseWorldInteractAnyway { get; set; }

	bool MouseGrabbed { get; }

	OrderedDictionary<string, HotKey> HotKeys { get; }

	event OnEntityAction InWorldAction;

	void TriggerOnMouseEnterSlot(ItemSlot slot);

	void TriggerOnMouseLeaveSlot(ItemSlot itemSlot);

	void TriggerOnMouseClickSlot(ItemSlot itemSlot);

	void RegisterHotKey(string hotkeyCode, string name, GlKeys key, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false);

	void RegisterHotKeyFirst(string hotkeyCode, string name, GlKeys key, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false);

	void SetHotKeyHandler(string hotkeyCode, ActionConsumable<KeyCombination> handler);

	void AddHotkeyListener(OnHotKeyDelegate handler);

	HotKey GetHotKeyByCode(string toggleKeyCombinationCode);

	bool IsHotKeyPressed(string hotKeyCode);

	bool IsHotKeyPressed(HotKey hotKey);
}
