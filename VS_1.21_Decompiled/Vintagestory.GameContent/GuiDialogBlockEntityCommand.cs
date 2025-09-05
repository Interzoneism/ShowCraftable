using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogBlockEntityCommand : GuiDialogBlockEntity
{
	public override string ToggleKeyCombinationCode => null;

	public GuiDialogBlockEntityCommand(BlockPos BlockEntityPosition, string command, bool silent, ICoreClientAPI capi, string title)
		: base("Command block", BlockEntityPosition, capi)
	{
		_ = GuiElementItemSlotGridBase.unscaledSlotPadding;
		int num = 5;
		_ = GuiElementPassiveItemSlot.unscaledSlotSize;
		_ = GuiElementItemSlotGridBase.unscaledSlotPadding;
		double num2 = 700.0;
		_ = num2 / 2.0;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 30.0, num2, 30.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 0.0, num2 - 20.0, 200.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(0.0, 0.0, num2 - 20.0 - 1.0, 199.0).FixedUnder(elementBounds, num - 10);
		ElementBounds bounds = elementBounds3.CopyOffsetedSibling(elementBounds3.fixedWidth + 6.0, -1.0).WithFixedWidth(20.0).FixedGrow(0.0, 2.0);
		ElementBounds elementBounds4 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds3, 2 + 2 * num).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds bounds2 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds4, 44 + 2 * num).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds bounds3 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds4, 44 + 2 * num).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds5 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds5.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds4 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		if (base.SingleComposer != null)
		{
			base.SingleComposer.Dispose();
		}
		base.SingleComposer = capi.Gui.CreateCompo("commandeditordialog", bounds4).AddShadedDialogBG(elementBounds5).AddDialogTitleBar(title, OnTitleBarClose)
			.BeginChildElements(elementBounds5)
			.AddStaticText("Commands", CairoFont.WhiteSmallText(), elementBounds)
			.BeginClip(elementBounds3)
			.AddTextArea(elementBounds2, OnCommandCodeChanged, CairoFont.TextInput().WithFontSize(16f), "commands")
			.EndClip()
			.AddVerticalScrollbar(OnNewCmdScrollbarvalue, bounds, "scrollbar")
			.AddSwitch(null, elementBounds2.BelowCopy(0.0, 10.0).WithFixedSize(30.0, 30.0), "silentSwitch", 25.0, 3.0)
			.AddStaticText(Lang.Get("Execute silently"), CairoFont.WhiteSmallText(), elementBounds2.BelowCopy(30.0, 13.0).WithFixedSize(150.0, 30.0))
			.AddSmallButton(Lang.Get("Copy to clipboard"), OnCopy, elementBounds4)
			.AddSmallButton(Lang.Get("Cancel"), OnCancel, bounds2)
			.AddSmallButton(Lang.Get("Save"), OnSave, bounds3)
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetTextArea("commands").SetValue(command);
		base.SingleComposer.GetTextArea("commands").OnCursorMoved = OnTextAreaCursorMoved;
		base.SingleComposer.GetSwitch("silentSwitch").On = silent;
		base.SingleComposer.GetScrollbar("scrollbar").SetHeights((float)elementBounds2.fixedHeight - 1f, (float)elementBounds2.fixedHeight);
		base.SingleComposer.UnfocusOwnElements();
	}

	private bool OnCopy()
	{
		capi.Input.ClipboardText = base.SingleComposer.GetTextArea("commands").GetText();
		return true;
	}

	private void OnNewCmdScrollbarvalue(float value)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
		textArea.Bounds.fixedY = 1f - value;
		textArea.Bounds.CalcWorldBounds();
	}

	private bool OnCancel()
	{
		TryClose();
		return true;
	}

	private bool OnSave()
	{
		string text = base.SingleComposer.GetTextArea("commands").GetText();
		bool silent = base.SingleComposer.GetSwitch("silentSwitch").On;
		capi.Network.SendBlockEntityPacket(base.BlockEntityPosition, 12, SerializerUtil.Serialize(new BlockEntityCommandPacket
		{
			Commands = text,
			Silent = silent
		}));
		TryClose();
		return true;
	}

	private void OnCommandCodeChanged(string t1)
	{
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

	private void OnTitleBarClose()
	{
		TryClose();
	}
}
