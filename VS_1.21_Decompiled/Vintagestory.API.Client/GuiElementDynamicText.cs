using System;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class GuiElementDynamicText : GuiElementTextBase
{
	private EnumTextOrientation orientation;

	private LoadedTexture textTexture;

	public Action OnClick;

	public bool autoHeight;

	public int QuantityTextLines => textUtil.GetQuantityTextLines(Font, text, Bounds.InnerWidth);

	public GuiElementDynamicText(ICoreClientAPI capi, string text, CairoFont font, ElementBounds bounds)
		: base(capi, text, font, bounds)
	{
		orientation = font.Orientation;
		textTexture = new LoadedTexture(capi);
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		RecomposeText();
	}

	public void AutoHeight()
	{
		Bounds.fixedHeight = GetMultilineTextHeight() / (double)RuntimeEnv.GUIScale;
		Bounds.CalcWorldBounds();
		autoHeight = true;
	}

	public void RecomposeText(bool async = false)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		if (autoHeight)
		{
			AutoHeight();
		}
		if (async)
		{
			TyronThreadPool.QueueTask(delegate
			{
				//IL_0027: Unknown result type (might be due to invalid IL or missing references)
				//IL_0031: Expected O, but got Unknown
				ImageSurface surface = new ImageSurface((Format)0, (int)Bounds.InnerWidth, (int)Bounds.InnerHeight);
				Context ctx = genContext(surface);
				DrawMultilineTextAt(ctx, 0.0, 0.0, orientation);
				api.Event.EnqueueMainThreadTask(delegate
				{
					generateTexture(surface, ref textTexture);
					ctx.Dispose();
					((Surface)surface).Dispose();
				}, "recompstatbar");
			});
		}
		else
		{
			ImageSurface val = new ImageSurface((Format)0, (int)Bounds.InnerWidth, (int)Bounds.InnerHeight);
			Context val2 = genContext(val);
			DrawMultilineTextAt(val2, 0.0, 0.0, orientation);
			generateTexture(val, ref textTexture);
			val2.Dispose();
			((Surface)val).Dispose();
		}
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexturePremultipliedAlpha(textTexture.TextureId, (int)Bounds.renderX, (int)Bounds.renderY, (int)Bounds.InnerWidth, (int)Bounds.InnerHeight);
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		OnClick?.Invoke();
	}

	public void SetNewTextAsync(string text, bool autoHeight = false, bool forceRedraw = false)
	{
		SetNewText(text, autoHeight, forceRedraw, async: true);
	}

	public void SetNewText(string text, bool autoHeight = false, bool forceRedraw = false, bool async = false)
	{
		if (base.text != text || forceRedraw)
		{
			base.text = text;
			Bounds.CalcWorldBounds();
			if (autoHeight)
			{
				AutoHeight();
			}
			RecomposeText(async);
		}
	}

	public override void Dispose()
	{
		textTexture?.Dispose();
	}
}
