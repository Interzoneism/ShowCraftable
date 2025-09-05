using System;
using System.IO;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogSignPost : GuiDialogGeneric
{
	private BlockPos blockEntityPos;

	public Action<string[]> OnTextChanged;

	public Action OnCloseCancel;

	private bool didSave;

	private CairoFont signPostFont;

	private bool ignorechange;

	public GuiDialogSignPost(string DialogTitle, BlockPos blockEntityPos, string[] textByCardinalDirection, ICoreClientAPI capi, CairoFont signPostFont)
		: base(DialogTitle, capi)
	{
		this.signPostFont = signPostFont;
		this.blockEntityPos = blockEntityPos;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 150.0, 20.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 15.0, 150.0, 25.0);
		ElementBounds elementBounds3 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds3.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftTop).WithFixedAlignmentOffset(60.0 + GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
		float num = 27f;
		float num2 = 32f;
		float num3 = 250f;
		base.SingleComposer = capi.Gui.CreateCompo("blockentitytexteditordialog", bounds).AddShadedDialogBG(elementBounds3).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
			.BeginChildElements(elementBounds3)
			.AddStaticText(Lang.Get("North"), CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy().WithFixedWidth(num3))
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy().WithFixedWidth(num3), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text0")
			.AddStaticText(Lang.Get("Northeast"), CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, num2).WithFixedWidth(num3))
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, num).WithFixedWidth(num3), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text1")
			.AddStaticText(Lang.Get("East"), CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, num2).WithFixedWidth(num3))
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, num).WithFixedWidth(num3), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text2")
			.AddStaticText(Lang.Get("Southeast"), CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, num2).WithFixedWidth(num3))
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, num).WithFixedWidth(num3), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text3")
			.AddStaticText(Lang.Get("South"), CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, num2).WithFixedWidth(num3))
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, num).WithFixedWidth(num3), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text4")
			.AddStaticText(Lang.Get("Southwest"), CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, num2).WithFixedWidth(num3))
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, num).WithFixedWidth(num3), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text5")
			.AddStaticText(Lang.Get("West"), CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, num2).WithFixedWidth(num3))
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, num).WithFixedWidth(num3), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text6")
			.AddStaticText(Lang.Get("Northwest"), CairoFont.WhiteDetailText(), elementBounds = elementBounds.BelowCopy(0.0, num2).WithFixedWidth(num3))
			.AddTextInput(elementBounds2 = elementBounds2.BelowCopy(0.0, num).WithFixedWidth(num3), OnTextChangedDlg, CairoFont.WhiteSmallText(), "text7")
			.AddSmallButton(Lang.Get("Cancel"), OnButtonCancel, elementBounds2 = elementBounds2.BelowCopy(0.0, 20.0).WithFixedSize(100.0, 20.0).WithAlignment(EnumDialogArea.LeftFixed)
				.WithFixedPadding(10.0, 2.0))
			.AddSmallButton(Lang.Get("Save"), OnButtonSave, elementBounds2 = elementBounds2.FlatCopy().WithFixedSize(100.0, 20.0).WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedPadding(10.0, 2.0))
			.EndChildElements()
			.Compose();
		for (int i = 0; i < 8; i++)
		{
			base.SingleComposer.GetTextInput("text" + i).SetValue(textByCardinalDirection[i]);
		}
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
	}

	private void OnTextChangedDlg(string text)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		if (ignorechange)
		{
			return;
		}
		ignorechange = true;
		ImageSurface val = new ImageSurface((Format)0, 1, 1);
		Context val2 = new Context((Surface)(object)val);
		signPostFont.SetupContext(val2);
		string[] array = new string[8];
		for (int i = 0; i < 8; i++)
		{
			GuiElementTextInput textInput = base.SingleComposer.GetTextInput("text" + i);
			array[i] = textInput.GetText();
			if (array[i] == null)
			{
				array[i] = "";
			}
			int num = 0;
			while (true)
			{
				TextExtents val3 = val2.TextExtents(array[i]);
				if (!(((TextExtents)(ref val3)).Width > 185.0) || num++ >= 100)
				{
					break;
				}
				array[i] = array[i].Substring(0, array[i].Length - 1);
			}
			textInput.SetValue(array[i]);
		}
		OnTextChanged?.Invoke(array);
		ignorechange = false;
		((Surface)val).Dispose();
		val2.Dispose();
	}

	private void OnTitleBarClose()
	{
		OnButtonCancel();
	}

	private bool OnButtonSave()
	{
		string[] array = new string[8];
		for (int i = 0; i < 8; i++)
		{
			GuiElementTextInput textInput = base.SingleComposer.GetTextInput("text" + i);
			array[i] = textInput.GetText();
		}
		byte[] data;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			for (int j = 0; j < 8; j++)
			{
				binaryWriter.Write(array[j]);
			}
			data = memoryStream.ToArray();
		}
		capi.Network.SendBlockEntityPacket(blockEntityPos, 1002, data);
		didSave = true;
		TryClose();
		return true;
	}

	private bool OnButtonCancel()
	{
		TryClose();
		return true;
	}

	public override void OnGuiClosed()
	{
		if (!didSave)
		{
			OnCloseCancel?.Invoke();
		}
		base.OnGuiClosed();
	}
}
