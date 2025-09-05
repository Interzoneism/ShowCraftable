using System;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class CairoFont : FontConfig, IDisposable
{
	private static ImageSurface surface;

	public static Context FontMeasuringContext;

	public bool RenderTwice;

	public double LineHeightMultiplier = 1.0;

	private FontOptions CairoFontOptions;

	public FontSlant Slant;

	public EnumTextOrientation Orientation;

	static CairoFont()
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		surface = new ImageSurface((Format)0, 1, 1);
		FontMeasuringContext = new Context((Surface)(object)surface);
	}

	public CairoFont()
	{
	}

	public CairoFont(FontConfig config)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		UnscaledFontsize = config.UnscaledFontsize;
		Fontname = config.Fontname;
		FontWeight = config.FontWeight;
		Color = config.Color;
		StrokeColor = config.StrokeColor;
		StrokeWidth = config.StrokeWidth;
	}

	public CairoFont(double unscaledFontSize, string fontName)
	{
		UnscaledFontsize = unscaledFontSize;
		Fontname = fontName;
	}

	public CairoFont WithLineHeightMultiplier(double lineHeightMul)
	{
		LineHeightMultiplier = lineHeightMul;
		return this;
	}

	public CairoFont WithStroke(double[] color, double width)
	{
		StrokeColor = color;
		StrokeWidth = width;
		return this;
	}

	public CairoFont(double unscaledFontSize, string fontName, double[] color, double[] strokeColor = null)
	{
		UnscaledFontsize = unscaledFontSize;
		Fontname = fontName;
		Color = color;
		StrokeColor = strokeColor;
		if (StrokeColor != null)
		{
			StrokeWidth = 1.0;
		}
	}

	public void AutoFontSize(string text, ElementBounds bounds, bool onlyShrink = true)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		double unscaledFontsize = UnscaledFontsize;
		UnscaledFontsize = 50.0;
		double unscaledFontsize2 = UnscaledFontsize;
		double num = bounds.InnerWidth - 1.0;
		TextExtents textExtents = GetTextExtents(text);
		UnscaledFontsize = unscaledFontsize2 * (num / ((TextExtents)(ref textExtents)).Width);
		if (onlyShrink)
		{
			UnscaledFontsize = Math.Min(UnscaledFontsize, unscaledFontsize);
		}
	}

	public void AutoBoxSize(string text, ElementBounds bounds, bool onlyGrow = false)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		double num = 0.0;
		double num2 = 0.0;
		FontExtents fontExtents = GetFontExtents();
		if (text.Contains('\n'))
		{
			string[] array = text.Split('\n');
			for (int i = 0; i < array.Length && (array[i].Length != 0 || i != array.Length - 1); i++)
			{
				TextExtents textExtents = GetTextExtents(array[i]);
				num = Math.Max(num, ((TextExtents)(ref textExtents)).Width);
				num2 += ((FontExtents)(ref fontExtents)).Height;
			}
		}
		else
		{
			TextExtents textExtents2 = GetTextExtents(text);
			num = ((TextExtents)(ref textExtents2)).Width;
			num2 = ((FontExtents)(ref fontExtents)).Height;
		}
		if (text.Length == 0)
		{
			num = 0.0;
			num2 = 0.0;
		}
		if (onlyGrow)
		{
			bounds.fixedWidth = Math.Max(bounds.fixedWidth, num / (double)RuntimeEnv.GUIScale + 1.0);
			bounds.fixedHeight = Math.Max(bounds.fixedHeight, num2 / (double)RuntimeEnv.GUIScale);
		}
		else
		{
			bounds.fixedWidth = Math.Max(1.0, num / (double)RuntimeEnv.GUIScale + 1.0);
			bounds.fixedHeight = Math.Max(1.0, num2 / (double)RuntimeEnv.GUIScale);
		}
	}

	public CairoFont WithColor(double[] color)
	{
		Color = (double[])color.Clone();
		return this;
	}

	public CairoFont WithWeight(FontWeight weight)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		FontWeight = weight;
		return this;
	}

	public CairoFont WithRenderTwice()
	{
		RenderTwice = true;
		return this;
	}

	public CairoFont WithSlant(FontSlant slant)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		Slant = slant;
		return this;
	}

	public CairoFont WithFont(string fontname)
	{
		Fontname = fontname;
		return this;
	}

	public void SetupContext(Context ctx)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		ctx.SetFontSize(GuiElement.scaled(UnscaledFontsize));
		ctx.SelectFontFace(Fontname, Slant, FontWeight);
		CairoFontOptions = new FontOptions();
		CairoFontOptions.Antialias = (Antialias)3;
		ctx.FontOptions = CairoFontOptions;
		if (Color != null)
		{
			if (Color.Length == 3)
			{
				ctx.SetSourceRGB(Color[0], Color[1], Color[2]);
			}
			if (Color.Length == 4)
			{
				ctx.SetSourceRGBA(Color[0], Color[1], Color[2], Color[3]);
			}
		}
	}

	public FontExtents GetFontExtents()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		SetupContext(FontMeasuringContext);
		return FontMeasuringContext.FontExtents;
	}

	public TextExtents GetTextExtents(string text)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		SetupContext(FontMeasuringContext);
		return FontMeasuringContext.TextExtents(text);
	}

	public CairoFont Clone()
	{
		CairoFont cairoFont = (CairoFont)MemberwiseClone();
		cairoFont.Color = new double[Color.Length];
		Array.Copy(Color, cairoFont.Color, Color.Length);
		return cairoFont;
	}

	public CairoFont WithFontSize(float fontSize)
	{
		UnscaledFontsize = fontSize;
		return this;
	}

	public static CairoFont SmallButtonText(EnumButtonStyle style = EnumButtonStyle.Normal)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		return new CairoFont
		{
			Color = (double[])GuiStyle.ButtonTextColor.Clone(),
			FontWeight = (FontWeight)(style != EnumButtonStyle.Small),
			Orientation = EnumTextOrientation.Center,
			Fontname = GuiStyle.StandardFontName,
			UnscaledFontsize = GuiStyle.SmallFontSize
		};
	}

	public static CairoFont ButtonText()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		return new CairoFont
		{
			Color = (double[])GuiStyle.ButtonTextColor.Clone(),
			FontWeight = (FontWeight)1,
			Orientation = EnumTextOrientation.Center,
			Fontname = GuiStyle.DecorativeFontName,
			UnscaledFontsize = 24.0
		};
	}

	public static CairoFont ButtonPressedText()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		return new CairoFont
		{
			Color = (double[])GuiStyle.ActiveButtonTextColor.Clone(),
			FontWeight = (FontWeight)1,
			Fontname = GuiStyle.DecorativeFontName,
			Orientation = EnumTextOrientation.Center,
			UnscaledFontsize = 24.0
		};
	}

	public CairoFont WithOrientation(EnumTextOrientation orientation)
	{
		Orientation = orientation;
		return this;
	}

	public static CairoFont TextInput()
	{
		CairoFont cairoFont = new CairoFont();
		cairoFont.Color = new double[4] { 1.0, 1.0, 1.0, 0.9 };
		cairoFont.Fontname = GuiStyle.StandardFontName;
		cairoFont.UnscaledFontsize = 18.0;
		return cairoFont;
	}

	public static CairoFont SmallTextInput()
	{
		CairoFont cairoFont = new CairoFont();
		cairoFont.Color = new double[4] { 0.0, 0.0, 0.0, 0.9 };
		cairoFont.Fontname = GuiStyle.StandardFontName;
		cairoFont.UnscaledFontsize = GuiStyle.SmallFontSize;
		return cairoFont;
	}

	public static CairoFont WhiteMediumText()
	{
		return new CairoFont
		{
			Color = (double[])GuiStyle.DialogDefaultTextColor.Clone(),
			Fontname = GuiStyle.StandardFontName,
			UnscaledFontsize = GuiStyle.NormalFontSize
		};
	}

	public static CairoFont WhiteSmallishText()
	{
		return new CairoFont
		{
			Color = (double[])GuiStyle.DialogDefaultTextColor.Clone(),
			Fontname = GuiStyle.StandardFontName,
			UnscaledFontsize = GuiStyle.SmallishFontSize
		};
	}

	public static CairoFont WhiteSmallishText(string baseFont)
	{
		return new CairoFont
		{
			Color = (double[])GuiStyle.DialogDefaultTextColor.Clone(),
			Fontname = baseFont,
			UnscaledFontsize = GuiStyle.SmallishFontSize
		};
	}

	public static CairoFont WhiteSmallText()
	{
		return new CairoFont
		{
			Color = (double[])GuiStyle.DialogDefaultTextColor.Clone(),
			Fontname = GuiStyle.StandardFontName,
			UnscaledFontsize = GuiStyle.SmallFontSize
		};
	}

	public static CairoFont WhiteDetailText()
	{
		return new CairoFont
		{
			Color = (double[])GuiStyle.DialogDefaultTextColor.Clone(),
			Fontname = GuiStyle.StandardFontName,
			UnscaledFontsize = GuiStyle.DetailFontSize
		};
	}

	public void Dispose()
	{
		CairoFontOptions.Dispose();
	}
}
