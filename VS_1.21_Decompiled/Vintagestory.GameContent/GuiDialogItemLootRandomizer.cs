using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class GuiDialogItemLootRandomizer : GuiDialogGeneric
{
	private InventoryBase inv;

	private bool save;

	private bool updating;

	public override string ToggleKeyCombinationCode => null;

	public override ITreeAttribute Attributes
	{
		get
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute.SetInt("save", save ? 1 : 0);
			int num = 0;
			for (int i = 0; i < 10; i++)
			{
				ItemStack itemstack = inv[i].Itemstack;
				if (itemstack != null)
				{
					GuiElementNumberInput numberInput = base.SingleComposer.GetNumberInput("chance" + (i + 1));
					TreeAttribute treeAttribute2 = new TreeAttribute();
					treeAttribute2.SetItemstack("stack", itemstack.Clone());
					treeAttribute2.SetFloat("chance", numberInput.GetValue());
					treeAttribute["stack" + num++] = treeAttribute2;
				}
			}
			return treeAttribute;
		}
	}

	public GuiDialogItemLootRandomizer(InventoryBase inv, float[] chances, ICoreClientAPI capi, string title = "Item Loot Randomizer")
		: base(title, capi)
	{
		this.inv = inv;
		createDialog(chances, title);
	}

	public GuiDialogItemLootRandomizer(ItemStack[] stacks, float[] chances, ICoreClientAPI capi, string title = "Item Loot Randomizer")
		: base(title, capi)
	{
		inv = new InventoryGeneric(10, "lootrandomizer-1", capi);
		for (int i = 0; i < 10; i++)
		{
			inv[i].Itemstack = stacks[i];
		}
		createDialog(chances, title);
	}

	private void createDialog(float[] chances, string title)
	{
		double unscaledSlotPadding = GuiElementItemSlotGridBase.unscaledSlotPadding;
		ElementBounds elementBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, unscaledSlotPadding, 45.0 + unscaledSlotPadding, 10, 1).FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding);
		ElementBounds elementBounds2 = ElementBounds.Fixed(3.0, 0.0, 48.0, 30.0).FixedUnder(elementBounds, -4.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(10.0, 1.0);
		ElementBounds elementBounds4 = ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 0.0, 150.0, 30.0).WithFixedPadding(10.0, 1.0);
		ElementBounds elementBounds5 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(10.0, 1.0);
		ElementBounds elementBounds6 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds6.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		float num = chances.Sum();
		string text = "Total chance: " + (int)num + "%";
		base.SingleComposer = capi.Gui.CreateCompo("itemlootrandomizer", bounds).AddShadedDialogBG(elementBounds6).AddDialogTitleBar(title, OnTitleBarClose)
			.BeginChildElements(elementBounds6)
			.AddItemSlotGrid(inv, SendInvPacket, 10, elementBounds, "slots")
			.AddNumberInput(elementBounds2 = elementBounds2.FlatCopy(), delegate
			{
				OnTextChanced(0);
			}, CairoFont.WhiteDetailText(), "chance1")
			.AddNumberInput(elementBounds2 = elementBounds2.RightCopy(3.0), delegate
			{
				OnTextChanced(1);
			}, CairoFont.WhiteDetailText(), "chance2")
			.AddNumberInput(elementBounds2 = elementBounds2.RightCopy(3.0), delegate
			{
				OnTextChanced(2);
			}, CairoFont.WhiteDetailText(), "chance3")
			.AddNumberInput(elementBounds2 = elementBounds2.RightCopy(3.0), delegate
			{
				OnTextChanced(3);
			}, CairoFont.WhiteDetailText(), "chance4")
			.AddNumberInput(elementBounds2 = elementBounds2.RightCopy(3.0), delegate
			{
				OnTextChanced(4);
			}, CairoFont.WhiteDetailText(), "chance5")
			.AddNumberInput(elementBounds2 = elementBounds2.RightCopy(3.0), delegate
			{
				OnTextChanced(5);
			}, CairoFont.WhiteDetailText(), "chance6")
			.AddNumberInput(elementBounds2 = elementBounds2.RightCopy(3.0), delegate
			{
				OnTextChanced(6);
			}, CairoFont.WhiteDetailText(), "chance7")
			.AddNumberInput(elementBounds2 = elementBounds2.RightCopy(3.0), delegate
			{
				OnTextChanced(7);
			}, CairoFont.WhiteDetailText(), "chance8")
			.AddNumberInput(elementBounds2 = elementBounds2.RightCopy(3.0), delegate
			{
				OnTextChanced(8);
			}, CairoFont.WhiteDetailText(), "chance9")
			.AddNumberInput(elementBounds2 = elementBounds2.RightCopy(3.0), delegate
			{
				OnTextChanced(9);
			}, CairoFont.WhiteDetailText(), "chance10")
			.AddButton("Close", OnCloseClicked, elementBounds3.FixedUnder(elementBounds2, 25.0))
			.AddDynamicText(text, CairoFont.WhiteDetailText(), elementBounds4.FixedUnder(elementBounds2, 25.0), "totalchance")
			.AddButton("Save", OnSaveClicked, elementBounds5.FixedUnder(elementBounds2, 25.0))
			.EndChildElements()
			.Compose();
		for (int num2 = 0; num2 < 10; num2++)
		{
			base.SingleComposer.GetNumberInput("chance" + (num2 + 1)).SetValue(chances[num2].ToString() ?? "");
		}
		base.SingleComposer.GetSlotGrid("slots").CanClickSlot = OnCanClickSlot;
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

	private void OnTextChanced(int index)
	{
		if (!updating)
		{
			UpdateRatios(index);
		}
	}

	public void UpdateRatios(int forceUnchanged = -1)
	{
		updating = true;
		int num = 0;
		float num2 = 0f;
		for (int i = 0; i < 10; i++)
		{
			ItemSlot itemSlot = inv[i];
			num += ((itemSlot.Itemstack != null) ? 1 : 0);
			GuiElementNumberInput numberInput = base.SingleComposer.GetNumberInput("chance" + (i + 1));
			num2 += numberInput.GetValue();
		}
		float num3 = 100f / num2;
		int num4 = 0;
		for (int j = 0; j < 10; j++)
		{
			GuiElementNumberInput numberInput2 = base.SingleComposer.GetNumberInput("chance" + (j + 1));
			if (inv[j].Itemstack == null)
			{
				numberInput2.SetValue("");
				continue;
			}
			int num5 = (int)(numberInput2.GetValue() * num3);
			if (numberInput2.GetText().Length != 0)
			{
				if ((j != forceUnchanged || (int)numberInput2.GetValue() > 100) && num2 > 100f)
				{
					numberInput2.SetValue(num5.ToString() ?? "");
					num4 += num5;
				}
				else
				{
					num4 += (int)numberInput2.GetValue();
				}
			}
		}
		updating = false;
		GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("totalchance");
		int num6 = num4;
		dynamicText.SetNewText("Total chance: " + num6 + "%");
	}

	private bool OnCanClickSlot(int slotID)
	{
		ItemStack itemstack = capi.World.Player.InventoryManager.MouseItemSlot.Itemstack;
		if (itemstack == null)
		{
			inv[slotID].Itemstack = null;
		}
		else
		{
			inv[slotID].Itemstack = itemstack.Clone();
		}
		inv[slotID].MarkDirty();
		UpdateRatios();
		return false;
	}

	private void SendInvPacket(object t1)
	{
		UpdateRatios();
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}
}
