using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogBlockEntityQuern : GuiDialogBlockEntity
{
	private long lastRedrawMs;

	private float inputGrindTime;

	private float maxGrindTime;

	protected override double FloatyDialogPosition => 0.75;

	public GuiDialogBlockEntityQuern(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
		: base(DialogTitle, Inventory, BlockEntityPosition, capi)
	{
		if (!base.IsDuplicate)
		{
			capi.World.Player.InventoryManager.OpenInventory(Inventory);
			SetupDialog();
		}
	}

	private void OnInventorySlotModified(int slotid)
	{
		capi.Event.EnqueueMainThreadTask(SetupDialog, "setupquerndlg");
	}

	private void SetupDialog()
	{
		ItemSlot itemSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
		if (itemSlot != null && itemSlot.Inventory == base.Inventory)
		{
			capi.Input.TriggerOnMouseLeaveSlot(itemSlot);
		}
		else
		{
			itemSlot = null;
		}
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 90.0);
		ElementBounds bounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 30.0, 1, 1);
		ElementBounds bounds2 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 153.0, 30.0, 1, 1);
		ElementBounds elementBounds2 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds2.BothSizing = ElementSizing.FitToChildren;
		elementBounds2.WithChildren(elementBounds);
		ElementBounds bounds3 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		ClearComposers();
		base.SingleComposer = capi.Gui.CreateCompo("blockentitymillstone" + base.BlockEntityPosition, bounds3).AddShadedDialogBG(elementBounds2).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
			.BeginChildElements(elementBounds2)
			.AddDynamicCustomDraw(elementBounds, OnBgDraw, "symbolDrawer")
			.AddItemSlotGrid(base.Inventory, SendInvPacket, 1, new int[1], bounds, "inputSlot")
			.AddItemSlotGrid(base.Inventory, SendInvPacket, 1, new int[1] { 1 }, bounds2, "outputslot")
			.EndChildElements()
			.Compose();
		lastRedrawMs = capi.ElapsedMilliseconds;
		if (itemSlot != null)
		{
			base.SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
		}
	}

	public void Update(float inputGrindTime, float maxGrindTime)
	{
		this.inputGrindTime = inputGrindTime;
		this.maxGrindTime = maxGrindTime;
		if (IsOpened() && capi.ElapsedMilliseconds - lastRedrawMs > 500)
		{
			if (base.SingleComposer != null)
			{
				base.SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
			}
			lastRedrawMs = capi.ElapsedMilliseconds;
		}
	}

	private void OnBgDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
	{
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Expected O, but got Unknown
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		double num = 30.0;
		ctx.Save();
		Matrix matrix = ctx.Matrix;
		matrix.Translate(GuiElement.scaled(63.0), GuiElement.scaled(num + 2.0));
		matrix.Scale(GuiElement.scaled(0.6), GuiElement.scaled(0.6));
		ctx.Matrix = matrix;
		capi.Gui.Icons.DrawArrowRight(ctx, 2.0);
		double num2 = inputGrindTime / maxGrindTime;
		ctx.Rectangle(GuiElement.scaled(5.0), 0.0, GuiElement.scaled(125.0 * num2), GuiElement.scaled(100.0));
		ctx.Clip();
		LinearGradient val = new LinearGradient(0.0, 0.0, GuiElement.scaled(200.0), 0.0);
		((Gradient)val).AddColorStop(0.0, new Color(0.0, 0.4, 0.0, 1.0));
		((Gradient)val).AddColorStop(1.0, new Color(0.2, 0.6, 0.2, 1.0));
		ctx.SetSource((Pattern)(object)val);
		capi.Gui.Icons.DrawArrowRight(ctx, 0.0, strokeOrFill: false, defaultPattern: false);
		((Pattern)val).Dispose();
		ctx.Restore();
	}

	private void SendInvPacket(object p)
	{
		capi.Network.SendBlockEntityPacket(base.BlockEntityPosition.X, base.BlockEntityPosition.Y, base.BlockEntityPosition.Z, p);
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		base.Inventory.SlotModified += OnInventorySlotModified;
	}

	public override void OnGuiClosed()
	{
		base.Inventory.SlotModified -= OnInventorySlotModified;
		base.SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("outputslot").OnGuiClosed(capi);
		base.OnGuiClosed();
	}
}
