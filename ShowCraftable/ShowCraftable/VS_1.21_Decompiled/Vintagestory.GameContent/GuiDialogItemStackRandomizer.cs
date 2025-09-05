using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class GuiDialogItemStackRandomizer : GuiDialogGeneric
{
	private bool save;

	public override string ToggleKeyCombinationCode => null;

	public override ITreeAttribute Attributes
	{
		get
		{
			GuiElementNumberInput numberInput = base.SingleComposer.GetNumberInput("chance");
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute.SetInt("save", save ? 1 : 0);
			treeAttribute.SetFloat("totalChance", numberInput.GetValue() / 100f);
			return treeAttribute;
		}
	}

	public GuiDialogItemStackRandomizer(float totalChance, ICoreClientAPI capi)
		: base("Item Stack Randomizer", capi)
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 70.0, 60.0, 30.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(10.0, 1.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(10.0, 1.0);
		ElementBounds elementBounds4 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds4.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		base.SingleComposer = capi.Gui.CreateCompo("itemstackrandomizer", bounds).AddShadedDialogBG(elementBounds4).AddDialogTitleBar("Item Stack Randomizer", OnTitleBarClose)
			.BeginChildElements(elementBounds4)
			.AddStaticText("Chance for any loot to appear:", CairoFont.WhiteDetailText(), ElementBounds.Fixed(0.0, 30.0, 250.0, 30.0))
			.AddNumberInput(elementBounds = elementBounds.FlatCopy(), null, CairoFont.WhiteDetailText(), "chance")
			.AddButton("Close", OnCloseClicked, elementBounds2.FixedUnder(elementBounds, 25.0))
			.AddButton("Save", OnSaveClicked, elementBounds3.FixedUnder(elementBounds, 25.0))
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetNumberInput("chance").SetValue((totalChance * 100f).ToString() ?? "");
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

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public void ReloadValues()
	{
	}
}
