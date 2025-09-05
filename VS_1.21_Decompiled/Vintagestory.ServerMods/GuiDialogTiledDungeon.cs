using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace Vintagestory.ServerMods;

public class GuiDialogTiledDungeon : GuiDialogGeneric
{
	private bool save;

	public override ITreeAttribute Attributes
	{
		get
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute.SetInt("save", save ? 1 : 0);
			GuiElementTextInput textInput = base.SingleComposer.GetTextInput("constraints");
			treeAttribute.SetString("constraints", textInput.GetText());
			return treeAttribute;
		}
	}

	public GuiDialogTiledDungeon(string dialogTitle, string constraint, ICoreClientAPI capi)
		: base(dialogTitle, capi)
	{
		double unscaledSlotPadding = GuiElementItemSlotGridBase.unscaledSlotPadding;
		ElementBounds elementBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, unscaledSlotPadding, 45.0 + unscaledSlotPadding, 10, 1).FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding);
		ElementBounds refBounds = ElementBounds.Fixed(3.0, 0.0, 48.0, 30.0).FixedUnder(elementBounds, -4.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(10.0, 1.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(10.0, 1.0);
		ElementBounds elementBounds4 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds4.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		base.SingleComposer = capi.Gui.CreateCompo("tiledungeon", bounds).AddShadedDialogBG(elementBounds4).AddDialogTitleBar(dialogTitle, OnTitleBarClose)
			.BeginChildElements(elementBounds4)
			.AddTextInput(elementBounds, OnTextChanged, CairoFont.TextInput(), "constraints")
			.AddButton("Close", OnCloseClicked, elementBounds2.FixedUnder(refBounds, 25.0))
			.AddButton("Save", OnSaveClicked, elementBounds3.FixedUnder(refBounds, 25.0))
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetTextInput("constraints").SetValue(constraint);
	}

	private void OnTextChanged(string obj)
	{
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	private bool OnSaveClicked()
	{
		save = true;
		TryClose();
		return true;
	}

	private bool OnCloseClicked()
	{
		TryClose();
		return true;
	}
}
