using System;
using Cairo;

namespace Vintagestory.API.Client;

public class TextTextureUtil
{
	private TextBackground defaultBackground = new TextBackground();

	private ICoreClientAPI capi;

	public TextTextureUtil(ICoreClientAPI capi)
	{
		this.capi = capi;
	}

	public LoadedTexture GenTextTexture(string text, CairoFont font, int width, int height, TextBackground background = null, EnumTextOrientation orientation = EnumTextOrientation.Left, bool demulAlpha = false)
	{
		LoadedTexture loadedTexture = new LoadedTexture(capi);
		GenOrUpdateTextTexture(text, font, width, height, ref loadedTexture, background, orientation);
		return loadedTexture;
	}

	public void GenOrUpdateTextTexture(string text, CairoFont font, int width, int height, ref LoadedTexture loadedTexture, TextBackground background = null, EnumTextOrientation orientation = EnumTextOrientation.Left, bool demulAlpha = false)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		if (background == null)
		{
			background = defaultBackground;
		}
		ElementBounds bounds = new ElementBounds().WithFixedSize(width, height);
		ImageSurface val = new ImageSurface((Format)0, width, height);
		Context val2 = new Context((Surface)(object)val);
		GuiElementTextBase guiElementTextBase = new GuiElementTextBase(capi, text, font, bounds);
		val2.SetSourceRGBA(background.FillColor);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, width, height, background.Radius);
		if (background.BorderWidth > 0.0)
		{
			val2.FillPreserve();
			val2.Operator = (Operator)5;
			val2.LineWidth = background.BorderWidth;
			val2.SetSourceRGBA(background.BorderColor);
			val2.Stroke();
			val2.Operator = (Operator)2;
		}
		else
		{
			val2.Fill();
		}
		guiElementTextBase.textUtil.AutobreakAndDrawMultilineTextAt(val2, font, text, background.HorPadding, background.VerPadding, width, orientation);
		if (demulAlpha)
		{
			SurfaceTransformDemulAlpha.DemulAlpha(val);
		}
		capi.Gui.LoadOrUpdateCairoTexture(val, linearMag: false, ref loadedTexture);
		((Surface)val).Dispose();
		val2.Dispose();
	}

	public LoadedTexture GenTextTexture(string text, CairoFont font, int width, int height, TextBackground background = null)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		if (background == null)
		{
			background = defaultBackground;
		}
		ImageSurface val = new ImageSurface((Format)0, width, height);
		Context val2 = new Context((Surface)(object)val);
		if (background?.FillColor != null)
		{
			val2.SetSourceRGBA(background.FillColor);
			GuiElement.RoundRectangle(val2, 0.0, 0.0, width, height, background.Radius);
			val2.Fill();
		}
		if (background != null && background.Shade)
		{
			val2.SetSourceRGBA(GuiStyle.DialogLightBgColor[0] * 1.4, GuiStyle.DialogStrongBgColor[1] * 1.4, GuiStyle.DialogStrongBgColor[2] * 1.4, 1.0);
			val2.LineWidth = 5.0;
			GuiElement.RoundRectangle(val2, 0.0, 0.0, width, height, background.Radius);
			val2.StrokePreserve();
			SurfaceTransformBlur.BlurFull(val, 6.2);
			val2.SetSourceRGBA(new double[4]
			{
				0.17647058823529413,
				7.0 / 51.0,
				11.0 / 85.0,
				1.0
			});
			val2.LineWidth = background.BorderWidth;
			val2.Stroke();
		}
		if (background?.BorderColor != null)
		{
			val2.SetSourceRGBA(background.BorderColor);
			GuiElement.RoundRectangle(val2, 0.0, 0.0, width, height, background.Radius);
			val2.LineWidth = background.BorderWidth;
			val2.Stroke();
		}
		font.SetupContext(val2);
		FontExtents fontExtents = font.GetFontExtents();
		double height2 = ((FontExtents)(ref fontExtents)).Height;
		string[] array = text.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = array[i].TrimEnd();
			double num = background.HorPadding;
			double num2 = background.VerPadding;
			fontExtents = val2.FontExtents;
			val2.MoveTo(num, num2 + ((FontExtents)(ref fontExtents)).Ascent + (double)i * height2);
			if (font.StrokeWidth > 0.0)
			{
				val2.TextPath(array[i]);
				val2.LineWidth = font.StrokeWidth;
				val2.SetSourceRGBA(font.StrokeColor);
				val2.StrokePreserve();
				val2.SetSourceRGBA(font.Color);
				val2.Fill();
			}
			else
			{
				val2.ShowText(array[i]);
				if (font.RenderTwice)
				{
					val2.ShowText(array[i]);
				}
			}
		}
		int textureId = capi.Gui.LoadCairoTexture(val, linearMag: true);
		((Surface)val).Dispose();
		val2.Dispose();
		return new LoadedTexture(capi)
		{
			TextureId = textureId,
			Width = width,
			Height = height
		};
	}

	public LoadedTexture GenTextTexture(string text, CairoFont font, TextBackground background = null)
	{
		LoadedTexture loadedTexture = new LoadedTexture(capi);
		GenOrUpdateTextTexture(text, font, ref loadedTexture, background);
		return loadedTexture;
	}

	public void GenOrUpdateTextTexture(string text, CairoFont font, ref LoadedTexture loadedTexture, TextBackground background = null)
	{
		if (background == null)
		{
			background = defaultBackground.Clone();
			if (font.StrokeWidth > 0.0)
			{
				background.Padding = (int)Math.Ceiling(font.StrokeWidth);
			}
		}
		ElementBounds elementBounds = new ElementBounds();
		font.AutoBoxSize(text, elementBounds);
		int width = (int)Math.Ceiling(GuiElement.scaled(elementBounds.fixedWidth + 1.0 + (double)(2 * background.HorPadding)));
		int height = (int)Math.Ceiling(GuiElement.scaled(elementBounds.fixedHeight + 1.0 + (double)(2 * background.VerPadding)));
		GenOrUpdateTextTexture(text, font, width, height, ref loadedTexture, background);
	}

	public LoadedTexture GenUnscaledTextTexture(string text, CairoFont font, TextBackground background = null)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		if (background == null)
		{
			background = defaultBackground;
		}
		double num = 0.0;
		string[] array = text.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = array[i].TrimEnd();
			TextExtents textExtents = font.GetTextExtents(array[i]);
			num = Math.Max(((TextExtents)(ref textExtents)).Width, num);
		}
		FontExtents fontExtents = font.GetFontExtents();
		int width = (int)num + 1 + 2 * background.HorPadding;
		int height = (int)((FontExtents)(ref fontExtents)).Height * array.Length + 1 + 2 * background.VerPadding;
		return GenTextTexture(text, font, width, height, background);
	}

	public LoadedTexture GenTextTexture(string text, CairoFont font, int maxWidth, TextBackground background = null, EnumTextOrientation orientation = EnumTextOrientation.Left)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (background == null)
		{
			background = defaultBackground;
		}
		double val = 0.0;
		string[] array = text.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = array[i].TrimEnd();
			TextExtents textExtents = font.GetTextExtents(array[i]);
			val = Math.Max(((TextExtents)(ref textExtents)).Width, val);
		}
		int num = (int)Math.Min(maxWidth, val) + 2 * background.HorPadding;
		double num2 = new TextDrawUtil().GetMultilineTextHeight(font, text, num) + (double)(2 * background.VerPadding);
		return GenTextTexture(text, font, num, (int)num2 + 1, background, orientation);
	}
}
