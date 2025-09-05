using System;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

internal class GuiElementNewVersionText : GuiElementTextBase
{
	private LoadedTexture texture;

	private LoadedTexture hoverTexture;

	public bool visible;

	public double offsetY;

	private int shadowHeight = 10;

	public Action<string> OnClicked;

	private string versionnumber;

	private double[] backColor = new double[4]
	{
		197.0 / 255.0,
		137.0 / 255.0,
		24.0 / 85.0,
		1.0
	};

	public GuiElementNewVersionText(ICoreClientAPI capi, CairoFont font, ElementBounds bounds)
		: base(capi, "", font, bounds)
	{
		texture = new LoadedTexture(capi);
		hoverTexture = new LoadedTexture(capi);
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
	}

	public void RecomposeMultiLine(string versionnumber)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Expected O, but got Unknown
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Expected O, but got Unknown
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_044c: Expected O, but got Unknown
		RightPadding = (float)GuiElement.scaled(25.0);
		text = Lang.Get((RuntimeEnv.OS == OS.Windows) ? "versionavailable-autoupdate" : "versionavailable-manualupdate", versionnumber);
		Bounds.fixedHeight = GetMultilineTextHeight() / (double)RuntimeEnv.GUIScale;
		Bounds.CalcWorldBounds();
		offsetY = -2.0 * Bounds.fixedHeight;
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight + shadowHeight);
		Context val2 = genContext(val);
		double num = GuiElement.scaled(15.0);
		double num2 = GuiElement.scaled(14.0);
		double num3 = (Bounds.InnerHeight - num2) / 2.0;
		double[] darkBrownColor = GuiStyle.DarkBrownColor;
		darkBrownColor[0] /= 2.0;
		darkBrownColor[1] /= 2.0;
		darkBrownColor[2] /= 2.0;
		LinearGradient val3 = new LinearGradient(0.0, (double)Bounds.OuterHeightInt, 0.0, (double)(Bounds.OuterHeightInt + 10));
		((Gradient)val3).AddColorStop(0.0, new Color(darkBrownColor[0], darkBrownColor[1], darkBrownColor[2], 1.0));
		((Gradient)val3).AddColorStop(1.0, new Color(darkBrownColor[0], darkBrownColor[1], darkBrownColor[2], 0.0));
		val2.SetSource((Pattern)(object)val3);
		val2.Rectangle(0.0, (double)Bounds.OuterHeightInt, (double)Bounds.OuterWidthInt, (double)(Bounds.OuterHeightInt + 10));
		val2.Fill();
		((Pattern)val3).Dispose();
		val3 = new LinearGradient(0.0, 0.0, Bounds.OuterWidth, 0.0);
		((Gradient)val3).AddColorStop(0.0, new Color(backColor[0], backColor[1], backColor[2], 1.0));
		((Gradient)val3).AddColorStop(0.99, new Color(backColor[0], backColor[1], backColor[2], 1.0));
		((Gradient)val3).AddColorStop(1.0, new Color(backColor[0], backColor[1], backColor[2], 0.0));
		val2.SetSource((Pattern)(object)val3);
		val2.Rectangle(0.0, 0.0, (double)Bounds.OuterWidthInt, (double)Bounds.OuterHeightInt);
		val2.Fill();
		((Pattern)val3).Dispose();
		val2.Arc(Bounds.drawX + num, Bounds.OuterHeight / 2.0, num2 / 2.0 + GuiElement.scaled(4.0), 0.0, Math.PI * 2.0);
		val2.SetSourceRGBA(GuiStyle.DarkBrownColor);
		val2.Fill();
		byte[] data = api.Assets.Get("textures/gui/newversion.png").Data;
		BitmapExternal bitmapExternal = api.Render.BitmapCreateFromPng(data);
		val.Image(bitmapExternal, (int)(Bounds.drawX + num - num2 / 2.0), (int)(Bounds.drawY + num3), (int)num2, (int)num2);
		bitmapExternal.Dispose();
		DrawMultilineTextAt(val2, Bounds.drawX + num + 20.0, Bounds.drawY);
		generateTexture(val, ref texture);
		val2.Dispose();
		((Surface)val).Dispose();
		val = new ImageSurface((Format)0, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		val2 = genContext(val);
		val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
		val2.Paint();
		generateTexture(val, ref hoverTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	internal void Activate(string versionnumber)
	{
		this.versionnumber = versionnumber;
		visible = true;
		RecomposeMultiLine(versionnumber);
		MouseOverCursor = "linkselect";
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (visible)
		{
			api.Render.Render2DTexturePremultipliedAlpha(texture.TextureId, (int)Bounds.renderX, (double)(int)Bounds.renderY + offsetY, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight + shadowHeight);
			if (Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
			{
				api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, (int)Bounds.renderX, (double)(int)Bounds.renderY + offsetY, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
			}
			offsetY = Math.Min(0.0, offsetY + (double)(100f * deltaTime));
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		if (visible && Bounds.PointInside(args.X, args.Y))
		{
			OnClicked(versionnumber);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		texture?.Dispose();
		hoverTexture?.Dispose();
	}
}
