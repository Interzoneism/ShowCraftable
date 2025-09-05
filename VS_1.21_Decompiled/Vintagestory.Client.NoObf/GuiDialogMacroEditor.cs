using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

internal class GuiDialogMacroEditor : GuiDialog
{
	private List<SkillItem> skillItems;

	private int rows = 2;

	private int cols = 8;

	private int selectedIndex = -1;

	private IMacroBase currentMacro;

	private SkillItem currentSkillitem;

	private HotkeyCapturer hotkeyCapturer = new HotkeyCapturer();

	internal IMacroBase SelectedMacro
	{
		get
		{
			(capi.World as ClientMain).macroManager.MacrosByIndex.TryGetValue(selectedIndex, out var value);
			return value;
		}
	}

	public override string ToggleKeyCombinationCode => "macroeditor";

	public override bool PrefersUngrabbedMouse => true;

	public GuiDialogMacroEditor(ICoreClientAPI capi)
		: base(capi)
	{
		skillItems = new List<SkillItem>();
		ComposeDialog();
	}

	private void LoadSkillList()
	{
		skillItems.Clear();
		for (int i = 0; i < cols * rows; i++)
		{
			(capi.World as ClientMain).macroManager.MacrosByIndex.TryGetValue(i, out var value);
			SkillItem item;
			if (value == null)
			{
				item = new SkillItem();
			}
			else
			{
				if (value.iconTexture == null)
				{
					(value as Macro).GenTexture(capi, (int)GuiElementPassiveItemSlot.unscaledSlotSize);
				}
				item = new SkillItem
				{
					Code = new AssetLocation(value.Code),
					Name = value.Name,
					Hotkey = value.KeyCombination,
					Texture = value.iconTexture
				};
			}
			skillItems.Add(item);
		}
	}

	private void ComposeDialog()
	{
		LoadSkillList();
		selectedIndex = 0;
		currentSkillitem = skillItems[0];
		int num = 5;
		double num2 = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
		double num3 = (double)cols * num2;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 30.0, num3, (double)rows * num2);
		ElementBounds elementBounds2 = elementBounds.ForkBoundingParent(3.0, 6.0, 3.0, 3.0);
		double fixedWidth = num3 / 2.0 - 5.0;
		ElementBounds elementBounds3 = ElementBounds.FixedSize(fixedWidth, 30.0).FixedUnder(elementBounds2, num + 10);
		ElementBounds elementBounds4 = ElementBounds.Fixed(num3 / 2.0 + 8.0, 0.0, fixedWidth, 30.0).FixedUnder(elementBounds2, num + 10);
		ElementBounds elementBounds5 = ElementBounds.FixedSize(fixedWidth, 30.0).FixedUnder(elementBounds3, num - 10);
		ElementBounds elementBounds6 = ElementBounds.Fixed(num3 / 2.0 + 8.0, 0.0, fixedWidth, 30.0).FixedUnder(elementBounds4, num - 10);
		ElementBounds elementBounds7 = ElementBounds.FixedSize(300.0, 30.0).FixedUnder(elementBounds5, num + 10);
		ElementBounds elementBounds8 = ElementBounds.Fixed(0.0, 0.0, num3 - 20.0, 100.0);
		ElementBounds elementBounds9 = ElementBounds.Fixed(0.0, 0.0, num3 - 20.0 - 1.0, 99.0).FixedUnder(elementBounds7, num - 10);
		ElementBounds bounds = elementBounds9.CopyOffsetedSibling(elementBounds9.fixedWidth + 6.0, -1.0).WithFixedWidth(20.0).FixedGrow(0.0, 2.0);
		ElementBounds bounds2 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds9, 6 + 2 * num).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds bounds3 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds9, 6 + 2 * num).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds10 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds10.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds4 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		if (base.SingleComposer != null)
		{
			base.SingleComposer.Dispose();
		}
		base.SingleComposer = capi.Gui.CreateCompo("texteditordialog", bounds4).AddShadedDialogBG(elementBounds10).AddDialogTitleBar(Lang.Get("Macro Editor"), OnTitleBarClose)
			.BeginChildElements(elementBounds10)
			.AddInset(elementBounds2, 3)
			.BeginChildElements()
			.AddSkillItemGrid(skillItems, cols, rows, OnSlotClick, elementBounds, "skillitemgrid")
			.EndChildElements()
			.AddStaticText(Lang.Get("macroname"), CairoFont.WhiteSmallText(), elementBounds3)
			.AddStaticText(Lang.Get("macrohotkey"), CairoFont.WhiteSmallText(), elementBounds4)
			.AddTextInput(elementBounds5, OnMacroNameChanged, CairoFont.TextInput(), "macroname")
			.AddInset(elementBounds6, 2, 0.7f)
			.AddDynamicText("", CairoFont.TextInput(), elementBounds6.FlatCopy().WithFixedPadding(3.0, 3.0).WithFixedOffset(3.0, 3.0), "hotkey")
			.AddStaticText(Lang.Get("macrocommands"), CairoFont.WhiteSmallText(), elementBounds7)
			.BeginClip(elementBounds9)
			.AddTextArea(elementBounds8, OnCommandCodeChanged, CairoFont.TextInput().WithFontSize(16f), "commands")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarvalue, bounds, "scrollbar")
			.AddSmallButton(Lang.Get("Delete"), OnClearMacro, bounds2)
			.AddSmallButton(Lang.Get("Save"), OnSaveMacro, bounds3)
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetTextArea("commands").OnCursorMoved = OnTextAreaCursorMoved;
		base.SingleComposer.GetScrollbar("scrollbar").SetHeights((float)elementBounds8.fixedHeight - 1f, (float)elementBounds8.fixedHeight);
		base.SingleComposer.GetSkillItemGrid("skillitemgrid").selectedIndex = 0;
		OnSlotClick(0);
		base.SingleComposer.UnfocusOwnElements();
	}

	private void OnTextAreaCursorMoved(double posX, double posY)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		FontExtents fontExtents = base.SingleComposer.GetTextArea("commands").Font.GetFontExtents();
		double height = ((FontExtents)(ref fontExtents)).Height;
		base.SingleComposer.GetScrollbar("scrollbar").EnsureVisible(posX, posY);
		base.SingleComposer.GetScrollbar("scrollbar").EnsureVisible(posX, posY + height + 5.0);
	}

	private void OnCommandCodeChanged(string newCode)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
		base.SingleComposer.GetScrollbar("scrollbar").SetNewTotalHeight((float)textArea.Bounds.OuterHeight);
	}

	private void OnMacroNameChanged(string newname)
	{
	}

	private void OnSlotClick(int index)
	{
		GuiElementTextInput textInput = base.SingleComposer.GetTextInput("macroname");
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
		GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("hotkey");
		base.SingleComposer.GetSkillItemGrid("skillitemgrid").selectedIndex = index;
		selectedIndex = index;
		currentSkillitem = skillItems[index];
		if ((capi.World as ClientMain).macroManager.MacrosByIndex.ContainsKey(index))
		{
			currentMacro = SelectedMacro;
		}
		else
		{
			currentMacro = new Macro();
			currentSkillitem = new SkillItem();
		}
		textInput.SetValue(currentSkillitem.Name);
		textArea.LoadValue(textArea.Lineize(string.Join("\r\n", currentMacro.Commands)));
		if (currentSkillitem.Hotkey != null)
		{
			dynamicText.SetNewText(currentSkillitem.Hotkey?.ToString() ?? "");
		}
		else
		{
			dynamicText.SetNewText("");
		}
		base.SingleComposer.GetScrollbar("scrollbar").SetNewTotalHeight((float)textArea.Bounds.OuterHeight);
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		ComposeDialog();
	}

	private bool OnClearMacro()
	{
		if (selectedIndex < 0)
		{
			return true;
		}
		(capi.World as ClientMain).macroManager.DeleteMacro(selectedIndex);
		GuiElementTextInput textInput = base.SingleComposer.GetTextInput("macroname");
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
		GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("hotkey");
		textInput.SetValue("");
		textArea.SetValue("");
		dynamicText.SetNewText("");
		currentMacro = new Macro();
		currentSkillitem = new SkillItem();
		LoadSkillList();
		return true;
	}

	private bool OnSaveMacro()
	{
		if (selectedIndex < 0 || currentMacro == null)
		{
			return true;
		}
		GuiElementTextInput textInput = base.SingleComposer.GetTextInput("macroname");
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
		currentMacro.Name = textInput.GetText();
		if (currentMacro.Name.Length == 0)
		{
			currentMacro.Name = "Macro " + (selectedIndex + 1);
			textInput.SetValue(currentMacro.Name);
		}
		currentMacro.Commands = textArea.GetLines().ToArray();
		for (int i = 0; i < currentMacro.Commands.Length; i++)
		{
			currentMacro.Commands[i] = currentMacro.Commands[i].TrimEnd('\n', '\r');
		}
		currentMacro.Index = selectedIndex;
		currentMacro.Code = Regex.Replace(currentMacro.Name.Replace(" ", "_"), "[^a-z0-9_-]+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		currentMacro.GenTexture(capi, (int)GuiElementPassiveItemSlot.unscaledSlotSize);
		MacroManager macroManager = (capi.World as ClientMain).macroManager;
		base.SingleComposer.GetTextInput("macroname").Font.WithColor(GuiStyle.DialogDefaultTextColor);
		if (macroManager.MacrosByIndex.Values.FirstOrDefault((IMacroBase m) => m.Code == currentMacro.Code && m.Index != selectedIndex) != null)
		{
			capi.TriggerIngameError(this, "duplicatemacro", Lang.Get("A macro of this name exists already, please choose another name"));
			base.SingleComposer.GetTextInput("macroname").Font.WithColor(GuiStyle.ErrorTextColor);
			return false;
		}
		macroManager.SetMacro(selectedIndex, currentMacro);
		LoadSkillList();
		return true;
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	private void OnNewScrollbarvalue(float value)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
		textArea.Bounds.fixedY = 1f - value;
		textArea.Bounds.CalcWorldBounds();
	}

	public override void OnMouseDown(MouseEvent args)
	{
		base.OnMouseDown(args);
		if (selectedIndex >= 0)
		{
			GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("hotkey");
			dynamicText.Font.Color = new double[4] { 1.0, 1.0, 1.0, 0.9 };
			dynamicText.RecomposeText();
			if (dynamicText.Bounds.PointInside(args.X, args.Y))
			{
				dynamicText.SetNewText("?");
				hotkeyCapturer.BeginCapture();
			}
			else
			{
				CancelCapture();
			}
		}
	}

	public override void OnKeyUp(KeyEvent args)
	{
		if (!hotkeyCapturer.OnKeyUp(args, delegate
		{
			if (currentMacro != null)
			{
				currentMacro.KeyCombination = hotkeyCapturer.CapturedKeyComb;
				GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("hotkey");
				if (ScreenManager.hotkeyManager.IsHotKeyRegistered(currentMacro.KeyCombination))
				{
					dynamicText.Font.Color = GuiStyle.ErrorTextColor;
				}
				else
				{
					dynamicText.Font.Color = new double[4] { 1.0, 1.0, 1.0, 0.9 };
				}
				dynamicText.SetNewText(hotkeyCapturer.CapturedKeyComb.ToString(), autoHeight: false, forceRedraw: true);
			}
		}))
		{
			base.OnKeyUp(args);
		}
	}

	public override void OnKeyDown(KeyEvent args)
	{
		if (hotkeyCapturer.OnKeyDown(args))
		{
			if (hotkeyCapturer.IsCapturing())
			{
				base.SingleComposer.GetDynamicText("hotkey").SetNewText(hotkeyCapturer.CapturingKeyComb.ToString());
			}
			else
			{
				CancelCapture();
			}
		}
		else
		{
			base.OnKeyDown(args);
		}
	}

	private void CancelCapture()
	{
		GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("hotkey");
		if (SelectedMacro?.KeyCombination != null)
		{
			dynamicText.SetNewText(SelectedMacro.KeyCombination.ToString());
		}
		hotkeyCapturer.EndCapture();
	}

	public override bool CaptureAllInputs()
	{
		return hotkeyCapturer.IsCapturing();
	}

	public override void Dispose()
	{
		base.Dispose();
		if (skillItems == null)
		{
			return;
		}
		foreach (SkillItem skillItem in skillItems)
		{
			skillItem?.Dispose();
		}
	}
}
