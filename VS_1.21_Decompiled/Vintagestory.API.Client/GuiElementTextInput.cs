using System;
using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementTextInput : GuiElementEditableTextBase
{
	protected LoadedTexture highlightTexture;

	protected ElementBounds highlightBounds;

	internal bool DeleteOnRefocusBackSpace;

	protected int refocusStage;

	private LoadedTexture placeHolderTextTexture;

	private bool focusLostSinceKeyDown;

	public override bool Enabled
	{
		get
		{
			return base.Enabled;
		}
		set
		{
			enabled = value;
			MouseOverCursor = (value ? "textselect" : null);
		}
	}

	public GuiElementTextInput(ICoreClientAPI capi, ElementBounds bounds, Action<string> onTextChanged, CairoFont font)
		: base(capi, font, bounds)
	{
		MouseOverCursor = "textselect";
		OnTextChanged = onTextChanged;
		highlightTexture = new LoadedTexture(capi);
	}

	public void HideCharacters()
	{
		hideCharacters = true;
	}

	public void SetPlaceHolderText(string text)
	{
		TextTextureUtil textTextureUtil = new TextTextureUtil(api);
		placeHolderTextTexture?.Dispose();
		CairoFont cairoFont = Font.Clone();
		cairoFont.Color[3] *= 0.5;
		placeHolderTextTexture = textTextureUtil.GenTextTexture(text, cairoFont);
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected O, but got Unknown
		EmbossRoundRectangleElement(ctx, Bounds, inverse: true, 2, 1);
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
		ElementRoundRectangle(ctx, Bounds, isBackground: false, 1.0);
		ctx.Fill();
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context obj = genContext(val);
		obj.SetSourceRGBA(1.0, 1.0, 1.0, 0.2);
		obj.Paint();
		if (!enabled)
		{
			Font.Color[3] = 0.3499999940395355;
		}
		generateTexture(val, ref highlightTexture);
		obj.Dispose();
		((Surface)val).Dispose();
		highlightBounds = Bounds.CopyOffsetedSibling().WithFixedPadding(0.0, 0.0).FixedGrow(2.0 * Bounds.absPaddingX, 2.0 * Bounds.absPaddingY);
		highlightBounds.CalcWorldBounds();
		RecomposeText();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (base.HasFocus)
		{
			api.Render.GlToggleBlend(blend: true);
			api.Render.Render2DTexture(highlightTexture.TextureId, highlightBounds);
		}
		else if (placeHolderTextTexture != null && (text == null || text.Length == 0) && (lines == null || lines.Count == 0 || lines[0] == null || lines[0] == ""))
		{
			api.Render.GlToggleBlend(blend: true);
			api.Render.Render2DTexturePremultipliedAlpha(placeHolderTextTexture.TextureId, (int)(highlightBounds.renderX + highlightBounds.absPaddingX + 3.0), (int)(highlightBounds.renderY + highlightBounds.absPaddingY + (highlightBounds.OuterHeight - (double)placeHolderTextTexture.Height) / 2.0), placeHolderTextTexture.Width, placeHolderTextTexture.Height);
		}
		api.Render.GlScissor((int)Bounds.renderX, (int)((double)api.Render.FrameHeight - Bounds.renderY - Bounds.InnerHeight), Math.Max(0, Bounds.OuterWidthInt + 1 - (int)rightSpacing), Math.Max(0, Bounds.OuterHeightInt + 1 - (int)bottomSpacing));
		api.Render.GlScissorFlag(enable: true);
		RenderTextSelection();
		api.Render.Render2DTexturePremultipliedAlpha(textTexture.TextureId, Bounds.renderX - renderLeftOffset, Bounds.renderY, textSize.X, textSize.Y);
		api.Render.GlScissorFlag(enable: false);
		base.RenderInteractiveElements(deltaTime);
	}

	public override void OnFocusLost()
	{
		focusLostSinceKeyDown = true;
		base.OnFocusLost();
	}

	public override void OnFocusGained()
	{
		base.OnFocusGained();
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (DeleteOnRefocusBackSpace && args.KeyCode == 53 && focusLostSinceKeyDown)
		{
			SetValue("");
			return;
		}
		focusLostSinceKeyDown = false;
		base.OnKeyDown(api, args);
	}

	public override void Dispose()
	{
		base.Dispose();
		highlightTexture.Dispose();
		placeHolderTextTexture?.Dispose();
	}
}
