using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogBlockEntityTicker : GuiDialogBlockEntity
{
	public override string ToggleKeyCombinationCode => null;

	public GuiDialogBlockEntityTicker(BlockPos pos, int tickIntervalMs, bool active, ICoreClientAPI capi)
		: base("Command block", pos, capi)
	{
		_ = GuiElementItemSlotGridBase.unscaledSlotPadding;
		int num = 5;
		_ = GuiElementPassiveItemSlot.unscaledSlotSize;
		_ = GuiElementItemSlotGridBase.unscaledSlotPadding;
		double num2 = 400.0;
		_ = num2 / 2.0;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 34.0, num2, 30.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(110.0, 26.0, 80.0, 30.0);
		ElementBounds bounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds, 20 + 2 * num).WithFixedPadding(10.0, 2.0);
		ElementBounds bounds2 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds, 20 + 2 * num).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds3 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds3.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds3 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		if (base.SingleComposer != null)
		{
			base.SingleComposer.Dispose();
		}
		base.SingleComposer = capi.Gui.CreateCompo("commandeditordialog", bounds3).AddShadedDialogBG(elementBounds3).AddDialogTitleBar("Ticker Block", OnTitleBarClose)
			.BeginChildElements(elementBounds3)
			.AddStaticText("Timer (ms)", CairoFont.WhiteSmallText(), elementBounds)
			.AddNumberInput(elementBounds2, null, CairoFont.WhiteDetailText(), "automs")
			.AddSwitch(null, elementBounds2.RightCopy(90.0, 3.0).WithFixedPadding(0.0, 0.0), "onSwitch", 25.0, 3.0)
			.AddStaticText("Active", CairoFont.WhiteSmallText(), elementBounds2.RightCopy(120.0, 6.0))
			.AddSmallButton("Cancel", OnCancel, bounds)
			.AddSmallButton("Save", OnSave, bounds2)
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetNumberInput("automs").SetValue(tickIntervalMs);
		base.SingleComposer.GetSwitch("onSwitch").On = active;
		base.SingleComposer.UnfocusOwnElements();
	}

	private bool OnCancel()
	{
		TryClose();
		return true;
	}

	private bool OnSave()
	{
		EditTickerPacket data = new EditTickerPacket
		{
			Interval = (base.SingleComposer.GetNumberInput("automs").GetValue().ToString() ?? ""),
			Active = base.SingleComposer.GetSwitch("onSwitch").On
		};
		capi.Network.SendBlockEntityPacket(base.BlockEntityPosition, 12, SerializerUtil.Serialize(data));
		TryClose();
		return true;
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}
}
