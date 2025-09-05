using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiDialogBlockEntityInventory : GuiDialogBlockEntity
{
	private int cols;

	private EnumPosFlag screenPos;

	public override double DrawOrder => 0.2;

	public GuiDialogBlockEntityInventory(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, int cols, ICoreClientAPI capi)
		: base(dialogTitle, inventory, blockEntityPos, capi)
	{
		if (base.IsDuplicate)
		{
			return;
		}
		this.cols = cols;
		double elementToDialogPadding = GuiStyle.ElementToDialogPadding;
		double unscaledSlotPadding = GuiElementItemSlotGridBase.unscaledSlotPadding;
		int num = (int)Math.Ceiling((float)inventory.Count / (float)cols);
		int num2 = Math.Min(num, 7);
		ElementBounds elementBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, unscaledSlotPadding, unscaledSlotPadding, cols, num2);
		ElementBounds elementBounds2 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, cols, num);
		ElementBounds elementBounds3 = elementBounds.ForkBoundingParent(6.0, 6.0, 6.0, 6.0);
		screenPos = GetFreePos("smallblockgui");
		if (num2 < num)
		{
			ElementBounds elementBounds4 = elementBounds.CopyOffsetedSibling();
			elementBounds4.fixedHeight -= 3.0;
			ElementBounds elementBounds5 = elementBounds3.ForkBoundingParent(elementToDialogPadding, elementToDialogPadding + 30.0, elementToDialogPadding + 20.0, elementToDialogPadding).WithFixedAlignmentOffset(IsRight(screenPos) ? (0.0 - GuiStyle.DialogToScreenPadding) : GuiStyle.DialogToScreenPadding, 0.0).WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);
			if (!capi.Settings.Bool["immersiveMouseMode"])
			{
				elementBounds5.fixedOffsetY += (elementBounds5.fixedHeight + 10.0) * (double)YOffsetMul(screenPos);
				elementBounds5.fixedOffsetX += (elementBounds5.fixedWidth + 10.0) * (double)XOffsetMul(screenPos);
			}
			ElementBounds bounds = ElementStdBounds.VerticalScrollbar(elementBounds3).WithParent(elementBounds5);
			base.SingleComposer = capi.Gui.CreateCompo("blockentityinventory" + blockEntityPos, elementBounds5).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(dialogTitle, base.CloseIconPressed)
				.AddInset(elementBounds3)
				.AddVerticalScrollbar(base.OnNewScrollbarvalue, bounds, "scrollbar")
				.BeginClip(elementBounds4)
				.AddItemSlotGrid(inventory, base.DoSendPacket, cols, elementBounds2, "slotgrid")
				.EndClip()
				.Compose();
			base.SingleComposer.GetScrollbar("scrollbar").SetHeights((float)elementBounds.fixedHeight, (float)(elementBounds2.fixedHeight + unscaledSlotPadding));
		}
		else
		{
			ElementBounds elementBounds6 = elementBounds3.ForkBoundingParent(elementToDialogPadding, elementToDialogPadding + 20.0, elementToDialogPadding, elementToDialogPadding).WithFixedAlignmentOffset(IsRight(screenPos) ? (0.0 - GuiStyle.DialogToScreenPadding) : GuiStyle.DialogToScreenPadding, 0.0).WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);
			if (!capi.Settings.Bool["immersiveMouseMode"])
			{
				elementBounds6.fixedOffsetY += (elementBounds6.fixedHeight + 10.0) * (double)YOffsetMul(screenPos);
				elementBounds6.fixedOffsetX += (elementBounds6.fixedWidth + 10.0) * (double)XOffsetMul(screenPos);
			}
			base.SingleComposer = capi.Gui.CreateCompo("blockentityinventory" + blockEntityPos, elementBounds6).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(dialogTitle, base.CloseIconPressed)
				.AddInset(elementBounds3)
				.AddItemSlotGrid(inventory, base.DoSendPacket, cols, elementBounds, "slotgrid")
				.Compose();
		}
		base.SingleComposer.UnfocusOwnElements();
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		FreePos("smallblockgui", screenPos);
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		if (capi.Gui.GetDialogPosition(base.SingleComposer.DialogName) == null)
		{
			OccupyPos("smallblockgui", screenPos);
		}
	}
}
