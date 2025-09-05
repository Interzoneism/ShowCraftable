using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogTextInput : GuiDialogGeneric
{
	private double textareaFixedY;

	public Action<string> OnTextChanged;

	public Action OnCloseCancel;

	public float FontSize;

	protected bool didSave;

	protected TextAreaConfig signConfig;

	public Action<string> OnSave;

	public GuiDialogTextInput(string DialogTitle, string text, ICoreClientAPI capi, TextAreaConfig signConfig)
		: base(DialogTitle, capi)
	{
		if (signConfig == null)
		{
			signConfig = new TextAreaConfig();
		}
		this.signConfig = signConfig;
		FontSize = signConfig.FontSize;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, signConfig.MaxWidth + 4, signConfig.MaxHeight - 2);
		textareaFixedY = elementBounds.fixedY;
		ElementBounds elementBounds2 = elementBounds.ForkBoundingParent().WithFixedPosition(0.0, 30.0);
		ElementBounds bounds = elementBounds2.CopyOffsetedSibling(elementBounds.fixedWidth + 3.0).WithFixedWidth(20.0);
		ElementBounds bounds2 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds2, 10.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(8.0, 2.0)
			.WithFixedAlignmentOffset(-1.0, 0.0);
		ElementBounds bounds3 = ElementBounds.FixedSize(45.0, 22.0).FixedUnder(elementBounds2, 10.0).WithAlignment(EnumDialogArea.CenterFixed)
			.WithFixedAlignmentOffset(3.0, 0.0);
		ElementBounds bounds4 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds2, 10.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(8.0, 2.0);
		ElementBounds bounds5 = ElementBounds.FixedSize(signConfig.MaxWidth + 32, 220.0).WithFixedPadding(GuiStyle.ElementToDialogPadding);
		ElementBounds bounds6 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		CairoFont cairoFont = CairoFont.TextInput().WithFontSize(signConfig.FontSize).WithFont(signConfig.FontName);
		if (signConfig.BoldFont)
		{
			cairoFont.WithWeight((FontWeight)1);
		}
		cairoFont.LineHeightMultiplier = 0.9;
		string[] array = new string[8] { "14", "18", "20", "24", "28", "32", "36", "40" };
		if (!array.Contains<string>(signConfig.FontSize.ToString() ?? ""))
		{
			array = array.Append<string>(signConfig.FontSize.ToString() ?? "");
		}
		base.SingleComposer = capi.Gui.CreateCompo("blockentitytexteditordialog", bounds6).AddShadedDialogBG(bounds5).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
			.BeginChildElements(bounds5)
			.BeginClip(elementBounds2)
			.AddTextArea(elementBounds, OnTextAreaChanged, cairoFont, "text")
			.EndClip()
			.AddIf(signConfig.WithScrollbar)
			.AddVerticalScrollbar(OnNewScrollbarvalue, bounds, "scrollbar")
			.EndIf()
			.AddSmallButton(Lang.Get("Cancel"), OnButtonCancel, bounds2)
			.AddDropDown(array, array, array.IndexOf<string>(signConfig.FontSize.ToString() ?? ""), onfontsizechanged, bounds3)
			.AddSmallButton(Lang.Get("Save"), OnButtonSave, bounds4)
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetTextArea("text").SetMaxHeight((int)((float)signConfig.MaxHeight * RuntimeEnv.GUIScale));
		if (signConfig.WithScrollbar)
		{
			base.SingleComposer.GetScrollbar("scrollbar").SetHeights((float)elementBounds.fixedHeight, (float)elementBounds.fixedHeight);
		}
		if (text != null && text.Length > 0)
		{
			base.SingleComposer.GetTextArea("text").SetValue(text);
		}
	}

	private void onfontsizechanged(string code, bool selected)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("text");
		string text = textArea.GetText();
		textArea.SetFont(textArea.Font.Clone().WithFontSize(FontSize = code.ToInt()));
		textArea.Font.WithFontSize(FontSize = code.ToInt());
		textArea.SetMaxHeight(signConfig.MaxHeight);
		textArea.SetValue(text);
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		base.SingleComposer.FocusElement(base.SingleComposer.GetTextArea("text").TabIndex);
	}

	private void OnTextAreaChanged(string value)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("text");
		if (signConfig.WithScrollbar)
		{
			base.SingleComposer.GetScrollbar("scrollbar").SetNewTotalHeight((float)textArea.Bounds.fixedHeight);
		}
		OnTextChanged?.Invoke(textArea.GetText());
	}

	private void OnNewScrollbarvalue(float value)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("text");
		textArea.Bounds.fixedY = 3.0 + textareaFixedY - (double)value;
		textArea.Bounds.CalcWorldBounds();
	}

	private void OnTitleBarClose()
	{
		OnButtonCancel();
	}

	private bool OnButtonSave()
	{
		string text = base.SingleComposer.GetTextArea("text").GetText();
		OnSave(text);
		didSave = true;
		TryClose();
		return true;
	}

	private bool OnButtonCancel()
	{
		TryClose();
		return true;
	}

	public override void OnGuiClosed()
	{
		if (!didSave)
		{
			OnCloseCancel?.Invoke();
		}
		base.OnGuiClosed();
	}
}
