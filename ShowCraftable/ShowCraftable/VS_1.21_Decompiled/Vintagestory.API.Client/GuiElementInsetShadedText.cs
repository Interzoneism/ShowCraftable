using Cairo;

namespace Vintagestory.API.Client;

internal class GuiElementInsetShadedText : GuiElementTextBase
{
	public GuiElementInsetShadedText(ICoreClientAPI capi, string text, CairoFont font, ElementBounds bounds)
		: base(capi, text, font, bounds)
	{
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		ctx.Operator = (Operator)2;
		Font.SetupContext(ctx);
		ctx.MoveTo((double)(int)Bounds.drawX, (double)(int)Bounds.drawY);
		DrawTextLineAt(ctx, text, 0.0, 0.0);
	}
}
