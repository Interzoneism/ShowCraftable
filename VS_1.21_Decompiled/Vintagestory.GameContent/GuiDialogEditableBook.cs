using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class GuiDialogEditableBook : GuiDialogReadonlyBook
{
	public bool DidSave;

	public bool DidSign;

	private int maxPageCount;

	private bool ignoreTextChange;

	public GuiDialogEditableBook(ItemStack bookStack, ICoreClientAPI capi, int maxPageCount)
		: base(bookStack, capi)
	{
		this.maxPageCount = maxPageCount;
		KeyboardNavigation = false;
	}

	protected override void Compose()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		FontExtents fontExtents = font.GetFontExtents();
		double num = ((FontExtents)(ref fontExtents)).Height * font.LineHeightMultiplier / (double)RuntimeEnv.GUIScale;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 30.0, maxWidth, 24.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 0.0, 400.0, (double)maxLines * num + 1.0).FixedUnder(elementBounds, 5.0);
		ElementBounds elementBounds3 = ElementBounds.FixedSize(60.0, 30.0).FixedUnder(elementBounds2, 5.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds bounds = ElementBounds.FixedSize(80.0, 30.0).FixedUnder(elementBounds2, 17.0).WithAlignment(EnumDialogArea.CenterFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds4 = ElementBounds.FixedSize(60.0, 30.0).FixedUnder(elementBounds2, 5.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds5 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds3, 25.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds bounds2 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds3, 25.0).WithAlignment(EnumDialogArea.CenterFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds6 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds4, 25.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds7 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds7.BothSizing = ElementSizing.FitToChildren;
		elementBounds7.WithChildren(elementBounds5, elementBounds6);
		ElementBounds bounds3 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		base.SingleComposer = capi.Gui.CreateCompo("blockentitytexteditordialog", bounds3).AddShadedDialogBG(elementBounds7).AddDialogTitleBar(Lang.Get("Edit book"), OnTitleBarClose)
			.BeginChildElements(elementBounds7)
			.AddTextInput(elementBounds, null, CairoFont.TextInput().WithFontSize(18f), "title")
			.AddTextArea(elementBounds2, onTextChanged, font, "text")
			.AddSmallButton(Lang.Get("<"), OnPreviousPage, elementBounds3)
			.AddDynamicText("1/1", CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), bounds, "pageNum")
			.AddSmallButton(Lang.Get(">"), OnNextPage, elementBounds4)
			.AddSmallButton(Lang.Get("Cancel"), OnButtonCancel, elementBounds5)
			.AddSmallButton(Lang.Get("editablebook-sign"), OnButtonSign, bounds2)
			.AddSmallButton(Lang.Get("editablebook-save"), OnButtonSave, elementBounds6)
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetTextInput("title").SetPlaceHolderText(Lang.Get("Book title"));
		base.SingleComposer.GetTextInput("title").SetValue(Title);
		base.SingleComposer.GetTextArea("text").OnCaretPositionChanged = onCaretPositionChanged;
		base.SingleComposer.GetTextArea("text").Autoheight = false;
		updatePage(setCaretPosToEnd: false);
		base.SingleComposer.GetTextArea("text").OnTryTextChangeText = onTryTextChange;
	}

	private bool onTryTextChange(List<string> lines)
	{
		int num = 0;
		bool flag = lines.Count > Pages[curPage].LineCount;
		for (int i = 0; i < Pages.Count; i++)
		{
			num = ((i == curPage) ? lines.Count : Pages[i].LineCount);
		}
		if (num > maxPageCount * maxLines && flag)
		{
			return false;
		}
		return true;
	}

	private bool OnButtonSign()
	{
		new GuiDialogConfirm(capi, Lang.Get("Save and sign book now? It can not be edited afterwards."), onConfirmSign).TryOpen();
		return true;
	}

	private void onConfirmSign(bool ok)
	{
		if (ok)
		{
			StoreCurrentPage();
			Title = base.SingleComposer.GetTextInput("title").GetText();
			DidSign = true;
			DidSave = true;
			TryClose();
		}
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		base.SingleComposer.FocusElement(base.SingleComposer.GetTextArea("text").TabIndex);
	}

	private void OnTitleBarClose()
	{
		OnButtonCancel();
	}

	private void onTextChanged(string text)
	{
		if (!ignoreTextChange)
		{
			ignoreTextChange = true;
			GuiElementTextArea textArea = base.SingleComposer.GetTextArea("text");
			int caretPosLine = textArea.CaretPosLine;
			int caretPosInLine = textArea.CaretPosInLine;
			StoreCurrentPage();
			updatePage(setCaretPosToEnd: false);
			textArea.SetCaretPos(caretPosInLine, caretPosLine);
			ignoreTextChange = false;
		}
	}

	private void onCaretPositionChanged(int posLine, int posInLine)
	{
		if (!ignoreTextChange)
		{
			ignoreTextChange = true;
			if (posLine >= maxLines && curPage + 1 < maxPageCount && Pages.Count - 1 > curPage + 1)
			{
				GuiElementTextArea textArea = base.SingleComposer.GetTextArea("text");
				StoreCurrentPage();
				nextPage();
				textArea.SetCaretPos(posInLine, posLine - maxLines);
			}
			ignoreTextChange = false;
		}
	}

	private bool OnButtonSave()
	{
		StoreCurrentPage();
		Title = base.SingleComposer.GetTextInput("title").GetText();
		DidSave = true;
		TryClose();
		return true;
	}

	private bool OnButtonCancel()
	{
		DidSave = false;
		TryClose();
		return true;
	}

	protected bool OnNextPage()
	{
		if (curPage >= maxPageCount)
		{
			return true;
		}
		if (curPage + 1 >= Pages.Count)
		{
			return false;
		}
		if (Pages.Count <= curPage + 1)
		{
			PagePosition pagePosition = Pages[0];
			Pages.Add(new PagePosition
			{
				Start = pagePosition.Length + 1,
				Length = 1
			});
			AllPagesText += "___NEWPAGE___";
		}
		ignoreTextChange = true;
		StoreCurrentPage();
		curPage = Math.Min(curPage + 1, Pages.Count);
		updatePage();
		ignoreTextChange = false;
		return true;
	}

	private bool StoreCurrentPage()
	{
		PagePosition pagePosition = Pages[curPage];
		string text = base.SingleComposer.GetTextArea("text").GetText();
		AllPagesText = AllPagesText.Substring(0, pagePosition.Start) + text + AllPagesText.Substring(Math.Min(AllPagesText.Length, pagePosition.Start + pagePosition.Length)).Replace("\r", "");
		Pages = Pageize(AllPagesText, font, base.textAreaWidth, maxLines);
		if (curPage >= Pages.Count)
		{
			curPage = Pages.Count - 1;
		}
		return true;
	}

	protected bool OnPreviousPage()
	{
		ignoreTextChange = true;
		StoreCurrentPage();
		curPage = Math.Max(0, curPage - 1);
		updatePage();
		ignoreTextChange = false;
		return true;
	}

	public override void OnKeyDown(KeyEvent args)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("text");
		if (args.KeyCode == 53 && textArea.CaretPosInLine == 0 && textArea.CaretPosLine == 0 && curPage > 0)
		{
			StoreCurrentPage();
			curPage--;
			ignoreTextChange = true;
			updatePage();
			ignoreTextChange = false;
		}
		if (args.KeyCode == 47 && textArea.CaretPosInLine == 0 && textArea.CaretPosLine == 0 && curPage > 0)
		{
			StoreCurrentPage();
			curPage--;
			ignoreTextChange = true;
			updatePage();
			ignoreTextChange = false;
		}
		else if (args.KeyCode == 48 && curPage < Pages.Count - 1 && textArea.CaretPosWithoutLineBreaks == textArea.GetText().Length)
		{
			StoreCurrentPage();
			curPage++;
			ignoreTextChange = true;
			updatePage(setCaretPosToEnd: false);
			textArea.SetCaretPos(0);
			ignoreTextChange = false;
		}
		else if (args.KeyCode == 46 && textArea.CaretPosLine + 1 >= maxLines && curPage < Pages.Count - 1)
		{
			int caretPosInLine = textArea.CaretPosInLine;
			StoreCurrentPage();
			curPage++;
			ignoreTextChange = true;
			updatePage(setCaretPosToEnd: false);
			textArea.SetCaretPos(caretPosInLine);
			ignoreTextChange = false;
		}
		else if (args.KeyCode == 45 && curPage > 0 && textArea.CaretPosLine == 0)
		{
			StoreCurrentPage();
			curPage--;
			ignoreTextChange = true;
			updatePage();
			args.Handled = true;
			ignoreTextChange = false;
		}
		else
		{
			base.OnKeyDown(args);
		}
	}
}
