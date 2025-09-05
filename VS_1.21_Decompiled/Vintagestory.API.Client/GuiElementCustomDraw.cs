using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementCustomDraw : GuiElement
{
	private DrawDelegateWithBounds OnDraw;

	private bool interactive;

	private int texId;

	public GuiElementCustomDraw(ICoreClientAPI capi, ElementBounds bounds, DrawDelegateWithBounds OnDraw, bool interactive = false)
		: base(capi, bounds)
	{
		this.OnDraw = OnDraw;
		this.interactive = interactive;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		Bounds.CalcWorldBounds();
		if (!interactive)
		{
			OnDraw(ctxStatic, surfaceStatic, Bounds);
		}
		else
		{
			Redraw();
		}
	}

	public void Redraw()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		Context val2 = new Context((Surface)(object)val);
		OnDraw(val2, val, Bounds);
		generateTexture(val, ref texId);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (interactive)
		{
			api.Render.Render2DTexture(texId, Bounds);
		}
	}
}
