using System.Collections.Generic;
using Cairo;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogActivityCollections : GuiDialog
{
	public OrderedDictionary<AssetLocation, EntityActivityCollection> collections = new OrderedDictionary<AssetLocation, EntityActivityCollection>();

	protected ElementBounds clipBounds;

	protected GuiElementCellList<EntityActivityCollection> listElem;

	private int selectedIndex = -1;

	private EntityActivitySystem vas;

	public static long EntityId;

	public static bool AutoApply = true;

	private GuiDialogActivityCollection editDlg;

	private GuiDialogActivityCollection createDlg;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogActivityCollections(ICoreClientAPI capi)
		: base(capi)
	{
		vas = new EntityActivitySystem(capi.World.Player.Entity);
		Compose();
	}

	private void Compose()
	{
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		ElementBounds elementBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		double fixedHeight = 400.0;
		ElementBounds elementBounds4 = ElementBounds.Fixed(0.0, 25.0, 270.0, fixedHeight);
		clipBounds = elementBounds4.ForkBoundingParent();
		ElementBounds elementBounds5 = elementBounds4.FlatCopy().FixedGrow(3.0).WithFixedOffset(0.0, 0.0);
		ElementBounds bounds2 = elementBounds5.CopyOffsetedSibling(3.0 + elementBounds4.fixedWidth + 7.0).WithFixedWidth(20.0);
		CairoFont cairoFont = CairoFont.SmallButtonText(EnumButtonStyle.Small);
		ElementBounds bounds3 = elementBounds2.FlatCopy().FixedUnder(clipBounds, 10.0);
		ElementBounds elementBounds6 = ElementBounds.FixedSize(90.0, 21.0).WithAlignment(EnumDialogArea.RightFixed).FixedUnder(clipBounds, 10.0);
		TextExtents textExtents = cairoFont.GetTextExtents("Modify Activity");
		double num = ((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 20.0;
		base.SingleComposer = capi.Gui.CreateCompo("activitycollections", bounds).AddShadedDialogBG(elementBounds).AddDialogTitleBar("Activity collections", OnTitleBarClose)
			.BeginChildElements(elementBounds)
			.BeginClip(clipBounds)
			.AddInset(elementBounds5, 3)
			.AddCellList(elementBounds4, createCell, collections.Values, "collections")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarValue, bounds2, "scrollbar")
			.AddSmallButton(Lang.Get("Modify"), OnModifyCollection, bounds3, EnumButtonStyle.Small, "modifycollection")
			.AddTextInput(elementBounds6.FlatCopy().WithFixedOffset(0.0 - num, 2.0), null, CairoFont.WhiteDetailText(), "entityid")
			.AddSmallButton(Lang.Get("Apply to entity"), ApplyToEntityId, elementBounds6.FlatCopy().WithFixedPadding(4.0, 1.0), EnumButtonStyle.Small, "applytoentityid")
			.AddSwitch(onAutoApply, elementBounds6.BelowCopy(0.0, 7.0).WithFixedOffset(0.0 - num - 69.0, 0.0), "autocopy", 20.0)
			.AddStaticText("Autoapply modifications", CairoFont.WhiteDetailText().WithFontSize(14f), elementBounds6.BelowCopy(0.0, 9.0).WithFixedOffset(0.0, 0.0).WithFixedWidth(178.0))
			.AddSmallButton(Lang.Get("Close"), OnClose, elementBounds2.FixedUnder(clipBounds, 80.0))
			.AddIconButton("line", CairoFont.WhiteMediumText().WithColor(GuiStyle.ErrorTextColor), clearVisualize, elementBounds2.RightCopy(50.0, 1.0).WithFixedSize(22.0, 22.0).WithFixedPadding(3.0, 3.0), "clearvisualize")
			.AddSmallButton(Lang.Get("Create collection"), OnCreateCollection, elementBounds3.FixedUnder(clipBounds, 80.0), EnumButtonStyle.Normal, "create");
		if (EntityId != 0L)
		{
			base.SingleComposer.GetTextInput("entityid").SetValue(EntityId);
		}
		else
		{
			base.SingleComposer.GetTextInput("entityid").SetPlaceHolderText("entity id");
		}
		base.SingleComposer.GetSwitch("autocopy").On = AutoApply;
		listElem = base.SingleComposer.GetCellList<EntityActivityCollection>("collections");
		listElem.BeforeCalcBounds();
		listElem.UnscaledCellVerPadding = 0;
		listElem.unscaledCellSpacing = 5;
		base.SingleComposer.EndChildElements().Compose();
		ReloadCells();
		updateScrollbarBounds();
		updateButtonStates();
	}

	private void clearVisualize(bool on)
	{
		GuiDialogActivity.visualizer?.Dispose();
		GuiDialogActivity.visualizer = null;
	}

	private void onAutoApply(bool on)
	{
		AutoApply = on;
	}

	public bool ApplyToEntityId()
	{
		if (selectedIndex < 0)
		{
			return true;
		}
		capi.Network.GetChannel("activityEditor").SendPacket(new ApplyConfigPacket
		{
			ActivityCollectionName = collections.GetValueAtIndex(selectedIndex).Name,
			EntityId = (EntityId = base.SingleComposer.GetTextInput("entityid").GetText().ToInt())
		});
		return true;
	}

	private void updateButtonStates()
	{
		base.SingleComposer.GetButton("modifycollection").Enabled = selectedIndex >= 0;
		base.SingleComposer.GetButton("applytoentityid").Enabled = selectedIndex >= 0;
	}

	private bool OnModifyCollection()
	{
		if (selectedIndex < 0)
		{
			return true;
		}
		AssetLocation keyAtIndex = collections.GetKeyAtIndex(selectedIndex);
		if (EntityId > 0)
		{
			Entity entityById = capi.World.GetEntityById(EntityId);
			if (entityById != null)
			{
				vas.ActivityOffset = entityById.WatchedAttributes.GetBlockPos("importOffset", new BlockPos(entityById.Pos.Dimension));
			}
		}
		editDlg = new GuiDialogActivityCollection(capi, this, collections[keyAtIndex], vas, keyAtIndex);
		editDlg.TryOpen();
		return true;
	}

	public void ReloadCells()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			TypeNameHandling = (TypeNameHandling)3
		};
		capi.Assets.Reload(AssetCategory.config);
		List<IAsset> many = capi.Assets.GetMany("config/activitycollections/");
		collections.Clear();
		foreach (IAsset item in many)
		{
			EntityActivityCollection entityActivityCollection = (collections[item.Location] = item.ToObject<EntityActivityCollection>(settings));
			entityActivityCollection.OnLoaded(vas);
		}
		listElem.ReloadCells(collections.Values);
	}

	private bool OnCreateCollection()
	{
		if (createDlg != null && createDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "alreadyopened", Lang.Get("Close the other activity collection dialog first"));
			return false;
		}
		createDlg = new GuiDialogActivityCollection(capi, this, null, vas, null);
		createDlg.TryOpen();
		return true;
	}

	private bool OnClose()
	{
		TryClose();
		return true;
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	private IGuiElementCell createCell(EntityActivityCollection collection, ElementBounds bounds)
	{
		bounds.fixedPaddingY = 0.0;
		return new ActivityCellEntry(capi, bounds, collection.Name, collection.Activities.Count + " activities", didClickCell, 160f, 200f);
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
		listElem = base.SingleComposer.GetCellList<EntityActivityCollection>("collections");
		listElem.Bounds.fixedY = 0f - value;
		listElem.Bounds.CalcWorldBounds();
	}
}
