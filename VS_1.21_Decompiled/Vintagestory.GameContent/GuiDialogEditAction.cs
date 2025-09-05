using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogEditAction : GuiDialog
{
	private GuiDialogActivityCollection guiDialogActivityCollection;

	public IEntityAction entityAction;

	public bool Saved;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogEditAction(ICoreClientAPI capi, GuiDialogActivityCollection guiDialogActivityCollection, IEntityAction entityAction)
		: base(capi)
	{
		this.entityAction = entityAction?.Clone();
		this.guiDialogActivityCollection = guiDialogActivityCollection;
		Compose();
	}

	private void Compose()
	{
		ElementBounds elementBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds bounds2 = ElementBounds.Fixed(0.0, 30.0, 160.0, 25.0);
		ElementBounds elementBounds4 = ElementBounds.Fixed(0.0, 70.0, 350.0, 400.0);
		elementBounds4.verticalSizing = ElementSizing.FitToChildren;
		elementBounds4.AllowNoChildren = true;
		OrderedDictionary<string, Type> actionTypes = ActivityModSystem.ActionTypes;
		string[] array = actionTypes.Keys.ToArray();
		string[] names = actionTypes.Keys.ToArray();
		base.SingleComposer = capi.Gui.CreateCompo("editaction", bounds).AddShadedDialogBG(elementBounds).AddDialogTitleBar("Edit Action", OnTitleBarClose)
			.BeginChildElements(elementBounds)
			.AddDropDown(array, names, array.IndexOf(entityAction?.Type ?? ""), onSelectionChanged, bounds2)
			.BeginChildElements(elementBounds4);
		if (entityAction != null)
		{
			entityAction.AddGuiEditFields(capi, base.SingleComposer);
		}
		ElementBounds lastAddedElementBounds = base.SingleComposer.LastAddedElementBounds;
		base.SingleComposer.EndChildElements().AddSmallButton(Lang.Get("Cancel"), OnClose, elementBounds2.FixedUnder(lastAddedElementBounds, 80.0)).AddSmallButton(Lang.Get("Confirm"), OnSave, elementBounds3.FixedUnder(lastAddedElementBounds, 80.0), EnumButtonStyle.Normal, "confirm")
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetButton("confirm").Enabled = entityAction != null;
	}

	private bool OnClose()
	{
		TryClose();
		return true;
	}

	private bool OnSave()
	{
		if (!entityAction.StoreGuiEditFields(capi, base.SingleComposer))
		{
			return true;
		}
		Saved = true;
		TryClose();
		return true;
	}

	private void onSelectionChanged(string code, bool selected)
	{
		OrderedDictionary<string, Type> actionTypes = ActivityModSystem.ActionTypes;
		entityAction = (IEntityAction)Activator.CreateInstance(actionTypes[code]);
		base.SingleComposer.GetButton("confirm").Enabled = entityAction != null;
		Compose();
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}
}
