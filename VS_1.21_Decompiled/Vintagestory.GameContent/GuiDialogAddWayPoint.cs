using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogAddWayPoint : GuiDialogGeneric
{
	private EnumDialogType dialogType;

	internal Vec3d WorldPos;

	private int[] colors;

	private string[] icons;

	private string curIcon;

	private string curColor;

	private bool autoSuggest = true;

	private bool ignoreNextAutosuggestDisable;

	public override bool PrefersUngrabbedMouse => true;

	public override EnumDialogType DialogType => dialogType;

	public override double DrawOrder => 0.2;

	public override bool DisableMouseGrab => true;

	public GuiDialogAddWayPoint(ICoreClientAPI capi, WaypointMapLayer wml)
		: base("", capi)
	{
		icons = wml.WaypointIcons.Keys.ToArray();
		colors = wml.WaypointColors.ToArray();
		ComposeDialog();
	}

	public override bool TryOpen()
	{
		ComposeDialog();
		return base.TryOpen();
	}

	private void ComposeDialog()
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 28.0, 90.0, 25.0);
		ElementBounds elementBounds2 = elementBounds.RightCopy();
		ElementBounds elementBounds3 = ElementBounds.Fixed(0.0, 28.0, 360.0, 25.0);
		ElementBounds elementBounds4 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds4.BothSizing = ElementSizing.FitToChildren;
		elementBounds4.WithChildren(elementBounds, elementBounds2);
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		if (base.SingleComposer != null)
		{
			base.SingleComposer.Dispose();
		}
		int num = 22;
		curIcon = icons[0];
		curColor = ColorUtil.Int2Hex(colors[0]);
		base.SingleComposer = capi.Gui.CreateCompo("worldmap-addwp", bounds).AddShadedDialogBG(elementBounds4, withTitleBar: false).AddDialogTitleBar(Lang.Get("Add waypoint"), delegate
		{
			TryClose();
		})
			.BeginChildElements(elementBounds4)
			.AddStaticText(Lang.Get("Name"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.FlatCopy())
			.AddTextInput(elementBounds2 = elementBounds2.FlatCopy().WithFixedWidth(200.0), onNameChanged, CairoFont.TextInput(), "nameInput")
			.AddStaticText(Lang.Get("Pinned"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.BelowCopy(0.0, 9.0))
			.AddSwitch(onPinnedToggled, elementBounds2 = elementBounds2.BelowCopy(0.0, 5.0).WithFixedWidth(200.0), "pinnedSwitch")
			.AddRichtext(Lang.Get("waypoint-color"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.BelowCopy(0.0, 5.0))
			.AddColorListPicker(colors, onColorSelected, elementBounds = elementBounds.BelowCopy(0.0, 5.0).WithFixedSize(num, num), 270, "colorpicker")
			.AddStaticText(Lang.Get("Icon"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.WithFixedPosition(0.0, elementBounds.fixedY + elementBounds.fixedHeight).WithFixedWidth(200.0).BelowCopy())
			.AddIconListPicker(icons, onIconSelected, elementBounds = elementBounds.BelowCopy(0.0, 5.0).WithFixedSize(num + 5, num + 5), 270, "iconpicker")
			.AddSmallButton(Lang.Get("Cancel"), onCancel, elementBounds3.FlatCopy().FixedUnder(elementBounds).WithFixedWidth(100.0))
			.AddSmallButton(Lang.Get("Save"), onSave, elementBounds3.FlatCopy().FixedUnder(elementBounds).WithFixedWidth(100.0)
				.WithAlignment(EnumDialogArea.RightFixed), EnumButtonStyle.Normal, "saveButton")
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetButton("saveButton").Enabled = false;
		base.SingleComposer.ColorListPickerSetValue("colorpicker", 0);
		base.SingleComposer.IconListPickerSetValue("iconpicker", 0);
	}

	private void onIconSelected(int index)
	{
		curIcon = icons[index];
		autoSuggestName();
	}

	private void onColorSelected(int index)
	{
		curColor = ColorUtil.Int2Hex(colors[index]);
		autoSuggestName();
	}

	private void onPinnedToggled(bool on)
	{
	}

	private void autoSuggestName()
	{
		if (autoSuggest)
		{
			GuiElementTextInput textInput = base.SingleComposer.GetTextInput("nameInput");
			ignoreNextAutosuggestDisable = true;
			if (Lang.HasTranslation("wpSuggestion-" + curIcon + "-" + curColor))
			{
				textInput.SetValue(Lang.Get("wpSuggestion-" + curIcon + "-" + curColor));
			}
			else if (Lang.HasTranslation("wpSuggestion-" + curIcon))
			{
				textInput.SetValue(Lang.Get("wpSuggestion-" + curIcon));
			}
			else
			{
				textInput.SetValue("");
			}
		}
	}

	private bool onSave()
	{
		string text = base.SingleComposer.GetTextInput("nameInput").GetText();
		bool flag = base.SingleComposer.GetSwitch("pinnedSwitch").On;
		capi.SendChatMessage($"/waypoint addati {curIcon} ={WorldPos.X.ToString(GlobalConstants.DefaultCultureInfo)} ={WorldPos.Y.ToString(GlobalConstants.DefaultCultureInfo)} ={WorldPos.Z.ToString(GlobalConstants.DefaultCultureInfo)} {flag} {curColor} {text}");
		TryClose();
		return true;
	}

	private bool onCancel()
	{
		TryClose();
		return true;
	}

	private void onNameChanged(string t1)
	{
		base.SingleComposer.GetButton("saveButton").Enabled = t1.Trim() != "";
		if (!ignoreNextAutosuggestDisable)
		{
			autoSuggest = t1.Length == 0;
		}
		ignoreNextAutosuggestDisable = false;
	}

	public override bool CaptureAllInputs()
	{
		return IsOpened();
	}

	public override void OnMouseDown(MouseEvent args)
	{
		base.OnMouseDown(args);
		args.Handled = true;
	}

	public override void OnMouseUp(MouseEvent args)
	{
		base.OnMouseUp(args);
		args.Handled = true;
	}

	public override void OnMouseMove(MouseEvent args)
	{
		base.OnMouseMove(args);
		args.Handled = true;
	}

	public override void OnMouseWheel(MouseWheelEventArgs args)
	{
		base.OnMouseWheel(args);
		args.SetHandled();
	}
}
