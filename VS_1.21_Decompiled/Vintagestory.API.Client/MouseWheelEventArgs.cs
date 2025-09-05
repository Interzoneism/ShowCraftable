namespace Vintagestory.API.Client;

public class MouseWheelEventArgs
{
	public int delta;

	public float deltaPrecise;

	public int value;

	public float valuePrecise;

	public bool IsHandled { get; private set; }

	public void SetHandled(bool value = true)
	{
		IsHandled = value;
	}
}
