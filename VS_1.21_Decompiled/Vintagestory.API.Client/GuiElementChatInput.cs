using System;
using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementChatInput : GuiElementEditableTextBase
{
	private LoadedTexture highlightTexture;

	private ElementBounds highlightBounds;

	public GuiElementChatInput(ICoreClientAPI capi, ElementBounds bounds, Action<string> OnTextChanged)
		: base(capi, null, bounds)
	{
		highlightTexture = new LoadedTexture(capi);
		base.OnTextChanged = OnTextChanged;
		caretColor = new float[4] { 1f, 1f, 1f, 1f };
		Font = CairoFont.WhiteSmallText();
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Expected O, but got Unknown
		ctx.LineWidth = 1.0;
		ctx.NewPath();
		ctx.MoveTo(Bounds.drawX + 1.0, Bounds.drawY);
		ctx.LineTo(Bounds.drawX + 1.0 + Bounds.InnerWidth, Bounds.drawY);
		ctx.ClosePath();
		ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.7);
		ctx.Stroke();
		ctx.NewPath();
		ctx.MoveTo(Bounds.drawX + 1.0, Bounds.drawY + 1.0);
		ctx.LineTo(Bounds.drawX + 1.0 + Bounds.InnerWidth, Bounds.drawY + 1.0);
		ctx.ClosePath();
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.7);
		ctx.Stroke();
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context obj = genContext(val);
		obj.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		obj.Paint();
		obj.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
		obj.Paint();
		generateTexture(val, ref highlightTexture);
		obj.Dispose();
		((Surface)val).Dispose();
		highlightBounds = Bounds.CopyOffsetedSibling().WithFixedPadding(0.0, 0.0).FixedGrow(2.0 * Bounds.absPaddingX, 2.0 * Bounds.absPaddingY);
		highlightBounds.CalcWorldBounds();
		RecomposeText();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (hasFocus)
		{
			api.Render.Render2DTexturePremultipliedAlpha(highlightTexture.TextureId, highlightBounds);
		}
		api.Render.GlScissor((int)Bounds.renderX, (int)((double)api.Render.FrameHeight - Bounds.renderY - Bounds.InnerHeight), Bounds.OuterWidthInt + 1 - (int)rightSpacing, Bounds.OuterHeightInt + 1 - (int)bottomSpacing);
		api.Render.GlScissorFlag(enable: true);
		RenderTextSelection();
		api.Render.Render2DTexturePremultipliedAlpha(textTexture.TextureId, Bounds.renderX - renderLeftOffset, Bounds.renderY, textSize.X, textSize.Y);
		api.Render.GlScissorFlag(enable: false);
		base.RenderInteractiveElements(deltaTime);
	}

	public override void Dispose()
	{
		base.Dispose();
		highlightTexture.Dispose();
	}
}
