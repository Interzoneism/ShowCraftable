using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace Vintagestory.Client.NoObf;

public class GuiDialogStoryGenFailed : GuiDialog
{
	public StoryGenFailed storyGenFailed;

	public bool isInitilized;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogStoryGenFailed(ICoreClientAPI capi)
		: base(capi)
	{
	}

	private void Compose()
	{
		CairoFont baseFont = CairoFont.WhiteSmallText();
		ElementBounds bounds = ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding);
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 600.0, 500.0);
		ElementBounds refBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 30.0);
		ElementBounds elementBounds2 = elementBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0).FixedUnder(refBounds);
		ElementBounds elementBounds3 = elementBounds.CopyOffsetedSibling();
		ElementBounds bounds2 = ElementStdBounds.VerticalScrollbar(elementBounds2);
		string text = Lang.Get("storygenfailed-text");
		string text2 = ((storyGenFailed?.MissingStructures != null) ? string.Join(",", storyGenFailed.MissingStructures) : "");
		text = text + "\n" + text2 + "<br><br>";
		base.SingleComposer = capi.Gui.CreateCompo("storygenfailed", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(bounds).AddDialogTitleBar(Lang.Get("Automatic Story Location Generation Failed"), OnTitleBarClose)
			.BeginChildElements(bounds)
			.AddInset(elementBounds2)
			.AddVerticalScrollbar(OnNewScrollbarvalue, bounds2, "scrollbar")
			.BeginClip(elementBounds3)
			.AddRichtext(text, baseFont, elementBounds, null, "storygenfailed")
			.EndClip()
			.EndChildElements()
			.Compose();
		elementBounds3.CalcWorldBounds();
		base.SingleComposer.GetScrollbar("scrollbar").SetHeights((float)elementBounds3.fixedHeight, (float)elementBounds.fixedHeight);
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = base.SingleComposer.GetRichtext("storygenfailed").Bounds;
		bounds.fixedY = 10f - value;
		bounds.CalcWorldBounds();
	}

	private bool OnOk()
	{
		TryClose();
		return true;
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override void OnGuiOpened()
	{
		Compose();
		base.OnGuiOpened();
	}

	public override void OnLevelFinalize()
	{
		isInitilized = true;
		if (storyGenFailed != null)
		{
			Compose();
			TryOpen();
		}
	}
}
