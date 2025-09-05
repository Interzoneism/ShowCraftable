namespace Vintagestory.API.Client;

public static class GuiElementGrayBackgroundHelpber
{
	public static GuiComposer AddGrayBG(this GuiComposer composer, ElementBounds bounds)
	{
		if (!composer.Composed)
		{
			composer.AddStaticElement(new GuiElementGrayBackground(composer.Api, bounds));
		}
		return composer;
	}
}
