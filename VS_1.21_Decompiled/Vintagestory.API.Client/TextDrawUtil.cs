using System;
using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class TextDrawUtil
{
	private int caretPos;

	private bool gotLinebreak;

	private bool gotSpace;

	public TextLine[] Lineize(Context ctx, string text, double boxwidth, double lineHeightMultiplier = 1.0, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.Default, bool keepLinebreakChar = false)
	{
		return Lineize(ctx, text, linebreak, new TextFlowPath[1]
		{
			new TextFlowPath(boxwidth)
		}, 0.0, 0.0, lineHeightMultiplier, keepLinebreakChar);
	}

	public int GetQuantityTextLines(CairoFont font, string text, double boxWidth, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.Default)
	{
		return GetQuantityTextLines(font, text, linebreak, new TextFlowPath[1]
		{
			new TextFlowPath(boxWidth)
		});
	}

	public double GetMultilineTextHeight(CairoFont font, string text, double boxWidth, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.Default)
	{
		return (double)GetQuantityTextLines(font, text, boxWidth, linebreak) * GetLineHeight(font);
	}

	public TextLine[] Lineize(CairoFont font, string fulltext, double boxWidth, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.Default, bool keepLinebreakChar = false)
	{
		return Lineize(font, fulltext, linebreak, new TextFlowPath[1]
		{
			new TextFlowPath(boxWidth)
		}, 0.0, 0.0, keepLinebreakChar);
	}

	public void AutobreakAndDrawMultilineText(Context ctx, CairoFont font, string text, double boxWidth, EnumTextOrientation orientation = EnumTextOrientation.Left)
	{
		AutobreakAndDrawMultilineText(ctx, font, text, 0.0, 0.0, new TextFlowPath[1]
		{
			new TextFlowPath(boxWidth)
		}, orientation);
	}

	public double AutobreakAndDrawMultilineTextAt(Context ctx, CairoFont font, string text, double posX, double posY, double boxWidth, EnumTextOrientation orientation = EnumTextOrientation.Left)
	{
		ctx.Save();
		Matrix matrix = ctx.Matrix;
		matrix.Translate((double)(int)posX, (double)(int)posY);
		ctx.Matrix = matrix;
		double result = AutobreakAndDrawMultilineText(ctx, font, text, 0.0, 0.0, new TextFlowPath[1]
		{
			new TextFlowPath(boxWidth)
		}, orientation);
		ctx.Restore();
		return result;
	}

	public void DrawMultilineTextAt(Context ctx, CairoFont font, TextLine[] lines, double posX, double posY, double boxWidth, EnumTextOrientation orientation = EnumTextOrientation.Left)
	{
		ctx.Save();
		Matrix matrix = ctx.Matrix;
		matrix.Translate(posX, posY);
		ctx.Matrix = matrix;
		font.SetupContext(ctx);
		DrawMultilineText(ctx, font, lines, orientation);
		ctx.Restore();
	}

	public double GetLineHeight(CairoFont font)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		FontExtents fontExtents = font.GetFontExtents();
		return ((FontExtents)(ref fontExtents)).Height * font.LineHeightMultiplier;
	}

	public int GetQuantityTextLines(CairoFont font, string text, EnumLinebreakBehavior linebreak, TextFlowPath[] flowPath, double lineY = 0.0)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		if (text == null || text.Length == 0)
		{
			return 0;
		}
		ImageSurface val = new ImageSurface((Format)0, 1, 1);
		Context val2 = new Context((Surface)(object)val);
		font.SetupContext(val2);
		int result = Lineize(val2, text, linebreak, flowPath, 0.0, lineY, font.LineHeightMultiplier).Length;
		val2.Dispose();
		((Surface)val).Dispose();
		return result;
	}

	public double GetMultilineTextHeight(CairoFont font, string text, EnumLinebreakBehavior linebreak, TextFlowPath[] flowPath, double lineY = 0.0)
	{
		return (double)GetQuantityTextLines(font, text, linebreak, flowPath, lineY) * GetLineHeight(font);
	}

	public TextLine[] Lineize(CairoFont font, string fulltext, EnumLinebreakBehavior linebreak, TextFlowPath[] flowPath, double startOffsetX = 0.0, double startY = 0.0, bool keepLinebreakChar = false)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		if (fulltext == null || fulltext.Length == 0)
		{
			return Array.Empty<TextLine>();
		}
		ImageSurface val = new ImageSurface((Format)0, 1, 1);
		Context val2 = new Context((Surface)(object)val);
		font.SetupContext(val2);
		TextLine[] result = Lineize(val2, fulltext, linebreak, flowPath, startOffsetX, startY, font.LineHeightMultiplier, keepLinebreakChar);
		val2.Dispose();
		((Surface)val).Dispose();
		return result;
	}

	public TextLine[] Lineize(Context ctx, string text, EnumLinebreakBehavior linebreak, TextFlowPath[] flowPath, double startOffsetX = 0.0, double startY = 0.0, double lineHeightMultiplier = 1.0, bool keepLinebreakChar = false)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		if (text == null || text.Length == 0)
		{
			return Array.Empty<TextLine>();
		}
		if (linebreak == EnumLinebreakBehavior.Default)
		{
			linebreak = Lang.AvailableLanguages[Lang.CurrentLocale].LineBreakBehavior;
		}
		StringBuilder stringBuilder = new StringBuilder();
		List<TextLine> list = new List<TextLine>();
		caretPos = 0;
		FontExtents fontExtents = ctx.FontExtents;
		double num = ((FontExtents)(ref fontExtents)).Height * lineHeightMultiplier;
		double num2 = startOffsetX;
		double num3 = startY;
		string text2;
		TextExtents val;
		TextFlowPath textFlowPath;
		double num4;
		while ((text2 = getNextWord(text, linebreak)) != null)
		{
			string text3 = ((gotLinebreak || caretPos >= text.Length || !gotSpace) ? "" : " ");
			val = ctx.TextExtents(stringBuilder?.ToString() + text2 + text3);
			double width = ((TextExtents)(ref val)).Width;
			textFlowPath = GetCurrentFlowPathSection(flowPath, num3);
			if (textFlowPath == null)
			{
				Console.WriteLine("Flow path underflow. Something in the text flow system is incorrectly programmed.");
				textFlowPath = new TextFlowPath(500.0);
			}
			num4 = textFlowPath.X2 - textFlowPath.X1 - num2;
			if (width >= num4)
			{
				if (text2.Length > 0 && stringBuilder.Length == 0 && startOffsetX == 0.0)
				{
					int num5 = 500;
					while (text2.Length > 0 && width >= num4 && num5-- > 0)
					{
						text2 = text2.Substring(0, text2.Length - 1);
						val = ctx.TextExtents(stringBuilder?.ToString() + text2 + text3);
						width = ((TextExtents)(ref val)).Width;
						caretPos--;
					}
					stringBuilder.Append(text2);
					text2 = "";
				}
				string text4 = stringBuilder.ToString();
				val = ctx.TextExtents(text4);
				double width2 = ((TextExtents)(ref val)).Width;
				TextLine obj = new TextLine
				{
					Text = text4
				};
				LineRectangled lineRectangled = new LineRectangled(textFlowPath.X1 + num2, num3, width2, num);
				fontExtents = ctx.FontExtents;
				lineRectangled.Ascent = ((FontExtents)(ref fontExtents)).Ascent;
				obj.Bounds = lineRectangled;
				obj.LeftSpace = 0.0;
				obj.RightSpace = num4 - width2;
				list.Add(obj);
				stringBuilder.Clear();
				num3 += num;
				num2 = 0.0;
				if (gotLinebreak)
				{
					textFlowPath = GetCurrentFlowPathSection(flowPath, num3);
				}
			}
			stringBuilder.Append(text2);
			if (gotSpace)
			{
				stringBuilder.Append(" ");
			}
			if (textFlowPath == null)
			{
				textFlowPath = new TextFlowPath();
			}
			if (gotLinebreak)
			{
				if (keepLinebreakChar)
				{
					stringBuilder.Append("\n");
				}
				string text5 = stringBuilder.ToString();
				val = ctx.TextExtents(text5);
				double width3 = ((TextExtents)(ref val)).Width;
				TextLine obj2 = new TextLine
				{
					Text = text5
				};
				LineRectangled lineRectangled2 = new LineRectangled(textFlowPath.X1 + num2, num3, width3, num);
				fontExtents = ctx.FontExtents;
				lineRectangled2.Ascent = ((FontExtents)(ref fontExtents)).Ascent;
				obj2.Bounds = lineRectangled2;
				obj2.LeftSpace = 0.0;
				obj2.NextOffsetX = num2;
				obj2.RightSpace = num4 - width3;
				list.Add(obj2);
				stringBuilder.Clear();
				num3 += num;
				num2 = 0.0;
			}
		}
		textFlowPath = GetCurrentFlowPathSection(flowPath, num3);
		if (textFlowPath == null)
		{
			textFlowPath = new TextFlowPath();
		}
		num4 = textFlowPath.X2 - textFlowPath.X1 - num2;
		string text6 = stringBuilder.ToString();
		val = ctx.TextExtents(text6);
		double width4 = ((TextExtents)(ref val)).Width;
		TextLine obj3 = new TextLine
		{
			Text = text6
		};
		LineRectangled lineRectangled3 = new LineRectangled(textFlowPath.X1 + num2, num3, width4, num);
		fontExtents = ctx.FontExtents;
		lineRectangled3.Ascent = ((FontExtents)(ref fontExtents)).Ascent;
		obj3.Bounds = lineRectangled3;
		obj3.LeftSpace = 0.0;
		obj3.NextOffsetX = num2;
		obj3.RightSpace = num4 - width4;
		list.Add(obj3);
		return list.ToArray();
	}

	private TextFlowPath GetCurrentFlowPathSection(TextFlowPath[] flowPath, double posY)
	{
		for (int i = 0; i < flowPath.Length; i++)
		{
			if (flowPath[i].Y1 <= posY && flowPath[i].Y2 >= posY)
			{
				return flowPath[i];
			}
		}
		return null;
	}

	private string getNextWord(string fulltext, EnumLinebreakBehavior linebreakBh)
	{
		if (caretPos >= fulltext.Length)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		gotLinebreak = false;
		gotSpace = false;
		bool flag = linebreakBh != EnumLinebreakBehavior.None;
		while (caretPos < fulltext.Length)
		{
			char c = fulltext[caretPos];
			caretPos++;
			if (c == ' ' && flag)
			{
				gotSpace = true;
				break;
			}
			switch (c)
			{
			case '\t':
				if (stringBuilder.Length > 0)
				{
					caretPos--;
					break;
				}
				return "  ";
			case '\r':
				gotLinebreak = true;
				if (caretPos <= fulltext.Length - 1 && fulltext[caretPos] == '\n')
				{
					caretPos++;
				}
				break;
			case '\n':
				gotLinebreak = true;
				break;
			default:
				goto IL_00cf;
			}
			break;
			IL_00cf:
			stringBuilder.Append(c);
			if (linebreakBh == EnumLinebreakBehavior.AfterCharacter)
			{
				break;
			}
		}
		return stringBuilder.ToString();
	}

	public double AutobreakAndDrawMultilineText(Context ctx, CairoFont font, string text, double lineX, double lineY, TextFlowPath[] flowPath, EnumTextOrientation orientation = EnumTextOrientation.Left, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.AfterWord)
	{
		TextLine[] array = Lineize(font, text, linebreak, flowPath, lineX, lineY);
		DrawMultilineText(ctx, font, array, orientation);
		if (array.Length == 0)
		{
			return 0.0;
		}
		return array[^1].Bounds.Y + array[^1].Bounds.Height;
	}

	public void DrawMultilineText(Context ctx, CairoFont font, TextLine[] lines, EnumTextOrientation orientation = EnumTextOrientation.Left)
	{
		font.SetupContext(ctx);
		double num = 0.0;
		foreach (TextLine textLine in lines)
		{
			if (textLine.Text.Length != 0)
			{
				if (orientation == EnumTextOrientation.Center)
				{
					num = (textLine.LeftSpace + textLine.RightSpace) / 2.0;
				}
				if (orientation == EnumTextOrientation.Right)
				{
					num = textLine.LeftSpace + textLine.RightSpace;
				}
				DrawTextLine(ctx, font, textLine.Text, num + textLine.Bounds.X, textLine.Bounds.Y);
			}
		}
	}

	public void DrawTextLine(Context ctx, CairoFont font, string text, double offsetX = 0.0, double offsetY = 0.0, bool textPathMode = false)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (text == null || text.Length == 0)
		{
			return;
		}
		double num = (int)offsetX;
		FontExtents fontExtents = ctx.FontExtents;
		ctx.MoveTo(num, (double)(int)(offsetY + ((FontExtents)(ref fontExtents)).Ascent));
		if (textPathMode)
		{
			ctx.TextPath(text);
		}
		else if (font.StrokeWidth > 0.0)
		{
			ctx.TextPath(text);
			ctx.LineWidth = font.StrokeWidth;
			ctx.SetSourceRGBA(font.StrokeColor);
			ctx.StrokePreserve();
			ctx.SetSourceRGBA(font.Color);
			ctx.Fill();
		}
		else
		{
			ctx.ShowText(text);
			if (font.RenderTwice)
			{
				ctx.ShowText(text);
			}
		}
	}
}
