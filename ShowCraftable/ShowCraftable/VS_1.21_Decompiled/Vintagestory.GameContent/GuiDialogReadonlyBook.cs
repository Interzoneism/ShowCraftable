using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class GuiDialogReadonlyBook : GuiDialogGeneric
{
	public string AllPagesText;

	public string Title;

	protected int curPage;

	protected int maxLines = 20;

	protected int maxWidth = 400;

	public List<PagePosition> Pages = new List<PagePosition>();

	protected CairoFont font = CairoFont.TextInput().WithFontSize(18f);

	private TranscribePressedDelegate onTranscribedPressed;

	protected bool KeyboardNavigation = true;

	public double textAreaWidth => GuiElement.scaled(maxWidth);

	public string CurPageText
	{
		get
		{
			if (curPage >= Pages.Count)
			{
				return "";
			}
			if (Pages[curPage].Start < AllPagesText.Length)
			{
				return AllPagesText.Substring(Pages[curPage].Start, Math.Min(AllPagesText.Length - Pages[curPage].Start, Pages[curPage].Length)).TrimStart(' ');
			}
			return "";
		}
	}

	public GuiDialogReadonlyBook(ItemStack bookStack, ICoreClientAPI capi, TranscribePressedDelegate onTranscribedPressed = null)
		: base("", capi)
	{
		this.onTranscribedPressed = onTranscribedPressed;
		if (bookStack.Attributes.HasAttribute("textCodes"))
		{
			AllPagesText = string.Join("\n", (bookStack.Attributes["textCodes"] as StringArrayAttribute).value.Select((string code) => Lang.Get(code))).Replace("\r", "").Replace("___NEWPAGE___", "");
			Title = Lang.Get(bookStack.Attributes.GetString("titleCode", ""));
		}
		else
		{
			AllPagesText = bookStack.Attributes.GetString("text", "").Replace("\r", "");
			Title = bookStack.Attributes.GetString("title", "");
		}
		Pages = Pageize(AllPagesText, font, textAreaWidth, maxLines);
		Compose();
	}

	protected List<PagePosition> Pageize(string fullText, CairoFont font, double pageWidth, int maxLinesPerPage)
	{
		TextDrawUtil textDrawUtil = new TextDrawUtil();
		Stack<string> stack = new Stack<string>();
		foreach (TextLine item in textDrawUtil.Lineize(font, fullText, pageWidth, EnumLinebreakBehavior.Default, keepLinebreakChar: true).Reverse())
		{
			stack.Push(item.Text);
		}
		List<PagePosition> list = new List<PagePosition>();
		int num = 0;
		int num2 = 0;
		while (stack.Count > 0)
		{
			int num3 = 0;
			while (num3 < maxLinesPerPage && stack.Count > 0)
			{
				string text = stack.Pop();
				num3++;
				num2 += text.Length;
			}
			if (num3 > 0)
			{
				list.Add(new PagePosition
				{
					Start = num,
					Length = num2,
					LineCount = num3
				});
				num += num2;
			}
			num2 = 0;
		}
		if (list.Count == 0)
		{
			list.Add(new PagePosition
			{
				Start = 0,
				Length = 0
			});
		}
		return list;
	}

	protected virtual void Compose()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		FontExtents fontExtents = font.GetFontExtents();
		double num = ((FontExtents)(ref fontExtents)).Height * font.LineHeightMultiplier / (double)RuntimeEnv.GUIScale;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 30.0, maxWidth, (double)(maxLines + ((Pages.Count > 1) ? 2 : 0)) * num + 1.0);
		ElementBounds elementBounds2 = ElementBounds.FixedSize(60.0, 30.0).FixedUnder(elementBounds, 23.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds bounds = ElementBounds.FixedSize(80.0, 30.0).FixedUnder(elementBounds, 33.0).WithAlignment(EnumDialogArea.CenterFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds3 = ElementBounds.FixedSize(60.0, 30.0).FixedUnder(elementBounds, 23.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds4 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds2, 25.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds bounds2 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds3, 25.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds5 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds5.BothSizing = ElementSizing.FitToChildren;
		elementBounds5.WithChildren(elementBounds4);
		ElementBounds bounds3 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		base.SingleComposer = capi.Gui.CreateCompo("blockentitytexteditordialog", bounds3).AddShadedDialogBG(elementBounds5).AddDialogTitleBar(Title, delegate
		{
			TryClose();
		})
			.BeginChildElements(elementBounds5)
			.AddRichtext("", font, elementBounds, "text")
			.AddIf(Pages.Count > 1)
			.AddSmallButton(Lang.Get("<"), prevPage, elementBounds2)
			.EndIf()
			.AddDynamicText("1/1", CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), bounds, "pageNum")
			.AddIf(Pages.Count > 1)
			.AddSmallButton(Lang.Get(">"), nextPage, elementBounds3)
			.EndIf()
			.AddSmallButton(Lang.Get("Close"), () => TryClose(), elementBounds4)
			.AddIf(onTranscribedPressed != null)
			.AddSmallButton(Lang.Get("Transcribe"), onButtonTranscribe, bounds2)
			.EndIf()
			.EndChildElements()
			.Compose();
		updatePage();
	}

	private bool onButtonTranscribe()
	{
		onTranscribedPressed(CurPageText, Title, curPage);
		return true;
	}

	protected bool nextPage()
	{
		curPage = Math.Min(curPage + 1, Pages.Count - 1);
		updatePage();
		return true;
	}

	private bool prevPage()
	{
		curPage = Math.Max(curPage - 1, 0);
		updatePage();
		return true;
	}

	protected void updatePage(bool setCaretPosToEnd = true)
	{
		string curPageText = CurPageText;
		base.SingleComposer.GetDynamicText("pageNum").SetNewText(curPage + 1 + "/" + Pages.Count);
		GuiElement element = base.SingleComposer.GetElement("text");
		if (element is GuiElementTextArea guiElementTextArea)
		{
			guiElementTextArea.SetValue(curPageText, setCaretPosToEnd);
		}
		else
		{
			(element as GuiElementRichtext).SetNewText(curPageText, font);
		}
	}

	public override void OnKeyDown(KeyEvent args)
	{
		base.OnKeyDown(args);
		if (KeyboardNavigation)
		{
			if (args.KeyCode == 47 || args.KeyCode == 56)
			{
				prevPage();
			}
			if (args.KeyCode == 48 || args.KeyCode == 57)
			{
				nextPage();
			}
		}
	}
}
