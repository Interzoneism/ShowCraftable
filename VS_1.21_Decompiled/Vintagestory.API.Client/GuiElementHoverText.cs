using System;
using Cairo;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementHoverText : GuiElementTextBase
{
	public static TextBackground DefaultBackground = new TextBackground
	{
		Padding = 5,
		Radius = 1.0,
		FillColor = GuiStyle.DialogStrongBgColor,
		BorderColor = GuiStyle.DialogBorderColor,
		BorderWidth = 3.0,
		Shade = true
	};

	private LoadedTexture hoverTexture;

	private int unscaledMaxWidth;

	private double hoverWidth;

	private double hoverHeight;

	private bool autoDisplay = true;

	private bool visible;

	private bool isnowshown;

	private bool followMouse = true;

	private bool autoWidth;

	public bool fillBounds;

	public TextBackground Background;

	private Vec4f rendercolor;

	private double padding;

	private float zPosition = 500f;

	private GuiElementRichtext descriptionElement;

	public Vec4f RenderColor
	{
		get
		{
			return rendercolor;
		}
		set
		{
			rendercolor = value;
			descriptionElement.RenderColor = value;
		}
	}

	public float ZPosition
	{
		get
		{
			return zPosition;
		}
		set
		{
			zPosition = value;
			descriptionElement.zPos = value;
		}
	}

	public bool IsVisible => visible;

	public bool IsNowShown => isnowshown;

	public override double DrawOrder => 0.9;

	public GuiElementHoverText(ICoreClientAPI capi, string text, CairoFont font, int maxWidth, ElementBounds bounds, TextBackground background = null)
		: base(capi, text, font, bounds)
	{
		Background = background;
		if (background == null)
		{
			Background = DefaultBackground;
		}
		unscaledMaxWidth = maxWidth;
		hoverTexture = new LoadedTexture(capi);
		padding = Background.HorPadding;
		ElementBounds elementBounds = bounds.CopyOnlySize();
		elementBounds.WithFixedPadding(0.0);
		elementBounds.WithParent(bounds);
		elementBounds.IsDrawingSurface = true;
		elementBounds.fixedWidth = maxWidth;
		descriptionElement = new GuiElementRichtext(capi, Array.Empty<RichTextComponentBase>(), elementBounds);
		descriptionElement.zPos = 1001f;
	}

	public override void BeforeCalcBounds()
	{
		base.BeforeCalcBounds();
		descriptionElement.BeforeCalcBounds();
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
	}

	public override int OutlineColor()
	{
		return -2130706688;
	}

	public override void RenderBoundsDebug()
	{
		api.Render.RenderRectangle((int)Bounds.renderX, (int)Bounds.renderY, 550f, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight, OutlineColor());
	}

	private void RecalcBounds()
	{
		double fixedWidth = descriptionElement.Bounds.fixedWidth;
		fixedWidth = Math.Min(autoWidth ? (descriptionElement.MaxLineWidth / (double)RuntimeEnv.GUIScale) : fixedWidth, unscaledMaxWidth);
		hoverWidth = fixedWidth + 2.0 * padding;
		double value = Math.Max(descriptionElement.Bounds.fixedHeight + 2.0 * padding, 20.0);
		hoverHeight = GuiElement.scaled(value);
		hoverWidth = GuiElement.scaled(hoverWidth);
	}

	private void Recompose()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		descriptionElement.SetNewText(text, Font);
		RecalcBounds();
		Bounds.CalcWorldBounds();
		Bounds.CopyOnlySize().CalcWorldBounds();
		ImageSurface val = new ImageSurface((Format)0, (int)Math.Ceiling(hoverWidth), (int)Math.Ceiling(hoverHeight));
		Context val2 = genContext(val);
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		val2.Paint();
		if (Background?.FillColor != null)
		{
			val2.SetSourceRGBA(Background.FillColor);
			GuiElement.RoundRectangle(val2, 0.0, 0.0, hoverWidth, hoverHeight, Background.Radius);
			val2.Fill();
		}
		TextBackground background = Background;
		if (background != null && background.Shade)
		{
			val2.SetSourceRGBA(GuiStyle.DialogLightBgColor[0] * 1.4, GuiStyle.DialogStrongBgColor[1] * 1.4, GuiStyle.DialogStrongBgColor[2] * 1.4, 1.0);
			GuiElement.RoundRectangle(val2, 0.0, 0.0, hoverWidth, hoverHeight, Background.Radius);
			val2.LineWidth = Background.BorderWidth * 1.75;
			val2.Stroke();
			SurfaceTransformBlur.BlurFull(val, 8.2);
		}
		if (Background?.BorderColor != null)
		{
			val2.SetSourceRGBA(Background.BorderColor);
			GuiElement.RoundRectangle(val2, 0.0, 0.0, hoverWidth, hoverHeight, Background.Radius);
			val2.LineWidth = Background.BorderWidth;
			val2.Stroke();
		}
		generateTexture(val, ref hoverTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (text == null || text.Length == 0)
		{
			return;
		}
		if (api.Render.ScissorStack.Count > 0)
		{
			api.Render.GlScissorFlag(enable: false);
		}
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		isnowshown = false;
		if ((autoDisplay && IsPositionInside(mouseX, mouseY)) || visible)
		{
			isnowshown = true;
			if (hoverTexture.TextureId == 0 && !hoverTexture.Disposed)
			{
				Recompose();
			}
			int num = (int)GuiElement.scaled(padding);
			double num2 = Bounds.renderX;
			double num3 = Bounds.renderY;
			if (followMouse)
			{
				num2 = (double)mouseX + GuiElement.scaled(10.0);
				num3 = (double)mouseY + GuiElement.scaled(15.0);
			}
			if (num2 + hoverWidth > (double)api.Render.FrameWidth)
			{
				num2 -= num2 + hoverWidth - (double)api.Render.FrameWidth;
			}
			if (num3 + hoverHeight > (double)api.Render.FrameHeight)
			{
				num3 -= num3 + hoverHeight - (double)api.Render.FrameHeight;
			}
			api.Render.Render2DTexture(hoverTexture.TextureId, (int)num2 + (int)Bounds.absPaddingX, (int)num3 + (int)Bounds.absPaddingY, (int)hoverWidth + 1, (int)hoverHeight + 1, zPosition, RenderColor);
			Bounds.renderOffsetX = num2 - Bounds.renderX + (double)num;
			Bounds.renderOffsetY = num3 - Bounds.renderY + (double)num;
			descriptionElement.RenderColor = rendercolor;
			descriptionElement.RenderAsPremultipliedAlpha = base.RenderAsPremultipliedAlpha;
			descriptionElement.RenderInteractiveElements(deltaTime);
			Bounds.renderOffsetX = 0.0;
			Bounds.renderOffsetY = 0.0;
		}
		if (api.Render.ScissorStack.Count > 0)
		{
			api.Render.GlScissorFlag(enable: true);
		}
	}

	public void SetNewText(string text)
	{
		base.text = text;
		Recompose();
	}

	public void SetAutoDisplay(bool on)
	{
		autoDisplay = on;
	}

	public void SetVisible(bool on)
	{
		visible = on;
	}

	public void SetAutoWidth(bool on)
	{
		autoWidth = on;
	}

	public void SetFollowMouse(bool on)
	{
		followMouse = on;
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
	}

	public override void Dispose()
	{
		base.Dispose();
		hoverTexture.Dispose();
		descriptionElement.Dispose();
	}
}
