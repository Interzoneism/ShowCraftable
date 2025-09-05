namespace Vintagestory.API.Client;

public class KeyEvent
{
	public char KeyChar { get; set; }

	public int KeyCode { get; set; }

	public int? KeyCode2 { get; set; }

	public bool Handled { get; set; }

	public bool CtrlPressed { get; set; }

	public bool CommandPressed { get; set; }

	public bool ShiftPressed { get; set; }

	public bool AltPressed { get; set; }
}
