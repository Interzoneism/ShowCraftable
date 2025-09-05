using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogBlockEntityConditional : GuiDialogBlockEntity
{
	public override string ToggleKeyCombinationCode => null;

	public GuiDialogBlockEntityConditional(BlockPos BlockEntityPosition, string command, bool latching, ICoreClientAPI capi, string title)
		: base("Conditional block", BlockEntityPosition, capi)
	{
		_ = GuiElementItemSlotGridBase.unscaledSlotPadding;
		int num = 5;
		_ = GuiElementPassiveItemSlot.unscaledSlotSize;
		_ = GuiElementItemSlotGridBase.unscaledSlotPadding;
		double num2 = 600.0;
		_ = num2 / 2.0;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 30.0, num2, 30.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 0.0, num2 - 20.0, 80.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(0.0, 0.0, num2 - 20.0 - 1.0, 79.0).FixedUnder(elementBounds, num - 10);
		ElementBounds bounds = elementBounds3.CopyOffsetedSibling(elementBounds3.fixedWidth + 6.0, -1.0).WithFixedWidth(20.0).FixedGrow(0.0, 2.0);
		ElementBounds elementBounds4 = ElementBounds.Fixed(0.0, 0.0, num2 - 40.0, 20.0).FixedUnder(elementBounds3, 2 + 2 * num);
		ElementBounds elementBounds5 = ElementBounds.Fixed(0.0, 0.0, num2 - 20.0, 25.0).FixedUnder(elementBounds4, 2.0);
		ElementBounds elementBounds6 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds5, 4 + 2 * num).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds bounds2 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds6, 34 + 2 * num).WithFixedPadding(10.0, 2.0);
		ElementBounds bounds3 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds6, 34 + 2 * num).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds elementBounds7 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds7.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds4 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		if (base.SingleComposer != null)
		{
			base.SingleComposer.Dispose();
		}
		base.SingleComposer = capi.Gui.CreateCompo("commandeditordialog", bounds4).AddShadedDialogBG(elementBounds7).AddDialogTitleBar(title, OnTitleBarClose)
			.BeginChildElements(elementBounds7)
			.AddStaticText(Lang.Get("Condition (e.g. e[type=gazelle,range=10])"), CairoFont.WhiteSmallText(), elementBounds)
			.BeginClip(elementBounds3)
			.AddTextArea(elementBounds2, OnCommandCodeChanged, CairoFont.TextInput().WithFontSize(16f), "commands")
			.EndClip()
			.AddVerticalScrollbar(OnNewCmdScrollbarvalue, bounds, "scrollbar")
			.AddStaticText(Lang.Get("Condition syntax status"), CairoFont.WhiteSmallText(), elementBounds4)
			.AddInset(elementBounds5)
			.AddDynamicText("", CairoFont.WhiteSmallText(), elementBounds5.ForkContainingChild(2.0, 2.0, 2.0, 2.0), "result")
			.AddSmallButton(Lang.Get("Cancel"), OnCancel, bounds2)
			.AddSwitch(null, elementBounds5.BelowCopy(0.0, 10.0).WithFixedSize(30.0, 30.0), "latchingSwitch", 25.0, 3.0)
			.AddStaticText(Lang.Get("Latching"), CairoFont.WhiteSmallText(), elementBounds5.BelowCopy(30.0, 13.0).WithFixedSize(150.0, 30.0))
			.AddHoverText(Lang.Get("If latching is enabled, a repeatedly ticked Conditional Block only activates neibouring Command Block once, each time the condition changes"), CairoFont.WhiteSmallText(), 250, elementBounds5.BelowCopy(25.0, 10.0).WithFixedSize(82.0, 25.0))
			.AddSmallButton(Lang.Get("Copy to clipboard"), OnCopy, elementBounds6)
			.AddSmallButton(Lang.Get("Save"), OnSave, bounds3)
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetTextArea("commands").SetValue(command);
		base.SingleComposer.GetTextArea("commands").OnCursorMoved = OnTextAreaCursorMoved;
		base.SingleComposer.GetSwitch("latchingSwitch").On = latching;
		base.SingleComposer.GetScrollbar("scrollbar").SetHeights((float)elementBounds2.fixedHeight - 1f, (float)elementBounds2.fixedHeight);
		base.SingleComposer.UnfocusOwnElements();
	}

	private bool OnCopy()
	{
		capi.Input.ClipboardText = base.SingleComposer.GetTextArea("commands").GetText();
		return true;
	}

	private void OnNewCmdScrollbarvalue(float value)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
		textArea.Bounds.fixedY = 1f - value;
		textArea.Bounds.CalcWorldBounds();
	}

	private bool OnCancel()
	{
		TryClose();
		return true;
	}

	private bool OnSave()
	{
		string text = base.SingleComposer.GetTextArea("commands").GetText();
		bool silent = base.SingleComposer.GetSwitch("latchingSwitch").On;
		capi.Network.SendBlockEntityPacket(base.BlockEntityPosition, 12, SerializerUtil.Serialize(new BlockEntityCommandPacket
		{
			Commands = text,
			Silent = silent
		}));
		TryClose();
		return true;
	}

	private void OnCommandCodeChanged(string t1)
	{
		string text = t1.Trim();
		if (text.Length == 0)
		{
			base.SingleComposer.GetDynamicText("result").SetNewText("");
			return;
		}
		string text2 = "Ok";
		ICommandArgumentParser commandArgumentParser2;
		if (!text.StartsWith("isBlock"))
		{
			ICommandArgumentParser commandArgumentParser = new EntitiesArgParser("test", capi, isMandatoryArg: true);
			commandArgumentParser2 = commandArgumentParser;
		}
		else
		{
			ICommandArgumentParser commandArgumentParser = new IsBlockArgParser("cond", capi, isMandatoryArg: true);
			commandArgumentParser2 = commandArgumentParser;
		}
		ICommandArgumentParser commandArgumentParser3 = commandArgumentParser2;
		TextCommandCallingArgs textCommandCallingArgs = new TextCommandCallingArgs();
		textCommandCallingArgs.Caller = new Caller
		{
			Type = EnumCallerType.Console,
			CallerRole = "admin",
			CallerPrivileges = new string[1] { "*" },
			FromChatGroupId = GlobalConstants.ConsoleGroup,
			Pos = new Vec3d(0.5, 0.5, 0.5)
		};
		textCommandCallingArgs.RawArgs = new CmdArgs(text);
		TextCommandCallingArgs args = textCommandCallingArgs;
		if (commandArgumentParser3.TryProcess(args) != EnumParseResult.Good)
		{
			text2 = commandArgumentParser3.LastErrorMessage;
		}
		base.SingleComposer.GetDynamicText("result").SetNewText(text2);
	}

	private void OnTextAreaCursorMoved(double posX, double posY)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		FontExtents fontExtents = base.SingleComposer.GetTextArea("commands").Font.GetFontExtents();
		double height = ((FontExtents)(ref fontExtents)).Height;
		base.SingleComposer.GetScrollbar("scrollbar").EnsureVisible(posX, posY);
		base.SingleComposer.GetScrollbar("scrollbar").EnsureVisible(posX, posY + height + 5.0);
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}
}
