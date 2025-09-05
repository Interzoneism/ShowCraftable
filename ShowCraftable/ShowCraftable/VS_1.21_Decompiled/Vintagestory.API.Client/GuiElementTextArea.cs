using System;
using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementTextArea : GuiElementEditableTextBase
{
	private double minHeight;

	private LoadedTexture highlightTexture;

	private ElementBounds highlightBounds;

	public bool Autoheight = true;

	public GuiElementTextArea(ICoreClientAPI capi, ElementBounds bounds, Action<string> OnTextChanged, CairoFont font)
		: base(capi, font, bounds)
	{
		highlightTexture = new LoadedTexture(capi);
		multilineMode = true;
		minHeight = bounds.fixedHeight;
		base.OnTextChanged = OnTextChanged;
	}

	internal override void TextChanged()
	{
		if (Autoheight)
		{
			Bounds.fixedHeight = Math.Max(minHeight, textUtil.GetMultilineTextHeight(Font, string.Join("\n", lines), Bounds.InnerWidth));
		}
		Bounds.CalcWorldBounds();
		base.TextChanged();
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		EmbossRoundRectangleElement(ctx, Bounds, inverse: true, 3);
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.20000000298023224);
		ElementRoundRectangle(ctx, Bounds, isBackground: true, 3.0);
		ctx.Fill();
		GenerateHighlight();
		RecomposeText();
	}

	private void GenerateHighlight()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context obj = genContext(val);
		obj.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
		obj.Paint();
		generateTexture(val, ref highlightTexture);
		obj.Dispose();
		((Surface)val).Dispose();
		highlightBounds = Bounds.FlatCopy();
		highlightBounds.CalcWorldBounds();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (base.HasFocus)
		{
			api.Render.Render2DTexturePremultipliedAlpha(highlightTexture.TextureId, highlightBounds);
		}
		RenderTextSelection();
		api.Render.Render2DTexturePremultipliedAlpha(textTexture.TextureId, Bounds);
		base.RenderInteractiveElements(deltaTime);
	}

	public override void Dispose()
	{
		base.Dispose();
		highlightTexture.Dispose();
	}

	public void SetFont(CairoFont cairoFont)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		Font = cairoFont;
		FontExtents fontExtents = cairoFont.GetFontExtents();
		caretHeight = ((FontExtents)(ref fontExtents)).Height;
	}
}
