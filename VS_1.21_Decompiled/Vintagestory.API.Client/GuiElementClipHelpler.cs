namespace Vintagestory.API.Client;

public static class GuiElementClipHelpler
{
	public static GuiComposer BeginClip(this GuiComposer composer, ElementBounds bounds)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementClip(composer.Api, clip: true, bounds));
			composer.InsideClipBounds = bounds;
			composer.BeginChildElements();
		}
		return composer;
	}

	public static GuiComposer EndClip(this GuiComposer composer)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementClip(composer.Api, clip: false, ElementBounds.Empty));
			composer.InsideClipBounds = null;
			composer.EndChildElements();
		}
		return composer;
	}
}
