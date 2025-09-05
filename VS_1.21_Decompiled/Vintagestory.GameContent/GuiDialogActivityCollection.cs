using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cairo;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class GuiDialogActivityCollection : GuiDialog
{
	private GuiDialogActivityCollections dlg;

	public EntityActivityCollection collection;

	private EntityActivitySystem vas;

	public AssetLocation assetpath;

	private bool isNew;

	private int selectedIndex = -1;

	protected ElementBounds clipBounds;

	protected GuiElementCellList<EntityActivity> listElem;

	private bool pause;

	private GuiDialogActivity activityDlg;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogActivityCollection(ICoreClientAPI capi, GuiDialogActivityCollections dlg, EntityActivityCollection collection, EntityActivitySystem vas, AssetLocation assetpath)
		: base(capi)
	{
		if (collection == null)
		{
			isNew = true;
			collection = new EntityActivityCollection();
		}
		this.vas = vas;
		this.assetpath = assetpath;
		this.dlg = dlg;
		this.collection = collection.Clone();
		Compose();
	}

	public GuiDialogActivityCollection(ICoreClientAPI capi)
		: base(capi)
	{
		Compose();
	}

	private void Compose()
	{
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0350: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		ElementBounds elementBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding + 150.0, 20.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 20.0, 200.0, 20.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 200.0, 25.0).FixedUnder(elementBounds2);
		ElementBounds elementBounds4 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds elementBounds5 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		double fixedHeight = 350.0;
		ElementBounds elementBounds6 = ElementBounds.Fixed(0.0, 0.0, 350.0, fixedHeight).FixedUnder(elementBounds3, 10.0);
		clipBounds = elementBounds6.ForkBoundingParent();
		ElementBounds elementBounds7 = elementBounds6.FlatCopy().FixedGrow(3.0);
		ElementBounds bounds2 = elementBounds7.CopyOffsetedSibling(3.0 + elementBounds6.fixedWidth + 7.0).WithFixedWidth(20.0);
		CairoFont cairoFont = CairoFont.SmallButtonText(EnumButtonStyle.Small);
		ElementBounds elementBounds8 = elementBounds4.FlatCopy().FixedUnder(clipBounds, 10.0);
		ElementBounds elementBounds9 = elementBounds8.FlatCopy();
		TextExtents textExtents = cairoFont.GetTextExtents("Delete Activity");
		ElementBounds elementBounds10 = elementBounds9.WithFixedOffset(((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 16.0 + 10.0, 0.0);
		ElementBounds elementBounds11 = elementBounds8.FlatCopy().FixedRightOf(elementBounds10, 3.0);
		textExtents = cairoFont.GetTextExtents("Modify Activity");
		ElementBounds bounds3 = elementBounds11.WithFixedOffset(((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 16.0 + 10.0, 0.0);
		ElementBounds elementBounds12 = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0).WithFixedPadding(4.0, 2.0).FixedUnder(elementBounds8, 35.0);
		ElementBounds elementBounds13 = ElementBounds.Fixed(0, 0).WithFixedPadding(4.0, 2.0).FixedUnder(elementBounds12);
		textExtents = cairoFont.GetTextExtents("Execute Activity");
		int num = (int)(((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 16.0);
		ElementBounds bounds4 = ElementBounds.Fixed(num, 0).WithFixedPadding(4.0, 2.0).FixedUnder(elementBounds12);
		textExtents = cairoFont.GetTextExtents("Stop actions");
		ElementBounds bounds5 = ElementBounds.Fixed(num + (int)(((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 16.0), 0).WithFixedPadding(4.0, 2.0).FixedUnder(elementBounds12);
		collection.Activities = collection.Activities.OrderByDescending((EntityActivity a) => a.Priority).ToList();
		base.SingleComposer = capi.Gui.CreateCompo("activitycollection-" + (assetpath?.ToShortString() ?? "new"), bounds).AddShadedDialogBG(elementBounds).AddDialogTitleBar("Create/Modify Activity collection", OnTitleBarClose)
			.BeginChildElements(elementBounds)
			.AddStaticText("Collection Name", CairoFont.WhiteDetailText(), elementBounds2)
			.AddTextInput(elementBounds3, onNameChanced, CairoFont.WhiteDetailText(), "name")
			.BeginClip(clipBounds)
			.AddInset(elementBounds7, 3)
			.AddCellList(elementBounds6, createCell, collection.Activities, "activities")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarValue, bounds2, "scrollbar")
			.AddSmallButton(Lang.Get("Delete Activity"), OnDeleteActivity, elementBounds8, EnumButtonStyle.Small, "deleteactivity")
			.AddSmallButton(Lang.Get("Modify Activity"), OnModifyActivity, elementBounds10, EnumButtonStyle.Small, "modifyactivity")
			.AddSmallButton(Lang.Get("Add Activity"), OnCreateActivity, bounds3, EnumButtonStyle.Small)
			.AddIf(GuiDialogActivityCollections.EntityId > 0)
			.AddStaticText("For entity with id " + GuiDialogActivityCollections.EntityId, CairoFont.WhiteDetailText(), elementBounds12)
			.AddSmallButton(Lang.Get("Execute Activity"), OnExecuteActivity, elementBounds13, EnumButtonStyle.Small, "exec")
			.AddSmallButton(Lang.Get("Stop actions"), OnStopActivity, bounds4, EnumButtonStyle.Small, "stop")
			.AddSmallButton(Lang.Get("Toggle Autorun"), OnTogglePauseActivity, bounds5, EnumButtonStyle.Small, "pause")
			.EndIf()
			.AddSmallButton(Lang.Get("Close"), OnCancel, elementBounds4 = elementBounds4.FlatCopy().FixedUnder(elementBounds13, 60.0))
			.AddSmallButton(Lang.Get("Save Edits"), OnSave, elementBounds5.FixedUnder(elementBounds13, 60.0), EnumButtonStyle.Normal, "create");
		listElem = base.SingleComposer.GetCellList<EntityActivity>("activities");
		listElem.BeforeCalcBounds();
		listElem.UnscaledCellVerPadding = 0;
		listElem.unscaledCellSpacing = 5;
		base.SingleComposer.EndChildElements().Compose();
		base.SingleComposer.GetTextInput("name").SetValue(collection.Name);
		updateScrollbarBounds();
		updateButtonStates();
	}

	private bool OnStopActivity()
	{
		capi.SendChatMessage("/dev aee e[id=" + GuiDialogActivityCollections.EntityId + "] stop");
		return true;
	}

	private bool OnTogglePauseActivity()
	{
		pause = !pause;
		capi.SendChatMessage("/dev aee e[id=" + GuiDialogActivityCollections.EntityId + "] pause " + pause);
		return true;
	}

	private bool OnExecuteActivity()
	{
		capi.SendChatMessage("/dev aee e[id=" + GuiDialogActivityCollections.EntityId + "] runa " + collection.Activities[selectedIndex].Code);
		return true;
	}

	public int SaveActivity(EntityActivity activity, int index)
	{
		if (index >= collection.Activities.Count)
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to save, out of index bounds");
			return -1;
		}
		if (index < 0)
		{
			collection.Activities.Add(activity);
		}
		else
		{
			collection.Activities[index] = activity;
		}
		collection.Activities = collection.Activities.OrderByDescending((EntityActivity a) => a.Priority).ToList();
		listElem.ReloadCells(collection.Activities);
		OnSave();
		if (index >= 0)
		{
			return index;
		}
		return collection.Activities.Count - 1;
	}

	private void updateButtonStates()
	{
		base.SingleComposer.GetButton("deleteactivity").Enabled = selectedIndex >= 0;
		base.SingleComposer.GetButton("modifyactivity").Enabled = selectedIndex >= 0;
		if (base.SingleComposer.GetButton("exec") != null)
		{
			base.SingleComposer.GetButton("exec").Enabled = GuiDialogActivityCollections.EntityId != 0L && selectedIndex >= 0;
		}
	}

	private bool OnDeleteActivity()
	{
		if (activityDlg != null && activityDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to delete, place close any currently opened activity dialogs first");
			return false;
		}
		if (selectedIndex < 0)
		{
			return true;
		}
		collection.Activities.RemoveAt(selectedIndex);
		listElem.ReloadCells(collection.Activities);
		return true;
	}

	private bool OnModifyActivity()
	{
		if (selectedIndex < 0)
		{
			return true;
		}
		if (activityDlg != null && activityDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to modify. Close any currently opened activity dialogs first");
			return false;
		}
		activityDlg = new GuiDialogActivity(capi, this, vas, collection.Activities[selectedIndex], selectedIndex);
		activityDlg.TryOpen();
		activityDlg.OnClosed += delegate
		{
			activityDlg = null;
		};
		return true;
	}

	private bool OnSave()
	{
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		collection.Name = base.SingleComposer.GetTextInput("name").GetText();
		if (collection.Name == null || collection.Name.Length == 0 || collection.Activities.Count == 0)
		{
			capi.TriggerIngameError(this, "missingfields", "Requires at least one activity and a name");
			return false;
		}
		string path = ((!isNew) ? Path.Combine(GamePaths.AssetsPath, "survival", assetpath.Path) : Path.Combine(GamePaths.AssetsPath, "survival", "config", "activitycollections", GamePaths.ReplaceInvalidChars(collection.Name) + ".json"));
		JsonSerializerSettings val = new JsonSerializerSettings
		{
			TypeNameHandling = (TypeNameHandling)3
		};
		string text = JsonConvert.SerializeObject((object)collection, (Formatting)1, val);
		File.WriteAllText(path, text);
		dlg.ReloadCells();
		capi.Network.GetChannel("activityEditor").SendPacket(new ActivityCollectionsJsonPacket
		{
			Collections = new List<string> { text }
		});
		if (GuiDialogActivityCollections.AutoApply && GuiDialogActivityCollections.EntityId != 0L)
		{
			capi.SendChatMessage("/dev aee e[id=" + GuiDialogActivityCollections.EntityId + "] stop");
			dlg.ApplyToEntityId();
		}
		return true;
	}

	private void onNameChanced(string name)
	{
		collection.Name = name;
	}

	private bool OnCreateActivity()
	{
		activityDlg = new GuiDialogActivity(capi, this, vas, null, -1);
		activityDlg.TryOpen();
		activityDlg.OnClosed += delegate
		{
			activityDlg = null;
		};
		return true;
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

	private IGuiElementCell createCell(EntityActivity collection, ElementBounds bounds)
	{
		bounds.fixedPaddingY = 0.0;
		return new ActivityCellEntry(capi, bounds, "P" + Math.Round(collection.Priority, 2) + "  " + collection.Name, collection.Actions.Length + " actions, " + collection.Conditions.Length + " conds", didClickCell, 210f, 200f);
	}

	private void didClickCell(int index)
	{
		foreach (IGuiElementCell elementCell in listElem.elementCells)
		{
			(elementCell as ActivityCellEntry).Selected = false;
		}
		selectedIndex = index;
		(listElem.elementCells[index] as ActivityCellEntry).Selected = true;
		updateButtonStates();
	}

	private void updateScrollbarBounds()
	{
		if (listElem != null)
		{
			base.SingleComposer.GetScrollbar("scrollbar")?.Bounds.CalcWorldBounds();
			base.SingleComposer.GetScrollbar("scrollbar")?.SetHeights((float)clipBounds.fixedHeight, (float)listElem.Bounds.fixedHeight);
		}
	}

	private void OnNewScrollbarValue(float value)
	{
		listElem = base.SingleComposer.GetCellList<EntityActivity>("activities");
		listElem.Bounds.fixedY = 0f - value;
		listElem.Bounds.CalcWorldBounds();
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		activityDlg?.TryClose();
		activityDlg = null;
	}
}
