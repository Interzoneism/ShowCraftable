using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogEditcondition : GuiDialog
{
	private GuiDialogActivityCollection guiDialogActivityCollection;

	public IActionCondition actioncondition;

	public bool Saved;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogEditcondition(ICoreClientAPI capi)
		: base(capi)
	{
	}

	public GuiDialogEditcondition(ICoreClientAPI capi, GuiDialogActivityCollection guiDialogActivityCollection, IActionCondition actioncondition)
		: this(capi)
	{
		this.guiDialogActivityCollection = guiDialogActivityCollection;
		this.actioncondition = actioncondition?.Clone();
		Compose();
	}

	private void Compose()
	{
		ElementBounds elementBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds elementBounds4 = ElementBounds.Fixed(0.0, 30.0, 160.0, 25.0);
		ElementBounds elementBounds5 = ElementBounds.Fixed(0.0, 100.0, 300.0, 400.0);
		elementBounds5.verticalSizing = ElementSizing.FitToChildren;
		elementBounds5.AllowNoChildren = true;
		OrderedDictionary<string, Type> conditionTypes = ActivityModSystem.ConditionTypes;
		string[] array = conditionTypes.Keys.ToArray();
		string[] names = conditionTypes.Keys.ToArray();
		base.SingleComposer = capi.Gui.CreateCompo("editcondition", bounds).AddShadedDialogBG(elementBounds).AddDialogTitleBar("Edit condition", OnTitleBarClose)
			.BeginChildElements(elementBounds)
			.AddDropDown(array, names, array.IndexOf(actioncondition?.Type ?? ""), onSelectionChanged, elementBounds4)
			.AddSwitch(null, ElementBounds.FixedSize(20.0, 20.0).FixedUnder(elementBounds4, 10.0), "invert", 20.0)
			.AddStaticText("Invert Condition", CairoFont.WhiteDetailText(), ElementBounds.Fixed(30.0, 10.0, 200.0, 25.0).FixedUnder(elementBounds4))
			.BeginChildElements(elementBounds5);
		if (actioncondition != null)
		{
			actioncondition.AddGuiEditFields(capi, base.SingleComposer);
		}
		ElementBounds lastAddedElementBounds = base.SingleComposer.LastAddedElementBounds;
		base.SingleComposer.EndChildElements().AddSmallButton(Lang.Get("Cancel"), OnClose, elementBounds2.FixedUnder(lastAddedElementBounds, 110.0)).AddSmallButton(Lang.Get("Confirm"), OnSave, elementBounds3.FixedUnder(lastAddedElementBounds, 110.0), EnumButtonStyle.Normal, "confirm")
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetButton("confirm").Enabled = actioncondition != null;
		base.SingleComposer.GetSwitch("invert").On = actioncondition?.Invert ?? false;
	}

	private void onSelectionChanged(string code, bool selected)
	{
		OrderedDictionary<string, Type> conditionTypes = ActivityModSystem.ConditionTypes;
		actioncondition = (IActionCondition)Activator.CreateInstance(conditionTypes[code]);
		base.SingleComposer.GetButton("confirm").Enabled = actioncondition != null;
		Compose();
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	private bool OnClose()
	{
		TryClose();
		return true;
	}

	private bool OnSave()
	{
		Saved = true;
		actioncondition.Invert = base.SingleComposer.GetSwitch("invert").On;
		actioncondition.StoreGuiEditFields(capi, base.SingleComposer);
		TryClose();
		return true;
	}
}
