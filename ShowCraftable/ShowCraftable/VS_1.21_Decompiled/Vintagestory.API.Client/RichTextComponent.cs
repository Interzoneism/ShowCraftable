using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class RichTextComponent : RichTextComponentBase
{
	protected TextDrawUtil textUtil;

	protected EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.AfterWord;

	public string DisplayText;

	public CairoFont Font;

	public TextLine[] Lines;

	private double spaceWidthCached = -99.0;

	private double spaceWidth
	{
		get
		{
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			if (spaceWidthCached >= 0.0)
			{
				return spaceWidthCached;
			}
			TextExtents textExtents = Font.GetTextExtents("a b");
			double width = ((TextExtents)(ref textExtents)).Width;
			textExtents = Font.GetTextExtents("ab");
			return spaceWidthCached = (width - ((TextExtents)(ref textExtents)).Width) / (double)RuntimeEnv.GUIScale;
		}
	}

	public RichTextComponent(ICoreClientAPI api, string displayText, CairoFont font)
		: base(api)
	{
		DisplayText = displayText;
		Font = font;
		linebreak = Lang.AvailableLanguages[Lang.CurrentLocale].LineBreakBehavior;
		if (api != null)
		{
			init();
		}
	}

	protected void init()
	{
		if (DisplayText.Length > 0)
		{
			bool flag = false;
			while (!flag && DisplayText.Length > 0)
			{
				flag = true;
				if (DisplayText[DisplayText.Length - 1] == ' ')
				{
					PaddingRight += spaceWidth;
					flag = false;
					DisplayText = DisplayText.Substring(0, DisplayText.Length - 1);
				}
				if (DisplayText.Length == 0)
				{
					break;
				}
				if (DisplayText[0] == ' ')
				{
					PaddingLeft += spaceWidth;
					DisplayText = DisplayText.Substring(1, DisplayText.Length - 1);
					flag = false;
				}
			}
		}
		else
		{
			PaddingLeft = 0.0;
			PaddingRight = 0.0;
		}
		textUtil = new TextDrawUtil();
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		textUtil.DrawMultilineText(ctx, Font, Lines, Font.Orientation);
	}

	public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
	}

	public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		offsetX += GuiElement.scaled(PaddingLeft);
		Lines = textUtil.Lineize(Font, DisplayText, linebreak, flowPath, offsetX, lineY);
		nextOffsetX = offsetX;
		BoundsPerLine = new LineRectangled[Lines.Length];
		for (int i = 0; i < Lines.Length; i++)
		{
			TextLine textLine = Lines[i];
			BoundsPerLine[i] = textLine.Bounds;
		}
		if (Lines.Length != 0)
		{
			LineRectangled lineRectangled = BoundsPerLine[Lines.Length - 1];
			lineRectangled.Width += GuiElement.scaled(PaddingRight);
			nextOffsetX = Lines[Lines.Length - 1].NextOffsetX + lineRectangled.Width;
		}
		if (Lines.Length <= 1)
		{
			return EnumCalcBoundsResult.Continue;
		}
		return EnumCalcBoundsResult.Multiline;
	}

	protected double GetFontOrientOffsetX()
	{
		if (Lines.Length == 0)
		{
			return 0.0;
		}
		TextLine textLine = Lines[Lines.Length - 1];
		double result = 0.0;
		if (Font.Orientation == EnumTextOrientation.Center)
		{
			result = (textLine.LeftSpace + textLine.RightSpace) / 2.0;
		}
		if (Font.Orientation == EnumTextOrientation.Right)
		{
			result = textLine.LeftSpace + textLine.RightSpace;
		}
		return result;
	}
}
