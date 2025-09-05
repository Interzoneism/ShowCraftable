using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public static class GuiElementImageBackgroundHelper
{
	public static GuiComposer AddImageBG(this GuiComposer composer, ElementBounds bounds, AssetLocation textureLoc, float brightness = 1f, float alpha = 1f, float scale = 1f)
	{
		if (!composer.Composed)
		{
			composer.AddStaticElement(new GuiElementImageBackground(composer.Api, bounds, textureLoc, brightness, alpha, scale));
		}
		return composer;
	}
}
