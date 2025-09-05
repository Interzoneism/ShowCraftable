namespace Vintagestory.API.Client;

public static class GuiElementInsetHelper
{
	public static GuiComposer AddInset(this GuiComposer composer, ElementBounds bounds, int depth = 4, float brightness = 0.85f)
	{
		if (!composer.Composed)
		{
			composer.AddStaticElement(new GuiElementInset(composer.Api, bounds, depth, brightness));
		}
		return composer;
	}
}
