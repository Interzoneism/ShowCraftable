using System;
using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public abstract class GuiElementEditableTextBase : GuiElementTextBase
{
	public delegate bool OnTryTextChangeDelegate(List<string> lines);

	private struct TextSelection
	{
		internal int Start;

		internal int End;

		internal TextSelection(int start, int end)
		{
			Start = start;
			End = end;
		}
	}

	private enum CopyCutMode
	{
		Copy,
		Cut
	}

	private struct SelectedTextPos
	{
		internal int X;

		internal int Y;

		internal SelectedTextPos(int x, int y)
		{
			X = x;
			Y = y;
		}
	}

	private const int DoubleClickMilliseconds = 400;

	internal float[] caretColor = new float[4] { 1f, 1f, 1f, 1f };

	internal bool hideCharacters;

	internal bool multilineMode;

	internal int maxlines = 99999;

	internal double caretX;

	internal double caretY;

	internal double topPadding;

	internal double leftPadding = 3.0;

	internal double rightSpacing;

	internal double bottomSpacing;

	private int? selectedTextStart;

	private long lastClickTime;

	private int lastClickCursor;

	private bool handlingOnKeyEvent;

	private bool mouseDown;

	internal LoadedTexture caretTexture;

	internal LoadedTexture textTexture;

	private int selectionTextureId;

	public Action<int, int> OnCaretPositionChanged;

	public Action<string> OnTextChanged;

	public OnTryTextChangeDelegate OnTryTextChangeText;

	public Action<double, double> OnCursorMoved;

	internal Action OnFocused;

	internal Action OnLostFocus;

	public Action OnKeyPressed;

	internal long caretBlinkMilliseconds;

	internal bool caretDisplayed;

	internal double caretHeight;

	internal double renderLeftOffset;

	internal Vec2i textSize = new Vec2i();

	protected List<string> lines;

	protected List<string> linesStaging;

	public bool WordWrap = true;

	protected int pcaretPosLine;

	protected int pcaretPosInLine;

	public int TextLengthWithoutLineBreaks
	{
		get
		{
			int num = 0;
			for (int i = 0; i < lines.Count; i++)
			{
				num += lines[i].Length;
			}
			return num;
		}
	}

	public int CaretPosWithoutLineBreaks
	{
		get
		{
			int num = 0;
			for (int i = 0; i < CaretPosLine; i++)
			{
				num += lines[i].Length;
			}
			return num + CaretPosInLine;
		}
		set
		{
			int num = 0;
			for (int i = 0; i < lines.Count; i++)
			{
				int length = lines[i].Length;
				if (num + length > value)
				{
					SetCaretPos(value - num, i);
					return;
				}
				num += length;
			}
			if (!multilineMode)
			{
				SetCaretPos(num);
			}
			else
			{
				SetCaretPos(num, lines.Count);
			}
		}
	}

	public int CaretPosLine
	{
		get
		{
			return pcaretPosLine;
		}
		set
		{
			pcaretPosLine = value;
		}
	}

	public int CaretPosInLine
	{
		get
		{
			return pcaretPosInLine;
		}
		set
		{
			if (value > lines[CaretPosLine].Length)
			{
				throw new IndexOutOfRangeException("Caret @" + value + ", cannot beyond current line length of " + pcaretPosInLine);
			}
			pcaretPosInLine = value;
		}
	}

	public override bool Focusable => enabled;

	public List<string> GetLines()
	{
		return new List<string>(lines);
	}

	public GuiElementEditableTextBase(ICoreClientAPI capi, CairoFont font, ElementBounds bounds)
		: base(capi, "", font, bounds)
	{
		caretTexture = new LoadedTexture(capi);
		textTexture = new LoadedTexture(capi);
		selectionTextureId = GenerateSelectionTexture(capi);
		lines = new List<string> { "" };
		linesStaging = new List<string> { "" };
	}

	public override void OnFocusGained()
	{
		base.OnFocusGained();
		OnFocused?.Invoke();
	}

	public override void OnFocusLost()
	{
		base.OnFocusLost();
		selectedTextStart = null;
		OnLostFocus?.Invoke();
	}

	public void SetCaretPos(double x, double y)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		CaretPosLine = 0;
		ImageSurface val = new ImageSurface((Format)0, 1, 1);
		Context val2 = genContext(val);
		Font.SetupContext(val2);
		if (multilineMode)
		{
			FontExtents fontExtents = val2.FontExtents;
			double num = y / ((FontExtents)(ref fontExtents)).Height;
			if (num > (double)lines.Count)
			{
				CaretPosLine = lines.Count - 1;
				CaretPosInLine = lines[CaretPosLine].Length;
				val2.Dispose();
				((Surface)val).Dispose();
				return;
			}
			CaretPosLine = Math.Max(0, (int)num);
		}
		CaretPosLine = Math.Clamp(CaretPosLine, 0, lines.Count - 1);
		string text = lines[CaretPosLine].TrimEnd('\r', '\n');
		CaretPosInLine = text.Length;
		for (int i = 0; i < text.Length; i++)
		{
			TextExtents val3 = val2.TextExtents(text.Substring(0, i + 1));
			double xAdvance = ((TextExtents)(ref val3)).XAdvance;
			if (x - xAdvance <= 0.0)
			{
				CaretPosInLine = i;
				break;
			}
		}
		val2.Dispose();
		((Surface)val).Dispose();
		SetCaretPos(CaretPosInLine, CaretPosLine);
	}

	public void SetCaretPos(int posInLine, int posLine = 0)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		caretBlinkMilliseconds = api.ElapsedMilliseconds;
		caretDisplayed = true;
		CaretPosLine = GameMath.Clamp(posLine, 0, lines.Count - 1);
		CaretPosInLine = GameMath.Clamp(posInLine, 0, lines[CaretPosLine].TrimEnd('\r', '\n').Length);
		TextExtents textExtents;
		if (multilineMode)
		{
			textExtents = Font.GetTextExtents(lines[CaretPosLine].Substring(0, CaretPosInLine));
			caretX = ((TextExtents)(ref textExtents)).XAdvance;
			FontExtents fontExtents = Font.GetFontExtents();
			caretY = ((FontExtents)(ref fontExtents)).Height * (double)CaretPosLine;
		}
		else
		{
			string text = lines[0];
			if (hideCharacters)
			{
				text = new StringBuilder(lines[0]).Insert(0, "•", text.Length).ToString();
			}
			textExtents = Font.GetTextExtents(text.Substring(0, CaretPosInLine));
			caretX = ((TextExtents)(ref textExtents)).XAdvance;
			caretY = 0.0;
		}
		OnCursorMoved?.Invoke(caretX, caretY);
		renderLeftOffset = Math.Max(0.0, caretX - Bounds.InnerWidth + rightSpacing);
		OnCaretPositionChanged?.Invoke(posLine, posInLine);
	}

	public void SetValue(float value)
	{
		SetValue(value.ToString(GlobalConstants.DefaultCultureInfo));
	}

	public void SetValue(double value)
	{
		SetValue(value.ToString(GlobalConstants.DefaultCultureInfo));
	}

	public void SetValue(string text, bool setCaretPosToEnd = true)
	{
		LoadValue(Lineize(text));
		if (setCaretPosToEnd)
		{
			int length = lines[lines.Count - 1].Length;
			SetCaretPos(length, lines.Count - 1);
		}
	}

	public virtual void LoadValue(List<string> newLines)
	{
		OnTryTextChangeDelegate onTryTextChangeText = OnTryTextChangeText;
		if ((onTryTextChangeText != null && !onTryTextChangeText(newLines)) || (newLines.Count > maxlines && newLines.Count >= lines.Count))
		{
			linesStaging = new List<string>(lines);
			return;
		}
		lines = new List<string>(newLines);
		linesStaging = new List<string>(lines);
		CaretPosLine = Math.Clamp(CaretPosLine, 0, lines.Count - 1);
		CaretPosInLine = Math.Clamp(CaretPosInLine, 0, lines[CaretPosLine].Length);
		TextChanged();
	}

	public List<string> Lineize(string text)
	{
		if (text == null)
		{
			text = "";
		}
		List<string> list = new List<string>();
		text = text.Replace("\r\n", "\n").Replace('\r', '\n');
		if (multilineMode)
		{
			double boxWidth = Bounds.InnerWidth - 2.0 * Bounds.absPaddingX;
			if (!WordWrap)
			{
				boxWidth = 999999.0;
			}
			TextLine[] array = textUtil.Lineize(Font, text, boxWidth, EnumLinebreakBehavior.Default, keepLinebreakChar: true);
			foreach (TextLine textLine in array)
			{
				list.Add(textLine.Text);
			}
			if (list.Count == 0)
			{
				list.Add("");
			}
		}
		else
		{
			list.Add(text);
		}
		return list;
	}

	internal virtual void TextChanged()
	{
		OnTextChanged?.Invoke(string.Join("", lines));
		if (!handlingOnKeyEvent)
		{
			selectedTextStart = null;
		}
		RecomposeText();
	}

	internal virtual void RecomposeText()
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Expected O, but got Unknown
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Expected O, but got Unknown
		Bounds.CalcWorldBounds();
		string text = null;
		if (multilineMode)
		{
			textSize.X = (int)(Bounds.OuterWidth - rightSpacing);
			textSize.Y = (int)(Bounds.OuterHeight - bottomSpacing);
		}
		else
		{
			text = lines[0];
			if (hideCharacters)
			{
				text = new StringBuilder(text.Length).Insert(0, "•", text.Length).ToString();
			}
			Vec2i vec2i = textSize;
			double val = Bounds.InnerWidth - rightSpacing;
			TextExtents textExtents = Font.GetTextExtents(text);
			vec2i.X = (int)Math.Max(val, ((TextExtents)(ref textExtents)).Width);
			textSize.Y = (int)(Bounds.InnerHeight - bottomSpacing);
		}
		ImageSurface val2 = new ImageSurface((Format)0, textSize.X, textSize.Y);
		Context val3 = genContext(val2);
		Font.SetupContext(val3);
		FontExtents fontExtents = val3.FontExtents;
		double height = ((FontExtents)(ref fontExtents)).Height;
		if (multilineMode)
		{
			double boxWidth = Bounds.InnerWidth - 2.0 * Bounds.absPaddingX - rightSpacing;
			TextLine[] array = new TextLine[lines.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new TextLine
				{
					Text = lines[i].Replace("\r\n", "").Replace("\n", ""),
					Bounds = new LineRectangled(0.0, (double)i * height, Bounds.InnerWidth, height)
				};
			}
			textUtil.DrawMultilineTextAt(val3, Font, array, Bounds.absPaddingX + leftPadding, Bounds.absPaddingY, boxWidth);
		}
		else
		{
			double num = Bounds.OuterHeight - bottomSpacing;
			fontExtents = val3.FontExtents;
			topPadding = Math.Max(0.0, num - ((FontExtents)(ref fontExtents)).Height) / 2.0;
			textUtil.DrawTextLine(val3, Font, text, Bounds.absPaddingX + leftPadding, Bounds.absPaddingY + topPadding);
		}
		generateTexture(val2, ref textTexture);
		val3.Dispose();
		((Surface)val2).Dispose();
		if (caretTexture.TextureId == 0)
		{
			caretHeight = height;
			val2 = new ImageSurface((Format)0, 3, (int)height);
			val3 = genContext(val2);
			Font.SetupContext(val3);
			val3.SetSourceRGBA((double)caretColor[0], (double)caretColor[1], (double)caretColor[2], (double)caretColor[3]);
			val3.LineWidth = 1.0;
			val3.NewPath();
			val3.MoveTo(2.0, 0.0);
			val3.LineTo(2.0, height);
			val3.ClosePath();
			val3.Stroke();
			generateTexture(val2, ref caretTexture.TextureId);
			val3.Dispose();
			((Surface)val2).Dispose();
		}
	}

	private void DeleteSelectedText(int caretPos, int caretPosOffset)
	{
		TextSelection selection = GetSelection(caretPos);
		SetValue(GetText().Remove(selection.Start, selection.End - selection.Start), setCaretPosToEnd: false);
		selectedTextStart = null;
		if (caretPos == selection.End)
		{
			CaretPosWithoutLineBreaks = selection.Start + caretPosOffset;
		}
	}

	private void DeleteSelectedTextAndFixCursor(int caretPos, int caretPosOffset)
	{
		if (caretPos < selectedTextStart)
		{
			selectedTextStart += caretPosOffset;
			DeleteSelectedText(caretPos + caretPosOffset, caretPosOffset);
		}
		else
		{
			DeleteSelectedText(caretPos, caretPosOffset);
		}
	}

	private static int GetCharRank(char c)
	{
		if (IsWordChar(c))
		{
			return 3;
		}
		if (!char.IsWhiteSpace(c))
		{
			return 2;
		}
		return 1;
	}

	private void SelectWordAtCursor()
	{
		string text = lines[CaretPosLine];
		int caretPosInLine = CaretPosInLine;
		int num = ((caretPosInLine > 0) ? GetCharRank(text[caretPosInLine - 1]) : 0);
		if (caretPosInLine < text.Length)
		{
			num = Math.Max(num, GetCharRank(text[caretPosInLine]));
		}
		int num2 = caretPosInLine;
		while (num2 > 0 && GetCharRank(text[num2 - 1]) == num)
		{
			num2--;
		}
		int i;
		for (i = caretPosInLine; i < text.Length && GetCharRank(text[i]) == num; i++)
		{
		}
		if (num2 != caretPosInLine || i != caretPosInLine)
		{
			selectedTextStart = CaretPosWithoutLineBreaks - (caretPosInLine - num2);
			SetCaretPos(i, CaretPosLine);
		}
	}

	private TextSelection GetSelection(int caretPos)
	{
		return new TextSelection(Math.Min(selectedTextStart.Value, caretPos), Math.Max(selectedTextStart.Value, caretPos));
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		if (args.Button != EnumMouseButton.Left)
		{
			return;
		}
		bool num = ((Func<bool>)(() => api.Input.KeyboardKeyStateRaw[1] || api.Input.KeyboardKeyStateRaw[2]))();
		if (num && !selectedTextStart.HasValue)
		{
			selectedTextStart = CaretPosWithoutLineBreaks;
		}
		SetCaretPos((double)args.X - Bounds.absX, (double)args.Y - Bounds.absY);
		if (!num)
		{
			long elapsedMilliseconds = api.ElapsedMilliseconds;
			int caretPosWithoutLineBreaks = CaretPosWithoutLineBreaks;
			bool num2 = lastClickCursor == caretPosWithoutLineBreaks && elapsedMilliseconds - lastClickTime < 400;
			lastClickTime = elapsedMilliseconds;
			lastClickCursor = caretPosWithoutLineBreaks;
			if (num2)
			{
				SelectWordAtCursor();
				return;
			}
			selectedTextStart = caretPosWithoutLineBreaks;
		}
		mouseDown = true;
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseMove(api, args);
		if (mouseDown)
		{
			SetCaretPos((double)args.X - Bounds.absX, (double)args.Y - Bounds.absY);
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseUp(api, args);
		if (args.Button == EnumMouseButton.Left)
		{
			mouseDown = false;
			if (selectedTextStart == CaretPosWithoutLineBreaks)
			{
				selectedTextStart = null;
			}
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (base.HasFocus)
		{
			handlingOnKeyEvent = true;
			OnKeyDownInternal(api, args);
			handlingOnKeyEvent = false;
		}
	}

	private void OnKeyDownInternal(ICoreClientAPI api, KeyEvent args)
	{
		if (args.AltPressed)
		{
			args.Handled = true;
			return;
		}
		if ((args.CtrlPressed || args.CommandPressed) && OnControlAction(args))
		{
			api.Gui.PlaySound("tick");
			args.Handled = true;
			return;
		}
		bool handled = multilineMode || args.KeyCode != 52;
		int keyCode = args.KeyCode;
		if ((keyCode == 53 || keyCode == 55) ? true : false)
		{
			if (!args.CtrlPressed || !OnDeleteWord((args.KeyCode != 53) ? 1 : (-1)))
			{
				if (!selectedTextStart.HasValue)
				{
					if (args.KeyCode == 53)
					{
						OnKeyBackSpace();
					}
					else
					{
						OnKeyDelete();
					}
				}
				else
				{
					DeleteSelectedText(CaretPosWithoutLineBreaks, 0);
					api.Gui.PlaySound("tick");
				}
			}
			args.Handled = true;
			return;
		}
		bool flag = args.ShiftPressed != selectedTextStart.HasValue;
		if (flag)
		{
			keyCode = args.KeyCode;
			bool flag2 = (((uint)(keyCode - 45) <= 3u || (uint)(keyCode - 58) <= 1u) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			if (!args.CtrlPressed && selectedTextStart.HasValue)
			{
				int caretPosWithoutLineBreaks = CaretPosWithoutLineBreaks;
				flag = selectedTextStart < caretPosWithoutLineBreaks;
				if (flag)
				{
					keyCode = args.KeyCode;
					bool flag3 = ((keyCode == 45 || keyCode == 47 || keyCode == 58) ? true : false);
					flag = flag3;
				}
				bool flag2 = flag;
				if (!flag2)
				{
					bool flag3 = selectedTextStart > caretPosWithoutLineBreaks;
					if (flag3)
					{
						keyCode = args.KeyCode;
						bool flag4 = ((keyCode == 46 || keyCode == 48 || keyCode == 59) ? true : false);
						flag3 = flag4;
					}
					flag2 = flag3;
				}
				if (flag2)
				{
					CaretPosWithoutLineBreaks = selectedTextStart.Value;
				}
				keyCode = args.KeyCode;
				if ((uint)(keyCode - 47) <= 1u)
				{
					selectedTextStart = null;
					args.Handled = true;
					api.Gui.PlaySound("tick");
					return;
				}
			}
			selectedTextStart = (args.ShiftPressed ? new int?(CaretPosWithoutLineBreaks) : ((int?)null));
		}
		if (args.KeyCode == 59)
		{
			if (args.CtrlPressed)
			{
				SetCaretPos(lines[lines.Count - 1].TrimEnd('\r', '\n').Length, lines.Count - 1);
			}
			else
			{
				SetCaretPos(lines[CaretPosLine].TrimEnd('\r', '\n').Length, CaretPosLine);
			}
			api.Gui.PlaySound("tick");
		}
		if (args.KeyCode == 58)
		{
			if (args.CtrlPressed)
			{
				SetCaretPos(0);
			}
			else
			{
				SetCaretPos(0, CaretPosLine);
			}
			api.Gui.PlaySound("tick");
		}
		if (args.KeyCode == 47)
		{
			MoveCursor(-1, args.CtrlPressed);
		}
		if (args.KeyCode == 48)
		{
			MoveCursor(1, args.CtrlPressed);
		}
		if (args.KeyCode == 46 && CaretPosLine < lines.Count - 1)
		{
			SetCaretPos(CaretPosInLine, CaretPosLine + 1);
			api.Gui.PlaySound("tick");
		}
		if (args.KeyCode == 45 && CaretPosLine > 0)
		{
			SetCaretPos(CaretPosInLine, CaretPosLine - 1);
			api.Gui.PlaySound("tick");
		}
		if (!mouseDown && selectedTextStart == CaretPosWithoutLineBreaks)
		{
			selectedTextStart = null;
		}
		keyCode = args.KeyCode;
		if ((keyCode == 49 || keyCode == 82) ? true : false)
		{
			if (multilineMode)
			{
				OnKeyEnter();
			}
			else
			{
				handled = false;
			}
		}
		if (args.KeyCode == 50)
		{
			handled = false;
		}
		args.Handled = handled;
	}

	public override string GetText()
	{
		return string.Join("", lines);
	}

	private bool OnControlAction(KeyEvent args)
	{
		switch (GlKeyNames.GetPrintableChar(args.KeyCode))
		{
		case "a":
		{
			selectedTextStart = 0;
			List<string> list = lines;
			SetCaretPos(list[list.Count - 1].Length, lines.Count - 1);
			return true;
		}
		case "c":
			return OnCopyCut(CopyCutMode.Copy);
		case "x":
			return OnCopyCut(CopyCutMode.Cut);
		case "v":
			return OnPaste();
		default:
			return false;
		}
	}

	private bool OnCopyCut(CopyCutMode mode)
	{
		if (!selectedTextStart.HasValue)
		{
			return false;
		}
		string obj = GetText();
		int caretPosWithoutLineBreaks = CaretPosWithoutLineBreaks;
		TextSelection selection = GetSelection(caretPosWithoutLineBreaks);
		int start = selection.Start;
		string text = obj.Substring(start, selection.End - start);
		if (text.Length != 0)
		{
			api.Forms.SetClipboardText(text);
		}
		if (mode == CopyCutMode.Cut)
		{
			DeleteSelectedText(caretPosWithoutLineBreaks, 0);
		}
		return true;
	}

	private bool OnPaste()
	{
		if (selectedTextStart.HasValue)
		{
			DeleteSelectedText(CaretPosWithoutLineBreaks, 0);
		}
		string clipboardText = api.Forms.GetClipboardText();
		clipboardText = clipboardText.Replace("\ufeff", "");
		string text = string.Join("", lines);
		int caretPosWithoutLineBreaks = CaretPosWithoutLineBreaks;
		string obj = text.Substring(0, caretPosWithoutLineBreaks);
		string obj2 = clipboardText;
		string text2 = text;
		int num = caretPosWithoutLineBreaks;
		SetValue(obj + obj2 + text2.Substring(num, text2.Length - num), setCaretPosToEnd: false);
		CaretPosWithoutLineBreaks = caretPosWithoutLineBreaks + clipboardText.Length;
		return true;
	}

	private bool OnDeleteWord(int direction)
	{
		if (selectedTextStart.HasValue)
		{
			return false;
		}
		selectedTextStart = CaretPosWithoutLineBreaks;
		MoveCursor(direction, wholeWord: true, wholeWordWithWhitespace: true);
		DeleteSelectedText(CaretPosWithoutLineBreaks, 0);
		return true;
	}

	private void OnKeyEnter()
	{
		if (selectedTextStart.HasValue)
		{
			DeleteSelectedText(CaretPosWithoutLineBreaks, 0);
		}
		if (lines.Count < maxlines)
		{
			string text = linesStaging[CaretPosLine].Substring(0, CaretPosInLine);
			string item = linesStaging[CaretPosLine].Substring(CaretPosInLine);
			linesStaging[CaretPosLine] = text + "\n";
			linesStaging.Insert(CaretPosLine + 1, item);
			OnTryTextChangeDelegate onTryTextChangeText = OnTryTextChangeText;
			if (onTryTextChangeText == null || onTryTextChangeText(linesStaging))
			{
				lines = new List<string>(linesStaging);
				TextChanged();
				SetCaretPos(0, CaretPosLine + 1);
				api.Gui.PlaySound("tick");
			}
		}
	}

	private void OnKeyDelete()
	{
		string text = GetText();
		int caretPosWithoutLineBreaks = CaretPosWithoutLineBreaks;
		if (text.Length != caretPosWithoutLineBreaks)
		{
			text = text.Substring(0, caretPosWithoutLineBreaks) + text.Substring(caretPosWithoutLineBreaks + 1, text.Length - caretPosWithoutLineBreaks - 1);
			LoadValue(Lineize(text));
			api.Gui.PlaySound("tick");
		}
	}

	private void OnKeyBackSpace()
	{
		int caretPosWithoutLineBreaks = CaretPosWithoutLineBreaks;
		if (caretPosWithoutLineBreaks != 0)
		{
			string text = GetText();
			text = text.Substring(0, caretPosWithoutLineBreaks - 1) + text.Substring(caretPosWithoutLineBreaks, text.Length - caretPosWithoutLineBreaks);
			int caretPosWithoutLineBreaks2 = CaretPosWithoutLineBreaks;
			LoadValue(Lineize(text));
			if (caretPosWithoutLineBreaks2 > 0)
			{
				CaretPosWithoutLineBreaks = caretPosWithoutLineBreaks2 - 1;
			}
			api.Gui.PlaySound("tick");
		}
	}

	public override void OnKeyPress(ICoreClientAPI api, KeyEvent args)
	{
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		if (!base.HasFocus)
		{
			return;
		}
		string text = lines[CaretPosLine].Substring(0, CaretPosInLine) + args.KeyChar + lines[CaretPosLine].Substring(CaretPosInLine, lines[CaretPosLine].Length - CaretPosInLine);
		double num = Bounds.InnerWidth - 2.0 * Bounds.absPaddingX - rightSpacing;
		linesStaging[CaretPosLine] = text;
		if (multilineMode)
		{
			TextExtents textExtents = Font.GetTextExtents(text.TrimEnd('\r', '\n'));
			if (((TextExtents)(ref textExtents)).Width >= num)
			{
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < lines.Count; i++)
				{
					stringBuilder.Append((i == CaretPosLine) ? text : lines[i]);
				}
				linesStaging = Lineize(stringBuilder.ToString());
				if (lines.Count >= maxlines && linesStaging.Count >= maxlines)
				{
					return;
				}
			}
		}
		int caretPosWithoutLineBreaks = CaretPosWithoutLineBreaks;
		handlingOnKeyEvent = true;
		LoadValue(linesStaging);
		CaretPosWithoutLineBreaks = caretPosWithoutLineBreaks + 1;
		if (selectedTextStart.HasValue)
		{
			DeleteSelectedTextAndFixCursor(caretPosWithoutLineBreaks, 1);
		}
		handlingOnKeyEvent = false;
		args.Handled = true;
		api.Gui.PlaySound("tick");
		OnKeyPressed?.Invoke();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (base.HasFocus)
		{
			if (api.ElapsedMilliseconds - caretBlinkMilliseconds > 900)
			{
				caretBlinkMilliseconds = api.ElapsedMilliseconds;
				caretDisplayed = !caretDisplayed;
			}
			if (caretDisplayed && caretX - renderLeftOffset < Bounds.InnerWidth)
			{
				api.Render.Render2DTexturePremultipliedAlpha(caretTexture.TextureId, Bounds.renderX + caretX + GuiElement.scaled(1.5) - renderLeftOffset, Bounds.renderY + caretY + topPadding, 2.0, caretHeight);
			}
		}
	}

	protected void RenderTextSelection()
	{
		if (!selectedTextStart.HasValue)
		{
			return;
		}
		TextSelection selection = GetSelection(CaretPosWithoutLineBreaks);
		SelectedTextPos position = GetPosition(selection.Start);
		SelectedTextPos position2 = GetPosition(selection.End);
		if (position.Y == position2.Y)
		{
			RenderSelectionLine(position.X, position2.X, position.Y);
			return;
		}
		RenderSelectionLine(position.X, -1, position.Y);
		for (int i = position.Y + 1; i < position2.Y; i++)
		{
			RenderSelectionLine(0, -1, i);
		}
		RenderSelectionLine(0, position2.X, position2.Y);
	}

	private void RenderSelectionLine(int fromX, int toX, int lineIndex)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		double num = Bounds.renderX + leftPadding;
		double num2 = Bounds.renderY + topPadding;
		FontExtents fontExtents = Font.GetFontExtents();
		double height = ((FontExtents)(ref fontExtents)).Height;
		TextExtents textExtents;
		double num3;
		if (fromX != 0)
		{
			textExtents = Font.GetTextExtents(lines[lineIndex].Substring(0, fromX));
			num3 = ((TextExtents)(ref textExtents)).XAdvance;
		}
		else
		{
			num3 = 0.0;
		}
		double num4 = num + num3;
		double posY = num2 + height * (double)lineIndex;
		textExtents = Font.GetTextExtents(lines[lineIndex].Substring(fromX, ((toX == -1) ? lines[lineIndex].Length : toX) - fromX));
		double xAdvance = ((TextExtents)(ref textExtents)).XAdvance;
		api.Render.Render2DTexturePremultipliedAlpha(selectionTextureId, num4 - renderLeftOffset, posY, xAdvance, height);
	}

	private SelectedTextPos GetPosition(int positionWithoutLineBreaks)
	{
		int num = 0;
		foreach (string line in lines)
		{
			if (positionWithoutLineBreaks > line.Length)
			{
				num++;
				positionWithoutLineBreaks -= line.Length;
				continue;
			}
			break;
		}
		return new SelectedTextPos(positionWithoutLineBreaks, num);
	}

	public override void Dispose()
	{
		base.Dispose();
		caretTexture.Dispose();
		textTexture.Dispose();
		if (selectionTextureId != 0)
		{
			api.Gui.DeleteTexture(selectionTextureId);
			selectionTextureId = 0;
		}
	}

	public void MoveCursor(int dir, bool wholeWord = false, bool wholeWordWithWhitespace = false)
	{
		bool flag = ((CaretPosInLine > 0 || CaretPosLine > 0) && dir < 0) || ((CaretPosInLine < lines[CaretPosLine].Length || CaretPosLine < lines.Count - 1) && dir > 0);
		if (wholeWord)
		{
			dir = ((dir >= 0) ? 1 : (-1));
			string text = GetText();
			int num = ((dir < 0) ? (-1) : text.Length);
			int num2 = ((dir < 0) ? (-1) : 0);
			int i = CaretPosWithoutLineBreaks + num2;
			bool flag2 = i != num && char.IsWhiteSpace(text[i]);
			for (; i != num && char.IsWhiteSpace(text[i]); i += dir)
			{
			}
			if (i != num && !IsWordChar(text[i]))
			{
				for (; i != num && !IsWordChar(text[i]) && !char.IsWhiteSpace(text[i]); i += dir)
				{
				}
			}
			else
			{
				for (; i != num && IsWordChar(text[i]); i += dir)
				{
				}
			}
			if (!flag2 && wholeWordWithWhitespace)
			{
				for (; i != num && char.IsWhiteSpace(text[i]); i += dir)
				{
				}
			}
			CaretPosWithoutLineBreaks = i - num2;
		}
		else
		{
			CaretPosWithoutLineBreaks += dir;
		}
		if (flag)
		{
			api.Gui.PlaySound("tick");
		}
	}

	public void SetMaxLines(int maxlines)
	{
		this.maxlines = maxlines;
	}

	public void SetMaxHeight(int maxheight)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		FontExtents fontExtents = Font.GetFontExtents();
		maxlines = (int)Math.Floor((double)maxheight / ((FontExtents)(ref fontExtents)).Height);
	}

	private static int GenerateSelectionTexture(ICoreClientAPI api)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, 32, 32);
		try
		{
			Context val2 = new Context((Surface)(object)val);
			try
			{
				val2.SetSourceRGBA(0.0, 0.75, 1.0, 0.5);
				val2.Paint();
				return api.Gui.LoadCairoTexture(val, linearMag: true);
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static bool IsWordChar(char c)
	{
		if (c != '_')
		{
			return char.IsLetterOrDigit(c);
		}
		return true;
	}
}
