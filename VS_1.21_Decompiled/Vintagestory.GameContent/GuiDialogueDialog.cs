using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class GuiDialogueDialog : GuiDialog
{
	protected GuiDialog chatDialog;

	private ElementBounds clipBounds;

	private GuiElementRichtext textElem;

	private EntityAgent npcEntity;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogueDialog(ICoreClientAPI capi, EntityAgent npcEntity)
		: base(capi)
	{
		this.npcEntity = npcEntity;
	}

	public void InitAndOpen()
	{
		Compose();
		TryOpen();
	}

	public void ClearDialogue()
	{
		RichTextComponentBase[] components = textElem.Components;
		for (int i = 0; i < components.Length; i++)
		{
			components[i].Dispose();
		}
		GuiElementRichtext guiElementRichtext = textElem;
		components = Array.Empty<RichTextComponent>();
		guiElementRichtext.SetNewText(components);
	}

	public void EmitDialogue(RichTextComponentBase[] cmps)
	{
		RichTextComponentBase[] components = textElem.Components;
		for (int i = 0; i < components.Length; i++)
		{
			if (components[i] is LinkTextComponent linkTextComponent)
			{
				linkTextComponent.Clickable = false;
			}
		}
		textElem.AppendText(cmps);
		updateScrollbarBounds();
	}

	public override void OnKeyDown(KeyEvent args)
	{
		if (args.KeyCode >= 110 && args.KeyCode < 118)
		{
			int num = args.KeyCode - 110;
			int num2 = 0;
			RichTextComponentBase[] components = textElem.Components;
			for (int i = 0; i < components.Length; i++)
			{
				if (components[i] is LinkTextComponent { Clickable: not false } linkTextComponent)
				{
					if (num2 == num)
					{
						linkTextComponent.Trigger();
						args.Handled = true;
						break;
					}
					num2++;
				}
			}
		}
		base.OnKeyDown(args);
	}

	public void Compose()
	{
		ClearComposers();
		CairoFont.WhiteMediumText().WithFont(GuiStyle.DecorativeFontName).WithColor(GuiStyle.DiscoveryTextColor)
			.WithStroke(GuiStyle.DialogBorderColor, 2.0)
			.WithOrientation(EnumTextOrientation.Center);
		ElementBounds elementBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		int num = 600;
		int num2 = 470;
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 30.0, num, num2);
		clipBounds = elementBounds2.ForkBoundingParent();
		ElementBounds elementBounds3 = elementBounds2.FlatCopy().FixedGrow(3.0).WithFixedOffset(-2.0, -2.0);
		ElementBounds bounds2 = elementBounds3.CopyOffsetedSibling(3.0 + elementBounds2.fixedWidth + 7.0).WithFixedWidth(20.0);
		ElementBounds elementBounds4 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		string text = npcEntity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName;
		string text2 = Lang.Get("tradingwindow-" + npcEntity.Code.Path, text);
		base.SingleComposer = capi.Gui.CreateCompo("dialogue-" + npcEntity.EntityId, bounds).AddShadedDialogBG(elementBounds).AddDialogTitleBar(text2, OnTitleBarClose)
			.BeginChildElements(elementBounds)
			.BeginClip(clipBounds)
			.AddInset(elementBounds3, 3)
			.AddRichtext("", CairoFont.WhiteSmallText(), elementBounds2.WithFixedPadding(5.0).WithFixedSize(num - 10, num2 - 10), "dialogueText")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarValue, bounds2, "scrollbar")
			.AddSmallButton(Lang.Get("Goodbye!"), OnByeClicked, elementBounds4.FixedUnder(clipBounds, 20.0))
			.Compose();
		textElem = base.SingleComposer.GetRichtext("dialogueText");
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	private bool OnByeClicked()
	{
		TryClose();
		return true;
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
	}

	private void updateScrollbarBounds()
	{
		if (textElem != null)
		{
			GuiElementScrollbar scrollbar = base.SingleComposer.GetScrollbar("scrollbar");
			scrollbar.Bounds.CalcWorldBounds();
			scrollbar.SetHeights((float)clipBounds.fixedHeight, (float)textElem.Bounds.fixedHeight);
			scrollbar.ScrollToBottom();
		}
	}

	private void OnNewScrollbarValue(float value)
	{
		textElem.Bounds.fixedY = 0f - value;
		textElem.Bounds.CalcWorldBounds();
	}

	public override void OnFinalizeFrame(float dt)
	{
		base.OnFinalizeFrame(dt);
		EntityPos pos = capi.World.Player.Entity.Pos;
		if (IsOpened() && pos.SquareDistanceTo(npcEntity.Pos) > 25f)
		{
			capi.Event.EnqueueMainThreadTask(delegate
			{
				TryClose();
			}, "closedlg");
		}
	}
}
