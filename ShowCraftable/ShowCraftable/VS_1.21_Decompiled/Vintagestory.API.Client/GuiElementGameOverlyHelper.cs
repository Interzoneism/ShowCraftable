namespace Vintagestory.API.Client;

public static class GuiElementGameOverlyHelper
{
	public static GuiComposer AddGameOverlay(this GuiComposer composer, ElementBounds bounds, double[] backgroundColor = null)
	{
		if (!composer.Composed)
		{
			if (backgroundColor == null)
			{
				backgroundColor = GuiStyle.DialogDefaultBgColor;
			}
			composer.AddStaticElement(new GuiElementGameOverlay(composer.Api, bounds, backgroundColor));
		}
		return composer;
	}
}
