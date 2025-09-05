using System;
using Cairo;

namespace Vintagestory.API.Client;

public abstract class GuiElementElementListPickerBase<T> : GuiElementControl
{
	public Action<bool> handler;

	public bool On;

	private LoadedTexture activeTexture;

	private T elem;

	public bool ShowToolTip;

	private GuiElementHoverText hoverText;

	public string TooltipText
	{
		set
		{
			hoverText.SetNewText(value);
		}
	}

	public override bool Focusable => enabled;

	public GuiElementElementListPickerBase(ICoreClientAPI capi, T elem, ElementBounds bounds)
		: base(capi, bounds)
	{
		activeTexture = new LoadedTexture(capi);
		this.elem = elem;
		hoverText = new GuiElementHoverText(capi, "", CairoFont.WhiteSmallText(), 200, Bounds.CopyOnlySize());
		hoverText.Bounds.ParentBounds = bounds;
		hoverText.SetAutoWidth(on: true);
		bounds.ChildBounds.Add(hoverText.Bounds);
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		DrawElement(elem, ctx, surface);
		ComposeActiveButton();
		hoverText.ComposeElements(ctx, surface);
	}

	public abstract void DrawElement(T elem, Context ctx, ImageSurface surface);

	private void ComposeActiveButton()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.InnerWidth + 6, (int)Bounds.InnerHeight + 6);
		Context obj = genContext(val);
		obj.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		obj.Paint();
		obj.SetSourceRGBA(1.0, 1.0, 1.0, 0.65);
		GuiElement.RoundRectangle(obj, 3.0, 3.0, Bounds.InnerWidth + 1.0, Bounds.InnerHeight + 1.0, 1.0);
		obj.LineWidth = 2.0;
		obj.Stroke();
		generateTexture(val, ref activeTexture);
		obj.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (On)
		{
			api.Render.Render2DTexturePremultipliedAlpha(activeTexture.TextureId, Bounds.renderX - 3.0, Bounds.renderY - 3.0, activeTexture.Width, activeTexture.Height);
		}
		if (ShowToolTip)
		{
			hoverText.RenderInteractiveElements(deltaTime);
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		On = !On;
		handler?.Invoke(On);
		api.Gui.PlaySound("toggleswitch");
	}

	public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
	{
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseUp(api, args);
	}

	public void SetValue(bool on)
	{
		On = on;
	}

	public override void Dispose()
	{
		base.Dispose();
		activeTexture.Dispose();
		hoverText?.Dispose();
	}
}
