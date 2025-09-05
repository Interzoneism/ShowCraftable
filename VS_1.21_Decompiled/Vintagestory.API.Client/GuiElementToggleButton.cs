using System;
using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementToggleButton : GuiElementTextBase
{
	private Action<bool> handler;

	public bool Toggleable;

	public bool On;

	private LoadedTexture releasedTexture;

	private LoadedTexture pressedTexture;

	private LoadedTexture hoverTexture;

	private int unscaledDepth = 4;

	private string icon;

	private double pressedYOffset;

	private double nonPressedYOffset;

	public override bool Focusable => enabled;

	public GuiElementToggleButton(ICoreClientAPI capi, string icon, string text, CairoFont font, Action<bool> OnToggled, ElementBounds bounds, bool toggleable = false)
		: base(capi, text, font, bounds)
	{
		releasedTexture = new LoadedTexture(capi);
		pressedTexture = new LoadedTexture(capi);
		hoverTexture = new LoadedTexture(capi);
		handler = OnToggled;
		Toggleable = toggleable;
		this.icon = icon;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		ComposeReleasedButton();
		ComposePressedButton();
	}

	private void ComposeReleasedButton()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		double num = GuiElement.scaled(unscaledDepth);
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context val2 = genContext(val);
		val2.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, GuiStyle.ElementBGRadius);
		val2.FillPreserve();
		val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
		val2.Fill();
		EmbossRoundRectangleElement(val2, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, inverse: false, (int)num);
		TextExtents textExtents = Font.GetTextExtents(GetText());
		double yBearing = ((TextExtents)(ref textExtents)).YBearing;
		FontExtents fontExtents = Font.GetFontExtents();
		nonPressedYOffset = 0.0 - ((FontExtents)(ref fontExtents)).Ascent - yBearing + (Bounds.InnerHeight + yBearing) / 2.0 - 2.0;
		DrawMultilineTextAt(val2, Bounds.absPaddingX, Bounds.absPaddingY + nonPressedYOffset, EnumTextOrientation.Center);
		if (icon != null && icon.Length > 0)
		{
			api.Gui.Icons.DrawIcon(val2, icon, Bounds.absPaddingX + GuiElement.scaled(4.0), Bounds.absPaddingY + GuiElement.scaled(4.0), Bounds.InnerWidth - GuiElement.scaled(9.0), Bounds.InnerHeight - GuiElement.scaled(9.0), Font.Color);
		}
		generateTexture(val, ref releasedTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	private void ComposePressedButton()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Expected O, but got Unknown
		double num = GuiElement.scaled(unscaledDepth);
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context val2 = genContext(val);
		val2.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, GuiStyle.ElementBGRadius);
		val2.FillPreserve();
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.1);
		val2.Fill();
		EmbossRoundRectangleElement(val2, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, inverse: true, (int)num);
		TextExtents textExtents = Font.GetTextExtents(GetText());
		double yBearing = ((TextExtents)(ref textExtents)).YBearing;
		FontExtents fontExtents = Font.GetFontExtents();
		pressedYOffset = 0.0 - ((FontExtents)(ref fontExtents)).Ascent - yBearing + (Bounds.InnerHeight + yBearing) / 2.0;
		DrawMultilineTextAt(val2, Bounds.absPaddingX, Bounds.absPaddingY + pressedYOffset, EnumTextOrientation.Center);
		if (icon != null && icon.Length > 0)
		{
			val2.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
			api.Gui.Icons.DrawIcon(val2, icon, Bounds.absPaddingX + GuiElement.scaled(4.0), Bounds.absPaddingY + GuiElement.scaled(4.0), Bounds.InnerWidth - GuiElement.scaled(8.0), Bounds.InnerHeight - GuiElement.scaled(8.0), GuiStyle.DialogDefaultTextColor);
		}
		generateTexture(val, ref pressedTexture);
		val2.Dispose();
		((Surface)val).Dispose();
		val = new ImageSurface((Format)0, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		val2 = genContext(val);
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		val2.Fill();
		double[] color = Font.Color;
		Font.Color = GuiStyle.ActiveButtonTextColor;
		DrawMultilineTextAt(val2, Bounds.absPaddingX + pressedYOffset + GuiElement.scaled(4.0), 0.0, EnumTextOrientation.Center);
		if (icon != null && icon.Length > 0)
		{
			val2.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
			api.Gui.Icons.DrawIcon(val2, icon, Bounds.absPaddingX + GuiElement.scaled(4.0), Bounds.absPaddingY + GuiElement.scaled(4.0), Bounds.InnerWidth - GuiElement.scaled(8.0), Bounds.InnerHeight - GuiElement.scaled(8.0), GuiStyle.DialogDefaultTextColor);
		}
		Font.Color = color;
		generateTexture(val, ref hoverTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexturePremultipliedAlpha(On ? pressedTexture.TextureId : releasedTexture.TextureId, Bounds);
		if (icon == null && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds.renderX, Bounds.renderY + (On ? pressedYOffset : nonPressedYOffset), Bounds.OuterWidthInt, Bounds.OuterHeightInt);
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
		if (!Toggleable)
		{
			On = false;
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		if (!Toggleable)
		{
			On = false;
		}
		base.OnMouseUp(api, args);
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (base.HasFocus && args.KeyCode == 49)
		{
			args.Handled = true;
			On = !On;
			handler?.Invoke(On);
			api.Gui.PlaySound("toggleswitch");
		}
	}

	public void SetValue(bool on)
	{
		On = on;
	}

	public override void Dispose()
	{
		base.Dispose();
		releasedTexture.Dispose();
		pressedTexture.Dispose();
		hoverTexture.Dispose();
	}
}
