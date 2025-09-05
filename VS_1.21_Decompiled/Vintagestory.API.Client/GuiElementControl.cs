namespace Vintagestory.API.Client;

public abstract class GuiElementControl : GuiElement
{
	protected bool enabled = true;

	public virtual bool Enabled
	{
		get
		{
			return enabled;
		}
		set
		{
			enabled = value;
		}
	}

	public GuiElementControl(ICoreClientAPI capi, ElementBounds bounds)
		: base(capi, bounds)
	{
	}
}
