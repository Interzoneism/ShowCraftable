using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class GuiElementTextButton : GuiElementControl
{
	private GuiElementStaticText normalText;

	private GuiElementStaticText pressedText;

	private LoadedTexture normalTexture;

	private LoadedTexture activeTexture;

	private LoadedTexture hoverTexture;

	private LoadedTexture disabledTexture;

	private ActionConsumable onClick;

	private bool isOver;

	private EnumButtonStyle buttonStyle;

	private bool active;

	private bool currentlyMouseDownOnElement;

	public bool PlaySound = true;

	public static double Padding = 2.0;

	private double textOffsetY;

	public bool Visible = true;

	public override bool Focusable => enabled;

	public string Text
	{
		get
		{
			return normalText.GetText();
		}
		set
		{
			normalText.Text = value;
			pressedText.Text = value;
		}
	}

	public GuiElementTextButton(ICoreClientAPI capi, string text, CairoFont font, CairoFont hoverFont, ActionConsumable onClick, ElementBounds bounds, EnumButtonStyle style = EnumButtonStyle.Normal)
		: base(capi, bounds)
	{
		hoverTexture = new LoadedTexture(capi);
		activeTexture = new LoadedTexture(capi);
		normalTexture = new LoadedTexture(capi);
		disabledTexture = new LoadedTexture(capi);
		buttonStyle = style;
		normalText = new GuiElementStaticText(capi, text, EnumTextOrientation.Center, bounds.CopyOnlySize(), font);
		normalText.AutoBoxSize(onlyGrow: true);
		pressedText = new GuiElementStaticText(capi, text, EnumTextOrientation.Center, bounds.CopyOnlySize(), hoverFont);
		this.onClick = onClick;
	}

	public void SetOrientation(EnumTextOrientation orientation)
	{
		normalText.orientation = orientation;
		pressedText.orientation = orientation;
	}

	public override void BeforeCalcBounds()
	{
		normalText.AutoBoxSize(onlyGrow: true);
		Bounds.fixedWidth = normalText.Bounds.fixedWidth;
		Bounds.fixedHeight = normalText.Bounds.fixedHeight;
		pressedText.Bounds = normalText.Bounds.CopyOnlySize();
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Expected O, but got Unknown
		Bounds.CalcWorldBounds();
		normalText.Bounds.CalcWorldBounds();
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context val2 = genContext(val);
		ComposeButton(val2, val);
		generateTexture(val, ref normalTexture);
		ContextUtils.Clear(val2);
		if (buttonStyle != EnumButtonStyle.None)
		{
			val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
			val2.Rectangle(0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
			val2.Fill();
		}
		pressedText.Bounds.fixedY += textOffsetY;
		pressedText.ComposeElements(val2, val);
		pressedText.Bounds.fixedY -= textOffsetY;
		generateTexture(val, ref activeTexture);
		ContextUtils.Clear(val2);
		if (buttonStyle != EnumButtonStyle.None)
		{
			val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
			val2.Rectangle(0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
			val2.Fill();
		}
		generateTexture(val, ref hoverTexture);
		val2.Dispose();
		((Surface)val).Dispose();
		val = new ImageSurface((Format)0, 2, 2);
		val2 = genContext(val);
		if (buttonStyle != EnumButtonStyle.None)
		{
			val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
			val2.Rectangle(0.0, 0.0, 2.0, 2.0);
			val2.Fill();
		}
		generateTexture(val, ref disabledTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	private void ComposeButton(Context ctx, ImageSurface surface)
	{
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		double num = GuiElement.scaled(2.5);
		if (buttonStyle == EnumButtonStyle.Normal || buttonStyle == EnumButtonStyle.Small)
		{
			num = GuiElement.scaled(1.5);
		}
		if (buttonStyle != EnumButtonStyle.None)
		{
			GuiElement.Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
			ctx.SetSourceRGBA(23.0 / 85.0, 52.0 / 255.0, 12.0 / 85.0, 0.8);
			ctx.Fill();
		}
		if (buttonStyle == EnumButtonStyle.MainMenu)
		{
			GuiElement.Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth, num);
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
			ctx.Fill();
		}
		if (buttonStyle == EnumButtonStyle.Normal || buttonStyle == EnumButtonStyle.Small)
		{
			GuiElement.Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth - num, num);
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
			ctx.Fill();
			GuiElement.Rectangle(ctx, 0.0, 0.0 + num, num, Bounds.OuterHeight - num);
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
			ctx.Fill();
		}
		SurfaceTransformBlur.BlurPartial(surface, 2.0, 5);
		FontExtents fontExtents = normalText.Font.GetFontExtents();
		TextExtents textExtents = normalText.Font.GetTextExtents(normalText.GetText());
		double num2 = 0.0 - ((FontExtents)(ref fontExtents)).Ascent - ((TextExtents)(ref textExtents)).YBearing;
		textOffsetY = (num2 + (normalText.Bounds.InnerHeight + ((TextExtents)(ref textExtents)).YBearing) / 2.0) / (double)RuntimeEnv.GUIScale;
		normalText.Bounds.fixedY += textOffsetY;
		normalText.ComposeElements(ctx, surface);
		normalText.Bounds.fixedY -= textOffsetY;
		Bounds.CalcWorldBounds();
		if (buttonStyle == EnumButtonStyle.MainMenu)
		{
			GuiElement.Rectangle(ctx, 0.0, 0.0 + Bounds.OuterHeight - num, Bounds.OuterWidth, num);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
			ctx.Fill();
		}
		if (buttonStyle == EnumButtonStyle.Normal || buttonStyle == EnumButtonStyle.Small)
		{
			GuiElement.Rectangle(ctx, 0.0 + num, 0.0 + Bounds.OuterHeight - num, Bounds.OuterWidth - 2.0 * num, num);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
			ctx.Fill();
			GuiElement.Rectangle(ctx, 0.0 + Bounds.OuterWidth - num, 0.0, num, Bounds.OuterHeight);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
			ctx.Fill();
		}
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (Visible)
		{
			api.Render.Render2DTexturePremultipliedAlpha(normalTexture.TextureId, Bounds);
			if (!enabled)
			{
				api.Render.Render2DTexturePremultipliedAlpha(disabledTexture.TextureId, Bounds);
			}
			else if (active || currentlyMouseDownOnElement)
			{
				api.Render.Render2DTexturePremultipliedAlpha(activeTexture.TextureId, Bounds);
			}
			else if (isOver)
			{
				api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds);
			}
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (!Visible || !base.HasFocus || args.KeyCode != 49)
		{
			return;
		}
		args.Handled = true;
		if (enabled)
		{
			if (PlaySound)
			{
				api.Gui.PlaySound("menubutton_press");
			}
			args.Handled = onClick();
		}
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		bool num = isOver;
		setIsOver();
		if (!num && isOver && PlaySound)
		{
			api.Gui.PlaySound("menubutton");
		}
	}

	protected void setIsOver()
	{
		isOver = Visible && enabled && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY);
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (Visible && enabled)
		{
			base.OnMouseDownOnElement(api, args);
			currentlyMouseDownOnElement = true;
			if (PlaySound)
			{
				api.Gui.PlaySound("menubutton_down");
			}
			setIsOver();
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		if (Visible)
		{
			if (currentlyMouseDownOnElement && !Bounds.PointInside(args.X, args.Y) && !active && PlaySound)
			{
				api.Gui.PlaySound("menubutton_up");
			}
			base.OnMouseUp(api, args);
			currentlyMouseDownOnElement = false;
		}
	}

	public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (enabled && currentlyMouseDownOnElement && Bounds.PointInside(args.X, args.Y) && (args.Button == EnumMouseButton.Left || args.Button == EnumMouseButton.Right))
		{
			args.Handled = onClick();
		}
		currentlyMouseDownOnElement = false;
	}

	public void SetActive(bool active)
	{
		this.active = active;
	}

	public override void Dispose()
	{
		base.Dispose();
		hoverTexture?.Dispose();
		activeTexture?.Dispose();
		pressedText?.Dispose();
		disabledTexture?.Dispose();
		normalTexture?.Dispose();
	}
}
