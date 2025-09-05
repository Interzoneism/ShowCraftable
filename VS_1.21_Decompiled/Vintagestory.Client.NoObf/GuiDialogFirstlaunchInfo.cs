using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class GuiDialogFirstlaunchInfo : GuiDialog
{
	private string playstyle;

	public override string ToggleKeyCombinationCode => "firstlaunchinfo";

	public GuiDialogFirstlaunchInfo(ICoreClientAPI capi)
		: base(capi)
	{
		Compose();
		capi.ChatCommands.Create("firstlaunchinfo").WithDescription("Show the first launch info dialog").HandleWith(OnCmd);
	}

	private TextCommandResult OnCmd(TextCommandCallingArgs textCommandCallingArgs)
	{
		if (IsOpened())
		{
			TryClose();
		}
		else
		{
			TryOpen();
		}
		return TextCommandResult.Success();
	}

	private void Compose()
	{
		string vtmlCode = ((playstyle == "creativebuilding") ? Lang.Get("start-creativeintro") : Lang.Get("start-survivalintro"));
		CairoFont baseFont = CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.149999976158142);
		RichTextComponentBase[] components = VtmlUtil.Richtextify(capi, vtmlCode, baseFont, didClickLink);
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, 400.0, 300.0);
		elementBounds.ParentBounds = ElementBounds.Empty;
		GuiElementRichtext guiElementRichtext = new GuiElementRichtext(capi, components, elementBounds);
		guiElementRichtext.BeforeCalcBounds();
		elementBounds.ParentBounds = null;
		float num = (float)(guiElementRichtext.Bounds.fixedY + guiElementRichtext.Bounds.fixedHeight);
		ClearComposers();
		base.SingleComposer = capi.Gui.CreateCompo("helpdialog", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding), withTitleBar: false).BeginChildElements()
			.AddInteractiveElement(guiElementRichtext)
			.AddSmallButton(Lang.Get("button-close"), OnClose, ElementStdBounds.MenuButton((num + 50f) / 80f).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(6.0))
			.AddSmallButton(Lang.Get("button-close-noshow"), OnCloseAndDontShow, ElementStdBounds.MenuButton((num + 50f) / 80f).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(6.0))
			.EndChildElements()
			.Compose();
	}

	private void didClickLink(LinkTextComponent component)
	{
		TryClose();
		component.HandleLink();
	}

	public override void OnGuiOpened()
	{
		Compose();
		base.OnGuiOpened();
	}

	private bool OnCloseAndDontShow()
	{
		TryClose();
		if (playstyle == "creativebuilding")
		{
			ClientSettings.ShowCreativeHelpDialog = false;
		}
		else
		{
			ClientSettings.ShowSurvivalHelpDialog = false;
		}
		return true;
	}

	private bool OnClose()
	{
		TryClose();
		return true;
	}

	public override void OnLevelFinalize()
	{
		playstyle = (capi.World as ClientMain).ServerInfo.Playstyle;
	}
}
