using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class GuiDialogJournal : GuiDialogGeneric
{
	private List<JournalEntry> journalitems = new List<JournalEntry>();

	private string[] pages;

	private int currentLoreItemIndex;

	private int page;

	private ElementBounds containerBounds;

	public override string ToggleKeyCombinationCode => null;

	public override bool PrefersUngrabbedMouse => false;

	public GuiDialogJournal(List<JournalEntry> journalitems, ICoreClientAPI capi)
		: base(Lang.Get("Journal"), capi)
	{
		this.journalitems = journalitems;
	}

	private void ComposeDialog()
	{
		_ = GuiStyle.ElementToDialogPadding;
		ElementBounds elementBounds = ElementBounds.Fixed(3.0, 3.0, 283.0, 25.0).WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 32.0, 285.0, 500.0);
		ElementBounds elementBounds3 = elementBounds2.ForkBoundingParent();
		ElementBounds elementBounds4 = elementBounds2.FlatCopy().FixedGrow(6.0).WithFixedOffset(-3.0, -3.0);
		ElementBounds elementBounds5 = elementBounds4.CopyOffsetedSibling(elementBounds2.fixedWidth + 7.0).WithFixedWidth(20.0);
		ElementBounds elementBounds6 = ElementBounds.Fill.WithFixedPadding(6.0);
		elementBounds6.BothSizing = ElementSizing.FitToChildren;
		elementBounds6.WithChildren(elementBounds4, elementBounds3, elementBounds5);
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(5.0, 0.0);
		ClearComposers();
		Composers["loreList"] = capi.Gui.CreateCompo("loreList", bounds).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(Lang.Get("Journal Inventory"), CloseIconPressed)
			.BeginChildElements(elementBounds6)
			.AddInset(elementBounds4, 3)
			.BeginClip(elementBounds3)
			.AddContainer(containerBounds = elementBounds3.ForkContainingChild(0.0, 0.0, 0.0, -3.0), "journallist");
		GuiElementContainer container = Composers["loreList"].GetContainer("journallist");
		CairoFont hoverFont = CairoFont.WhiteSmallText().Clone().WithColor(GuiStyle.ActiveButtonTextColor);
		for (int i = 0; i < journalitems.Count; i++)
		{
			int page = i;
			GuiElementTextButton guiElementTextButton = new GuiElementTextButton(capi, Lang.Get(journalitems[i].Title), CairoFont.WhiteSmallText(), hoverFont, () => onClickItem(page), elementBounds, EnumButtonStyle.Small);
			guiElementTextButton.SetOrientation(EnumTextOrientation.Left);
			container.Add(guiElementTextButton);
			elementBounds = elementBounds.BelowCopy();
		}
		if (journalitems.Count == 0)
		{
			string vtmlCode = "<i>" + Lang.Get("No lore found. Collect lore in the world to fill this list!.") + "</i>";
			container.Add(new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, vtmlCode, CairoFont.WhiteSmallText()), elementBounds));
		}
		Composers["loreList"].EndClip().AddVerticalScrollbar(OnNewScrollbarvalue, elementBounds5, "scrollbar").EndChildElements()
			.Compose();
		containerBounds.CalcWorldBounds();
		elementBounds3.CalcWorldBounds();
		Composers["loreList"].GetScrollbar("scrollbar").SetHeights((float)elementBounds3.fixedHeight, (float)containerBounds.fixedHeight);
	}

	private bool onClickItem(int page)
	{
		currentLoreItemIndex = page;
		this.page = 0;
		CairoFont cairoFont = CairoFont.WhiteDetailText().WithFontSize(17f).WithLineHeightMultiplier(1.149999976158142);
		new TextDrawUtil();
		StringBuilder stringBuilder = new StringBuilder();
		JournalEntry journalEntry = journalitems[currentLoreItemIndex];
		for (int i = 0; i < journalEntry.Chapters.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.AppendLine();
			}
			stringBuilder.Append(Lang.Get(journalEntry.Chapters[i].Text));
		}
		pages = Paginate(stringBuilder.ToString(), cairoFont, GuiElement.scaled(629.0), GuiElement.scaled(450.0));
		double elementToDialogPadding = GuiStyle.ElementToDialogPadding;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 630.0, 450.0);
		ElementBounds elementBounds2 = elementBounds.ForkBoundingParent(elementToDialogPadding, elementToDialogPadding + 20.0, elementToDialogPadding, elementToDialogPadding + 30.0).WithAlignment(EnumDialogArea.LeftMiddle);
		elementBounds2.fixedX = 350.0;
		Composers["loreItem"] = capi.Gui.CreateCompo("loreItem", elementBounds2).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(Lang.Get(journalitems[page].Title), CloseIconPressedLoreItem)
			.AddRichtext(pages[0], cairoFont, elementBounds, "page")
			.AddDynamicText("1 / " + pages.Length, CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center), ElementBounds.Fixed(250.0, 500.0, 100.0, 30.0), "currentpage")
			.AddButton(Lang.Get("Previous Page"), OnPrevPage, ElementBounds.Fixed(17.0, 500.0, 100.0, 23.0).WithFixedPadding(10.0, 4.0), CairoFont.WhiteSmallishText())
			.AddButton(Lang.Get("Next Page"), OnNextPage, ElementBounds.Fixed(520.0, 500.0, 100.0, 23.0).WithFixedPadding(10.0, 4.0), CairoFont.WhiteSmallishText())
			.Compose();
		return true;
	}

	private string[] Paginate(string fullText, CairoFont font, double pageWidth, double pageHeight)
	{
		TextDrawUtil textDrawUtil = new TextDrawUtil();
		Stack<string> stack = new Stack<string>();
		foreach (TextLine item in textDrawUtil.Lineize(font, fullText, pageWidth).Reverse())
		{
			stack.Push(item.Text);
		}
		double lineHeight = textDrawUtil.GetLineHeight(font);
		int num = (int)(pageHeight / lineHeight);
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		while (stack.Count > 0)
		{
			int num2 = 0;
			while (num2 < num && stack.Count > 0)
			{
				string text = stack.Pop();
				string[] array = text.Split(new string[1] { "___NEWPAGE___" }, 2, StringSplitOptions.None);
				if (array.Length > 1)
				{
					stringBuilder.AppendLine(array[0]);
					if (array[1].Length > 0)
					{
						stack.Push(array[1]);
					}
					break;
				}
				num2++;
				stringBuilder.AppendLine(text);
			}
			string text2 = stringBuilder.ToString().TrimEnd();
			if (text2.Length > 0)
			{
				list.Add(text2);
			}
			stringBuilder.Clear();
		}
		return list.ToArray();
	}

	private bool OnNextPage()
	{
		CairoFont baseFont = CairoFont.WhiteDetailText().WithFontSize(17f).WithLineHeightMultiplier(1.149999976158142);
		page = Math.Min(pages.Length - 1, page + 1);
		Composers["loreItem"].GetRichtext("page").SetNewText(pages[page], baseFont);
		Composers["loreItem"].GetDynamicText("currentpage").SetNewText(page + 1 + " / " + pages.Length);
		return true;
	}

	private bool OnPrevPage()
	{
		CairoFont baseFont = CairoFont.WhiteDetailText().WithFontSize(17f).WithLineHeightMultiplier(1.149999976158142);
		page = Math.Max(0, page - 1);
		Composers["loreItem"].GetRichtext("page").SetNewText(pages[page], baseFont);
		Composers["loreItem"].GetDynamicText("currentpage").SetNewText(page + 1 + " / " + pages.Length);
		return true;
	}

	public override void OnGuiOpened()
	{
		ComposeDialog();
	}

	private void CloseIconPressed()
	{
		TryClose();
	}

	private void CloseIconPressedLoreItem()
	{
		Composers.Remove("loreItem");
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = Composers["loreList"].GetContainer("journallist").Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
	}

	public void ReloadValues()
	{
	}
}
