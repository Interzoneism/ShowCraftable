using System;
using System.Linq;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class GuiJsonDialog : GuiDialogGeneric
{
	private JsonDialogSettings settings;

	public override string DebugName => "jsondialog-" + settings.Code;

	public override string ToggleKeyCombinationCode => null;

	public override bool PrefersUngrabbedMouse => settings.DisableWorldInteract;

	public GuiJsonDialog(JsonDialogSettings settings, ICoreClientAPI capi)
		: base("", capi)
	{
		this.settings = settings;
		ComposeDialog(focusFirstElement: true);
	}

	public GuiJsonDialog(JsonDialogSettings settings, ICoreClientAPI capi, bool focusFirstElement)
		: base("", capi)
	{
		this.settings = settings;
		ComposeDialog(focusFirstElement);
	}

	public override void Recompose()
	{
		ComposeDialog();
	}

	public void ComposeDialog(bool focusFirstElement = false)
	{
		double sizeMultiplier = settings.SizeMultiplier;
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(settings.Alignment).WithFixedPadding(10.0).WithScale(sizeMultiplier)
			.WithFixedPosition(settings.PosX, settings.PosY);
		GuiComposer guiComposer = capi.Gui.CreateCompo("cmdDlg" + settings.Code, bounds).AddDialogBG(ElementStdBounds.DialogBackground().WithScale(sizeMultiplier).WithFixedPadding(settings.Padding), withTitleBar: false).BeginChildElements();
		double num = 0.0;
		int num2 = 1;
		for (int i = 0; i < settings.Rows.Length; i++)
		{
			DialogRow dialogRow = settings.Rows[i];
			num += (double)dialogRow.TopPadding;
			double num3 = 0.0;
			double num4 = 0.0;
			for (int j = 0; j < dialogRow.Elements.Length; j++)
			{
				DialogElement dialogElement = dialogRow.Elements[j];
				num3 = Math.Max(dialogElement.Height, num3);
				num4 += (double)dialogElement.PaddingLeft;
				ComposeElement(guiComposer, settings, dialogElement, num2, num4, num);
				num2++;
				num4 += (double)(dialogElement.Width + 20);
			}
			num += num3 + (double)dialogRow.BottomPadding;
		}
		Composers["cmdDlg" + settings.Code] = guiComposer.EndChildElements().Compose(focusFirstElement);
	}

	private void ComposeElement(GuiComposer composer, JsonDialogSettings settings, DialogElement elem, int elemKey, double x, double y)
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		double sizeMultiplier = settings.SizeMultiplier;
		double num = 0.0;
		if (elem.Label != null)
		{
			CairoFont cairoFont = CairoFont.WhiteSmallText();
			cairoFont.UnscaledFontsize *= sizeMultiplier;
			string text = Lang.Get(elem.Label);
			TextExtents textExtents = cairoFont.GetTextExtents(text);
			num = ((TextExtents)(ref textExtents)).Width / sizeMultiplier / (double)RuntimeEnv.GUIScale + 1.0;
			FontExtents fontExtents = cairoFont.GetFontExtents();
			ElementBounds elementBounds = ElementBounds.Fixed(x, y + Math.Max(0.0, ((double)elem.Height * sizeMultiplier - ((FontExtents)(ref fontExtents)).Height / (double)RuntimeEnv.GUIScale) / 2.0), num, elem.Height).WithScale(sizeMultiplier);
			composer.AddStaticText(text, cairoFont, elementBounds);
			num += 8.0;
			if (elem.Tooltip != null)
			{
				CairoFont cairoFont2 = CairoFont.WhiteSmallText();
				cairoFont2.UnscaledFontsize *= sizeMultiplier;
				composer.AddHoverText(Lang.Get(elem.Tooltip), cairoFont2, 350, elementBounds.FlatCopy(), "tooltip-" + elem.Code);
				composer.GetHoverText("tooltip-" + elem.Code).SetAutoWidth(on: true);
			}
		}
		ElementBounds elementBounds2 = ElementBounds.Fixed(x + num, y, (double)elem.Width - num, elem.Height).WithScale(sizeMultiplier);
		string currentValue = settings.OnGet?.Invoke(elem.Code);
		switch (elem.Type)
		{
		case EnumDialogElementType.Slider:
		{
			string key3 = "slider-" + elemKey;
			composer.AddSlider(delegate(int newval)
			{
				settings.OnSet?.Invoke(elem.Code, newval.ToString() ?? "");
				return true;
			}, elementBounds2, key3);
			int.TryParse(currentValue, out var result);
			composer.GetSlider(key3).SetValues(result, elem.MinValue, elem.MaxValue, elem.Step);
			composer.GetSlider(key3).Scale = sizeMultiplier;
			break;
		}
		case EnumDialogElementType.Switch:
		{
			string key4 = "switch-" + elemKey;
			composer.AddSwitch(delegate(bool newval)
			{
				settings.OnSet?.Invoke(elem.Code, newval ? "1" : "0");
			}, elementBounds2, key4, 30.0 * sizeMultiplier, 5.0 * sizeMultiplier);
			composer.GetSwitch(key4).SetValue(currentValue == "1");
			break;
		}
		case EnumDialogElementType.Input:
		{
			string key6 = "input-" + elemKey;
			CairoFont cairoFont8 = CairoFont.WhiteSmallText();
			cairoFont8.UnscaledFontsize *= sizeMultiplier;
			composer.AddTextInput(elementBounds2, delegate(string newval)
			{
				settings.OnSet?.Invoke(elem.Code, newval);
			}, cairoFont8, key6);
			composer.GetTextInput(key6).SetValue(currentValue);
			break;
		}
		case EnumDialogElementType.NumberInput:
		{
			string key5 = "numberinput-" + elemKey;
			CairoFont cairoFont7 = CairoFont.WhiteSmallText();
			cairoFont7.UnscaledFontsize *= sizeMultiplier;
			composer.AddNumberInput(elementBounds2, delegate(string newval)
			{
				settings.OnSet?.Invoke(elem.Code, newval);
			}, cairoFont7, key5);
			composer.GetNumberInput(key5).SetValue(currentValue);
			break;
		}
		case EnumDialogElementType.Button:
			if (elem.Icon != null)
			{
				composer.AddIconButton(elem.Icon, delegate
				{
					settings.OnSet?.Invoke(elem.Code, null);
				}, elementBounds2);
			}
			else
			{
				CairoFont cairoFont5 = CairoFont.ButtonText();
				cairoFont5.WithFontSize(elem.FontSize);
				composer.AddButton(Lang.Get(elem.Text), delegate
				{
					settings.OnSet?.Invoke(elem.Code, null);
					return true;
				}, elementBounds2.WithFixedPadding(8.0, 0.0), cairoFont5);
			}
			if (elem.Tooltip != null && elem.Label == null)
			{
				CairoFont cairoFont6 = CairoFont.WhiteSmallText();
				cairoFont6.UnscaledFontsize *= sizeMultiplier;
				composer.AddHoverText(Lang.Get(elem.Tooltip), cairoFont6, 350, elementBounds2.FlatCopy(), "tooltip-" + elem.Code);
				composer.GetHoverText("tooltip-" + elem.Code).SetAutoWidth(on: true);
			}
			break;
		case EnumDialogElementType.Text:
			composer.AddStaticText(Lang.Get(elem.Text), CairoFont.WhiteMediumText().WithFontSize(elem.FontSize), elementBounds2);
			break;
		case EnumDialogElementType.Select:
		case EnumDialogElementType.DynamicSelect:
		{
			string[] array = elem.Values;
			string[] source = elem.Names;
			if (elem.Type == EnumDialogElementType.DynamicSelect)
			{
				string[] array2 = currentValue.Split(new string[1] { "\n" }, StringSplitOptions.None);
				array = array2[0].Split(new string[1] { "||" }, StringSplitOptions.None);
				source = array2[1].Split(new string[1] { "||" }, StringSplitOptions.None);
				currentValue = array2[2];
			}
			source = source.Select((string s) => Lang.Get(s)).ToArray();
			int selectedIndex = Array.FindIndex(array, (string w) => w.Equals(currentValue));
			if (elem.Mode == EnumDialogElementMode.DropDown)
			{
				string key = "dropdown-" + elemKey;
				composer.AddDropDown(array, source, selectedIndex, delegate(string newval, bool on)
				{
					settings.OnSet?.Invoke(elem.Code, newval);
				}, elementBounds2, key);
				composer.GetDropDown(key).Scale = sizeMultiplier;
				composer.GetDropDown(key).Font.UnscaledFontsize *= sizeMultiplier;
			}
			else
			{
				if (elem.Icons == null || elem.Icons.Length == 0)
				{
					break;
				}
				ElementBounds[] array3 = new ElementBounds[elem.Icons.Length];
				double num2 = (elem.Height - 4 * elem.Icons.Length) / elem.Icons.Length;
				for (int num3 = 0; num3 < array3.Length; num3++)
				{
					array3[num3] = elementBounds2.FlatCopy().WithFixedHeight(num2 - 4.0).WithFixedOffset(0.0, (double)num3 * (4.0 + num2))
						.WithScale(sizeMultiplier);
				}
				string key2 = "togglebuttons-" + elemKey;
				CairoFont cairoFont3 = CairoFont.WhiteSmallText();
				cairoFont3.UnscaledFontsize *= sizeMultiplier;
				composer.AddIconToggleButtons(elem.Icons, cairoFont3, delegate(int newval)
				{
					settings.OnSet?.Invoke(elem.Code, elem.Values[newval]);
				}, array3, key2);
				if (currentValue != null && currentValue.Length > 0)
				{
					composer.ToggleButtonsSetValue(key2, selectedIndex);
				}
				if (elem.Tooltips != null)
				{
					for (int num4 = 0; num4 < elem.Tooltips.Length; num4++)
					{
						CairoFont cairoFont4 = CairoFont.WhiteSmallText();
						cairoFont4.UnscaledFontsize *= sizeMultiplier;
						composer.AddHoverText(Lang.Get(elem.Tooltips[num4]), cairoFont4, 350, array3[num4].FlatCopy());
					}
				}
			}
			break;
		}
		}
	}

	public override void OnMouseDown(MouseEvent args)
	{
		base.OnMouseDown(args);
		foreach (GuiComposer value in Composers.Values)
		{
			if (value.Bounds.PointInside(args.X, args.Y))
			{
				args.Handled = true;
				break;
			}
		}
	}

	public void ReloadValues()
	{
		GuiComposer composer = Composers["cmdDlg" + settings.Code];
		int num = 1;
		for (int i = 0; i < settings.Rows.Length; i++)
		{
			DialogRow dialogRow = settings.Rows[i];
			for (int j = 0; j < dialogRow.Elements.Length; j++)
			{
				DialogElement dialogElement = dialogRow.Elements[j];
				string currentValue = settings.OnGet?.Invoke(dialogElement.Code);
				switch (dialogElement.Type)
				{
				case EnumDialogElementType.Slider:
				{
					string key7 = "slider-" + num;
					int.TryParse(currentValue, out var result);
					composer.GetSlider(key7).SetValues(result, dialogElement.MinValue, dialogElement.MaxValue, dialogElement.Step);
					break;
				}
				case EnumDialogElementType.Switch:
				{
					string key6 = "switch-" + num;
					composer.GetSwitch(key6).SetValue(currentValue == "1");
					break;
				}
				case EnumDialogElementType.Input:
				{
					string key4 = "input-" + num;
					composer.GetTextInput(key4).SetValue(currentValue);
					break;
				}
				case EnumDialogElementType.NumberInput:
				{
					string key5 = "numberinput-" + num;
					composer.GetNumberInput(key5).SetValue(currentValue);
					break;
				}
				case EnumDialogElementType.Select:
				case EnumDialogElementType.DynamicSelect:
				{
					string[] array = dialogElement.Values;
					if (dialogElement.Type == EnumDialogElementType.DynamicSelect)
					{
						string[] array2 = currentValue.Split(new string[1] { "\n" }, StringSplitOptions.None);
						array = array2[0].Split(new string[1] { "||" }, StringSplitOptions.None);
						string[] names = array2[1].Split(new string[1] { "||" }, StringSplitOptions.None);
						currentValue = array2[2];
						string key = "dropdown-" + num;
						composer.GetDropDown(key).SetList(array, names);
					}
					int selectedIndex = Array.FindIndex(array, (string w) => w.Equals(currentValue));
					if (dialogElement.Mode == EnumDialogElementMode.DropDown)
					{
						string key2 = "dropdown-" + num;
						composer.GetDropDown(key2).SetSelectedIndex(selectedIndex);
					}
					else if (dialogElement.Icons != null && dialogElement.Icons.Length != 0)
					{
						string key3 = "togglebuttons-" + num;
						if (currentValue != null && currentValue.Length > 0)
						{
							composer.ToggleButtonsSetValue(key3, selectedIndex);
						}
					}
					break;
				}
				}
				num++;
			}
		}
	}
}
