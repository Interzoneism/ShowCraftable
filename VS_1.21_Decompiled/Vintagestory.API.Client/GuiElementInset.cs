using Cairo;

namespace Vintagestory.API.Client;

internal class GuiElementInset : GuiElement
{
	private int depth;

	private float brightness;

	public GuiElementInset(ICoreClientAPI capi, ElementBounds bounds, int depth, float brightness)
		: base(capi, bounds)
	{
		this.depth = depth;
		this.brightness = brightness;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		if (brightness < 1f)
		{
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, (double)(1f - brightness));
			GuiElement.Rectangle(ctx, Bounds);
			ctx.Fill();
		}
		EmbossRoundRectangleElement(ctx, Bounds, inverse: true, depth);
	}
}
