using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementCustomRender : GuiElement
{
	private RenderDelegateWithBounds onRender;

	public GuiElementCustomRender(ICoreClientAPI capi, ElementBounds bounds, RenderDelegateWithBounds onRender)
		: base(capi, bounds)
	{
		this.onRender = onRender;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		Bounds.CalcWorldBounds();
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		onRender(deltaTime, Bounds);
	}
}
