using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogCreatureContents : GuiDialog
{
	private InventoryGeneric inv;

	private Entity owningEntity;

	public int packetIdOffset;

	private EnumPosFlag screenPos;

	private string title;

	private ICustomDialogPositioning icdp;

	private Vec3d entityPos = new Vec3d();

	public override string ToggleKeyCombinationCode => null;

	protected double FloatyDialogPosition => 0.6;

	protected double FloatyDialogAlign => 0.8;

	public override bool UnregisterOnClose => true;

	public override bool PrefersUngrabbedMouse => false;

	public GuiDialogCreatureContents(InventoryGeneric inv, Entity owningEntity, ICoreClientAPI capi, string code, string title = null, ICustomDialogPositioning icdp = null)
		: base(capi)
	{
		this.inv = inv;
		this.title = title;
		this.owningEntity = owningEntity;
		this.icdp = icdp;
		Compose(code);
	}

	public void Compose(string code)
	{
		double unscaledSlotPadding = GuiElementItemSlotGridBase.unscaledSlotPadding;
		int rows = (int)Math.Ceiling((float)inv.Count / 4f);
		ElementBounds elementBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, unscaledSlotPadding, 40.0 + unscaledSlotPadding, 4, rows).FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding);
		screenPos = GetFreePos("smallblockgui");
		float num = 10f;
		ElementBounds elementBounds2 = elementBounds.ForkBoundingParent(num, num + 30f, num, num).WithFixedAlignmentOffset(IsRight(screenPos) ? (0.0 - GuiStyle.DialogToScreenPadding) : GuiStyle.DialogToScreenPadding, 0.0).WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);
		if (!capi.Settings.Bool["immersiveMouseMode"])
		{
			elementBounds2.fixedOffsetY += (elementBounds2.fixedHeight + 10.0) * (double)YOffsetMul(screenPos);
			elementBounds2.fixedOffsetX += (elementBounds2.fixedWidth + 10.0) * (double)XOffsetMul(screenPos);
		}
		base.SingleComposer = capi.Gui.CreateCompo(code + owningEntity.EntityId, elementBounds2).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(Lang.Get(title ?? code), OnTitleBarClose)
			.AddItemSlotGrid(inv, DoSendPacket, 4, elementBounds, "slots")
			.Compose();
	}

	private void DoSendPacket(object p)
	{
		capi.Network.SendEntityPacketWithOffset(owningEntity.EntityId, packetIdOffset, p);
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		if (capi.Gui.GetDialogPosition(base.SingleComposer.DialogName) == null)
		{
			OccupyPos("smallblockgui", screenPos);
		}
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		capi.World.Player.InventoryManager.CloseInventoryAndSync(inv);
		base.SingleComposer.GetSlotGrid("slots").OnGuiClosed(capi);
		FreePos("smallblockgui", screenPos);
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (capi.Settings.Bool["immersiveMouseMode"])
		{
			double num = owningEntity.SelectionBox.X2 - owningEntity.OriginSelectionBox.X2;
			double num2 = owningEntity.SelectionBox.Z2 - owningEntity.OriginSelectionBox.Z2;
			Vec3d pos = new Vec3d(owningEntity.Pos.X + num, owningEntity.Pos.Y + FloatyDialogPosition, owningEntity.Pos.Z + num2);
			if (icdp != null)
			{
				pos = icdp.GetDialogPosition();
			}
			Vec3d vec3d = MatrixToolsd.Project(pos, capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
			if (vec3d.Z < 0.0)
			{
				return;
			}
			base.SingleComposer.Bounds.Alignment = EnumDialogArea.None;
			base.SingleComposer.Bounds.fixedOffsetX = 0.0;
			base.SingleComposer.Bounds.fixedOffsetY = 0.0;
			base.SingleComposer.Bounds.absFixedX = vec3d.X - base.SingleComposer.Bounds.OuterWidth / 2.0;
			base.SingleComposer.Bounds.absFixedY = (double)capi.Render.FrameHeight - vec3d.Y - base.SingleComposer.Bounds.OuterHeight * FloatyDialogAlign;
			base.SingleComposer.Bounds.absMarginX = 0.0;
			base.SingleComposer.Bounds.absMarginY = 0.0;
		}
		base.OnRenderGUI(deltaTime);
	}

	public override void OnFinalizeFrame(float dt)
	{
		base.OnFinalizeFrame(dt);
		entityPos.Set(owningEntity.Pos.X, owningEntity.Pos.Y, owningEntity.Pos.Z);
		entityPos.Add(owningEntity.SelectionBox.X2 - owningEntity.OriginSelectionBox.X2, 0.0, owningEntity.SelectionBox.Z2 - owningEntity.OriginSelectionBox.Z2);
		if (!IsInRangeOfBlock())
		{
			capi.Event.EnqueueMainThreadTask(delegate
			{
				TryClose();
			}, "closedlg");
		}
	}

	public override bool TryClose()
	{
		return base.TryClose();
	}

	public virtual bool IsInRangeOfBlock()
	{
		return (double)GameMath.Sqrt(capi.World.Player.Entity.Pos.XYZ.Add(capi.World.Player.Entity.LocalEyePos).SquareDistanceTo(entityPos)) <= (double)capi.World.Player.WorldData.PickingRange;
	}
}
