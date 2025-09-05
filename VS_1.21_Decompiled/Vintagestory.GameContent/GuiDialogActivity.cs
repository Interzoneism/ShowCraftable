using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogActivity : GuiDialog
{
	private GuiDialogActivityCollection guiDialogActivityCollection;

	public EntityActivity entityActivity;

	public bool Saved;

	protected ElementBounds actionsClipBounds;

	protected ElementBounds conditionsClipBounds;

	protected GuiElementCellList<IEntityAction> actionListElem;

	protected GuiElementCellList<IActionCondition> conditionsListElem;

	private int selectedActionIndex = -1;

	private int selectedConditionIndex = -1;

	private int collectionIndex;

	internal static ActivityVisualizer visualizer;

	private GuiDialogEditcondition editCondDlg;

	private GuiDialogEditAction editActionDlg;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogActivity(ICoreClientAPI capi, GuiDialogActivityCollection guiDialogActivityCollection, EntityActivitySystem vas, EntityActivity entityActivity, int collectionIndex)
		: base(capi)
	{
		if (entityActivity == null)
		{
			entityActivity = new EntityActivity();
		}
		this.guiDialogActivityCollection = guiDialogActivityCollection;
		this.entityActivity = entityActivity.Clone();
		this.entityActivity.OnLoaded(vas);
		this.collectionIndex = collectionIndex;
		Compose();
	}

	private void Compose()
	{
		//IL_042a: Unknown result type (might be due to invalid IL or missing references)
		//IL_042f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_0485: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04db: Unknown result type (might be due to invalid IL or missing references)
		//IL_052c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0531: Unknown result type (might be due to invalid IL or missing references)
		//IL_0678: Unknown result type (might be due to invalid IL or missing references)
		//IL_067d: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d3: Unknown result type (might be due to invalid IL or missing references)
		ElementBounds elementBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 40.0);
		CairoFont cairoFont = CairoFont.SmallButtonText(EnumButtonStyle.Small);
		ElementBounds elementBounds2 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 20.0, 180.0, 20.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 180.0, 25.0).FixedUnder(elementBounds2);
		ElementBounds elementBounds4 = elementBounds2.RightCopy(12.0).WithFixedWidth(103.0);
		ElementBounds elementBounds5 = elementBounds3.RightCopy(12.0).WithFixedWidth(103.0);
		ElementBounds elementBounds6 = elementBounds4.RightCopy(12.0).WithFixedWidth(60.0);
		ElementBounds elementBounds7 = elementBounds5.RightCopy(12.0).WithFixedWidth(60.0);
		ElementBounds elementBounds8 = elementBounds6.RightCopy(12.0).WithFixedWidth(40.0);
		ElementBounds elementBounds9 = elementBounds7.RightCopy(12.0).WithFixedWidth(40.0);
		ElementBounds bounds2 = elementBounds8.RightCopy(12.0).WithFixedWidth(100.0);
		ElementBounds bounds3 = elementBounds9.RightCopy(12.0).WithFixedWidth(80.0);
		ElementBounds elementBounds10 = elementBounds3.BelowCopy(0.0, 15.0);
		ElementBounds elementBounds11 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds elementBounds12 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds elementBounds13 = ElementBounds.Fixed(0.0, 0.0, 500.0, 280.0);
		actionsClipBounds = elementBounds13.ForkBoundingParent().FixedUnder(elementBounds10, -3.0);
		ElementBounds elementBounds14 = elementBounds13.FlatCopy().FixedGrow(3.0);
		ElementBounds bounds4 = elementBounds14.CopyOffsetedSibling(3.0 + elementBounds13.fixedWidth + 7.0).WithFixedWidth(20.0);
		ElementBounds elementBounds15 = elementBounds11.FlatCopy().FixedUnder(actionsClipBounds, 10.0);
		ElementBounds elementBounds16 = elementBounds15.FlatCopy();
		TextExtents textExtents = cairoFont.GetTextExtents("Delete Action");
		ElementBounds elementBounds17 = elementBounds16.WithFixedOffset(((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 16.0 + 10.0, 0.0);
		ElementBounds elementBounds18 = elementBounds15.FlatCopy().FixedRightOf(elementBounds17);
		textExtents = cairoFont.GetTextExtents("Modify Action");
		ElementBounds elementBounds19 = elementBounds18.WithFixedOffset(((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 16.0 + 10.0, 0.0);
		ElementBounds elementBounds20 = elementBounds15.FlatCopy().FixedRightOf(elementBounds19);
		textExtents = cairoFont.GetTextExtents("Add Action");
		ElementBounds elementBounds21 = elementBounds20.WithFixedOffset(((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 16.0 + 20.0, 0.0);
		ElementBounds elementBounds22 = elementBounds15.FlatCopy().FixedRightOf(elementBounds21);
		textExtents = cairoFont.GetTextExtents("M. Up");
		ElementBounds bounds5 = elementBounds22.WithFixedOffset(((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 16.0 + 2.0, 0.0);
		ElementBounds elementBounds23 = elementBounds3.FlatCopy().FixedUnder(elementBounds19, 10.0);
		ElementBounds elementBounds24 = ElementBounds.Fixed(0.0, 0.0, 500.0, 120.0);
		conditionsClipBounds = elementBounds24.ForkBoundingParent().FixedUnder(elementBounds23);
		ElementBounds elementBounds25 = elementBounds24.FlatCopy().FixedGrow(3.0);
		ElementBounds bounds6 = elementBounds25.CopyOffsetedSibling(3.0 + elementBounds24.fixedWidth + 7.0).WithFixedWidth(20.0);
		ElementBounds elementBounds26 = elementBounds11.FlatCopy().FixedUnder(conditionsClipBounds, 10.0);
		ElementBounds elementBounds27 = elementBounds26.FlatCopy();
		textExtents = cairoFont.GetTextExtents("Delete Condition");
		ElementBounds elementBounds28 = elementBounds27.WithFixedOffset(((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 16.0 + 10.0, 0.0);
		ElementBounds elementBounds29 = elementBounds26.FlatCopy().FixedRightOf(elementBounds28);
		textExtents = cairoFont.GetTextExtents("Modify Condition");
		ElementBounds bounds7 = elementBounds29.WithFixedOffset(((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 16.0 + 10.0, 0.0);
		ElementBounds elementBounds30 = ElementBounds.Fixed(0.0, 0.0, 25.0, 25.0).WithAlignment(EnumDialogArea.RightFixed).FixedUnder(conditionsClipBounds, 10.0);
		string dialogName = "activityedit-" + (guiDialogActivityCollection.assetpath?.ToShortString() ?? "new") + "-" + collectionIndex;
		base.SingleComposer = capi.Gui.CreateCompo(dialogName, bounds).AddShadedDialogBG(elementBounds).AddDialogTitleBar("Create/Modify Activity", OnTitleBarClose)
			.BeginChildElements(elementBounds)
			.AddStaticText("Activity Name", CairoFont.WhiteDetailText(), elementBounds2)
			.AddTextInput(elementBounds3, onNameChanged, CairoFont.WhiteDetailText(), "name")
			.AddStaticText("Activity Code", CairoFont.WhiteDetailText(), elementBounds4)
			.AddTextInput(elementBounds5, onCodeChanged, CairoFont.WhiteDetailText(), "code")
			.AddStaticText("Priority", CairoFont.WhiteDetailText(), elementBounds6)
			.AddNumberInput(elementBounds7, onPrioChanged, CairoFont.WhiteDetailText(), "priority")
			.AddStaticText("Slot", CairoFont.WhiteDetailText(), elementBounds8)
			.AddNumberInput(elementBounds9, onSlotChanged, CairoFont.WhiteDetailText(), "slot")
			.AddStaticText("Conditions OP", CairoFont.WhiteDetailText(), bounds2)
			.AddDropDown(new string[2] { "OR", "AND" }, new string[2] { "OR", "AND" }, (int)entityActivity.ConditionsOp, onDropOpChanged, bounds3, "opdropdown")
			.AddStaticText("Actions", CairoFont.WhiteDetailText(), elementBounds10)
			.BeginClip(actionsClipBounds)
			.AddInset(elementBounds14, 3)
			.AddCellList(elementBounds13, createActionCell, entityActivity.Actions, "actions")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarValueActions, bounds4, "actionsScrollbar")
			.AddSmallButton(Lang.Get("Delete Action"), OnDeleteAction, elementBounds15, EnumButtonStyle.Small, "deleteaction")
			.AddSmallButton(Lang.Get("Modify Action"), () => OpenActionDlg(entityActivity.Actions[selectedActionIndex]), elementBounds17, EnumButtonStyle.Small, "modifyaction")
			.AddSmallButton(Lang.Get("Add Action"), () => OpenActionDlg(null), elementBounds19, EnumButtonStyle.Small, "addaction")
			.AddSmallButton(Lang.Get("M. Up"), moveUp, elementBounds21, EnumButtonStyle.Small, "moveup")
			.AddSmallButton(Lang.Get("M. Down"), moveDown, bounds5, EnumButtonStyle.Small, "movedown")
			.AddStaticText("Conditions", CairoFont.WhiteDetailText(), elementBounds23)
			.BeginClip(conditionsClipBounds)
			.AddInset(elementBounds25, 3)
			.AddCellList(elementBounds24, createConditionCell, entityActivity.Conditions, "conditions")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarValueconditions, bounds6, "conditionsScrollbar")
			.AddSmallButton(Lang.Get("Delete condition"), OnDeletecondition, elementBounds26, EnumButtonStyle.Small, "deletecondition")
			.AddSmallButton(Lang.Get("Modify condition"), () => OpenconditionDlg(entityActivity.Conditions[selectedConditionIndex]), elementBounds28, EnumButtonStyle.Small, "modifycondition")
			.AddSmallButton(Lang.Get("Add condition"), () => OpenconditionDlg(null), bounds7, EnumButtonStyle.Small, "addcondition")
			.AddIconButton("line", OnVisualize, elementBounds30, "visualize")
			.AddSmallButton(Lang.Get("Close"), OnCancel, elementBounds11.FlatCopy().FixedUnder(elementBounds30, 40.0))
			.AddSmallButton(Lang.Get("Save"), OnSaveActivity, elementBounds12.FixedUnder(elementBounds30, 40.0), EnumButtonStyle.Normal, "create");
		base.SingleComposer.GetToggleButton("visualize").Toggleable = true;
		actionListElem = base.SingleComposer.GetCellList<IEntityAction>("actions");
		actionListElem.BeforeCalcBounds();
		actionListElem.UnscaledCellVerPadding = 0;
		actionListElem.unscaledCellSpacing = 5;
		conditionsListElem = base.SingleComposer.GetCellList<IActionCondition>("conditions");
		conditionsListElem.BeforeCalcBounds();
		conditionsListElem.UnscaledCellVerPadding = 0;
		conditionsListElem.unscaledCellSpacing = 5;
		base.SingleComposer.EndChildElements().Compose();
		updateButtonStates();
		updateScrollbarBounds();
		base.SingleComposer.GetTextInput("name").SetValue(entityActivity.Name);
		base.SingleComposer.GetTextInput("code").SetValue(entityActivity.Code);
		base.SingleComposer.GetNumberInput("priority").SetValue((float)entityActivity.Priority);
		base.SingleComposer.GetNumberInput("slot").SetValue(entityActivity.Slot.ToString() ?? "");
		base.SingleComposer.GetToggleButton("visualize").On = visualizer != null;
	}

	private bool moveDown()
	{
		if (selectedActionIndex >= entityActivity.Actions.Length - 1)
		{
			return false;
		}
		if (editActionDlg != null && editActionDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to delete, place close any currently opened action dialogs first");
			return false;
		}
		IEntityAction entityAction = entityActivity.Actions[selectedActionIndex];
		IEntityAction entityAction2 = entityActivity.Actions[selectedActionIndex + 1];
		entityActivity.Actions[selectedActionIndex] = entityAction2;
		entityActivity.Actions[selectedActionIndex + 1] = entityAction;
		actionListElem.ReloadCells(entityActivity.Actions);
		didClickActionCell(selectedActionIndex + 1);
		return true;
	}

	private bool moveUp()
	{
		if (selectedActionIndex == 0)
		{
			return false;
		}
		if (editActionDlg != null && editActionDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to delete, place close any currently opened action dialogs first");
			return false;
		}
		IEntityAction entityAction = entityActivity.Actions[selectedActionIndex];
		IEntityAction entityAction2 = entityActivity.Actions[selectedActionIndex - 1];
		entityActivity.Actions[selectedActionIndex] = entityAction2;
		entityActivity.Actions[selectedActionIndex - 1] = entityAction;
		actionListElem.ReloadCells(entityActivity.Actions);
		didClickActionCell(selectedActionIndex - 1);
		return true;
	}

	private void onDropOpChanged(string code, bool selected)
	{
		entityActivity.ConditionsOp = ((code == "AND") ? EnumConditionLogicOp.AND : EnumConditionLogicOp.OR);
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		editActionDlg?.TryClose();
		editCondDlg?.TryClose();
	}

	private void OnVisualize(bool on)
	{
		visualizer?.Dispose();
		if (on)
		{
			Entity sourceEntity = capi.World.Player.Entity;
			if (GuiDialogActivityCollections.EntityId != 0L)
			{
				sourceEntity = capi.World.GetEntityById(GuiDialogActivityCollections.EntityId) ?? capi.World.Player.Entity;
			}
			visualizer = new ActivityVisualizer(capi, entityActivity, sourceEntity);
		}
	}

	private void updateButtonStates()
	{
		base.SingleComposer.GetButton("deleteaction").Enabled = selectedActionIndex >= 0;
		base.SingleComposer.GetButton("modifyaction").Enabled = selectedActionIndex >= 0;
		base.SingleComposer.GetButton("moveup").Enabled = selectedActionIndex >= 0;
		base.SingleComposer.GetButton("movedown").Enabled = selectedActionIndex >= 0;
		base.SingleComposer.GetButton("deletecondition").Enabled = selectedConditionIndex >= 0;
		base.SingleComposer.GetButton("modifycondition").Enabled = selectedConditionIndex >= 0;
	}

	private bool OpenconditionDlg(IActionCondition condition)
	{
		editCondDlg?.TryClose();
		editCondDlg = new GuiDialogEditcondition(capi, guiDialogActivityCollection, condition);
		editCondDlg.TryOpen();
		editCondDlg.OnClosed += delegate
		{
			if (editCondDlg.Saved)
			{
				if (condition == null)
				{
					entityActivity.Conditions = entityActivity.Conditions.Append(editCondDlg.actioncondition);
				}
				else
				{
					entityActivity.Conditions[selectedConditionIndex] = editCondDlg.actioncondition;
				}
				conditionsListElem.ReloadCells(entityActivity.Conditions);
				updateScrollbarBounds();
			}
		};
		return true;
	}

	private bool OpenActionDlg(IEntityAction entityAction)
	{
		editActionDlg?.TryClose();
		editActionDlg = new GuiDialogEditAction(capi, guiDialogActivityCollection, entityAction);
		editActionDlg.TryOpen();
		editActionDlg.OnClosed += delegate
		{
			if (editActionDlg.Saved)
			{
				if (entityAction == null)
				{
					if (selectedActionIndex != -1 && selectedActionIndex < entityActivity.Actions.Length - 1)
					{
						entityActivity.Actions = entityActivity.Actions.InsertAt(editActionDlg.entityAction, selectedActionIndex + 1);
					}
					else
					{
						entityActivity.Actions = entityActivity.Actions.Append(editActionDlg.entityAction);
					}
				}
				else
				{
					entityActivity.Actions[selectedActionIndex] = editActionDlg.entityAction;
				}
				actionListElem.ReloadCells(entityActivity.Actions);
				updateScrollbarBounds();
			}
		};
		return true;
	}

	private bool OnDeletecondition()
	{
		if (editCondDlg != null && editCondDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to delete, place close any currently opened condition dialogs first");
			return false;
		}
		entityActivity.Conditions = entityActivity.Conditions.RemoveAt(selectedConditionIndex);
		selectedConditionIndex = Math.Max(0, selectedConditionIndex - 1);
		conditionsListElem.ReloadCells(entityActivity.Conditions);
		if (entityActivity.Conditions.Length != 0)
		{
			didClickconditionCell(selectedConditionIndex);
		}
		else
		{
			selectedConditionIndex = -1;
		}
		updateButtonStates();
		return true;
	}

	private bool OnDeleteAction()
	{
		if (editActionDlg != null && editActionDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to delete, place close any currently opened action dialogs first");
			return false;
		}
		entityActivity.Actions = entityActivity.Actions.RemoveAt(selectedActionIndex);
		selectedActionIndex = Math.Max(0, selectedActionIndex - 1);
		actionListElem.ReloadCells(entityActivity.Actions);
		if (entityActivity.Actions.Length != 0)
		{
			didClickActionCell(selectedActionIndex);
		}
		else
		{
			selectedActionIndex = -1;
		}
		updateButtonStates();
		return true;
	}

	private bool OnSaveActivity()
	{
		if (entityActivity.Code == null || entityActivity.Code.Length == 0)
		{
			entityActivity.Code = entityActivity.Name;
			base.SingleComposer.GetTextInput("code").SetValue(entityActivity.Code);
		}
		entityActivity.Priority = base.SingleComposer.GetNumberInput("priority").GetValue();
		int num;
		if (entityActivity.Actions.Length != 0 && entityActivity.Conditions.Length != 0 && entityActivity.Name != null)
		{
			num = ((entityActivity.Name.Length > 0) ? 1 : 0);
			if (num != 0)
			{
				goto IL_00cf;
			}
		}
		else
		{
			num = 0;
		}
		capi.TriggerIngameError(this, "missingfields", "Requires at least 1 action, 1 condition and activity name");
		goto IL_00cf;
		IL_00cf:
		if (num != 0)
		{
			collectionIndex = guiDialogActivityCollection.SaveActivity(entityActivity, collectionIndex);
		}
		return (byte)num != 0;
	}

	private void onSlotChanged(string text)
	{
		entityActivity.Slot = text.ToInt();
	}

	private void onPrioChanged(string text)
	{
		entityActivity.Priority = text.ToDouble();
	}

	private void onNameChanged(string text)
	{
		entityActivity.Name = text;
	}

	private void onCodeChanged(string text)
	{
		entityActivity.Code = text;
	}

	private IGuiElementCell createActionCell(IEntityAction action, ElementBounds bounds)
	{
		bounds.fixedPaddingY = 0.0;
		return new ActivityCellEntry(capi, bounds, entityActivity.Actions.IndexOf(action) + ". " + action.Type, action.ToString(), didClickActionCell, 150f, 350f);
	}

	private IGuiElementCell createConditionCell(IActionCondition condition, ElementBounds bounds)
	{
		bounds.fixedPaddingY = 0.0;
		return new ActivityCellEntry(capi, bounds, condition.Type, condition.ToString(), didClickconditionCell, 150f, 350f);
	}

	private void didClickconditionCell(int index)
	{
		foreach (IGuiElementCell elementCell in conditionsListElem.elementCells)
		{
			(elementCell as ActivityCellEntry).Selected = false;
		}
		selectedConditionIndex = index;
		(conditionsListElem.elementCells[index] as ActivityCellEntry).Selected = true;
		updateButtonStates();
	}

	private void didClickActionCell(int index)
	{
		foreach (IGuiElementCell elementCell in actionListElem.elementCells)
		{
			(elementCell as ActivityCellEntry).Selected = false;
		}
		selectedActionIndex = index;
		(actionListElem.elementCells[index] as ActivityCellEntry).Selected = true;
		updateButtonStates();
	}

	private bool OnCancel()
	{
		TryClose();
		return true;
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	private void updateScrollbarBounds()
	{
		if (actionListElem != null)
		{
			base.SingleComposer.GetScrollbar("actionsScrollbar")?.Bounds.CalcWorldBounds();
			base.SingleComposer.GetScrollbar("actionsScrollbar")?.SetHeights((float)actionsClipBounds.fixedHeight, (float)actionListElem.Bounds.fixedHeight);
			base.SingleComposer.GetScrollbar("conditionsScrollbar")?.Bounds.CalcWorldBounds();
			base.SingleComposer.GetScrollbar("conditionsScrollbar")?.SetHeights((float)conditionsClipBounds.fixedHeight, (float)conditionsListElem.Bounds.fixedHeight);
		}
	}

	private void OnNewScrollbarValueActions(float value)
	{
		actionListElem = base.SingleComposer.GetCellList<IEntityAction>("actions");
		actionListElem.Bounds.fixedY = 0f - value;
		actionListElem.Bounds.CalcWorldBounds();
	}

	private void OnNewScrollbarValueconditions(float value)
	{
		conditionsListElem = base.SingleComposer.GetCellList<IActionCondition>("conditions");
		conditionsListElem.Bounds.fixedY = 0f - value;
		conditionsListElem.Bounds.CalcWorldBounds();
	}
}
