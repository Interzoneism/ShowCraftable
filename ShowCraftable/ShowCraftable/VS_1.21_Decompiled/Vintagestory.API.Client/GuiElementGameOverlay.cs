using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementGameOverlay : GuiElement
{
	private double[] bgcolor;

	public GuiElementGameOverlay(ICoreClientAPI capi, ElementBounds bounds, double[] bgcolor)
		: base(capi, bounds)
	{
		this.bgcolor = bgcolor;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		ctx.SetSourceRGBA(bgcolor);
		ctx.Rectangle(Bounds.bgDrawX, Bounds.bgDrawY, Bounds.OuterWidth, Bounds.OuterHeight);
		ctx.FillPreserve();
		ShadePath(ctx);
	}
}
