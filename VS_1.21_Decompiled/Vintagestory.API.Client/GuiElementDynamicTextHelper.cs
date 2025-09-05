using System;

namespace Vintagestory.API.Client;

public static class GuiElementDynamicTextHelper
{
	public static GuiComposer AddDynamicText(this GuiComposer composer, string text, CairoFont font, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			GuiElementDynamicText element = new GuiElementDynamicText(composer.Api, text, font, bounds);
			composer.AddInteractiveElement(element, key);
		}
		return composer;
	}

	[Obsolete("Use AddDymiacText without orientation attribute, that can be configured through the font")]
	public static GuiComposer AddDynamicText(this GuiComposer composer, string text, CairoFont font, EnumTextOrientation orientation, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			font = font.WithOrientation(orientation);
			GuiElementDynamicText element = new GuiElementDynamicText(composer.Api, text, font, bounds);
			composer.AddInteractiveElement(element, key);
		}
		return composer;
	}

	public static GuiElementDynamicText GetDynamicText(this GuiComposer composer, string key)
	{
		return (GuiElementDynamicText)composer.GetElement(key);
	}
}
