using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public abstract class GuiDialogBlockEntity : GuiDialogGeneric
{
	public bool IsDuplicate { get; }

	public InventoryBase Inventory { get; }

	public BlockPos BlockEntityPosition { get; }

	public virtual AssetLocation OpenSound { get; set; }

	public virtual AssetLocation CloseSound { get; set; }

	protected virtual double FloatyDialogPosition => 0.75;

	protected virtual double FloatyDialogAlign => 0.75;

	public override bool PrefersUngrabbedMouse => false;

	public GuiDialogBlockEntity(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi)
		: base(dialogTitle, capi)
	{
		IsDuplicate = capi.World.Player.InventoryManager.Inventories.ContainsValue(inventory);
		if (!IsDuplicate)
		{
			Inventory = inventory;
			BlockEntityPosition = blockEntityPos;
		}
	}

	public GuiDialogBlockEntity(string dialogTitle, BlockPos blockEntityPos, ICoreClientAPI capi)
		: base(dialogTitle, capi)
	{
		IsDuplicate = capi.OpenedGuis.FirstOrDefault((object dlg) => (dlg as GuiDialogBlockEntity)?.BlockEntityPosition == blockEntityPos) != null;
		if (!IsDuplicate)
		{
			BlockEntityPosition = blockEntityPos;
		}
	}

	public override void OnFinalizeFrame(float dt)
	{
		base.OnFinalizeFrame(dt);
		if (!IsInRangeOfBlock(BlockEntityPosition))
		{
			capi.Event.EnqueueMainThreadTask(delegate
			{
				TryClose();
			}, "closedlg");
		}
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (capi.Settings.Bool["immersiveMouseMode"])
		{
			Vec3d vec3d = MatrixToolsd.Project(new Vec3d((double)BlockEntityPosition.X + 0.5, (double)BlockEntityPosition.Y + FloatyDialogPosition, (double)BlockEntityPosition.Z + 0.5), capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
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

	protected void DoSendPacket(object p)
	{
		capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.InternalY, BlockEntityPosition.Z, p);
	}

	protected void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = base.SingleComposer.GetSlotGrid("slotgrid").Bounds;
		bounds.fixedY = 10.0 - GuiElementItemSlotGridBase.unscaledSlotPadding - (double)value;
		bounds.CalcWorldBounds();
	}

	protected void CloseIconPressed()
	{
		TryClose();
	}

	public override void OnGuiOpened()
	{
		if (Inventory != null)
		{
			capi.World.Player.InventoryManager.OpenInventory(Inventory);
		}
		capi.Gui.PlaySound(OpenSound, randomizePitch: true);
	}

	public override bool TryOpen()
	{
		if (IsDuplicate)
		{
			return false;
		}
		return base.TryOpen();
	}

	public override void OnGuiClosed()
	{
		if (Inventory != null)
		{
			capi.World.Player.InventoryManager.CloseInventoryAndSync(Inventory);
		}
		capi.Network.SendBlockEntityPacket(BlockEntityPosition, 1001);
		capi.Gui.PlaySound(CloseSound, randomizePitch: true);
	}

	public void ReloadValues()
	{
	}
}
