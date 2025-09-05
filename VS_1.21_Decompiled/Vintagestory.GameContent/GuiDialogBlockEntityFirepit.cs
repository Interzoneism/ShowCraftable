using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogBlockEntityFirepit : GuiDialogBlockEntity
{
	private bool haveCookingContainer;

	private string currentOutputText;

	private ElementBounds cookingSlotsSlotBounds;

	private long lastRedrawMs;

	private EnumPosFlag screenPos;

	protected override double FloatyDialogPosition => 0.6;

	protected override double FloatyDialogAlign => 0.8;

	public override double DrawOrder => 0.2;

	public GuiDialogBlockEntityFirepit(string dlgTitle, InventoryBase Inventory, BlockPos bePos, SyncedTreeAttribute tree, ICoreClientAPI capi)
		: base(dlgTitle, Inventory, bePos, capi)
	{
		if (!base.IsDuplicate)
		{
			tree.OnModified.Add(new TreeModifiedListener
			{
				listener = OnAttributesModified
			});
			Attributes = tree;
		}
	}

	private void OnInventorySlotModified(int slotid)
	{
		capi.Event.EnqueueMainThreadTask(SetupDialog, "setupfirepitdlg");
	}

	private void SetupDialog()
	{
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0606: Unknown result type (might be due to invalid IL or missing references)
		//IL_060b: Unknown result type (might be due to invalid IL or missing references)
		ItemSlot itemSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
		if (itemSlot != null && itemSlot.Inventory?.InventoryID != base.Inventory?.InventoryID)
		{
			itemSlot = null;
		}
		string text = Attributes.GetString("outputText", "");
		bool flag = Attributes.GetInt("haveCookingContainer") > 0;
		FontExtents fontExtents;
		GuiElementDynamicText dynamicText;
		if (haveCookingContainer == flag && base.SingleComposer != null)
		{
			dynamicText = base.SingleComposer.GetDynamicText("outputText");
			dynamicText.Font.WithFontSize(14f);
			dynamicText.SetNewText(text, autoHeight: true);
			base.SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
			haveCookingContainer = flag;
			currentOutputText = text;
			dynamicText.Bounds.fixedOffsetY = 0.0;
			if (dynamicText.QuantityTextLines > 2)
			{
				ElementBounds bounds = dynamicText.Bounds;
				fontExtents = dynamicText.Font.GetFontExtents();
				bounds.fixedOffsetY = (0.0 - ((FontExtents)(ref fontExtents)).Height) / (double)RuntimeEnv.GUIScale * 0.65;
				dynamicText.Font.WithFontSize(12f);
				dynamicText.RecomposeText();
			}
			dynamicText.Bounds.CalcWorldBounds();
			return;
		}
		haveCookingContainer = flag;
		currentOutputText = text;
		int num = Attributes.GetInt("quantityCookingSlots");
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 210.0, 250.0);
		cookingSlotsSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 75.0, 4, num / 4);
		cookingSlotsSlotBounds.fixedHeight += 10.0;
		double num2 = cookingSlotsSlotBounds.fixedHeight + cookingSlotsSlotBounds.fixedY;
		ElementBounds elementBounds2 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, num2, 1, 1);
		ElementBounds elementBounds3 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 110.0 + num2, 1, 1);
		ElementBounds bounds2 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 153.0, num2, 1, 1);
		ElementBounds elementBounds4 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds4.BothSizing = ElementSizing.FitToChildren;
		elementBounds4.WithChildren(elementBounds);
		ElementBounds elementBounds5 = ElementStdBounds.AutosizedMainDialog.WithFixedAlignmentOffset(IsRight(screenPos) ? (0.0 - GuiStyle.DialogToScreenPadding) : GuiStyle.DialogToScreenPadding, 0.0).WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);
		if (!capi.Settings.Bool["immersiveMouseMode"])
		{
			elementBounds5.fixedOffsetY += (elementBounds.fixedHeight + 65.0 + (double)(haveCookingContainer ? 25 : 0)) * (double)YOffsetMul(screenPos);
			elementBounds5.fixedOffsetX += (elementBounds.fixedWidth + 10.0) * (double)XOffsetMul(screenPos);
		}
		int[] array = new int[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = 3 + i;
		}
		base.SingleComposer = capi.Gui.CreateCompo("blockentitystove" + base.BlockEntityPosition, elementBounds5).AddShadedDialogBG(elementBounds4).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
			.BeginChildElements(elementBounds4)
			.AddDynamicCustomDraw(elementBounds, OnBgDraw, "symbolDrawer")
			.AddDynamicText("", CairoFont.WhiteDetailText(), ElementBounds.Fixed(0.0, 30.0, 210.0, 45.0), "outputText")
			.AddIf(haveCookingContainer)
			.AddItemSlotGrid(base.Inventory, SendInvPacket, 4, array, cookingSlotsSlotBounds, "ingredientSlots")
			.EndIf()
			.AddItemSlotGrid(base.Inventory, SendInvPacket, 1, new int[1], elementBounds3, "fuelslot")
			.AddDynamicText("", CairoFont.WhiteDetailText(), elementBounds3.RightCopy(17.0, 16.0).WithFixedSize(60.0, 30.0), "fueltemp")
			.AddItemSlotGrid(base.Inventory, SendInvPacket, 1, new int[1] { 1 }, elementBounds2, "oreslot")
			.AddDynamicText("", CairoFont.WhiteDetailText(), elementBounds2.RightCopy(23.0, 16.0).WithFixedSize(60.0, 30.0), "oretemp")
			.AddItemSlotGrid(base.Inventory, SendInvPacket, 1, new int[1] { 2 }, bounds2, "outputslot")
			.EndChildElements()
			.Compose();
		lastRedrawMs = capi.ElapsedMilliseconds;
		if (itemSlot != null)
		{
			base.SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
		}
		dynamicText = base.SingleComposer.GetDynamicText("outputText");
		dynamicText.SetNewText(currentOutputText, autoHeight: true);
		dynamicText.Bounds.fixedOffsetY = 0.0;
		if (dynamicText.QuantityTextLines > 2)
		{
			ElementBounds bounds3 = dynamicText.Bounds;
			fontExtents = dynamicText.Font.GetFontExtents();
			bounds3.fixedOffsetY = (0.0 - ((FontExtents)(ref fontExtents)).Height) / (double)RuntimeEnv.GUIScale * 0.65;
			dynamicText.Font.WithFontSize(12f);
			dynamicText.RecomposeText();
		}
		dynamicText.Bounds.CalcWorldBounds();
	}

	private void OnAttributesModified()
	{
		if (!IsOpened())
		{
			return;
		}
		float num = Attributes.GetFloat("furnaceTemperature");
		float num2 = Attributes.GetFloat("oreTemperature");
		string text = num.ToString("#");
		string text2 = num2.ToString("#");
		text += ((text.Length > 0) ? "°C" : "");
		text2 += ((text2.Length > 0) ? "°C" : "");
		if (num > 0f && num <= 20f)
		{
			text = Lang.Get("Cold");
		}
		if (num2 > 0f && num2 <= 20f)
		{
			text2 = Lang.Get("Cold");
		}
		base.SingleComposer.GetDynamicText("fueltemp").SetNewText(text);
		base.SingleComposer.GetDynamicText("oretemp").SetNewText(text2);
		if (capi.ElapsedMilliseconds - lastRedrawMs > 500)
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
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Expected O, but got Unknown
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Expected O, but got Unknown
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		double num = cookingSlotsSlotBounds.fixedHeight + cookingSlotsSlotBounds.fixedY;
		ctx.Save();
		Matrix matrix = ctx.Matrix;
		matrix.Translate(GuiElement.scaled(5.0), GuiElement.scaled(53.0 + num));
		matrix.Scale(GuiElement.scaled(0.25), GuiElement.scaled(0.25));
		ctx.Matrix = matrix;
		capi.Gui.Icons.DrawFlame(ctx);
		double num2 = 210f - 210f * (Attributes.GetFloat("fuelBurnTime") / Attributes.GetFloat("maxFuelBurnTime", 1f));
		ctx.Rectangle(0.0, num2, 200.0, 210.0 - num2);
		ctx.Clip();
		LinearGradient val = new LinearGradient(0.0, GuiElement.scaled(250.0), 0.0, 0.0);
		((Gradient)val).AddColorStop(0.0, new Color(1.0, 1.0, 0.0, 1.0));
		((Gradient)val).AddColorStop(1.0, new Color(1.0, 0.0, 0.0, 1.0));
		ctx.SetSource((Pattern)(object)val);
		capi.Gui.Icons.DrawFlame(ctx, 0.0, strokeOrFill: false, defaultPattern: false);
		((Pattern)val).Dispose();
		ctx.Restore();
		ctx.Save();
		matrix = ctx.Matrix;
		matrix.Translate(GuiElement.scaled(63.0), GuiElement.scaled(num + 2.0));
		matrix.Scale(GuiElement.scaled(0.6), GuiElement.scaled(0.6));
		ctx.Matrix = matrix;
		capi.Gui.Icons.DrawArrowRight(ctx, 2.0);
		double num3 = Attributes.GetFloat("oreCookingTime") / Attributes.GetFloat("maxOreCookingTime", 1f);
		ctx.Rectangle(5.0, 0.0, 125.0 * num3, 100.0);
		ctx.Clip();
		val = new LinearGradient(0.0, 0.0, 200.0, 0.0);
		((Gradient)val).AddColorStop(0.0, new Color(0.0, 0.4, 0.0, 1.0));
		((Gradient)val).AddColorStop(1.0, new Color(0.2, 0.6, 0.2, 1.0));
		ctx.SetSource((Pattern)(object)val);
		capi.Gui.Icons.DrawArrowRight(ctx, 0.0, strokeOrFill: false, defaultPattern: false);
		((Pattern)val).Dispose();
		ctx.Restore();
	}

	private void SendInvPacket(object packet)
	{
		capi.Network.SendBlockEntityPacket(base.BlockEntityPosition.X, base.BlockEntityPosition.Y, base.BlockEntityPosition.Z, packet);
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		base.Inventory.SlotModified += OnInventorySlotModified;
		screenPos = GetFreePos("smallblockgui");
		OccupyPos("smallblockgui", screenPos);
		SetupDialog();
	}

	public override void OnGuiClosed()
	{
		base.Inventory.SlotModified -= OnInventorySlotModified;
		base.SingleComposer.GetSlotGrid("fuelslot").OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("oreslot").OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("outputslot").OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("ingredientSlots")?.OnGuiClosed(capi);
		base.OnGuiClosed();
		FreePos("smallblockgui", screenPos);
	}
}
