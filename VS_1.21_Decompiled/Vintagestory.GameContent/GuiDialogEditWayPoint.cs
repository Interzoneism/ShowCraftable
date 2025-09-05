using System.Drawing;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogEditWayPoint : GuiDialogGeneric
{
	private EnumDialogType dialogType;

	private int[] colors;

	private string[] icons;

	private Waypoint waypoint;

	private int wpIndex;

	internal Vec3d WorldPos;

	public override bool PrefersUngrabbedMouse => true;

	public override EnumDialogType DialogType => dialogType;

	public Waypoint Waypoint => waypoint;

	public int WpIndex => wpIndex;

	public override double DrawOrder => 0.2;

	public override bool DisableMouseGrab => true;

	public GuiDialogEditWayPoint(ICoreClientAPI capi, WaypointMapLayer wml, Waypoint waypoint, int index)
		: base("", capi)
	{
		icons = wml.WaypointIcons.Keys.ToArray();
		colors = wml.WaypointColors.ToArray();
		wpIndex = index;
		this.waypoint = waypoint;
		ComposeDialog();
	}

	public override bool TryOpen()
	{
		ComposeDialog();
		return base.TryOpen();
	}

	private void ComposeDialog()
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 28.0, 120.0, 25.0);
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
		int num2 = icons.IndexOf(waypoint.Icon);
		if (num2 < 0)
		{
			num2 = 0;
		}
		int num3 = colors.IndexOf(waypoint.Color);
		if (num3 < 0)
		{
			colors = colors.Append(waypoint.Color);
			num3 = colors.Length - 1;
		}
		base.SingleComposer = capi.Gui.CreateCompo("worldmap-modwp", bounds).AddShadedDialogBG(elementBounds4, withTitleBar: false).AddDialogTitleBar(Lang.Get("Modify waypoint"), delegate
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
			.AddStaticText(Lang.Get("Icon"), CairoFont.WhiteSmallText(), elementBounds = elementBounds.WithFixedPosition(0.0, elementBounds.fixedY + elementBounds.fixedHeight).WithFixedWidth(100.0).BelowCopy())
			.AddIconListPicker(icons, onIconSelected, elementBounds = elementBounds.BelowCopy(0.0, 5.0).WithFixedSize(num + 5, num + 5), 270, "iconpicker")
			.AddSmallButton(Lang.Get("Cancel"), onCancel, elementBounds3.FlatCopy().FixedUnder(elementBounds).WithFixedWidth(100.0))
			.AddSmallButton(Lang.Get("Delete"), onDelete, elementBounds3.FlatCopy().FixedUnder(elementBounds).WithFixedWidth(100.0)
				.WithAlignment(EnumDialogArea.CenterFixed))
			.AddSmallButton(Lang.Get("Save"), onSave, elementBounds3.FlatCopy().FixedUnder(elementBounds).WithFixedWidth(100.0)
				.WithAlignment(EnumDialogArea.RightFixed), EnumButtonStyle.Normal, "saveButton")
			.EndChildElements()
			.Compose();
		Color.FromArgb(255, ColorUtil.ColorR(waypoint.Color), ColorUtil.ColorG(waypoint.Color), ColorUtil.ColorB(waypoint.Color));
		base.SingleComposer.ColorListPickerSetValue("colorpicker", num3);
		base.SingleComposer.IconListPickerSetValue("iconpicker", num2);
		base.SingleComposer.GetTextInput("nameInput").SetValue(waypoint.Title);
		base.SingleComposer.GetSwitch("pinnedSwitch").SetValue(waypoint.Pinned);
	}

	private void onIconSelected(int index)
	{
		waypoint.Icon = icons[index];
	}

	private void onColorSelected(int index)
	{
		waypoint.Color = colors[index];
	}

	private void onPinnedToggled(bool t1)
	{
	}

	private void onIconSelectionChanged(string code, bool selected)
	{
	}

	private bool onDelete()
	{
		capi.SendChatMessage($"/waypoint remove {wpIndex}");
		TryClose();
		return true;
	}

	private bool onSave()
	{
		string text = base.SingleComposer.GetTextInput("nameInput").GetText();
		bool flag = base.SingleComposer.GetSwitch("pinnedSwitch").On;
		capi.SendChatMessage($"/waypoint modify {wpIndex} {ColorUtil.Int2Hex(waypoint.Color)} {waypoint.Icon} {flag} {text}");
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
