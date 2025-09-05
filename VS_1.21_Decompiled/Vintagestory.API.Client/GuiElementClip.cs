using Cairo;

namespace Vintagestory.API.Client;

internal class GuiElementClip : GuiElement
{
	private bool clip;

	public GuiElementClip(ICoreClientAPI capi, bool clip, ElementBounds bounds)
		: base(capi, bounds)
	{
		this.clip = clip;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (clip)
		{
			api.Render.PushScissor(Bounds);
		}
		else
		{
			api.Render.PopScissor();
		}
	}

	public override int OutlineColor()
	{
		return -65536;
	}

	public override void OnMouseDown(ICoreClientAPI api, MouseEvent mouse)
	{
	}
}
